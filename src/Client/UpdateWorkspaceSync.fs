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

let private inventoryToAddedPaths
    (items: DesktopInventoryItem list)
    : string list =
    items
    |> List.map (fun i ->
        if i.isDirectory then i.relative + "/.amb"
        else i.relative)

let private fetchImmediateInventory
    (label: string)
    : Result<DesktopInventoryItem list, string> =
    let body = encodeWorkspaceInventoryRequest label ""
    match postDesktop "/_desktop/workspace-inventory" body with
    | Error e -> Error e
    | Ok text -> decodeDesktopInventory text

let private postAddedReconcile
    (model: VM)
    (workspace: string)
    (paths: string list)
    : VM * Effect list =
    if paths.IsEmpty then
        okWithPoll model
    else
        let body = encodeReconciliationAddedRequest workspace paths
        let status, text =
            postJsonSync
                "/ambit/workspace/reconciliation/added"
                body
                (jsonHeaders ())
        postReconcileResponse model status text

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

let private withPathSyncRefresh
    (model: VM, effs: Effect list)
    : VM * Effect list =
    model, RequestWorkspacePathSyncSnapshot :: effs

/// After async workspace-push success body: clear Uploading, then reconcile or parse.
/// Prefer push `detail` in lastCmdResult so Upload does not look like a raw file-status JSON.
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
        match parseFileId with
        | Some fileId ->
            parseFileOp fileId model' |> withPathSyncRefresh
        | None ->
            let modelR, effs =
                postDirectoryReconcile model' scope.label scope.relative
            match modelR.lastCmdResult with
            | Some (CmdLastResult.Error _) ->
                modelR, RequestWorkspacePathSyncSnapshot :: effs
            | Some (CmdLastResult.Detail (_, warn)) ->
                { modelR with
                    lastCmdResult =
                        Some (
                            CmdLastResult.Detail (
                                None, sync.detail + "; " + warn)) },
                RequestWorkspacePathSyncSnapshot :: effs
            | _ ->
                { modelR with
                    lastCmdResult =
                        Some (CmdLastResult.Detail (None, sync.detail)) },
                RequestWorkspacePathSyncSnapshot :: effs
    | Ok { error = Some e } -> failWorkspacePush e model
    | Ok _ -> failWorkspacePush "request failed" model

/// Blocking poll so stub nodes appear before the heavy file push (keeps Uploading).
let private applyPollSyncKeepUpload (model: VM) : VM =
    let url =
        sprintf "/%s/poll?_=%d&rev=%d" currentFile (int (nowMs ())) model.revision.Value
    let status, text = getJsonSync url
    if status < 200 || status >= 300 then
        keepUploading model
    else
        match Serialization.decodePollResponse text with
        | Error _ -> keepUploading model
        | Ok poll when poll.changes.IsEmpty -> keepUploading model
        | Ok poll ->
            let state: State =
                { graph = model.graph
                  history = model.history
                  revision = model.revision }
            match SyncLogic.applyServerTail poll.changes state with
            | Error _ -> keepUploading model
            | Ok newState ->
                { model with
                    graph = newState.graph
                    history = newState.history
                    revision = newState.revision }
                |> withSiteMap
                |> keepUploading

/// Background: inventory + top-level stubs, then defer file push.
let continueWorkspaceStubsThenPush
    (scope: WorkspaceSyncScope)
    (model: VM)
    : VM * Effect list =
    match fetchImmediateInventory scope.label with
    | Error e -> fail (clearUploading model) e
    | Ok items ->
        let paths = inventoryToAddedPaths items
        if paths.IsEmpty then
            keepUploading model, [ Effect.ContinueWorkspacePush (scope, None) ]
        else
            let body = encodeReconciliationAddedRequest scope.label paths
            let status, text =
                postJsonSync
                    "/ambit/workspace/reconciliation/added"
                    body
                    (jsonHeaders ())
            if status < 200 || status >= 300 then
                fail (clearUploading model) (httpError status text)
            else
                match decodeReconciliationDirectory text with
                | Error e -> fail (clearUploading model) e
                | Ok n ->
                    let model' = applyPollSyncKeepUpload model
                    let model'' =
                        if n > 0 then
                            { model' with
                                lastCmdResult =
                                    Some (
                                        CmdLastResult.Detail (
                                            None, $"reconciled with warnings ({n})")) }
                        else
                            model'
                    keepUploading model'',
                    [ Effect.ContinueWorkspacePush (scope, None) ]

let private beginDeferredPush
    (scope: WorkspaceSyncScope)
    (parseFileId: NodeId option)
    (model: VM)
    : VM * Effect list =
    keepUploading model, [ Effect.ContinueWorkspacePush (scope, parseFileId) ]

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
                        [ Effect.ContinueWorkspaceStubsThenPush scope ]

let private contextualTargetForModel (model: VM) =
    model.selectedNodes
    |> Option.bind (fun selection ->
        contextualTarget
            model.graph
            selection.range.parent.nodeId
            selection.focus)

/// Upload: desktop push when mapped; else graph-only from DataDir (web).
let uploadOp (model: VM) : VM * Effect list =
    let canPush =
        DesktopCapabilities.canWorkspacePush model.desktopCapabilities
    let target = contextualTargetForModel model
    match WorkspaceUpload.plan canPush (focusIsWorkspaces model) target with
    | WorkspaceUploadAction.CreateWorkspaceFromFolder ->
        uploadCreateWorkspaceOp model
    | WorkspaceUploadAction.DesktopPush parseFileId ->
        match syncScopeFromFocus model with
        | Error msg -> fail model msg
        | Ok scope -> beginDeferredPush scope parseFileId model
    | WorkspaceUploadAction.ReconcileServerDisk ->
        match syncScopeFromFocus model with
        | Error msg -> fail model msg
        | Ok scope ->
            postDirectoryReconcile model scope.label scope.relative
    | WorkspaceUploadAction.ParseServerDisk fileId ->
        parseFileOp fileId model
    | WorkspaceUploadAction.Unavailable msg ->
        withResult
            model
            (CmdLastResult.Error(Some(displayName Upload), msg))

let uploadAvailable (model: VM) =
    WorkspaceUpload.isAvailable
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
                let detail =
                    match sync.state with
                    | Some state -> sprintf "download %s: %s" state sync.detail
                    | None -> sync.detail
                okDetail model detail |> withPathSyncRefresh
