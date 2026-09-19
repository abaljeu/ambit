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

    /// Bound on wall-clock time for a single change's persist step (disk write via
    /// DocumentWarm/CStyleReconcile). That reconcile path is a known-slow/hanging
    /// algorithm; this timeout exists to keep the mailbox context responsive, not to fix it.
    [<Literal>]
    let ChangeProcessingTimeoutMs = 8000

    /// Runs a synchronous computation on a background Task, bounding wall-clock time so
    /// a pathologically slow computation can never wedge the caller's mailbox context. If the
    /// timeout elapses, the background Task is abandoned (fire-and-forget): it may still run
    /// to completion later and write to disk concurrently with subsequently accepted changes.
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
        | EventBody.ActorStart _
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
        | SnapshotDone _ -> "SnapshotDone", ""
        | StartActor _ -> "StartActor", ""
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
        | EventsSince (after, _) -> "EventsSince", $"after={after}"

    let replyFailure error msg =
        match msg with
        | GetState reply -> reply.Reply(Error error)
        | GetEventId reply -> reply.Reply(Error error)
        | GetEventsSince (_, reply) -> reply.Reply(Error error)
        | GetEventHistory reply -> reply.Reply(EventLog.empty)
        | PostGraphOnly (_, _, reply) -> reply.Reply(Error error)
        | SnapshotDone _ -> ()
        | StartActor (_, _, reply) -> reply.Reply(Error error)
        | ActorStop (_, _, reply) -> reply.Reply(Error error)
        | CancelActor (_, _, reply) -> reply.Reply(Error error)
        | Login (_, reply) -> reply.Reply(Error error)
        | Logout (_, reply) -> reply.Reply(Error error)
        | AdmitCaller (_, reply) -> reply.Reply(false)
        | PostEvent (_, _, reply) -> reply.Reply(Error error)
        | EventsSince (_, reply) -> reply.Reply(EventLog.empty)

    type Started = {
        processor: MailboxProcessor<CoreMsg>
        bindCoreChanges: (Caller -> CoreChanges) -> unit
    }

    type MailboxContext = {
        credentials: CoreCredentials ref
        persist: PersistHandlers
        pool: CoreActorPool
        onError: string -> string -> exn -> unit
        formatError: string -> string
        eventLog: EventLog ref
        coreChanges: (Caller -> CoreChanges) option ref
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

    let private dispatchStartActor
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (reply: AsyncReplyChannel<Result<unit, string>>)
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
                match context.pool.startActor request getState with
                | Error err -> reply.Reply(Error err)
                | Ok secret ->
                    match
                        CoreEventDispatch.actorStart
                            (eventDispatchContext context)
                            caller
                            request
                    with
                    | Error err -> reply.Reply(Error err)
                    | Ok () ->
                        context.pool.schedule secret (make caller)
                        reply.Reply(Ok ())

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
                let focusId =
                    context.pool.getFocusId caller.secret
                    |> Option.defaultValue Graph.rootId
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
            dispatchPostGraphOnly
                context
                caller
                event
                reply
        | SnapshotDone graph -> context.persist.snapshotDone graph
        | StartActor (caller, request, reply) ->
            dispatchStartActor context caller request reply
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

    let private failedPersist persist error : PersistHandlers = {
        getState = persist.getState
        getEventId = persist.getEventId
        getEventsSince = persist.getEventsSince
        getEventLog = persist.getEventLog
        appendEvent = fun _ -> Error error
        applyEvent = fun _ _ -> Error error
        snapshotDone = fun _ -> ()
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

    let makeMailBox credentials persist pool onError formatError : MailboxContext =
        let eventLog, handlers =
            match seedEventLog persist with
            | Ok log -> log, persist
            | Error error -> EventLog.empty, failedSeed persist error
        { credentials = ref credentials
          persist = handlers
          pool = pool
          onError = onError
          formatError = formatError
          eventLog = ref eventLog
          coreChanges = ref None }

    let private started mailbox (context: MailboxContext) : Started =
        { processor = mailbox
          bindCoreChanges =
            fun make -> context.coreChanges.Value <- Some make }

    let rec private pump
        (context: MailboxContext)
        (inbox: MailboxProcessor<CoreMsg>)
        =
        async {
            let! msg = inbox.Receive()
            dispatch context msg
            return! pump context inbox
        }

    let private runUntil (until: Async<Result<unit, string>>) =
        Task.Run(fun () ->
            try
                until |> Async.RunSynchronously
            with ex ->
                Error $"Startup prelude failed: {ex.Message}")

    let start (context: MailboxContext) : Started =
        started (MailboxProcessor<CoreMsg>.Start(pump context)) context

    let startWithPrelude
        (context: MailboxContext)
        (until: Async<Result<unit, string>>)
        : Started =
        let untilTask = runUntil until
        let mailbox = MailboxProcessor<CoreMsg>.Start(fun inbox ->
            let rec startupLoop () = async {
                if untilTask.IsCompleted then
                    match untilTask.GetAwaiter().GetResult() with
                    | Ok () -> return! pump context inbox
                    | Error error -> return! failedLoop error
                else
                    let! _ =
                        inbox.TryScan(
                            (fun msg ->
                                match msg with
                                | GetState _
                                | GetEventId _
                                | GetEventsSince _
                                | GetEventHistory _
                                | EventsSince _ ->
                                    Some(async { dispatch context msg })
                                | _ -> None),
                            timeout = 20)
                    return! startupLoop ()
            }
            and failedLoop error = async {
                let! msg = inbox.Receive()
                let failed = failedPersist context.persist error
                dispatch { context with persist = failed } msg
                return! failedLoop error
            }
            startupLoop ()
        )
        started mailbox context
