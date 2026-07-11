module Gambol.Client.Commands

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Shared
open Gambol.Shared.Paste
open Gambol.Shared.ViewModel
open Gambol.Client.JsInterop
open Gambol.Client.Update
open Gambol.Client.UpdateEdit
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateOps
open Gambol.Client.UpdatePaste
open Gambol.Client.UpdateImport
open Gambol.Client.UpdateSave
open Gambol.Client.UpdateWorkspaceGit
open Gambol.Client.UpdateFileSearch
open Gambol.Client.UpdateRename
open Gambol.Client.UpdateAmbleRun
open Gambol.Shared.CommandDockLayout
open Gambol.Shared.CommandEntry

// ---------------------------------------------------------------------------
// Command types
// ---------------------------------------------------------------------------

type CommandOp = unit -> Updater option

type CommandEntry2 = {
    id: CommandId
    run: CommandOp
}

let private cmd (id: CommandId) (run: CommandOp) : CommandEntry2 =
    { id = id; run = run }

// ---------------------------------------------------------------------------
// Editing command ops (read live caret from DOM)
// ---------------------------------------------------------------------------

let private keyAlways (updater: Updater) : CommandOp = fun () -> Some updater

let private splitAtCursor () : Updater option =
    let text = readEditInputValue ()
    let pos = readEditInputCursor ()
    Some (splitNodeOp text pos)

let private editMoveUp () : Updater option =
    let el = document.getElementById "edit-input"
    if isNull el || not (isContentEditableCaretOnVisualFirstLine el) then
        None
    else
        match getContentEditableCaretClientX el with
        | None -> None
        | Some x -> Some (moveEditUpAtClientX x)

let private editMoveDown () : Updater option =
    let el = document.getElementById "edit-input"
    if isNull el || not (isContentEditableCaretOnVisualLastLine el) then
        None
    else
        match getContentEditableCaretClientX el with
        | None -> None
        | Some x -> Some (moveEditDownAtClientX x)

let private handleBackspace () : Updater option =
    if readEditInputCursor () = 0 && readEditInputSelectionEnd () = 0 then
        Some (joinWithPrevious (readEditInputValue ()))
    else None

let private handleDelete () : Updater option =
    let v = readEditInputValue ()
    if readEditInputSelectionEnd () = v.Length && readEditInputCursor () = v.Length then
        Some (joinWithNext v)
    else None

let private handleArrowLeft () : Updater option =
    if readEditInputCursor () = 0 && readEditInputSelectionEnd () = 0 then
        Some (moveEditUp System.Int32.MaxValue)
    else None

let private handleArrowRight () : Updater option =
    let v = readEditInputValue ()
    let len = v.Length
    if readEditInputCursor () = len && readEditInputSelectionEnd () = len then
        Some (moveEditDown 0)
    else None

let private stripHtmlToText (html: string) : string =
    let d = document.createElement "div"
    d.innerHTML <- html
    let t = d.innerText
    let s =
        if System.String.IsNullOrEmpty t then d.textContent else t
    if isNull s then "" else s.Trim()

let private plainTextForOpenTarget (raw: string) : string =
    if raw.IndexOf '<' >= 0 then stripHtmlToText raw else raw

let private firstChildPlainOpt (graph: Graph) (focusId: NodeId) : string option =
    Map.tryFind focusId graph.nodes
    |> Option.bind (fun node ->
        List.tryHead node.children
        |> Option.bind (fun ch -> Map.tryFind ch.id graph.nodes)
        |> Option.map (fun n -> plainTextForOpenTarget n.text))

let jumpTargetOp (model: VM) : VM * Effect list =
    let focusId =
        match model.selectedNodes with
        | None -> viewRootNodeId model
        | Some sel -> focusedNodeId model.graph sel

    let primaryRaw =
        match model.mode with
        | Editing _ -> readEditInputValue ()
        | _ -> model.graph.nodes.[focusId].text

    let primaryPlain = plainTextForOpenTarget primaryRaw
    let childPlain = firstChildPlainOpt model.graph focusId

    match OpenTarget.tryFindOpenableUriWithFirstChildFallback primaryPlain childPlain with
    | None -> model, []
    | Some url ->
        openUrlInNewTab url
        model, []

[<Emit("navigator.clipboard.writeText($0).then(function(){ $1(); }).catch(function(e){ console.error('Clipboard write failed:', e); })")>]
let private writeClipboardText (text: string) (continuation: unit -> unit) : unit = jsNative

let private nodeIdsPrefix = "x-gambol-nodeids:"

let copySelectionAsLinks (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
        let selectedIds =
            parentNode.children
            |> List.skip sel.range.start
            |> List.take (sel.range.endd - sel.range.start)
        let idsText =
            selectedIds
            |> List.map (fun child -> child.id)
            |> List.map (fun (NodeId guid) -> guid.ToString())
            |> String.concat "\n"
        writeClipboardText (nodeIdsPrefix + "\n" + idsText) ignore
        copySelectionOp model

let copyOp (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
        let selectedIds =
            parentNode.children
            |> List.skip sel.range.start
            |> List.take (sel.range.endd - sel.range.start)
            |> List.map (fun child -> child.id)
        let serialized = serializeSubtree model.graph model.siteMap selectedIds
        writeClipboardText serialized ignore
        copySelectionOp model

let private contextualTargetForModel (model: VM) =
    model.selectedNodes
    |> Option.bind (fun selection ->
        contextualTarget
            model.graph
            selection.range.parent.nodeId
            selection.focus)

let parseOrPushOp (model: VM) : VM * Effect list =
    match contextualTargetForModel model with
    | Some(ParseFile fileId) -> parseUnparsedFileOp fileId model
    | Some(PushWorkspace _) -> gitPushOp model
    | None -> model, []

let pullWorkspaceOp (model: VM) : VM * Effect list =
    match contextualTargetForModel model with
    | Some(PushWorkspace _) -> gitPullOp model
    | _ -> model, []

let private contextualCommandAvailable (model: VM) =
    match contextualTargetForModel model with
    | Some(ParseFile _) ->
        match model.desktopCapabilities with
        | Some { file = { canImport = true } } -> true
        | _ -> false
    | Some(PushWorkspace _) ->
        WorkspaceGitRemote.canDesktopGit model.desktopCapabilities
    | None -> false

// ---------------------------------------------------------------------------
// Command registry
// ---------------------------------------------------------------------------

let commandRegistry : CommandEntry2 list =
    [
      cmd EditNode (keyAlways startEditOp)
      cmd Rename (keyAlways openRenamePromptOp)
      cmd Exec (keyAlways runAmbleOp)
      cmd SplitAtCursor splitAtCursor
      cmd Delete (keyAlways deleteSelectionOp)
      cmd JoinWithPrevious handleBackspace
      cmd JoinWithNext handleDelete
      cmd CursorUp (keyAlways moveSelectionUp)
      cmd CursorDown (keyAlways moveSelectionDown)
      cmd CursorFoldLeft (keyAlways arrowLeftSelectionNoFoldOp)
      cmd CursorLeftToParent (keyAlways arrowLeftSelectionOp)
      cmd CursorUnfoldRight (keyAlways arrowRightSelectionOp)
      cmd MoveToPreviousNode handleArrowLeft
      cmd MoveToNextNode handleArrowRight
      cmd SelectionUp (keyAlways (shiftArrowOp -1))
      cmd SelectionDown (keyAlways (shiftArrowOp 1))
      cmd EditCursorUp editMoveUp
      cmd EditCursorDown editMoveDown
      cmd MoveUp (keyAlways moveNodeUpOp)
      cmd MoveDown (keyAlways moveNodeDownOp)
      cmd CursorToStart (keyAlways pageCursorLevelStartOp)
      cmd CursorToEnd (keyAlways pageCursorLevelEndOp)
      cmd MoveSelectionToStart (keyAlways moveSelectionToLevelStartOp)
      cmd MoveSelectionToEnd (keyAlways moveSelectionToLevelEndOp)
      cmd SelectToStart (keyAlways shiftPgUpOp)
      cmd SelectToEnd (keyAlways shiftPgDownOp)
      cmd CursorToTopOfView (keyAlways homeSelectionOp)
      cmd CursorToEndOfView (keyAlways endSelectionOp)
      cmd MoveSelectionToTopOfView (keyAlways moveSelectionToViewRootStartOp)
      cmd MoveSelectionToEndOfView (keyAlways moveSelectionToViewRootEndOp)
      cmd Indent (keyAlways indentOp)
      cmd Outdent (keyAlways outdentOp)
      cmd Escape (keyAlways handleEsc)
      cmd FoldUnfold (keyAlways toggleFoldSelectionOp)
      cmd ZoomIn (keyAlways zoomInOp)
      cmd ZoomOut (keyAlways zoomOutOp)
      cmd ZoomOwner (keyAlways zoomOwnerOp)
      cmd Undo (keyAlways undoOp)
      cmd Redo (keyAlways redoOp)
      cmd CopyContent (keyAlways copyOp)
      cmd CopyAsLinks (keyAlways copySelectionAsLinks)
      cmd DupNodes (keyAlways duplicateSelectionOp)
      cmd InsertFile (keyAlways openFileSearchDialogOp)
      cmd CommandPalette (keyAlways openCommandPaletteOp)
      cmd MoveSelected (keyAlways moveNodesOp)
      cmd Find (keyAlways findRootOp)
      cmd EditClasses (keyAlways openCssClassPromptOp)
      cmd JumpToTarget (keyAlways jumpTargetOp)
      cmd ParseOrPush (keyAlways parseOrPushOp)
      cmd Save (keyAlways gitSaveOp)
      cmd GitConnect (keyAlways gitConnectOp)
      cmd GitClone (keyAlways gitCloneOp)
      cmd GitPull (keyAlways pullWorkspaceOp)
      cmd GitPush (keyAlways gitPushOp)
      cmd GitStatus (keyAlways gitStatusOp)
    ]

// ---------------------------------------------------------------------------
// Palette and button filtering
// ---------------------------------------------------------------------------

let rec paletteWasSelecting (returnTo: Mode) : bool =
    match returnTo with
    | Selecting -> true
    | Editing _ -> false
    | Mode.CommandPalette (_, _, inner) -> paletteWasSelecting inner
    | SearchDialog s -> paletteWasSelecting s.returnTo
    | FileSearchDialog s -> paletteWasSelecting s.returnTo
    | CssClassPrompt (inner, _) -> paletteWasSelecting inner
    | RenamePrompt (inner, _) -> paletteWasSelecting inner

let commandContextMode (mode: Mode) : Mode =
    match mode with
    | Mode.CommandPalette (_, _, ret) -> ret
    | SearchDialog s -> s.returnTo
    | FileSearchDialog s -> s.returnTo
    | CssClassPrompt (inner, _) -> inner
    | RenamePrompt (inner, _) -> inner
    | m -> m

let private isDesktopGitCommand (id: CommandId) =
    match id with
    | GitConnect | GitClone | GitPull | GitPush | GitStatus -> true
    | _ -> false

let commandsForPalette (model: VM) (returnTo: Mode) : CommandEntry2 list =
    let sel = paletteWasSelecting returnTo
    let canGit = WorkspaceGitRemote.canDesktopGit model.desktopCapabilities
    commandRegistry
    |> List.filter (fun c ->
        match commandFor c.id with
        | None -> false
        | Some e ->
            inKeyScope sel e.keyScope
            && (canGit || not (isDesktopGitCommand c.id))
            && (c.id <> ParseOrPush || contextualCommandAvailable model)
            && (c.id <> GitPull
                || (canGit
                    && match contextualTargetForModel model with
                       | Some(PushWorkspace _) -> true
                       | _ -> false)))

let filteredCommands (model: VM) (returnTo: Mode) (query: string) : CommandEntry2 list =
    let baseList = commandsForPalette model returnTo
    if query = "" then baseList
    else
        let q = query.ToLowerInvariant()
        baseList
        |> List.filter (fun c ->
            displayName c.id |> fun n -> n.ToLowerInvariant().Contains q)

let tryFindCommand (id: CommandId) : CommandEntry2 option =
    commandRegistry |> List.tryFind (fun c -> c.id = id)
