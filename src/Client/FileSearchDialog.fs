module Gambol.Client.FileSearchDialog

open Gambol.Shared
open Gambol.Shared.ViewModel

let mutable lastFileSearchQuery = ""

let rememberFileSearchQuery (q: string) : unit =
    lastFileSearchQuery <- q

let private focusNodeIdOpt (model: VM) : NodeId option =
    match model.selectedNodes with
    | None -> None
    | Some sel -> Some (focusedNodeId model.graph sel)

let openFileSearchDialogOp (model: VM) : VM * Effect list =
    match focusNodeIdOpt model with
    | None -> model, []
    | Some _ ->
        { model with
            mode =
                FileSearchDialog
                    { query = lastFileSearchQuery
                      selectedIndex = 0
                      returnTo = model.mode } }, []

let closeFileSearchDialogOp (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        rememberFileSearchQuery s.query
        { model with mode = s.returnTo }, []
    | _ -> model, []

let fileSearchSelectUpOp (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        { model with
            mode =
                FileSearchDialog
                    { s with
                        selectedIndex = max 0 (s.selectedIndex - 1) } }, []
    | _ -> model, []

let fileSearchSelectDownOp (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        let n =
            match focusNodeIdOpt model with
            | None -> 0
            | Some focus ->
                ViewModelFileSearch.searchFiles s.query focus model.graph |> List.length
        let cap = max 0 (n - 1)
        let next = min (s.selectedIndex + 1) cap
        { model with mode = FileSearchDialog { s with selectedIndex = next } }, []
    | _ -> model, []

let fileSearchSetQueryOp (query: string) (model: VM) : VM * Effect list =
    match model.mode with
    | FileSearchDialog s ->
        { model with
            mode = FileSearchDialog { s with query = query; selectedIndex = 0 } }, []
    | _ -> model, []

let currentFileSearchResults (model: VM) : FileSearchResult list =
    match model.mode, focusNodeIdOpt model with
    | FileSearchDialog s, Some focus ->
        ViewModelFileSearch.searchFiles s.query focus model.graph
    | _ -> []

let isNewEnabled (model: VM) : bool =
    match model.mode, focusNodeIdOpt model with
    | FileSearchDialog s, Some focus ->
        FilePathResolve.isNewEnabled focus model.graph s.query
    | _ -> false
