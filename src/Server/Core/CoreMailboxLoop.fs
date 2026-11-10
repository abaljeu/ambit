namespace Gambol.Server

open System.Threading.Tasks
open Gambol.Shared

[<RequireQualifiedAccess>]
module internal CoreMailboxLoop =

    type MailboxContext = CoreMailboxEvents.MailboxContext
    type Started = CoreMailboxEvents.Started
    module EventLog = Gambol.Shared.EventLog

    let private runMsg (context: MailboxContext) (msg: CoreMsg) =
        match msg with
        | StartLoadSaveCommand (caller, path, peerName, request, reply) ->
            CoreMailboxLoad.dispatchStartLoadSaveCommand
                context caller path peerName request reply
        | RecordSearchStop (caller, rootId, reply) ->
            let dispatch = CoreMailboxEvents.eventDispatchContext context
            reply.Reply(
                CoreEventDispatch.actorStop
                    dispatch caller rootId ActorSucceeded)
        | ActorStop (caller, result, reply) ->
            CoreMailboxActors.dispatchActorStop context caller result reply
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

    /// Authority "Actor" is held while its receipt secret is live.
    /// Any other caller is held unless Session dropped that caller.
    let private authorityHeld
        (context: MailboxContext)
        (caller: Caller)
        =
        match caller.authority with
        | Authority "Actor" ->
            CoreMailboxEvents.actorReceiptIsLive context.pool caller
        | _ -> not (Session.wasDropped context.session caller)

    let private messageCaller (msg: CoreMsg) =
        match msg with
        | StartLoadSaveCommand (caller, _, _, _, _)
        | RecordSearchStop (caller, _, _)
        | ActorStop (caller, _, _)
        | PostEvent (caller, _, _) -> caller

    let private rejectDropped (msg: CoreMsg) =
        CoreMailboxEvents.replyFailure droppedAuthority msg

    let private catchDispatch
        (context: MailboxContext)
        (operation: string * string)
        (replyFailure: string -> unit)
        (run: unit -> unit)
        =
        try
            run ()
        with ex ->
            reportDispatchException
                context
                operation
                ex
                replyFailure

    let private dispatch (context: MailboxContext) (msg: CoreMsg) : unit =
        if authorityHeld context (messageCaller msg) then
            catchDispatch
                context
                (CoreMailboxEvents.operationContext msg)
                (fun error ->
                    CoreMailboxEvents.replyFailure error msg)
                (fun () -> runMsg context msg)
        else
            rejectDropped msg

    let internal applyNotice
        (persist: CoreMailboxAccess)
        (msg: Notice)
        : Result<unit, string> =
        match msg with
        | Notice.NodeParsed nodeId ->
            CoreMailboxFilesAxis.applyNodeParsed persist nodeId
        | Notice.NodeUploading nodeId ->
            CoreMailboxFilesAxis.writePullingFileUnpersisted persist nodeId
        | Notice.NodeReceived nodeId ->
            CoreMailboxFilesAxis.writeReceived persist nodeId
        | Notice.NodeParsing _
        | Notice.ParseFailed _
        | Notice.NodePersisting _
        | Notice.ActorStopped _ -> Ok ()
        | Notice.NodeUnpersisted _ ->
            CoreEventDispatch.writeAxis
                persist
                msg
                (NoticeGraph.writeGraph msg)
        | Notice.PersistFailed _ ->
            persist.noteWriteFailed ()
            Ok ()
        | Notice.NodeUnparsed _ ->
            CoreEventDispatch.writeAxis
                persist
                msg
                (NoticeGraph.writeGraph msg)
        | Notice.NodePersisted (nodeId, persistedVersion, updateTime) ->
            persist.noteDiffDone nodeId
            match
                CoreMailboxFilesAxis.writePersisted
                    persist nodeId persistedVersion updateTime
            with
            | Error err -> Error err
            | Ok () ->
                persist.notePersisted ()
                Ok ()

    let private reportNotice (context: MailboxContext) (err: string) =
        context.onError "Notice" err (exn err)

    let private editEvent (placed: PlacedOrder) (ops: Op list) =
        let edited =
            { placed with
                order = Order.Change(ops, ChangeOrigin.Edit) }
        MailboxLog.eventFromPlacedOrder (EventId.zero) edited

    let private replyPosted
        (context: MailboxContext)
        (caller: Caller)
        (reply: AsyncReplyChannel<Result<EventId, string>>)
        (event: Ev)
        =
        match
            CoreEventDispatch.postEvent
                (CoreMailboxEvents.eventDispatchContext context)
                caller
                event
        with
        | Error err -> reply.Reply(Error err)
        | Ok (stored, _) -> reply.Reply(Ok stored.id)

    let private replyEdit
        (context: MailboxContext)
        (placed: PlacedOrder)
        (caller: Caller)
        (reply: AsyncReplyChannel<Result<EventId, string>>)
        (ops: Op list)
        =
        match editEvent placed ops with
        | Error err -> reply.Reply(Error err)
        | Ok event -> replyPosted context caller reply event

    let private replyPlaced
        (context: MailboxContext)
        (placed: PlacedOrder)
        (caller: Caller)
        (reply: AsyncReplyChannel<Result<EventId, string>>)
        =
        match MailboxLog.eventFromPlacedOrder (EventId.zero) placed with
        | Error err -> reply.Reply(Error err)
        | Ok event -> replyPosted context caller reply event

    /// Shared ActorFunction posts as the minted Actor credential.
    let private poolActor
        (actor:
            Gambol.Shared.FunctionRun ->
                Gambol.Shared.ActorChanges ->
                Async<unit>)
        (run: Gambol.Shared.FunctionRun)
        (door: CoreChangesDoor)
        : Async<unit> =
        let changes =
            CoreDoor.actorChanges
                (CoreDoor.forCaller
                    (CoreDoor.actorCaller run.secret)
                    door)
        actor run changes

    let private functionStartOf
        (context: MailboxContext)
        (order: ActorOrder)
        : FunctionStart =
        { actor = poolActor order.actor
          getGraph = fun () -> CoreMailboxEvents.graphNow context
          actorNodeId = order.actorNodeId }

    /// The pool admits and launches. Ok secret is the affirmation.
    let private scheduleActorOrder
        (context: MailboxContext)
        (caller: Caller)
        (order: ActorOrder)
        : Result<Credential, string> =
        match context.pool.startFunction (functionStartOf context order) with
        | Error err -> Error err
        | Ok secret ->
            match context.coreChanges.Value with
            | None ->
                context.pool.drop secret
                Error "mailbox not initialized"
            | Some make ->
                context.pool.schedule secret (make caller)
                Ok secret

    let private replyAffirmedStart
        (context: MailboxContext)
        (placed: PlacedOrder)
        (caller: Caller)
        (reply: AsyncReplyChannel<Result<EventId, string>>)
        (order: ActorOrder)
        (secret: Credential)
        =
        let body = EventBody.Order(Order.StartActor order)
        match
            CoreEventDispatch.appendLifecycle
                (CoreMailboxEvents.eventDispatchContext context)
                caller
                placed.orderId.Value
                body
        with
        | Error err ->
            context.pool.drop secret
            reply.Reply(Error err)
        | Ok stored -> reply.Reply(Ok stored.id)

    /// Pool first. StartActor is posted only after the affirmation.
    let private replyActorOrder
        (context: MailboxContext)
        (placed: PlacedOrder)
        (caller: Caller)
        (reply: AsyncReplyChannel<Result<EventId, string>>)
        (order: ActorOrder)
        =
        match CoreMailboxEvents.admitCaller context caller with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            match scheduleActorOrder context caller order with
            | Error err -> reply.Reply(Error err)
            | Ok secret ->
                replyAffirmedStart
                    context placed caller reply order secret

    let private replyStarted
        (reply: AsyncReplyChannel<Result<EventId, string>>)
        (result: Result<unit, string>)
        =
        reply.Reply(Result.map (fun () -> EventId.zero) result)

    let private placeOrder
        (context: MailboxContext)
        (placed: PlacedOrder)
        (caller: Caller)
        (reply: AsyncReplyChannel<Result<EventId, string>>)
        =
        match placed.order with
        | Order.Load subject ->
            CoreMailboxLoad.replyLoad
                context placed caller reply subject
        | Order.Change (ops, ChangeOrigin.Edit) ->
            replyEdit context placed caller reply ops
        | Order.Change (_, ChangeOrigin.Parse)
        | Order.Undo _
        | Order.Redo _ ->
            replyPlaced context placed caller reply
        | Order.CancelActor focusId ->
            CoreMailboxActors.replyCancel
                context placed caller reply focusId
        | Order.StartActor order ->
            replyActorOrder context placed caller reply order
        | Order.StartActorDeprecated request ->
            CoreMailboxActors.dispatchStartActor
                context
                caller
                request
                (replyStarted reply)

    let private runQuery (context: MailboxContext) (query: Query) =
        match query with
        | Query.GraphAtEvent reply ->
            match context.persist.graphAtEvent () with
            | Error err -> reply.Reply(Error err)
            | Ok state ->
                let graph =
                    GraphSpan.withLockPresent
                        (context.pool.liveActorNodeIds ())
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
        | Query.GraphAtEvent _ -> "GraphAtEvent", ""
        | Query.LastEventId _ -> "LastEventId", ""
        | Query.EventsSince (after, _) ->
            "EventsSince", $"after={after}"

    let private replyQueryFailure error query =
        match query with
        | Query.GraphAtEvent reply -> reply.Reply(Error error)
        | Query.LastEventId reply -> reply.Reply(Error error)
        | Query.EventsSince (_, reply) -> reply.Reply(Error error)

    let private dispatchAsk (context: MailboxContext) (query: Query) =
        catchDispatch
            context
            (queryOperation query)
            (fun error -> replyQueryFailure error query)
            (fun () -> runQuery context query)

    let private dispatchMailbox
        (context: MailboxContext)
        (message: Mailbox)
        =
        match message with
        | Mailbox.CoreMsg msg -> dispatch context msg
        | Mailbox.PlaceOrder (placed, caller, reply) ->
            if authorityHeld context caller then
                catchDispatch
                    context
                    ("PlaceOrder", "")
                    (fun error -> reply.Reply(Error error))
                    (fun () ->
                        placeOrder context placed caller reply)
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
            let logged =
                CoreEventDispatch.logNotice
                    (CoreMailboxEvents.eventDispatchContext context)
                    msg
            match logged with
            | Error err -> reportNotice context err
            | Ok _ -> ()
            match applyNotice context.persist msg with
            | Ok () -> ()
            | Error err -> reportNotice context err
        | CoreQueue.DropAuthority stopped ->
            Session.removeAuthority stopped
        | CoreQueue.Shutdown reply ->
            reply.Reply()

    let private failedPersist (persist: CoreMailboxAccess) error : CoreMailboxAccess = {
        graphAtEvent = persist.graphAtEvent
        getEventId = persist.getEventId
        getEventsSince = persist.getEventsSince
        getEventLog = persist.getEventLog
        appendEvent = fun _ -> Error error
        applyEvent = fun _ _ -> Error error
        replaceGraph = persist.replaceGraph
        noteDiffDone = ignore
        noteParsed = persist.noteParsed
        pushPersist = persist.pushPersist
        notePersisted = persist.notePersisted
        noteWriteFailed = persist.noteWriteFailed
    }

    let private failedSeed persist error : CoreMailboxAccess =
        { failedPersist persist error with
            getEventsSince = fun _ -> Error error }

    let private seedEventLog (persist: CoreMailboxAccess) =
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
        (handlers: CoreMailboxAccess)
        (rawPush: NodeId -> unit)
        (nodeId: NodeId)
        =
        match CoreMailboxFilesAxis.markUnparsedThenPush handlers rawPush nodeId with
        | Ok () -> ()
        | Error err -> reportPushError persist err

    let makeMailBox
        (persist: PersistFilling)
        pool
        (rawPush: NodeId -> unit)
        (session: Session)
        : MailboxContext * (NodeId -> unit) =
        let seeded = persist.handlers
        let eventLog, seedError, handlers, restartTip =
            match seedEventLog seeded with
            | Ok log -> log, None, seeded, EventLog.tip log
            | Error error ->
                EventLog.empty,
                Some error,
                failedSeed seeded error,
                EventId.zero
        let front = CoreMailboxFilesAxis.markUnparsedThenPush handlers rawPush
        let reported = pushReporting persist handlers rawPush
        { persist = handlers
          pool = pool
          parsePush = front
          onError = persist.onError
          formatError = persist.formatError
          eventLog = ref eventLog
          restartTip = restartTip
          eventLogSeedError = seedError
          coreChanges = ref None
          session = session },
        reported

    let private armMailbox (mailbox: MailboxProcessor<CoreQueue>) =
        let gate = obj ()
        let stopped = ref false
        let shutdown () =
            let run =
                lock gate (fun () ->
                    if stopped.Value then
                        false
                    else
                        stopped.Value <- true
                        true)
            if run then
                try
                    mailbox.PostAndReply(fun reply ->
                        CoreQueue.Shutdown reply)
                    |> ignore
                with _ -> ()
        ServerRunRegistry.registerMailbox shutdown
        shutdown

    let private started mailbox (context: MailboxContext) : Started =
        { processor = mailbox
          bindCoreChanges =
            fun make -> context.coreChanges.Value <- Some make
          shutdown = armMailbox mailbox }

    let private isStartupRead (message: Mailbox) =
        match message with
        | Mailbox.Ask (Query.GraphAtEvent _)
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
            match item with
            | CoreQueue.Shutdown reply ->
                reply.Reply()
            | other ->
                dispatchItem context other
                return! pump context inbox
        }

    let private runUntil (until: Async<Result<unit, string>>) =
        let task =
            Task.Run(fun () ->
                try
                    until |> Async.RunSynchronously
                with ex ->
                    Error $"Startup prelude failed: {ex.Message}")
        ServerRunRegistry.track task
        task

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
                match item with
                | CoreQueue.Shutdown reply ->
                    reply.Reply()
                | other ->
                    let failed = failedPersist context.persist error
                    dispatchItem { context with persist = failed } other
                    return! failedLoop error
            }
            startupLoop ()
        )
        started mailbox context

    let start (context: MailboxContext) (persist: PersistFilling) : Started =
        match persist.until with
        | None -> startNow context
        | Some until -> startWithPrelude context until
