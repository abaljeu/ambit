namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared

type CoreMsg =
    | GetState of AsyncReplyChannel<Result<State, string>>
    | GetRevision of AsyncReplyChannel<Result<Revision, string>>
    | GetChangesSince of
        after: Revision * AsyncReplyChannel<Result<Change list, string>>
    | PostChange of
        caller: Caller *
        changes: Change list *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
    | PostGraphOnlyChange of
        changes: Change list *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
    | SnapshotDone of graph: Graph option
    | StartActor of
        caller: Caller *
        request: StartActorRequest *
        AsyncReplyChannel<Result<unit, string>>
    | ActorStop of
        caller: Caller *
        result: ActorResult *
        AsyncReplyChannel<Result<unit, string>>

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
        | PostChange (_, changes, _) ->
            "PostChange", $"changeCount={changes.Length}"
        | PostGraphOnlyChange (changes, _) ->
            "PostGraphOnlyChange", $"changeCount={changes.Length}"
        | SnapshotDone _ -> "SnapshotDone", ""
        | StartActor _ -> "StartActor", ""
        | ActorStop (_, result, _) ->
            match result with
            | ActorSucceeded -> "ActorStop", "ActorSucceeded"

    let replyFailure error msg =
        match msg with
        | GetState reply -> reply.Reply(Error error)
        | GetRevision reply -> reply.Reply(Error error)
        | GetChangesSince (_, reply) -> reply.Reply(Error error)
        | PostChange (_, _, reply) -> reply.Reply(Error error)
        | PostGraphOnlyChange (_, reply) -> reply.Reply(Error error)
        | SnapshotDone _ -> ()
        | StartActor (_, _, reply) -> reply.Reply(Error error)
        | ActorStop (_, _, reply) -> reply.Reply(Error error)

    let private admitSecret
        (credentials: CoreCredentials)
        (secret: Credential)
        : Result<unit, string> =
        let live =
            credentials.contains secret
            |> Async.RunSynchronously
        match CoreAuth.admit live with
        | Error err -> Error(CoreAdmissionError.text err)
        | Ok () -> Ok ()

    let private admitCaller
        (credentials: CoreCredentials)
        (caller: Caller)
        : Result<unit, string> =
        match caller.authority, admitSecret credentials caller.secret with
        | Authority name, _ when String.IsNullOrWhiteSpace name ->
            Error CoreAuth.refuse
        | _, Error err -> Error err
        | _, Ok () -> Ok ()

    type private Loop = {
        credentials: CoreCredentials
        persist: PersistHandlers
        pool: CoreActorPool
        onError: string -> string -> exn -> unit
        formatError: string -> string
    }

    let private admitActorPost (loop: Loop) (caller: Caller) =
        match admitCaller loop.credentials caller with
        | Error err -> Error err
        | Ok () ->
            match caller.authority with
            | Authority "Actor" ->
                match CoreAuth.admit (loop.pool.isLive caller.secret) with
                | Error err -> Error(CoreAdmissionError.text err)
                | Ok () -> Ok ()
            | _ -> Ok ()

    let private dispatchStartActor
        (loop: Loop)
        (caller: Caller)
        (request: StartActorRequest)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match admitCaller loop.credentials caller with
        | Error err -> reply.Reply(Error err)
        | Ok () -> reply.Reply(loop.pool.startActor request)

    let private dispatchActorStop
        (loop: Loop)
        (caller: Caller)
        (result: ActorResult)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match admitCaller loop.credentials caller with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            match CoreAuth.admit (loop.pool.isLive caller.secret) with
            | Error err ->
                reply.Reply(Error(CoreAdmissionError.text err))
            | Ok () ->
                reply.Reply(loop.pool.finish caller.secret result)

    let private dispatchPostChange
        (loop: Loop)
        (caller: Caller)
        (changes: Change list)
        (reply: AsyncReplyChannel<Result<CoreChangesAccepted, string>>)
        : unit =
        match admitActorPost loop caller with
        | Error err -> reply.Reply(Error err)
        | Ok () -> reply.Reply(loop.persist.postChange changes)

    let private runMsg (loop: Loop) (msg: CoreMsg) =
        match msg with
        | GetState reply ->
            match loop.persist.getState () with
            | Error err -> reply.Reply(Error err)
            | Ok state ->
                let graph =
                    GraphSpan.withLockPresent
                        (loop.pool.liveFocusIds ())
                        state.graph
                reply.Reply(Ok { state with graph = graph })
        | GetRevision reply -> reply.Reply(loop.persist.getRevision ())
        | GetChangesSince (after, reply) ->
            reply.Reply(loop.persist.getChangesSince after)
        | PostChange (caller, changes, reply) ->
            dispatchPostChange loop caller changes reply
        | PostGraphOnlyChange (changes, reply) ->
            reply.Reply(loop.persist.postGraphOnlyChange changes)
        | SnapshotDone graph -> loop.persist.snapshotDone graph
        | StartActor (caller, request, reply) ->
            dispatchStartActor loop caller request reply
        | ActorStop (caller, result, reply) ->
            dispatchActorStop loop caller result reply

    let private dispatch (loop: Loop) (msg: CoreMsg) : unit =
        try
            runMsg loop msg
        with ex ->
            let operation, context = operationContext msg
            try
                loop.onError operation context ex
            with _ ->
                ()
            try
                replyFailure (loop.formatError operation) msg
            with _ ->
                ()

    let private makeLoop credentials persist pool onError formatError : Loop =
        { credentials = credentials
          persist = persist
          pool = pool
          onError = onError
          formatError = formatError }

    let start
        (credentials: CoreCredentials)
        (persist: PersistHandlers)
        (pool: CoreActorPool)
        (onError: string -> string -> exn -> unit)
        (formatError: string -> string)
        : MailboxProcessor<CoreMsg> =
        let loop = makeLoop credentials persist pool onError formatError
        MailboxProcessor<CoreMsg>.Start(fun inbox ->
            let rec pump () = async {
                let! msg = inbox.Receive()
                dispatch loop msg
                return! pump ()
            }
            pump ()
        )

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

        let loop = makeLoop credentials persist pool onError formatError
        let failedHandlers error : PersistHandlers = {
            getState = persist.getState
            getRevision = persist.getRevision
            getChangesSince = persist.getChangesSince
            postChange = fun _ -> Error error
            postGraphOnlyChange = fun _ -> Error error
            snapshotDone = fun _ -> ()
        }

        MailboxProcessor<CoreMsg>.Start(fun inbox ->
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
                                | GetChangesSince _ ->
                                    Some(async { dispatch loop msg })
                                | _ -> None),
                            timeout = 20)
                    return! startupLoop ()
            }
            and normalLoop () = async {
                let! msg = inbox.Receive()
                dispatch loop msg
                return! normalLoop ()
            }
            and failedLoop error = async {
                let! msg = inbox.Receive()
                let failed = failedHandlers error
                dispatch { loop with persist = failed } msg
                return! failedLoop error
            }

            startupLoop ()
        )
