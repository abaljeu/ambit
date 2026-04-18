module Gambol.Client.SearchDialog

open Gambol.Shared
open Gambol.Shared.ViewModel

let openSearchDialogWithOnPick
    (onPick: NodeSearchResult -> VM -> VM * Effect list)
    (model: VM)
    : VM * Effect list =
    { model with mode = SearchDialog ("", 0, model.mode, onPick) }, []

let closeSearchDialogOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog (_, _, ret, _) -> { model with mode = ret }, []
    | _ -> model, []

let searchSelectUpOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog (q, selectedIndex, ret, onPick) ->
        { model with mode = SearchDialog (q, max 0 (selectedIndex - 1), ret, onPick) }, []
    | _ -> model, []

let searchSelectDownOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog (q, selectedIndex, ret, onPick) ->
        let n = ViewModelSearch.searchNodes q model.graph |> List.length
        let cap = max 0 (n - 1)
        let next = min (selectedIndex + 1) cap
        { model with mode = SearchDialog (q, next, ret, onPick) }, []
    | _ -> model, []

let searchSetQueryOp (query: string) (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog (_, _, ret, onPick) ->
        { model with mode = SearchDialog (query, 0, ret, onPick) }, []
    | _ -> model, []

let currentSearchResults (model: VM) : NodeSearchResult list =
    match model.mode with
    | SearchDialog (query, _, _, _) -> ViewModelSearch.searchNodes query model.graph
    | _ -> []

let runSearchSelectionOp (mode: Mode) (model: VM) : VM * Effect list =
    match mode with
    | SearchDialog (q, selectedIndex, ret, onPick) ->
        let closed = { model with mode = ret }
        match ViewModelSearch.trySearchResultAtDisplayIndex q model.graph selectedIndex with
        | None -> model, []
        | Some hit -> onPick hit closed
    | _ -> model, []
