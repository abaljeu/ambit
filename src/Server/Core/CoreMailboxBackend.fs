namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared

type internal CoreMsg =
    | GetState of AsyncReplyChannel<Result<State, string>>
    | GetRevision of AsyncReplyChannel<Result<Revision, string>>
    | GetChangesSince of
        after: Revision * AsyncReplyChannel<Result<Change list, string>>
    | GetEventHistory of AsyncReplyChannel<HistoryEvent list>
    | PostChange of
        caller: Caller *
        changes: Change list *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
    | PostGraphOnlyChange of
        caller: Caller *
        changes: Change list *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
    | Logout of
        Caller *
        AsyncReplyChannel<Result<unit, string>>
    | SnapshotDone of graph: Graph option
    | StartActor of
        caller: Caller *
        request: StartActorRequest *
        AsyncReplyChannel<Result<unit, string>>
    | ActorStop of
        caller: Caller *
        result: ActorResult *
        AsyncReplyChannel<Result<unit, string>>
    | Login of
        Caller *
        AsyncReplyChannel<Result<unit, string>>
    | AdmitCaller of Caller * AsyncReplyChannel<bool>

type PersistHandlers = {
    getState: unit -> Result<State, string>
    getRevision: unit -> Result<Revision, string>
    getChangesSince: Revision -> Result<Change list, string>
    postChange:
        Change list -> Result<CoreChangesAccepted, string>
    postGraphOnlyChange:
        Change list -> Result<CoreChangesAccepted, string>
    snapshotDone: Graph option -> unit
}

[<RequireQualifiedAccess>]
module internal CoreMailboxBackend =

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

    let overlayFresh confirmations fresh stampOps =
        let stamped = PersistStamp.appendToLast fresh stampOps
        let stampedById =
            stamped
            |> List.map (fun change -> change.changeId, change)
            |> Map.ofList
        let confirmed =
            confirmations
            |> List.map (fun change ->
                Map.tryFind change.changeId stampedById
                |> Option.defaultValue change)
        stamped, confirmed

    let operationContext msg =
        match msg with
        | GetState _ -> "GetState", ""
        | GetRevision _ -> "GetRevision", ""
        | GetChangesSince (after, _) ->
            "GetChangesSince", $"after={after}"
        | GetEventHistory _ -> "GetEventHistory", ""
        | PostChange (_, changes, _) ->
            "PostChange", $"changeCount={changes.Length}"
        | PostGraphOnlyChange (_, changes, _) ->
            "PostGraphOnlyChange", $"changeCount={changes.Length}"
        | SnapshotDone _ -> "SnapshotDone", ""
        | StartActor _ -> "StartActor", ""
        | ActorStop (_, result, _) ->
            match result with
            | ActorSucceeded -> "ActorStop", "ActorSucceeded"
            | ActorFailed -> "ActorStop", "ActorFailed"
        | Login _ -> "Login", ""
        | Logout _ -> "Logout", ""
        | AdmitCaller _ -> "AdmitCaller", ""

    let replyFailure error msg =
        match msg with
        | GetState reply -> reply.Reply(Error error)
        | GetRevision reply -> reply.Reply(Error error)
        | GetChangesSince (_, reply) -> reply.Reply(Error error)
        | GetEventHistory reply -> reply.Reply([])
        | PostChange (_, _, reply) -> reply.Reply(Error error)
        | PostGraphOnlyChange (_, _, reply) -> reply.Reply(Error error)
        | SnapshotDone _ -> ()
        | StartActor (_, _, reply) -> reply.Reply(Error error)
        | ActorStop (_, _, reply) -> reply.Reply(Error error)
        | Login (_, reply) -> reply.Reply(Error error)
        | Logout (_, reply) -> reply.Reply(Error error)
        | AdmitCaller (_, reply) -> reply.Reply(false)

    type private MailboxContext = {
        credentials: CoreCredentials ref
        persist: PersistHandlers
        pool: CoreActorPool
        onError: string -> string -> exn -> unit
        formatError: string -> string
        mailboxHistory: History ref
        mailbox: MailboxProcessor<CoreMsg> option ref
    }

    let private addCaller (context: MailboxContext) caller =
        context.credentials.Value <-
            CoreCredentials.add caller context.credentials.Value

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

    let private recordActorStarted (context: MailboxContext) focusId =
        let history = context.mailboxHistory.Value
        let event =
            ActorEvent(history.nextId, ActorStarted(focusId, "Actor"))
        context.mailboxHistory.Value <-
            { history with
                past = history.past @ [ event ]
                nextId = history.nextId + 1 }

    let private recordActorFinished (context: MailboxContext) focusId =
        let history = context.mailboxHistory.Value
        let event = ActorEvent(history.nextId, ActorFinished(focusId))
        context.mailboxHistory.Value <-
            { history with
                past = history.past @ [ event ]
                nextId = history.nextId + 1 }

    let private dispatchStartActor
        (context: MailboxContext)
        (caller: Caller)
        (request: StartActorRequest)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match admitCaller context caller with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            match context.mailbox.Value with
            | None -> reply.Reply(Error "mailbox not initialized")
            | Some mailbox ->
                let getState () =
                    match context.persist.getState () with
                    | Ok state -> state.graph
                    | Error _ -> Graph.create ()
                let rec makeCoreChanges (c: Caller) : CoreChanges =
                    { getState = fun () -> mailbox.PostAndAsyncReply GetState
                      getRevision = fun () ->
                        async {
                            let! result = mailbox.PostAndAsyncReply GetRevision
                            return match result with
                                   | Ok rev -> rev
                                   | Error _ -> Revision 0
                        }
                      getChangesSince = fun after ->
                        async {
                            let! result =
                                mailbox.PostAndAsyncReply(fun reply ->
                                    GetChangesSince(after, reply))
                            return match result with
                                   | Ok changes -> changes
                                   | Error _ -> []
                        }
                      isReady = fun () -> true
                      postChange = fun changes ->
                        mailbox.PostAndAsyncReply(fun reply ->
                            PostChange(c, changes, reply))
                      postGraphOnlyChange = fun changes ->
                        mailbox.PostAndAsyncReply(fun reply ->
                            PostGraphOnlyChange(c, changes, reply))
                      actorStop = fun result ->
                        mailbox.PostAndAsyncReply(fun reply ->
                            ActorStop(c, result, reply))
                      asCaller = makeCoreChanges }
                let coreChanges = makeCoreChanges caller
                match context.pool.startActor request getState with
                | Error err -> reply.Reply(Error err)
                | Ok started ->
                    recordActorStarted context started.focusId
                    context.pool.schedule started.secret coreChanges
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
            match CoreAuth.admit (context.pool.isLive caller.secret) with
            | Error err ->
                reply.Reply(Error(CoreAdmissionError.text err))
            | Ok () ->
                let focusId =
                    context.pool.getFocusId caller.secret
                    |> Option.defaultValue Graph.rootId
                recordActorFinished context focusId
                reply.Reply(context.pool.finish caller.secret result)

    let private appendChangeEvents (context: MailboxContext) (changes: Change list) : unit =
        changes
        |> List.iter (fun change ->
            let event = ChangeEvent(change)
            context.mailboxHistory.Value <- {
                context.mailboxHistory.Value with
                    past = context.mailboxHistory.Value.past @ [ event ]
            })

    let private dispatchPostChange
        (persistPost:
            Change list -> Result<CoreChangesAccepted, string>)
        (context: MailboxContext)
        (caller: Caller)
        (changes: Change list)
        (reply: AsyncReplyChannel<Result<CoreChangesAccepted, string>>)
        : unit =
        match admitCaller context caller with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            match persistPost changes with
            | Error _ as err -> reply.Reply(err)
            | Ok accepted as result ->
                appendChangeEvents context accepted.changes
                reply.Reply(result)

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
        | GetRevision reply -> reply.Reply(context.persist.getRevision ())
        | GetChangesSince (after, reply) ->
            reply.Reply(context.persist.getChangesSince after)
        | GetEventHistory reply ->
            reply.Reply(context.mailboxHistory.Value.past)
        | PostChange (caller, changes, reply) ->
            dispatchPostChange
                context.persist.postChange
                context
                caller
                changes
                reply
        | PostGraphOnlyChange (caller, changes, reply) ->
            dispatchPostChange
                context.persist.postGraphOnlyChange
                context
                caller
                changes
                reply
        | SnapshotDone graph -> context.persist.snapshotDone graph
        | StartActor (caller, request, reply) ->
            dispatchStartActor context caller request reply
        | ActorStop (caller, result, reply) ->
            dispatchActorStop context caller result reply
        | Login (caller, reply) ->
            addCaller context caller
            reply.Reply(Ok ())
        | Logout (caller, reply) ->
            context.credentials.Value <-
                CoreCredentials.remove caller context.credentials.Value
            reply.Reply(Ok ())
        | AdmitCaller (caller, reply) ->
            reply.Reply(hasCaller context caller)

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

    let private makeMailBox credentials persist pool onError formatError : MailboxContext =
        { credentials = ref credentials
          persist = persist
          pool = pool
          onError = onError
          formatError = formatError
          mailboxHistory = ref History.empty
          mailbox = ref None }

    let start
        (credentials: CoreCredentials)
        (persist: PersistHandlers)
        (pool: CoreActorPool)
        (onError: string -> string -> exn -> unit)
        (formatError: string -> string)
        : MailboxProcessor<CoreMsg> =
        let context = makeMailBox credentials persist pool onError formatError
        let mailbox = MailboxProcessor<CoreMsg>.Start(fun inbox ->
            let rec pump () = async {
                let! msg = inbox.Receive()
                dispatch context msg
                return! pump ()
            }
            pump ()
        )
        context.mailbox.Value <- Some mailbox
        mailbox

    let startWithPrelude
        (credentials: CoreCredentials)
        (persist: PersistHandlers)
        (pool: CoreActorPool)
        (onError: string -> string -> exn -> unit)
        (formatError: string -> string)
        (until: Async<Result<unit, string>>)
        : MailboxProcessor<CoreMsg> =
        let untilTask =
            Task.Run(fun () ->
                try
                    until |> Async.RunSynchronously
                with ex ->
                    Error $"Startup prelude failed: {ex.Message}")

        let context = makeMailBox credentials persist pool onError formatError
        let failedHandlers error : PersistHandlers = {
            getState = persist.getState
            getRevision = persist.getRevision
            getChangesSince = persist.getChangesSince
            postChange = fun _ -> Error error
            postGraphOnlyChange = fun _ -> Error error
            snapshotDone = fun _ -> ()
        }

        let mailbox = MailboxProcessor<CoreMsg>.Start(fun inbox ->
            let rec startupLoop () = async {
                if untilTask.IsCompleted then
                    match untilTask.GetAwaiter().GetResult() with
                    | Ok () ->
                        return! normalLoop ()
                    | Error error ->
                        return! failedLoop error
                else
                    let! _ =
                        inbox.TryScan(
                            (fun msg ->
                                match msg with
                                | GetState _
                                | GetRevision _
                                | GetChangesSince _
                                | GetEventHistory _ ->
                                    Some(async { dispatch context msg })
                                | _ -> None),
                            timeout = 20)
                    return! startupLoop ()
            }
            and normalLoop () = async {
                let! msg = inbox.Receive()
                dispatch context msg
                return! normalLoop ()
            }
            and failedLoop error = async {
                let! msg = inbox.Receive()
                let failed = failedHandlers error
                dispatch { context with persist = failed } msg
                return! failedLoop error
            }

            startupLoop ()
        )
        context.mailbox.Value <- Some mailbox
        mailbox
