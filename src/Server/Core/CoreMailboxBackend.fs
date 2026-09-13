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
        changes: Change list *
        actor: (Authority * Credential) option *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
    | PostGraphOnlyChange of
        changes: Change list *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
    | SnapshotDone of graph: Graph option
    | StartActor of
        StartActorRequest *
        AsyncReplyChannel<Result<ActorStarted, string>>
    | ActorStop of
        actor: Authority *
        secret: Credential *
        ActorResult *
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

type ActorMailboxCtx = {
    pool: CoreActorPool
    callers: CallerTable
    events: EventLog
    credentials: CoreCredentials
}

[<RequireQualifiedAccess>]
module ActorMailboxCtx =

    let create () : ActorMailboxCtx =
        let credentials = CoreCredentials.create ()
        { credentials = credentials
          callers = CallerTable.create ()
          events = EventLog.create ()
          pool = CoreActorPool.create credentials }

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
        | PostChange (changes, _, _) ->
            "PostChange", $"changeCount={changes.Length}"
        | PostGraphOnlyChange (changes, _) ->
            "PostGraphOnlyChange", $"changeCount={changes.Length}"
        | SnapshotDone _ -> "SnapshotDone", ""
        | StartActor _ -> "StartActor", ""
        | ActorStop _ -> "ActorStop", ""

    let replyFailure error msg =
        match msg with
        | GetState reply -> reply.Reply(Error error)
        | GetRevision reply -> reply.Reply(Error error)
        | GetChangesSince (_, reply) -> reply.Reply(Error error)
        | PostChange (_, _, reply) -> reply.Reply(Error error)
        | PostGraphOnlyChange (_, reply) -> reply.Reply(Error error)
        | SnapshotDone _ -> ()
        | StartActor (_, reply) -> reply.Reply(Error error)
        | ActorStop (_, _, _, reply) -> reply.Reply(Error error)

    let private unwrap result =
        match result with
        | Ok value -> value
        | Error error -> failwith error

    let actorCoreChanges
        (inbox: MailboxProcessor<CoreMsg>)
        (authority: Authority)
        (secret: Credential)
        : CoreChanges =
        { getState = fun () -> inbox.PostAndAsyncReply GetState
          getRevision =
            fun () -> async {
                let! result = inbox.PostAndAsyncReply GetRevision
                return unwrap result
            }
          getChangesSince =
            fun after -> async {
                let! result =
                    inbox.PostAndAsyncReply(fun reply ->
                        GetChangesSince(after, reply))
                return unwrap result
            }
          isReady = fun () -> true
          postChange =
            fun changes ->
                inbox.PostAndAsyncReply(fun reply ->
                    PostChange(changes, Some(authority, secret), reply))
          postGraphOnlyChange =
            fun changes ->
                inbox.PostAndAsyncReply(fun reply ->
                    PostGraphOnlyChange(changes, reply)) }

    let private kickoffStart
        (actors: ActorMailboxCtx)
        (inbox: MailboxProcessor<CoreMsg>)
        (req: StartActorRequest)
        (reply: AsyncReplyChannel<Result<ActorStarted, string>>)
        : unit =
        if not (actors.callers.admit req.caller req.secret) then
            reply.Reply(Error CoreAuth.refuse)
        else
            Async.Start(async {
                let! state = inbox.PostAndAsyncReply GetState
                match state with
                | Error err -> reply.Reply(Error err)
                | Ok s ->
                    let bind auth secret =
                        actorCoreChanges inbox auth secret
                    let started =
                        actors.pool.startActor
                            req.name
                            req.focus
                            s.graph
                            s.revision
                            bind
                            actors.events
                    reply.Reply(started)
            })

    let private handlePostChange
        (handlers: PersistHandlers)
        (actors: ActorMailboxCtx)
        (changes: Change list)
        (actorOpt: (Authority * Credential) option)
        (reply: AsyncReplyChannel<Result<CoreChangesAccepted, string>>)
        : unit =
        match actorOpt with
        | None -> reply.Reply(handlers.postChange changes)
        | Some (auth, secret) ->
            match actors.pool.admit auth secret with
            | Error err -> reply.Reply(Error err)
            | Ok () -> reply.Reply(handlers.postChange changes)

    let private handleActorStop
        (actors: ActorMailboxCtx)
        (auth: Authority)
        (secret: Credential)
        (result: ActorResult)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match actors.pool.admit auth secret with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            match result with
            | ActorSucceeded ->
                ignore (actors.events.appendFinished auth)
                actors.pool.drop auth
                reply.Reply(Ok ())

    let private dispatchMatch
        (handlers: PersistHandlers)
        (actors: ActorMailboxCtx)
        (inbox: MailboxProcessor<CoreMsg>)
        (failed: string option)
        (msg: CoreMsg)
        : unit =
        match msg, failed with
        | StartActor (_, reply), Some err ->
            reply.Reply(Error err)
        | ActorStop (_, _, _, reply), Some err ->
            reply.Reply(Error err)
        | StartActor (req, reply), None ->
            kickoffStart actors inbox req reply
        | ActorStop (auth, secret, result, reply), None ->
            handleActorStop actors auth secret result reply
        | PostChange (changes, actorOpt, reply), _ ->
            handlePostChange handlers actors changes actorOpt reply
        | GetState reply, _ ->
            reply.Reply(handlers.getState ())
        | GetRevision reply, _ ->
            reply.Reply(handlers.getRevision ())
        | GetChangesSince (after, reply), _ ->
            reply.Reply(handlers.getChangesSince after)
        | PostGraphOnlyChange (changes, reply), _ ->
            reply.Reply(handlers.postGraphOnlyChange changes)
        | SnapshotDone graph, _ ->
            handlers.snapshotDone graph

    let dispatch
        (handlers: PersistHandlers)
        (actors: ActorMailboxCtx)
        (inbox: MailboxProcessor<CoreMsg>)
        (failed: string option)
        (onError: string -> string -> exn -> unit)
        (formatError: string -> string)
        (msg: CoreMsg)
        : unit =
        try
            dispatchMatch handlers actors inbox failed msg
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
        (handlers: PersistHandlers)
        (onError: string -> string -> exn -> unit)
        (formatError: string -> string)
        (actors: ActorMailboxCtx)
        : MailboxProcessor<CoreMsg> =
        MailboxProcessor<CoreMsg>.Start(fun inbox ->
            let rec loop () = async {
                let! msg = inbox.Receive()
                dispatch
                    handlers actors inbox None onError formatError msg
                return! loop ()
            }
            loop ()
        )

    let startWithPrelude
        (handlers: PersistHandlers)
        (onError: string -> string -> exn -> unit)
        (formatError: string -> string)
        (actors: ActorMailboxCtx)
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
            let go handlers failed msg =
                dispatch
                    handlers actors inbox failed onError formatError msg
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
                                    Some(async { go handlers None msg })
                                | _ -> None),
                            timeout = 20)
                    return! startupLoop ()
            }
            and normalLoop () = async {
                let! msg = inbox.Receive()
                go handlers None msg
                return! normalLoop ()
            }
            and failedLoop error = async {
                let! msg = inbox.Receive()
                go (failedHandlers error) (Some error) msg
                return! failedLoop error
            }

            startupLoop ()
        )
