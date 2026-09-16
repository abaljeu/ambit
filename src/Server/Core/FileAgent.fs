namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

type FileAgentDependencies = {
    persistGraphOps:
        string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>
    appendException: string -> string -> exn -> unit
    /// Wall-clock bound for the persistGraphOps call. Overridable so tests can exercise
    /// the timeout path without waiting the full production timeout.
    changeProcessingTimeoutMs: int
}

// FileAgent — persist filling for one dataDir. Does not start a mailbox.
type FileAgent = private {
    handlers: PersistHandlers
    onError: string -> string -> exn -> unit
    formatError: string -> string
    isReady: unit -> bool
    flushSnapshot: unit -> Async<Result<unit, string>>
    dispose: unit -> unit
    initialState: Gambol.Shared.State
}

module FileAgent =

    let defaultDependencies (dataDir: string) =
        {
            persistGraphOps = DocumentPersistence.persistGraphOps
            appendException =
                HttpResponseLog.appendException
                    (HttpResponseLog.logPath dataDir)
                    "FileAgent"
            changeProcessingTimeoutMs =
                CoreMailboxBackend.ChangeProcessingTimeoutMs
        }

    let createWithDependencies
        (dependencies: FileAgentDependencies)
        (dataDir: string)
        : FileAgent =
        let loadedState =
            match DocumentLoader.tryLoadState dataDir with
            | Ok state -> state
            | Error msg -> failwith msg

        let eventStream = EventLogFile.openStream dataDir
        let eventOffsets = EventLogFile.buildIndex eventStream
        let persistedEventLog =
            ref (
                EventLogFile.readAllEvents eventStream eventOffsets
                |> Gambol.Shared.EventLog.restorePersisted)
        let state = ref loadedState
        /// False after a soft file-write failure until process restart (meta stays behind).
        let persistClean = ref true

        let capturedInitialState = state.Value

        eventStream.Seek(0L, SeekOrigin.End) |> ignore

        let accepted confirmed externalChanges message =
            CoreChanges.accepted
                state.Value.revision
                true
                confirmed
                externalChanges
                message

        let syncPersistChange
            (rev: int)
            (preGraph: Graph)
            (postGraph: Graph)
            (ops: Op list)
            =
            let persisted =
                CoreMailboxBackend.runBounded
                    dependencies.changeProcessingTimeoutMs
                    (fun () ->
                        dependencies.persistGraphOps dataDir preGraph postGraph ops)
            match persisted with
            | Error err -> Error err
            | Ok stamped ->
                // Soft-fail (or prior soft-fail in this process): keep meta behind the
                // log so startup replay can restore graph edits that never hit disk.
                if stamped.message.IsSome then
                    persistClean.Value <- false
                let shouldCheckpoint =
                    stamped.message.IsNone && persistClean.Value
                if not shouldCheckpoint then
                    Ok stamped
                else
                    match Bookkeeping.writeRevision dataDir rev with
                    | Error err -> Error err
                    | Ok () -> Ok stamped

        let applyOne
            (s, confirmations, fresh, changed, externalChanges)
            (event: Ev)
            =
            match persistedEventLog.Value.events
                  |> List.tryFind (fun e -> e.submissionId = event.submissionId) with
            | Some storedEvent ->
                Ok(s, storedEvent :: confirmations, fresh, changed, externalChanges)
            | None ->
                match Ev.ops event with
                | None -> Error "Ev has no Ops"
                | Some ops ->
                    let result, amended, appliedOps =
                        ChangeAmendment.applyOps ops s
                    match result with
                    | ApplyResult.Invalid (_, errMsg) -> Error errMsg
                    | ApplyResult.Unchanged _ ->
                        Error "Unchanged submission is rejected."
                    | ApplyResult.Changed s' ->
                        let nextRev = s.revision.Value + 1
                        let nextState = { s' with revision = Revision nextRev }
                        let applied =
                            CoreMailboxBackend.withAppliedOps event appliedOps
                        Ok(
                            nextState,
                            applied :: confirmations,
                            applied :: fresh,
                            true,
                            externalChanges || amended)

        let applyBatch (events: Ev list) =
            events
            |> List.fold
                (fun acc event ->
                    match acc with
                    | Error err -> Error err
                    | Ok stateAndLog -> applyOne stateAndLog event)
                (Ok(state.Value, [], [], false, false))
            |> Result.map (fun (newState, confirmations, fresh, changed, externalChanges) ->
                newState, List.rev confirmations, List.rev fresh, changed, externalChanges)

        let validatePostChange graphOnly preGraph postGraph =
            if graphOnly then
                Ok ()
            else
                DocumentPersistence.validatePathMoves dataDir preGraph postGraph
                |> Result.bind (fun () ->
                    DocumentPersistence.validateGraphDiskEffects
                        dataDir
                        preGraph
                        postGraph)

        let persistPostChange graphOnly changed preGraph newState fresh =
            if changed && not graphOnly then
                let ops =
                    fresh
                    |> List.collect (fun event ->
                        Ev.ops event |> Option.defaultValue [])
                syncPersistChange
                    newState.revision.Value
                    preGraph
                    newState.graph
                    ops
                |> Result.map Some
            else
                Ok None

        let preparePostChange
            (newState: State)
            (confirmations: Ev list)
            (fresh: Ev list)
            (stampedOpt: PersistGraphOk option)
            =
            let stampOps, stampedGraph, persistMessage =
                match stampedOpt with
                | Some stamped ->
                    PersistStamp.opsBetween newState.graph stamped.graph,
                    stamped.graph,
                    stamped.message
                | None -> [], newState.graph, None
            let stampedFresh, ackChanges =
                CoreMailboxBackend.overlayFreshEvents
                    confirmations
                    fresh
                    stampOps
            let finalState =
                match stampedOpt with
                | Some _ -> { newState with graph = stampedGraph }
                | None -> newState
            finalState, ackChanges, persistMessage

        let commitPostChange
            finalState
            ackChanges
            externalChanges
            persistMessage
            (reply: AsyncReplyChannel<Result<CoreChangesAccepted, string>>)
            =
            state.Value <- finalState
            reply.Reply(
                Ok(accepted ackChanges externalChanges persistMessage))

        let leftoverEvents (changes: Change list) =
            changes |> List.map (Ev.ofChange "")

        let processPostEvents
            (events: Ev list)
            graphOnly
            : Result<CoreChangesAccepted, string> =
            if events.IsEmpty then
                Error "changes must not be empty"
            else
                match applyBatch events with
                | Error err -> Error err
                | Ok (newState, confirmations, fresh, changed, externalChanges) ->
                    let preGraph = state.Value.graph
                    match validatePostChange graphOnly preGraph newState.graph with
                    | Error err -> Error err
                    | Ok () ->
                        match
                            persistPostChange
                                graphOnly
                                changed
                                preGraph
                                newState
                                fresh
                        with
                        | Error err -> Error err
                        | Ok stampedOpt ->
                            let finalState, ackChanges, persistMessage =
                                preparePostChange
                                    newState
                                    confirmations
                                    fresh
                                    stampedOpt
                            state.Value <- finalState
                            Ok(accepted
                                ackChanges
                                externalChanges
                                persistMessage)

        let handlers: PersistHandlers = {
            getState = fun () -> Ok state.Value
            getRevision = fun () -> Ok state.Value.revision
            getEventsSince = fun after ->
                Ok(
                    Gambol.Shared.EventLog.since after persistedEventLog.Value
                    |> fun log -> log.events)
            appendEvent = fun event ->
                match EventLogFile.appendEvent eventStream eventOffsets event with
                | Error err -> Error err
                | Ok () ->
                    persistedEventLog.Value <-
                        Gambol.Shared.EventLog.restore
                            [ event ]
                            persistedEventLog.Value
                    Ok ()
            applyEvent = fun event graphOnly ->
                processPostEvents [ event ] graphOnly
            postChange = fun changes ->
                processPostEvents (leftoverEvents changes) false
            postGraphOnlyChange = fun changes ->
                processPostEvents (leftoverEvents changes) true
            snapshotDone = fun _ -> ()
        }

        let onError operation context ex =
            dependencies.appendException operation context ex

        let formatError operation =
            $"Internal server error in FileAgent {operation} (dataDir={dataDir})."

        { handlers = handlers
          onError = onError
          formatError = formatError
          isReady = fun () -> true
          flushSnapshot = fun () -> async { return Ok () }
          dispose =
            fun () ->
                eventStream.Flush()
                eventStream.Dispose()
          initialState = capturedInitialState }

    let create (dataDir: string) : FileAgent =
        createWithDependencies (defaultDependencies dataDir) dataDir

    let persist (agent: FileAgent) : PersistFilling = {
        handlers = agent.handlers
        onError = agent.onError
        formatError = agent.formatError
        isReady = agent.isReady
        flushSnapshot = agent.flushSnapshot
        dispose = agent.dispose
        until = None
        bindSnapshot = ignore
    }

    let initialState (agent: FileAgent) : State =
        agent.initialState
