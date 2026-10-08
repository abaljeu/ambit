namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared

[<RequireQualifiedAccess>]
module internal CoreMailboxBackend =

    type Ev = Gambol.Shared.Ev
    type EventLog = Gambol.Shared.EventLog
    module Ev = Gambol.Shared.Ev
    module EventLog = Gambol.Shared.EventLog

    /// Bound for non-file change work that may keep the mailbox context busy.
    /// Never wrap work-tree-gated file Persist: timeout abandonment would leave a late writer.
    [<Literal>]
    let ChangeProcessingTimeoutMs = 8000

    /// Runs a synchronous computation on a background Task, bounding wall-clock time. If the
    /// timeout elapses, the background Task is abandoned and may still complete later.
    /// Uses WaitAny (not Wait/Result) because WaitAny reports timeout vs settled without
    /// itself throwing on a faulted task; GetAwaiter().GetResult() then rethrows `f`'s
    /// original exception unwrapped (as if called synchronously), so the caller's existing
    /// exception handling is unaffected.
    let runBounded
        (timeoutMs: int)
        (f: unit -> Result<'a, string>)
        : Result<'a, string> =
        let task = Task.Run(fun () -> f ())
        let settledIndex = Task.WaitAny([| task :> Task |], timeoutMs)
        if settledIndex = -1 then
            Error "change processing timed out"
        else
            task.GetAwaiter().GetResult()

    let withAppliedOps
        (event: Gambol.Shared.Ev)
        (ops: Op list)
        : Gambol.Shared.Ev =
        match event.body with
        | EventBody.Change _ -> { event with body = EventBody.Change ops }
        | EventBody.Undo(target, _) ->
            { event with body = EventBody.Undo(target, ops) }
        | EventBody.Redo(target, _) ->
            { event with body = EventBody.Redo(target, ops) }
        | EventBody.ActorStart _ | EventBody.Cancel _
        | EventBody.ActorStop _ -> event

    let overlayFreshEvents
        (confirmations: Gambol.Shared.Ev list)
        (fresh: Gambol.Shared.Ev list)
        (stampOps: Op list)
        : Gambol.Shared.Ev list * Gambol.Shared.Ev list =
        let stamped = PersistStamp.appendToLastEvent fresh stampOps
        let stampedById =
            stamped
            |> List.map (fun event -> event.submissionId, event)
            |> Map.ofList
        let confirmed =
            confirmations
            |> List.map (fun event ->
                Map.tryFind event.submissionId stampedById
                |> Option.defaultValue event)
        stamped, confirmed

    let operationContext msg =
        match msg with
        | GetState _ -> "GetState", ""
        | GetEventId _ -> "GetEventId", ""
        | GetEventsSince (after, _) ->
            "GetEventsSince", $"after={after}"
        | GetEventHistory _ -> "GetEventHistory", ""
        | PostGraphOnly (_, event, _) ->
            let n = Ev.ops event |> Option.defaultValue [] |> List.length
            "PostGraphOnly", $"ops={n}"
        | StartActor _ -> "StartActor", ""
        | StartPeerActor (_, PeerActorName name, _, _) ->
            "StartPeerActor", name
        | StartLoadSaveCommand (_, path, PeerActorName name, _, _) ->
            "StartLoadSaveCommand", $"{path}:{name}"
        | RecordSearchStart _ -> "RecordSearchStart", ""
        | RecordSearchStop _ -> "RecordSearchStop", ""
        | Load (_, subject, _) ->
            "Load", $"{subject}"
        | ActorStop (_, result, _) ->
            match result with
            | ActorSucceeded -> "ActorStop", "ActorSucceeded"
            | ActorFailed _ -> "ActorStop", "ActorFailed"
            | ActorCancelled -> "ActorStop", "ActorCancelled"
        | CancelActor _ -> "CancelActor", ""
        | Login _ -> "Login", ""
        | Logout _ -> "Logout", ""
        | AdmitCaller _ -> "AdmitCaller", ""
        | PostEvent _ -> "PostEvent", ""
        | PostEvents _ -> "PostEvents", ""
        | EventsSince (after, _) -> "EventsSince", $"after={after}"

    let replyFailure error msg =
        match msg with
        | GetState reply -> reply.Reply(Error error)
        | GetEventId reply -> reply.Reply(Error error)
        | GetEventsSince (_, reply) -> reply.Reply(Error error)
        | GetEventHistory reply -> reply.Reply(EventLog.empty)
        | PostGraphOnly (_, _, reply) -> reply.Reply(Error error)
        | StartActor (_, _, reply) -> reply.Reply(Error error)
        | StartPeerActor (_, _, _, reply) -> reply.Reply(Error error)
        | StartLoadSaveCommand (_, _, _, _, reply) ->
            reply.Reply(Error error)
        | RecordSearchStart (_, _, reply) ->
            reply.Reply(Error error)
        | RecordSearchStop (_, _, reply) ->
            reply.Reply(Error error)
        | Load (_, _, reply) -> reply.Reply(Error error)
        | ActorStop (_, _, reply) -> reply.Reply(Error error)
        | CancelActor (_, _, reply) -> reply.Reply(Error error)
        | Login (_, reply) -> reply.Reply(Error error)
        | Logout (_, reply) -> reply.Reply(Error error)
        | AdmitCaller (_, reply) -> reply.Reply(false)
        | PostEvent (_, _, reply) -> reply.Reply(Error error)
        | PostEvents (_, _, reply) -> reply.Reply(Error error)
        | EventsSince (_, reply) -> reply.Reply(EventLog.empty)

    type Started = {
        processor: MailboxProcessor<QueueSum>
        bindCoreChanges: (Caller -> CoreChanges) -> unit
    }

    type MailboxContext = {
        credentials: CoreCredentials ref
        persist: PersistHandlers
        pool: CoreActorPool
        parsePush: NodeId -> unit
        onError: string -> string -> exn -> unit
        formatError: string -> string
        eventLog: EventLog ref
        coreChanges: (Caller -> CoreChanges) option ref
        isReady: unit -> bool
    }

    let private addCaller (context: MailboxContext) caller =
        context.credentials.Value <-
            CoreCredentials.add caller context.credentials.Value

    let private removeCaller (context: MailboxContext) caller =
        context.credentials.Value <-
            CoreCredentials.remove caller context.credentials.Value

    let private hasCaller (context: MailboxContext) caller =
        CoreCredentials.contains caller context.credentials.Value

    let private admitCaller (context: MailboxContext) (caller: Caller) =
        match caller.authority with
        | Authority name when String.IsNullOrWhiteSpace name ->
            Error CoreAuth.refuse
        | Authority "Actor" ->
            match CoreAuth.admit (context.pool.isLive caller.secret) with
            | Error err -> Error(CoreAdmissionError.text err)
            | Ok () -> Ok ()
        | _ ->
            match CoreAuth.admit (hasCaller context caller) with
            | Error err -> Error(CoreAdmissionError.text err)
            | Ok () -> Ok ()

    let private eventDispatchContext context : CoreEventDispatch.Context =
        { admit = admitCaller context
          persist = context.persist
          eventLog = context.eventLog }

    let private dispatchActorStartResult
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        start
        : unit =
        match admitCaller context caller with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            match context.coreChanges.Value with
            | None -> reply.Reply(Error "mailbox not initialized")
            | Some make ->
                let getState () =
                    match context.persist.getState () with
                    | Ok state -> state.graph
                    | Error _ -> Graph.create ()
                match start request getState with
                | Error err -> reply.Reply(Error err)
                | Ok None -> reply.Reply(Ok ())
                | Ok(Some secret) ->
                    match
                        CoreEventDispatch.actorStart
                            (eventDispatchContext context)
                            caller
                            request
                    with
                    | Error err ->
                        CoreActorPool.dropAndReply
                            context.pool secret reply err
                    | Ok () ->
                        context.pool.schedule secret (make caller)
                        reply.Reply(Ok ())

    let private dispatchStartActor context caller request reply =
        dispatchActorStartResult
            context
            caller
            request
            reply
            (fun start getState ->
                context.pool.startActor start getState
                |> Result.map Some)

    let private dispatchStartPeerActor
        context caller peerName request reply =
        dispatchActorStartResult
            context
            caller
            request
            reply
            (fun start getState ->
                context.pool.startPeerActor peerName start getState
                |> Result.map Some)

    let private dispatchStartLoadSaveCommand
        context
        caller
        path
        peerName
        (request: LoadSaveCommandRequest)
        reply
        =
        dispatchActorStartResult
            context
            caller
            request.start
            reply
            (fun _ getState ->
                CoreActorPool.startLoadSaveCommand
                    context.pool
                    path
                    peerName
                    request
                    getState)

    let private dispatchActorStop
        (context: MailboxContext)
        (caller: Caller)
        (result: ActorResult)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match admitCaller context caller with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            match caller.authority with
            | Authority "Actor" ->
                match context.pool.getFocusId caller.secret with
                | None -> reply.Reply(Ok ())
                | Some focusId ->
                    match
                        CoreEventDispatch.actorStop
                            (eventDispatchContext context)
                            caller
                            focusId
                            result
                    with
                    | Error err -> reply.Reply(Error err)
                    | Ok () ->
                        reply.Reply(
                            context.pool.finish caller.secret result)
            | _ ->
                reply.Reply(Error CoreAuth.refuse)

    let private dispatchCancelActor
        (context: MailboxContext)
        (caller: Caller)
        (focusId: NodeId)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match admitCaller context caller with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            match context.pool.trySecretForFocus focusId with
            | None -> reply.Reply(Ok ())
            | Some secret ->
                match
                    CoreEventDispatch.actorStop
                        (eventDispatchContext context)
                        caller
                        focusId
                        ActorCancelled
                with
                | Error err -> reply.Reply(Error err)
                | Ok () ->
                    reply.Reply(
                        context.pool.finish secret ActorCancelled)

    /// Graph-only: same Ev flow as postEvent, but skips file persistence.
    let private dispatchPostGraphOnly
        (context: MailboxContext)
        (caller: Caller)
        (event: Ev)
        (reply: AsyncReplyChannel<Result<CoreChangesAccepted, string>>)
        : unit =
        match CoreEventDispatch.postEvent (eventDispatchContext context) caller event true with
        | Error err -> reply.Reply(Error err)
        | Ok (_, Some accepted) -> reply.Reply(Ok accepted)
        | Ok (_, None) ->
            match context.persist.getEventId () with
            | Error err -> reply.Reply(Error err)
            | Ok eventId ->
                reply.Reply(
                    Ok(
                        CoreChanges.accepted
                            eventId
                            true
                            []
                            false
                            None))

    let private dispatchPostEvent
        (context: MailboxContext)
        (caller: Caller)
        (event: Ev)
        (reply:
            AsyncReplyChannel<
                Result<Ev * CoreChangesAccepted option, string>>)
        : unit =
        CoreEventDispatch.postEvent (eventDispatchContext context) caller event false
        |> reply.Reply

    let private storedSubmission context submissionId =
        context.eventLog.Value.events
        |> List.tryFind (fun event ->
            event.submissionId = submissionId)

    let private currentGraph context =
        match context.persist.getState () with
        | Ok state -> state.graph
        | Error _ -> Graph.create ()

    let private launchPosted
        context caller (request: ActorStart) submissionId =
        match storedSubmission context submissionId with
        | Some existing -> Ok existing
        | None ->
            match context.coreChanges.Value with
            | None -> Error "mailbox not initialized"
            | Some make ->
                match
                    context.pool.startActor
                        request
                        (fun () -> currentGraph context)
                with
                | Error err -> Error err
                | Ok secret ->
                    match
                        CoreEventDispatch.commitClientBody
                            (eventDispatchContext context)
                            caller
                            submissionId
                            (EventBody.ActorStart request)
                    with
                    | Error err ->
                        context.pool.drop secret
                        Error err
                    | Ok stored ->
                        context.pool.schedule secret (make caller)
                        Ok stored

    let private cancelPosted context caller focusId submissionId =
        match storedSubmission context submissionId with
        | Some existing -> Ok [ existing ]
        | None ->
            match context.pool.trySecretForFocus focusId with
            | None -> Ok []
            | Some secret ->
                match
                    CoreEventDispatch.commitClientBody
                        (eventDispatchContext context)
                        caller
                        submissionId
                        (EventBody.ActorStop(
                            focusId,
                            ActorCancelled))
                with
                | Error err -> Error err
                | Ok stored ->
                    match context.pool.finish secret ActorCancelled with
                    | Error err -> Error err
                    | Ok () -> Ok [ stored ]

    let private postedHost context caller : CorePostedList.Host =
        { admit = admitCaller context
          postChange =
            fun event ->
                CoreEventDispatch.postEvent
                    (eventDispatchContext context)
                    caller
                    event
                    false
          launch =
            fun event ->
                match event.body with
                | EventBody.ActorStart request ->
                    launchPosted
                        context caller request event.submissionId
                | _ -> Error "ActorStart is required"
          cancel =
            fun event ->
                match event.body with
                | EventBody.Cancel focusId ->
                    cancelPosted
                        context caller focusId event.submissionId
                | _ -> Error "Cancel is required"
          eventId = context.persist.getEventId
          isReady = context.isReady }

    let private dispatchPostEvents
        context caller events
        (reply: AsyncReplyChannel<Result<CoreChangesAccepted, string>>)
        =
        CorePostedList.apply (postedHost context caller) caller events
        |> reply.Reply

    let private isFileSubject (graph: Graph) (subject: NodeId) =
        match Map.tryFind subject graph.nodes with
        | Some { kind = Special File } -> Ok ()
        | Some _ -> Error "Load subject is not a File node"
        | None -> Error "Load subject not found"

    let private dispatchRecordSearchStart
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match admitCaller context caller with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            CoreEventDispatch.actorStart
                (eventDispatchContext context)
                caller
                request
            |> reply.Reply

    /// The queue puts ActorStop on the event source. The id is the root.
    let private dispatchRecordSearchStop
        (context: MailboxContext)
        (caller: Caller)
        (rootId: NodeId)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match admitCaller context caller with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            CoreEventDispatch.actorStop
                (eventDispatchContext context)
                caller
                rootId
                ActorSucceeded
            |> reply.Reply

    let private dispatchLoad
        (context: MailboxContext)
        (caller: Caller)
        (subject: NodeId)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match admitCaller context caller with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            match context.persist.getState () with
            | Error err -> reply.Reply(Error err)
            | Ok state ->
                match isFileSubject state.graph subject with
                | Error err -> reply.Reply(Error err)
                | Ok () ->
                    context.parsePush subject
                    reply.Reply(Ok ())

    let private runMsg (context: MailboxContext) (msg: CoreMsg) =
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
        | PostGraphOnly (caller, event, reply) ->
            dispatchPostGraphOnly context caller event reply
        | StartActor (caller, request, reply) ->
            dispatchStartActor context caller request reply
        | StartPeerActor (caller, peerName, request, reply) ->
            dispatchStartPeerActor
                context caller peerName request reply
        | StartLoadSaveCommand (caller, path, peerName, request, reply) ->
            dispatchStartLoadSaveCommand
                context caller path peerName request reply
        | RecordSearchStart (caller, request, reply) ->
            dispatchRecordSearchStart context caller request reply
        | RecordSearchStop (caller, rootId, reply) ->
            dispatchRecordSearchStop context caller rootId reply
        | Load (caller, subject, reply) ->
            dispatchLoad context caller subject reply
        | ActorStop (caller, result, reply) ->
            dispatchActorStop context caller result reply
        | CancelActor (caller, focusId, reply) ->
            dispatchCancelActor context caller focusId reply
        | Login (caller, reply) ->
            addCaller context caller
            reply.Reply(Ok ())
        | Logout (caller, reply) ->
            removeCaller context caller
            reply.Reply(Ok ())
        | AdmitCaller (caller, reply) ->
            reply.Reply(hasCaller context caller)
        | PostEvent (caller, event, reply) ->
            dispatchPostEvent context caller event reply
        | PostEvents (caller, events, reply) ->
            dispatchPostEvents context caller events reply
        | EventsSince (after, reply) ->
            reply.Reply(EventLog.since after context.eventLog.Value)

    let private dispatch (contex: MailboxContext) (msg: CoreMsg) : unit =
        try
            runMsg contex msg
        with ex ->
            let operation, context = operationContext msg
            try
                contex.onError operation context ex
            with _ ->
                ()
            try
                replyFailure (contex.formatError operation) msg
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

    let makeMailBox
        credentials
        (persist: PersistFilling)
        pool
        parsePush
        : MailboxContext =
        let seeded = persist.handlers
        let eventLog, handlers =
            match seedEventLog seeded with
            | Ok log -> log, seeded
            | Error error -> EventLog.empty, failedSeed seeded error
        { credentials = ref credentials
          persist = handlers
          pool = pool
          parsePush = parsePush
          onError = persist.onError
          formatError = persist.formatError
          eventLog = ref eventLog
          coreChanges = ref None
          isReady = persist.isReady }

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
