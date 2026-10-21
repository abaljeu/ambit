namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

type FileAgentDependencies = {
    persistGraphOps:
        string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>
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
        persistClean: bool ref
        lastPersisted: Graph ref
        pendingCheckpoint: EventId list ref
        collect: (PersistSubmit -> unit) option ref
        persistFinished: (Notice -> unit) option ref
    }

    let defaultDependencies (dataDir: string) =
        {
            persistGraphOps = DocumentPersistChange.persistGraphOps
            appendException =
                HttpResponseLog.appendException
                    (HttpResponseLog.logPath dataDir)
                    "FileAgent"
        }

    let private accepted loaded confirmed externalChanges message =
        CoreChanges.accepted
            loaded.state.Value.eventId
            true
            confirmed
            externalChanges
            message

    let private collectPersist
        (loaded: LoadedFile)
        (eventId: EventId)
        (preGraph: Graph)
        (postGraph: Graph)
        (ops: Op list)
        =
        match loaded.collect.Value with
        | None -> Error "persist collectors are not bound"
        | Some collect ->
            loaded.pendingCheckpoint.Value <-
                loaded.pendingCheckpoint.Value @ [ eventId ]
            collect {
                nodeIds = PersistCollectors.owningSpecials postGraph ops
                dataDir = Some loaded.dataDir
                preGraph = preGraph
                postGraph = postGraph
                ops = ops
                kind = PersistKind.Ops
                notify = true
            }
            Ok ()

    let private persistCollected
        (loaded: LoadedFile)
        (eventId: EventId)
        (preGraph: Graph)
        (postGraph: Graph)
        (ops: Op list)
        =
        collectPersist loaded eventId preGraph postGraph ops

    let private applyOne
        loaded
        (graphOnly: bool)
        (s, confirmations, fresh, changed, externalChanges)
        (event: Ev)
        =
        match loaded.persistedEventLog.Value.events
              |> List.tryFind (fun e -> e.submissionId = event.submissionId) with
        | Some storedEvent ->
            Ok(s, storedEvent :: confirmations, fresh, changed, externalChanges)
        | None ->
            match Ev.ops event with
            | None -> Error "Event has no Ops"
            | Some ops ->
                let result, amended, appliedOps =
                    if graphOnly then
                        ChangeAmendment.applyForGraphOnly event.commandName ops s
                    else
                        ChangeAmendment.applyForCommand event.commandName ops s
                match result with
                | ApplyResult.Invalid (_, errMsg) -> Error errMsg
                | ApplyResult.Unchanged _ ->
                    Error "Unchanged submission is rejected."
                | ApplyResult.Changed s' ->
                    let nextState = { s' with eventId = event.id }
                    let applied =
                        CoreMailboxEvents.withAppliedOps event appliedOps
                    Ok(
                        nextState,
                        applied :: confirmations,
                        applied :: fresh,
                        true,
                        externalChanges || amended)

    let private applyBatch loaded (graphOnly: bool) (events: Ev list) =
        events
        |> List.fold
            (fun acc event ->
                match acc with
                | Error err -> Error err
                | Ok stateAndLog -> applyOne loaded graphOnly stateAndLog event)
            (Ok(loaded.state.Value, [], [], false, false))
        |> Result.map (fun (newState, confirmations, fresh, changed, externalChanges) ->
            newState, List.rev confirmations, List.rev fresh, changed, externalChanges)

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

    let private persistPostChange loaded graphOnly changed preGraph newState fresh =
        if changed && not graphOnly then
            let ops =
                fresh
                |> List.collect (fun event ->
                    Ev.ops event |> Option.defaultValue [])
            persistCollected
                loaded
                newState.eventId
                preGraph
                newState.graph
                ops
        else
            Ok ()

    let private preparePostChange
        (newState: State)
        (confirmations: Ev list)
        =
        newState, confirmations, None

    let private commitPostChange
        loaded
        finalState
        ackEvents
        externalChanges
        persistMessage
        (reply: AsyncReplyChannel<Result<CoreChangesAccepted, string>>)
        =
        loaded.state.Value <- finalState
        reply.Reply(
            Ok(accepted loaded ackEvents externalChanges persistMessage))

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
                    | Ok () ->
                        let finalState, ackEvents, persistMessage =
                            preparePostChange newState confirmations
                        loaded.state.Value <- finalState
                        Ok(accepted
                            loaded
                            ackEvents
                            externalChanges
                            persistMessage)

    let private scheduleCatchUp (loaded: LoadedFile) (nodeId: NodeId) =
        match loaded.collect.Value with
        | None -> ()
        | Some collect ->
            let post = loaded.state.Value.graph
            match Map.tryFind nodeId post.nodes with
            | Some node when
                node.parseState = ParseState.Parsed
                && node.persistState = PersistState.Unpersisted ->
                collect {
                    nodeIds = [ nodeId ]
                    dataDir = Some loaded.dataDir
                    preGraph = loaded.lastPersisted.Value
                    postGraph = post
                    ops = []
                    kind = PersistKind.Change
                    notify = true
                }
            | _ -> ()

    /// A clean Edit write checkpoints the event id. A soft-fail keeps the
    /// meta behind the log for the rest of this process.
    let private takeCheckpoint (loaded: LoadedFile) =
        match loaded.pendingCheckpoint.Value with
        | [] -> loaded.state.Value.eventId
        | head :: rest ->
            loaded.pendingCheckpoint.Value <- rest
            head

    let private noteStamped
        (loaded: LoadedFile)
        (_ops: Op list)
        (message: string option)
        =
        let eventId = takeCheckpoint loaded
        match message with
        | Some _ -> loaded.persistClean.Value <- false
        | None when not loaded.persistClean.Value -> ()
        | None ->
            match Bookkeeping.writeEventId loaded.dataDir eventId with
            | Error _ -> loaded.persistClean.Value <- false
            | Ok () ->
                loaded.lastPersisted.Value <- loaded.state.Value.graph

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
        replaceGraph = fun _ graph ->
            loaded.state.Value <-
                { loaded.state.Value with graph = graph }
        noteDiffDone = ignore
        noteParsed = scheduleCatchUp loaded
        noteStamped = noteStamped loaded
        noteWriteFailed = ignore
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
          persistClean = ref true
          lastPersisted = ref loadedState.graph
          pendingCheckpoint = ref []
          collect = ref None
          persistFinished = ref None }

    let private startPersist
        (loaded: LoadedFile)
        (dependencies: FileAgentDependencies)
        =
        loaded.lastPersisted.Value <- loaded.state.Value.graph
        let collect, consumer, requestStop = PersistCollectors.create ()
        loaded.collect.Value <- Some collect
        let join =
            PersistThread.start {
                consumer = consumer
                persistOps = dependencies.persistGraphOps
                persistChange = DocumentPersistChange.persistGraphChange
                finish = PersistThread.finishWhenBound loaded.persistFinished
                postNotice = PersistThread.postWhenBound loaded.persistFinished
                changeFailed = fun _ _ -> ()
            }
        fun () ->
            requestStop ()
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
