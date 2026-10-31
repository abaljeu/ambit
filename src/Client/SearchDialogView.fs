module Gambol.Client.SearchDialogView

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Client.Controller
open Gambol.Client.SearchDialogChrome

let private queryDebounce = createTimeoutSlot ()

let private quietGap = createTimeoutSlot ()

let private wireOnce = createWireOnce ()

let private renderScroll : DialogRenderScroll<NodeId> = createDialogRenderScroll ()

let private dialogKeys = {
    closeDialog = Gambol.Client.SearchDialog.closeSearchDialogOp
    moveSelectionUp = Gambol.Client.SearchDialog.searchSelectUpOp
    moveSelectionDown = Gambol.Client.SearchDialog.searchSelectDownOp
    acceptSelection = fun m ->
        let name =
            match m.mode with
            | SearchDialog s -> Some s.invokedCommand
            | _ -> None
        withDiagnostic name (fun m ->
            Gambol.Client.SearchDialog.runSearchSelectionOp m.mode m) m
}

/// The quiet gap reuses the search settle delay. The spec names no duration.
let armQuietGap (query: string) (dispatch: Msg -> unit) : unit =
    quietGap.armTimeout dialogQueryDebounceMs (fun () ->
        dispatch (ApplyOp (Gambol.Client.SearchDialog.searchPictureEffects query)))

let cancelQuietGap () : unit = quietGap.clearTimeout ()

let private flushSearchQuery (input: HTMLInputElement) (dispatch: Msg -> unit) : unit =
    queryDebounce.clearTimeout ()
    dispatch (NodeSearchQuery input.value)

let private scheduleSearchQuery (input: HTMLInputElement) (dispatch: Msg -> unit) : unit =
    quietGap.clearTimeout ()
    queryDebounce.armTimeout dialogQueryDebounceMs (fun () ->
        dispatch (NodeSearchQuery input.value))

let private searchKeyAction (input: HTMLInputElement) (dispatch: Msg -> unit) : DialogKeyAction = {
    keys = dialogKeys
    flushQuery = fun () -> flushSearchQuery input dispatch
    dispatch = dispatch
}

let private wireSearchDialog (input: HTMLInputElement) (dispatch: Msg -> unit) : unit =
    wireOnce.runOnce (fun () ->
        wireDialogList {
            input = input
            resultsId = "search-dialog-results"
            onInput = fun () -> scheduleSearchQuery input dispatch
            onKey = handleDialogKey (searchKeyAction input dispatch)
            onNearEnd = fun () ->
                dispatch (ApplyOp Gambol.Client.SearchDialog.loadMoreSearchResultsOp)
            onResultClick = fun idx ->
                queryDebounce.clearTimeout ()
                dispatch (ApplyOp (fun m ->
                    match m.mode with
                    | SearchDialog s ->
                        Gambol.Client.SearchDialog.runSearchSelectionOp
                            (SearchDialog { s with selectedIndex = idx }) m
                    | _ -> m, []))
        })

/// Show or hide the node-search overlay and keep it in sync with query/results.
let renderSearchDialog (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.getElementById "search-dialog"
    if isNull container then () else

    match model.mode with
    | SearchDialog s ->
        let input =
            showDialogHeading container {
                contextId = "search-dialog-context"
                title = s.invokedCommand
                inputId = "search-dialog-input"
                query = s.query
            }
        let scrollSelection =
            renderScroll.scrollFor {
                query = s.query
                scope = model.zoomRoot
                graph = model.graph
                selectedIndex = s.selectedIndex
            }
        let items =
            Gambol.Client.SearchDialog.currentSearchResults model
            |> List.map (fun hit -> formatNamedDialogHit hit.name hit.text)
        renderDialogResultList container items s.selectedIndex scrollSelection
        wireSearchDialog input dispatch
    | _ ->
        queryDebounce.clearTimeout ()
        quietGap.clearTimeout ()
        renderScroll.resetScroll ()
        hideDialog container
