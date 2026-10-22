namespace Gambol.Server

open System
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open Gambol.Shared
open Gambol.Shared

/// PostgreSQL-backed persist filling. Does not start a mailbox.
type DbAgent = private {
    filling: PersistFilling
}

[<RequireQualifiedAccess>]
module DbAgent =

    type private LoadedPersist = {
        state: State ref
        baselineGraph: Graph ref
        eventLog: EventLog ref
        diffInProgress: bool ref
        diffNeeded: bool ref
        diffWorkspaces: NodeId list ref
        diffIds: NodeId list ref
        opsNotifiedIds: NodeId list ref
        pendingNotify: NodeId list ref
        diffMarksLeft: int ref
        ready: TaskCompletionSource<unit>
        persistFinished: (Notice -> unit) option ref
        startupError: string option ref
        connectionString: string
        liveSaveDataDir: string option
        persistNode: string -> Graph -> NodeId -> Result<DateTime, string>
        stack: PersistRequest BlockingCollection option ref
    }

    type private LivePersist = {
        stamped: PersistGraphOk option
        allowFileDiff: bool
        notifiedIds: NodeId list
    }

    let private overlayRowId (row: Database.EventRow) (event: Ev) : Ev =
        { event with id = EventId.fromJson row.event_id }

    let private advancePastRowIds (rows: Database.EventRow list) log =
        match rows with
        | [] -> log
        | _ ->
            rows
            |> List.map (fun row -> EventId.fromJson row.event_id)
            |> List.reduce EventId.max
            |> fun maxId -> EventLog.advancePast maxId log

    let private loadReconciled
        (connectionString: string)
        (state: State)
        : State * EventLog =
        let log =
            if String.IsNullOrWhiteSpace connectionString then
                EventLog.empty
            else
                let rows =
                    Database.getEvents connectionString
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                let decoded =
                    rows
                    |> List.choose (fun row ->
                        EventLogFile.decodeEvent row.payload
                        |> Result.toOption
                        |> Option.map (overlayRowId row))
                advancePastRowIds rows (EventLog.restorePersisted decoded)
        let recovered = EventLog.recover state log
        if
            not (String.IsNullOrWhiteSpace connectionString)
            && not (List.isEmpty log.events)
            && List.isEmpty (snd recovered).events
        then
            Database.clearEvents connectionString
            |> Async.AwaitTask
            |> Async.RunSynchronously
        recovered

    let private loadInitialState (connectionString: string) : Async<State> =
        Database.loadPersistedState connectionString
        |> Async.AwaitTask

    let private makeLoaded
        (initialState: State)
        (connectionString: string)
        (liveSaveDataDir: string option)
        persistNode
        : LoadedPersist =
        { state = ref initialState
          baselineGraph = ref initialState.graph
          eventLog = ref EventLog.empty
          diffInProgress = ref false
          diffNeeded = ref false
          diffWorkspaces = ref []
          diffIds = ref []
          opsNotifiedIds = ref []
          pendingNotify = ref []
          diffMarksLeft = ref 0
          ready =
            TaskCompletionSource<unit>(
                TaskCreationOptions.RunContinuationsAsynchronously)
          persistFinished = ref None
          startupError = ref None
          connectionString = connectionString
          liveSaveDataDir = liveSaveDataDir
          persistNode = persistNode
          stack = ref None }

    let private trimDeletedIds loaded deletedIds =
        let deletedNodeIds = deletedIds |> List.map NodeId
        let trim = DatabaseProjection.trimDeletedNodes deletedNodeIds
        loaded.state.Value <-
            { loaded.state.Value with graph = trim loaded.state.Value.graph }
        loaded.baselineGraph.Value <- trim loaded.baselineGraph.Value

    let private applyMaintenance loaded
        (result: DatabaseProjection.ProjectionMaintenanceResult)
        =
        if result.requiresReload then
            match
                Database.tryLoadGraphFromProjection loaded.connectionString
                |> Async.AwaitTask
                |> Async.RunSynchronously
            with
            | Ok (graph, _) ->
                loaded.state.Value <- { loaded.state.Value with graph = graph }
                loaded.baselineGraph.Value <- graph
                Ok ()
            | Error e -> Error $"Startup projection sweep failed: {e}"
        else
            trimDeletedIds loaded result.deletedIds
            Ok ()

    let private accepted loaded confirmed externalChanges message =
        CoreChanges.accepted
            loaded.state.Value.eventId
            loaded.ready.Task.IsCompletedSuccessfully
            confirmed
            externalChanges
            message

    let private tryPersistedEvent loaded submissionId =
        loaded.eventLog.Value.events
        |> List.tryFind (fun e -> e.submissionId = submissionId)

    let private applyOneEvent
        loaded
        (graphOnly: bool)
        ((s: State), confirmations, externalChanges)
        (event: Ev)
        =
        match tryPersistedEvent loaded event.submissionId with
        | Some storedEvent ->
            Ok(s, storedEvent :: confirmations, externalChanges)
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
                        externalChanges || amended)

    let private applyBatch loaded (graphOnly: bool) events =
        try
            events
            |> List.fold
                (fun acc event ->
                    match acc with
                    | Error err -> Error err
                    | Ok stateAndLog ->
                        applyOneEvent loaded graphOnly stateAndLog event)
                (Ok(loaded.state.Value, [], false))
            |> Result.map (fun (newState, confirmations, externalChanges) ->
                newState, List.rev confirmations, externalChanges)
        with ex ->
            eprintfn "DbAgent: failed to apply batch: %s" ex.Message
            Error $"Database error: {ex.Message}"

    let private persistGraphProjection loaded (newState: State) events =
        if String.IsNullOrWhiteSpace loaded.connectionString then
            Ok ()
        elif List.isEmpty (Map.toList newState.graph.nodes) then
            Ok ()
        else
            try
                use conn = Database.getConnection loaded.connectionString
                conn.Open()
                use tx = conn.BeginTransaction()
                let patch =
                    DatabaseProjection.plan
                        newState.graph
                        newState.eventId
                        events
                (DatabaseProjection.persistWithTx tx newState.graph patch)
                    .GetAwaiter()
                    .GetResult()
                tx.Commit()
                Ok ()
            with ex ->
                eprintfn "DbAgent: failed to persist projection: %s" ex.Message
                Error $"Database error: {ex.Message}"

    /// Workspaces that own the ops which requested this file diff.
    let private workspacesTouched (graph: Graph) (events: Ev list) =
        events
        |> List.collect (fun event ->
            Ev.ops event |> Option.defaultValue [])
        |> List.choose PersistThread.opAnchor
        |> List.choose (GraphQuery.enclosingWorkspace graph)
        |> List.distinct

    let private rememberWorkspaces loaded (graph: Graph) (events: Ev list) =
        let more = workspacesTouched graph events
        loaded.diffWorkspaces.Value <-
            loaded.diffWorkspaces.Value @ more |> List.distinct

    let private pushIds (loaded: LoadedPersist) (ids: NodeId list) =
        match loaded.stack.Value with
        | None -> ()
        | Some stack ->
            ids
            |> List.iter (fun nodeId ->
                PersistRequestStack.push stack { nodeId = nodeId })

    let private startFileDiff loaded =
        let graph = loaded.state.Value.graph
        let skip = loaded.opsNotifiedIds.Value
        loaded.opsNotifiedIds.Value <- []
        let pending = loaded.diffWorkspaces.Value
        loaded.diffWorkspaces.Value <- []
        let unparsed id =
            PersistThread.unparsedNode graph id
        let ids =
            pending
            |> List.filter (fun id ->
                not (List.contains id skip) && not (unparsed id))
        let held = pending |> List.filter unparsed
        match ids with
        | [] ->
            if not (List.isEmpty held) then
                loaded.diffWorkspaces.Value <- held
                loaded.diffNeeded.Value <- true
            else
                loaded.diffNeeded.Value <- false
                if List.isEmpty pending then
                    eprintfn
                        "DbAgent: file diff has no owning workspace"
        | _ ->
            if List.isEmpty held then
                loaded.diffNeeded.Value <- false
            else
                loaded.diffWorkspaces.Value <- held
                loaded.diffNeeded.Value <- true
            loaded.diffInProgress.Value <- true
            loaded.diffIds.Value <- ids
            loaded.diffMarksLeft.Value <- List.length ids
            pushIds loaded ids

    let private requestFileDiff loaded =
        match loaded.persistFinished.Value with
        | Some _ -> startFileDiff loaded
        | None -> ()

    let private validatePostChange loaded graphOnly preGraph postGraph =
        match graphOnly, loaded.liveSaveDataDir with
        | true, _ -> Ok ()
        | false, None -> Ok ()
        | false, Some dataDir ->
            DocumentPersistChange.validatePathMoves dataDir preGraph postGraph
            |> Result.bind (fun () ->
                DocumentPersistChange.validateGraphDiskEffects
                    dataDir
                    preGraph
                    postGraph)

    let private liveOf stamped allowFileDiff : LivePersist = {
        stamped = stamped
        allowFileDiff = allowFileDiff
        notifiedIds = []
    }

    let private submitLiveOps loaded postGraph ops =
        match loaded.stack.Value with
        | None -> Error "persist stack is not bound"
        | Some _ ->
            loaded.pendingNotify.Value <-
                PersistThread.owningSpecials postGraph ops
            Ok(liveOf None false)

    let private persistLiveChange loaded graphOnly _preGraph newState fresh =
        match graphOnly, loaded.liveSaveDataDir, fresh with
        | false, Some _, _::_ ->
            let ops =
                fresh
                |> List.collect (fun event ->
                    Ev.ops event |> Option.defaultValue [])
            submitLiveOps loaded newState.graph ops
        | _ -> Ok(liveOf None true)

    let private preparePostChange newState confirmations =
        newState, confirmations, None

    let private commitPostChange
        loaded
        graphOnly
        fresh
        stateToStore
        ackEvents
        externalChanges
        persistMessage
        (live: LivePersist)
        =
        match
            CoreMailboxEvents.runBounded
                CoreMailboxEvents.ChangeProcessingTimeoutMs
                (fun () -> persistGraphProjection loaded stateToStore ackEvents)
        with
        | Error err ->
            loaded.pendingNotify.Value <- []
            Error err
        | Ok () ->
            loaded.state.Value <- stateToStore
            let ids = loaded.pendingNotify.Value
            loaded.pendingNotify.Value <- []
            pushIds loaded ids
            if graphOnly then
                loaded.baselineGraph.Value <- stateToStore.graph
            elif not (List.isEmpty fresh) && live.allowFileDiff then
                loaded.baselineGraph.Value <- stateToStore.graph
                rememberWorkspaces loaded stateToStore.graph fresh
                loaded.opsNotifiedIds.Value <-
                    live.notifiedIds @ loaded.opsNotifiedIds.Value
                    |> List.distinct
                if loaded.diffInProgress.Value then
                    loaded.diffNeeded.Value <- true
                else
                    requestFileDiff loaded
            elif not (List.isEmpty fresh) then
                rememberWorkspaces loaded stateToStore.graph fresh
            Ok(accepted loaded ackEvents externalChanges persistMessage)

    let private finishAppliedPostChange
        loaded
        graphOnly
        newState
        confirmations
        fresh
        externalChanges
        =
        let preGraph = loaded.state.Value.graph
        match validatePostChange loaded graphOnly preGraph newState.graph with
        | Error err -> Error err
        | Ok () ->
            match
                persistLiveChange loaded graphOnly preGraph newState fresh
            with
            | Error err -> Error err
            | Ok live ->
                let stateToStore, ackEvents, persistMessage =
                    preparePostChange newState confirmations
                commitPostChange
                    loaded
                    graphOnly
                    fresh
                    stateToStore
                    ackEvents
                    externalChanges
                    persistMessage
                    live

    let private processPostEvents loaded (events: Ev list) graphOnly =
        if events.IsEmpty then
            Error "changes must not be empty"
        else
            match
                CoreMailboxEvents.runBounded
                    CoreMailboxEvents.ChangeProcessingTimeoutMs
                    (fun () -> applyBatch loaded graphOnly events)
            with
            | Error err -> Error err
            | Ok (newState, confirmations, externalChanges) ->
                if newState.eventId = loaded.state.Value.eventId then
                    Ok(accepted loaded confirmations externalChanges None)
                else
                    let submittedIds =
                        events |> List.map (fun e -> e.submissionId) |> Set.ofList
                    let fresh =
                        confirmations
                        |> List.filter (fun event ->
                            Set.contains event.submissionId submittedIds)
                    finishAppliedPostChange
                        loaded
                        graphOnly
                        newState
                        confirmations
                        fresh
                        externalChanges

    let private countDiffMark loaded nodeId =
        let counts =
            loaded.diffInProgress.Value
            && List.contains nodeId loaded.diffIds.Value
        if counts then
            loaded.diffIds.Value <-
                loaded.diffIds.Value
                |> List.filter (fun id -> id <> nodeId)
            let left = loaded.diffMarksLeft.Value - 1
            if left > 0 then
                loaded.diffMarksLeft.Value <- left
            else
                loaded.diffMarksLeft.Value <- 0
                loaded.diffInProgress.Value <- false
                if loaded.diffNeeded.Value then
                    requestFileDiff loaded

    let private eventsSince loaded after =
        EventLog.since after loaded.eventLog.Value |> fun log -> log.events

    let private recordPersistedEvent loaded (persisted: Ev) =
        loaded.eventLog.Value <-
            EventLog.restore [ persisted ] loaded.eventLog.Value
        loaded.state.Value <-
            { loaded.state.Value with eventId = persisted.id }

    let private writePersistedEvent loaded (persisted: Ev) =
        let n = EventId.value persisted.id
        try
            Database.appendEvent
                loaded.connectionString
                n
                persisted.submissionId
                (EventLogFile.encodeEvent persisted)
            |> Async.AwaitTask
            |> Async.RunSynchronously
            recordPersistedEvent loaded persisted
            Ok ()
        with ex ->
            Error $"Event persist error: {ex.Message}"

    let private appendPersistedEvent
        loaded
        (persisted: Ev)
        =
        if String.IsNullOrWhiteSpace loaded.connectionString then
            recordPersistedEvent loaded persisted
            Ok ()
        else
            CoreMailboxEvents.runBounded
                CoreMailboxEvents.ChangeProcessingTimeoutMs
                (fun () -> writePersistedEvent loaded persisted)

    let private catchUpParsed (loaded: LoadedPersist) (nodeId: NodeId) =
        match loaded.liveSaveDataDir with
        | None -> ()
        | Some _ ->
            pushIds loaded [ nodeId ]

    let private noteParsed (loaded: LoadedPersist) (nodeId: NodeId) =
        match Map.tryFind nodeId loaded.state.Value.graph.nodes with
        | Some node when
            node.documentState.parseState = ParseState.Parsed
            && node.documentState.persistState = PersistState.Unpersisted ->
            catchUpParsed loaded nodeId
            if
                loaded.diffNeeded.Value
                && not loaded.diffInProgress.Value
            then
                requestFileDiff loaded
        | _ -> ()

    let private takePendingNotify (loaded: LoadedPersist) =
        let pending = loaded.pendingNotify.Value
        loaded.pendingNotify.Value <- []
        pending

    /// A failed write leaves the node Unpersisted. Drop an in-progress
    /// file diff so a later post can try again.
    let private noteWriteFailed (loaded: LoadedPersist) () =
        loaded.pendingNotify.Value <- []
        if loaded.diffInProgress.Value then
            let pending = loaded.diffIds.Value
            loaded.diffIds.Value <- []
            loaded.diffMarksLeft.Value <- 0
            loaded.diffInProgress.Value <- false
            loaded.diffWorkspaces.Value <-
                List.distinct (pending @ loaded.diffWorkspaces.Value)
        loaded.diffNeeded.Value <- true

    let private noteStamped (loaded: LoadedPersist) () =
        let pending = takePendingNotify loaded
        loaded.opsNotifiedIds.Value <-
            pending @ loaded.opsNotifiedIds.Value
            |> List.distinct
        loaded.baselineGraph.Value <- loaded.state.Value.graph
        if List.isEmpty loaded.diffWorkspaces.Value then
            ()
        elif loaded.diffInProgress.Value then
            loaded.diffNeeded.Value <- true
        else
            requestFileDiff loaded

    let private persistNoticeRow loaded notice graph =
        if String.IsNullOrWhiteSpace loaded.connectionString then
            ()
        else
            try
                (Database.writeNoticeRow
                    loaded.connectionString
                    notice
                    graph)
                    .GetAwaiter()
                    .GetResult()
            with ex ->
                eprintfn
                    "DbAgent: failed to persist notice: %s"
                    ex.Message

    let private persistHandlers loaded = {
        getState = fun () -> Ok loaded.state.Value
        getEventId = fun () -> Ok loaded.state.Value.eventId
        getEventsSince = fun after -> Ok(eventsSince loaded after)
        getEventLog = fun () -> Ok loaded.eventLog.Value
        appendEvent = appendPersistedEvent loaded
        applyEvent = fun event graphOnly ->
            processPostEvents loaded [ event ] graphOnly
        replaceGraph = fun notice graph ->
            loaded.state.Value <-
                { loaded.state.Value with graph = graph }
            persistNoticeRow loaded notice graph
        noteDiffDone = countDiffMark loaded
        noteParsed = noteParsed loaded
        noteStamped = noteStamped loaded
        noteWriteFailed = noteWriteFailed loaded
    }

    let private logUnhandledException liveSaveDataDir operation context (ex: exn) =
        match liveSaveDataDir with
        | Some dataDir ->
            HttpResponseLog.appendException
                (HttpResponseLog.logPath dataDir)
                "DbAgent"
                operation
                context
                ex
        | None ->
            eprintfn
                "DbAgent: unhandled exception in %s (%s): %s"
                operation
                context
                ex.Message

    let private formatError liveSaveDataDir operation =
        match liveSaveDataDir with
        | Some dir ->
            $"Internal server error in DbAgent {operation} (dataDir={dir})."
        | None ->
            $"Internal server error in DbAgent {operation}."

    let private startupPrelude loaded runStartupSweep = async {
        let result =
            try
                runStartupSweep loaded.state.Value.graph
            with ex ->
                Error $"Startup projection sweep failed: {ex.Message}"
        match result with
        | Ok maintenanceResult ->
            match applyMaintenance loaded maintenanceResult with
            | Ok () ->
                loaded.ready.TrySetResult() |> ignore
                return Ok ()
            | Error error ->
                loaded.startupError.Value <- Some error
                return Error error
        | Error error ->
            loaded.startupError.Value <- Some error
            return Error error
    }

    /// No data directory: there is no file. Keep the node's current time.
    let private persistLive
        (dataDir: string option)
        (persistNode: string -> Graph -> NodeId -> Result<DateTime, string>)
        (graph: Graph)
        (nodeId: NodeId)
        : Result<DateTime, string> =
        match dataDir with
        | Some dir -> persistNode dir graph nodeId
        | None ->
            match Map.tryFind nodeId graph.nodes with
            | None -> Error "node not found"
            | Some node -> Ok(NodeUpdateTime.toDbPrecision node.updateTime)

    let private createLoaded
        (initialState: State)
        (connectionString: string)
        (liveSaveDataDir: string option)
        persistNode
        (runStartupSweep:
            Graph -> Result<DatabaseProjection.ProjectionMaintenanceResult, string>)
        : DbAgent =
        let loaded =
            makeLoaded
                initialState
                connectionString
                liveSaveDataDir
                persistNode
        let recovered = loadReconciled connectionString initialState
        loaded.eventLog.Value <- snd recovered
        loaded.state.Value <- fst recovered
        let stack = PersistRequestStack.create ()
        loaded.stack.Value <- Some stack
        let join =
            PersistThread.start {
                currentState = fun () -> Volatile.Read(&loaded.state).Value
                take = fun () -> PersistRequestStack.takeUntilStop stack
                persistNode =
                    fun graph nodeId ->
                        persistLive
                            loaded.liveSaveDataDir
                            loaded.persistNode
                            graph
                            nodeId
                postNotice = PersistThread.postWhenBound loaded.persistFinished
            }
        let stopPersist () =
            PersistRequestStack.stop stack
            join ()
        { filling =
            { handlers = persistHandlers loaded
              onError = logUnhandledException loaded.liveSaveDataDir
              formatError = formatError loaded.liveSaveDataDir
              isReady = fun () -> loaded.ready.Task.IsCompletedSuccessfully
              dispose = stopPersist
              until = Some(startupPrelude loaded runStartupSweep)
              bindPersistFinished =
                fun post -> loaded.persistFinished.Value <- Some post } }

    let private liveStartupSweep (connectionString: string) (_: Graph) =
        try
            let result =
                DatabaseProjection.startupSweepPatch
                |> DatabaseProjection.maintenanceCommand
                |> DatabaseProjection.executeMaintenance connectionString
                |> Async.AwaitTask
                |> Async.RunSynchronously
            match result with
            | Error e -> Error $"Startup projection sweep failed: {e}"
            | Ok r ->
                let facts = r.logFacts
                let affected =
                    facts.affectedNodeIds
                    |> List.map string
                    |> String.concat ", "
                eprintfn "%s"
                    ($"DbAgent: projection repair deleted={facts.deletedCount} "
                     + $"ownershipUpdates={facts.ownershipUpdateCount} "
                     + $"insertNodes={facts.insertNodeCount} "
                     + $"insertChildren={facts.insertChildCount} "
                     + $"ordinalShifts={facts.ordinalShiftCount} "
                     + $"affected=[{affected}]")
                Ok r
        with ex ->
            Error $"Startup projection sweep failed: {ex.Message}"

    let private createWithLiveSave
        (connectionString: string)
        (liveSaveDataDir: string option)
        : DbAgent =
        let initialState =
            loadInitialState connectionString |> Async.RunSynchronously
        createLoaded
            initialState
            connectionString
            liveSaveDataDir
            DocumentPersistChange.persistNode
            (liveStartupSweep connectionString)

    let private wrapFakeSweep
        (runStartupSweep: Graph -> Result<Guid list, string>)
        : Graph -> Result<DatabaseProjection.ProjectionMaintenanceResult, string> =
        fun graph ->
            runStartupSweep graph
            |> Result.map (fun ids ->
                { deletedIds = ids
                  requiresReload = false
                  logFacts = ProjectionOwnershipRepair.emptyPlan.logFacts })

    let createForTest
        (initialState: State)
        (runStartupSweep: Graph -> Result<Guid list, string>)
        : DbAgent =
        createLoaded
            initialState
            ""
            None
            DocumentPersistChange.persistNode
            (wrapFakeSweep runStartupSweep)

    /// Test-only seam: injects a stand-in for the live-persist step (and an optional
    /// liveSaveDataDir) so failure/timeout behavior can be exercised without a real DB
    /// connection or the real (slow/bug-prone) document reconcile path.
    let createForTestWithDependencies
        (initialState: State)
        (liveSaveDataDir: string option)
        persistNode
        (runStartupSweep: Graph -> Result<Guid list, string>)
        : DbAgent =
        createLoaded
            initialState
            ""
            liveSaveDataDir
            persistNode
            (wrapFakeSweep runStartupSweep)

    let createWithDataDir
        (connectionString: string)
        (dataDir: string)
        : DbAgent =
        createWithLiveSave connectionString (Some dataDir)

    let create (connectionString: string) : DbAgent =
        createWithLiveSave connectionString None

    let internal persist (agent: DbAgent) : PersistFilling =
        agent.filling
