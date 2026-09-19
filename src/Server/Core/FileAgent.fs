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

    type private LoadedFile = {
        dataDir: string
        dependencies: FileAgentDependencies
        eventStream: FileStream
        eventOffsets: int64 ResizeArray
        persistedEventLog: EventLog ref
        state: State ref
        persistClean: bool ref
    }

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

    let private accepted loaded confirmed externalChanges message =
        CoreChanges.accepted
            loaded.state.Value.eventId
            true
            confirmed
            externalChanges
            message

    let private syncPersistChange
        (loaded: LoadedFile)
        (rev: int)
        (preGraph: Graph)
        (postGraph: Graph)
        (ops: Op list)
        =
        let persisted =
            CoreMailboxBackend.runBounded
                loaded.dependencies.changeProcessingTimeoutMs
                (fun () ->
                    loaded.dependencies.persistGraphOps
                        loaded.dataDir
                        preGraph
                        postGraph
                        ops)
        match persisted with
        | Error err -> Error err
        | Ok stamped ->
            // Soft-fail (or prior soft-fail in this process): keep meta behind the
            // log so startup replay can restore graph edits that never hit disk.
            if stamped.message.IsSome then
                loaded.persistClean.Value <- false
            let shouldCheckpoint =
                stamped.message.IsNone && loaded.persistClean.Value
            if not shouldCheckpoint then
                Ok stamped
            else
                match Bookkeeping.writeRevision loaded.dataDir rev with
                | Error err -> Error err
                | Ok () -> Ok stamped

    let private applyOne
        loaded
        (s, confirmations, fresh, changed, externalChanges)
        (event: Ev)
        =
        match loaded.persistedEventLog.Value.events
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
                    let nextState = { s' with eventId = event.id }
                    let applied =
                        CoreMailboxBackend.withAppliedOps event appliedOps
                    Ok(
                        nextState,
                        applied :: confirmations,
                        applied :: fresh,
                        true,
                        externalChanges || amended)

    let private applyBatch loaded (events: Ev list) =
        events
        |> List.fold
            (fun acc event ->
                match acc with
                | Error err -> Error err
                | Ok stateAndLog -> applyOne loaded stateAndLog event)
            (Ok(loaded.state.Value, [], [], false, false))
        |> Result.map (fun (newState, confirmations, fresh, changed, externalChanges) ->
            newState, List.rev confirmations, List.rev fresh, changed, externalChanges)

    let private validatePostChange loaded graphOnly preGraph postGraph =
        if graphOnly then
            Ok ()
        else
            DocumentPersistence.validatePathMoves
                loaded.dataDir
                preGraph
                postGraph
            |> Result.bind (fun () ->
                DocumentPersistence.validateGraphDiskEffects
                    loaded.dataDir
                    preGraph
                    postGraph)

    let private persistPostChange loaded graphOnly changed preGraph newState fresh =
        if changed && not graphOnly then
            let ops =
                fresh
                |> List.collect (fun event ->
                    Ev.ops event |> Option.defaultValue [])
            syncPersistChange
                loaded
                (EventId.value newState.eventId)
                preGraph
                newState.graph
                ops
            |> Result.map Some
        else
            Ok None

    let private preparePostChange
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

    let private commitPostChange
        loaded
        finalState
        ackChanges
        externalChanges
        persistMessage
        (reply: AsyncReplyChannel<Result<CoreChangesAccepted, string>>)
        =
        loaded.state.Value <- finalState
        reply.Reply(
            Ok(accepted loaded ackChanges externalChanges persistMessage))

    let private processPostEvents
        loaded
        (events: Ev list)
        graphOnly
        : Result<CoreChangesAccepted, string> =
        if events.IsEmpty then
            Error "changes must not be empty"
        else
            match applyBatch loaded events with
            | Error err -> Error err
            | Ok (newState, confirmations, fresh, changed, externalChanges) ->
                let preGraph = loaded.state.Value.graph
                match validatePostChange loaded graphOnly preGraph newState.graph with
                | Error err -> Error err
                | Ok () ->
                    match
                        persistPostChange
                            loaded
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
                        loaded.state.Value <- finalState
                        Ok(accepted
                            loaded
                            ackChanges
                            externalChanges
                            persistMessage)

    let private persistHandlers loaded : PersistHandlers = {
        getState = fun () -> Ok loaded.state.Value
        getEventId = fun () -> Ok loaded.state.Value.eventId
        getEventsSince = fun after ->
            Ok(
                EventLog.since after loaded.persistedEventLog.Value
                |> fun log -> log.events)
        appendEvent = fun event ->
            match
                EventLogFile.appendEvent
                    loaded.eventStream
                    loaded.eventOffsets
                    event
            with
            | Error err -> Error err
            | Ok () ->
                loaded.persistedEventLog.Value <-
                    EventLog.restore [ event ] loaded.persistedEventLog.Value
                loaded.state.Value <-
                    { loaded.state.Value with eventId = event.id }
                Ok ()
        applyEvent = fun event graphOnly ->
            processPostEvents loaded [ event ] graphOnly
        snapshotDone = fun _ -> ()
    }

    let private loadOrFail (dataDir: string) =
        match DocumentLoader.tryLoadState dataDir with
        | Ok state -> state
        | Error msg -> failwith msg

    let private reconcileLoaded loaded =
        let recovered =
            EventLog.recover
                loaded.state.Value
                loaded.persistedEventLog.Value
        if
            not (List.isEmpty loaded.persistedEventLog.Value.events)
            && List.isEmpty (snd recovered).events
        then
            EventLogFile.truncate
                loaded.eventStream
                loaded.eventOffsets
        loaded.state.Value <- fst recovered
        loaded.persistedEventLog.Value <- snd recovered

    let createWithDependencies
        (dependencies: FileAgentDependencies)
        (dataDir: string)
        : FileAgent =
        let loadedState = loadOrFail dataDir
        let eventStream = EventLogFile.openStream dataDir
        let eventOffsets = EventLogFile.buildIndex eventStream
        let loaded =
            { dataDir = dataDir
              dependencies = dependencies
              eventStream = eventStream
              eventOffsets = eventOffsets
              persistedEventLog =
                ref (
                    EventLogFile.readAllEvents eventStream eventOffsets
                    |> EventLog.restorePersisted)
              state = ref loadedState
              persistClean = ref true }
        reconcileLoaded loaded
        let capturedInitialState = loaded.state.Value
        eventStream.Seek(0L, SeekOrigin.End) |> ignore
        let onError operation context ex =
            dependencies.appendException operation context ex
        let formatError operation =
            $"Internal server error in FileAgent {operation} (dataDir={dataDir})."
        { handlers = persistHandlers loaded
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
