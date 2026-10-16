module Gambol.Client.UpdateWorkspaceLoad

open Gambol.Client.UpdateHelpers
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
    (subjectId: NodeId option)
    (model: VM)
    : VM * Effect list =
    let request = QueuedWorkspacePush(scope, subjectId)
    okDetail
        { model with
            syncInfo = SyncInfo.queueRequest request model.syncInfo }
        "load queued until current sync completes"

let private desktopSubject
    (fileOpt: NodeId option)
    (model: VM)
    : NodeId option =
    match fileOpt with
    | Some id -> Some id
    | None ->
        contextualTargetForModel model
        |> Option.map contextualTargetId

let private runDeskLoadAction
    (action: WorkspaceUploadAction)
    (model: VM)
    : VM * Effect list =
    match action with
    | WorkspaceUploadAction.DesktopPush fileOpt ->
        match syncScopeFromFocus model, desktopSubject fileOpt model with
        | Error msg, _ -> fail model msg
        | Ok _, None ->
            fail model "Load subject is not a Workspace, Directory, or File"
        | Ok scope, Some subjectId when WorkspaceUpload.canStart model.syncInfo ->
            startWorkspacePush scope (Some subjectId) model
        | Ok scope, Some subjectId ->
            queueWorkspacePush scope (Some subjectId) model
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
      holdStack = false
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

let private holdFor (plan: WorkspaceUploadAction option) =
    match plan with
    | Some (WorkspaceUploadAction.DesktopPush _) -> true
    | _ -> false

let private submitLoad
    (plan: WorkspaceUploadAction option)
    (request: LoadSaveCommandRequest)
    =
    SubmitLoadSaveCommand ({ request with holdStack = holdFor plan }, plan)

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
