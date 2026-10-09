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

let private startDirectoryReconcile
    (scope: WorkspaceSyncScope)
    (model: VM)
    : VM * Effect list =
    let parsing =
        { model with
            syncInfo = SyncInfo.withSyncState Parsing model.syncInfo }
    let model', effs = okDetail parsing "reconciling server disk"
    model', effs @ [ Effect.ContinueDirectoryReconcile scope ]

let private loadFocusId (model: VM) =
    match model.selectedNodes with
    | Some selection -> focusedNodeId model.graph selection
    | None -> model.zoomRoot

let private parseFocusedFile (fileId: NodeId) (model: VM) =
    if WorkspaceUpload.canStartWeb model.syncInfo then
        parseFileOp
            (WorkspaceUploadAction.ParseServerDisk fileId)
            fileId
            model
    else
        fail model (WorkspaceUpload.queueBlockedDetail model.syncInfo)

/// Git Load after-step: parse a focused File; otherwise directory-match the Workspace.
let gitLoadAfterOp (model: VM) : VM * Effect list =
    match contextualTargetForModel model with
    | Some(ParseFile fileId) -> parseFocusedFile fileId model
    | _ ->
        match
            WorkspaceSyncScope.tryWorkspaceRootFromFocus
                model.graph
                (loadFocusId model)
        with
        | Error msg -> fail model msg
        | Ok scope when WorkspaceUpload.canStartWeb model.syncInfo ->
            startDirectoryReconcile scope model
        | Ok _ ->
            fail model (WorkspaceUpload.queueBlockedDetail model.syncInfo)

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
    | WorkspaceUploadAction.ReconcileServerDisk
    | WorkspaceUploadAction.ParseServerDisk _ ->
        // Mailbox Load already took this Workspace, Directory, or File.
        model, []
    | WorkspaceUploadAction.Unavailable msg ->
        withResult
            model
            (CmdLastResult.Error(Some(displayName Load), msg))

let private uploadPlan (model: VM) : WorkspaceUploadAction =
    let canPush =
        DesktopCapabilities.canWorkspacePush model.desktopCapabilities
    let hasMapping =
        match syncScopeFromFocus model with
        | Ok scope -> canCompareWorkspacePathSync model scope.label
        | Error _ -> false
    WorkspaceUpload.plan
        canPush
        hasMapping
        (focusIsWorkspaces model)
        (contextualTargetForModel model)

let private spansWorkspaces (model: VM) =
    ResidentProjection.selectionSpansMultipleWorkspaces
        model.graph
        (selectedLoadTargetIds model)

let private oneWorkspaceError (model: VM) : VM * Effect list =
    withResult
        model
        (CmdLastResult.Error(
            Some(displayName Load),
            "Load requires all selected targets in one Workspace"))

let private deskAfter
    (action: WorkspaceUploadAction)
    (model: VM)
    : VM * Effect list =
    if spansWorkspaces model then
        oneWorkspaceError model
    else
        runDeskLoadAction action model

/// Desktop Upload when mapped; else the desk after-step for DataDir.
let deskLoadOp (model: VM) : VM * Effect list =
    deskAfter (uploadPlan model) model

/// Desk after-step for a plan already chosen for this Load.
let deskLoadWith
    (action: WorkspaceUploadAction)
    (model: VM)
    : VM * Effect list =
    deskAfter action model

let private loadRequestFor
    (prePick: LoadSavePrePick)
    (nodeId: NodeId)
    (graphIds: NodeId list)
    (model: VM)
    =
    { operation = LoadSaveOperation.Load
      prePick = prePick
      start =
        { zoomId = model.zoomRoot
          focusId = nodeId
          commandId = nodeId
          graphIds = graphIds
          eventId = model.eventId } }

/// Whole selection. Explicit git Load and a desktop push stay one focus message.
let private subjectsForLoad
    (prePick: LoadSavePrePick)
    (plan: WorkspaceUploadAction option)
    (model: VM)
    : NodeId list =
    match prePick, plan with
    | LoadSavePrePick.Git, _ -> []
    | _, Some (WorkspaceUploadAction.DesktopPush _) -> []
    | _ ->
        WorkspaceUpload.loadSubjects
            model.graph
            (selectedLoadTargetIds model)

let private plannedUpload (prePick: LoadSavePrePick) (model: VM) =
    if prePick = LoadSavePrePick.Git then
        None
    else
        Some (uploadPlan model)

let private submitLoad
    (plan: WorkspaceUploadAction option)
    (request: LoadSaveCommandRequest)
    =
    SubmitLoadSaveCommand (request, plan)

let loadOpFor
    (prePick: LoadSavePrePick)
    (model: VM)
    : VM * Effect list =
    if spansWorkspaces model then
        oneWorkspaceError model
    else
        let plan = plannedUpload prePick model
        let targets = subjectsForLoad prePick plan model
        let graphIds =
            IncludedDescendantIds.throughChildrenOfExpandedNodes
                model.graph
                model.siteMap
                model.zoomRoot
        match targets with
        | [] ->
            model,
            [ submitLoad
                plan
                (loadSaveCommandRequest
                    LoadSaveOperation.Load
                    prePick
                    model) ]
        | ids ->
            model,
            ids
            |> List.map (fun nodeId ->
                submitLoad plan (loadRequestFor prePick nodeId graphIds model))

let loadOp = loadOpFor LoadSavePrePick.Plain

let loadAvailable (model: VM) =
    WorkspaceUpload.isAvailable
        (DesktopCapabilities.canWorkspacePush model.desktopCapabilities)
        (focusIsWorkspaces model)
        (contextualTargetForModel model)
