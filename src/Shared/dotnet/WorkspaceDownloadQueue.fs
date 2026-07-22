namespace Gambol.Shared

open System

/// Desktop download job queue: one Running + at most one Queued.
[<RequireQualifiedAccess>]
module WorkspaceDownloadQueue =

    type JobState =
        | Queued
        | Running
        | Completed
        | Failed

    type DownloadJob =
        { id: Guid
          scope: WorkspaceSyncScope
          state: JobState
          detail: string
          started: DateTime option
          finished: DateTime option }

    type EnqueueResult =
        | Started of DownloadJob
        | Queued of DownloadJob
        | Refused of string

    type QueueState =
        { running: DownloadJob option
          queued: DownloadJob option
          history: DownloadJob list }

    let empty =
        { running = None
          queued = None
          history = [] }

    let private newJob scope =
        { id = Guid.NewGuid()
          scope = scope
          state = JobState.Queued
          detail = ""
          started = None
          finished = None }

    let tryEnqueue (state: QueueState) (scope: WorkspaceSyncScope) : EnqueueResult * QueueState =
        match state.running, state.queued with
        | None, _ ->
            let job =
                { newJob scope with
                    state = JobState.Running
                    detail = "running"
                    started = Some DateTime.UtcNow }
            (Started job,
             { state with
                 running = Some job
                 queued = state.queued })
        | Some _, None ->
            let job =
                { newJob scope with
                    detail = "queued" }
            (Queued job, { state with queued = Some job })
        | Some _, Some _ ->
            (Refused "download queue full (one running and one queued); retry later",
             state)

    let tryGetJob (state: QueueState) (jobId: Guid) : DownloadJob option =
        let active =
            match state.running with
            | Some job -> [ job ]
            | None -> []
            @ match state.queued with
              | Some job -> [ job ]
              | None -> []
        (active @ state.history)
        |> List.tryFind (fun j -> j.id = jobId)

    let finishRunning (state: QueueState) (success: bool) (detail: string) : QueueState =
        match state.running with
        | None -> state
        | Some running ->
            let finished =
                { running with
                    state = if success then JobState.Completed else JobState.Failed
                    detail = detail
                    finished = Some DateTime.UtcNow }
            let nextRunning =
                state.queued
                |> Option.map (fun q ->
                    { q with
                        state = JobState.Running
                        detail = "running"
                        started = Some DateTime.UtcNow })
            { running = nextRunning
              queued = None
              history = finished :: state.history }
