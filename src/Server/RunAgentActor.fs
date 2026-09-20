namespace Gambol.Server

open System
open Gambol.Shared
open Gambol.CloudAgents

/// Run Agent Actor: pack extract, complete, replace Focus Children.
[<RequireQualifiedAccess>]
module RunAgentActor =

    type private CompleteOutcome =
        | TextReady of string
        | CompleteFailed of string
        | CompleteCancelled

    let private failedFromError =
        function
        | AuthenticationFailed msg -> CompleteFailed msg
        | _ -> CompleteFailed ""

    let private askOptions =
        { AgentOptions.DisplayName = Some "AI"
          ModelHint = None }

    let private actorCaller (input: ActorInput) : Caller =
        { authority = Authority "Actor"
          name = ""
          secret = input.secret }

    let private systemPrompt =
        "You are Ambit AI. You edit outline children under Focus "
        + "in a Zoom-rooted extract."
        + Environment.NewLine
        + Environment.NewLine
        + "The Focus node is your prompt. The next message is that extract "
        + "(mixed Amb / codec text) with Focus marked."
        + Environment.NewLine
        + Environment.NewLine
        + "Return ONLY outline text that replaces every child of Focus."
        + Environment.NewLine
        + "- One sentence per node."
        + Environment.NewLine
        + "- Use outlining for structure (parent/child), not paragraphs "
        + "or prose blocks."
        + Environment.NewLine
        + "- Prefer Amb outline shape when the extract uses Amb."
        + Environment.NewLine
        + "- No preamble, no markdown fences, no explanation outside "
        + "the outline."
        + Environment.NewLine
        + "- Do not rewrite Focus itself."

    let private packExtract (input: ActorInput) =
        let extract = Graph.withFocus (Some input.focusId) input.graph
        AmbDocument.writeWith
            AmbWriteWalk.SuppliedExtract
            extract
            input.zoomId

    let private commandArgs keys repos (input: ActorInput) =
        match Map.tryFind input.commandId input.graph.nodes with
        | None -> { Keyname = None; Reponame = None }
        | Some node -> AiCommandArgs.fromText keys repos node.text

    let private requestCancel config agentId runId =
        AgentRunner.cancel config agentId runId |> ignore

    let private pollUntilDone config agentId runId =
        async {
            use! _cancel =
                Async.OnCancel(fun () ->
                    requestCancel config agentId runId)
            let! ct = Async.CancellationToken
            let rec loop () =
                async {
                    if ct.IsCancellationRequested then
                        requestCancel config agentId runId
                        return CompleteCancelled
                    else
                        match AgentRunner.poll config agentId runId with
                        | Error err -> return failedFromError err
                        | Ok(Finished result) ->
                            return TextReady result.Text
                        | Ok(Failed msg) -> return CompleteFailed msg
                        | Ok Cancelled -> return CompleteCancelled
                        | Ok Creating
                        | Ok Running ->
                            do! Async.Sleep 50
                            return! loop ()
                }
            return! loop ()
        }

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

    let private complete (args: StartArgs) =
        async {
            match AgentRunner.start
                args.Config args.Prompt args.Repos args.Options with
            | Error err -> return failedFromError err
            | Ok(agentId, runId) ->
                return! pollUntilDone args.Config agentId runId
        }

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

    let private runBody keys repos (input: ActorInput) coreChanges =
        async {
            let args = commandArgs keys repos input
            match packExtract input with
            | Error _ -> return ActorFailed ""
            | Ok document ->
                match! complete (toStartArgs keys repos args document) with
                | CompleteCancelled -> return ActorCancelled
                | CompleteFailed msg -> return ActorFailed msg
                | TextReady text ->
                    let! planned = planOnCurrent input coreChanges text
                    match planned with
                    | Error _ -> return ActorFailed ""
                    | Ok ops ->
                        do! postReplace input coreChanges ops
                        return ActorSucceeded
        }

    /// ActorFn for Actor name `ai`. Selection is CoreActorPool's job.
    let actorFn (keys: AiKey list) (repos: AiRepo list) : ActorFn =
        fun input coreChanges ->
            async {
                let! result = runBody keys repos input coreChanges
                do! stop input coreChanges result
            }
