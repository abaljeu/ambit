module Gambol.Client.SearchDialogView

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Controller
open Gambol.Client.JsInterop
open Gambol.Client.Update
open Gambol.Shared.CommandCategory

let private searchDebounceMs = 50

let private scrollIntoViewNearest (el: HTMLElement) : unit =
    let o = Fable.Core.JsInterop.createEmpty<ScrollIntoViewOptions>
    o.block <- ScrollAlignment.Nearest
    el.scrollIntoView o

let private renderSearchResults
    (container: HTMLElement)
    (items: string list)
    (selectedIndex: int)
    (scrollSelection: bool)
    : unit =
    let ul = container.querySelector ".amb-dialog-results" :?> HTMLElement
    let previousScrollTop = ul.scrollTop
    ul.innerHTML <- ""
    let clampedSel = if items.IsEmpty then 0 else min selectedIndex (items.Length - 1)
    let mutable selectedLi: Element option = None
    items
    |> List.iteri (fun i label ->
        let li = document.createElement "li"
        li.textContent <- label
        li.dataset.["idx"] <- string i
        if i = clampedSel then
            li.classList.add "amb-dialog-selected"
            selectedLi <- Some li
        ul.appendChild li |> ignore)
    if scrollSelection then
        selectedLi |> Option.iter (fun el -> scrollIntoViewNearest (el :?> HTMLElement))
    else
        ul.scrollTop <- previousScrollTop

let private debounceTimer = ref (None: float option)

let private clearSearchDebounce () : unit =
    match debounceTimer.Value with
    | None -> ()
    | Some id ->
        window.clearTimeout id |> ignore
        debounceTimer.Value <- None

let private flushSearchQuery (input: HTMLInputElement) (dispatch: Msg -> unit) : unit =
    clearSearchDebounce ()
    dispatch (NodeSearchQuery input.value)

let private scheduleSearchQuery (input: HTMLInputElement) (dispatch: Msg -> unit) : unit =
    clearSearchDebounce ()
    debounceTimer.Value <-
        Some(
            window.setTimeout(
                (fun _ ->
                    debounceTimer.Value <- None
                    dispatch (NodeSearchQuery input.value)),
                searchDebounceMs))

let private handleSearchKey
    (input: HTMLInputElement)
    (ke: KeyboardEvent)
    (dispatch: Msg -> unit)
    : unit =
    let keyStr = formatKeyCombo ke
    let overlay op =
        ke.preventDefault()
        flushSearchQuery input dispatch
        dispatch (ApplyOp op)
    match keyStr with
    | "Escape"    -> overlay Gambol.Client.SearchDialog.closeSearchDialogOp
    | "ArrowUp"   -> overlay Gambol.Client.SearchDialog.searchSelectUpOp
    | "ArrowDown" -> overlay Gambol.Client.SearchDialog.searchSelectDownOp
    | "Enter"     ->
        ke.preventDefault()
        flushSearchQuery input dispatch
        dispatch (ApplyOp (fun m ->
            let name =
                match m.mode with
                | SearchDialog s -> Some s.invokedCommand
                | _ -> None
            withDiagnostic name (fun m ->
                Gambol.Client.SearchDialog.runSearchSelectionOp m.mode m) m))
    | _           -> ()

let private dockAccentClasses =
    [ "amb-dock-base"; "amb-dock-move"; "amb-dock-select"; "amb-dock-more"; "amb-dock-file" ]

let private setDockAccent (el: HTMLElement) (cls: string) : unit =
    dockAccentClasses |> List.iter (fun c -> el.classList.remove c)
    el.classList.add cls

let private searchDialogWired = ref false

type private SearchRenderKey =
    { query: string
      zoomRoot: NodeId
      graph: Graph
      selectedIndex: int }

let mutable private lastSearchRenderKey: SearchRenderKey option = None

let private searchRenderKeyMatches (left: SearchRenderKey) (right: SearchRenderKey) : bool =
    left.query = right.query
    && left.zoomRoot = right.zoomRoot
    && left.selectedIndex = right.selectedIndex
    && LanguagePrimitives.PhysicalEquality left.graph right.graph

let private wireSearchDialog (input: HTMLInputElement) (dispatch: Msg -> unit) : unit =
    if not searchDialogWired.Value then
        searchDialogWired.Value <- true
        let ul = document.getElementById "search-dialog-results"

        input.addEventListener("input", fun _ ->
            scheduleSearchQuery input dispatch)

        input.addEventListener("keydown", fun (ev: Event) ->
            let ke = ev :?> KeyboardEvent
            if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
                ev.preventDefault()
            handleSearchKey input ke dispatch)

        ul.addEventListener("scroll", fun _ ->
            let nearEnd = ul.scrollTop + ul.clientHeight >= ul.scrollHeight - 48.0
            if nearEnd then
                dispatch (ApplyOp Gambol.Client.SearchDialog.loadMoreSearchResultsOp))

        ul.addEventListener("click", fun (ev: Event) ->
            let target = ev.target :?> HTMLElement
            match target.closest "li" with
            | None -> ()
            | Some li ->
                clearSearchDebounce ()
                let idxStr = (li :?> HTMLElement).dataset.["idx"]
                let idx = if System.String.IsNullOrEmpty idxStr then 0 else int idxStr
                dispatch (ApplyOp (fun m ->
                    match m.mode with
                    | SearchDialog s ->
                        Gambol.Client.SearchDialog.runSearchSelectionOp
                            (SearchDialog { s with selectedIndex = idx }) m
                    | _ -> m, [])))

/// Show or hide the node-search overlay and keep it in sync with query/results.
let renderSearchDialog (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.getElementById "search-dialog"
    if isNull container then () else

    match model.mode with
    | SearchDialog s ->
        let wasOpen = container.classList.contains "amb-dialog-open"
        container.classList.add "amb-dialog-open"
        let ctx = document.getElementById "search-dialog-context"
        if not (isNull ctx) then
            ctx.textContent <- s.invokedCommand
            setDockAccent ctx (searchDialogDockCssClass s.invokedCommand)
        let input = document.getElementById "search-dialog-input" :?> HTMLInputElement
        if not wasOpen || input.value <> s.query then input.value <- s.query
        if not wasOpen then
            window.setTimeout((fun _ ->
                focusPreventScroll input
                input.select()), 0) |> ignore
        let renderKey =
            { query = s.query
              zoomRoot = model.zoomRoot
              graph = model.graph
              selectedIndex = s.selectedIndex }
        let scrollSelection =
            lastSearchRenderKey
            |> Option.exists (searchRenderKeyMatches renderKey)
            |> not
        lastSearchRenderKey <- Some renderKey
        let items =
            Gambol.Client.SearchDialog.currentSearchResults model
            |> List.map (fun hit ->
                match hit.name with
                | Filename.Ok s -> $"{s}  {hit.text}"
                | _ -> hit.text)
        renderSearchResults container items s.selectedIndex scrollSelection

        wireSearchDialog input dispatch
    | _ ->
        clearSearchDebounce ()
        lastSearchRenderKey <- None
        container.classList.remove "amb-dialog-open"
