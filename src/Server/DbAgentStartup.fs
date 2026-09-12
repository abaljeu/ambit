namespace Gambol.Server

open System
open System.Threading.Tasks

/// Private startup scheduling for the PostgreSQL agent mailbox.
[<RequireQualifiedAccess>]
module DbAgentStartup =

    let run
        (runSweep: unit -> Result<DatabaseProjection.ProjectionMaintenanceResult, string>)
        (applySuccess: DatabaseProjection.ProjectionMaintenanceResult -> Result<unit, string>)
        (setReady: unit -> unit)
        (tryHandleRead: CoreMsg -> Async<unit> option)
        (normalLoop: unit -> Async<unit>)
        (failedLoop: string -> Async<unit>)
        (inbox: MailboxProcessor<CoreMsg>)
        : Async<unit> =
        let sweepTask =
            Task.Run(fun () ->
                try
                    runSweep ()
                with ex ->
                    Error $"Startup projection sweep failed: {ex.Message}")

        let rec startupLoop () = async {
            if sweepTask.IsCompleted then
                match sweepTask.GetAwaiter().GetResult() with
                | Ok result ->
                    match applySuccess result with
                    | Ok () ->
                        setReady ()
                        return! normalLoop ()
                    | Error error ->
                        return! failedLoop error
                | Error error ->
                    return! failedLoop error
            else
                let! _ = inbox.TryScan(tryHandleRead, timeout = 20)
                return! startupLoop ()
        }

        startupLoop ()

    let startWithBackend
        (runSweep: unit -> Result<DatabaseProjection.ProjectionMaintenanceResult, string>)
        (applySuccess: DatabaseProjection.ProjectionMaintenanceResult -> Result<unit, string>)
        (setReady: unit -> unit)
        (handlers: PersistHandlers)
        (onError: string -> string -> exn -> unit)
        (formatError: string -> string)
        (startupResult: Result<unit, string> option ref)
        : MailboxProcessor<CoreMsg> =
        let sweepTask =
            Task.Run(fun () ->
                try
                    runSweep ()
                with ex ->
                    Error $"Startup projection sweep failed: {ex.Message}")

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
                if sweepTask.IsCompleted then
                    match sweepTask.GetAwaiter().GetResult() with
                    | Ok result ->
                        match applySuccess result with
                        | Ok () ->
                            startupResult.Value <- Some (Ok ())
                            setReady ()
                            return! normalLoop ()
                        | Error error ->
                            startupResult.Value <- Some (Error error)
                            return! failedLoop error
                    | Error error ->
                        startupResult.Value <- Some (Error error)
                        return! failedLoop error
                else
                    let! _ =
                        inbox.TryScan(
                            (fun msg ->
                                match msg with
                                | GetState reply ->
                                    Some(async {
                                        reply.Reply(handlers.getState ())
                                    })
                                | GetRevision reply ->
                                    Some(async {
                                        reply.Reply(handlers.getRevision ())
                                    })
                                | GetChangesSince (after, reply) ->
                                    Some(async {
                                        reply.Reply(
                                            handlers.getChangesSince after)
                                    })
                                | _ -> None),
                            timeout = 20)
                    return! startupLoop ()
            }
            and normalLoop () = async {
                let! msg = inbox.Receive()
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
                    let operation, context =
                        CoreMailboxBackend.operationContext msg
                    try
                        onError operation context ex
                    with _ ->
                        ()
                    try
                        CoreMailboxBackend.replyFailure
                            (formatError operation)
                            msg
                    with _ ->
                        ()
                return! normalLoop ()
            }
            and failedLoop error = async {
                let! msg = inbox.Receive()
                let failed = failedHandlers error
                try
                    match msg with
                    | GetState reply ->
                        reply.Reply(failed.getState ())
                    | GetRevision reply ->
                        reply.Reply(failed.getRevision ())
                    | GetChangesSince (after, reply) ->
                        reply.Reply(failed.getChangesSince after)
                    | PostChange (changes, reply) ->
                        reply.Reply(failed.postChange changes)
                    | PostGraphOnlyChange (changes, reply) ->
                        reply.Reply(failed.postGraphOnlyChange changes)
                    | SnapshotDone _ -> ()
                with _ ->
                    ()
                return! failedLoop error
            }

            startupLoop ()
        )
