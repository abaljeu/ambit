module Gambol.Client.Controller

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Shared
open Gambol.Shared.Paste

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

/// Handle a paste event: extract plain text (from HTML or plain), dispatch PasteNodes.
/// Prefer text/plain — code editors (VS Code, etc.) always supply it with real newlines.
/// Fall back to stripping text/html only when plain is absent (e.g. browser-page copy).
let onPaste (ev: Event) (dispatch: Msg -> unit) : unit =
    ev.preventDefault()
    let plain = getClipboardData ev "text/plain"
    let text  = if plain <> "" then plain else stripHtmlToText (getClipboardData ev "text/html")
    if text <> "" then dispatch (User (PasteNodes text))

let private onCopyOrCut (model: VM) (ev: Event) (dispatch: Msg -> unit) (msg: Msg) : unit =
    match model.selectedNodes with
    | None -> ()
    | Some sel ->
        ev.preventDefault()
        let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
        let selectedIds =
            parentNode.children
            |> List.skip sel.range.start
            |> List.take (sel.range.endd - sel.range.start)
        let serialized = serializeSubtree model.graph model.siteMap selectedIds
        setClipboardData ev "text/plain" serialized
        dispatch msg

/// Handle a copy event in select mode: serialize the visible selected subtree to the
/// system clipboard and dispatch CopySelection so update can store model.clipboard.
let onCopy (model: VM) (ev: Event) (dispatch: Msg -> unit) : unit =
    onCopyOrCut model ev dispatch (User CopySelection)

/// Handle a cut event in select mode: same as copy, then dispatch CutSelection
/// so update removes the nodes and adjusts the selection.
let onCut (model: VM) (ev: Event) (dispatch: Msg -> unit) : unit =
    onCopyOrCut model ev dispatch (User CutSelection)

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let app = document.getElementById "app"

/// Check if a key string represents a single printable character
let isPrintableKey (key: string) : bool =
    key.Length = 1 && key >= " "

type SelectionKeyContext = { keyEvent: KeyboardEvent; selectedNodeText: string }

type EditingKeyContext = { keyEvent: KeyboardEvent; editInput: HTMLInputElement }

/// Result returned by a key handler. `Some msg` causes preventDefault + dispatch;
/// `None` lets the browser process the event normally.
type KeyHandler<'Context> = 'Context -> Msg option

let printableKeyToken = "__PRINTABLE__"

let selectionKeyTable: (string * KeyHandler<SelectionKeyContext>) list =
        [ "F2",             (fun ctx -> Some (User (StartEdit ctx.selectedNodeText)))
          "Enter",          (fun ctx -> Some (User (StartEdit ctx.selectedNodeText)))
          "ArrowUp",        (fun _ ->   Some (User MoveSelectionUp))
          "ArrowDown",      (fun _ ->   Some (User MoveSelectionDown))
          "Shift+ArrowUp",  (fun _ ->   Some (User ShiftArrowUp))
          "Shift+ArrowDown",(fun _ ->   Some (User ShiftArrowDown))
          "Alt+ArrowUp",    (fun _ ->   Some (User MoveNodeUp))
          "Alt+ArrowDown",  (fun _ ->   Some (User MoveNodeDown))
          "Ctrl+ArrowUp",   (fun _ ->   Some (User MoveNodeUp))
          "Ctrl+ArrowDown", (fun _ ->   Some (User MoveNodeDown))
          "Tab",            (fun _ ->   Some (User IndentSelection))
          "Shift+Tab",      (fun _ ->   Some (User OutdentSelection))
          "Escape",         (fun _ ->   Some (User CancelEdit))
          "Ctrl+.",         (fun _ ->   Some (User ToggleFoldSelection))
          "Ctrl+z",         (fun _ ->   Some (User Undo))
          "Ctrl+y",         (fun _ ->   Some (User Redo))
          printableKeyToken,(fun ctx -> Some (User (StartEdit ctx.keyEvent.key))) ]

let handleBackspace (ctx: EditingKeyContext) : Msg option =
    if int ctx.editInput.selectionStart = 0 then Some (User (JoinWithPrevious ctx.editInput.value))
    else None

let handleDelete (ctx: EditingKeyContext) : Msg option =
    if int ctx.editInput.selectionStart = ctx.editInput.value.Length then Some (User (JoinWithNext ctx.editInput.value))
    else None

let handleArrowLeft (ctx: EditingKeyContext) : Msg option =
    if int ctx.editInput.selectionStart = 0 && int ctx.editInput.selectionEnd = 0 then Some (User MoveSelectionUp)
    else None

let handleArrowRight (ctx: EditingKeyContext) : Msg option =
    let len = ctx.editInput.value.Length
    if int ctx.editInput.selectionStart = len && int ctx.editInput.selectionEnd = len then Some (User MoveSelectionDown)
    else None

let editingKeyTable: (string * KeyHandler<EditingKeyContext>) list =
        [ "Enter",          (fun ctx -> Some (User (SplitNode (ctx.editInput.value, int ctx.editInput.selectionStart))))
          "Backspace",      handleBackspace
          "Delete",         handleDelete
          "ArrowLeft",      handleArrowLeft
          "ArrowRight",     handleArrowRight
          "Ctrl+ArrowLeft", handleArrowLeft
          "Ctrl+ArrowRight",handleArrowRight
          "ArrowUp",        (fun ctx -> Some (User (MoveEditUp   (int ctx.editInput.selectionStart))))
          "ArrowDown",      (fun ctx -> Some (User (MoveEditDown (int ctx.editInput.selectionStart))))
          "Alt+ArrowUp",    (fun _ ->   Some (User MoveNodeUp))
          "Alt+ArrowDown",  (fun _ ->   Some (User MoveNodeDown))
          "Ctrl+ArrowUp",   (fun _ ->   Some (User MoveNodeUp))
          "Ctrl+ArrowDown", (fun _ ->   Some (User MoveNodeDown))
          "Tab",            (fun _ ->   Some (User IndentSelection))
          "Shift+Tab",      (fun _ ->   Some (User OutdentSelection))
          "Escape",         (fun _ ->   Some (User CancelEdit))
          "Ctrl+.",         (fun _ ->   Some (User ToggleFoldSelection))
          "Ctrl+z",         (fun _ ->   Some (User Undo))
          "Ctrl+y",         (fun _ ->   Some (User Redo)) ]

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
    (dispatch: Msg -> unit)
    : unit =
    match tryResolveOperation table keyEvent with
    | None -> ()
    | Some handler ->
        match handler ctx with
        | Some msg ->
            keyEvent.preventDefault()
            dispatch msg
        | None -> ()
