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
    if text <> "" then dispatch (PasteNodes text)

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
    onCopyOrCut model ev dispatch CopySelection

/// Handle a cut event in select mode: same as copy, then dispatch CutSelection
/// so update removes the nodes and adjusts the selection.
let onCut (model: VM) (ev: Event) (dispatch: Msg -> unit) : unit =
    onCopyOrCut model ev dispatch CutSelection

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let app = document.getElementById "app"

/// Check if a key string represents a single printable character
let isPrintableKey (key: string) : bool =
    key.Length = 1 && key >= " "

type SelectionKeyContext = { keyEvent: KeyboardEvent; selectedNodeText: string }

type EditingKeyContext = { keyEvent: KeyboardEvent; editInput: HTMLInputElement }

/// Result returned by a key handler. `Handled msg` causes preventDefault + dispatch;
/// `NotHandled` lets the browser process the event normally.
type KeyResult =
    | Handled of Msg
    | NotHandled

type KeyHandler<'Context> = 'Context -> KeyResult

let printableKeyToken = "__PRINTABLE__"

let selectionKeyTable: (string * KeyHandler<SelectionKeyContext>) list =
        [ "F2",             (fun ctx -> Handled (StartEdit ctx.selectedNodeText))
          "Enter",          (fun ctx -> Handled (StartEdit ctx.selectedNodeText))
          "ArrowUp",        (fun _ ->   Handled MoveSelectionUp)
          "ArrowDown",      (fun _ ->   Handled MoveSelectionDown)
          "Shift+ArrowUp",  (fun _ ->   Handled ShiftArrowUp)
          "Shift+ArrowDown",(fun _ ->   Handled ShiftArrowDown)
          "Alt+ArrowUp",    (fun _ ->   Handled MoveNodeUp)
          "Alt+ArrowDown",  (fun _ ->   Handled MoveNodeDown)
          "Ctrl+ArrowUp",   (fun _ ->   Handled MoveNodeUp)
          "Ctrl+ArrowDown", (fun _ ->   Handled MoveNodeDown)
          "Tab",            (fun _ ->   Handled IndentSelection)
          "Shift+Tab",      (fun _ ->   Handled OutdentSelection)
          "Escape",         (fun _ ->   Handled CancelEdit)
          "Ctrl+.",         (fun _ ->   Handled ToggleFoldSelection)
          printableKeyToken,(fun ctx -> Handled (StartEdit ctx.keyEvent.key)) ]

let handleBackspace (ctx: EditingKeyContext) : KeyResult =
    if int ctx.editInput.selectionStart = 0 then Handled (JoinWithPrevious ctx.editInput.value)
    else NotHandled

let editingKeyTable: (string * KeyHandler<EditingKeyContext>) list =
        [ "Enter",          (fun ctx -> Handled (SplitNode (ctx.editInput.value, int ctx.editInput.selectionStart)))
          "Backspace",      handleBackspace
          "ArrowUp",        (fun _ ->   Handled MoveSelectionUp)
          "ArrowDown",      (fun _ ->   Handled MoveSelectionDown)
          "Alt+ArrowUp",    (fun _ ->   Handled MoveNodeUp)
          "Alt+ArrowDown",  (fun _ ->   Handled MoveNodeDown)
          "Ctrl+ArrowUp",   (fun _ ->   Handled MoveNodeUp)
          "Ctrl+ArrowDown", (fun _ ->   Handled MoveNodeDown)
          "Tab",            (fun _ ->   Handled IndentSelection)
          "Shift+Tab",      (fun _ ->   Handled OutdentSelection)
          "Escape",         (fun _ ->   Handled CancelEdit)
          "Ctrl+.",         (fun _ ->   Handled ToggleFoldSelection) ]

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
        | Handled msg ->
            keyEvent.preventDefault()
            dispatch msg
        | NotHandled -> ()
