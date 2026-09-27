module Gambol.Client.UpdateWorkspaceLoad

open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateImport
open Gambol.Client.UpdateWorkspaceDesktop
open Gambol.Client.UpdateWorkspaceSync
open Gambol.Shared
open Gambol.Shared.CommandEntry
open Gambol.Shared.ViewModel

let private contextualTargetForModel (model: VM) =
    focusContextualTarget model

let private queueLoadRequest (model: VM) : VM * Effect list =
    okDetail
        { model with
            syncInfo = SyncInfo.queueRequest QueuedLoad model.syncInfo }
        (WorkspaceUpload.queueBlockedDetail model.syncInfo)

let private queueWorkspacePush
    (scope: WorkspaceSyncScope)
    (parseFileId: NodeId option)
    (model: VM)
    : VM * Effect list =
    let request = QueuedWorkspacePush(scope, parseFileId)
    okDetail
        { model with
            syncInfo = SyncInfo.queueRequest request model.syncInfo }
        "load queued until current sync completes"

let private runDeskLoadAction
    (action: WorkspaceUploadAction)
    (model: VM)
    : VM * Effect list =
    match action with
    | WorkspaceUploadAction.DesktopPush parseFileId ->
        match syncScopeFromFocus model with
        | Error msg -> fail model msg
        | Ok scope when WorkspaceUpload.canStart model.syncInfo ->
            startWorkspacePush scope parseFileId model
        | Ok scope ->
            queueWorkspacePush scope parseFileId model
    | WorkspaceUploadAction.CreateWorkspaceFromFolder when
        WorkspaceUpload.canStart model.syncInfo ->
        uploadCreateWorkspaceOp model
    | WorkspaceUploadAction.CreateWorkspaceFromFolder ->
        queueLoadRequest model
    | WorkspaceUploadAction.ReconcileServerDisk when
        WorkspaceUpload.canStartWeb model.syncInfo ->
        match syncScopeFromFocus model with
        | Error msg -> fail model msg
        | Ok scope ->
            let parsing =
                { model with
                    syncInfo =
                        SyncInfo.withSyncState Parsing model.syncInfo }
            let model', effs =
                okDetail parsing "reconciling server disk"
            model', effs @ [ Effect.ContinueDirectoryReconcile scope ]
    | (WorkspaceUploadAction.ParseServerDisk fileId as parseAction) when
        WorkspaceUpload.canStartWeb model.syncInfo ->
        parseFileOp parseAction fileId model
    | WorkspaceUploadAction.ReconcileServerDisk
    | WorkspaceUploadAction.ParseServerDisk _ ->
        queueLoadRequest model
    | WorkspaceUploadAction.Unavailable msg ->
        withResult
            model
            (CmdLastResult.Error(Some(displayName Load), msg))

/// Load command: desktop Upload when mapped; else graph-only from DataDir (web / unmapped).
let deskLoadOp (model: VM) : VM * Effect list =
    let targetIds = selectedLoadTargetIds model
    if
        ResidentProjection.selectionSpansMultipleWorkspaces
            model.graph
            targetIds
    then
        withResult
            model
            (CmdLastResult.Error(
                Some(displayName Load),
                "Load requires all selected targets in one Workspace"))
    else
        let canPush =
            DesktopCapabilities.canWorkspacePush model.desktopCapabilities
        let target = contextualTargetForModel model
        let hasMapping =
            match syncScopeFromFocus model with
            | Ok scope ->
                match lookupMappedPath scope.label with
                | Ok(Some _) -> true
                | _ -> false
            | Error _ -> false
        WorkspaceUpload.plan
            canPush
            hasMapping
            (focusIsWorkspaces model)
            target
        |> fun action -> runDeskLoadAction action model

let loadOpFor
    (prePick: LoadSavePrePick)
    (model: VM)
    : VM * Effect list =
    let targetIds = selectedLoadTargetIds model
    if
        ResidentProjection.selectionSpansMultipleWorkspaces
            model.graph
            targetIds
    then
        withResult
            model
            (CmdLastResult.Error(
                Some(displayName Load),
                "Load requires all selected targets in one Workspace"))
    else
        model,
        [ SubmitLoadSaveCommand
            (loadSaveCommandRequest
                LoadSaveOperation.Load
                prePick
                model) ]

let loadOp = loadOpFor LoadSavePrePick.Plain

let loadAvailable (model: VM) =
    WorkspaceUpload.isAvailable
        (DesktopCapabilities.canWorkspacePush model.desktopCapabilities)
        (focusIsWorkspaces model)
        (contextualTargetForModel model)
