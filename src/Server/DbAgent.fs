namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

/// PostgreSQL-backed agent. Same message type as `FileAgent`.
type DbAgent =
    { mailbox: MailboxProcessor<FileAgentMsg>
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
        (runStartupSweep: Graph -> Result<Guid list, string>)
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

        let encodeStateJson () =
            ApiResponseSerialization.encodeStateResponse
                { graph = state.Value.graph
                  revision = state.Value.revision
                  isReady = ready.Task.IsCompletedSuccessfully }
            |> Encode.toString 0

        let encodeChangeAckJson
            (ackedChangeIds: Guid list)
            (stampOps: Op list)
            (message: string option)
            =
            Encode.toString 0 (
                Serialization.encodeChangeBatchAck
                    { revision = state.Value.revision
                      ackedChangeIds = ackedChangeIds
                      stampOps = stampOps
                      message = message })

        let isPersistedDuplicateSubmission (action: HistoryAction) =
            Database.hasPersistedChangeId
                connectionString
                (HistoryAction.actionId action)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        let applyBatch (actions: HistoryAction list) =
            let step (s, acked, logEntries) action =
                let actionId = HistoryAction.actionId action
                let baseRevision = HistoryAction.baseRevision action
                if baseRevision <> s.revision.Value
                    && isPersistedDuplicateSubmission action then
                    Ok(s, actionId :: acked, logEntries)
                elif baseRevision <> s.revision.Value then
                    Error
                        $"Revision mismatch: server is at revision {s.revision.Value}, but this action targets base revision {baseRevision}."
                else
                    match History.applyAction action s with
                    | Error error -> Error error
                    | Ok (s', materialized) ->
                        let nextRev = s.revision.Value + 1
                        let nextState = { s' with revision = Revision nextRev }
                        let logEntry = nextRev, materialized
                        Ok(nextState, actionId :: acked, logEntry :: logEntries)

            actions
            |> List.fold
                (fun acc action ->
                    match acc with
                    | Error err -> Error err
                    | Ok stateAndLog -> step stateAndLog action)
                (Ok(state.Value, [], []))
            |> Result.map (fun (newState, acked, entries) ->
                newState, List.rev acked, List.rev entries)

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
            body
            graphOnly
            (reply: AsyncReplyChannel<Result<string, string>>)
            inbox
            =
            match Decode.fromString Serialization.decodeChangeBatch body with
            | Error err ->
                reply.Reply(Error $"Invalid JSON: {err}")
            | Ok batch ->
                match applyBatch batch.changes with
                | Error err -> reply.Reply(Error err)
                | Ok (newState, ackedChangeIds, logEntries) ->
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
                            let logEntries' =
                                logEntries
                                |> List.map (fun (rev, change) -> rev, change)
                                |> fun entries ->
                                    let changes = entries |> List.map snd
                                    let enriched = PersistStamp.appendToLast changes stampOps
                                    List.zip (entries |> List.map fst) enriched
                            match
                                FileAgent.runBounded
                                    FileAgent.ChangeProcessingTimeoutMs
                                    (fun () -> persistBatch stateToStore logEntries')
                            with
                            | Error err -> reply.Reply(Error err)
                            | Ok () ->
                                state.Value <- stateToStore
                                reply.Reply(
                                    Ok(
                                        encodeChangeAckJson
                                            ackedChangeIds
                                            stampOps
                                            persistMessage))
                                if graphOnly then
                                    persistedGraph.Value <- stateToStore.graph
                                elif not (List.isEmpty logEntries) then
                                    persistedGraph.Value <- stateToStore.graph
                                    if snapshotInProgress.Value then snapshotNeeded.Value <- true
                                    else startSnapshot inbox

        let operationContext msg =
            let bodyLength (body: string) =
                if isNull body then 0 else body.Length
            match msg with
            | GetState _ -> "GetState", ""
            | GetRevision _ -> "GetRevision", ""
            | GetChangesSince (after, _) ->
                "GetChangesSince", $"after={after}"
            | PostChange (body, _) ->
                "PostChange", $"bodyLength={bodyLength body}"
            | PostGraphOnlyChange (body, _) ->
                "PostGraphOnlyChange", $"bodyLength={bodyLength body}"
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
                Some(async { reply.Reply(Ok (encodeStateJson ())) })
            | GetRevision reply ->
                Some(async { reply.Reply(Ok state.Value.revision.Value) })
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
                            | PostChange (body, reply) ->
                                handlePostChange body false reply inbox
                            | PostGraphOnlyChange (body, reply) ->
                                handlePostChange body true reply inbox
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
                    trimDeletedIds
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
                DatabaseProjection.startupSweepPatch
                |> DatabaseProjection.maintenanceCommand
                |> DatabaseProjection.executeMaintenance connectionString
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> Ok
            with ex ->
                Error $"Startup projection sweep failed: {ex.Message}"

        createLoaded
            initialState
            connectionString
            liveSaveDataDir
            DocumentPersistence.persistGraphOps
            runStartupSweep

    let createForTest
        (initialState: State)
        (runStartupSweep: Graph -> Result<Guid list, string>)
        : DbAgent =
        createLoaded
            initialState
            ""
            None
            DocumentPersistence.persistGraphOps
            runStartupSweep

    /// Test-only seam: injects a stand-in for the live-persist step (and an optional
    /// liveSaveDataDir) so failure/timeout behavior can be exercised without a real DB
    /// connection or the real (slow/bug-prone) document reconcile path.
    let createForTestWithDependencies
        (initialState: State)
        (liveSaveDataDir: string option)
        (persistGraphOps: string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>)
        (runStartupSweep: Graph -> Result<Guid list, string>)
        : DbAgent =
        createLoaded initialState "" liveSaveDataDir persistGraphOps runStartupSweep

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

    let tryGetState (agent: DbAgent) : Async<Result<string, string>> =
        agent.mailbox.PostAndAsyncReply(GetState)

    let getState (agent: DbAgent) : Async<string> =
        async {
            let! result = tryGetState agent
            return unwrap result
        }

    let getRevision (agent: DbAgent) : Async<int> =
        async {
            let! result = agent.mailbox.PostAndAsyncReply(GetRevision)
            return unwrap result
        }

    let getChangesSince (agent: DbAgent) (after: int) : Async<Change list> =
        async {
            let! result =
                agent.mailbox.PostAndAsyncReply(fun reply ->
                    GetChangesSince(after, reply))
            return unwrap result
        }

    let postChange (agent: DbAgent) (body: string) : Async<Result<string, string>> =
        agent.mailbox.PostAndAsyncReply(fun reply -> PostChange(body, reply))

    let postGraphOnlyChange
        (agent: DbAgent)
        (body: string)
        : Async<Result<string, string>> =
        agent.mailbox.PostAndAsyncReply(fun reply ->
            PostGraphOnlyChange(body, reply))
