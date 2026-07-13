namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

/// PostgreSQL-backed agent. Same message type as `FileAgent`.
type DbAgent = { mailbox: MailboxProcessor<FileAgentMsg> }

[<RequireQualifiedAccess>]
module DbAgent =

    let private decodeChangePayload (s: string) =
        Decode.fromString Serialization.decodeChange s

    let private loadInitialState (connectionString: string) : Async<State> =
        Database.loadPersistedState connectionString decodeChangePayload |> Async.AwaitTask

    let private createWithLiveSave
        (connectionString: string)
        (liveSaveDataDir: string option)
        (writeBackup: State -> unit)
        : DbAgent =
        let initialState =
            loadInitialState connectionString |> Async.RunSynchronously

        let state = ref initialState
        let persistedGraph = ref initialState.graph
        let snapshotInProgress = ref false
        let snapshotNeeded = ref false
        let snapshotWaiters = ref<AsyncReplyChannel<Result<unit, string>> list> []

        let notifySnapshotWaiters () =
            if not snapshotInProgress.Value && not snapshotNeeded.Value then
                snapshotWaiters.Value
                |> List.iter (fun reply -> reply.Reply(Ok ()))
                snapshotWaiters.Value <- []

        let encodeStateJson () =
            Encode.toString 0 (
                Thoth.Json.Core.Encode.object
                    [ "revision", Serialization.encodeRevision state.Value.revision
                      "graph", Serialization.encodeGraph state.Value.graph ])

        let encodeChangeAckJson (ackedChangeIds: Guid list) =
            Encode.toString 0 (
                Serialization.encodeChangeBatchAck
                    { revision = state.Value.revision
                      ackedChangeIds = ackedChangeIds })

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
                        let logEntry = nextRev, change, ChangeLog.encodeChange change
                        Ok(nextState, acked @ [ change.changeId ], logEntries @ [ logEntry ])

            changes
            |> List.fold
                (fun acc change ->
                    match acc with
                    | Error err -> Error err
                    | Ok stateAndLog -> step stateAndLog change)
                (Ok(state.Value, [], []))

        let persistBatch (newState: State) (logEntries: (int * Change * string) list) =
            try
                use conn = Database.getConnection connectionString
                conn.Open()
                use tx = conn.BeginTransaction()

                logEntries
                |> List.iter (fun (serverRevAfter, change, json) ->
                    (Database.appendChangeWithTx
                        tx
                        serverRevAfter
                        change.id
                        change.changeId
                        json)
                        .GetAwaiter()
                        .GetResult())

                match logEntries with
                | [] -> ()
                | _ ->
                    (Database.replaceGraphProjectionWithTx
                        tx
                        newState.graph
                        newState.revision.Value)
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
                        let liveSaveOk =
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
                                    false
                                | Ok _ -> true
                            | None -> true
                        writeBackup snapshotState
                        if liveSaveOk then Some postGraph else None
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
                        match persistBatch newState logEntries with
                        | Error err -> reply.Reply(Error err)
                        | Ok () ->
                            state.Value <- newState
                            reply.Reply(Ok (encodeChangeAckJson ackedChangeIds))
                            if graphOnly then
                                persistedGraph.Value <- newState.graph
                            elif not (List.isEmpty logEntries) then
                                if snapshotInProgress.Value then snapshotNeeded.Value <- true
                                else startSnapshot inbox

        let mailbox =
            MailboxProcessor<FileAgentMsg>.Start(fun inbox ->
                let rec loop () = async {
                    let! msg = inbox.Receive()
                    match msg with
                    | GetState reply ->
                        reply.Reply(encodeStateJson ())
                    | GetRevision reply ->
                        reply.Reply(state.Value.revision.Value)
                    | GetChangesSince (after, reply) ->
                        let rows =
                            Database.getChangesAfterCheckpointRevision connectionString after
                            |> Async.AwaitTask
                            |> Async.RunSynchronously
                        let changes =
                            rows
                            |> List.choose (fun row ->
                                match decodeChangePayload row.payload with
                                | Ok change -> Some change
                                | Error _ -> None)
                        reply.Reply(changes)
                    | PostChange (body, reply) ->
                        handlePostChange body false reply inbox
                    | PostGraphOnlyChange (body, reply) ->
                        handlePostChange body true reply inbox
                    | FlushSnapshot reply ->
                        if snapshotInProgress.Value || snapshotNeeded.Value then
                            snapshotWaiters.Value <- reply :: snapshotWaiters.Value
                            if not snapshotInProgress.Value && snapshotNeeded.Value then
                                startSnapshot inbox
                        else
                            reply.Reply(Ok ())
                    | SnapshotDone (Some snapshotGraph) ->
                        if GraphProjection.graphEquals state.Value.graph snapshotGraph then
                            persistedGraph.Value <- snapshotGraph
                        snapshotInProgress.Value <- false
                        if snapshotNeeded.Value then startSnapshot inbox
                        else notifySnapshotWaiters ()
                    | SnapshotDone None ->
                        snapshotInProgress.Value <- false
                        if snapshotNeeded.Value then startSnapshot inbox
                        else notifySnapshotWaiters ()
                    return! loop ()
                }
                loop ())

        { mailbox = mailbox }

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
