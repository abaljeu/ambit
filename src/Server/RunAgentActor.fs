namespace Gambol.Server

open System
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

    let private actorCaller (input: ActorInput) : Caller =
        { authority = Authority "Actor"
          name = ""
          secret = input.secret }

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

    let private packExtract (input: ActorInput) =
        AiExtractPack.packExtract
            input.graph
            input.zoomId
            input.focusId

    let private commandArgs keys repos (input: ActorInput) =
        match Map.tryFind input.commandId input.graph.nodes with
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

    let private firstBehaviorToken (input: ActorInput) =
        match Map.tryFind input.commandId input.graph.nodes with
        | None -> ""
        | Some node ->
            match AiKeys.tokensFromText node.text with
            | token :: _ -> token.ToLowerInvariant()
            | [] -> ""

    let private isGrokBot input = firstBehaviorToken input = "gbot"

    let private nodeGuid (NodeId value) = value.ToString()

    let private postReplace
        (input: ActorInput)
        (coreChanges: CoreChanges)
        ops
        =
        async {
            let event =
                { id = EventId.zero
                  submissionId = Guid.NewGuid()
                  authority = Authority "Actor"
                  commandName = ""
                  body = EventBody.Change ops }
            let! _ =
                coreChanges.asCaller(actorCaller input).postEvents
                    [ event ]
            return ()
        }

    let private failedFromStream =
        function
        | AuthenticationFailed msg -> CompleteFailed msg
        | ApiError("cancelled", _) -> CompleteCancelled
        | ApiError("failed", msg) -> CompleteFailed msg
        | _ -> CompleteFailed ""

    let private parentChildren (graph: Graph) parentId =
        match Map.tryFind parentId graph.nodes with
        | Some node -> node.children
        | None -> []

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
        (input: ActorInput)
        (coreChanges: CoreChanges)
        (lists: Map<NodeId, ChildNode list>)
        (add: FocusXmlStream.PlannedAdd)
        =
        async {
            let! state = coreChanges.getState ()
            let graph =
                match state with
                | Ok s -> s.graph
                | Error _ -> input.graph
            let oldChildren =
                rememberedChildren
                    lists graph input.focusId add.parentId
            let edge = ChildNode.owner add.childId
            let newChildren =
                nextChildren
                    input.focusId
                    lists
                    add.parentId
                    oldChildren
                    edge
            let ops =
                [ Op.NewNode(add.childId, add.text)
                  ChildListWire.replace
                    add.parentId oldChildren newChildren ]
            do! postReplace input coreChanges ops
            return Map.add add.parentId newChildren lists
        }

    let private postAddNow
        (input: ActorInput)
        coreChanges
        lists
        add
        =
        postAdd input coreChanges lists add
        |> Async.RunSynchronously

    let private postAdds
        (input: ActorInput)
        coreChanges
        lists
        adds
        =
        List.fold
            (fun lists add ->
                postAddNow input coreChanges lists add)
            lists
            adds

    let private applyChunk
        (input: ActorInput)
        coreChanges
        live
        chunk
        =
        let tokens, draft =
            FocusXmlStream.push chunk live.draft
        let step =
            FocusXmlStream.apply NodeId.New tokens draft
        { draft = step.draft
          lists = postAdds input coreChanges live.lists step.adds }

    let private flushLive
        (input: ActorInput)
        coreChanges
        live
        =
        let step = FocusXmlStream.flush NodeId.New live.draft
        { draft = step.draft
          lists = postAdds input coreChanges live.lists step.adds }

    let private onStreamEvent
        (input: ActorInput)
        coreChanges
        live
        ev
        =
        match ev with
        | AssistantText chunk ->
            applyChunk input coreChanges live chunk
        | RunFinished _ ->
            flushLive input coreChanges live
        | RunFailed _
        | RunCancelled ->
            live

    let private streamFold
        (input: ActorInput)
        coreChanges
        : StreamFold<LiveDraft>
        =
        { Seed =
            { draft = FocusXmlStream.start input.focusId
              lists = Map.empty }
          OnEvent = onStreamEvent input coreChanges }

    let private streamOutcome
        (input: ActorInput)
        (outcome: Result<AgentResult * LiveDraft, AgentError>)
        =
        match outcome with
        | Error err -> failedFromStream err
        | Ok(_, live) when Map.containsKey input.focusId live.lists ->
            StreamedOk
        | Ok(result, _) -> TextReady result.Text

    let private runStream
        (input: ActorInput)
        coreChanges
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
                let outcome = stream (streamFold input coreChanges)
                if ct.IsCancellationRequested then
                    return CompleteCancelled
                else
                    return streamOutcome input outcome
        }

    let private streamUntilDone
        (input: ActorInput)
        coreChanges
        (args: StreamArgs)
        =
        runStream
            input
            coreChanges
            (fun () ->
                requestCancel args.Config args.AgentId args.RunId)
            (fun fold -> AgentRunner.streamUntilComplete args fold)

    let private toStartArgs keys repos (args: AiCommandArgs) document : StartArgs =
        { Config =
            { RunnerConfig.ApiKey = AiKeys.resolve keys args.Keyname }
          Prompt =
            systemPrompt
            + Environment.NewLine
            + Environment.NewLine
            + document
          Repos = AiRepos.resolve repos args.Reponame
          Options = askOptions }

    let private complete
        (input: ActorInput)
        coreChanges
        (args: StartArgs)
        =
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
                return!
                    streamUntilDone input coreChanges stream
        }

    let private completeGrok
        (grok: GrokBotConfig)
        (input: ActorInput)
        coreChanges
        document
        =
        async {
            let sessionId = input.sessionId
            let wakeArgs =
                { Config = grok
                  Text = document
                  CommandId = nodeGuid input.commandId
                  FocusId = nodeGuid input.focusId
                  SessionId = sessionId }
            match GrokBotRunner.wake wakeArgs with
            | Error err -> return failedFromError err
            | Ok() ->
                let stream =
                    { Config = grok
                      SessionId = sessionId
                      PollIntervalMs = 50
                      MaxWaitMs = None }
                return!
                    runStream
                        input
                        coreChanges
                        (fun () -> requestGrokCancel grok sessionId)
                        (fun fold ->
                            GrokBotRunner.streamUntilComplete
                                stream
                                fold)
        }

    let private stop
        (input: ActorInput)
        (coreChanges: CoreChanges)
        result
        =
        async {
            let! _ =
                coreChanges.asCaller(actorCaller input).actorStop
                    result
            return ()
        }

    let private planOnCurrent
        (input: ActorInput)
        (coreChanges: CoreChanges)
        text
        =
        async {
            let! state = coreChanges.getState ()
            let graph =
                match state with
                | Ok s -> s.graph
                | Error _ -> input.graph
            return FocusChildrenReplace.plan graph input.focusId text
        }

    let private runComplete keys repos grok input coreChanges document =
        if isGrokBot input then
            completeGrok grok input coreChanges document
        else
            let args = commandArgs keys repos input
            complete
                input
                coreChanges
                (toStartArgs keys repos args document)

    let private runBody keys repos grok (input: ActorInput) coreChanges =
        async {
            match packExtract input with
            | Error _ -> return ActorFailed ""
            | Ok document ->
                match!
                    runComplete
                        keys repos grok input coreChanges document with
                | CompleteCancelled -> return ActorCancelled
                | CompleteFailed msg -> return ActorFailed msg
                | StreamedOk -> return ActorSucceeded
                | TextReady text ->
                    let! planned = planOnCurrent input coreChanges text
                    match planned with
                    | Error _ -> return ActorFailed ""
                    | Ok ops ->
                        do! postReplace input coreChanges ops
                        return ActorSucceeded
        }

    /// ActorFn for Actor name `ai`. Selection is CoreActorPool's job.
    let actorFn
        (keys: AiKeySet)
        (repos: AiRepo list)
        (grok: GrokBotConfig)
        : ActorFn =
        fun input coreChanges ->
            async {
                let! result = runBody keys repos grok input coreChanges
                do! stop input coreChanges result
            }
