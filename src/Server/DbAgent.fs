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

    let create (connectionString: string) : DbAgent =
        let initialState =
            loadInitialState connectionString |> Async.RunSynchronously

        let state = ref initialState

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

        let handlePostChange body (reply: AsyncReplyChannel<Result<string, string>>) _inbox =
            match Decode.fromString Serialization.decodeChangeBatch body with
            | Error err ->
                reply.Reply(Error $"Invalid JSON: {err}")
            | Ok batch ->
                match applyBatch batch.changes with
                | Error err -> reply.Reply(Error err)
                | Ok (newState, ackedChangeIds, logEntries) ->
                    match persistBatch newState logEntries with
                    | Error err -> reply.Reply(Error err)
                    | Ok () ->
                        state.Value <- newState
                        reply.Reply(Ok (encodeChangeAckJson ackedChangeIds))

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
                        handlePostChange body reply inbox
                    | SnapshotDone ->
                        ()
                    return! loop ()
                }
                loop ())

        { mailbox = mailbox }

    let getState (agent: DbAgent) : Async<string> =
        agent.mailbox.PostAndAsyncReply(GetState)

    let getRevision (agent: DbAgent) : Async<int> =
        agent.mailbox.PostAndAsyncReply(GetRevision)

    let getChangesSince (agent: DbAgent) (after: int) : Async<Change list> =
        agent.mailbox.PostAndAsyncReply(fun reply -> GetChangesSince(after, reply))

    let postChange (agent: DbAgent) (body: string) : Async<Result<string, string>> =
        agent.mailbox.PostAndAsyncReply(fun reply -> PostChange(body, reply))
