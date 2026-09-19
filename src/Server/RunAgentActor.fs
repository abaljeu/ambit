namespace Gambol.Server

open System
open Gambol.Shared
open Gambol.CloudAgents

/// Run Agent Actor: pack extract, complete, replace Focus Children.
[<RequireQualifiedAccess>]
module RunAgentActor =

    let private askOptions =
        { AgentOptions.DisplayName = Some "Ask"
          ModelHint = None }

    let private actorCaller (input: ActorInput) : Caller =
        { authority = Authority "Actor"
          name = ""
          secret = input.secret }

    let private focusText (input: ActorInput) =
        match Map.tryFind input.focusId input.graph.nodes with
        | Some node -> node.text
        | None -> ""

    let private systemPrompt (input: ActorInput) =
        "Return outline text that replaces every child of Focus. Focus: "
        + focusText input
        + ". Use Amb outline when possible. Outline only."

    let private packExtract (input: ActorInput) =
        let extract = Graph.withFocus (Some input.focusId) input.graph
        AmbDocument.writeWith
            AmbWriteWalk.SuppliedExtract
            extract
            input.zoomId

    let private runnerConfig () =
        match Environment.GetEnvironmentVariable "CURSOR_API_KEY" with
        | null
        | "" -> { RunnerConfig.ApiKey = "" }
        | value -> { RunnerConfig.ApiKey = value }

    let private complete (input: ActorInput) (document: string) =
        let config = runnerConfig ()
        let prompt =
            systemPrompt input
            + Environment.NewLine
            + Environment.NewLine
            + document
        match AgentRunner.start config prompt None askOptions with
        | Error _ -> Error "agent start failed"
        | Ok(agentId, runId) ->
            match
                AgentRunner.waitUntilComplete
                    config agentId runId 500 None
            with
            | Ok result -> Ok result.Text
            | Error _ -> Error "agent complete failed"

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

    let private runBody (input: ActorInput) coreChanges =
        async {
            match packExtract input with
            | Error _ -> return ActorFailed
            | Ok document ->
                match complete input document with
                | Error _ -> return ActorFailed
                | Ok text ->
                    let! planned = planOnCurrent input coreChanges text
                    match planned with
                    | Error _ -> return ActorFailed
                    | Ok ops ->
                        do! postReplace input coreChanges ops
                        return ActorSucceeded
        }

    /// ActorFn for Actor name `ai`. Selection is CoreActorPool's job.
    let actorFn: ActorFn =
        fun input coreChanges ->
            async {
                let! result = runBody input coreChanges
                do! stop input coreChanges result
            }
