module Gambol.Client.Controller

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Shared
open Gambol.Shared.Paste
open Gambol.Client.Update

// ---------------------------------------------------------------------------
// Clipboard / paste helpers
// ---------------------------------------------------------------------------

/// Strip HTML tags to plain text via a temporary DOM element.
/// Block elements (p, div, br, tr, li, td) become newlines via innerText.
[<Emit("(function(h){var d=document.createElement('div');d.innerHTML=h;return(d.innerText||d.textContent||'').trim();})(" + "$0" + ")")>]
let stripHtmlToText (html: string) : string = jsNative

/// Read a named format from a paste ClipboardEvent's clipboardData.
[<Emit("$0.clipboardData.getData($1)")>]
let getClipboardData (ev: Event) (format: string) : string = jsNative

/// Write a named format to a copy/cut ClipboardEvent's clipboardData.
[<Emit("$0.clipboardData.setData($1,$2)")>]
let setClipboardData (ev: Event) (format: string) (data: string) : unit = jsNative

/// Get the character offset at a given client (x, y) position using the browser's caret APIs.
[<Emit("(function(x,y){if(document.caretRangeFromPoint){var r=document.caretRangeFromPoint(x,y);return r?r.startOffset:0;}if(document.caretPositionFromPoint){var p=document.caretPositionFromPoint(x,y);return p?p.offset:0;}return 0;})($0,$1)")>]
let getCaretOffset (x: float) (y: float) : int = jsNative

/// Handle a paste event: extract plain text, apply pasteNodesOp.
let onPaste (ev: Event) (applyOp: Op -> unit) : unit =
    ev.preventDefault()
    let plain = getClipboardData ev "text/plain"
    let text  = if plain <> "" then plain else stripHtmlToText (getClipboardData ev "text/html")
    if text <> "" then applyOp (pasteNodesOp text)

let private onCopyOrCut (model: VM) (ev: Event) (applyOp: Op -> unit) (op: Op) : unit =
    match model.selectedNodes with
    | None -> ()
    | Some sel ->
        ev.preventDefault()
        let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
        let selectedIds =
            parentNode.children
            |> List.skip sel.range.start
            |> List.take (sel.range.endd - sel.range.start)
        let serialized =
            if model.linkPasteEnabled then
                // Write raw NodeId GUIDs so paste-reference can find the existing nodes
                selectedIds
                |> List.map (fun (NodeId guid) -> guid.ToString())
                |> String.concat "\n"
            else
                serializeSubtree model.graph model.siteMap selectedIds
        setClipboardData ev "text/plain" serialized
        applyOp op

/// Handle a copy event: serialize the selected subtree to the clipboard.
let onCopy (model: VM) (ev: Event) (applyOp: Op -> unit) : unit =
    onCopyOrCut model ev applyOp copySelectionOp

/// Handle a cut event: serialize and remove the selected subtree.
let onCut (model: VM) (ev: Event) (applyOp: Op -> unit) : unit =
    onCopyOrCut model ev applyOp cutSelectionOp

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let app = document.getElementById "app"

/// Check if a key string represents a single printable character
let isPrintableKey (key: string) : bool =
    key.Length = 1 && key >= " "

type SelectionKeyContext = { keyEvent: KeyboardEvent; selectedNodeText: string }

type EditingKeyContext = { keyEvent: KeyboardEvent; editInput: HTMLInputElement }

/// A key handler returns an Op to apply, or None to let the browser handle the event.
type KeyHandler<'Context> = 'Context -> Op option

let printableKeyToken = "__PRINTABLE__"

// ---------------------------------------------------------------------------
// Selection key handlers
// ---------------------------------------------------------------------------

/// Lift an Op into any key-handler context (for keys with fixed, context-free behaviour).
let private always (op: Op) (_: 'ctx) : Op option = Some op

let private startEditFromSelection (ctx: SelectionKeyContext) : Op option =
    Some (startEdit ctx.selectedNodeText)

let private startEditFromKey (ctx: SelectionKeyContext) : Op option =
    Some (startEdit ctx.keyEvent.key)

let selectionKeyTable: (string * KeyHandler<SelectionKeyContext>) list =
    [ "F2",              startEditFromSelection
      "Enter",           startEditFromSelection
      "Delete",          always deleteSelectionOp
      "ArrowUp",         always moveSelectionUp
      "ArrowDown",       always moveSelectionDown
      "Shift+ArrowUp",   always (shiftArrowOp -1)
      "Shift+ArrowDown", always (shiftArrowOp  1)
      "Alt+ArrowUp",     always moveNodeUpOp
      "Alt+ArrowDown",   always moveNodeDownOp
      "Ctrl+ArrowUp",    always moveNodeUpOp
      "Ctrl+ArrowDown",  always moveNodeDownOp
      "Tab",             always indentOp
      "Shift+Tab",       always outdentOp
      "Escape",          always cancelEdit
      "Ctrl+.",          always toggleFoldSelectionOp
      "Ctrl+]",          always zoomInOp
      "Ctrl+[",          always zoomOutOp
      "Ctrl+z",          always undoOp
      "Ctrl+y",          always redoOp
      printableKeyToken, startEditFromKey ]

// ---------------------------------------------------------------------------
// Editing key handlers
// ---------------------------------------------------------------------------

let private splitAtCursor (ctx: EditingKeyContext) : Op option =
    let text = ctx.editInput.value
    let pos  = int ctx.editInput.selectionStart
    Some (splitNodeOp text pos)

let private editMoveUp (ctx: EditingKeyContext) : Op option =
    Some (moveEditUp ctx.editInput.selectionStart)

let private editMoveDown (ctx: EditingKeyContext) : Op option =
    Some (moveEditDown ctx.editInput.selectionStart)

let handleBackspace (ctx: EditingKeyContext) : Op option =
    if int ctx.editInput.selectionStart = 0 then
        Some (joinWithPrevious ctx.editInput.value)
    else None

let handleDelete (ctx: EditingKeyContext) : Op option =
    if int ctx.editInput.selectionStart = ctx.editInput.value.Length then
        Some (joinWithNext ctx.editInput.value)
    else None

let handleArrowLeft (ctx: EditingKeyContext) : Op option =
    if int ctx.editInput.selectionStart = 0 && int ctx.editInput.selectionEnd = 0 then
        Some (moveEditUp System.Int32.MaxValue)
    else None

let handleArrowRight (ctx: EditingKeyContext) : Op option =
    let len = ctx.editInput.value.Length
    if int ctx.editInput.selectionStart = len && int ctx.editInput.selectionEnd = len then
        Some (moveEditDown 0)
    else None

let editingKeyTable: (string * KeyHandler<EditingKeyContext>) list =
    [ "Enter",           splitAtCursor
      "Backspace",       handleBackspace
      "Delete",          handleDelete
      "ArrowLeft",       handleArrowLeft
      "ArrowRight",      handleArrowRight
      "Ctrl+ArrowLeft",  handleArrowLeft
      "Ctrl+ArrowRight", handleArrowRight
      "ArrowUp",         editMoveUp
      "ArrowDown",       editMoveDown
      "Alt+ArrowUp",     always moveNodeUpOp
      "Alt+ArrowDown",   always moveNodeDownOp
      "Ctrl+ArrowUp",    always moveNodeUpOp
      "Ctrl+ArrowDown",  always moveNodeDownOp
      "Tab",             always indentOp
      "Shift+Tab",       always outdentOp
      "Escape",          always cancelEdit
      "Ctrl+.",          always toggleFoldSelectionOp
      "Ctrl+]",          always zoomInOp
      "Ctrl+[",          always zoomOutOp
      "Ctrl+z",          always undoOp
      "Ctrl+y",          always redoOp ]

let tryResolveOperation
    (table: (string * KeyHandler<'Context>) list)
    (ke: KeyboardEvent)
    : KeyHandler<'Context> option =
    let tryKey k = table |> List.tryPick (fun (t, h) -> if t = k then Some h else None)
    // Try modifier-qualified keys first (more specific beats less specific)
    let qualified =
        if ke.altKey   then tryKey ("Alt+"   + ke.key) else
        if ke.shiftKey then tryKey ("Shift+" + ke.key) else
        if ke.ctrlKey  then tryKey ("Ctrl+"  + ke.key) else
        None
    match qualified with
    | Some _ -> qualified
    | None ->
        match tryKey ke.key with
        | Some handler -> Some handler
        | None ->
            if isPrintableKey ke.key && not ke.ctrlKey && not ke.metaKey && not ke.altKey then
                tryKey printableKeyToken
            else
                None

let handleKey
    (table: (string * KeyHandler<'Context>) list)
    (ctx: 'Context)
    (keyEvent: KeyboardEvent)
    (applyOp: Op -> unit)
    : unit =
    match tryResolveOperation table keyEvent with
    | None -> ()
    | Some handler ->
        match handler ctx with
        | Some op ->
            keyEvent.preventDefault()
            applyOp op
        | None -> ()
