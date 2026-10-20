namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared

[<RequireQualifiedAccess>]
module internal CoreMailboxLoad =

    type MailboxContext = CoreMailboxEvents.MailboxContext
    type Started = CoreMailboxEvents.Started
    module EventLog = Gambol.Shared.EventLog

    let private loadSubjectError =
        function
        | LoadSubjectRejection.NotLoadSubject ->
            "Load subject is not a Workspace, Directory, or File node"
        | LoadSubjectRejection.NotFound -> "Load subject not found"

    let private isLoadSubject (graph: Graph) (subject: NodeId) =
        LoadSubject.ofId graph subject
        |> Result.map (fun _ -> ())
        |> Result.mapError loadSubjectError

    /// Mailbox Load door. Desk Load calls this in the same turn.
    let private dispatchLoad
        (context: MailboxContext)
        (caller: Caller)
        (subject: NodeId)
        : Result<unit, string> =
        match context.persist.getState () with
        | Error err -> Error err
        | Ok state ->
            match isLoadSubject state.graph subject with
            | Error err -> Error err
            | Ok () -> context.parsePush subject

    let private dispatchStartLoadSaveCommand
        context
        caller
        path
        peerName
        (request: LoadSaveCommandRequest)
        reply
        =
        CoreMailboxActors.dispatchActorStartResult
            context
            caller
            request.start
            reply
            (fun _ getState ->
                DeskLoadPush.runLoadSave
                    { pool = context.pool
                      getState = getState
                      peerName = peerName }
                    (dispatchLoad context caller)
                    request
                    path
                |> Result.mapError StartError.Rejected)
    let private runMsg (context: MailboxContext) (msg: CoreMsg) =
        match msg with
        | PostGraphOnly (caller, event, reply) ->
            CoreMailboxEvents.dispatchPostGraphOnly context caller event reply
        | StartActor (caller, request, reply) ->
            CoreMailboxActors.dispatchStartActor context caller request reply
        | StartPeerActor (caller, peerName, request, reply) ->
            CoreMailboxActors.dispatchStartPeerActor
                context caller peerName request reply
        | StartLoadSaveCommand (caller, path, peerName, request, reply) ->
            dispatchStartLoadSaveCommand
                context caller path peerName request reply
        | RecordSearchStart (caller, request, reply) ->
            let dispatch = CoreMailboxEvents.eventDispatchContext context
            reply.Reply(CoreEventDispatch.actorStart dispatch caller request)
        | RecordSearchStop (caller, rootId, reply) ->
            let dispatch = CoreMailboxEvents.eventDispatchContext context
            reply.Reply(
                CoreEventDispatch.actorStop
                    dispatch caller rootId ActorSucceeded)
        | LoadNode (caller, subject, reply) ->
            reply.Reply(dispatchLoad context caller subject)
        | ActorStop (caller, result, reply) ->
            CoreMailboxActors.dispatchActorStop context caller result reply
        | CancelActor (caller, focusId, reply) ->
            CoreMailboxActors.dispatchCancelActor context caller focusId reply
        | PostEvent (caller, event, reply) ->
            CoreMailboxActors.dispatchPostEvent context caller event reply

    let private reportDispatchException
        (context: MailboxContext)
        (operation, detail)
        ex
        (replyFailure: string -> unit)
        =
        try
            context.onError operation detail ex
        with _ ->
            ()
        try
            replyFailure (context.formatError operation)
        with _ ->
            ()

    let private droppedAuthority = "authority dropped"

    /// Actor authority is the live pool secret. DropAuthority leaves it.
    /// A caller Session never held still runs.
    let private authorityHeld
        (context: MailboxContext)
        (caller: Caller)
        =
        match caller.authority with
        | Authority "Actor" -> true
        | _ -> not (Session.wasDropped context.session caller)

    let private messageCaller (msg: CoreMsg) =
        match msg with
        | PostGraphOnly (caller, _, _)
        | StartActor (caller, _, _)
        | StartPeerActor (caller, _, _, _)
        | StartLoadSaveCommand (caller, _, _, _, _)
        | RecordSearchStart (caller, _, _)
        | RecordSearchStop (caller, _, _)
        | LoadNode (caller, _, _)
        | ActorStop (caller, _, _)
        | CancelActor (caller, _, _)
        | PostEvent (caller, _, _) -> caller

    let private rejectDropped (msg: CoreMsg) =
        CoreMailboxEvents.replyFailure droppedAuthority msg

    let private dispatch (context: MailboxContext) (msg: CoreMsg) : unit =
        if authorityHeld context (messageCaller msg) then
            try
                runMsg context msg
            with ex ->
                let operation, detail =
                    CoreMailboxEvents.operationContext msg
                reportDispatchException
                    context
                    (operation, detail)
                    ex
                    (fun error ->
                        CoreMailboxEvents.replyFailure error msg)
        else
            rejectDropped msg

    let private writeAxis
        (persist: PersistHandlers)
        (mutate: Graph -> Result<Graph, string>)
        : Result<unit, string> =
        match persist.getState () with
        | Error err -> Error err
        | Ok state ->
            match mutate state.graph with
            | Error err -> Error err
            | Ok graph ->
                persist.replaceGraph graph
                Ok ()

    let private writeParseState
        (persist: PersistHandlers)
        (nodeId: NodeId)
        (parseState: ParseState)
        : Result<unit, string> =
        writeAxis persist (fun graph ->
            GraphMutate.setParseState nodeId parseState graph)

    /// Marks the node Unparsed, then pushes. The mark writes the axis
    /// directly and does not enqueue.
    let markUnparsedThenPush
        (persist: PersistHandlers)
        (rawPush: NodeId -> unit)
        (nodeId: NodeId)
        : Result<unit, string> =
        writeParseState persist nodeId ParseState.Unparsed
        |> Result.map (fun () -> rawPush nodeId)

    /// Core loop apply. NodeParsed writes Parsed only when pullingFile
    /// is clear, then noteParsed so a blocked edit can catch up. When
    /// pullingFile is set, that File Node stays Unparsed. It does not
    /// set Persisted. NodeParsed is the renamed ParseFinished.
    /// NodeUploading and NodeReceived are logged by the queue puller and
    /// do not set state.
    /// MarkUnparsed writes Unparsed only.
    /// SnapshotDone runs the diff-count hook, then sets Persisted.
    let internal applyNotice
        (persist: PersistHandlers)
        (msg: Notice)
        : Result<unit, string> =
        match msg with
        | Notice.NodeParsed nodeId ->
            match writeParseState persist nodeId ParseState.Parsed with
            | Error err -> Error err
            | Ok () ->
                persist.noteParsed nodeId
                Ok ()
        | Notice.NodeUploading _
        | Notice.NodeReceived _ -> Ok ()
        | Notice.MarkUnparsed nodeId ->
            writeParseState persist nodeId ParseState.Unparsed
        | Notice.SnapshotDone nodeId ->
            persist.noteDiffDone nodeId
            writeAxis persist (fun live ->
                GraphMutate.setPersistState
                    nodeId
                    PersistState.Persisted
                    live)

    let private reportNotice (context: MailboxContext) (err: string) =
        context.onError "Notice" err (exn err)

    let private replyUnsupported
        (reply: AsyncReplyChannel<Result<'a, string>>)
        =
        reply.Reply(Error "unsupported")

    let private runQuery (context: MailboxContext) (query: Query) =
        match query with
        | Query.State reply ->
            match context.persist.getState () with
            | Error err -> reply.Reply(Error err)
            | Ok state ->
                let graph =
                    GraphSpan.withLockPresent
                        (context.pool.liveFocusIds ())
                        state.graph
                reply.Reply(Ok { state with graph = graph })
        | Query.LastEventId reply ->
            reply.Reply(context.persist.getEventId ())
        | Query.EventsSince (after, reply) ->
            match context.eventLogSeedError with
            | Some error -> reply.Reply(Error error)
            | None ->
                context.eventLog.Value
                |> EventLog.since after
                |> Ok
                |> reply.Reply

    let private queryOperation query =
        match query with
        | Query.State _ -> "State", ""
        | Query.LastEventId _ -> "LastEventId", ""
        | Query.EventsSince (after, _) ->
            "EventsSince", $"after={after}"

    let private replyQueryFailure error query =
        match query with
        | Query.State reply -> reply.Reply(Error error)
        | Query.LastEventId reply -> reply.Reply(Error error)
        | Query.EventsSince (_, reply) -> reply.Reply(Error error)

    let private dispatchAsk (context: MailboxContext) (query: Query) =
        try
            runQuery context query
        with ex ->
            let operation, detail = queryOperation query
            reportDispatchException
                context
                (operation, detail)
                ex
                (fun error -> replyQueryFailure error query)

    let private dispatchMailbox
        (context: MailboxContext)
        (message: Mailbox)
        =
        match message with
        | Mailbox.CoreMsg msg -> dispatch context msg
        | Mailbox.PlaceOrder (caller, _, _, _, reply) ->
            if authorityHeld context caller then
                replyUnsupported reply
            else
                reply.Reply(Error droppedAuthority)
        | Mailbox.Ask query -> dispatchAsk context query

    let private dispatchItem
        (context: MailboxContext)
        (item: CoreQueue)
        =
        match item with
        | CoreQueue.Public message -> dispatchMailbox context message
        | CoreQueue.Notice msg ->
            context.noticeLog := msg :: context.noticeLog.Value
            match applyNotice context.persist msg with
            | Ok () -> ()
            | Error err -> reportNotice context err
        | CoreQueue.DropAuthority stopped ->
            Session.removeAuthority stopped

    let private failedPersist (persist: PersistHandlers) error : PersistHandlers = {
        getState = persist.getState
        getEventId = persist.getEventId
        getEventsSince = persist.getEventsSince
        getEventLog = persist.getEventLog
        appendEvent = fun _ -> Error error
        applyEvent = fun _ _ -> Error error
        replaceGraph = persist.replaceGraph
        noteDiffDone = ignore
        noteParsed = persist.noteParsed
    }

    let private failedSeed persist error : PersistHandlers =
        { failedPersist persist error with
            getEventsSince = fun _ -> Error error }

    let private seedEventLog (persist: PersistHandlers) =
        try
            // getEventsSince must be healthy before we trust getEventLog for seed;
            // otherwise a broken Poll door would leave writes open.
            match persist.getEventsSince EventId.zero with
            | Error error -> Error error
            | Ok _ -> persist.getEventLog ()
        with ex ->
            Error ex.Message

    let private reportPushError (persist: PersistFilling) (err: string) =
        persist.onError "parse push" err (exn err)

    let private pushReporting
        (persist: PersistFilling)
        (handlers: PersistHandlers)
        (rawPush: NodeId -> unit)
        (nodeId: NodeId)
        =
        match markUnparsedThenPush handlers rawPush nodeId with
        | Ok () -> ()
        | Error err -> reportPushError persist err

    let makeMailBox
        (persist: PersistFilling)
        pool
        (rawPush: NodeId -> unit)
        (session: Session)
        : MailboxContext * (NodeId -> unit) =
        let seeded = persist.handlers
        let eventLog, seedError, handlers =
            match seedEventLog seeded with
            | Ok log -> log, None, seeded
            | Error error ->
                EventLog.empty, Some error, failedSeed seeded error
        let front = markUnparsedThenPush handlers rawPush
        let reported = pushReporting persist handlers rawPush
        { persist = handlers
          pool = pool
          parsePush = front
          onError = persist.onError
          formatError = persist.formatError
          eventLog = ref eventLog
          eventLogSeedError = seedError
          coreChanges = ref None
          noticeLog = ref []
          session = session },
        reported

    let private started mailbox (context: MailboxContext) : Started =
        { processor = mailbox
          bindCoreChanges =
            fun make -> context.coreChanges.Value <- Some make }

    let private isStartupRead (message: Mailbox) =
        match message with
        | Mailbox.Ask (Query.State _)
        | Mailbox.Ask (Query.LastEventId _)
        | Mailbox.Ask (Query.EventsSince _) -> true
        | Mailbox.CoreMsg _
        | Mailbox.PlaceOrder _ -> false

    let rec private pump
        (context: MailboxContext)
        (inbox: MailboxProcessor<CoreQueue>)
        =
        async {
            let! item = inbox.Receive()
            dispatchItem context item
            return! pump context inbox
        }

    let private runUntil (until: Async<Result<unit, string>>) =
        Task.Run(fun () ->
            try
                until |> Async.RunSynchronously
            with ex ->
                Error $"Startup prelude failed: {ex.Message}")

    let private startNow (context: MailboxContext) : Started =
        started (MailboxProcessor<CoreQueue>.Start(pump context)) context

    let private startWithPrelude
        (context: MailboxContext)
        (until: Async<Result<unit, string>>)
        : Started =
        let untilTask = runUntil until
        let mailbox = MailboxProcessor<CoreQueue>.Start(fun inbox ->
            let rec startupLoop () = async {
                if untilTask.IsCompleted then
                    match untilTask.GetAwaiter().GetResult() with
                    | Ok () -> return! pump context inbox
                    | Error error -> return! failedLoop error
                else
                    let! _ =
                        inbox.TryScan(
                            (fun item ->
                                match item with
                                | CoreQueue.Public message
                                    when isStartupRead message ->
                                    Some(async {
                                        dispatchMailbox context message
                                    })
                                | _ -> None),
                            timeout = 20)
                    return! startupLoop ()
            }
            and failedLoop error = async {
                let! item = inbox.Receive()
                let failed = failedPersist context.persist error
                dispatchItem { context with persist = failed } item
                return! failedLoop error
            }
            startupLoop ()
        )
        started mailbox context

    let start (context: MailboxContext) (persist: PersistFilling) : Started =
        match persist.until with
        | None -> startNow context
        | Some until -> startWithPrelude context until
