module Gambol.Client.FileSearchDialogView

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Controller
open Gambol.Client.SearchDialogChrome
open Gambol.Client.UpdateFileSearch
open Gambol.Shared.CommandEntry

let private insertCommandName = Some (displayName InsertFile)

let private insertTitle = "Insert…"

let private queryDebounce = createTimeoutSlot ()

let private wireOnce = createWireOnce ()

let private renderScroll : DialogRenderScroll<NodeId option> = createDialogRenderScroll ()

let private dialogKeys = {
    closeDialog = Gambol.Client.FileSearchDialog.closeFileSearchDialogOp
    moveSelectionUp = Gambol.Client.FileSearchDialog.fileSearchSelectUpOp
    moveSelectionDown = Gambol.Client.FileSearchDialog.fileSearchSelectDownOp
    acceptSelection = fun m ->
        withDiagnostic insertCommandName (fun m ->
            runFileSearchSelectionOp m.mode m) m
}

let private flushFileSearchQuery (input: HTMLInputElement) (dispatch: Msg -> unit) : unit =
    queryDebounce.clearTimeout ()
    dispatch (FileSearchQuery input.value)

let private scheduleFileSearchQuery (input: HTMLInputElement) (dispatch: Msg -> unit) : unit =
    queryDebounce.armTimeout dialogQueryDebounceMs (fun () ->
        dispatch (FileSearchQuery input.value))

let private formatHitLabel (hit: FileSearchResult) : string =
    if not (System.String.IsNullOrWhiteSpace hit.pathLabel) then
        hit.pathLabel
    else
        formatNamedDialogHit hit.name hit.text

let private focusNodeIdOpt (model: VM) : NodeId option =
    match model.selectedNodes with
    | None -> None
    | Some sel -> Some (focusedNodeId model.graph sel)

let private fileSearchKeyAction
    (input: HTMLInputElement)
    (dispatch: Msg -> unit)
    : DialogKeyAction = {
    keys = dialogKeys
    flushQuery = fun () -> flushFileSearchQuery input dispatch
    dispatch = dispatch
}

let private setButtonVisible (btn: HTMLButtonElement) (visible: bool) : unit =
    if visible then btn.removeAttribute "hidden"
    else btn.setAttribute("hidden", "")

let private wireInsertButton (id: string) (op: Updater) (dispatch: Msg -> unit) : unit =
    let btn = document.getElementById id :?> HTMLButtonElement
    btn.addEventListener("click", fun _ ->
        queryDebounce.clearTimeout ()
        dispatch (ApplyOp (withDiagnostic insertCommandName op)))

let private showInsertButtons (model: VM) : unit =
    let wsBtn = document.getElementById "file-search-dialog-new-workspace" :?> HTMLButtonElement
    let fileBtn = document.getElementById "file-search-dialog-new-file" :?> HTMLButtonElement
    let folderBtn = document.getElementById "file-search-dialog-new-folder" :?> HTMLButtonElement
    let showWs = Gambol.Client.FileSearchDialog.insertDialogFocusIsWorkspaces model
    let showFileFolder = Gambol.Client.FileSearchDialog.insertDialogShowsFileFolder model
    setButtonVisible wsBtn showWs
    setButtonVisible fileBtn showFileFolder
    setButtonVisible folderBtn showFileFolder

let private wireFileSearchDialog (input: HTMLInputElement) (dispatch: Msg -> unit) : unit =
    wireOnce.runOnce (fun () ->
        wireDialogList {
            input = input
            resultsId = "file-search-dialog-results"
            onInput = fun () -> scheduleFileSearchQuery input dispatch
            onKey = handleDialogKey (fileSearchKeyAction input dispatch)
            onNearEnd = fun () ->
                dispatch (ApplyOp Gambol.Client.FileSearchDialog.loadMoreFileSearchResultsOp)
            onResultClick = fun idx ->
                queryDebounce.clearTimeout ()
                dispatch (ApplyOp (fun m ->
                    match m.mode with
                    | FileSearchDialog s ->
                        runFileSearchSelectionOp
                            (FileSearchDialog { s with selectedIndex = idx }) m
                    | _ -> m, []))
        }
        wireInsertButton "file-search-dialog-new-workspace" runFileSearchNewWorkspaceOp dispatch
        wireInsertButton "file-search-dialog-new-file" runFileSearchNewFileOp dispatch
        wireInsertButton "file-search-dialog-new-folder" runFileSearchNewFolderOp dispatch)

/// Show or hide the file-search overlay and keep it in sync with query/results.
let renderFileSearchDialog (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.getElementById "file-search-dialog"
    if isNull container then () else

    match model.mode with
    | FileSearchDialog s ->
        let input =
            showDialogHeading container {
                contextId = "file-search-dialog-context"
                title = insertTitle
                inputId = "file-search-dialog-input"
                query = s.query
            }
        let scrollSelection =
            renderScroll.scrollFor {
                query = s.query
                scope = focusNodeIdOpt model
                graph = model.graph
                selectedIndex = s.selectedIndex
            }
        let items =
            Gambol.Client.FileSearchDialog.currentFileSearchResults model
            |> List.map formatHitLabel
        renderDialogResultList container items s.selectedIndex scrollSelection
        showInsertButtons model
        wireFileSearchDialog input dispatch
    | _ ->
        queryDebounce.clearTimeout ()
        renderScroll.resetScroll ()
        hideDialog container
