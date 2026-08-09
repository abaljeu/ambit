module Gambol.Client.UpdateWorkspaceDownload

open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateWorkspaceDesktop
open Gambol.Client.UpdateWorkspaceSync
open Gambol.Shared
open Gambol.Shared.ViewModel

let private postWorkspaceDownload
    (scope: WorkspaceSyncScope)
    : Result<DesktopWorkspaceSyncResponse, string> =
    match postDesktop "/_desktop/workspace-download" (encodeSyncScope scope) with
    | Error e -> Error e
    | Ok text ->
        match decodeDesktopWorkspaceSync text with
        | Error e -> Error e
        | Ok resp when resp.ok -> Ok resp
        | Ok { error = Some e } -> Error e
        | Ok _ -> Error "request failed"

let private downloadScoped
    (scope: WorkspaceSyncScope)
    : Result<DesktopWorkspaceSyncResponse, string> =
    match ensureMapped scope.label with
    | Error e -> Error e
    | Ok _ -> postWorkspaceDownload scope

let private labelHasLocalMapping (label: string) : bool =
    match lookupMappedPath label with
    | Ok(Some _) -> true
    | _ -> false

let failWorkspaceDownload (msg: string) (model: VM) : VM * Effect list =
    fail model msg

let failWorkspaceDownloadHttp
    (status: int)
    (body: string)
    (model: VM)
    : VM * Effect list =
    failWorkspaceDownload (httpError status body) model

/// Poll async download job; stamp graph nodes then refresh path sync.
let pollWorkspaceDownloadJob (jobId: string) (text: string) (model: VM) : VM * Effect list =
    match decodeDesktopWorkspaceDownloadJob text with
    | Error e -> failWorkspaceDownload e model
    | Ok job ->
        match job.state with
        | "completed" ->
            consoleLog ("[Gambol sync] download completed: " + job.detail)
            let stampOps =
                WorkspaceUploadStructure.planAlignFileStampOps
                    model.graph
                    job.label
                    job.pathStamps
            match stampOps with
            | [] ->
                okDetail model job.detail |> withPathSyncRefresh
            | ops ->
                let change =
                    { id = model.revision.Value
                      changeId = System.Guid.NewGuid()
                      ops = ops }
                match applyAndPostSync change model with
                | Error e -> failWorkspaceDownload e model
                | Ok model' ->
                    okDetail (withSiteMap model') job.detail
                    |> withPathSyncRefresh
        | "failed" -> failWorkspaceDownload job.detail model
        | "running" | "queued" ->
            let detail = sprintf "download %s: %s" job.state job.detail
            let model', _ = okDetail model detail
            model', [ Effect.ContinueWorkspaceDownload jobId ]
        | _ -> failWorkspaceDownload ("unknown download state: " + job.state) model

let downloadOp (model: VM) : VM * Effect list =
    if not (DesktopCapabilities.canWorkspaceSync model.desktopCapabilities) then
        model, []
    else
        match syncScopeFromFocus model with
        | Error msg -> fail model msg
        | Ok scope ->
            match downloadScoped scope with
            | Error "cancelled" -> model, []
            | Error e -> fail model e
            | Ok sync ->
                match sync.jobId with
                | Some jobId ->
                    let detail =
                        match sync.state with
                        | Some state -> sprintf "download %s: %s" state sync.detail
                        | None -> sync.detail
                    let model', _ = okDetail model detail
                    model', [ Effect.ContinueWorkspaceDownload jobId ]
                | None ->
                    let detail =
                        match sync.state with
                        | Some state -> sprintf "download %s: %s" state sync.detail
                        | None -> sync.detail
                    okDetail model detail |> withPathSyncRefresh

/// Debounce window before coalescing accumulated auto-download targets.
let autoDownloadDebounceMs = 400

/// Persist stamps arrived: accumulate File targets and arm the debounce tick.
/// Desktop-only; plain web (no workspace sync) is a no-op.
let accumulateAutoDownloadFromOps (ops: Op list) (model: VM) : VM * Effect list =
    if not (DesktopCapabilities.canWorkspaceSync model.desktopCapabilities) then
        model, []
    else
        match WorkspaceUploadStructure.autoDownloadFileTargets model.graph ops with
        | [] -> model, []
        | targets ->
            { model with
                pendingAutoDownloads = model.pendingAutoDownloads @ targets },
            [ Effect.ScheduleAutoDownloadTick autoDownloadDebounceMs ]

/// Remote poll changes carry the same persist `SetUpdateTime` ops.
let accumulateAutoDownloadFromChanges
    (changes: Change list)
    (model: VM)
    : VM * Effect list =
    accumulateAutoDownloadFromOps (changes |> List.collect (fun c -> c.ops)) model

/// Debounce tick: coalesce pending targets per label, keep already-mapped
/// labels, and fire-and-forget one scoped download each. No job polling and no
/// stamp-align Change, so a stamp-only change cannot feed back into itself.
let runAutoDownloadTick (model: VM) : VM * Effect list =
    WorkspaceSyncScope.coalesceDownloadTargets model.pendingAutoDownloads
    |> List.filter (fun scope -> labelHasLocalMapping scope.label)
    |> List.iter (fun scope -> postWorkspaceDownload scope |> ignore)
    { model with pendingAutoDownloads = [] }, []
