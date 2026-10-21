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
        persistClean: bool ref
        lastPersisted: Graph ref
        pendingCheckpoint: EventId list ref
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

    let private accepted loaded confirmed externalChanges message =
        CoreChanges.accepted
            loaded.state.Value.eventId
            true
            confirmed
            externalChanges
            message

    let private pushIds (loaded: LoadedFile) (ids: NodeId list) =
        match loaded.stack.Value with
        | None -> Error "persist stack is not bound"
        | Some stack ->
            ids
            |> List.iter (fun nodeId ->
                PersistRequestStack.push stack { nodeId = nodeId })
            Ok ()

    let private persistedNode (graph: Graph) (nodeId: NodeId) =
        match Map.tryFind nodeId graph.nodes with
        | Some node when
            node.persistState = PersistState.Persisted -> true
        | _ -> false

    /// A new special node is created Persisted, so the thread would skip
    /// its first file write. Mark it Unpersisted before the stack push.
    let private markUnpersisted (graph: Graph) (nodeId: NodeId) =
        if not (persistedNode graph nodeId) then
            graph
        else
            match
                GraphMutate.setPersistState
                    nodeId
                    PersistState.Unpersisted
                    graph
            with
            | Ok next -> next
            | Error _ -> graph

    let private openForWrite (loaded: LoadedFile) (ids: NodeId list) =
        let graph =
            ids
            |> List.fold markUnpersisted loaded.state.Value.graph
        loaded.state.Value <-
            { loaded.state.Value with graph = graph }

    let private persistCollected
        (loaded: LoadedFile)
        (eventId: EventId)
        (_preGraph: Graph)
        (postGraph: Graph)
        (ops: Op list)
        =
        loaded.pendingCheckpoint.Value <-
            loaded.pendingCheckpoint.Value @ [ eventId ]
        let ids = PersistThread.owningSpecials postGraph ops
        openForWrite loaded ids
        if List.isEmpty ids then
            PersistThread.postWhenBound
                loaded.persistFinished
                (Notice.NodeStamped([], None))
            Ok ()
        else
            pushIds loaded ids

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
                    let finalState, ackEvents, persistMessage =
                        preparePostChange newState confirmations
                    loaded.state.Value <- finalState
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
                        Ok(accepted
                            loaded
                            ackEvents
                            externalChanges
                            persistMessage)

    let private scheduleCatchUp (loaded: LoadedFile) (nodeId: NodeId) =
        let post = loaded.state.Value.graph
        match Map.tryFind nodeId post.nodes with
        | Some node when
            node.parseState = ParseState.Parsed
            && node.persistState = PersistState.Unpersisted ->
            pushIds loaded [ nodeId ] |> ignore
        | _ -> ()

    /// A clean Edit write checkpoints the event id. A soft-fail keeps the
    /// meta behind the log for the rest of this process. A second stamp
    /// with an empty queue leaves that checkpoint where it is.
    let private takeCheckpoint (loaded: LoadedFile) =
        match loaded.pendingCheckpoint.Value with
        | [] -> None
        | head :: rest ->
            loaded.pendingCheckpoint.Value <- rest
            Some head

    let private noteStamped
        (loaded: LoadedFile)
        (_ops: Op list)
        (message: string option)
        =
        match takeCheckpoint loaded with
        | None -> ()
        | Some eventId ->
            match message with
            | Some _ -> loaded.persistClean.Value <- false
            | None when not loaded.persistClean.Value -> ()
            | None ->
                match Bookkeeping.writeEventId loaded.dataDir eventId with
                | Error _ -> loaded.persistClean.Value <- false
                | Ok () ->
                    loaded.lastPersisted.Value <-
                        loaded.state.Value.graph

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
          stack = ref None
          persistFinished = ref None }

    let private startPersist
        (loaded: LoadedFile)
        (dependencies: FileAgentDependencies)
        =
        loaded.lastPersisted.Value <- loaded.state.Value.graph
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
