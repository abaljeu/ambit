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

let saveOpFor
    (prePick: LoadSavePrePick)
    (model: VM)
    : VM * Effect list =
    if spansWorkspaces model then
        oneWorkspaceError model
    else
        model,
        [ SubmitLoadSaveCommand (
            loadSaveCommandRequest
                LoadSaveOperation.Save
                prePick
                model,
            None) ]

let saveOp = saveOpFor LoadSavePrePick.Plain
