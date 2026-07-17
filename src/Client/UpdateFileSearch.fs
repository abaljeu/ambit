module Gambol.Client.UpdateFileSearch

open Browser.Dom
open Browser.Types
open Gambol.Client.FileSearchDialog
open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.ViewModel

let private readFileSearchQueryInput () : string =
    let el = document.getElementById "file-search-dialog-input"
    if isNull el then ""
    else (el :?> HTMLInputElement).value

let private focusInsertPoint (sel: Selection) : FocusInsertPoint =
    { parentId = sel.range.parent.nodeId
      index = sel.range.endd }

let private withCmdError (msg: string) (model: VM) : VM * Effect list =
    { model with lastCmdResult = Some (CmdLastResult.Error (None, msg)) }, []

let private applyOpsChange (ops: Op list) (model: VM) : VM * Effect list =
    if ops.IsEmpty then
        model, []
    else
        let change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = ops }
        match applyAndPost change model with
        | Error _ -> withCmdError "could not apply" model
        | Ok (m, effects) -> withSiteMap m, effects

let private focusParentId (model: VM) : NodeId option =
    match model.selectedNodes with
    | None -> None
    | Some sel -> Some (focusedNodeId model.graph sel)

/// Op: Insert a file reference at the focus row from an existing workspaces file node.
let fileSearchPickExisting (fileNodeId: NodeId) (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let ops =
            FileNodeOps.planInsertFileRefAtFocus (focusInsertPoint sel) fileNodeId model.graph
        applyOpsChange ops model

let fileSearchCreateWorkspace (query: string) (model: VM) : VM * Effect list =
    let _, ops = FileNodeOps.planCreateWorkspace model.graph query
    applyOpsChange ops model

let fileSearchCreateFile (query: string) (model: VM) : VM * Effect list =
    match focusParentId model with
    | None -> withCmdError "no selection" model
    | Some parentId ->
        let _, ops = FileNodeOps.planCreateOwnedFile model.graph parentId query
        if ops.IsEmpty then withCmdError "cannot create file here" model
        else applyOpsChange ops model

let fileSearchCreateFolder (query: string) (model: VM) : VM * Effect list =
    match focusParentId model with
    | None -> withCmdError "no selection" model
    | Some parentId ->
        let _, ops = FileNodeOps.planCreateOwnedDirectory model.graph parentId query
        if ops.IsEmpty then withCmdError "cannot create folder here" model
        else applyOpsChange ops model

let runFileSearchSelectionOp (mode: Mode) (model: VM) : VM * Effect list =
    match mode with
    | FileSearchDialog s ->
        FileSearchDialog.rememberFileSearchQuery s.query
        let closed = { model with mode = s.returnTo }
        match closed.selectedNodes with
        | None -> model, []
        | Some sel ->
            let focusId = focusedNodeId closed.graph sel
            match
                ViewModelFileSearch.tryFileResultAtDisplayIndex
                    s.query focusId closed.graph s.selectedIndex
            with
            | None -> model, []
            | Some hit -> fileSearchPickExisting hit.nodeId closed
    | _ -> model, []

let runFileSearchNewWorkspaceOp (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        let query = readFileSearchQueryInput ()
        FileSearchDialog.rememberFileSearchQuery query
        let closed = { model with mode = s.returnTo }
        fileSearchCreateWorkspace query closed
    | _ -> model, []

let runFileSearchNewFileOp (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        let query = readFileSearchQueryInput ()
        FileSearchDialog.rememberFileSearchQuery query
        let closed = { model with mode = s.returnTo }
        fileSearchCreateFile query closed
    | _ -> model, []

let runFileSearchNewFolderOp (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        let query = readFileSearchQueryInput ()
        FileSearchDialog.rememberFileSearchQuery query
        let closed = { model with mode = s.returnTo }
        fileSearchCreateFolder query closed
    | _ -> model, []

let openFileSearchDialogOp = FileSearchDialog.openFileSearchDialogOp
