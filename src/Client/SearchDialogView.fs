module Gambol.Client.SearchDialogView

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Controller
open Gambol.Client.JsInterop
open Gambol.Client.Update
open Gambol.Shared.CommandCategory

let private scrollIntoViewNearest (el: HTMLElement) : unit =
    let o = Fable.Core.JsInterop.createEmpty<ScrollIntoViewOptions>
    o.block <- ScrollAlignment.Nearest
    el.scrollIntoView o

let private renderSearchResults
    (container: HTMLElement)
    (items: string list)
    (selectedIndex: int)
    : unit =
    let ul = container.querySelector ".amb-dialog-results" :?> HTMLElement
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
    selectedLi |> Option.iter (fun el -> scrollIntoViewNearest (el :?> HTMLElement))

let private handleSearchKey (ke: KeyboardEvent) (dispatch: Msg -> unit) : unit =
    let keyStr = formatKeyCombo ke
    let named op =
        ke.preventDefault()
        dispatch (ApplyOp (withDiagnostic op))
    match keyStr with
    | "Escape"    -> named Gambol.Client.SearchDialog.closeSearchDialogOp
    | "ArrowUp"   -> named Gambol.Client.SearchDialog.searchSelectUpOp
    | "ArrowDown" -> named Gambol.Client.SearchDialog.searchSelectDownOp
    | "Enter"     ->
        ke.preventDefault()
        dispatch (ApplyOp (withDiagnostic (fun m ->
            Gambol.Client.SearchDialog.runSearchSelectionOp m.mode m)))
    | _           -> ()

let private dockAccentClasses =
    [ "amb-dock-base"; "amb-dock-move"; "amb-dock-select"; "amb-dock-more"; "amb-dock-file" ]

let private setDockAccent (el: HTMLElement) (cls: string) : unit =
    dockAccentClasses |> List.iter (fun c -> el.classList.remove c)
    el.classList.add cls

let private searchDialogWired = ref false

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
        if input.value <> s.query then input.value <- s.query
        if not wasOpen then
            window.setTimeout((fun _ ->
                focusPreventScroll input
                input.select()), 0) |> ignore
        let items =
            Gambol.Client.SearchDialog.currentSearchResults model
            |> List.map (fun hit ->
                match hit.name with
                | Filename.Ok s -> $"{s}  {hit.text}"
                | _ -> hit.text)
        renderSearchResults container items s.selectedIndex

        if not searchDialogWired.Value then
            searchDialogWired.Value <- true
            let ul = document.getElementById "search-dialog-results"

            input.addEventListener("input", fun _ ->
                dispatch (ApplyOp (Gambol.Client.SearchDialog.searchSetQueryOp input.value)))

            input.addEventListener("keydown", fun (ev: Event) ->
                let ke = ev :?> KeyboardEvent
                if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
                    ev.preventDefault()
                handleSearchKey ke dispatch)

            ul.addEventListener("click", fun (ev: Event) ->
                let target = ev.target :?> HTMLElement
                match target.closest "li" with
                | None -> ()
                | Some li ->
                    let idxStr = (li :?> HTMLElement).dataset.["idx"]
                    let idx = if System.String.IsNullOrEmpty idxStr then 0 else int idxStr
                    dispatch (ApplyOp (fun m ->
                        match m.mode with
                        | SearchDialog s ->
                            Gambol.Client.SearchDialog.runSearchSelectionOp
                                (SearchDialog { s with selectedIndex = idx }) m
                        | _ -> m, [])))
    | _ ->
        container.classList.remove "amb-dialog-open"
