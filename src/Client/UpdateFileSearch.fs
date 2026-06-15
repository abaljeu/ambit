module Gambol.Client.UpdateFileSearch

open Gambol.Client.FileSearchDialog
open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.ViewModel

let private focusInsertPoint (sel: Selection) : FocusInsertPoint =
    { parentId = sel.range.parent.nodeId
      index = sel.range.endd }

let private applyOpsChange (ops: Op list) (model: VM) : VM * Effect list =
    if ops.IsEmpty then
        model, []
    else
        let change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = ops }
        match applyAndPost change model with
        | None, _ -> model, []
        | Some m, effects -> withSiteMap m, effects

/// Op: Insert a file reference at the focus row from an existing workspaces file node.
let fileSearchPickExisting (fileNodeId: NodeId) (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let ops =
            FileNodeOps.planInsertFileRefAtFocus (focusInsertPoint sel) fileNodeId model.graph
        applyOpsChange ops model

/// Op: Create a workspaces file from a concrete path query and insert a ref at focus.
let fileSearchPickNew (query: string) (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let focusId = focusedNodeId model.graph sel
        let insert = focusInsertPoint sel
        match FilePathResolve.tryResolveConcreteTarget focusId model.graph query with
        | None -> model, []
        | Some target ->
            match FileNodeOps.planAddFileAtFocus model.graph insert target with
            | Error _ -> model, []
            | Ok (_, ops) -> applyOpsChange ops model

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

let runFileSearchNewOp (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        FileSearchDialog.rememberFileSearchQuery s.query
        let closed = { model with mode = s.returnTo }
        fileSearchPickNew s.query closed
    | _ -> model, []

let openFileSearchDialogOp = FileSearchDialog.openFileSearchDialogOp
