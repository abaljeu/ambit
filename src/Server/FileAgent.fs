namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

type FileAgentMsg =
    | GetState of AsyncReplyChannel<string>
    | GetRevision of AsyncReplyChannel<int>
    | GetChangesSince of after: int * AsyncReplyChannel<Change list>
    | PostChange of body: string * AsyncReplyChannel<Result<string, string>>
    | PostGraphOnlyChange of body: string * AsyncReplyChannel<Result<string, string>>
    | SnapshotDone of graph: Graph option

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

        let capturedInitialState = state.Value

        logStream.Seek(0L, SeekOrigin.End) |> ignore

        let encodeStateJson () =
            ApiResponseSerialization.encodeStateResponse
                { graph = state.Value.graph
                  revision = state.Value.revision
                  isReady = true }
            |> Encode.toString 0

        let encodeChangeAckJson (ackedChangeIds: Guid list) (stampOps: Op list) =
            Encode.toString 0 (
                Serialization.encodeChangeBatchAck
                    { revision = state.Value.revision
                      ackedChangeIds = ackedChangeIds
                      stampOps = stampOps })

        let isDuplicateSubmission (change: Change) (history: History) =
            history.past |> List.exists (fun c -> c.id = change.id && c.changeId = change.changeId)

        let writeMetaRevision (rev: int) =
            try
                let metaTmpPath = metaPath + ".tmp"
                File.WriteAllText(metaTmpPath, string rev)
                File.Move(metaTmpPath, metaPath, true)
                Ok ()
            with ex ->
                Error ex.Message

        let syncPersistChange
            (rev: int)
            (preGraph: Graph)
            (postGraph: Graph)
            (ops: Op list)
            =
            match DocumentPersistence.persistGraphOps dataDir preGraph postGraph ops with
            | Error err -> Error err
            | Ok stamped ->
                match writeMetaRevision rev with
                | Error err -> Error err
                | Ok () -> Ok stamped

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
                        let logEntry = change.id, change
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

        let handlePostChange
            body
            graphOnly
            (reply: AsyncReplyChannel<Result<string, string>>)
            =
            match Decode.fromString Serialization.decodeChangeBatch body with
            | Error err ->
                reply.Reply(Error $"Invalid JSON: {err}")
            | Ok batch ->
                match applyBatch batch.changes with
                | Error err -> reply.Reply(Error err)
                | Ok (newState, ackedChangeIds, logEntries, changed) ->
                    let preGraph = state.Value.graph
                    let validation =
                        if graphOnly then
                            Ok ()
                        else
                            DocumentPersistence.validatePathMoves
                                dataDir
                                preGraph
                                newState.graph
                            |> Result.bind (fun () ->
                                DocumentPersistence.validateGraphDiskEffects
                                    dataDir
                                    preGraph
                                    newState.graph)
                    match validation with
                    | Error err -> reply.Reply(Error err)
                    | Ok () ->
                        let diskPersist =
                            if changed && not graphOnly then
                                let ops =
                                    logEntries
                                    |> List.collect (fun (_, change) -> change.ops)
                                syncPersistChange
                                    newState.revision.Value
                                    preGraph
                                    newState.graph
                                    ops
                                |> Result.map Some
                            else
                                Ok None
                        match diskPersist with
                        | Error err -> reply.Reply(Error err)
                        | Ok stampedOpt ->
                            let stampOps, stampedGraph =
                                match stampedOpt with
                                | Some stamped ->
                                    PersistStamp.opsBetween newState.graph stamped, stamped
                                | None -> [], newState.graph
                            let encodedLog =
                                logEntries
                                |> List.map snd
                                |> fun changes ->
                                    PersistStamp.appendToLast changes stampOps
                                |> List.map (fun change ->
                                    change.id, ChangeLog.encodeChange change)
                            match persistLogEntries encodedLog with
                            | Error err -> reply.Reply(Error err)
                            | Ok offsets ->
                                offsets |> List.iter offsetIndex.Add
                                let finalState =
                                    match stampedOpt with
                                    | Some _ ->
                                        { newState with graph = stampedGraph }
                                    | None -> newState
                                state.Value <- finalState
                                reply.Reply(Ok (encodeChangeAckJson ackedChangeIds stampOps))

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
                    handlePostChange body false reply
                | PostGraphOnlyChange (body, reply) ->
                    handlePostChange body true reply
                | _ -> ()
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

    let postGraphOnlyChange
        (agent: FileAgent)
        (body: string)
        : Async<Result<string, string>> =
        agent.mailbox.PostAndAsyncReply(fun reply ->
            PostGraphOnlyChange(body, reply))

    let flushSnapshot (_: FileAgent) : Async<Result<unit, string>> =
        async { return Ok () }

    let dispose (agent: FileAgent) =
        agent.logStream.Flush()
        agent.logStream.Dispose()
