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
        match CoreMailboxEvents.admitCaller context caller with
        | Error err -> Error err
        | Ok () ->
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
    let private runRead (context: MailboxContext) (msg: CoreMsg) =
        match msg with
        | GetState reply ->
            match context.persist.getState () with
            | Error err -> reply.Reply(Error err)
            | Ok state ->
                let graph =
                    GraphSpan.withLockPresent
                        (context.pool.liveFocusIds ())
                        state.graph
                reply.Reply(Ok { state with graph = graph })
        | GetEventId reply -> reply.Reply(context.persist.getEventId ())
        | GetEventsSince (after, reply) ->
            reply.Reply(context.persist.getEventsSince after)
        | GetEventHistory reply ->
            reply.Reply(context.eventLog.Value)
        | EventsSince (after, reply) ->
            reply.Reply(EventLog.since after context.eventLog.Value)
        | _ -> ()

    let private runMsg (context: MailboxContext) (msg: CoreMsg) =
        match msg with
        | GetState _
        | GetEventId _
        | GetEventsSince _
        | GetEventHistory _
        | EventsSince _ -> runRead context msg
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
            CoreMailboxActors.dispatchRecordSearchStart
                context caller request reply
        | RecordSearchStop (caller, rootId, reply) ->
            CoreMailboxActors.dispatchRecordSearchStop
                context caller rootId reply
        | Load (caller, subject, reply) ->
            reply.Reply(dispatchLoad context caller subject)
        | ActorStop (caller, result, reply) ->
            CoreMailboxActors.dispatchActorStop context caller result reply
        | CancelActor (caller, focusId, reply) ->
            CoreMailboxActors.dispatchCancelActor context caller focusId reply
        | Login (caller, reply) ->
            CoreMailboxEvents.addCaller context caller
            reply.Reply(Ok ())
        | Logout (caller, reply) ->
            CoreMailboxEvents.removeCaller context caller
            reply.Reply(Ok ())
        | AdmitCaller (caller, reply) ->
            reply.Reply(CoreMailboxEvents.hasCaller context caller)
        | PostEvent (caller, event, reply) ->
            CoreMailboxActors.dispatchPostEvent context caller event reply

    let private dispatch (contex: MailboxContext) (msg: CoreMsg) : unit =
        try
            runMsg contex msg
        with ex ->
            let operation, context = CoreMailboxEvents.operationContext msg
            try
                contex.onError operation context ex
            with _ ->
                ()
            try
                CoreMailboxEvents.replyFailure (contex.formatError operation) msg
            with _ ->
                ()

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

    /// Core loop apply. ParseFinished writes Parsed only, then noteParsed
    /// so a blocked edit can catch up. It does not set Persisted.
    /// MarkUnparsed writes Unparsed only.
    /// SnapshotDone runs snapshot bookkeeping, then sets Persisted.
    let internal applyInMsg
        (persist: PersistHandlers)
        (msg: InMsg)
        : Result<unit, string> =
        match msg with
        | InMsg.ParseFinished nodeId ->
            match writeParseState persist nodeId ParseState.Parsed with
            | Error err -> Error err
            | Ok () ->
                persist.noteParsed nodeId
                Ok ()
        | InMsg.MarkUnparsed nodeId ->
            writeParseState persist nodeId ParseState.Unparsed
        | InMsg.SnapshotDone(nodeId, graph) ->
            persist.snapshotDone nodeId graph
            writeAxis persist (fun live ->
                GraphMutate.setPersistState
                    nodeId
                    PersistState.Persisted
                    live)

    let private reportInMsg (context: MailboxContext) (err: string) =
        context.onError "InMsg" err (exn err)

    let private dispatchItem
        (context: MailboxContext)
        (item: QueueSum)
        =
        match item with
        | QueueSum.Core msg -> dispatch context msg
        | QueueSum.In msg ->
            match applyInMsg context.persist msg with
            | Ok () -> ()
            | Error err -> reportInMsg context err

    let private failedPersist persist error : PersistHandlers = {
        getState = persist.getState
        getEventId = persist.getEventId
        getEventsSince = persist.getEventsSince
        getEventLog = persist.getEventLog
        appendEvent = fun _ -> Error error
        applyEvent = fun _ _ -> Error error
        replaceGraph = persist.replaceGraph
        snapshotDone = fun _ _ -> ()
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
        credentials
        (persist: PersistFilling)
        pool
        (rawPush: NodeId -> unit)
        : MailboxContext * (NodeId -> unit) =
        let seeded = persist.handlers
        let eventLog, handlers =
            match seedEventLog seeded with
            | Ok log -> log, seeded
            | Error error -> EventLog.empty, failedSeed seeded error
        let front = markUnparsedThenPush handlers rawPush
        let reported = pushReporting persist handlers rawPush
        { credentials = ref credentials
          persist = handlers
          pool = pool
          parsePush = front
          onError = persist.onError
          formatError = persist.formatError
          eventLog = ref eventLog
          coreChanges = ref None },
        reported

    let private started mailbox (context: MailboxContext) : Started =
        { processor = mailbox
          bindCoreChanges =
            fun make -> context.coreChanges.Value <- Some make }

    let private isStartupRead (msg: CoreMsg) =
        match msg with
        | GetState _
        | GetEventId _
        | GetEventsSince _
        | GetEventHistory _
        | EventsSince _ -> true
        | _ -> false

    let rec private pump
        (context: MailboxContext)
        (inbox: MailboxProcessor<QueueSum>)
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
        started (MailboxProcessor<QueueSum>.Start(pump context)) context

    let private startWithPrelude
        (context: MailboxContext)
        (until: Async<Result<unit, string>>)
        : Started =
        let untilTask = runUntil until
        let mailbox = MailboxProcessor<QueueSum>.Start(fun inbox ->
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
                                | QueueSum.Core msg when isStartupRead msg ->
                                    Some(async { dispatch context msg })
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
