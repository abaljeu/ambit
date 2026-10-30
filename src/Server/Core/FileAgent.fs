namespace Gambol.Server

open System
open System.Collections.Concurrent
open System.IO
open System.Threading
open Gambol.Shared

type FileAgentDependencies = {
    persistNode: string -> Graph -> NodeId -> Result<DateTime, string>
    appendException: string -> string -> exn -> unit
}

// FileAgent — persist filling for one dataDir. Does not start a mailbox.
type FileAgent = private {
    filling: PersistFilling
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
        stack: PersistRequest BlockingCollection option ref
        persistFinished: (Notice -> unit) option ref
    }

    let defaultDependencies (dataDir: string) =
        {
            persistNode = DocumentPersistChange.persistNode
            appendException =
                HttpResponseLog.appendException
                    (HttpResponseLog.logPath dataDir)
                    "FileAgent"
        }

    let private accepted loaded confirmed externalChanges =
        CoreChanges.accepted
            loaded.state.Value.eventId
            true
            confirmed
            externalChanges

    let private applyOne
        loaded
        (graphOnly: bool)
        (s, confirmations, externalChanges)
        (event: Ev)
        =
        match loaded.persistedEventLog.Value.events
              |> List.tryFind (fun e -> e.submissionId = event.submissionId) with
        | Some storedEvent ->
            Ok(s, storedEvent :: confirmations, externalChanges)
        | None ->
            match Ev.ops event with
            | None -> Error "Event has no Ops"
            | Some ops ->
                let result, amended, appliedOps =
                    ChangeAmendment.applyPosted
                        graphOnly
                        event.id
                        event
                        ops
                        s
                match result with
                | ApplyResult.Invalid (_, errMsg) -> Error errMsg
                | ApplyResult.Unchanged _ ->
                    Error "Unchanged submission is rejected."
                | ApplyResult.Changed s' ->
                    let nextState = { s' with eventId = event.id }
                    let applied = Ev.withOps event appliedOps
                    Ok(
                        nextState,
                        applied :: confirmations,
                        externalChanges || amended)

    let private applyBatch loaded (graphOnly: bool) (events: Ev list) =
        events
        |> List.fold
            (fun acc event ->
                match acc with
                | Error err -> Error err
                | Ok stateAndLog -> applyOne loaded graphOnly stateAndLog event)
            (Ok(loaded.state.Value, [], false))
        |> Result.map (fun (newState, confirmations, externalChanges) ->
            newState, List.rev confirmations, externalChanges)

    let private validatePostChange loaded graphOnly preGraph postGraph =
        if graphOnly then
            Ok ()
        else
            DocumentPersistChange.validatePathMoves
                loaded.dataDir
                preGraph
                postGraph
            |> Result.bind (fun () ->
                DocumentPersistChange.validateGraphDiskEffects
                    loaded.dataDir
                    preGraph
                    postGraph)

    let private processPostEvents
        loaded
        (events: Ev list)
        graphOnly
        : Result<CoreChangesAccepted, string> =
        if events.IsEmpty then
            Error "changes must not be empty"
        else
            match applyBatch loaded graphOnly events with
            | Error err -> Error err
            | Ok (newState, confirmations, externalChanges) ->
                let preGraph = loaded.state.Value.graph
                match validatePostChange loaded graphOnly preGraph newState.graph with
                | Error err -> Error err
                | Ok () ->
                    loaded.state.Value <- newState
                    Ok(accepted loaded confirmations externalChanges)

    let private persistHandlers loaded : PersistHandlers = {
        getState = fun () -> Ok loaded.state.Value
        getEventId = fun () -> Ok loaded.state.Value.eventId
        getEventsSince = fun after ->
            Ok(
                EventLog.since after loaded.persistedEventLog.Value
                |> fun log -> log.events)
        getEventLog = fun () -> Ok loaded.persistedEventLog.Value
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
        replaceGraph = fun _notice graph ->
            loaded.state.Value <-
                { loaded.state.Value with graph = graph }
        noteDiffDone = ignore
        noteParsed = ignore
        pushPersist = PersistRequestStack.pushBoundNode loaded.stack
        notePersisted = ignore
        noteWriteFailed = ignore
    }

    let private loadOrFail (dataDir: string) =
        match DocumentLoader.tryLoadState dataDir with
        | Ok state -> state
        | Error msg -> failwith msg

    let private checkpointAhead (state: State) (log: EventLog) =
        EventId.value state.eventId > EventId.value (EventLog.tip log)

    // A checkpoint past the log tip drops the log. Otherwise the graph
    // stays as loaded and the event id becomes the log tip.
    let private reconcileLoaded loaded =
        let state = loaded.state.Value
        let log = loaded.persistedEventLog.Value
        if checkpointAhead state log then
            EventLogFile.truncate
                loaded.eventStream
                loaded.eventOffsets
            loaded.persistedEventLog.Value <-
                EventLog.afterCheckpoint state.eventId
        else
            loaded.state.Value <-
                { state with eventId = EventLog.tip log }

    let private openLoaded
        (dependencies: FileAgentDependencies)
        (dataDir: string)
        (eventStream: FileStream)
        (eventOffsets: int64 ResizeArray)
        (loadedState: State)
        =
        { dataDir = dataDir
          dependencies = dependencies
          eventStream = eventStream
          eventOffsets = eventOffsets
          persistedEventLog =
            ref (
                EventLogFile.readAllEvents eventStream eventOffsets
                |> EventLog.restorePersisted)
          state = ref loadedState
          stack = ref None
          persistFinished = ref None }

    let private startPersist
        (loaded: LoadedFile)
        (dependencies: FileAgentDependencies)
        =
        let stack = PersistRequestStack.create ()
        loaded.stack.Value <- Some stack
        let join =
            PersistThread.start {
                currentState = fun () -> Volatile.Read(&loaded.state).Value
                take = fun () -> PersistRequestStack.takeUntilStop stack
                persistNode =
                    fun graph nodeId ->
                        dependencies.persistNode loaded.dataDir graph nodeId
                postNotice = PersistThread.postWhenBound loaded.persistFinished
            }
        fun () ->
            PersistRequestStack.stop stack
            join ()

    let createWithDependencies
        (dependencies: FileAgentDependencies)
        (dataDir: string)
        : FileAgent =
        let loadedState = loadOrFail dataDir
        let eventStream = EventLogFile.openStream dataDir
        let eventOffsets = EventLogFile.buildIndex eventStream
        let loaded =
            openLoaded
                dependencies dataDir eventStream eventOffsets loadedState
        reconcileLoaded loaded
        let stopPersist = startPersist loaded dependencies
        let capturedInitialState = loaded.state.Value
        eventStream.Seek(0L, SeekOrigin.End) |> ignore
        let onError operation context ex =
            dependencies.appendException operation context ex
        let formatError operation =
            $"Internal server error in FileAgent {operation} (dataDir={dataDir})."
        { filling =
            { handlers = persistHandlers loaded
              onError = onError
              formatError = formatError
              isReady = fun () -> true
              dispose =
                fun () ->
                    stopPersist ()
                    eventStream.Flush()
                    eventStream.Dispose()
              until = None
              bindPersistFinished =
                fun post -> loaded.persistFinished.Value <- Some post }
          initialState = capturedInitialState }

    let create (dataDir: string) : FileAgent =
        createWithDependencies (defaultDependencies dataDir) dataDir

    let internal persist (agent: FileAgent) : PersistFilling =
        agent.filling

    let initialState (agent: FileAgent) : State =
        agent.initialState
