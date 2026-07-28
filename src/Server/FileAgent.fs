namespace Gambol.Server

open System
open System.IO
open System.Threading.Tasks
open Gambol.Shared

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

type FileAgentMsg =
    | GetState of AsyncReplyChannel<Result<string, string>>
    | GetRevision of AsyncReplyChannel<Result<int, string>>
    | GetChangesSince of
        after: int * AsyncReplyChannel<Result<Change list, string>>
    | PostChange of body: string * AsyncReplyChannel<Result<string, string>>
    | PostGraphOnlyChange of body: string * AsyncReplyChannel<Result<string, string>>
    | SnapshotDone of graph: Graph option

type FileAgentDependencies = {
    persistGraphOps:
        string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>
    appendException: string -> string -> exn -> unit
    /// Wall-clock bound for the persistGraphOps call. Overridable so tests can exercise
    /// the timeout path without waiting the full production timeout.
    changeProcessingTimeoutMs: int
}

// FileAgent — serialises all reads/writes for a single file
type FileAgent = {
    mailbox: MailboxProcessor<FileAgentMsg>
    logStream: FileStream
    initialState: Gambol.Shared.State  // post-replay state captured at startup; used by DB setup
}

module FileAgent =

    /// Bound on wall-clock time for a single change's persist step (disk write via
    /// DocumentWarm/CStyleReconcile). That reconcile path is a known-slow/hanging
    /// algorithm; this timeout exists to keep the mailbox loop responsive, not to fix it.
    [<Literal>]
    let ChangeProcessingTimeoutMs = 8000

    /// Runs a synchronous computation on a background Task, bounding wall-clock time so
    /// a pathologically slow computation can never wedge the caller's mailbox loop. If the
    /// timeout elapses, the background Task is abandoned (fire-and-forget): it may still run
    /// to completion later and write to disk concurrently with subsequently accepted changes.
    /// Uses WaitAny (not Wait/Result) because WaitAny reports timeout vs settled without
    /// itself throwing on a faulted task; GetAwaiter().GetResult() then rethrows `f`'s
    /// original exception unwrapped (as if called synchronously), so the caller's existing
    /// exception handling is unaffected.
    let runBounded (timeoutMs: int) (f: unit -> Result<'a, string>) : Result<'a, string> =
        let task = Task.Run(fun () -> f ())
        let settledIndex = Task.WaitAny([| task :> Task |], timeoutMs)
        if settledIndex = -1 then
            Error "change processing timed out"
        else
            task.GetAwaiter().GetResult()

    let defaultDependencies (dataDir: string) =
        {
            persistGraphOps = DocumentPersistence.persistGraphOps
            appendException =
                HttpResponseLog.appendException
                    (HttpResponseLog.logPath dataDir)
                    "FileAgent"
            changeProcessingTimeoutMs = ChangeProcessingTimeoutMs
        }

    /// Apply log entries at indexes `[fromRev .. offsetIndex.Count - 1]`.
    /// Index `i` is the change that produces server revision `i + 1`.
    let private replayLogFrom
        (logStream: FileStream)
        (offsetIndex: int64 ResizeArray)
        (fromRev: int)
        (start: State)
        : State =
        let lo = max 0 fromRev
        let hi = offsetIndex.Count - 1
        if hi < lo then
            start
        else
            [ lo..hi ]
            |> List.fold
                (fun s i ->
                    let _, json = ChangeLog.readEntryAt logStream offsetIndex.[i]
                    match ChangeLog.decodeChange json with
                    | Error _ -> s
                    | Ok change ->
                        match History.applyChange change s with
                        | ApplyResult.Invalid _ -> s
                        | ApplyResult.Unchanged s' ->
                            { s' with revision = Revision(i + 1) }
                        | ApplyResult.Changed s' ->
                            { s' with revision = Revision(i + 1) })
                { start with revision = Revision lo }

    let createWithDependencies
        (dependencies: FileAgentDependencies)
        (dataDir: string)
        : FileAgent =
        let loadedState =
            match DocumentLoader.tryLoadState dataDir with
            | Ok state -> state
            | Error msg -> failwith msg

        let logStream = Bookkeeping.openLogStream dataDir

        let offsetIndex = ChangeLog.buildIndex logStream
        // Soft-failed live-saves advance the log without rewriting disk; replay the
        // tail past the checkpoint revision so those graph edits survive restart.
        let initialState =
            replayLogFrom
                logStream
                offsetIndex
                loadedState.revision.Value
                loadedState
        if initialState.revision.Value > loadedState.revision.Value then
            Bookkeeping.writeRevision dataDir initialState.revision.Value
            |> ignore
        let state = ref initialState
        /// False after a soft file-write failure until process restart (meta stays behind).
        let persistClean = ref true

        let capturedInitialState = state.Value

        logStream.Seek(0L, SeekOrigin.End) |> ignore

        let encodeStateJson () =
            ApiResponseSerialization.encodeStateResponse
                { graph = state.Value.graph
                  revision = state.Value.revision
                  isReady = true }
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

        let isDuplicateSubmission (change: Change) (history: History) =
            history.past |> List.exists (fun c -> c.id = change.id && c.changeId = change.changeId)

        let syncPersistChange
            (rev: int)
            (preGraph: Graph)
            (postGraph: Graph)
            (ops: Op list)
            =
            let persisted =
                runBounded dependencies.changeProcessingTimeoutMs (fun () ->
                    dependencies.persistGraphOps dataDir preGraph postGraph ops)
            match persisted with
            | Error err -> Error err
            | Ok stamped ->
                // Soft-fail (or prior soft-fail in this process): keep meta behind the
                // log so startup replay can restore graph edits that never hit disk.
                if stamped.message.IsSome then
                    persistClean.Value <- false
                let shouldCheckpoint =
                    stamped.message.IsNone && persistClean.Value
                if not shouldCheckpoint then
                    Ok stamped
                else
                    match Bookkeeping.writeRevision dataDir rev with
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
                            let stampOps, stampedGraph, persistMessage =
                                match stampedOpt with
                                | Some stamped ->
                                    PersistStamp.opsBetween
                                        newState.graph
                                        stamped.graph,
                                    stamped.graph,
                                    stamped.message
                                | None -> [], newState.graph, None
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
                                reply.Reply(
                                    Ok(
                                        encodeChangeAckJson
                                            ackedChangeIds
                                            stampOps
                                            persistMessage))

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
                $"Internal server error in FileAgent {operation} (dataDir={dataDir})."
            match msg with
            | GetState reply -> reply.Reply(Error error)
            | GetRevision reply -> reply.Reply(Error error)
            | GetChangesSince (_, reply) -> reply.Reply(Error error)
            | PostChange (_, reply) -> reply.Reply(Error error)
            | PostGraphOnlyChange (_, reply) -> reply.Reply(Error error)
            | SnapshotDone _ -> ()

        let dispatch msg =
            match msg with
            | GetState reply ->
                reply.Reply(Ok (encodeStateJson ()))
            | GetRevision reply ->
                reply.Reply(Ok state.Value.revision.Value)
            | GetChangesSince (after, reply) ->
                let changes =
                    [ after .. offsetIndex.Count - 1 ]
                    |> List.choose (fun i ->
                        let _, json =
                            ChangeLog.readEntryAt logStream offsetIndex.[i]
                        match ChangeLog.decodeChange json with
                        | Ok change -> Some change
                        | Error _ -> None)
                reply.Reply(Ok changes)
            | PostChange (body, reply) ->
                handlePostChange body false reply
            | PostGraphOnlyChange (body, reply) ->
                handlePostChange body true reply
            | SnapshotDone _ -> ()

        let mailbox = MailboxProcessor<FileAgentMsg>.Start(fun inbox ->
            let rec loop () = async {
                let! msg = inbox.Receive()
                try
                    dispatch msg
                with ex ->
                    let operation, context = operationContext msg
                    try
                        dependencies.appendException operation context ex
                    with _ ->
                        ()
                    try
                        replyFailure operation msg
                    with _ ->
                        ()
                return! loop ()
            }
            loop ()
        )

        { mailbox = mailbox; logStream = logStream; initialState = capturedInitialState }

    let create (dataDir: string) : FileAgent =
        createWithDependencies (defaultDependencies dataDir) dataDir

    let private unwrap result =
        match result with
        | Ok value -> value
        | Error error -> failwith error

    let tryGetState (agent: FileAgent) : Async<Result<string, string>> =
        agent.mailbox.PostAndAsyncReply(GetState)

    let getState (agent: FileAgent) : Async<string> =
        async {
            let! result = tryGetState agent
            return unwrap result
        }

    let getRevision (agent: FileAgent) : Async<int> =
        async {
            let! result = agent.mailbox.PostAndAsyncReply(GetRevision)
            return unwrap result
        }

    let getChangesSince (agent: FileAgent) (after: int) : Async<Change list> =
        async {
            let! result =
                agent.mailbox.PostAndAsyncReply(fun reply ->
                    GetChangesSince(after, reply))
            return unwrap result
        }

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
