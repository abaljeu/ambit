namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared
open Gambol.Shared

/// PostgreSQL-backed persist filling. Does not start a mailbox.
type DbAgent = private {
    handlers: PersistHandlers
    onError: string -> string -> exn -> unit
    formatError: string -> string
    until: Async<Result<unit, string>>
    bindSnapshot: (Graph option -> unit) -> unit
    isReady: unit -> bool
    flushSnapshot: unit -> Async<Result<unit, string>>
    dispose: unit -> unit
}

[<RequireQualifiedAccess>]
module DbAgent =

    type private LoadedPersist = {
        state: State ref
        persistedGraph: Graph ref
        eventLog: EventLog ref
        snapshotInProgress: bool ref
        snapshotNeeded: bool ref
        ready: TaskCompletionSource<unit>
        snapshotPost: (Graph option -> unit) option ref
        startupError: string option ref
        connectionString: string
        liveSaveDataDir: string option
        persistGraphOps:
            string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>
    }

    let private loadReconciled
        (connectionString: string)
        (state: State)
        : State * EventLog =
        let log =
            if String.IsNullOrWhiteSpace connectionString then
                EventLog.empty
            else
                Database.getEventsAfter connectionString (-1)
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> List.choose (fun row ->
                    EventLogFile.decodeEvent row.payload
                    |> Result.toOption)
                |> EventLog.restorePersisted
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
          ready =
            TaskCompletionSource<unit>(
                TaskCreationOptions.RunContinuationsAsynchronously)
          snapshotPost = ref None
          startupError = ref None
          connectionString = connectionString
          liveSaveDataDir = liveSaveDataDir
          persistGraphOps = persistGraphOps }

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
        ((s: State), confirmations, externalChanges)
        (event: Ev)
        =
        match tryPersistedEvent loaded event.submissionId with
        | Some storedEvent ->
            Ok(s, storedEvent :: confirmations, externalChanges)
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
                        externalChanges || amended)

    let private applyBatch loaded events =
        try
            events
            |> List.fold
                (fun acc event ->
                    match acc with
                    | Error err -> Error err
                    | Ok stateAndLog -> applyOneEvent loaded stateAndLog event)
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
                        (EventId.value newState.eventId)
                        events
                (DatabaseProjection.persistWithTx tx newState.graph patch)
                    .GetAwaiter()
                    .GetResult()
                tx.Commit()
                Ok ()
            with ex ->
                eprintfn "DbAgent: failed to persist projection: %s" ex.Message
                Error $"Database error: {ex.Message}"

    let private writeLiveSnapshot liveSaveDataDir preGraph postGraph =
        try
            match liveSaveDataDir with
            | Some dataDir ->
                match
                    DocumentPersistence.persistGraphChange
                        dataDir preGraph postGraph
                with
                | Error err ->
                    eprintfn "DbAgent: failed to write live documents: %s" err
                    None
                | Ok stamped -> Some stamped.graph
            | None -> Some postGraph
        with ex ->
            eprintfn
                "DbAgent: failed to write live documents: %s"
                ex.Message
            None

    let private startSnapshot loaded (post: Graph option -> unit) =
        loaded.snapshotInProgress.Value <- true
        loaded.snapshotNeeded.Value <- false
        let snapshotState = loaded.state.Value
        let preGraph = loaded.persistedGraph.Value
        let postGraph = snapshotState.graph
        Task.Run(fun () ->
            let persisted =
                writeLiveSnapshot loaded.liveSaveDataDir preGraph postGraph
            post persisted
        )
        |> ignore

    let private requestSnapshot loaded =
        match loaded.snapshotPost.Value with
        | Some post -> startSnapshot loaded post
        | None -> ()

    let private validatePostChange loaded graphOnly preGraph postGraph =
        match graphOnly, loaded.liveSaveDataDir with
        | true, _ -> Ok ()
        | false, None -> Ok ()
        | false, Some dataDir ->
            DocumentPersistence.validatePathMoves dataDir preGraph postGraph
            |> Result.bind (fun () ->
                DocumentPersistence.validateGraphDiskEffects
                    dataDir
                    preGraph
                    postGraph)

    let private persistLiveChange loaded graphOnly preGraph newState fresh =
        match graphOnly, loaded.liveSaveDataDir, fresh with
        | false, Some dataDir, _::_ ->
            let ops =
                fresh
                |> List.collect (fun event ->
                    Ev.ops event |> Option.defaultValue [])
            CoreMailboxBackend.runBounded
                CoreMailboxBackend.ChangeProcessingTimeoutMs
                (fun () ->
                    loaded.persistGraphOps
                        dataDir
                        preGraph
                        newState.graph
                        ops)
            |> Result.map Some
        | _ -> Ok None

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
        let stampedFresh, ackChanges =
            CoreMailboxBackend.overlayFreshEvents confirmations fresh stampOps
        stateToStore, ackChanges, persistMessage

    let private commitPostChange
        loaded
        graphOnly
        fresh
        stateToStore
        ackChanges
        externalChanges
        persistMessage
        =
        match
            CoreMailboxBackend.runBounded
                CoreMailboxBackend.ChangeProcessingTimeoutMs
                (fun () -> persistGraphProjection loaded stateToStore ackChanges)
        with
        | Error err -> Error err
        | Ok () ->
            loaded.state.Value <- stateToStore
            if graphOnly then
                loaded.persistedGraph.Value <- stateToStore.graph
            elif not (List.isEmpty fresh) then
                loaded.persistedGraph.Value <- stateToStore.graph
                if loaded.snapshotInProgress.Value then
                    loaded.snapshotNeeded.Value <- true
                else
                    requestSnapshot loaded
            Ok(accepted loaded ackChanges externalChanges persistMessage)

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
            | Ok stampedOpt ->
                let stateToStore, ackChanges, persistMessage =
                    preparePostChange
                        newState confirmations fresh stampedOpt
                commitPostChange
                    loaded
                    graphOnly
                    fresh
                    stateToStore
                    ackChanges
                    externalChanges
                    persistMessage

    let private processPostEvents loaded (events: Ev list) graphOnly =
        if events.IsEmpty then
            Error "changes must not be empty"
        else
            match
                CoreMailboxBackend.runBounded
                    CoreMailboxBackend.ChangeProcessingTimeoutMs
                    (fun () -> applyBatch loaded events)
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

    let private handleSnapshotDone loaded persisted =
        match persisted with
        | Some graph
            when GraphProjection.graphEquals loaded.state.Value.graph graph ->
            loaded.persistedGraph.Value <- graph
        | _ -> ()
        loaded.snapshotInProgress.Value <- false
        if loaded.snapshotNeeded.Value then
            requestSnapshot loaded

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
            Error $"Ev persist error: {ex.Message}"

    let private appendPersistedEvent
        loaded
        (persisted: Ev)
        =
        if String.IsNullOrWhiteSpace loaded.connectionString then
            recordPersistedEvent loaded persisted
            Ok ()
        else
            CoreMailboxBackend.runBounded
                CoreMailboxBackend.ChangeProcessingTimeoutMs
                (fun () -> writePersistedEvent loaded persisted)

    let private persistHandlers loaded = {
        getState = fun () -> Ok loaded.state.Value
        getEventId = fun () -> Ok loaded.state.Value.eventId
        getEventsSince = fun after -> Ok(eventsSince loaded after)
        appendEvent = appendPersistedEvent loaded
        applyEvent = fun event graphOnly ->
            processPostEvents loaded [ event ] graphOnly
        snapshotDone = handleSnapshotDone loaded
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
        { handlers = persistHandlers loaded
          onError = logUnhandledException loaded.liveSaveDataDir
          formatError = formatError loaded.liveSaveDataDir
          until = startupPrelude loaded runStartupSweep
          bindSnapshot =
            fun post -> loaded.snapshotPost.Value <- Some post
          isReady = fun () -> loaded.ready.Task.IsCompletedSuccessfully
          flushSnapshot = fun () -> async { return Ok () }
          dispose = fun () -> () }

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
            DocumentPersistence.persistGraphOps
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
            DocumentPersistence.persistGraphOps
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

    let persist (agent: DbAgent) : PersistFilling = {
        handlers = agent.handlers
        onError = agent.onError
        formatError = agent.formatError
        isReady = agent.isReady
        flushSnapshot = agent.flushSnapshot
        dispose = agent.dispose
        until = Some agent.until
        bindSnapshot = agent.bindSnapshot
    }
