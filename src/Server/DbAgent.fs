namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared

module Decode = Thoth.Json.Newtonsoft.Decode

/// PostgreSQL-backed agent. Same message type as `FileAgent`.
type DbAgent =
    private { mailbox: MailboxProcessor<FileAgentMsg>
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

        let overlayFresh confirmations fresh stampOps =
            let stamped = PersistStamp.appendToLast fresh stampOps
            let stampedById =
                stamped
                |> List.map (fun change -> change.changeId, change)
                |> Map.ofList
            let confirmed =
                confirmations
                |> List.map (fun change ->
                    Map.tryFind change.changeId stampedById
                    |> Option.defaultValue change)
            stamped, confirmed

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

        let startSnapshot (inbox: MailboxProcessor<FileAgentMsg>) =
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

        let handlePostChange
            (changes: Change list)
            graphOnly
            (reply: AsyncReplyChannel<Result<CoreChangesAccepted, string>>)
            inbox
            =
            if changes.IsEmpty then
                reply.Reply(Error "changes must not be empty")
            else
                match
                    FileAgent.runBounded
                        FileAgent.ChangeProcessingTimeoutMs
                        (fun () -> applyBatch changes)
                with
                | Error err -> reply.Reply(Error err)
                | Ok (newState, confirmations, logEntries, externalChanges) ->
                    let preGraph = state.Value.graph
                    let pathValidation =
                        match graphOnly, liveSaveDataDir with
                        | true, _ -> Ok ()
                        | false, None -> Ok ()
                        | false, Some dataDir ->
                            DocumentPersistence.validatePathMoves
                                dataDir
                                preGraph
                                newState.graph
                            |> Result.bind (fun () ->
                                DocumentPersistence.validateGraphDiskEffects
                                    dataDir
                                    preGraph
                                    newState.graph)

                    match pathValidation with
                    | Error err -> reply.Reply(Error err)
                    | Ok () ->
                        let livePersist =
                            match graphOnly, liveSaveDataDir, logEntries with
                            | false, Some dataDir, _::_ ->
                                let ops =
                                    logEntries
                                    |> List.collect (fun (_, change) -> change.ops)
                                FileAgent.runBounded
                                    FileAgent.ChangeProcessingTimeoutMs
                                    (fun () ->
                                        persistGraphOps
                                            dataDir
                                            preGraph
                                            newState.graph
                                            ops)
                                |> Result.map Some
                            | _ -> Ok None
                        match livePersist with
                        | Error err -> reply.Reply(Error err)
                        | Ok stampedOpt ->
                            let stampOps, stateToStore, persistMessage =
                                match stampedOpt with
                                | Some stamped ->
                                    PersistStamp.opsBetween
                                        newState.graph
                                        stamped.graph,
                                    { newState with graph = stamped.graph },
                                    stamped.message
                                | None -> [], newState, None
                            let fresh = logEntries |> List.map snd
                            let stampedFresh, ackChanges =
                                overlayFresh confirmations fresh stampOps
                            let logEntries' =
                                List.zip
                                    (logEntries |> List.map fst)
                                    stampedFresh
                            match
                                FileAgent.runBounded
                                    FileAgent.ChangeProcessingTimeoutMs
                                    (fun () -> persistBatch stateToStore logEntries')
                            with
                            | Error err -> reply.Reply(Error err)
                            | Ok () ->
                                state.Value <- stateToStore
                                reply.Reply(
                                    Ok(accepted ackChanges externalChanges persistMessage))
                                if graphOnly then
                                    persistedGraph.Value <- stateToStore.graph
                                elif not (List.isEmpty logEntries) then
                                    persistedGraph.Value <- stateToStore.graph
                                    if snapshotInProgress.Value then snapshotNeeded.Value <- true
                                    else startSnapshot inbox

        let operationContext msg =
            match msg with
            | GetState _ -> "GetState", ""
            | GetRevision _ -> "GetRevision", ""
            | GetChangesSince (after, _) ->
                "GetChangesSince", $"after={after}"
            | PostChange (changes, _) ->
                "PostChange", $"changeCount={changes.Length}"
            | PostGraphOnlyChange (changes, _) ->
                "PostGraphOnlyChange", $"changeCount={changes.Length}"
            | SnapshotDone _ -> "SnapshotDone", ""

        let replyFailure operation msg =
            let error =
                match liveSaveDataDir with
                | Some dir ->
                    $"Internal server error in DbAgent {operation} (dataDir={dir})."
                | None ->
                    $"Internal server error in DbAgent {operation}."
            match msg with
            | GetState reply -> reply.Reply(Error error)
            | GetRevision reply -> reply.Reply(Error error)
            | GetChangesSince (_, reply) -> reply.Reply(Error error)
            | PostChange (_, reply) -> reply.Reply(Error error)
            | PostGraphOnlyChange (_, reply) -> reply.Reply(Error error)
            | SnapshotDone _ -> ()

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

        let tryHandleRead msg =
            match msg with
            | GetState reply ->
                Some(async { reply.Reply(Ok state.Value) })
            | GetRevision reply ->
                Some(async { reply.Reply(Ok state.Value.revision) })
            | GetChangesSince (after, reply) ->
                Some(async {
                    let rows =
                        Database.getChangesAfterCheckpointRevision
                            connectionString
                            after
                        |> Async.AwaitTask
                        |> Async.RunSynchronously
                    let changes =
                        rows
                        |> List.choose (fun row ->
                            decodeChangePayload row.payload |> Result.toOption)
                    reply.Reply(Ok changes)
                })
            | _ -> None

        let mailbox =
            MailboxProcessor<FileAgentMsg>.Start(fun inbox ->
                let rec loop () = async {
                    let! msg = inbox.Receive()
                    try
                        match tryHandleRead msg with
                        | Some read -> do! read
                        | None ->
                            match msg with
                            | PostChange (changes, reply) ->
                                handlePostChange changes false reply inbox
                            | PostGraphOnlyChange (changes, reply) ->
                                handlePostChange changes true reply inbox
                            | SnapshotDone persisted ->
                                match persisted with
                                | Some graph
                                    when GraphProjection.graphEquals
                                        state.Value.graph
                                        graph ->
                                    persistedGraph.Value <- graph
                                | _ -> ()
                                snapshotInProgress.Value <- false
                                if snapshotNeeded.Value then startSnapshot inbox
                            | _ -> ()
                    with ex ->
                        let operation, context = operationContext msg
                        try logUnhandledException operation context ex with _ -> ()
                        try replyFailure operation msg with _ -> ()
                    return! loop ()
                }
                let rec failedLoop error = async {
                    let! msg = inbox.Receive()
                    match tryHandleRead msg with
                    | Some read -> do! read
                    | None ->
                        match msg with
                        | PostChange (_, reply)
                        | PostGraphOnlyChange (_, reply) ->
                            reply.Reply(Error error)
                        | SnapshotDone _ -> ()
                        | _ -> ()
                    return! failedLoop error
                }

                DbAgentStartup.run
                    (fun () -> runStartupSweep initialState.graph)
                    applyMaintenance
                    (fun () -> ready.TrySetResult() |> ignore)
                    tryHandleRead
                    loop
                    failedLoop
                    inbox)

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

    let private unwrap result =
        match result with
        | Ok value -> value
        | Error error -> failwith error

    let tryGetState (agent: DbAgent) : Async<Result<State, string>> =
        agent.mailbox.PostAndAsyncReply GetState

    let getState (agent: DbAgent) : Async<State> =
        async {
            let! result = tryGetState agent
            return unwrap result
        }

    let getRevision (agent: DbAgent) : Async<Revision> =
        async {
            let! result = agent.mailbox.PostAndAsyncReply GetRevision
            return unwrap result
        }

    let getChangesSince (agent: DbAgent) (after: int) : Async<Change list> =
        async {
            let! result =
                agent.mailbox.PostAndAsyncReply(fun reply ->
                    GetChangesSince(after, reply))
            return unwrap result
        }

    let private postChange
        (agent: DbAgent)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        agent.mailbox.PostAndAsyncReply(fun reply -> PostChange(changes, reply))

    let private postGraphOnlyChange
        (agent: DbAgent)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        agent.mailbox.PostAndAsyncReply(fun reply ->
            PostGraphOnlyChange(changes, reply))

    /// The only route from this agent to the Core Changes contract.
    let coreChanges (agent: DbAgent) : CoreChanges =
        { getState = fun () -> tryGetState agent
          getRevision = fun () -> getRevision agent
          getChangesSince = fun after -> getChangesSince agent after.Value
          isReady = fun () -> isReady agent
          postChange = postChange agent
          postGraphOnlyChange = postGraphOnlyChange agent }
