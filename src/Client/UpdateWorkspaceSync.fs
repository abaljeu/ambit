module Gambol.Client.UpdateWorkspaceSync

open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateImport
open Gambol.Shared
open Gambol.Shared.CommandEntry
open Gambol.Shared.ViewModel
open Thoth.Json.Core

let private jsonHeaders () = jsonMutatingPostHeaders ()

let private withResult (model: VM) (result: CmdLastResult) : VM * Effect list =
    { model with lastCmdResult = Some result }, []

let private fail (model: VM) (msg: string) : VM * Effect list =
    consoleLog ("[Gambol sync] " + msg)
    withResult model (CmdLastResult.Error (None, msg))

let private okDetail (model: VM) (msg: string) : VM * Effect list =
    consoleLog ("[Gambol sync] " + msg)
    withResult model (CmdLastResult.Detail (None, msg))

/// Success detail plus immediate sync poll (server applied graph-only ops).
let private okDetailWithPoll (model: VM) (msg: string) : VM * Effect list =
    let model', effs = okDetail model msg
    let si, pollEffs = SyncPlanner.tryStartPoll model'.revision model'.syncInfo
    { model' with syncInfo = si }, effs @ pollEffs

let private okWithPoll (model: VM) : VM * Effect list =
    consoleLog "[Gambol sync] ok"
    let si, pollEffs = SyncPlanner.tryStartPoll model.revision model.syncInfo
    { model with
        syncInfo = si
        lastCmdResult = Some (CmdLastResult.Ok None) },
    pollEffs

let private httpError (status: int) (body: string) : string =
    match decodePostChangeError body with
    | Some err -> err
    | None ->
        "HTTP " + string status + ": " + LogText.truncateForLog 200 body

let private encodeWorkspacePath (workspace: string) (path: string) : string =
    Encode.object
        [ "label", Encode.string workspace
          "path", Encode.string path ]
    |> Thoth.Json.JavaScript.Encode.toString 0

let private encodeSyncScope (scope: WorkspaceSyncScope) : string =
    let kind =
        match scope.kind with
        | SyncScopeKind.Workspace -> "workspace"
        | SyncScopeKind.Directory -> "directory"
        | SyncScopeKind.File -> "file"
    Encode.object
        [ "label", Encode.string scope.label
          "relative", Encode.string scope.relative
          "kind", Encode.string kind ]
    |> Thoth.Json.JavaScript.Encode.toString 0

let private postDesktop (url: string) (body: string) : Result<string, string> =
    let status, text = postJsonSync url body (jsonHeaders ())
    if status < 200 || status >= 300 then Error(httpError status text)
    else Ok text

let private putDesktop (url: string) (body: string) : Result<string, string> =
    let status, text = putJsonSync url body (jsonHeaders ())
    if status < 200 || status >= 300 then Error(httpError status text)
    else Ok text

let private pickFolder () : Result<string, string> =
    match postDesktop "/_desktop/pick-folder" "{}" with
    | Error e -> Error e
    | Ok text ->
        match decodeDesktopPickFolder text with
        | Error e -> Error e
        | Ok { cancelled = true } -> Error "cancelled"
        | Ok { path = Some path } -> Ok path
        | Ok _ -> Error "no folder selected"

let private upsertMapping (workspace: string) (path: string) : Result<unit, string> =
    match putDesktop "/_desktop/workspace-mappings" (encodeWorkspacePath workspace path) with
    | Error e -> Error e
    | Ok _ -> Ok ()

let private folderBasename (path: string) : string =
    let trimmed = path.TrimEnd('\\', '/')
    let i =
        max (trimmed.LastIndexOf('\\')) (trimmed.LastIndexOf('/'))
    if i < 0 then trimmed
    else trimmed.Substring(i + 1)

let private lookupMappedPath (label: string) : Result<string option, string> =
    let status, text = getJsonSync "/_desktop/workspace-mappings"
    if status < 200 || status >= 300 then
        Error(httpError status text)
    else
        match decodeMappedRootPath text label with
        | Error e -> Error e
        | Ok pathOpt -> Ok pathOpt

/// GET mappings; pick-folder + PUT when label has no mapping.
let private ensureMapped (label: string) : Result<string, string> =
    if System.String.IsNullOrWhiteSpace label then
        Error "ROOT and Workspaces cannot be mapped"
    else
        match lookupMappedPath label with
        | Error e -> Error e
        | Ok(Some path) -> Ok path
        | Ok None ->
            match pickFolder () with
            | Error e -> Error e
            | Ok path ->
                match upsertMapping label path with
                | Error e -> Error e
                | Ok () -> Ok path

let private syncScopeFromFocus (model: VM) : Result<WorkspaceSyncScope, string> =
    match model.selectedNodes with
    | None -> Error "select a workspace, directory, or file"
    | Some sel ->
        let nodeId = focusedNodeId model.graph sel
        WorkspaceSyncScope.tryFromFocus model.graph nodeId

let private postReconcileResponse
    (model: VM)
    (status: int)
    (text: string)
    : VM * Effect list =
    if status < 200 || status >= 300 then
        fail model (httpError status text)
    else
        match decodeReconciliationDirectory text with
        | Ok n when n > 0 ->
            okDetailWithPoll model $"reconciled with warnings ({n})"
        | Ok _ -> okWithPoll model
        | Error e -> fail model e

let private postDirectoryReconcile
    (model: VM)
    (workspace: string)
    (path: string)
    : VM * Effect list =
    let body = encodeReconciliationDirectoryRequest workspace path
    let status, text =
        postJsonSync
            "/ambit/workspace/reconciliation/directory"
            body
            (jsonHeaders ())
    postReconcileResponse model status text

let private inventoryToStubItems (items: DesktopInventoryItem list) : WorkspaceUploadStructure.InventoryItem list =
    items
    |> List.map (fun i ->
        { relative = i.relative
          isDirectory = i.isDirectory })

/// Apply + synchronous POST so server graph has the workspace before push/reconcile.
let private applyAndPostSync (change: Change) (model: VM) : Result<VM, string> =
    let state: State =
        { graph = model.graph
          revision = model.revision
          history = model.history }
    match History.applyChange change state with
    | ApplyResult.Invalid (_, msg) -> Error msg
    | ApplyResult.Unchanged _ -> Error "change not applied"
    | ApplyResult.Changed newState ->
        let body =
            SyncBatch.toDeltaChain model.revision.Value [ change ]
            |> encodePendingBatchBody
        let url = sprintf "/%s/changes" currentFile
        let status, text = postJsonSync url body (jsonHeaders ())
        if status < 200 || status >= 300 then
            Error(httpError status text)
        else
            match decodeChangeAckResponse text with
            | Error e -> Error e
            | Ok ack ->
                Ok
                    { model with
                        graph = PersistStamp.applyToGraph ack.stampOps newState.graph
                        history = newState.history
                        revision = ack.revision }

/// Local graph only — stubs paint before structure POST / body push.
let private applyStructureLocally (change: Change) (model: VM) : Result<VM, string> =
    let state: State =
        { graph = model.graph
          revision = model.revision
          history = model.history }
    match History.applyChange change state with
    | ApplyResult.Invalid (_, msg) -> Error msg
    | ApplyResult.Unchanged _ -> Error "change not applied"
    | ApplyResult.Changed newState ->
        Ok
            { model with
                graph = newState.graph
                history = newState.history }

let private reparseSkippedUploadFiles
    (model: VM)
    (workspace: string)
    (paths: string list)
    : VM * Effect list =
    let next, parseEffects =
        paths
        |> List.fold
            (fun ((current: VM), (effs: Effect list)) rel ->
                match WorkspaceUploadStructure.tryResolveFileNode current.graph workspace rel with
                | None -> current, effs
                | Some fileId ->
                    let afterParse, parseEffs = parseFileOp fileId current
                    afterParse, effs @ parseEffs)
            (model, [])
    next, WorkspaceUpload.sequenceParseEffects parseEffects

let private createWorkspaceOnServer (ops: Op list) (model: VM) : Result<VM, string> =
    if ops.IsEmpty then
        Error "could not create workspace"
    else
        let change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = ops }
        applyAndPostSync change model |> Result.map withSiteMap

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

/// Empty selection means the view root is the focus (same as edit/jump).
let private effectiveFocusId (model: VM) : NodeId =
    match model.selectedNodes with
    | None -> viewRootNodeId model
    | Some sel -> focusedNodeId model.graph sel

let focusIsWorkspaces (model: VM) : bool =
    effectiveFocusId model = Graph.workspacesId

let private clearUploading (m: VM) =
    { m with
        syncInfo = SyncInfo.withSyncState Idle m.syncInfo }

let private keepUploading (m: VM) =
    { m with
        syncInfo = SyncInfo.withSyncState Uploading m.syncInfo }

/// Ensure map (may pick-folder), then JSON body for async `/_desktop/workspace-push`.
let tryPrepareWorkspacePushBody
    (scope: WorkspaceSyncScope)
    : Result<string, string> =
    match ensureMapped scope.label with
    | Error e -> Error e
    | Ok _ -> Ok (encodeSyncScope scope)

let cancelWorkspacePush (model: VM) : VM * Effect list =
    clearUploading model, []

let failWorkspacePush (msg: string) (model: VM) : VM * Effect list =
    fail (clearUploading model) msg

let failWorkspacePushHttp
    (status: int)
    (body: string)
    (model: VM)
    : VM * Effect list =
    failWorkspacePush (httpError status body) model

let failWorkspaceDownload (msg: string) (model: VM) : VM * Effect list =
    fail model msg

let failWorkspaceDownloadHttp
    (status: int)
    (body: string)
    (model: VM)
    : VM * Effect list =
    failWorkspaceDownload (httpError status body) model

let private withPathSyncRefresh
    (model: VM, effs: Effect list)
    : VM * Effect list =
    model, RequestWorkspacePathSyncSnapshot :: effs

let private contextualTargetForModel (model: VM) =
    model.selectedNodes
    |> Option.bind (fun selection ->
        contextualTarget
            model.graph
            selection.range.parent.nodeId
            selection.focus)

/// After async workspace-push success: clear Uploading, reparse skipped or file focus.
let completeWorkspacePush
    (scope: WorkspaceSyncScope)
    (parseFileId: NodeId option)
    (text: string)
    (model: VM)
    : VM * Effect list =
    match decodeDesktopWorkspaceSync text with
    | Error e -> failWorkspacePush e model
    | Ok sync when sync.ok ->
        consoleLog ("[Gambol sync] " + sync.detail)
        let model' = clearUploading model
        let skipped =
            sync.skippedPaths |> Option.defaultValue []
        match parseFileId with
        | Some fileId ->
            parseFileOp fileId model' |> withPathSyncRefresh
        | None when not skipped.IsEmpty ->
            let modelR, parseEffs = reparseSkippedUploadFiles model' scope.label skipped
            let detail =
                sync.detail
                + sprintf "; reparsed %d skipped file(s)" skipped.Length
            { modelR with
                lastCmdResult = Some (CmdLastResult.Detail (None, detail)) },
            RequestWorkspacePathSyncSnapshot :: parseEffs
        | None ->
            okDetail model' sync.detail |> withPathSyncRefresh
    | Ok { error = Some e } -> failWorkspacePush e model
    | Ok _ -> failWorkspacePush "request failed" model

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

/// Body for async POST `/_desktop/workspace-inventory`.
let encodeWorkspaceInventoryBody (scope: WorkspaceSyncScope) : string =
    encodeSyncScope scope

/// Undo local stubs if structure POST fails after optimistic apply.
let private undoLocalStructure (model: VM) : VM =
    let state: State =
        { graph = model.graph
          history = model.history
          revision = model.revision }
    match History.undo state with
    | ApplyResult.Changed s ->
        { model with
            graph = s.graph
            history = s.history }
        |> withSiteMap
    | _ -> model

let failUploadStructurePost (msg: string) (model: VM) : VM * Effect list =
    fail (clearUploading (undoLocalStructure model)) msg

let failUploadStructurePostHttp
    (status: int)
    (body: string)
    (model: VM)
    : VM * Effect list =
    failUploadStructurePost (httpError status body) model

/// Inventory arrived: plan + apply stubs locally, then async structure POST.
let completeUploadInventory
    (scope: WorkspaceSyncScope)
    (parseFileId: NodeId option)
    (text: string)
    (model: VM)
    : VM * Effect list =
    match decodeDesktopUploadInventory text with
    | Error e -> fail (clearUploading model) e
    | Ok { items = items } ->
        let stubItems = inventoryToStubItems items
        match WorkspaceUploadStructure.planStubOps model.graph scope.label stubItems with
        | Error e -> fail (clearUploading model) e
        | Ok ops when ops.IsEmpty ->
            keepUploading model,
            [ Effect.ContinueWorkspacePush (scope, parseFileId) ]
        | Ok ops ->
            let change =
                { id = model.revision.Value
                  changeId = System.Guid.NewGuid()
                  ops = ops }
            match applyStructureLocally change model with
            | Error e -> fail (clearUploading model) e
            | Ok model' ->
                keepUploading (withSiteMap model'),
                [ Effect.ContinuePostUploadStructure (change, scope, parseFileId) ]

/// Structure Change ACK: stamp + revision, then body push.
let completeUploadStructurePost
    (scope: WorkspaceSyncScope)
    (parseFileId: NodeId option)
    (text: string)
    (model: VM)
    : VM * Effect list =
    match decodeChangeAckResponse text with
    | Error e -> failUploadStructurePost e model
    | Ok ack ->
        let model' =
            { model with
                graph = PersistStamp.applyToGraph ack.stampOps model.graph
                revision = ack.revision }
            |> withSiteMap
            |> keepUploading
        model', [ Effect.ContinueWorkspacePush (scope, parseFileId) ]

/// Workspaces + Upload: pick → create → map → paint Uploading → stubs → push.
let uploadCreateWorkspaceOp (model: VM) : VM * Effect list =
    if not (DesktopCapabilities.canWorkspacePush model.desktopCapabilities) then
        fail model "desktop unavailable: cannot create workspace from folder"
    else
        match pickFolder () with
        | Error "cancelled" -> model, []
        | Error e -> fail model e
        | Ok path ->
            let basename = folderBasename path
            if System.String.IsNullOrWhiteSpace basename then
                fail model "folder name is empty"
            else
                let wsId, ops =
                    FileNodeOps.planCreateWorkspace model.graph basename
                match createWorkspaceOnServer ops model with
                | Error e -> fail model e
                | Ok model' ->
                    let label =
                        match Map.tryFind wsId model'.graph.nodes with
                        | Some node ->
                            Filename.tryValue node.name
                            |> Option.defaultValue basename
                        | None -> basename
                    match upsertMapping label path with
                    | Error e -> fail model' e
                    | Ok () ->
                        let scope =
                            { label = label
                              relative = ""
                              kind = SyncScopeKind.Workspace }
                        keepUploading model',
                        [ Effect.ContinueWorkspaceStubsThenPush (scope, None) ]

/// Upload: desktop push when mapped; else graph-only from DataDir (web).
let uploadOp (model: VM) : VM * Effect list =
    if not (WorkspaceUpload.canStart model.syncInfo) then
        fail model "wait for pending synchronization to finish"
    else
        let canPush =
            DesktopCapabilities.canWorkspacePush model.desktopCapabilities
        let target = contextualTargetForModel model
        match WorkspaceUpload.plan canPush (focusIsWorkspaces model) target with
        | WorkspaceUploadAction.CreateWorkspaceFromFolder ->
            uploadCreateWorkspaceOp model
        | WorkspaceUploadAction.DesktopPush parseFileId ->
            match syncScopeFromFocus model with
            | Error msg -> fail model msg
            | Ok scope ->
                keepUploading model,
                [ Effect.ContinueWorkspaceStubsThenPush (scope, parseFileId) ]
        | WorkspaceUploadAction.ReconcileServerDisk ->
            match syncScopeFromFocus model with
            | Error msg -> fail model msg
            | Ok scope ->
                postDirectoryReconcile model scope.label scope.relative
                |> withPathSyncRefresh
        | WorkspaceUploadAction.ParseServerDisk fileId ->
            parseFileOp fileId model
        | WorkspaceUploadAction.Unavailable msg ->
            withResult
                model
                (CmdLastResult.Error(Some(displayName Upload), msg))

let uploadAvailable (model: VM) =
    WorkspaceUpload.canStart model.syncInfo
    && WorkspaceUpload.isAvailable
        (DesktopCapabilities.canWorkspacePush model.desktopCapabilities)
        (focusIsWorkspaces model)
        (contextualTargetForModel model)

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
