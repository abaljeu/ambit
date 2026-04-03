module Gambol.Client.SearchDialogView

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Controller
open Gambol.Client.Update

let private scrollIntoViewNearest (el: HTMLElement) : unit =
    let o = Fable.Core.JsInterop.createEmpty<ScrollIntoViewOptions>
    o.block <- ScrollAlignment.Nearest
    el.scrollIntoView o

let private renderSearchResults
    (container: HTMLElement)
    (items: string list)
    (selectedResult: int)
    : unit =
    let ul = container.querySelector ".amb-palette-results" :?> HTMLElement
    ul.innerHTML <- ""
    let clampedSel = if items.IsEmpty then 0 else min selectedResult (items.Length - 1)
    let mutable selectedLi: Element option = None
    items
    |> List.iteri (fun i label ->
        let li = document.createElement "li"
        li.textContent <- label
        if i = clampedSel then
            li.classList.add "amb-palette-selected"
            selectedLi <- Some li
        ul.appendChild li |> ignore)
    selectedLi |> Option.iter (fun el -> scrollIntoViewNearest (el :?> HTMLElement))

let private searchDialogWired = ref false

/// Show or hide the node-search overlay and keep it in sync with query/results.
let renderSearchDialog (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.getElementById "search-dialog"
    if isNull container then () else

    match model.mode with
    | SearchDialog (q, selectedResult, _) ->
        container.classList.add "amb-palette-open"
        let input = document.getElementById "search-dialog-input" :?> HTMLInputElement
        window.setTimeout((fun _ -> input.focus()), 0) |> ignore
        let items =
            Gambol.Client.SearchDialog.currentSearchResults model
            |> List.map (fun hit ->
                match hit.name with
                | Some name -> $"${name}  {hit.text}"
                | None -> hit.text)
        renderSearchResults container items selectedResult

        if not searchDialogWired.Value then
            searchDialogWired.Value <- true
            let ul = document.getElementById "search-dialog-results"

            input.addEventListener("input", fun _ ->
                dispatch (ApplyOp (Gambol.Client.SearchDialog.searchSetQueryOp input.value)))

            input.addEventListener("keydown", fun (ev: Event) ->
                let ke = ev :?> KeyboardEvent
                if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
                    ev.preventDefault()
                handleSearchDialogKey ke dispatch)

            ul.addEventListener("click", fun (ev: Event) ->
                let target = ev.target :?> HTMLElement
                match target.closest "li" with
                | None -> ()
                | Some li ->
                    let lis = ul.querySelectorAll "li"
                    let mutable idx = 0
                    for i in 0 .. int lis.length - 1 do
                        if System.Object.ReferenceEquals(lis.[i], li) then idx <- i
                    dispatch (ApplyOp (fun m ->
                        match m.mode with
                        | SearchDialog (q, _, ret) ->
                            let m' = { m with mode = SearchDialog (q, idx, ret) }
                            Gambol.Client.SearchDialog.runSearchSelectionOp m'
                        | _ -> m, [])))
    | _ ->
        container.classList.remove "amb-palette-open"
