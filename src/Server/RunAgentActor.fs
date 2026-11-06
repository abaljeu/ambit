namespace Gambol.Server

open System
open System.Collections.Concurrent
open System.Runtime.CompilerServices
open Gambol.Shared
open Gambol.CloudAgents

/// Run Agent Actor: pack extract, complete, replace Focus Children.
[<RequireQualifiedAccess>]
module RunAgentActor =

    type private CompleteOutcome =
        | TextReady of string
        | StreamedOk
        | CompleteFailed of string
        | CompleteCancelled

    type private LiveDraft =
        { draft: FocusXmlStream.Draft
          lists: Map<NodeId, ChildNode list> }

    let private failedFromError =
        function
        | AuthenticationFailed msg -> CompleteFailed msg
        | _ -> CompleteFailed ""

    let private askOptions =
        { AgentOptions.DisplayName = Some "AI"
          ModelHint = Some "grok-4.7"
          ModelParams =
            [ { ModelParam.Id = "context"
                ModelParam.Value = "256k" }
              { ModelParam.Id = "reasoning_effort"
                ModelParam.Value = "low" }
              { ModelParam.Id = "fast"
                ModelParam.Value = "true" } ] }

    type private Ask =
        { graph: Graph
          focusId: NodeId
          actorNodeId: NodeId
          secret: Credential
          sessionId: string }

    let private askFrom (actorNodeId: NodeId) (run: FunctionRun) : Ask =
        let graph = run.getGraph ()
        let focusId =
            match graph.focus with
            | Some id -> id
            | None -> actorNodeId
        { graph = graph
          focusId = focusId
          actorNodeId = actorNodeId
          secret = run.secret
          sessionId = run.sessionId }

    let private systemPrompt =
        "You are Ambit AI. You edit outline children under Focus."
        + Environment.NewLine
        + Environment.NewLine
        + "The next message is the XML Zoom-rooted extract. "
        + "The Focus / prompt node is the one with css class prompt."
        + Environment.NewLine
        + Environment.NewLine
        + "Return ONLY a <> XML fragment that replaces every "
        + "child of Focus."
        + Environment.NewLine
        + "- Each element is one outline node; element text "
        + "is the node text."
        + Environment.NewLine
        + "- Nest elements for parent/child. Do not use "
        + "indent-outline."
        + Environment.NewLine
        + "- No preamble, no markdown fences, no explanation "
        + "outside the fragment."
        + Environment.NewLine
        + "- Do not rewrite Focus itself."

    let private packExtract (ask: Ask) =
        AiExtractPack.packExtract
            ask.graph
            ask.focusId
            ask.focusId

    let private commandArgs keys repos (ask: Ask) =
        match Map.tryFind ask.actorNodeId ask.graph.nodes with
        | None -> { Keyname = None; Reponame = None }
        | Some node -> AiCommandArgs.fromText keys repos node.text

    let private logCancel runId outcome =
        match outcome with
        | Ok CancelRequested
        | Ok NotCancellable -> ()
        | Error err ->
            eprintfn "RunAgentActor: cancel run %s failed: %A" runId err

    /// Off-thread: the pool fires the token inside the Core mailbox.
    let private requestCancel config agentId runId =
        async { AgentRunner.cancel config agentId runId |> logCancel runId }
        |> Async.Start

    let private requestGrokCancel config sessionId =
        match GrokBotRunner.cancel config sessionId with
        | Ok() -> ()
        | Error err ->
            eprintfn
                "RunAgentActor: cancel grok %s failed: %A"
                sessionId
                err

    let private firstBehaviorToken (ask: Ask) =
        match Map.tryFind ask.actorNodeId ask.graph.nodes with
        | None -> ""
        | Some node ->
            match AiKeys.tokensFromText node.text with
            | token :: _ -> token.ToLowerInvariant()
            | [] -> ""

    let private isGrokBot input = firstBehaviorToken input = "gbot"

    let private nodeGuid (NodeId value) = value.ToString()

    let private postReplace (changes: ActorChanges) ops =
        async {
            let event =
                { id = EventId.zero
                  submissionId = Guid.NewGuid()
                  authority = Authority "Actor"
                  commandName = ""
                  body = EventBody.Change ops }
            let! _ = changes.postChanges [ event ]
            return ()
        }

    let private failedFromStream =
        function
        | AuthenticationFailed msg -> CompleteFailed msg
        | ApiError("cancelled", _) -> CompleteCancelled
        | ApiError("failed", msg) -> CompleteFailed msg
        | _ -> CompleteFailed ""

    let private parentChildren (graph: Graph) parentId =
        Graph.children graph parentId

    let private rememberedChildren
        (lists: Map<NodeId, ChildNode list>)
        graph
        focusId
        parentId
        =
        match Map.tryFind parentId lists with
        | Some kids -> kids
        | None when parentId = focusId ->
            parentChildren graph parentId
        | None -> []

    let private nextChildren
        focusId
        lists
        parentId
        oldChildren
        edge
        =
        match Map.tryFind parentId lists with
        | Some kids -> kids @ [ edge ]
        | None when parentId = focusId -> [ edge ]
        | None -> oldChildren @ [ edge ]

    let private postAdd
        (ask: Ask)
        (changes: ActorChanges)
        (lists: Map<NodeId, ChildNode list>)
        (add: FocusXmlStream.PlannedAdd)
        =
        async {
            let! state = changes.graphAtEvent ()
            let graph =
                match state with
                | Ok s -> s.graph
                | Error _ -> ask.graph
            let oldChildren =
                rememberedChildren
                    lists graph ask.focusId add.parentId
            let edge = ChildNode.owner add.childId
            let newChildren =
                nextChildren
                    ask.focusId
                    lists
                    add.parentId
                    oldChildren
                    edge
            let ops =
                [ Op.NewNode(add.childId, add.text)
                  ChildListWire.replace
                    add.parentId oldChildren newChildren ]
            do! postReplace changes ops
            return Map.add add.parentId newChildren lists
        }

    let private postAddNow (ask: Ask) changes lists add =
        postAdd ask changes lists add |> Async.RunSynchronously

    let private postAdds (ask: Ask) changes lists adds =
        List.fold
            (fun lists add -> postAddNow ask changes lists add)
            lists
            adds

    let private applyChunk ask changes live chunk =
        let tokens, draft =
            FocusXmlStream.push chunk live.draft
        let step =
            FocusXmlStream.apply NodeId.New tokens draft
        { draft = step.draft
          lists = postAdds ask changes live.lists step.adds }

    let private flushLive ask changes live =
        let step = FocusXmlStream.flush NodeId.New live.draft
        { draft = step.draft
          lists = postAdds ask changes live.lists step.adds }

    let private onStreamEvent ask changes live ev =
        match ev with
        | AssistantText chunk ->
            applyChunk ask changes live chunk
        | RunFinished _ -> flushLive ask changes live
        | RunFailed _
        | RunCancelled -> live

    let private streamFold (ask: Ask) changes : StreamFold<LiveDraft> =
        { Seed =
            { draft = FocusXmlStream.start ask.focusId
              lists = Map.empty }
          OnEvent = onStreamEvent ask changes }

    let private streamOutcome
        (ask: Ask)
        (outcome: Result<AgentResult * LiveDraft, AgentError>)
        =
        match outcome with
        | Error err -> failedFromStream err
        | Ok(_, live) when Map.containsKey ask.focusId live.lists ->
            StreamedOk
        | Ok(result, _) -> TextReady result.Text

    let private runStream
        (ask: Ask)
        changes
        (onCancel: unit -> unit)
        (stream:
            StreamFold<LiveDraft> ->
                Result<AgentResult * LiveDraft, AgentError>)
        =
        async {
            use! _cancel = Async.OnCancel onCancel
            let! ct = Async.CancellationToken
            if ct.IsCancellationRequested then
                onCancel ()
                return CompleteCancelled
            else
                let outcome = stream (streamFold ask changes)
                if ct.IsCancellationRequested then
                    return CompleteCancelled
                else
                    return streamOutcome ask outcome
        }

    let private streamUntilDone (ask: Ask) changes (args: StreamArgs) =
        runStream
            ask
            changes
            (fun () ->
                requestCancel args.Config args.AgentId args.RunId)
            (fun fold -> AgentRunner.streamUntilComplete args fold)

    let private promptAndExtract document =
        systemPrompt
        + Environment.NewLine
        + Environment.NewLine
        + document

    let private toStartArgs keys repos (args: AiCommandArgs) document : StartArgs =
        { Config =
            { RunnerConfig.ApiKey = AiKeys.resolve keys args.Keyname }
          Prompt = promptAndExtract document
          Repos = AiRepos.resolve repos args.Reponame
          Options = askOptions }

    let private complete (ask: Ask) changes (args: StartArgs) =
        async {
            match AgentRunner.start
                args.Config args.Prompt args.Repos args.Options with
            | Error err -> return failedFromError err
            | Ok(agentId, runId) ->
                let stream =
                    { Config = args.Config
                      AgentId = agentId
                      RunId = runId
                      MaxWaitMs = None }
                return! streamUntilDone ask changes stream
        }

    let private completeGrok
        (grok: GrokBotBinding)
        (ask: Ask)
        changes
        document
        =
        async {
            let sessionId = ask.sessionId
            let wakeArgs =
                { Config = grok.Config
                  Text = promptAndExtract document
                  CommandId = nodeGuid ask.actorNodeId
                  FocusId = nodeGuid ask.focusId
                  SessionId = sessionId
                  ResponseUrl = grok.ResponseUrl }
            match GrokBotRunner.wake wakeArgs with
            | Error err -> return failedFromError err
            | Ok() ->
                let stream =
                    { Config = grok.Config
                      SessionId = sessionId
                      PollIntervalMs = 50
                      MaxWaitMs = None }
                return!
                    runStream
                        ask
                        changes
                        (fun () ->
                            requestGrokCancel grok.Config sessionId)
                        (fun fold ->
                            GrokBotRunner.streamUntilComplete
                                stream
                                fold)
        }

    let private stop (changes: ActorChanges) result =
        async {
            let! _ = changes.actorStop result
            return ()
        }

    let private planOnCurrent (ask: Ask) (changes: ActorChanges) text =
        async {
            let! state = changes.graphAtEvent ()
            let graph =
                match state with
                | Ok s -> s.graph
                | Error _ -> ask.graph
            return FocusChildrenReplace.plan graph ask.focusId text
        }

    let private runComplete keys repos grok ask changes document =
        if isGrokBot ask then
            completeGrok grok ask changes document
        else
            let args = commandArgs keys repos ask
            complete ask changes (toStartArgs keys repos args document)

    let private runBody keys repos grok (ask: Ask) changes =
        async {
            match packExtract ask with
            | Error _ -> return ActorFailed ""
            | Ok document ->
                match!
                    runComplete keys repos grok ask changes document
                    with
                | CompleteCancelled -> return ActorCancelled
                | CompleteFailed msg -> return ActorFailed msg
                | StreamedOk -> return ActorSucceeded
                | TextReady text ->
                    let! planned = planOnCurrent ask changes text
                    match planned with
                    | Error _ -> return ActorFailed ""
                    | Ok ops ->
                        do! postReplace changes ops
                        return ActorSucceeded
        }

    type private AiMaterials =
        { keys: AiKeySet
          repos: AiRepo list
          grok: GrokBotBinding }

    let private offered =
        ConcurrentDictionary<NodeId, Gambol.Shared.ActorFunction>()

    let private bound =
        ConditionalWeakTable<CoreActorPool, AiMaterials>()

    let private tryBound pool =
        let mutable found = Unchecked.defaultof<AiMaterials>
        if bound.TryGetValue(pool, &found) then Some found else None

    /// Test seam supplies the function for one actor node.
    let offerActor
        (actorNodeId: NodeId)
        (actor: Gambol.Shared.ActorFunction)
        =
        offered[actorNodeId] <- actor

    let internal takeOfferedActor (actorNodeId: NodeId) =
        match offered.TryRemove actorNodeId with
        | true, actor -> Some actor
        | _ -> None

    let internal bindActorMaterials pool keys repos grok =
        bound.Add(pool, { keys = keys; repos = repos; grok = grok })

    /// ActorFunction for Actor name `ai`. The pool mints sessionId.
    let actorFn
        (keys: AiKeySet)
        (repos: AiRepo list)
        (grok: GrokBotBinding)
        (actorNodeId: NodeId)
        : Gambol.Shared.ActorFunction =
        fun run changes ->
            async {
                let ask = askFrom actorNodeId run
                let! result = runBody keys repos grok ask changes
                do! stop changes result
            }

    let internal actorForNode pool actorNodeId =
        match tryBound pool with
        | None -> None
        | Some mats ->
            Some (actorFn mats.keys mats.repos mats.grok actorNodeId)
