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
        authority: Authority *
        secret: Credential *
        changes: Change list *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
    | PostGraphOnlyChange of
        changes: Change list *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
    | SnapshotDone of graph: Graph option
    | StartActor of
        authority: Authority *
        secret: Credential *
        request: StartActorRequest *
        AsyncReplyChannel<Result<unit, string>>
    | ActorStop of
        authority: Authority *
        secret: Credential *
        result: ActorResult *
        AsyncReplyChannel<Result<unit, string>>

/// Actor cases on the one CoreMsg loop. Not PersistHandlers.
type ActorMailboxHandlers = {
    startActor: StartActorRequest -> Async<Result<unit, string>>
    isLive: Credential -> bool
    actorStop: Credential -> ActorResult -> Result<unit, string>
}

[<RequireQualifiedAccess>]
module ActorMailboxHandlers =

    /// Persist-only filling: Actor live-table check is pass-through.
    let noop: ActorMailboxHandlers = {
        startActor = fun _ -> async.Return (Ok ())
        isLive = fun _ -> true
        actorStop = fun _ _ -> Ok ()
    }

    let fromPool (pool: CoreActorPool) : ActorMailboxHandlers = {
        startActor = pool.startActor
        isLive = pool.isLive
        actorStop = pool.finish
    }

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
        | PostChange (_, _, changes, _) ->
            "PostChange", $"changeCount={changes.Length}"
        | PostGraphOnlyChange (changes, _) ->
            "PostGraphOnlyChange", $"changeCount={changes.Length}"
        | SnapshotDone _ -> "SnapshotDone", ""
        | StartActor _ -> "StartActor", ""
        | ActorStop (_, _, result, _) ->
            match result with
            | ActorSucceeded -> "ActorStop", "ActorSucceeded"

    let replyFailure error msg =
        match msg with
        | GetState reply -> reply.Reply(Error error)
        | GetRevision reply -> reply.Reply(Error error)
        | GetChangesSince (_, reply) -> reply.Reply(Error error)
        | PostChange (_, _, _, reply) -> reply.Reply(Error error)
        | PostGraphOnlyChange (_, reply) -> reply.Reply(Error error)
        | SnapshotDone _ -> ()
        | StartActor (_, _, _, reply) -> reply.Reply(Error error)
        | ActorStop (_, _, _, reply) -> reply.Reply(Error error)

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
        (authority: Authority)
        (secret: Credential)
        : Result<unit, string> =
        match authority, admitSecret credentials secret with
        | Authority name, _ when String.IsNullOrWhiteSpace name ->
            Error CoreAuth.refuse
        | _, Error err -> Error err
        | _, Ok () -> Ok ()

    let private admitActorPost
        (credentials: CoreCredentials)
        (actors: ActorMailboxHandlers)
        (authority: Authority)
        (secret: Credential)
        : Result<unit, string> =
        match admitCaller credentials authority secret with
        | Error err -> Error err
        | Ok () ->
            match authority with
            | Authority "Actor" ->
                match CoreAuth.admit (actors.isLive secret) with
                | Error err -> Error(CoreAdmissionError.text err)
                | Ok () -> Ok ()
            | _ -> Ok ()

    let private dispatchStartActor
        (credentials: CoreCredentials)
        (actors: ActorMailboxHandlers)
        (authority: Authority)
        (secret: Credential)
        (request: StartActorRequest)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match admitCaller credentials authority secret with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            let result =
                actors.startActor request |> Async.RunSynchronously
            reply.Reply result

    let private dispatchActorStop
        (credentials: CoreCredentials)
        (actors: ActorMailboxHandlers)
        (authority: Authority)
        (secret: Credential)
        (result: ActorResult)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match admitCaller credentials authority secret with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            match CoreAuth.admit (actors.isLive secret) with
            | Error err ->
                reply.Reply(Error(CoreAdmissionError.text err))
            | Ok () ->
                reply.Reply(actors.actorStop secret result)

    let private dispatchPostChange
        (credentials: CoreCredentials)
        (handlers: PersistHandlers)
        (actors: ActorMailboxHandlers)
        (authority: Authority)
        (secret: Credential)
        (changes: Change list)
        (reply: AsyncReplyChannel<Result<CoreChangesAccepted, string>>)
        : unit =
        match admitActorPost credentials actors authority secret with
        | Error err -> reply.Reply(Error err)
        | Ok () -> reply.Reply(handlers.postChange changes)

    let dispatch
        (credentials: CoreCredentials)
        (handlers: PersistHandlers)
        (actors: ActorMailboxHandlers)
        (onError: string -> string -> exn -> unit)
        (formatError: string -> string)
        (msg: CoreMsg)
        : unit =
        try
            match msg with
            | GetState reply ->
                reply.Reply(handlers.getState ())
            | GetRevision reply ->
                reply.Reply(handlers.getRevision ())
            | GetChangesSince (after, reply) ->
                reply.Reply(handlers.getChangesSince after)
            | PostChange (authority, secret, changes, reply) ->
                dispatchPostChange
                    credentials handlers actors
                    authority secret changes reply
            | PostGraphOnlyChange (changes, reply) ->
                reply.Reply(handlers.postGraphOnlyChange changes)
            | SnapshotDone graph ->
                handlers.snapshotDone graph
            | StartActor (authority, secret, request, reply) ->
                dispatchStartActor
                    credentials actors authority secret request reply
            | ActorStop (authority, secret, result, reply) ->
                dispatchActorStop
                    credentials actors authority secret result reply
        with ex ->
            let operation, context = operationContext msg
            try
                onError operation context ex
            with _ ->
                ()
            try
                replyFailure (formatError operation) msg
            with _ ->
                ()

    let start
        (credentials: CoreCredentials)
        (handlers: PersistHandlers)
        (actors: ActorMailboxHandlers)
        (onError: string -> string -> exn -> unit)
        (formatError: string -> string)
        : MailboxProcessor<CoreMsg> =
        MailboxProcessor<CoreMsg>.Start(fun inbox ->
            let rec loop () = async {
                let! msg = inbox.Receive()
                dispatch
                    credentials handlers actors onError formatError msg
                return! loop ()
            }
            loop ()
        )

    let startWithPrelude
        (credentials: CoreCredentials)
        (handlers: PersistHandlers)
        (actors: ActorMailboxHandlers)
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

        let failedHandlers error : PersistHandlers = {
            getState = handlers.getState
            getRevision = handlers.getRevision
            getChangesSince = handlers.getChangesSince
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
                                    Some(async {
                                        dispatch
                                            credentials
                                            handlers
                                            actors
                                            onError
                                            formatError
                                            msg
                                    })
                                | _ -> None),
                            timeout = 20)
                    return! startupLoop ()
            }
            and normalLoop () = async {
                let! msg = inbox.Receive()
                dispatch
                    credentials handlers actors onError formatError msg
                return! normalLoop ()
            }
            and failedLoop error = async {
                let! msg = inbox.Receive()
                let failed = failedHandlers error
                dispatch
                    credentials failed actors onError formatError msg
                return! failedLoop error
            }

            startupLoop ()
        )
