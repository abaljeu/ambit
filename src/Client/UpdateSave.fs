module Gambol.Client.UpdateSave

open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateWorkspaceDownload
open Gambol.Client.UpdateWorkspaceSync
open Gambol.Shared
open Gambol.Shared.CommandEntry
open Gambol.Shared.ViewModel

let private spansWorkspaces (model: VM) =
    ResidentProjection.selectionSpansMultipleWorkspaces
        model.graph
        (selectedLoadTargetIds model)

let private oneWorkspaceError (model: VM) : VM * Effect list =
    withResult
        model
        (CmdLastResult.Error(
            Some(displayName Save),
            "Save requires all selected targets in one Workspace"))

/// Desk file send. A browser with no desktop app skips this send.
let deskSaveOp (model: VM) : VM * Effect list =
    if DesktopCapabilities.canWorkspaceSync model.desktopCapabilities then
        sendWorkspaceDownload model
    else
        model, []

let private mappingPresence (model: VM) : WorkspaceMappingPresence =
    let labels =
        model.workspaceRemotes
        |> Map.toList
        |> List.map fst
        |> Set.ofList
    PathPick.mappingPresence labels model.workspaceMappedLabels

let private saveFocusId (model: VM) : NodeId =
    match model.selectedNodes with
    | Some selection -> focusedNodeId model.graph selection
    | None -> model.zoomRoot

/// Desk when plain Save has a local mapping and no remote. Otherwise the actor.
let private prePickForSave
    (prePick: LoadSavePrePick)
    (model: VM)
    : LoadSavePrePick =
    match
        PathPick.plainSavePath
            model.graph
            (saveFocusId model)
            prePick
            (mappingPresence model)
    with
    | Some LoadSavePath.Desk -> LoadSavePrePick.Desk
    | _ -> prePick

let saveOpFor
    (prePick: LoadSavePrePick)
    (model: VM)
    : VM * Effect list =
    if spansWorkspaces model then
        oneWorkspaceError model
    else
        let prePick = prePickForSave prePick model
        model,
        [ SubmitLoadSaveCommand (
            loadSaveCommandRequest
                LoadSaveOperation.Save
                prePick
                model,
            None) ]

let saveOp = saveOpFor LoadSavePrePick.Plain
