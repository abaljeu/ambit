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
        collect: (PersistSubmit -> PersistOutcome) option ref
        snapshotPost: (InMsg -> unit) option ref
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

    let private acceptWrote
        (loaded: LoadedFile)
        (eventId: EventId)
        (stamped: PersistGraphOk)
        =
        // Soft-fail (or prior soft-fail in this process): keep meta behind the
        // log so startup replay can restore graph edits that never hit disk.
        if stamped.message.IsSome then
            loaded.persistClean.Value <- false
        let shouldCheckpoint =
            stamped.message.IsNone && loaded.persistClean.Value
        if not shouldCheckpoint then
            Ok (Some stamped)
        else
            match Bookkeeping.writeEventId loaded.dataDir eventId with
            | Error err -> Error err
            | Ok () ->
                loaded.lastPersisted.Value <- stamped.graph
                Ok (Some stamped)

    let private collectPersist
        (loaded: LoadedFile)
        (preGraph: Graph)
        (postGraph: Graph)
        (ops: Op list)
        =
        match loaded.collect.Value with
        | None -> Error "persist collectors are not bound"
        | Some collect ->
            Ok (collect {
                nodeIds = PersistCollectors.owningSpecials postGraph ops
                dataDir = Some loaded.dataDir
                preGraph = preGraph
                postGraph = postGraph
                ops = ops
                kind = PersistKind.Ops
                notify = true
                wait = true
            })

    let private fromOutcome
        (loaded: LoadedFile)
        (eventId: EventId)
        (outcome: PersistOutcome)
        =
        match outcome with
        | PersistOutcome.Wrote stamped -> acceptWrote loaded eventId stamped
        | PersistOutcome.Blocked -> Ok None
        | PersistOutcome.Queued -> Ok None
        | PersistOutcome.Failed err -> Error err

    let private persistCollected
        (loaded: LoadedFile)
        (eventId: EventId)
        (preGraph: Graph)
        (postGraph: Graph)
        (ops: Op list)
        =
        match collectPersist loaded preGraph postGraph ops with
        | Error err -> Error err
        | Ok outcome -> fromOutcome loaded eventId outcome

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
        let stampedFresh, ackEvents =
            CoreMailboxEvents.overlayFreshEvents
                confirmations
                fresh
                stampOps
        let finalState =
            match stampedOpt with
            | Some _ -> { newState with graph = stampedGraph }
            | None -> newState
        finalState, ackEvents, persistMessage

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
                    | Ok stampedOpt ->
                        let finalState, ackEvents, persistMessage =
                            preparePostChange
                                newState
                                confirmations
                                fresh
                                stampedOpt
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
                    wait = false
                } |> ignore
            | _ -> ()

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
        replaceGraph = fun graph ->
            loaded.state.Value <-
                { loaded.state.Value with graph = graph }
        snapshotDone = fun _ _ -> ()
        noteParsed = scheduleCatchUp loaded
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
          collect = ref None
          snapshotPost = ref None }

    let private startPersist
        (loaded: LoadedFile)
        (dependencies: FileAgentDependencies)
        =
        loaded.lastPersisted.Value <- loaded.state.Value.graph
        let collect, consumer = PersistCollectors.create ()
        loaded.collect.Value <- Some collect
        PersistThread.start {
            consumer = consumer
            persistOps = dependencies.persistGraphOps
            persistChange = DocumentPersistChange.persistGraphChange
            finish = PersistThread.finishWhenBound loaded.snapshotPost
            changeFailed = fun _ _ -> ()
        }

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
        startPersist loaded dependencies
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
              flushSnapshot = fun () -> async { return Ok () }
              dispose =
                fun () ->
                    eventStream.Flush()
                    eventStream.Dispose()
              until = None
              bindSnapshot =
                fun post -> loaded.snapshotPost.Value <- Some post }
          initialState = capturedInitialState }

    let create (dataDir: string) : FileAgent =
        createWithDependencies (defaultDependencies dataDir) dataDir

    let internal persist (agent: FileAgent) : PersistFilling =
        agent.filling

    let initialState (agent: FileAgent) : State =
        agent.initialState
