namespace Gambol.Desktop

open System
open System.Net.Http
open System.Threading
open Gambol.Shared

/// Runs staged workspace downloads (1 Running + 1 Queued).
[<RequireQualifiedAccess>]
module WorkspaceDownloadManager =

    type Manager =
        { client: HttpClient
          ambitBase: string
          cookieHeader: string option
          resolveMappedRoot: string -> Result<string, string>
          mutable queue: WorkspaceDownloadQueue.QueueState
          lockObj: obj }

    let create
        (client: HttpClient)
        (ambitBase: string)
        (cookieHeader: string option)
        (resolveMappedRoot: string -> Result<string, string>)
        : Manager =
        { client = client
          ambitBase = ambitBase
          cookieHeader = cookieHeader
          resolveMappedRoot = resolveMappedRoot
          queue = WorkspaceDownloadQueue.empty
          lockObj = obj() }

    let rec finishJob (mgr: Manager) (success: bool) (detail: string) =
        let nextJob =
            lock mgr.lockObj (fun () ->
                mgr.queue <-
                    WorkspaceDownloadQueue.finishRunning mgr.queue success detail
                mgr.queue.running)
        match nextJob with
        | Some job ->
            ThreadPool.QueueUserWorkItem(fun _ -> runJob mgr job) |> ignore
        | None -> ()

    and runJob (mgr: Manager) (job: WorkspaceDownloadQueue.DownloadJob) =
        match mgr.resolveMappedRoot job.scope.label with
        | Error e -> finishJob mgr false e
        | Ok mappedRoot ->
            let result =
                WorkspaceFileSync.getStaged
                    mgr.client
                    mgr.ambitBase
                    mappedRoot
                    job.scope
                    mgr.cookieHeader
                    job.id
            match result with
            | Ok r -> finishJob mgr true r.detail
            | Error err -> finishJob mgr false err

    let tryEnqueue (mgr: Manager) (scope: WorkspaceSyncScope) =
        lock mgr.lockObj (fun () ->
            let result, next = WorkspaceDownloadQueue.tryEnqueue mgr.queue scope
            mgr.queue <- next

            match result with
            | WorkspaceDownloadQueue.EnqueueResult.Started job ->
                ThreadPool.QueueUserWorkItem(fun _ -> runJob mgr job) |> ignore
            | WorkspaceDownloadQueue.EnqueueResult.Queued _ -> ()
            | WorkspaceDownloadQueue.EnqueueResult.Refused _ -> ()

            result)

    let tryGetJob (mgr: Manager) (jobId: Guid) =
        lock mgr.lockObj (fun () ->
            WorkspaceDownloadQueue.tryGetJob mgr.queue jobId)
