namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

type FileAgentDependencies = {
    persistGraphOps:
        string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string>
    appendException: string -> string -> exn -> unit
    /// Wall-clock bound for the persistGraphOps call. Overridable so tests can exercise
    /// the timeout path without waiting the full production timeout.
    changeProcessingTimeoutMs: int
}

// FileAgent — serialises all reads/writes for a single file
type FileAgent = private {
    mailbox: MailboxProcessor<CoreMsg>
    logStream: FileStream
    initialState: Gambol.Shared.State  // checkpoint state captured at startup; used by DB setup
}

module FileAgent =

    let defaultDependencies (dataDir: string) =
        {
            persistGraphOps = DocumentPersistence.persistGraphOps
            appendException =
                HttpResponseLog.appendException
                    (HttpResponseLog.logPath dataDir)
                    "FileAgent"
            changeProcessingTimeoutMs =
                CoreMailboxBackend.ChangeProcessingTimeoutMs
        }

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
        let state = ref loadedState
        /// False after a soft file-write failure until process restart (meta stays behind).
        let persistClean = ref true

        let capturedInitialState = state.Value

        logStream.Seek(0L, SeekOrigin.End) |> ignore

        let accepted confirmed externalChanges message =
            CoreChanges.accepted
                state.Value.revision
                true
                confirmed
                externalChanges
                message

        let syncPersistChange
            (rev: int)
            (preGraph: Graph)
            (postGraph: Graph)
            (ops: Op list)
            =
            let persisted =
                CoreMailboxBackend.runBounded
                    dependencies.changeProcessingTimeoutMs
                    (fun () ->
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
            let step (s, confirmations, fresh, changed, externalChanges) change =
                match ChangeLog.tryFindByChangeId logStream offsetIndex change.changeId with
                | Some stored ->
                    Ok(s, stored :: confirmations, fresh, changed, externalChanges)
                | None ->
                    let result, amended, applied =
                        ChangeAmendment.applyChange change s

                    match result with
                    | ApplyResult.Invalid (_, errMsg) -> Error errMsg
                    | ApplyResult.Unchanged _ ->
                        Error "Unchanged submission is rejected."
                    | ApplyResult.Changed s' ->
                        let nextRev = s.revision.Value + 1
                        let nextState = { s' with revision = Revision nextRev }
                        Ok(
                            nextState,
                            applied :: confirmations,
                            applied :: fresh,
                            true,
                            externalChanges || amended)

            changes
            |> List.fold
                (fun acc change ->
                    match acc with
                    | Error err -> Error err
                    | Ok stateAndLog -> step stateAndLog change)
                (Ok(state.Value, [], [], false, false))
            |> Result.map (fun (newState, confirmations, fresh, changed, externalChanges) ->
                newState, List.rev confirmations, List.rev fresh, changed, externalChanges)

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

        let validatePostChange graphOnly preGraph postGraph =
            if graphOnly then
                Ok ()
            else
                DocumentPersistence.validatePathMoves dataDir preGraph postGraph
                |> Result.bind (fun () ->
                    DocumentPersistence.validateGraphDiskEffects
                        dataDir
                        preGraph
                        postGraph)

        let persistPostChange graphOnly changed preGraph newState fresh =
            if changed && not graphOnly then
                let ops =
                    fresh
                    |> List.collect (fun change -> change.ops)
                syncPersistChange
                    newState.revision.Value
                    preGraph
                    newState.graph
                    ops
                |> Result.map Some
            else
                Ok None

        let preparePostChange
            (newState: State)
            (confirmations: Change list)
            (fresh: Change list)
            (stampedOpt: PersistGraphOk option)
            =
            let stampOps, stampedGraph, persistMessage =
                match stampedOpt with
                | Some stamped ->
                    PersistStamp.opsBetween newState.graph stamped.graph,
                    stamped.graph,
                    stamped.message
                | None -> [], newState.graph, None
            let stampedFresh, ackChanges =
                CoreMailboxBackend.overlayFresh confirmations fresh stampOps
            let encodedLog =
                stampedFresh
                |> List.map (fun change ->
                    change.id, ChangeLog.encodeChange change)
            let finalState =
                match stampedOpt with
                | Some _ -> { newState with graph = stampedGraph }
                | None -> newState
            finalState, ackChanges, encodedLog, persistMessage

        let commitPostChange
            finalState
            ackChanges
            externalChanges
            persistMessage
            encodedLog
            (reply: AsyncReplyChannel<Result<CoreChangesAccepted, string>>)
            =
            match persistLogEntries encodedLog with
            | Error err -> reply.Reply(Error err)
            | Ok offsets ->
                offsets |> List.iter offsetIndex.Add
                state.Value <- finalState
                reply.Reply(
                    Ok(accepted ackChanges externalChanges persistMessage))

        let handlePostChange
            (changes: Change list)
            graphOnly
            (reply: AsyncReplyChannel<Result<CoreChangesAccepted, string>>)
            =
            if changes.IsEmpty then
                reply.Reply(Error "changes must not be empty")
            else
                match applyBatch changes with
                | Error err -> reply.Reply(Error err)
                | Ok (newState, confirmations, fresh, changed, externalChanges) ->
                    let preGraph = state.Value.graph
                    match validatePostChange graphOnly preGraph newState.graph with
                    | Error err -> reply.Reply(Error err)
                    | Ok () ->
                        match
                            persistPostChange
                                graphOnly
                                changed
                                preGraph
                                newState
                                fresh
                        with
                        | Error err -> reply.Reply(Error err)
                        | Ok stampedOpt ->
                            let finalState, ackChanges, encodedLog, persistMessage =
                                preparePostChange
                                    newState
                                    confirmations
                                    fresh
                                    stampedOpt
                            commitPostChange
                                finalState
                                ackChanges
                                externalChanges
                                persistMessage
                                encodedLog
                                reply

        let replyFailure operation msg =
            let error =
                $"Internal server error in FileAgent {operation} (dataDir={dataDir})."
            CoreMailboxBackend.replyFailure error msg

        let dispatch msg =
            match msg with
            | GetState reply ->
                reply.Reply(Ok state.Value)
            | GetRevision reply ->
                reply.Reply(Ok state.Value.revision)
            | GetChangesSince (after, reply) ->
                let changes =
                    [ after.Value .. offsetIndex.Count - 1 ]
                    |> List.choose (fun i ->
                        let _, json =
                            ChangeLog.readEntryAt logStream offsetIndex.[i]
                        match ChangeLog.decodeChange json with
                        | Ok change -> Some change
                        | Error _ -> None)
                reply.Reply(Ok changes)
            | PostChange (changes, reply) ->
                handlePostChange changes false reply
            | PostGraphOnlyChange (changes, reply) ->
                handlePostChange changes true reply
            | SnapshotDone _ -> ()

        let mailbox = MailboxProcessor<CoreMsg>.Start(fun inbox ->
            let rec loop () = async {
                let! msg = inbox.Receive()
                try
                    dispatch msg
                with ex ->
                    let operation, context =
                        CoreMailboxBackend.operationContext msg
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

    let tryGetState (agent: FileAgent) : Async<Result<State, string>> =
        CoreMailbox.tryGetState agent.mailbox

    let getState (agent: FileAgent) : Async<Result<State, string>> =
        CoreMailbox.getState agent.mailbox

    let getRevision (agent: FileAgent) : Async<Revision> =
        CoreMailbox.getRevision agent.mailbox

    let getChangesSince (agent: FileAgent) (after: Revision) : Async<Change list> =
        CoreMailbox.getChangesSince agent.mailbox after

    /// The only route from this agent to the Core Changes contract.
    let coreChanges (agent: FileAgent) : CoreChanges =
        CoreMailbox.coreChanges (fun () -> true) agent.mailbox

    let flushSnapshot (_: FileAgent) : Async<Result<unit, string>> =
        async { return Ok () }

    /// Checkpoint state captured at startup; used by DB setup and startup checks.
    let initialState (agent: FileAgent) : State =
        agent.initialState

    let dispose (agent: FileAgent) =
        agent.logStream.Flush()
        agent.logStream.Dispose()
