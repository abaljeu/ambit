module Gambol.Client.View

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Shared
open Gambol.Shared.ViewModel
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

/// Handle a paste event: extract plain text (from HTML or plain), dispatch PasteNodes.
/// Prefer text/plain — code editors (VS Code, etc.) always supply it with real newlines.
/// Fall back to stripping text/html only when plain is absent (e.g. browser-page copy).
let onPaste (ev: Event) (dispatch: Msg -> unit) : unit =
    ev.preventDefault()
    let plain = getClipboardData ev "text/plain"
    let text  = if plain <> "" then plain else stripHtmlToText (getClipboardData ev "text/html")
    if text <> "" then dispatch (PasteNodes text)

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let app = document.getElementById "app"

/// Check if a key string represents a single printable character
let isPrintableKey (key: string) : bool =
    key.Length = 1 && key >= " "

type SelectionKeyContext = { keyEvent: KeyboardEvent; selectedNodeText: string }

type EditingKeyContext = { keyEvent: KeyboardEvent; editInput: HTMLInputElement }

type KeyHandler<'Context> = 'Context -> Msg

let printableKeyToken = "__PRINTABLE__"

let selectionKeyTable: (string * KeyHandler<SelectionKeyContext>) list =
        [ "F2", (fun ctx -> StartEdit ctx.selectedNodeText)
            ; "Enter", (fun ctx -> StartEdit ctx.selectedNodeText)
            ; "ArrowUp", (fun _ -> MoveSelectionUp)
            ; "ArrowDown", (fun _ -> MoveSelectionDown)
            ; "Shift+ArrowUp", (fun _ -> ShiftArrowUp)
            ; "Shift+ArrowDown", (fun _ -> ShiftArrowDown)
            ; "Alt+ArrowUp", (fun _ -> MoveNodeUp)
            ; "Alt+ArrowDown", (fun _ -> MoveNodeDown)
            ; "Ctrl+ArrowUp", (fun _ -> MoveNodeUp)
            ; "Ctrl+ArrowDown", (fun _ -> MoveNodeDown)
            ; "Tab", (fun _ -> IndentSelection)
            ; "Shift+Tab", (fun _ -> OutdentSelection)
            ; "Escape", (fun _ -> CancelEdit)
            ; printableKeyToken, (fun ctx -> StartEdit ctx.keyEvent.key) ]

let editingKeyTable: (string * KeyHandler<EditingKeyContext>) list =
        [ "Enter", (fun ctx -> SplitNode (ctx.editInput.value, int ctx.editInput.selectionStart))
            ; "ArrowUp", (fun _ -> MoveSelectionUp)
            ; "ArrowDown", (fun _ -> MoveSelectionDown)
            ; "Alt+ArrowUp", (fun _ -> MoveNodeUp)
            ; "Alt+ArrowDown", (fun _ -> MoveNodeDown)
            ; "Ctrl+ArrowUp", (fun _ -> MoveNodeUp)
            ; "Ctrl+ArrowDown", (fun _ -> MoveNodeDown)
            ; "Tab", (fun _ -> IndentSelection)
            ; "Shift+Tab", (fun _ -> OutdentSelection)
            ; "Escape", (fun _ -> CancelEdit) ]

let tryResolveOperation
    (table: (string * KeyHandler<'Context>) list)
    (ke: KeyboardEvent)
    : KeyHandler<'Context> option =
    let tryKey k = table |> List.tryPick (fun (t, h) -> if t = k then Some h else None)
    // Try modifier-qualified keys first (more specific beats less specific)
    let qualified =
        if ke.altKey   then tryKey ("Alt+"   + ke.key) else
        if ke.shiftKey then tryKey ("Shift+" + ke.key) else
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
        keyEvent.preventDefault()
        handler ctx |> dispatch

// ---------------------------------------------------------------------------
// Render
// ---------------------------------------------------------------------------

/// Render the full outline from the model.
/// Wires event handlers that call dispatch.
let rec render (model: Model) (dispatch: Msg -> unit) : unit =
    app.innerHTML <- ""

    let root = model.graph.nodes.[model.graph.root]
    for childId in root.children do
        renderNode model dispatch 0 childId false false

    // Hidden input — captures keystrokes in selection mode
    let hiddenInput = document.createElement "input"
    hiddenInput.id <- "hidden-input"
    hiddenInput.setAttribute("autocomplete", "off")
    hiddenInput.setAttribute("tabindex", "-1")
    hiddenInput.addEventListener("keydown", fun (ev: Event) ->
        let ke = ev :?> KeyboardEvent
        // Always prevent Tab/Shift+Tab from navigating browser focus
        if ke.key = "Tab" then ev.preventDefault()
        match model.selectedNodes with
        | None -> ()
        | Some sel ->
            let nodeId = focusedNodeId model.graph sel
            let nodeText = model.graph.nodes.[nodeId].text
            let ctx =
                { keyEvent = ke
                  selectedNodeText = nodeText }
            handleKey selectionKeyTable ctx ke dispatch
    )
    hiddenInput.addEventListener("paste", fun ev -> onPaste ev dispatch)
    app.appendChild hiddenInput |> ignore

    // Focus management
    match model.mode with
    | Editing _ ->
        let editInput = document.getElementById "edit-input"
        if not (isNull editInput) then
            (editInput :?> HTMLInputElement).focus()
            let inp = editInput :?> HTMLInputElement
            let pos =
                match model.mode with
                | Editing (_, Some p) -> p
                | _ -> inp.value.Length
            inp.setSelectionRange(pos, pos)
    | Selecting ->
        hiddenInput.focus()

and renderNode (model: Model) (dispatch: Msg -> unit) (depth: int) (nodeId: NodeId) (inSelectedSubtree: bool) (inFocusedSubtree: bool) : unit =
    let node = model.graph.nodes.[nodeId]
    let row = document.createElement "div"
    row.classList.add "row"

    // True when nodeId is one of the direct children in the NodeRange.
    let isDirectlySelected =
        match model.selectedNodes with
        | None -> false
        | Some sel ->
            let parentNode = model.graph.nodes.[sel.range.parent]
            parentNode.children
            |> List.indexed
            |> List.exists (fun (i, id) -> id = nodeId && i >= sel.range.start && i < sel.range.endd)

    // True when this node is the focus node (active end of the selection).
    let isFocusNode =
        match model.selectedNodes with
        | None -> false
        | Some sel -> focusedNodeId model.graph sel = nodeId

    // Highlight if directly selected or if an ancestor was directly selected.
    let isSelected = isDirectlySelected || inSelectedSubtree
    if isSelected then
        row.classList.add "selected"

    // Focus highlight on the focus node and its descendants.
    if isFocusNode || inFocusedSubtree then
        row.classList.add "focused"

    // Edit input is only shown for the focus node.
    let isEditingFocusNode = isFocusNode

    // Indentation
    for _ in 1 .. depth do
        let indent = document.createElement "div"
        indent.classList.add "indent"
        row.appendChild indent |> ignore

    // Content: either edit input or text div
    let isEditing = isEditingFocusNode && (match model.mode with Editing _ -> true | _ -> false)
    if isEditing then
        let editInput = document.createElement "input"
        editInput.id <- "edit-input"
        editInput.classList.add "edit-input"
        editInput.setAttribute("tabindex", "-1")
        // Set the prefill text (originalText initially; dispatch overwrites with actual prefill)
        let prefill =
            match model.mode with
            | Editing (originalText, _) -> originalText
            | _ -> node.text
        (editInput :?> HTMLInputElement).value <- prefill
        editInput.addEventListener("keydown", fun (ev: Event) ->
            let ke = ev :?> KeyboardEvent
            let ctx =
                { keyEvent = ke
                  editInput = (editInput :?> HTMLInputElement) }
            handleKey editingKeyTable ctx ke dispatch
        )
        // Prevent clicks inside the edit input from bubbling to the row's
        // mousedown handler (which would commit and exit edit mode)
        editInput.addEventListener("mousedown", fun (ev: Event) ->
            ev.stopPropagation()
        )
        editInput.addEventListener("paste", fun ev -> onPaste ev dispatch)
        row.appendChild editInput |> ignore
    else
        let textDiv = document.createElement "div"
        textDiv.classList.add "text"
        textDiv.textContent <- node.text
        row.appendChild textDiv |> ignore

    // Row mousedown → select (fires before blur)
    row.addEventListener("mousedown", fun (ev: Event) ->
        ev.preventDefault() // prevent focus shift to row div
        dispatch (SelectRow nodeId)
    )

    app.appendChild row |> ignore

    // Recurse into children
    for childId in node.children do
        renderNode model dispatch (depth + 1) childId (isDirectlySelected || inSelectedSubtree) (isFocusNode || inFocusedSubtree)
