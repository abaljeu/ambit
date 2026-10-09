namespace Gambol.Server

open System
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
        persistedGraph: Graph ref
        eventLog: EventLog ref
        snapshotInProgress: bool ref
        snapshotNeeded: bool ref
        snapshotWorkspaces: NodeId list ref
        snapshotIds: NodeId list ref
        opsNotifiedIds: NodeId list ref
        snapshotMarks: int ref
        ready: TaskCompletionSource<unit>
        snapshotPost: (InMsg -> unit) option ref
        startupError: string option ref
        connectionString: string
        liveSaveDataDir: string option
        persistGraphOps:
            string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>
        collect: (PersistSubmit -> PersistOutcome) option ref
    }

    type private LivePersist = {
        stamped: PersistGraphOk option
        allowSnapshot: bool
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
        persistGraphOps
        : LoadedPersist =
        { state = ref initialState
          persistedGraph = ref initialState.graph
          eventLog = ref EventLog.empty
          snapshotInProgress = ref false
          snapshotNeeded = ref false
          snapshotWorkspaces = ref []
          snapshotIds = ref []
          opsNotifiedIds = ref []
          snapshotMarks = ref 0
          ready =
            TaskCompletionSource<unit>(
                TaskCreationOptions.RunContinuationsAsynchronously)
          snapshotPost = ref None
          startupError = ref None
          connectionString = connectionString
          liveSaveDataDir = liveSaveDataDir
          persistGraphOps = persistGraphOps
          collect = ref None }

    let private trimDeletedIds loaded deletedIds =
        let deletedNodeIds = deletedIds |> List.map NodeId
        let trim = DatabaseProjection.trimDeletedNodes deletedNodeIds
        loaded.state.Value <-
            { loaded.state.Value with graph = trim loaded.state.Value.graph }
        loaded.persistedGraph.Value <- trim loaded.persistedGraph.Value

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
                loaded.persistedGraph.Value <- graph
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

    /// Workspaces that own the ops which requested this live snapshot.
    let private workspacesTouched (graph: Graph) (events: Ev list) =
        events
        |> List.collect (fun event ->
            Ev.ops event |> Option.defaultValue [])
        |> List.choose PersistCollectors.opAnchor
        |> List.choose (GraphQuery.enclosingWorkspace graph)
        |> List.distinct

    let private rememberWorkspaces loaded (graph: Graph) (events: Ev list) =
        let more = workspacesTouched graph events
        loaded.snapshotWorkspaces.Value <-
            loaded.snapshotWorkspaces.Value @ more |> List.distinct

    let private enqueueSnapshot
        loaded
        (ids: NodeId list)
        (preGraph: Graph)
        (postGraph: Graph)
        =
        match loaded.collect.Value with
        | None -> ()
        | Some collect ->
            collect {
                nodeIds = ids
                dataDir = loaded.liveSaveDataDir
                preGraph = preGraph
                postGraph = postGraph
                ops = []
                kind = PersistKind.Change
                notify = true
                wait = false
            } |> ignore

    let private startSnapshot loaded =
        let graph = loaded.state.Value.graph
        let skip = loaded.opsNotifiedIds.Value
        loaded.opsNotifiedIds.Value <- []
        let pending = loaded.snapshotWorkspaces.Value
        loaded.snapshotWorkspaces.Value <- []
        let unparsed id =
            PersistCollectors.unparsedNode graph id
        let ids =
            pending
            |> List.filter (fun id ->
                not (List.contains id skip) && not (unparsed id))
        let held = pending |> List.filter unparsed
        match ids with
        | [] ->
            if not (List.isEmpty held) then
                loaded.snapshotWorkspaces.Value <- held
                loaded.snapshotNeeded.Value <- true
            else
                loaded.snapshotNeeded.Value <- false
                if List.isEmpty pending then
                    eprintfn
                        "DbAgent: live snapshot has no owning workspace"
        | _ ->
            if List.isEmpty held then
                loaded.snapshotNeeded.Value <- false
            else
                loaded.snapshotWorkspaces.Value <- held
                loaded.snapshotNeeded.Value <- true
            loaded.snapshotInProgress.Value <- true
            loaded.snapshotIds.Value <- ids
            loaded.snapshotMarks.Value <- List.length ids
            enqueueSnapshot
                loaded
                ids
                loaded.persistedGraph.Value
                loaded.state.Value.graph

    let private requestSnapshot loaded =
        match loaded.snapshotPost.Value with
        | Some _ -> startSnapshot loaded
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

    let private liveOf stamped allowSnapshot : LivePersist = {
        stamped = stamped
        allowSnapshot = allowSnapshot
        notifiedIds = []
    }

    let private liveOutcome (outcome: PersistOutcome) : Result<LivePersist, string> =
        match outcome with
        | PersistOutcome.Wrote stamped ->
            Ok(liveOf (Some stamped) true)
        | PersistOutcome.Blocked -> Ok(liveOf None false)
        | PersistOutcome.Queued -> Ok(liveOf None false)
        | PersistOutcome.Failed err -> Error err

    let private notifiedOwners
        (graph: Graph)
        (ids: NodeId list)
        (stamped: PersistGraphOk option)
        =
        match stamped with
        | Some ok when ok.message.IsNone ->
            ids |> List.filter (PersistCollectors.openNode graph)
        | _ -> []

    let private submitLiveOps loaded dataDir preGraph postGraph ops =
        match loaded.collect.Value with
        | None -> Error "persist collectors are not bound"
        | Some collect ->
            let nodeIds = PersistCollectors.owningSpecials postGraph ops
            match
                liveOutcome (
                    collect {
                        nodeIds = nodeIds
                        dataDir = Some dataDir
                        preGraph = preGraph
                        postGraph = postGraph
                        ops = ops
                        kind = PersistKind.Ops
                        notify = true
                        wait = true
                    })
            with
            | Error err -> Error err
            | Ok live ->
                Ok {
                    live with
                        notifiedIds =
                            notifiedOwners postGraph nodeIds live.stamped
                }

    let private persistLiveChange loaded graphOnly preGraph newState fresh =
        match graphOnly, loaded.liveSaveDataDir, fresh with
        | false, Some dataDir, _::_ ->
            let ops =
                fresh
                |> List.collect (fun event ->
                    Ev.ops event |> Option.defaultValue [])
            submitLiveOps loaded dataDir preGraph newState.graph ops
        | _ -> Ok(liveOf None true)

    let private preparePostChange newState confirmations fresh
        (stampedOpt: PersistGraphOk option)
        =
        let stampOps, stateToStore, persistMessage =
            match stampedOpt with
            | Some stamped ->
                PersistStamp.opsBetween newState.graph stamped.graph,
                { newState with graph = stamped.graph },
                stamped.message
            | None -> [], newState, None
        let stampedFresh, ackEvents =
            CoreMailboxEvents.overlayFreshEvents confirmations fresh stampOps
        stateToStore, ackEvents, persistMessage

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
        | Error err -> Error err
        | Ok () ->
            loaded.state.Value <- stateToStore
            if graphOnly then
                loaded.persistedGraph.Value <- stateToStore.graph
            elif not (List.isEmpty fresh) && live.allowSnapshot then
                loaded.persistedGraph.Value <- stateToStore.graph
                rememberWorkspaces loaded stateToStore.graph fresh
                loaded.opsNotifiedIds.Value <-
                    live.notifiedIds @ loaded.opsNotifiedIds.Value
                    |> List.distinct
                if loaded.snapshotInProgress.Value then
                    loaded.snapshotNeeded.Value <- true
                else
                    requestSnapshot loaded
            elif not (List.isEmpty fresh) then
                rememberWorkspaces loaded stateToStore.graph fresh
                loaded.snapshotNeeded.Value <- true
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
                    preparePostChange
                        newState confirmations fresh live.stamped
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

    let private countSnapshotMark loaded nodeId =
        let counts =
            loaded.snapshotInProgress.Value
            && List.contains nodeId loaded.snapshotIds.Value
        if counts then
            loaded.snapshotIds.Value <-
                loaded.snapshotIds.Value
                |> List.filter (fun id -> id <> nodeId)
            let left = loaded.snapshotMarks.Value - 1
            if left > 0 then
                loaded.snapshotMarks.Value <- left
            else
                loaded.snapshotMarks.Value <- 0
                loaded.snapshotInProgress.Value <- false
                if loaded.snapshotNeeded.Value then
                    requestSnapshot loaded

    /// A failed Change leaves those nodes Unpersisted. Drop the batch
    /// so a later post can try again. Do not post SnapshotDone.
    let private changeFailed (loaded: LoadedPersist) (ids: NodeId list) (err: string) =
        eprintfn "DbAgent: failed to write live documents: %s" err
        let hitsBatch =
            loaded.snapshotInProgress.Value
            && List.exists
                (fun id -> List.contains id loaded.snapshotIds.Value)
                ids
        if hitsBatch then
            let pending = loaded.snapshotIds.Value
            loaded.snapshotIds.Value <- []
            loaded.snapshotMarks.Value <- 0
            loaded.snapshotInProgress.Value <- false
            loaded.snapshotWorkspaces.Value <-
                List.distinct (pending @ loaded.snapshotWorkspaces.Value)
            loaded.snapshotNeeded.Value <- true

    let private handleSnapshotDone loaded nodeId persisted =
        match persisted with
        | Some graph
            when GraphProjection.graphEquals loaded.state.Value.graph graph ->
            loaded.persistedGraph.Value <- graph
        | _ -> ()
        countSnapshotMark loaded nodeId

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
            enqueueSnapshot
                loaded
                [ nodeId ]
                loaded.persistedGraph.Value
                loaded.state.Value.graph

    let private noteParsed (loaded: LoadedPersist) (nodeId: NodeId) =
        match Map.tryFind nodeId loaded.state.Value.graph.nodes with
        | Some node when
            node.parseState = ParseState.Parsed
            && node.persistState = PersistState.Unpersisted ->
            catchUpParsed loaded nodeId
            if
                loaded.snapshotNeeded.Value
                && not loaded.snapshotInProgress.Value
            then
                requestSnapshot loaded
        | _ -> ()

    let private persistHandlers loaded = {
        getState = fun () -> Ok loaded.state.Value
        getEventId = fun () -> Ok loaded.state.Value.eventId
        getEventsSince = fun after -> Ok(eventsSince loaded after)
        getEventLog = fun () -> Ok loaded.eventLog.Value
        appendEvent = appendPersistedEvent loaded
        applyEvent = fun event graphOnly ->
            processPostEvents loaded [ event ] graphOnly
        replaceGraph = fun graph ->
            loaded.state.Value <-
                { loaded.state.Value with graph = graph }
        snapshotDone = handleSnapshotDone loaded
        noteParsed = noteParsed loaded
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

    let private createLoaded
        (initialState: State)
        (connectionString: string)
        (liveSaveDataDir: string option)
        persistGraphOps
        (runStartupSweep:
            Graph -> Result<DatabaseProjection.ProjectionMaintenanceResult, string>)
        : DbAgent =
        let loaded =
            makeLoaded
                initialState
                connectionString
                liveSaveDataDir
                persistGraphOps
        let recovered = loadReconciled connectionString initialState
        loaded.eventLog.Value <- snd recovered
        loaded.state.Value <- fst recovered
        let collect, consumer = PersistCollectors.create ()
        loaded.collect.Value <- Some collect
        PersistThread.start {
            consumer = consumer
            persistOps = loaded.persistGraphOps
            persistChange = DocumentPersistChange.persistGraphChange
            finish = PersistThread.finishWhenBound loaded.snapshotPost
            changeFailed = changeFailed loaded
        }
        { filling =
            { handlers = persistHandlers loaded
              onError = logUnhandledException loaded.liveSaveDataDir
              formatError = formatError loaded.liveSaveDataDir
              isReady = fun () -> loaded.ready.Task.IsCompletedSuccessfully
              flushSnapshot = fun () -> async { return Ok () }
              dispose = fun () -> ()
              until = Some(startupPrelude loaded runStartupSweep)
              bindSnapshot =
                fun post -> loaded.snapshotPost.Value <- Some post } }

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
            DocumentPersistChange.persistGraphOps
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
            DocumentPersistChange.persistGraphOps
            (wrapFakeSweep runStartupSweep)

    /// Test-only seam: injects a stand-in for the live-persist step (and an optional
    /// liveSaveDataDir) so failure/timeout behavior can be exercised without a real DB
    /// connection or the real (slow/bug-prone) document reconcile path.
    let createForTestWithDependencies
        (initialState: State)
        (liveSaveDataDir: string option)
        persistGraphOps
        (runStartupSweep: Graph -> Result<Guid list, string>)
        : DbAgent =
        createLoaded
            initialState
            ""
            liveSaveDataDir
            persistGraphOps
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
