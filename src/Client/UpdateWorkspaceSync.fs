module Gambol.Client.UpdateWorkspaceSync

open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateImport
open Gambol.Shared
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

let private postWorkspaceSync
    (url: string)
    (scope: WorkspaceSyncScope)
    : Result<DesktopWorkspaceSyncResponse, string> =
    match postDesktop url (encodeSyncScope scope) with
    | Error e -> Error e
    | Ok text ->
        match decodeDesktopWorkspaceSync text with
        | Error e -> Error e
        | Ok resp when resp.ok -> Ok resp
        | Ok { error = Some e } -> Error e
        | Ok _ -> Error "request failed"

let private postDirectoryReconcile
    (model: VM)
    (workspace: string)
    (path: string)
    (okDetailText: string)
    : VM * Effect list =
    let body = encodeReconciliationDirectoryRequest workspace path
    let status, text =
        postJsonSync
            "/ambit/workspace/reconciliation/directory"
            body
            (jsonHeaders ())
    if status < 200 || status >= 300 then
        fail model (httpError status text)
    else
        match decodeReconciliationDirectory text with
        | Ok n when n > 0 ->
            okDetailWithPoll model $"reconciled with warnings ({n})"
        | Ok _ -> okDetailWithPoll model okDetailText
        | Error e -> fail model e

let private applyOpsChange (ops: Op list) (model: VM) : Result<VM * Effect list, string> =
    if ops.IsEmpty then
        Error "could not create workspace"
    else
        let change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = ops }
        match applyAndPost change model with
        | Error e -> Error e
        | Ok (m, effects) -> Ok(withSiteMap m, effects)

let private pushScoped
    (scope: WorkspaceSyncScope)
    : Result<DesktopWorkspaceSyncResponse, string> =
    match ensureMapped scope.label with
    | Error e -> Error e
    | Ok _ ->
        postWorkspaceSync "/_desktop/workspace-push" scope

let private pullScoped
    (scope: WorkspaceSyncScope)
    : Result<DesktopWorkspaceSyncResponse, string> =
    match ensureMapped scope.label with
    | Error e -> Error e
    | Ok _ ->
        postWorkspaceSync "/_desktop/workspace-pull" scope

let focusIsWorkspaces (model: VM) : bool =
    match model.selectedNodes with
    | None -> false
    | Some sel ->
        focusedNodeId model.graph sel = Graph.workspacesId

/// Workspaces + Upload: pick folder → create WS → map → push → reconcile.
let uploadCreateWorkspaceOp (model: VM) : VM * Effect list =
    if not (DesktopCapabilities.canWorkspacePush model.desktopCapabilities) then
        model, []
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
                match applyOpsChange ops model with
                | Error e -> fail model e
                | Ok(model', createEffs) ->
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
                        match
                            postWorkspaceSync
                                "/_desktop/workspace-push"
                                scope
                        with
                        | Error e -> fail model' e
                        | Ok sync ->
                            let model'', reconEffs =
                                postDirectoryReconcile
                                    model'
                                    label
                                    ""
                                    sync.detail
                            model'', createEffs @ reconEffs

let uploadNamedScope (model: VM) : VM * Effect list =
    if not (DesktopCapabilities.canWorkspacePush model.desktopCapabilities) then
        match syncScopeFromFocus model with
        | Error msg -> fail model msg
        | Ok scope ->
            postDirectoryReconcile
                model
                scope.label
                scope.relative
                ("workspace reconciled from server disk: " + scope.label)
    else
        match syncScopeFromFocus model with
        | Error msg -> fail model msg
        | Ok scope ->
            match pushScoped scope with
            | Error "cancelled" -> model, []
            | Error e -> fail model e
            | Ok sync ->
                postDirectoryReconcile
                    model
                    scope.label
                    scope.relative
                    sync.detail

/// File Upload: ensure map → push file scope → Parse.
let uploadFileOp (fileId: NodeId) (model: VM) : VM * Effect list =
    if DesktopCapabilities.canWorkspacePush model.desktopCapabilities then
        match syncScopeFromFocus model with
        | Error msg -> fail model msg
        | Ok scope ->
            match pushScoped scope with
            | Error "cancelled" -> model, []
            | Error e -> fail model e
            | Ok _ -> parseFileOp fileId model
    else
        parseFileOp fileId model

let downloadOp (model: VM) : VM * Effect list =
    if not (DesktopCapabilities.canWorkspaceSync model.desktopCapabilities) then
        model, []
    else
        match syncScopeFromFocus model with
        | Error msg -> fail model msg
        | Ok scope ->
            match pullScoped scope with
            | Error "cancelled" -> model, []
            | Error e -> fail model e
            | Ok sync -> okDetail model sync.detail
