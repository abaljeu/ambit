module Gambol.Client.UpdateSave

open Gambol.Client.JsInterop
open Gambol.Client.UpdateCodec
open Gambol.Client.UpdateHelpers
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

let private canSave (model: VM) =
    match model.serverCapabilities with
    | Some { canGitSave = true } -> true
    | _ -> false

let deskSaveUrl (fileName: string) =
    sprintf "/%s/save" fileName

/// Persist data-dir snapshot via the server Save endpoint.
let deskSaveOp (model: VM) : VM * Effect list =
    if not (canSave model) then
        model, []
    else
        model, [ ContinueDeskSave ]

let runDeskSaveWith
    (decodeResponse: string -> Result<GitSaveResponse, string>)
    log
    post
    fileName
    =
    post
        (deskSaveUrl fileName)
        (fun text ->
            match decodeResponse text with
            | Ok { ok = true; detail = detail } ->
                log ("[Gambol] save: " + detail)
            | Ok { error = Some err } ->
                log ("[Gambol] save failed: " + err)
            | Ok _ ->
                log "[Gambol] save failed: unknown response"
            | Error err ->
                log ("[Gambol] save decode failed: " + err))
        (fun status text ->
            log (
                "[Gambol] save HTTP "
                + string status
                + ": "
                + LogText.summarizeHttpBody 200 text))
        (fun () -> log "[Gambol] save network error")

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
