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
open Gambol.Client.UpdateExport
open Gambol.Shared.CommandDockLayout

// ---------------------------------------------------------------------------
// Command types
// ---------------------------------------------------------------------------

type CommandKeyScope =
    | SelectionOnly
    | EditingOnly
    | SelectionOrEditing

type CommandOp = unit -> Updater option

// ---------------------------------------------------------------------------
// Command metadata
// ---------------------------------------------------------------------------

type CommandCategory =
    | Primary
    | Navigate
    | EditText
    | MoveStructure
    | Clipboard
    | Format
    | FileIO

type CommandEntry = {
    name: string
    run: CommandOp
    keys: string list
    keyScope: CommandKeyScope
    category: CommandCategory
    surface: CommandDockSurface
    glyph: string option
}

let private cmd
        name run keys keyScope category surface glyph
        : CommandEntry =
    { name = name
      run = run
      keys = keys
      keyScope = keyScope
      category = category
      surface = surface
      glyph = glyph }

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

// ---------------------------------------------------------------------------
// Command registry
// ---------------------------------------------------------------------------

let editCommand =
    cmd "Edit node" (keyAlways startEditOp) [ "F2"; "Enter" ]
        SelectionOnly EditText NoButton (Some "\u270E")

let commandRegistry : CommandEntry list =
    [
      editCommand

      cmd "Split at cursor" splitAtCursor [ "Enter" ]
          EditingOnly EditText NoButton None

      cmd "Delete" (keyAlways deleteSelectionOp) [ "Delete"; "Backspace" ]
          SelectionOnly EditText PaletteOnly (Some "\u2421")

      cmd "Join with previous" handleBackspace [ "Backspace" ]
          EditingOnly EditText NoButton None

      cmd "Join with next" handleDelete [ "Delete" ]
          EditingOnly EditText NoButton None

      cmd "Cursor up" (keyAlways moveSelectionUp) [ "ArrowUp"; "," ]
          SelectionOnly Navigate NoButton (Some "\u2191")

      cmd "Cursor down" (keyAlways moveSelectionDown) [ "ArrowDown"; "o" ]
          SelectionOnly Navigate NoButton (Some "\u2193")

      cmd "Cursor fold left" (keyAlways arrowLeftSelectionNoFoldOp)
          [ "Shift+ArrowLeft"; "A" ] SelectionOnly Navigate NoButton None

      cmd "Cursor left to parent" (keyAlways arrowLeftSelectionOp)
          [ "ArrowLeft"; "a" ] SelectionOnly Navigate NoButton (Some "\u2190")

      cmd "Cursor unfold right" (keyAlways arrowRightSelectionOp)
          [ "Shift+ArrowRight"; "ArrowRight"; "e"; "E" ]
          SelectionOnly Navigate NoButton (Some "\u2192")

      cmd "Move to previous node" handleArrowLeft [ "ArrowLeft" ]
          EditingOnly EditText NoButton None

      cmd "Move to next node" handleArrowRight [ "ArrowRight" ]
          EditingOnly EditText NoButton None

      cmd "Selection up" (keyAlways (shiftArrowOp -1))
          [ "Shift+ArrowUp"; "<" ] SelectionOrEditing MoveStructure SelectTools
          (Some "\u2191")

      cmd "Selection down" (keyAlways (shiftArrowOp 1))
          [ "Shift+ArrowDown"; "O" ] SelectionOrEditing MoveStructure SelectTools
          (Some "\u2193")

      cmd "Edit cursor up" editMoveUp [ "ArrowUp" ]
          EditingOnly EditText NoButton None

      cmd "Edit cursor down" editMoveDown [ "ArrowDown" ]
          EditingOnly EditText NoButton None

      cmd "Move Up" (keyAlways moveNodeUpOp)
          [ "Alt+ArrowUp"; "Ctrl+ArrowUp" ] SelectionOrEditing MoveStructure MoveTools
          (Some "\u2191")

      cmd "Move Down" (keyAlways moveNodeDownOp)
          [ "Alt+ArrowDown"; "Ctrl+ArrowDown" ] SelectionOrEditing MoveStructure MoveTools
          (Some "\u2193")

      cmd "Cursor to Start" (keyAlways pageCursorLevelStartOp) [ "PageUp" ]
          SelectionOnly Navigate NoButton None

      cmd "Cursor to End" (keyAlways pageCursorLevelEndOp) [ "PageDown" ]
          SelectionOnly Navigate NoButton None

      cmd "Move Selection to Start" (keyAlways moveSelectionToLevelStartOp)
          [ "Alt+PageUp" ] SelectionOrEditing MoveStructure MoveTools (Some "\u21A4")

      cmd "Move Selection to End" (keyAlways moveSelectionToLevelEndOp)
          [ "Alt+PageDown" ] SelectionOrEditing MoveStructure MoveTools (Some "\u21A6")

      cmd "Select to Start" (keyAlways shiftPgUpOp) [ "Shift+PageUp" ]
          SelectionOnly MoveStructure SelectTools (Some "\u21A4")

      cmd "Select to End" (keyAlways shiftPgDownOp) [ "Shift+PageDown" ]
          SelectionOnly MoveStructure SelectTools (Some "\u21A6")

      cmd "Cursor to Top of View" (keyAlways homeSelectionOp) [ "Home" ]
          SelectionOnly Navigate NoButton None

      cmd "Cursor to End of View" (keyAlways endSelectionOp) [ "End" ]
          SelectionOnly Navigate NoButton None

      cmd "Move Selection to Top of View" (keyAlways moveSelectionToViewRootStartOp)
          [ "Alt+Home" ] SelectionOrEditing MoveStructure PaletteOnly None

      cmd "Move Selection to End of View" (keyAlways moveSelectionToViewRootEndOp)
          [ "Alt+End" ] SelectionOrEditing MoveStructure PaletteOnly None

      cmd "Indent" (keyAlways indentOp) [ "Tab" ]
          SelectionOrEditing MoveStructure MoveTools (Some "\u21E5")

      cmd "Outdent" (keyAlways outdentOp) [ "Shift+Tab" ]
          SelectionOrEditing MoveStructure MoveTools (Some "\u21E4")

      cmd "Cancel" (keyAlways handleEsc) [ "Escape" ]
          SelectionOrEditing Navigate NoButton None

      cmd "Fold / unfold" (keyAlways toggleFoldSelectionOp) [ "Ctrl+." ]
          SelectionOrEditing Navigate NoButton None

      cmd "Zoom in" (keyAlways zoomInOp) [ "Ctrl+]"; "]" ]
          SelectionOrEditing Navigate Base (Some "]")

      cmd "Zoom out" (keyAlways zoomOutOp) [ "Ctrl+["; "[" ]
          SelectionOrEditing Navigate Base (Some "[")

      cmd "Undo" (keyAlways undoOp) [ "Ctrl+Z"; "z" ]
          SelectionOrEditing Primary Base (Some "\u21B6")

      cmd "Redo" (keyAlways redoOp) [ "Ctrl+Y"; "y" ]
          SelectionOrEditing Primary Base (Some "\u21B7")

      cmd "Copy content" (keyAlways copyOp) [ "c" ]
          SelectionOnly Clipboard MoreTools (Some "Copy")

      cmd "Copy as links" (keyAlways copySelectionAsLinks) [ "Ctrl+C"; "C" ]
          SelectionOnly Clipboard PaletteOnly None

      cmd "Duplicate (link)" (keyAlways duplicateSelectionOp) [ "D" ]
          SelectionOnly Clipboard MoreTools (Some "Dup")

      cmd "Command palette" (keyAlways openCommandPaletteOp) [ "Ctrl+P"; "p" ]
          SelectionOrEditing Primary MoreTools (Some "P")

      cmd "Move Selected" (keyAlways moveNodesOp) [ "m"; "Ctrl+m" ]
          SelectionOrEditing MoveStructure MoveTools (Some "M")

      cmd "Find" (keyAlways findRootOp) [ "/"; "Ctrl+f" ]
          SelectionOrEditing Primary Base (Some "/")

      cmd "Edit classes" (keyAlways openCssClassPromptOp) [ "Alt+C"; "." ]
          SelectionOrEditing Format MoreTools (Some "Class")

      cmd "Jump to Target" (keyAlways jumpTargetOp) [ "Alt+j"; "j" ]
          SelectionOrEditing Navigate Base (Some "\u2197")

      cmd "Import" (keyAlways importLocalOp) [ "Ctrl+Shift+>" ]
          SelectionOrEditing FileIO PaletteOnly None

      cmd "Export" (keyAlways exportLocalOp) [ "Ctrl+Shift+<" ]
          SelectionOrEditing FileIO PaletteOnly None
    ]

// ---------------------------------------------------------------------------
// Palette and button filtering
// ---------------------------------------------------------------------------

let rec paletteWasSelecting (returnTo: Mode) : bool =
    match returnTo with
    | Selecting -> true
    | Editing _ -> false
    | CommandPalette (_, _, inner) -> paletteWasSelecting inner
    | SearchDialog s -> paletteWasSelecting s.returnTo
    | CssClassPrompt (inner, _) -> paletteWasSelecting inner

let private inKeyScope (sel: bool) (scope: CommandKeyScope) : bool =
    match scope with
    | SelectionOnly -> sel
    | EditingOnly -> not sel
    | SelectionOrEditing -> true

let commandContextMode (mode: Mode) : Mode =
    match mode with
    | CommandPalette (_, _, ret) -> ret
    | SearchDialog s -> s.returnTo
    | CssClassPrompt (inner, _) -> inner
    | m -> m

let commandsForPalette (returnTo: Mode) : CommandEntry list =
    let sel = paletteWasSelecting returnTo
    commandRegistry |> List.filter (fun c -> inKeyScope sel c.keyScope)

let filteredCommands (returnTo: Mode) (query: string) : CommandEntry list =
    let baseList = commandsForPalette returnTo
    if query = "" then baseList
    else
        let q = query.ToLowerInvariant()
        baseList |> List.filter (fun c -> c.name.ToLowerInvariant().Contains(q))

let tryFindCommand (name: string) : CommandEntry option =
    commandRegistry |> List.tryFind (fun c -> c.name = name)
