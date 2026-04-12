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

        let encodeChangeAckJson (ackChangeId: Guid) =
            Encode.toString 0 (
                Thoth.Json.Core.Encode.object
                    [ "ackChangeId", Thoth.Json.Core.Encode.guid ackChangeId
                      "revision", Serialization.encodeRevision state.Value.revision ])

        let isDuplicateSubmission (change: Change) (history: History) =
            history.past |> List.exists (fun c -> c.id = change.id && c.changeId = change.changeId)

        let handlePostChange body (reply: AsyncReplyChannel<Result<string, string>>) _inbox =
            match Decode.fromString Serialization.decodeChange body with
            | Error err ->
                reply.Reply(Error $"Invalid JSON: {err}")
            | Ok change ->
                if isDuplicateSubmission change state.Value.history then
                    reply.Reply(Ok (encodeChangeAckJson change.changeId))
                elif change.id <> state.Value.revision.Value then
                    reply.Reply(
                        Error
                            $"Revision mismatch: server is at revision {state.Value.revision.Value}, but this change targets base revision {change.id}.")
                else
                    match History.applyChange change state.Value with
                    | ApplyResult.Invalid (_, errMsg) ->
                        reply.Reply(Error errMsg)
                    | ApplyResult.Unchanged _ ->
                        reply.Reply(Ok (encodeChangeAckJson change.changeId))
                    | ApplyResult.Changed newState ->
                        let json = ChangeLog.encodeChange change
                        let serverRevAfter = state.Value.revision.Value + 1

                        try
                            use conn = Database.getConnection connectionString
                            conn.Open()
                            use tx = conn.BeginTransaction()

                            (Database.appendChangeWithTx tx serverRevAfter change.id json)
                                .GetAwaiter()
                                .GetResult()

                            (Database.replaceGraphProjectionWithTx tx newState.graph serverRevAfter)
                                .GetAwaiter()
                                .GetResult()

                            tx.Commit()

                            state.Value <-
                                { newState with revision = Revision serverRevAfter }

                            reply.Reply(Ok (encodeChangeAckJson change.changeId))
                        with ex ->
                            eprintfn "DbAgent: failed to persist change %d: %s" change.id ex.Message
                            reply.Reply(Error $"Database error: {ex.Message}")

        let mailbox =
            MailboxProcessor<FileAgentMsg>.Start(fun inbox ->
                let rec loop () = async {
                    let! msg = inbox.Receive()
                    match msg with
                    | GetState reply ->
                        reply.Reply(encodeStateJson ())
                    | GetRevision reply ->
                        reply.Reply(state.Value.revision.Value)
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

    let postChange (agent: DbAgent) (body: string) : Async<Result<string, string>> =
        agent.mailbox.PostAndAsyncReply(fun reply -> PostChange(body, reply))
