namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared

module Decode = Thoth.Json.Newtonsoft.Decode

/// PostgreSQL-backed persist filling. Does not start a mailbox.
type DbAgent = private {
    handlers: PersistHandlers
    onError: string -> string -> exn -> unit
    formatError: string -> string
    until: Async<Result<unit, string>>
    bindMailbox: MailboxProcessor<CoreMsg> -> unit
    isReady: unit -> bool
    flushSnapshot: unit -> Async<Result<unit, string>>
    dispose: unit -> unit
}

[<RequireQualifiedAccess>]
module DbAgent =

    type private LoadedPersist = {
        state: State ref
        persistedGraph: Graph ref
        snapshotInProgress: bool ref
        snapshotNeeded: bool ref
        ready: TaskCompletionSource<unit>
        mailboxRef: MailboxProcessor<CoreMsg> option ref
        startupError: string option ref
        connectionString: string
        liveSaveDataDir: string option
        persistGraphOps:
            string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>
    }

    let private decodeChangePayload (s: string) =
        Decode.fromString Serialization.decodeChange s

    let private loadInitialState (connectionString: string) : Async<State> =
        Database.loadPersistedState connectionString decodeChangePayload
        |> Async.AwaitTask

    let private makeLoaded
        (initialState: State)
        (connectionString: string)
        (liveSaveDataDir: string option)
        persistGraphOps
        : LoadedPersist =
        { state = ref initialState
          persistedGraph = ref initialState.graph
          snapshotInProgress = ref false
          snapshotNeeded = ref false
          ready =
            TaskCompletionSource<unit>(
                TaskCreationOptions.RunContinuationsAsynchronously)
          mailboxRef = ref None
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
            loaded.state.Value.revision
            loaded.ready.Task.IsCompletedSuccessfully
            confirmed
            externalChanges
            message

    let private tryPersistedChange loaded changeId =
        if String.IsNullOrEmpty loaded.connectionString then
            None
        else
            Database.tryGetPersistedPayload loaded.connectionString changeId
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> Option.bind (decodeChangePayload >> Result.toOption)

    let private applyOneChange loaded (s, confirmations, logEntries, externalChanges) change =
        match tryPersistedChange loaded change.changeId with
        | Some stored ->
            Ok(s, stored :: confirmations, logEntries, externalChanges)
        | None ->
            let result, amended, applied =
                ChangeAmendment.applyChange change s
            match result with
            | ApplyResult.Invalid (_, errMsg) -> Error errMsg
            | ApplyResult.Unchanged _ ->
                Error "Unchanged submission is rejected."
            | ApplyResult.Changed s' ->
                let nextRev = s.revision.Value + 1
                let nextState = { s' with revision = Revision nextRev }
                Ok(
                    nextState,
                    applied :: confirmations,
                    (nextRev, applied) :: logEntries,
                    externalChanges || amended)

    let private applyBatch loaded changes =
        try
            changes
            |> List.fold
                (fun acc change ->
                    match acc with
                    | Error err -> Error err
                    | Ok stateAndLog -> applyOneChange loaded stateAndLog change)
                (Ok(loaded.state.Value, [], [], false))
            |> Result.map (fun (newState, confirmations, entries, externalChanges) ->
                newState, List.rev confirmations, List.rev entries, externalChanges)
        with ex ->
            eprintfn "DbAgent: failed to apply batch: %s" ex.Message
            Error $"Database error: {ex.Message}"

    let private persistBatch loaded newState
        (logEntries: (int * Change) list)
        =
        try
            use conn = Database.getConnection loaded.connectionString
            conn.Open()
            use tx = conn.BeginTransaction()
            logEntries
            |> List.iter (fun (serverRevAfter, change) ->
                (Database.appendChangeWithTx
                    tx
                    serverRevAfter
                    change.id
                    change.changeId
                    (ChangeLog.encodeChange change))
                    .GetAwaiter()
                    .GetResult())
            match logEntries with
            | [] -> ()
            | _ ->
                let patch =
                    logEntries
                    |> List.map snd
                    |> DatabaseProjection.plan
                        newState.graph
                        newState.revision.Value
                (DatabaseProjection.persistWithTx tx newState.graph patch)
                    .GetAwaiter()
                    .GetResult()
            tx.Commit()
            Ok ()
        with ex ->
            eprintfn "DbAgent: failed to persist batch: %s" ex.Message
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

    let private startSnapshot loaded (inbox: MailboxProcessor<CoreMsg>) =
        loaded.snapshotInProgress.Value <- true
        loaded.snapshotNeeded.Value <- false
        let snapshotState = loaded.state.Value
        let preGraph = loaded.persistedGraph.Value
        let postGraph = snapshotState.graph
        Task.Run(fun () ->
            let persisted =
                writeLiveSnapshot loaded.liveSaveDataDir preGraph postGraph
            inbox.Post(SnapshotDone persisted)
        )
        |> ignore

    let private requestSnapshot loaded =
        match loaded.mailboxRef.Value with
        | Some inbox -> startSnapshot loaded inbox
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

    let private persistLiveChange loaded graphOnly preGraph newState logEntries =
        match graphOnly, loaded.liveSaveDataDir, logEntries with
        | false, Some dataDir, _::_ ->
            let ops =
                logEntries
                |> List.collect (fun (_, change) -> change.ops)
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

    let private preparePostChange newState confirmations logEntries
        (stampedOpt: PersistGraphOk option)
        =
        let stampOps, stateToStore, persistMessage =
            match stampedOpt with
            | Some stamped ->
                PersistStamp.opsBetween newState.graph stamped.graph,
                { newState with graph = stamped.graph },
                stamped.message
            | None -> [], newState, None
        let fresh = logEntries |> List.map snd
        let stampedFresh, ackChanges =
            CoreMailboxBackend.overlayFresh confirmations fresh stampOps
        let storedEntries =
            List.zip (logEntries |> List.map fst) stampedFresh
        stateToStore, ackChanges, storedEntries, persistMessage

    let private commitPostChange
        loaded
        graphOnly
        sourceEntries
        stateToStore
        ackChanges
        externalChanges
        persistMessage
        storedEntries
        =
        match
            CoreMailboxBackend.runBounded
                CoreMailboxBackend.ChangeProcessingTimeoutMs
                (fun () -> persistBatch loaded stateToStore storedEntries)
        with
        | Error err -> Error err
        | Ok () ->
            loaded.state.Value <- stateToStore
            if graphOnly then
                loaded.persistedGraph.Value <- stateToStore.graph
            elif not (List.isEmpty sourceEntries) then
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
        logEntries
        externalChanges
        =
        let preGraph = loaded.state.Value.graph
        match validatePostChange loaded graphOnly preGraph newState.graph with
        | Error err -> Error err
        | Ok () ->
            match
                persistLiveChange loaded graphOnly preGraph newState logEntries
            with
            | Error err -> Error err
            | Ok stampedOpt ->
                let stateToStore, ackChanges, storedEntries, persistMessage =
                    preparePostChange
                        newState confirmations logEntries stampedOpt
                commitPostChange
                    loaded
                    graphOnly
                    logEntries
                    stateToStore
                    ackChanges
                    externalChanges
                    persistMessage
                    storedEntries

    let private processPostChange loaded (changes: Change list) graphOnly =
        if changes.IsEmpty then
            Error "changes must not be empty"
        else
            match
                CoreMailboxBackend.runBounded
                    CoreMailboxBackend.ChangeProcessingTimeoutMs
                    (fun () -> applyBatch loaded changes)
            with
            | Error err -> Error err
            | Ok (newState, confirmations, logEntries, externalChanges) ->
                finishAppliedPostChange
                    loaded
                    graphOnly
                    newState
                    confirmations
                    logEntries
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

    let private changesSince loaded (after: Revision) =
        let rows =
            Database.getChangesAfterCheckpointRevision
                loaded.connectionString
                after.Value
            |> Async.AwaitTask
            |> Async.RunSynchronously
        rows
        |> List.choose (fun row ->
            decodeChangePayload row.payload |> Result.toOption)

    let private persistHandlers loaded = {
        getState = fun () -> Ok loaded.state.Value
        getRevision = fun () -> Ok loaded.state.Value.revision
        getChangesSince = fun after -> Ok(changesSince loaded after)
        postChange = fun changes -> processPostChange loaded changes false
        postGraphOnlyChange = fun changes ->
            processPostChange loaded changes true
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
        { handlers = persistHandlers loaded
          onError = logUnhandledException loaded.liveSaveDataDir
          formatError = formatError loaded.liveSaveDataDir
          until = startupPrelude loaded runStartupSweep
          bindMailbox =
            fun mailbox -> loaded.mailboxRef.Value <- Some mailbox
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

    let persistOf (agent: DbAgent) = agent.handlers
    let onErrorOf (agent: DbAgent) = agent.onError
    let formatErrorOf (agent: DbAgent) = agent.formatError
    let untilOf (agent: DbAgent) = agent.until
    let attachMailbox (agent: DbAgent) mailbox =
        agent.bindMailbox mailbox
    let isReadyOf (agent: DbAgent) = agent.isReady
    let flushOf (agent: DbAgent) = agent.flushSnapshot
    let disposeOf (agent: DbAgent) = agent.dispose
