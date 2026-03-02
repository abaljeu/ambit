module Gambol.Client.View

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Client
open Gambol.Client.Update

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
            ; "Escape", (fun _ -> CancelEdit)
            ; printableKeyToken, (fun ctx -> StartEdit ctx.keyEvent.key) ]

let editingKeyTable: (string * KeyHandler<EditingKeyContext>) list =
        [ "Enter", (fun ctx -> SplitNode (ctx.editInput.value, int ctx.editInput.selectionStart))
            ; "ArrowUp", (fun _ -> MoveSelectionUp)
            ; "ArrowDown", (fun _ -> MoveSelectionDown)
            ; "Escape", (fun _ -> CancelEdit) ]

let tryResolveOperation
    (table: (string * KeyHandler<'Context>) list)
    (ke: KeyboardEvent)
    : KeyHandler<'Context> option =
    match table |> List.tryPick (fun (k, handler) -> if k = ke.key then Some handler else None) with
    | Some handler -> Some handler
    | None ->
        if isPrintableKey ke.key && not ke.ctrlKey && not ke.metaKey && not ke.altKey then
            table
            |> List.tryPick (fun (k, handler) -> if k = printableKeyToken then Some handler else None)
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
        renderNode model dispatch 0 childId

    // Hidden input — captures keystrokes in selection mode
    let hiddenInput = document.createElement "input"
    hiddenInput.id <- "hidden-input"
    hiddenInput.setAttribute("autocomplete", "off")
    hiddenInput.addEventListener("keydown", fun (ev: Event) ->
        let ke = ev :?> KeyboardEvent
        match model.selectedNodes with
        | None -> ()
        | Some range ->
            let nodeId = firstSelectedNodeId model.graph range
            let nodeText = model.graph.nodes.[nodeId].text
            let ctx =
                { keyEvent = ke
                  selectedNodeText = nodeText }
            handleKey selectionKeyTable ctx ke dispatch
    )
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

and renderNode (model: Model) (dispatch: Msg -> unit) (depth: int) (nodeId: NodeId) : unit =
    let node = model.graph.nodes.[nodeId]
    let row = document.createElement "div"
    row.classList.add "row"

    let isSelected =
        match model.selectedNodes with
        | None -> false
        | Some range ->
            let parentNode = model.graph.nodes.[range.parent]
            parentNode.children
            |> List.indexed
            |> List.exists (fun (i, id) -> id = nodeId && i >= range.start && i < range.endd)
    if isSelected then
        row.classList.add "selected"

    // Indentation
    for _ in 1 .. depth do
        let indent = document.createElement "div"
        indent.classList.add "indent"
        row.appendChild indent |> ignore

    // Content: either edit input or text div
    let isEditing = isSelected && (match model.mode with Editing _ -> true | _ -> false)
    if isEditing then
        let editInput = document.createElement "input"
        editInput.id <- "edit-input"
        editInput.classList.add "edit-input"
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
        renderNode model dispatch (depth + 1) childId
