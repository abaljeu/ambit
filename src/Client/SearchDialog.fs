module Gambol.Client.SearchDialog

open Gambol.Shared
open Gambol.Shared.ViewModel

let openSearchDialogOp (model: VM) : VM * Effect list =
    { model with mode = SearchDialog ("", 0, model.mode) }, []

let closeSearchDialogOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog (_, _, ret) -> { model with mode = ret }, []
    | _ -> model, []

let searchSelectUpOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog (q, selectedResult, ret) ->
        { model with mode = SearchDialog (q, max 0 (selectedResult - 1), ret) }, []
    | _ -> model, []

let searchSelectDownOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog (q, selectedResult, ret) ->
        { model with mode = SearchDialog (q, selectedResult + 1, ret) }, []
    | _ -> model, []

let searchSetQueryOp (query: string) (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog (_, _, ret) -> { model with mode = SearchDialog (query, 0, ret) }, []
    | _ -> model, []

let currentSearchResults (model: VM) : ViewModelSearch.NodeSearchResult list =
    match model.mode with
    | SearchDialog (query, _, _) -> ViewModelSearch.searchNodes query model.graph
    | _ -> []

let runSearchSelectionOp (model: VM) : VM * Effect list =
    match model.mode with
    | SearchDialog (_, selectedResult, ret) ->
        match List.tryItem selectedResult (currentSearchResults model) with
        | None -> { model with mode = ret }, []
        | Some hit ->
            ViewModelSearch.selectNodeFromSearch hit.nodeId model, []
    | _ -> model, []
