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
        (tryHandleRead: FileAgentMsg -> Async<unit> option)
        (normalLoop: unit -> Async<unit>)
        (failedLoop: string -> Async<unit>)
        (inbox: MailboxProcessor<FileAgentMsg>)
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
