module Gambol.Client.UpdateWorkspaceSync

open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateImport
open Gambol.Client.UpdateWorkspaceDesktop
open Gambol.Shared
open Gambol.Shared.CommandEntry
open Gambol.Shared.ViewModel

let private jsonHeaders () = jsonMutatingPostHeaders ()

let withResult (model: VM) (result: CmdLastResult) : VM * Effect list =
    { model with lastCmdResult = Some result }, []

let fail (model: VM) (msg: string) : VM * Effect list =
    consoleLog ("[Gambol sync] " + msg)
    withResult model (CmdLastResult.Error (None, msg))

let okDetail (model: VM) (msg: string) : VM * Effect list =
    consoleLog ("[Gambol sync] " + msg)
    withResult model (CmdLastResult.Detail (None, msg))

/// Success detail plus Load Fetch+Poll (or ordinary Poll if no Focus target).
let private okDetailWithPoll (model: VM) (msg: string) : VM * Effect list =
    let model', effs = okDetail model msg
    let si, pollEffs = tryStartLoadFetch model'
    { model' with syncInfo = si }, effs @ pollEffs

let private okWithPoll (model: VM) : VM * Effect list =
    consoleLog "[Gambol sync] ok"
    let si, pollEffs = tryStartLoadFetch model
    { model with
        syncInfo = si
        lastCmdResult = Some (CmdLastResult.Ok None) },
    pollEffs

let syncScopeFromFocus (model: VM) : Result<WorkspaceSyncScope, string> =
    match model.selectedNodes with
    | None -> Error "select a workspace, directory, or file"
    | Some sel ->
        let nodeId = focusedNodeId model.graph sel
        WorkspaceSyncScope.tryFromFocus model.graph nodeId

let private inventoryToStubItems (items: DesktopInventoryItem list) : WorkspaceUploadStructure.InventoryItem list =
    items
    |> List.map (fun i ->
        { relative = i.relative
          isDirectory = i.isDirectory })

let private reconcileWorkspaceAck
    (submitted: PendingChange)
    (ack: ChangeSuccessResponse)
    (graph: Graph)
    (history: ClientHistory)
    (revision: Revision)
    : AckReconcile =
    let state: ClientSyncState =
        { graph = graph
          history = history
          revision = revision }
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ submitted ]
            syncState = Sending 1 }
    if
        ack.externalChanges
        || not (SyncLogic.isConfirmationEcho [ submitted ] ack.changes)
    then
        SyncLogic.reconcileExternalAck
            [ submitted ]
            ack.revision
            state
            syncInfo
    else
        SyncLogic.reconcileAck
            [ submitted ]
            ack.changes
            ack.revision
            state
            syncInfo

/// Apply + synchronous POST so server graph has the workspace before push/reconcile.
let applyAndPostSync (commandName: string) (change: Change) (model: VM) : Result<VM, string> =
    let clientState: ClientSyncState =
        { graph = model.graph
          revision = model.revision
          history = model.history }
    match SyncLogic.applyLocalChange commandName change clientState with
    | Error msg -> Error msg
    | Ok (nextState, submitted) ->
        let body =
            SyncBatch.toWireBatch model.revision.Value [ submitted ]
            |> encodePendingBatchBody
        let url = sprintf "/%s/changes" currentFile
        let status, text = postJsonSync url body (jsonHeaders ())
        if status < 200 || status >= 300 then
            Error(httpError status text)
        else
            match decodeChangeSuccessResponse text with
            | Error e -> Error e
            | Ok ack ->
                match
                    reconcileWorkspaceAck
                        submitted
                        ack
                        nextState.graph
                        nextState.history
                        model.revision
                with
                | AckReconcile.Applied (st, _, _, _) ->
                    Ok
                        { model with
                            graph = st.graph
                            history = st.history
                            revision = st.revision }
                | AckReconcile.Ignored ->
                    Ok
                        { model with
                            graph = nextState.graph
                            history = nextState.history }
                | AckReconcile.Rejected msg -> Error msg

/// Local graph only — stubs paint before structure POST / body push.
let private applyStructureLocally
    (commandName: string) (change: Change) (model: VM) : Result<VM * PendingChange, string> =
    let clientState: ClientSyncState =
        { graph = model.graph
          revision = model.revision
          history = model.history }
    match SyncLogic.applyLocalChange commandName change clientState with
    | Error msg -> Error msg
    | Ok (nextState, submitted) ->
        Ok (
            { model with
                graph = nextState.graph
                history = nextState.history },
            submitted)

let private markServerFilesPresent
    (scope: WorkspaceSyncScope)
    (paths: string list)
    (model: VM)
    : Result<VM, string> =
    let ops =
        WorkspaceUploadStructure.planServerFilePresentOps
            model.graph
            scope.label
            paths
    if ops.IsEmpty then
        Ok model
    else
        let change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = ops }
        applyAndPostSync (displayName Load) change model |> Result.map withSiteMap

let private createWorkspaceOnServer (ops: Op list) (model: VM) : Result<VM, string> =
    if ops.IsEmpty then
        Error "could not create workspace"
    else
        let change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = ops }
        applyAndPostSync (displayName Load) change model |> Result.map withSiteMap

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

let keepUploading (m: VM) =
    { m with
        syncInfo = SyncInfo.withSyncState Uploading m.syncInfo }

/// Start the inventory, stub, and push phases for an already-selected scope.
let startWorkspacePush
    (scope: WorkspaceSyncScope)
    (parseFileId: NodeId option)
    (model: VM)
    : VM * Effect list =
    keepUploading model,
    [ Effect.ContinueWorkspaceStubsThenPush(scope, parseFileId) ]

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

let withPathSyncRefresh
    (model: VM, effs: Effect list)
    : VM * Effect list =
    model, RequestWorkspacePathSyncSnapshot :: effs

/// After async directory reconcile: clear busy, decode, poll + path-sync refresh.
let completeDirectoryReconcile (text: string) (model: VM) : VM * Effect list =
    let model' = clearUploading model
    match decodeReconciliationDirectory text with
    | Ok n when n > 0 ->
        okDetailWithPoll model' $"reconciled with warnings ({n})"
        |> withPathSyncRefresh
    | Ok _ -> okWithPoll model' |> withPathSyncRefresh
    | Error e -> fail model' e |> withPathSyncRefresh

let failDirectoryReconcile (msg: string) (model: VM) : VM * Effect list =
    fail (clearUploading model) msg |> withPathSyncRefresh

let failDirectoryReconcileHttp
    (status: int)
    (body: string)
    (model: VM)
    : VM * Effect list =
    failDirectoryReconcile (httpError status body) model

/// After async workspace-push success: clear Uploading; parse only for single-file Upload.
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
        let uploaded =
            sync.uploadedPaths |> Option.defaultValue []
        match markServerFilesPresent scope (uploaded @ skipped) model' with
        | Error error -> failWorkspacePush error model'
        | Ok presentModel ->
            match parseFileId with
            | Some fileId ->
                parseFileOp
                    (WorkspaceUploadAction.DesktopPush(Some fileId))
                    fileId
                    presentModel
                |> withPathSyncRefresh
            | None ->
                okDetailWithPoll presentModel sync.detail
                |> withPathSyncRefresh
    | Ok { error = Some e } -> failWorkspacePush e model
    | Ok _ -> failWorkspacePush "request failed" model

/// Body for async POST `/_desktop/workspace-inventory`.
let encodeWorkspaceInventoryBody (scope: WorkspaceSyncScope) : string =
    encodeSyncScope scope

/// Undo local stubs if structure POST fails after optimistic apply.
let private undoLocalStructure (model: VM) : VM =
    let clientState: ClientSyncState =
        { graph = model.graph
          history = model.history
          revision = model.revision }
    match SyncLogic.applyLocalUndo (System.Guid.NewGuid()) clientState with
    | Some (Ok (nextState, _)) ->
        { model with
            graph = nextState.graph
            history = nextState.history }
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
            match applyStructureLocally (displayName Load) change model with
            | Error e -> fail (clearUploading model) e
            | Ok (model', submitted) ->
                keepUploading (withSiteMap model'),
                [ Effect.ContinuePostUploadStructure (submitted, scope, parseFileId) ]

/// Structure Change ACK: stamp + revision, then body push.
let completeUploadStructurePost
    (submitted: PendingChange)
    (scope: WorkspaceSyncScope)
    (parseFileId: NodeId option)
    (text: string)
    (model: VM)
    : VM * Effect list =
    match decodeChangeSuccessResponse text with
    | Error e -> failUploadStructurePost e model
    | Ok ack ->
        match
            reconcileWorkspaceAck
                submitted
                ack
                model.graph
                model.history
                model.revision
        with
        | AckReconcile.Applied (st, _, _, _) ->
            let model' =
                { model with
                    graph = st.graph
                    revision = st.revision }
                |> withSiteMap
                |> keepUploading
            model', [ Effect.ContinueWorkspacePush (scope, parseFileId) ]
        | AckReconcile.Ignored ->
            keepUploading (withSiteMap model),
            [ Effect.ContinueWorkspacePush (scope, parseFileId) ]
        | AckReconcile.Rejected msg ->
            fail
                (clearUploading
                    { model with
                        syncInfo =
                            SyncInfo.withSyncState ServerRejected model.syncInfo })
                msg

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
