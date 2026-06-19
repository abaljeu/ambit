namespace Gambol.Server

open System
open System.IO
open System.Threading.Tasks
open Gambol.Shared

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

type FileAgentMsg =
    | GetState of AsyncReplyChannel<string>
    | GetRevision of AsyncReplyChannel<int>
    | GetChangesSince of after: int * AsyncReplyChannel<Change list>
    | PostChange of body: string * AsyncReplyChannel<Result<string, string>>
    | SnapshotDone

// FileAgent — serialises all reads/writes for a single file
type FileAgent = {
    mailbox: MailboxProcessor<FileAgentMsg>
    logStream: FileStream
    initialState: Gambol.Shared.State  // post-replay state captured at startup; used by DB setup
}

module FileAgent =

    let create (dataDir: string) (filename: string) : FileAgent =
        let snapshotPath = Path.Combine(dataDir, filename)
        let metaPath = snapshotPath + ".meta"
        let logPath = snapshotPath + ".log"

        let initialState =
            match DocumentLoader.tryLoadState dataDir filename with
            | Ok state -> state
            | Error msg -> failwith msg

        let logStream =
            new FileStream(logPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)

        let offsetIndex = ChangeLog.buildIndex logStream
        let state = ref initialState
        let snapshotInProgress = ref false
        let snapshotNeeded = ref false

        let capturedInitialState = state.Value

        logStream.Seek(0L, SeekOrigin.End) |> ignore

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

        let startSnapshot (inbox: MailboxProcessor<FileAgentMsg>) =
            snapshotInProgress.Value <- true
            snapshotNeeded.Value <- false
            let rev = state.Value.revision.Value
            Task.Run(fun () ->
                try
                    match DocumentPersistence.writeAllDocuments dataDir state.Value.graph with
                    | Error _ -> ()
                    | Ok _ ->
                        let metaTmpPath = metaPath + ".tmp"
                        File.WriteAllText(metaTmpPath, string rev)
                        // Rename meta last — documents are written first; log retains changes.
                        File.Move(metaTmpPath, metaPath, true)
                with _ ->
                    () // snapshot failure is non-fatal; log has the data
                inbox.Post(SnapshotDone)
            ) |> ignore

        let applyBatch (changes: Change list) =
            let step (s, acked, logEntries, changed) (change: Change) =
                if isDuplicateSubmission change s.history then
                    Ok(s, acked @ [ change.changeId ], logEntries, changed)
                elif change.id <> s.revision.Value then
                    Error
                        $"Revision mismatch: server is at revision {s.revision.Value}, but this change targets base revision {change.id}."
                else
                    match History.applyChange change s with
                    | ApplyResult.Invalid (_, errMsg) -> Error errMsg
                    | ApplyResult.Unchanged s' ->
                        Ok(s', acked @ [ change.changeId ], logEntries, changed)
                    | ApplyResult.Changed s' ->
                        let nextRev = s.revision.Value + 1
                        let nextState = { s' with revision = Revision nextRev }
                        let logEntry = change.id, ChangeLog.encodeChange change
                        Ok(nextState, acked @ [ change.changeId ], logEntries @ [ logEntry ], true)

            changes
            |> List.fold
                (fun acc change ->
                    match acc with
                    | Error err -> Error err
                    | Ok stateAndLog -> step stateAndLog change)
                (Ok(state.Value, [], [], false))

        let persistLogEntries (logEntries: (int * string) list) =
            let logStart = logStream.Length
            logStream.Seek(0L, SeekOrigin.End) |> ignore
            try
                let offsets = ChangeLog.appendEntries logStream logEntries
                Ok offsets
            with ex ->
                logStream.SetLength(logStart)
                logStream.Seek(0L, SeekOrigin.End) |> ignore
                Error $"Log error: {ex.Message}"

        let handlePostChange body (reply: AsyncReplyChannel<Result<string, string>>) inbox =
            match Decode.fromString Serialization.decodeChangeBatch body with
            | Error err ->
                reply.Reply(Error $"Invalid JSON: {err}")
            | Ok batch ->
                match applyBatch batch.changes with
                | Error err -> reply.Reply(Error err)
                | Ok (newState, ackedChangeIds, logEntries, changed) ->
                    match persistLogEntries logEntries with
                    | Error err -> reply.Reply(Error err)
                    | Ok offsets ->
                        offsets |> List.iter offsetIndex.Add
                        state.Value <- newState
                        reply.Reply(Ok (encodeChangeAckJson ackedChangeIds))
                        if changed then
                            if snapshotInProgress.Value then snapshotNeeded.Value <- true
                            else startSnapshot inbox

        let mailbox = MailboxProcessor<FileAgentMsg>.Start(fun inbox ->
            let rec loop () = async {
                let! msg = inbox.Receive()
                match msg with
                | GetState reply ->
                    reply.Reply(encodeStateJson ())
                | GetRevision reply ->
                    reply.Reply(state.Value.revision.Value)
                | GetChangesSince (after, reply) ->
                    let changes =
                        [ after .. offsetIndex.Count - 1 ]
                        |> List.choose (fun i ->
                            let _, json = ChangeLog.readEntryAt logStream offsetIndex.[i]
                            match ChangeLog.decodeChange json with
                            | Ok change -> Some change
                            | Error _ -> None)
                    reply.Reply(changes)
                | PostChange (body, reply) ->
                    handlePostChange body reply inbox
                | SnapshotDone ->
                    snapshotInProgress.Value <- false
                    if snapshotNeeded.Value then startSnapshot inbox
                return! loop ()
            }
            loop ()
        )

        { mailbox = mailbox; logStream = logStream; initialState = capturedInitialState }

    let getState (agent: FileAgent) : Async<string> =
        agent.mailbox.PostAndAsyncReply(GetState)

    let getRevision (agent: FileAgent) : Async<int> =
        agent.mailbox.PostAndAsyncReply(GetRevision)

    let getChangesSince (agent: FileAgent) (after: int) : Async<Change list> =
        agent.mailbox.PostAndAsyncReply(fun reply -> GetChangesSince(after, reply))

    let postChange (agent: FileAgent) (body: string) : Async<Result<string, string>> =
        agent.mailbox.PostAndAsyncReply(fun reply -> PostChange(body, reply))

    let dispose (agent: FileAgent) =
        agent.logStream.Flush()
        agent.logStream.Dispose()
