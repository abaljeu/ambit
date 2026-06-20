module Gambol.Client.FileSearchDialogView

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Controller
open Gambol.Client.JsInterop
open Gambol.Client.Update
open Gambol.Client.UpdateFileSearch
open Gambol.Shared.CommandCategory

let private scrollIntoViewNearest (el: HTMLElement) : unit =
    let o = Fable.Core.JsInterop.createEmpty<ScrollIntoViewOptions>
    o.block <- ScrollAlignment.Nearest
    el.scrollIntoView o

let private formatHitLabel (hit: FileSearchResult) : string =
    if not (System.String.IsNullOrWhiteSpace hit.pathLabel) then
        hit.pathLabel
    else
        match hit.name with
        | Filename.Ok s -> $"${s}  {hit.text}"
        | _ -> hit.text

let private renderFileSearchResults
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

let private handleFileSearchKey (ke: KeyboardEvent) (dispatch: Msg -> unit) : unit =
    let keyStr = formatKeyCombo ke
    let named label op =
        ke.preventDefault()
        dispatch (ApplyOp (withDiagnostic keyStr label op))
    match keyStr with
    | "Escape"    -> named "Close file search" Gambol.Client.FileSearchDialog.closeFileSearchDialogOp
    | "ArrowUp"   -> named "Select previous result" Gambol.Client.FileSearchDialog.fileSearchSelectUpOp
    | "ArrowDown" -> named "Select next result" Gambol.Client.FileSearchDialog.fileSearchSelectDownOp
    | "Enter"     ->
        ke.preventDefault()
        dispatch (ApplyOp (withDiagnostic keyStr "Choose file" (fun m ->
            runFileSearchSelectionOp m.mode m)))
    | _           -> ()

let private dockAccentClasses =
    [ "amb-dock-base"; "amb-dock-move"; "amb-dock-select"; "amb-dock-more"; "amb-dock-file" ]

let private setDockAccent (el: HTMLElement) (cls: string) : unit =
    dockAccentClasses |> List.iter (fun c -> el.classList.remove c)
    el.classList.add cls

let private fileSearchDialogWired = ref false

/// Show or hide the file-search overlay and keep it in sync with query/results.
let renderFileSearchDialog (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.getElementById "file-search-dialog"
    if isNull container then () else

    match model.mode with
    | FileSearchDialog s ->
        let wasOpen = container.classList.contains "amb-dialog-open"
        container.classList.add "amb-dialog-open"
        let ctx = document.getElementById "file-search-dialog-context"
        if not (isNull ctx) then
            ctx.textContent <- "Insert…"
            setDockAccent ctx (searchDialogDockCssClass "Insert…")
        let input = document.getElementById "file-search-dialog-input" :?> HTMLInputElement
        if input.value <> s.query then input.value <- s.query
        if not wasOpen then
            window.setTimeout((fun _ ->
                focusPreventScroll input
                input.select()), 0) |> ignore
        let items =
            Gambol.Client.FileSearchDialog.currentFileSearchResults model
            |> List.map formatHitLabel
        renderFileSearchResults container items s.selectedIndex

        let wsBtn = document.getElementById "file-search-dialog-new-workspace" :?> HTMLButtonElement
        let fileBtn = document.getElementById "file-search-dialog-new-file" :?> HTMLButtonElement
        let folderBtn = document.getElementById "file-search-dialog-new-folder" :?> HTMLButtonElement
        let showWs = Gambol.Client.FileSearchDialog.insertDialogFocusIsWorkspaces model
        let showFileFolder = Gambol.Client.FileSearchDialog.insertDialogShowsFileFolder model
        let setButtonVisible (btn: HTMLButtonElement) (visible: bool) =
            if visible then btn.removeAttribute "hidden"
            else btn.setAttribute("hidden", "")
        setButtonVisible wsBtn showWs
        setButtonVisible fileBtn showFileFolder
        setButtonVisible folderBtn showFileFolder

        if not fileSearchDialogWired.Value then
            fileSearchDialogWired.Value <- true
            let ul = document.getElementById "file-search-dialog-results"

            input.addEventListener("input", fun _ ->
                dispatch (ApplyOp (Gambol.Client.FileSearchDialog.fileSearchSetQueryOp input.value)))

            input.addEventListener("keydown", fun (ev: Event) ->
                let ke = ev :?> KeyboardEvent
                if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
                    ev.preventDefault()
                handleFileSearchKey ke dispatch)

            ul.addEventListener("click", fun (ev: Event) ->
                let target = ev.target :?> HTMLElement
                match target.closest "li" with
                | None -> ()
                | Some li ->
                    let idxStr = (li :?> HTMLElement).dataset.["idx"]
                    let idx = if System.String.IsNullOrEmpty idxStr then 0 else int idxStr
                    dispatch (ApplyOp (fun m ->
                        match m.mode with
                        | FileSearchDialog s ->
                            runFileSearchSelectionOp
                                (FileSearchDialog { s with selectedIndex = idx }) m
                        | _ -> m, [])))

            wsBtn.addEventListener("click", fun _ ->
                dispatch (ApplyOp (withDiagnostic "" "New workspace" runFileSearchNewWorkspaceOp)))

            fileBtn.addEventListener("click", fun _ ->
                dispatch (ApplyOp (withDiagnostic "" "New file" runFileSearchNewFileOp)))

            folderBtn.addEventListener("click", fun _ ->
                dispatch (ApplyOp (withDiagnostic "" "New folder" runFileSearchNewFolderOp)))
    | _ ->
        container.classList.remove "amb-dialog-open"
