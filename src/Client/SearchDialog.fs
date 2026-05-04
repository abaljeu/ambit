module Gambol.Client.SearchDialog

open Gambol.Shared
open Gambol.Shared.ViewModel

let mutable lastNodeSearchQuery = ""

let private rememberSearchQuery (q: string) : unit =
    lastNodeSearchQuery <- q

let openSearchDialogWithOnPick
    (invokedCommand: string)
    (onPick: NodeSearchResult -> VM -> VM * Effect list)
    (model: VM)
    : VM * Effect list =
    { model with
        mode =
            SearchDialog
                { invokedCommand = invokedCommand
                  query = lastNodeSearchQuery
                  selectedIndex = 0
                  returnTo = model.mode
                  onPick = onPick } }, []

let closeSearchDialogOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog s ->
        rememberSearchQuery s.query
        { model with mode = s.returnTo }, []
    | _ -> model, []

let searchSelectUpOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog s ->
        { model with
            mode =
                SearchDialog
                    { s with
                        selectedIndex = max 0 (s.selectedIndex - 1) } }, []
    | _ -> model, []

let searchSelectDownOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog s ->
        let n =
            ViewModelSearch.searchNodes s.query model.zoomRoot model.graph
            |> List.length
        let cap = max 0 (n - 1)
        let next = min (s.selectedIndex + 1) cap
        { model with mode = SearchDialog { s with selectedIndex = next } }, []
    | _ -> model, []

let searchSetQueryOp (query: string) (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog s ->
        { model with
            mode = SearchDialog { s with query = query; selectedIndex = 0 } }, []
    | _ -> model, []

let currentSearchResults (model: VM) : NodeSearchResult list =
    match model.mode with
    | SearchDialog s -> ViewModelSearch.searchNodes s.query model.zoomRoot model.graph
    | _ -> []

let runSearchSelectionOp (mode: Mode) (model: VM) : VM * Effect list =
    match mode with
    | SearchDialog s ->
        rememberSearchQuery s.query
        let closed = { model with mode = s.returnTo }
        match ViewModelSearch.trySearchResultAtDisplayIndex s.query model.zoomRoot model.graph s.selectedIndex with
        | None -> model, []
        | Some hit -> s.onPick hit closed
    | _ -> model, []
