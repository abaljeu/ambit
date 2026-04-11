namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

/// PostgreSQL-backed agent. Exposes the same message type as FileAgent so the
/// rest of the server (Api.fs, Server.fs) needs no structural changes.
type DbAgent = { mailbox: MailboxProcessor<FileAgentMsg> }

[<RequireQualifiedAccess>]
module DbAgent =

    // ------------------------------------------------------------------
    // Startup: load latest snapshot then replay newer changes
    // ------------------------------------------------------------------

    let private loadInitialState (connectionString: string) : Async<State> =
        async {
            let! snapshotOpt =
                Database.getLatestSnapshot connectionString |> Async.AwaitTask

            let (baseGraph, baseRevision) =
                match snapshotOpt with
                | None ->
                    Graph.create (), 0
                | Some row ->
                    Snapshot.read row.content, row.revision

            let! rows =
                Database.getChangesAfter connectionString baseRevision |> Async.AwaitTask

            let initialState =
                { graph = baseGraph
                  history = History.empty
                  revision = Revision baseRevision }

            let state =
                rows
                |> List.fold
                    (fun (st: State) row ->
                        match Decode.fromString Serialization.decodeChange row.payload with
                        | Error _ -> st
                        | Ok change ->
                            match History.applyChange change st with
                            | ApplyResult.Changed newState ->
                                { newState with revision = Revision (st.revision.Value + 1) }
                            | _ -> st)
                    initialState

            return state
        }

    // ------------------------------------------------------------------
    // Create
    // ------------------------------------------------------------------

    let create (connectionString: string) : DbAgent =

        // Init schema synchronously before the agent starts (blocking is acceptable at startup).
        Database.initSchema connectionString |> Async.AwaitTask |> Async.RunSynchronously

        let initialState =
            loadInitialState connectionString |> Async.RunSynchronously

        let state = ref initialState
        let snapshotInProgress = ref false
        let snapshotNeeded = ref false

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

        let startSnapshot (inbox: MailboxProcessor<FileAgentMsg>) =
            snapshotInProgress.Value <- true
            snapshotNeeded.Value <- false
            let text = Snapshot.write state.Value.graph
            let rev = state.Value.revision.Value
            Task.Run(fun () ->
                try
                    (Database.insertSnapshot connectionString rev text)
                        .GetAwaiter().GetResult()
                with _ ->
                    ()  // snapshot failure is non-fatal; changes table has the data
                inbox.Post(SnapshotDone)
            ) |> ignore

        let handlePostChange body (reply: AsyncReplyChannel<Result<string, string>>) inbox =
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
                        try
                            (Database.appendChange connectionString change.id json)
                                .GetAwaiter().GetResult()
                        with ex ->
                            // Log but don't fail the client; the in-memory state is still updated.
                            eprintfn "DbAgent: failed to persist change %d: %s" change.id ex.Message
                        state.Value <- { newState with revision = Revision (state.Value.revision.Value + 1) }
                        reply.Reply(Ok (encodeChangeAckJson change.changeId))
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
                    | PostChange (body, reply) ->
                        handlePostChange body reply inbox
                    | SnapshotDone ->
                        snapshotInProgress.Value <- false
                        if snapshotNeeded.Value then startSnapshot inbox
                    return! loop ()
                }
                loop ()
            )

        { mailbox = mailbox }

    // ------------------------------------------------------------------
    // Public API (mirrors FileAgent)
    // ------------------------------------------------------------------

    let getState (agent: DbAgent) : Async<string> =
        agent.mailbox.PostAndAsyncReply(GetState)

    let getRevision (agent: DbAgent) : Async<int> =
        agent.mailbox.PostAndAsyncReply(GetRevision)

    let postChange (agent: DbAgent) (body: string) : Async<Result<string, string>> =
        agent.mailbox.PostAndAsyncReply(fun reply -> PostChange(body, reply))
