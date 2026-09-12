namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared

module Decode = Thoth.Json.Newtonsoft.Decode

/// PostgreSQL-backed agent. Same message type as `FileAgent`.
type DbAgent =
    private { mailbox: MailboxProcessor<CoreMsg>
              isReady: unit -> bool }

[<RequireQualifiedAccess>]
module DbAgent =

    let private decodeChangePayload (s: string) =
        Decode.fromString Serialization.decodeChange s

    let private loadInitialState (connectionString: string) : Async<State> =
        Database.loadPersistedState connectionString decodeChangePayload |> Async.AwaitTask

    let private createLoaded
        (initialState: State)
        (connectionString: string)
        (liveSaveDataDir: string option)
        (persistGraphOps: string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>)
        (runStartupSweep: Graph -> Result<DatabaseProjection.ProjectionMaintenanceResult, string>)
        : DbAgent =
        let state = ref initialState
        let persistedGraph = ref initialState.graph
        let snapshotInProgress = ref false
        let snapshotNeeded = ref false
        let ready =
            TaskCompletionSource<unit>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        let startupError: string option ref = ref None

        let trimDeletedIds deletedIds =
            let deletedNodeIds = deletedIds |> List.map NodeId
            let trim = DatabaseProjection.trimDeletedNodes deletedNodeIds
            state.Value <- { state.Value with graph = trim state.Value.graph }
            persistedGraph.Value <- trim persistedGraph.Value

        let applyMaintenance
            (result: DatabaseProjection.ProjectionMaintenanceResult)
            : Result<unit, string> =
            if result.requiresReload then
                match
                    Database.tryLoadGraphFromProjection connectionString
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                with
                | Ok (graph, _) ->
                    state.Value <- { state.Value with graph = graph }
                    persistedGraph.Value <- graph
                    Ok ()
                | Error e -> Error $"Startup projection sweep failed: {e}"
            else
                trimDeletedIds result.deletedIds
                Ok ()

        let accepted confirmed externalChanges message =
            CoreChanges.accepted
                state.Value.revision
                ready.Task.IsCompletedSuccessfully
                confirmed
                externalChanges
                message

        let tryPersistedChange (changeId: Guid) =
            if String.IsNullOrEmpty connectionString then
                None
            else
                Database.tryGetPersistedPayload connectionString changeId
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> Option.bind (decodeChangePayload >> Result.toOption)

        let applyBatch (changes: Change list) =
            try
                let step (s, confirmations, logEntries, externalChanges) change =
                    match tryPersistedChange change.changeId with
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
                            let nextState =
                                { s' with revision = Revision nextRev }
                            let logEntry = nextRev, applied
                            Ok(
                                nextState,
                                applied :: confirmations,
                                logEntry :: logEntries,
                                externalChanges || amended)

                changes
                |> List.fold
                    (fun acc change ->
                        match acc with
                        | Error err -> Error err
                        | Ok stateAndLog -> step stateAndLog change)
                    (Ok(state.Value, [], [], false))
                |> Result.map (fun (newState, confirmations, entries, externalChanges) ->
                    newState, List.rev confirmations, List.rev entries, externalChanges)
            with ex ->
                eprintfn "DbAgent: failed to apply batch: %s" ex.Message
                Error $"Database error: {ex.Message}"

        let persistBatch (newState: State) (logEntries: (int * Change) list) =
            try
                use conn = Database.getConnection connectionString
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

        let startSnapshot (inbox: MailboxProcessor<CoreMsg>) =
            snapshotInProgress.Value <- true
            snapshotNeeded.Value <- false
            let snapshotState = state.Value
            let preGraph = persistedGraph.Value
            let postGraph = snapshotState.graph
            Task.Run(fun () ->
                let persisted =
                    try
                        match liveSaveDataDir with
                        | Some dataDir ->
                            match
                                DocumentPersistence.persistGraphChange
                                    dataDir
                                    preGraph
                                    postGraph
                            with
                            | Error err ->
                                eprintfn
                                    "DbAgent: failed to write live documents: %s"
                                    err
                                None
                            | Ok stamped -> Some stamped.graph
                        | None -> Some postGraph
                    with ex ->
                        eprintfn
                            "DbAgent: failed to write live documents: %s"
                            ex.Message
                        None
                inbox.Post(SnapshotDone persisted)
            ) |> ignore

        let validatePostChange graphOnly preGraph postGraph =
            match graphOnly, liveSaveDataDir with
            | true, _ -> Ok ()
            | false, None -> Ok ()
            | false, Some dataDir ->
                DocumentPersistence.validatePathMoves dataDir preGraph postGraph
                |> Result.bind (fun () ->
                    DocumentPersistence.validateGraphDiskEffects
                        dataDir
                        preGraph
                        postGraph)

        let persistLiveChange
            graphOnly
            preGraph
            (newState: State)
            (logEntries: (int * Change) list)
            =
            match graphOnly, liveSaveDataDir, logEntries with
            | false, Some dataDir, _::_ ->
                let ops =
                    logEntries
                    |> List.collect (fun (_, change) -> change.ops)
                CoreMailboxBackend.runBounded
                    CoreMailboxBackend.ChangeProcessingTimeoutMs
                    (fun () ->
                        persistGraphOps
                            dataDir
                            preGraph
                            newState.graph
                            ops)
                |> Result.map Some
            | _ -> Ok None

        let preparePostChange
            (newState: State)
            (confirmations: Change list)
            (logEntries: (int * Change) list)
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

        let mailboxRef: MailboxProcessor<CoreMsg> option ref = ref None

        let commitPostChange
            graphOnly
            sourceEntries
            stateToStore
            ackChanges
            externalChanges
            persistMessage
            storedEntries
            : Result<CoreChangesAccepted, string> =
            match
                CoreMailboxBackend.runBounded
                    CoreMailboxBackend.ChangeProcessingTimeoutMs
                    (fun () -> persistBatch stateToStore storedEntries)
            with
            | Error err -> Error err
            | Ok () ->
                state.Value <- stateToStore
                if graphOnly then
                    persistedGraph.Value <- stateToStore.graph
                elif not (List.isEmpty sourceEntries) then
                    persistedGraph.Value <- stateToStore.graph
                    if snapshotInProgress.Value then
                        snapshotNeeded.Value <- true
                    else
                        match mailboxRef.Value with
                        | Some inbox -> startSnapshot inbox
                        | None -> ()
                Ok(accepted ackChanges externalChanges persistMessage)

        let finishAppliedPostChange
            graphOnly
            (newState: State)
            (confirmations: Change list)
            (logEntries: (int * Change) list)
            externalChanges
            : Result<CoreChangesAccepted, string> =
            let preGraph = state.Value.graph
            match validatePostChange graphOnly preGraph newState.graph with
            | Error err -> Error err
            | Ok () ->
                match
                    persistLiveChange
                        graphOnly
                        preGraph
                        newState
                        logEntries
                with
                | Error err -> Error err
                | Ok stampedOpt ->
                    let stateToStore, ackChanges, storedEntries, persistMessage =
                        preparePostChange
                            newState
                            confirmations
                            logEntries
                            stampedOpt
                    commitPostChange
                        graphOnly
                        logEntries
                        stateToStore
                        ackChanges
                        externalChanges
                        persistMessage
                        storedEntries

        let processPostChange
            (changes: Change list)
            graphOnly
            : Result<CoreChangesAccepted, string> =
            match startupError.Value with
            | Some error -> Error error
            | None when not ready.Task.IsCompletedSuccessfully ->
                Error "Database agent startup in progress"
            | None ->
                if changes.IsEmpty then
                    Error "changes must not be empty"
                else
                    match
                        CoreMailboxBackend.runBounded
                            CoreMailboxBackend.ChangeProcessingTimeoutMs
                            (fun () -> applyBatch changes)
                    with
                    | Error err -> Error err
                    | Ok (newState, confirmations, logEntries, externalChanges) ->
                        finishAppliedPostChange
                            graphOnly
                            newState
                            confirmations
                            logEntries
                            externalChanges

        let handleSnapshotDone persisted =
            match persisted with
            | Some graph
                when GraphProjection.graphEquals
                    state.Value.graph
                    graph ->
                persistedGraph.Value <- graph
            | _ -> ()
            snapshotInProgress.Value <- false
            if snapshotNeeded.Value then
                match mailboxRef.Value with
                | Some inbox -> startSnapshot inbox
                | None -> ()

        let handlers: PersistHandlers = {
            getState = fun () -> Ok state.Value
            getRevision = fun () -> Ok state.Value.revision
            getChangesSince = fun after ->
                let rows =
                    Database.getChangesAfterCheckpointRevision
                        connectionString
                        after.Value
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                let changes =
                    rows
                    |> List.choose (fun row ->
                        decodeChangePayload row.payload
                        |> Result.toOption)
                Ok changes
            postChange = fun changes ->
                processPostChange changes false
            postGraphOnlyChange = fun changes ->
                processPostChange changes true
            snapshotDone = handleSnapshotDone
        }

        let logUnhandledException operation context (ex: exn) =
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

        let formatError operation =
            match liveSaveDataDir with
            | Some dir ->
                $"Internal server error in DbAgent {operation} (dataDir={dir})."
            | None ->
                $"Internal server error in DbAgent {operation}."

        let sweepTask =
            Task.Run(fun () ->
                try
                    runStartupSweep initialState.graph
                with ex ->
                    Error $"Startup projection sweep failed: {ex.Message}")

        Task.Run(fun () ->
            match sweepTask.GetAwaiter().GetResult() with
            | Ok result ->
                match applyMaintenance result with
                | Ok () -> ready.TrySetResult() |> ignore
                | Error error -> startupError.Value <- Some error
            | Error error -> startupError.Value <- Some error)
        |> ignore

        let mailbox =
            CoreMailboxBackend.start handlers logUnhandledException formatError

        mailboxRef.Value <- Some mailbox

        { mailbox = mailbox
          isReady = fun () -> ready.Task.IsCompletedSuccessfully }

    let private createWithLiveSave
        (connectionString: string)
        (liveSaveDataDir: string option)
        : DbAgent =
        let initialState =
            loadInitialState connectionString |> Async.RunSynchronously

        let runStartupSweep (_: Graph) =
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

        createLoaded
            initialState
            connectionString
            liveSaveDataDir
            DocumentPersistence.persistGraphOps
            runStartupSweep

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
        (persistGraphOps: string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>)
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

    let isReady (agent: DbAgent) =
        agent.isReady ()

    let tryGetState (agent: DbAgent) : Async<Result<State, string>> =
        CoreMailbox.tryGetState agent.mailbox

    let getState (agent: DbAgent) : Async<Result<State, string>> =
        CoreMailbox.getState agent.mailbox

    let getRevision (agent: DbAgent) : Async<Revision> =
        CoreMailbox.getRevision agent.mailbox

    let getChangesSince (agent: DbAgent) (after: Revision) : Async<Change list> =
        CoreMailbox.getChangesSince agent.mailbox after

    /// The only route from this agent to the Core Changes contract.
    let coreChanges (agent: DbAgent) : CoreChanges =
        CoreMailbox.coreChanges agent.isReady agent.mailbox
