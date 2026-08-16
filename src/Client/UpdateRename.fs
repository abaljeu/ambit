module Gambol.Client.UpdateRename

open Browser.Dom
open Browser.Types
open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.CommandEntry
open Gambol.Shared.ViewModel

let private nameString (name: Filename) : string =
    match name with
    | Filename.Ok s -> s
    | _ -> ""

let private initialRenameValue (model: VM) : string =
    match model.selectedNodes with
    | None -> ""
    | Some sel ->
        let nodeId = focusedNodeId model.graph sel
        model.graph.nodes.[nodeId].name |> nameString

let openRenamePromptOp (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let nodeId = focusedNodeId model.graph sel
        if not (NodeRenameOps.isRenameAllowed model.graph nodeId) then
            let msg =
                match model.graph.nodes |> Map.tryFind nodeId with
                | Some { kind = Special Workspace } ->
                    "cannot rename a workspace"
                | _ -> "cannot rename this node"
            { model with lastCmdResult = Some (CmdLastResult.Error (None, msg)) }, []
        else
            { model with mode = RenamePrompt (model.mode, initialRenameValue model) }, []

let closeRenamePromptOp (model: VM) : VM * Effect list =
    match model.mode with
    | RenamePrompt (ret, _) -> { model with mode = ret }, []
    | _ -> model, []

let private readRenamePromptValue () : string =
    let el = document.getElementById "rename-prompt-input"
    if isNull el then ""
    else (el :?> HTMLInputElement).value

let submitRenamePromptOp (model: VM) : VM * Effect list =
    match model.mode, model.selectedNodes with
    | RenamePrompt (ret, _), Some sel ->
        let nodeId = focusedNodeId model.graph sel
        let newName = readRenamePromptValue ()
        let result = { model with mode = ret }
        match NodeRenameOps.planRenameNode model.graph nodeId newName with
        | Error msg ->
            { result with lastCmdResult = Some (CmdLastResult.Error (None, msg)) }, []
        | Ok (ops, _) ->
            if ops.IsEmpty then
                result, []
            else
                let change =
                    { id = model.revision.Value
                      changeId = System.Guid.NewGuid()
                      ops = ops }
                match applyAndPost (displayName Rename) change result with
                | Ok (m, effects) -> withSiteMap m, effects
                | Error msg ->
                    { result with lastCmdResult = Some (CmdLastResult.Error (None, msg)) }, []
    | _ -> model, []
