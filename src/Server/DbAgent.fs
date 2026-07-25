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
        (writeBackup: State -> unit)
        (runStartupSweep: Graph -> Result<Guid list, string>)
        : DbAgent =
        let state = ref initialState
        let persistedGraph = ref initialState.graph
        let snapshotInProgress = ref false
        let snapshotNeeded = ref false
        let snapshotWaiters = ref<AsyncReplyChannel<Result<unit, string>> list> []
        let ready =
            TaskCompletionSource<unit>(
                TaskCreationOptions.RunContinuationsAsynchronously)

        let trimDeletedIds deletedIds =
            let deletedNodeIds = deletedIds |> List.map NodeId
            let trim = DatabaseProjection.trimDeletedNodes deletedNodeIds
            state.Value <- { state.Value with graph = trim state.Value.graph }
            persistedGraph.Value <- trim persistedGraph.Value

        let notifySnapshotWaiters () =
            if not snapshotInProgress.Value && not snapshotNeeded.Value then
                snapshotWaiters.Value
                |> List.iter (fun reply -> reply.Reply(Ok ()))
                snapshotWaiters.Value <- []

        let encodeStateJson () =
            ApiResponseSerialization.encodeStateResponse
                { graph = state.Value.graph
                  revision = state.Value.revision
                  isReady = ready.Task.IsCompletedSuccessfully }
            |> Encode.toString 0

        let encodeChangeAckJson (ackedChangeIds: Guid list) (stampOps: Op list) =
            Encode.toString 0 (
                Serialization.encodeChangeBatchAck
                    { revision = state.Value.revision
                      ackedChangeIds = ackedChangeIds
                      stampOps = stampOps })

        let isDuplicateSubmission (change: Change) (history: History) =
            history.past |> List.exists (fun c -> c.id = change.id && c.changeId = change.changeId)

        let isPersistedDuplicateSubmission (change: Change) =
            Database.hasPersistedChangeId connectionString change.changeId
            |> Async.AwaitTask
            |> Async.RunSynchronously

        let applyBatch (changes: Change list) =
            let step (s, acked, logEntries) (change: Change) =
                if isDuplicateSubmission change s.history then
                    Ok(s, acked @ [ change.changeId ], logEntries)
                elif change.id <> s.revision.Value
                    && isPersistedDuplicateSubmission change then
                    Ok(s, acked @ [ change.changeId ], logEntries)
                elif change.id <> s.revision.Value then
                    Error
                        $"Revision mismatch: server is at revision {s.revision.Value}, but this change targets base revision {change.id}."
                else
                    match History.applyChange change s with
                    | ApplyResult.Invalid (_, errMsg) -> Error errMsg
                    | ApplyResult.Unchanged s' ->
                        Ok(s', acked @ [ change.changeId ], logEntries)
                    | ApplyResult.Changed s' ->
                        let nextRev = s.revision.Value + 1
                        let nextState = { s' with revision = Revision nextRev }
                        let logEntry = nextRev, change
                        Ok(nextState, acked @ [ change.changeId ], logEntries @ [ logEntry ])

            changes
            |> List.fold
                (fun acc change ->
                    match acc with
                    | Error err -> Error err
                    | Ok stateAndLog -> step stateAndLog change)
                (Ok(state.Value, [], []))

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
                        let liveSave =
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
                                    Error ()
                                | Ok stamped -> Ok stamped
                            | None -> Ok postGraph
                        writeBackup snapshotState
                        match liveSave with
                        | Ok graph -> Some graph
                        | Error () -> None
                    with ex ->
                        eprintfn "DbAgent: failed to write disk backup: %s" ex.Message
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
                                DocumentPersistence.persistGraphOps
                                    dataDir
                                    preGraph
                                    newState.graph
                                    ops
                                |> Result.map Some
                            | _ -> Ok None
                        match livePersist with
                        | Error err -> reply.Reply(Error err)
                        | Ok stampedOpt ->
                            let stampOps, stateToStore =
                                match stampedOpt with
                                | Some stamped ->
                                    PersistStamp.opsBetween newState.graph stamped,
                                    { newState with graph = stamped }
                                | None -> [], newState
                            let logEntries' =
                                logEntries
                                |> List.map (fun (rev, change) -> rev, change)
                                |> fun entries ->
                                    let changes = entries |> List.map snd
                                    let enriched = PersistStamp.appendToLast changes stampOps
                                    List.zip (entries |> List.map fst) enriched
                            match persistBatch stateToStore logEntries' with
                            | Error err -> reply.Reply(Error err)
                            | Ok () ->
                                state.Value <- stateToStore
                                reply.Reply(Ok (encodeChangeAckJson ackedChangeIds stampOps))
                                if graphOnly then
                                    persistedGraph.Value <- stateToStore.graph
                                elif not (List.isEmpty logEntries) then
                                    persistedGraph.Value <- stateToStore.graph
                                    if snapshotInProgress.Value then snapshotNeeded.Value <- true
                                    else startSnapshot inbox

        let tryHandleRead msg =
            match msg with
            | GetState reply ->
                Some(async { reply.Reply(encodeStateJson ()) })
            | GetRevision reply ->
                Some(async { reply.Reply(state.Value.revision.Value) })
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
                    reply.Reply(changes)
                })
            | _ -> None

        let mailbox =
            MailboxProcessor<FileAgentMsg>.Start(fun inbox ->
                let rec loop () = async {
                    let! msg = inbox.Receive()
                    match tryHandleRead msg with
                    | Some read -> do! read
                    | None ->
                        match msg with
                        | PostChange (body, reply) ->
                            handlePostChange body false reply inbox
                        | PostGraphOnlyChange (body, reply) ->
                            handlePostChange body true reply inbox
                        | FlushSnapshot reply ->
                            if snapshotInProgress.Value || snapshotNeeded.Value then
                                snapshotWaiters.Value <- reply :: snapshotWaiters.Value
                                if
                                    not snapshotInProgress.Value
                                    && snapshotNeeded.Value
                                then
                                    startSnapshot inbox
                            else
                                reply.Reply(Ok ())
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
                            else notifySnapshotWaiters ()
                        | _ -> ()
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
                        | FlushSnapshot reply -> reply.Reply(Error error)
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
        (writeBackup: State -> unit)
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
            writeBackup
            runStartupSweep

    let createForTest
        (initialState: State)
        (runStartupSweep: Graph -> Result<Guid list, string>)
        : DbAgent =
        createLoaded initialState "" None ignore runStartupSweep

    let createWithBackup (connectionString: string) (writeBackup: State -> unit) : DbAgent =
        createWithLiveSave connectionString None writeBackup

    let createWithDataDir
        (connectionString: string)
        (dataDir: string)
        (writeBackup: State -> unit)
        : DbAgent =
        createWithLiveSave connectionString (Some dataDir) writeBackup

    let create (connectionString: string) : DbAgent =
        createWithBackup connectionString (fun _ -> ())

    let isReady (agent: DbAgent) =
        agent.isReady ()

    let getState (agent: DbAgent) : Async<string> =
        agent.mailbox.PostAndAsyncReply(GetState)

    let getRevision (agent: DbAgent) : Async<int> =
        agent.mailbox.PostAndAsyncReply(GetRevision)

    let getChangesSince (agent: DbAgent) (after: int) : Async<Change list> =
        agent.mailbox.PostAndAsyncReply(fun reply -> GetChangesSince(after, reply))

    let postChange (agent: DbAgent) (body: string) : Async<Result<string, string>> =
        agent.mailbox.PostAndAsyncReply(fun reply -> PostChange(body, reply))

    let postGraphOnlyChange
        (agent: DbAgent)
        (body: string)
        : Async<Result<string, string>> =
        agent.mailbox.PostAndAsyncReply(fun reply ->
            PostGraphOnlyChange(body, reply))
