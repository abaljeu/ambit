namespace Gambol.Server

open System.Threading.Tasks
open Gambol.Shared

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
        | PostChange (changes, _) ->
            "PostChange", $"changeCount={changes.Length}"
        | PostGraphOnlyChange (changes, _) ->
            "PostGraphOnlyChange", $"changeCount={changes.Length}"
        | SnapshotDone _ -> "SnapshotDone", ""

    let replyFailure error msg =
        match msg with
        | GetState reply -> reply.Reply(Error error)
        | GetRevision reply -> reply.Reply(Error error)
        | GetChangesSince (_, reply) -> reply.Reply(Error error)
        | PostChange (_, reply) -> reply.Reply(Error error)
        | PostGraphOnlyChange (_, reply) -> reply.Reply(Error error)
        | SnapshotDone _ -> ()

    let dispatch
        (handlers: PersistHandlers)
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
            | PostChange (changes, reply) ->
                reply.Reply(handlers.postChange changes)
            | PostGraphOnlyChange (changes, reply) ->
                reply.Reply(handlers.postGraphOnlyChange changes)
            | SnapshotDone graph ->
                handlers.snapshotDone graph
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
        : MailboxProcessor<CoreMsg> =
        MailboxProcessor<CoreMsg>.Start(fun inbox ->
            let rec loop () = async {
                let! msg = inbox.Receive()
                dispatch handlers onError formatError msg
                return! loop ()
            }
            loop ()
        )

    let startWithPrelude
        (handlers: PersistHandlers)
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
                                | GetState _ | GetRevision _ | GetChangesSince _ ->
                                    Some(async {
                                        dispatch handlers onError formatError msg
                                    })
                                | _ -> None),
                            timeout = 20)
                    return! startupLoop ()
            }
            and normalLoop () = async {
                let! msg = inbox.Receive()
                dispatch handlers onError formatError msg
                return! normalLoop ()
            }
            and failedLoop error = async {
                let! msg = inbox.Receive()
                let failed = failedHandlers error
                dispatch failed onError formatError msg
                return! failedLoop error
            }

            startupLoop ()
        )
