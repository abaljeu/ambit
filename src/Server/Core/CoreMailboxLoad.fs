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

    let private writeParseState
        (persist: PersistHandlers)
        (notice: Notice)
        (nodeId: NodeId)
        (parseState: ParseState)
        : Result<unit, string> =
        CoreEventDispatch.writeAxis persist notice (fun graph ->
            GraphMutate.setParseState nodeId parseState graph)

    /// Marks the node Unparsed, then pushes. The mark writes the axis
    /// directly and does not enqueue.
    let markUnparsedThenPush
        (persist: PersistHandlers)
        (rawPush: NodeId -> unit)
        (nodeId: NodeId)
        : Result<unit, string> =
        writeParseState
            persist
            (Notice.MarkUnparsed nodeId)
            nodeId
            ParseState.Unparsed
        |> Result.map (fun () -> rawPush nodeId)

    /// Core loop apply. NodeParsed writes Parsed only when pullingFile
    /// is clear, then pushes when the node is Parsed and Unpersisted.
    /// The push does not wait. noteParsed still runs. When pullingFile
    /// is set, that File Node stays Unparsed. It does not set Persisted.
    /// NodeParsed is the renamed ParseFinished.
    /// NodeUploading sets PullingFile in memory and Unpersisted. It does
    /// not insert the node. The column word stays Parsed or Unparsed.
    /// NodeReceived sets Unparsed and Persisted. withParseState clears
    /// PullingFile. The queue puller logs both messages.
    /// NodeParsing is logged by the queue puller. It does not set Unparsed,
    /// does not change parse or persist state, and does not push.
    /// NodePersisting sets Unpersisted and leaves parse state unchanged.
    /// PersistFailed does not change persist state.
    /// MarkUnparsed writes Unparsed only.
    /// NodePersisted runs the diff-count hook, sets persistedVersion
    /// to the written version, and applies updateTime. Persisted is
    /// set when the node is already Persisted, or when it is
    /// Unpersisted and the written version is at least changedVersion.
    /// An older write stays Unpersisted and changedVersion stays.
    /// On Ok it calls notePersisted, which requests the file diff.
    let private withPullingFile (nodeId: NodeId) (graph: Graph) =
        match Map.tryFind nodeId graph.nodes with
        | None -> Error "node not found"
        | Some node ->
            let updated = Node.withDocumentState PullingFile node
            Ok { graph with nodes = Map.add nodeId updated graph.nodes }

    let private writeUnpersisted
        (nodeId: NodeId)
        (live: Graph)
        : Result<Graph, string> =
        GraphMutate.setPersistState
            nodeId
            PersistState.Unpersisted
            live

    let private writePullingFileUnpersisted
        (persist: PersistHandlers)
        (nodeId: NodeId)
        : Result<unit, string> =
        CoreEventDispatch.writeAxis
            persist
            (Notice.NodeUploading nodeId)
            (fun live ->
                writeUnpersisted nodeId live
                |> Result.bind (withPullingFile nodeId))

    let private writeReceived
        (persist: PersistHandlers)
        (nodeId: NodeId)
        : Result<unit, string> =
        CoreEventDispatch.writeAxis
            persist
            (Notice.NodeReceived nodeId)
            (fun live ->
            match GraphMutate.setParseState nodeId ParseState.Unparsed live with
            | Error err -> Error err
            | Ok graph ->
                GraphMutate.setPersistState
                    nodeId
                    PersistState.Persisted
                    graph)

    let private stampUpdateTime
        (nodeId: NodeId)
        (updateTime: DateTime)
        (graph: Graph)
        : Result<Graph, string> =
        match Map.tryFind nodeId graph.nodes with
        | None -> Error "node not found"
        | Some node ->
            let stamped = NodeUpdateTime.withStamp updateTime node
            Ok { graph with nodes = Map.add nodeId stamped graph.nodes }

    let private parsedAndUnpersisted (node: Node) =
        node.parseState = ParseState.Parsed
        && node.persistState = PersistState.Unpersisted

    /// Read the node after Parsed is written. Push only that case.
    let private pushParsedUnpersisted
        (persist: PersistHandlers)
        (nodeId: NodeId)
        =
        match persist.getState () with
        | Error _ -> ()
        | Ok state ->
            match Map.tryFind nodeId state.graph.nodes with
            | Some node when parsedAndUnpersisted node ->
                persist.pushPersist nodeId
            | _ -> ()

    let private applyNodeParsed
        (persist: PersistHandlers)
        (nodeId: NodeId)
        =
        writeParseState
            persist
            (Notice.NodeParsed nodeId)
            nodeId
            ParseState.Parsed
        |> Result.map (fun () ->
            pushParsedUnpersisted persist nodeId
            persist.noteParsed nodeId)

    let private withPersistedVersion
        (nodeId: NodeId)
        (persistedVersion: EventId)
        (graph: Graph)
        : Result<Graph, string> =
        match Map.tryFind nodeId graph.nodes with
        | None -> Error "node not found"
        | Some node ->
            let updated =
                { node with
                    documentState =
                        { node.documentState with
                            persistedVersion = persistedVersion } }
            Ok { graph with nodes = Map.add nodeId updated graph.nodes }

    let private writePersisted persist nodeId persistedVersion updateTime =
        CoreEventDispatch.writeAxis
            persist
            (Notice.NodePersisted(nodeId, persistedVersion, updateTime))
            (fun live ->
                match Map.tryFind nodeId live.nodes with
                | None -> Error "node not found"
                | Some node ->
                    let next =
                        DocumentState.persistStateForWritten
                            node.documentState
                            persistedVersion
                    GraphMutate.setPersistState nodeId next live
                    |> Result.bind (
                        withPersistedVersion nodeId persistedVersion)
                    |> Result.bind (stampUpdateTime nodeId updateTime))

    let internal applyNotice
        (persist: PersistHandlers)
        (msg: Notice)
        : Result<unit, string> =
        match msg with
        | Notice.NodeParsed nodeId ->
            applyNodeParsed persist nodeId
        | Notice.NodeUploading nodeId ->
            writePullingFileUnpersisted persist nodeId
        | Notice.NodeReceived nodeId ->
            writeReceived persist nodeId
        | Notice.NodeParsing _ -> Ok ()
        | Notice.NodePersisting nodeId ->
            CoreEventDispatch.writeAxis persist msg (fun live ->
                writeUnpersisted nodeId live)
        | Notice.PersistFailed _ ->
            persist.noteWriteFailed ()
            Ok ()
        | Notice.MarkUnparsed nodeId ->
            writeParseState persist msg nodeId ParseState.Unparsed
        | Notice.NodePersisted (nodeId, persistedVersion, updateTime) ->
            persist.noteDiffDone nodeId
            match
                writePersisted persist nodeId persistedVersion updateTime
            with
            | Error err -> Error err
            | Ok () ->
                persist.notePersisted ()
                Ok ()

    let private reportNotice (context: MailboxContext) (err: string) =
        context.onError "Notice" err (exn err)

    let private replyUnsupported
        (reply: AsyncReplyChannel<Result<'a, string>>)
        =
        reply.Reply(Error "unsupported")

    let private replyLoad
        (context: MailboxContext)
        (caller: Caller)
        (orderId: Guid)
        (subject: NodeId)
        (reply: AsyncReplyChannel<Result<EventId, string>>)
        =
        match dispatchLoad context caller subject with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            match
                CoreEventDispatch.appendLifecycle
                    (CoreMailboxEvents.eventDispatchContext context)
                    caller
                    orderId
                    (EventBody.Load subject)
            with
            | Error err -> reply.Reply(Error err)
            | Ok stored -> reply.Reply(Ok stored.id)

    let private placeOrder context caller orderId order reply =
        match order with
        | Order.Load subject ->
            replyLoad context caller orderId subject reply
        | Order.Change _
        | Order.Undo _
        | Order.Redo _
        | Order.StartActor _
        | Order.StartPeerActor _
        | Order.LoadSave _
        | Order.Cancel _ -> replyUnsupported reply

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
        | Mailbox.PlaceOrder (caller, orderId, _, order, reply) ->
            if authorityHeld context caller then
                placeOrder context caller orderId order reply
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
        pushPersist = persist.pushPersist
        notePersisted = persist.notePersisted
        noteWriteFailed = persist.noteWriteFailed
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
        let eventLog, seedError, handlers, restartTip =
            match seedEventLog seeded with
            | Ok log -> log, None, seeded, EventLog.tip log
            | Error error ->
                EventLog.empty,
                Some error,
                failedSeed seeded error,
                EventId.zero
        let front = markUnparsedThenPush handlers rawPush
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
