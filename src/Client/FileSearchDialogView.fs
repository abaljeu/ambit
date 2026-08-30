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
open Gambol.Shared.CommandEntry

let private insertCommandName = Some (displayName InsertFile)

let private fileSearchDebounceMs = 50

let private scrollIntoViewNearest (el: HTMLElement) : unit =
    let o = Fable.Core.JsInterop.createEmpty<ScrollIntoViewOptions>
    o.block <- ScrollAlignment.Nearest
    el.scrollIntoView o

let private formatHitLabel (hit: FileSearchResult) : string =
    if not (System.String.IsNullOrWhiteSpace hit.pathLabel) then
        hit.pathLabel
    else
        match hit.name with
        | Filename.Ok s -> $"{s}  {hit.text}"
        | _ -> hit.text

let private renderFileSearchResults
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

let private clearFileSearchDebounce () : unit =
    match debounceTimer.Value with
    | None -> ()
    | Some id ->
        window.clearTimeout id |> ignore
        debounceTimer.Value <- None

let private flushFileSearchQuery (input: HTMLInputElement) (dispatch: Msg -> unit) : unit =
    clearFileSearchDebounce ()
    dispatch (FileSearchQuery input.value)

let private scheduleFileSearchQuery (input: HTMLInputElement) (dispatch: Msg -> unit) : unit =
    clearFileSearchDebounce ()
    debounceTimer.Value <-
        Some(
            window.setTimeout(
                (fun _ ->
                    debounceTimer.Value <- None
                    dispatch (FileSearchQuery input.value)),
                fileSearchDebounceMs))

let private handleFileSearchKey
    (input: HTMLInputElement)
    (ke: KeyboardEvent)
    (dispatch: Msg -> unit)
    : unit =
    let keyStr = formatKeyCombo ke
    let overlay op =
        ke.preventDefault()
        flushFileSearchQuery input dispatch
        dispatch (ApplyOp op)
    match keyStr with
    | "Escape"    -> overlay Gambol.Client.FileSearchDialog.closeFileSearchDialogOp
    | "ArrowUp"   -> overlay Gambol.Client.FileSearchDialog.fileSearchSelectUpOp
    | "ArrowDown" -> overlay Gambol.Client.FileSearchDialog.fileSearchSelectDownOp
    | "Enter"     ->
        ke.preventDefault()
        flushFileSearchQuery input dispatch
        dispatch (ApplyOp (withDiagnostic insertCommandName (fun m ->
            runFileSearchSelectionOp m.mode m)))
    | _           -> ()

let private dockAccentClasses =
    [ "amb-dock-base"; "amb-dock-move"; "amb-dock-select"; "amb-dock-more"; "amb-dock-file" ]

let private setDockAccent (el: HTMLElement) (cls: string) : unit =
    dockAccentClasses |> List.iter (fun c -> el.classList.remove c)
    el.classList.add cls

let private fileSearchDialogWired = ref false

type private FileSearchRenderKey =
    { query: string
      focusNodeId: NodeId option
      graph: Graph
      selectedIndex: int }

let mutable private lastFileSearchRenderKey: FileSearchRenderKey option = None

let private fileSearchRenderKeyMatches (left: FileSearchRenderKey) (right: FileSearchRenderKey) : bool =
    left.query = right.query
    && left.focusNodeId = right.focusNodeId
    && left.selectedIndex = right.selectedIndex
    && LanguagePrimitives.PhysicalEquality left.graph right.graph

let private focusNodeIdOpt (model: VM) : NodeId option =
    match model.selectedNodes with
    | None -> None
    | Some sel -> Some (focusedNodeId model.graph sel)

let private wireFileSearchDialog (input: HTMLInputElement) (dispatch: Msg -> unit) : unit =
    if not fileSearchDialogWired.Value then
        fileSearchDialogWired.Value <- true
        let ul = document.getElementById "file-search-dialog-results"

        input.addEventListener("input", fun _ ->
            scheduleFileSearchQuery input dispatch)

        input.addEventListener("keydown", fun (ev: Event) ->
            let ke = ev :?> KeyboardEvent
            if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
                ev.preventDefault()
            handleFileSearchKey input ke dispatch)

        ul.addEventListener("scroll", fun _ ->
            let nearEnd = ul.scrollTop + ul.clientHeight >= ul.scrollHeight - 48.0
            if nearEnd then
                dispatch (ApplyOp Gambol.Client.FileSearchDialog.loadMoreFileSearchResultsOp))

        ul.addEventListener("click", fun (ev: Event) ->
            let target = ev.target :?> HTMLElement
            match target.closest "li" with
            | None -> ()
            | Some li ->
                clearFileSearchDebounce ()
                let idxStr = (li :?> HTMLElement).dataset.["idx"]
                let idx = if System.String.IsNullOrEmpty idxStr then 0 else int idxStr
                dispatch (ApplyOp (fun m ->
                    match m.mode with
                    | FileSearchDialog s ->
                        runFileSearchSelectionOp
                            (FileSearchDialog { s with selectedIndex = idx }) m
                    | _ -> m, [])))

        let wsBtn = document.getElementById "file-search-dialog-new-workspace" :?> HTMLButtonElement
        let fileBtn = document.getElementById "file-search-dialog-new-file" :?> HTMLButtonElement
        let folderBtn = document.getElementById "file-search-dialog-new-folder" :?> HTMLButtonElement

        wsBtn.addEventListener("click", fun _ ->
            clearFileSearchDebounce ()
            dispatch (ApplyOp (withDiagnostic insertCommandName runFileSearchNewWorkspaceOp)))

        fileBtn.addEventListener("click", fun _ ->
            clearFileSearchDebounce ()
            dispatch (ApplyOp (withDiagnostic insertCommandName runFileSearchNewFileOp)))

        folderBtn.addEventListener("click", fun _ ->
            clearFileSearchDebounce ()
            dispatch (ApplyOp (withDiagnostic insertCommandName runFileSearchNewFolderOp)))

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
        if not wasOpen || input.value <> s.query then input.value <- s.query
        if not wasOpen then
            window.setTimeout((fun _ ->
                focusPreventScroll input
                input.select()), 0) |> ignore
        let renderKey =
            { query = s.query
              focusNodeId = focusNodeIdOpt model
              graph = model.graph
              selectedIndex = s.selectedIndex }
        let scrollSelection =
            lastFileSearchRenderKey
            |> Option.exists (fileSearchRenderKeyMatches renderKey)
            |> not
        lastFileSearchRenderKey <- Some renderKey
        let items =
            Gambol.Client.FileSearchDialog.currentFileSearchResults model
            |> List.map formatHitLabel
        renderFileSearchResults container items s.selectedIndex scrollSelection

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

        wireFileSearchDialog input dispatch
    | _ ->
        clearFileSearchDebounce ()
        lastFileSearchRenderKey <- None
        container.classList.remove "amb-dialog-open"
