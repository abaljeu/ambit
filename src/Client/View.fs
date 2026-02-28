module Gambol.Client.View

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Client

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let app = document.getElementById "app"

/// Check if a key string represents a single printable character
let isPrintableKey (key: string) : bool =
    key.Length = 1 && key >= " "

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
        match model.selectedNode with
        | None -> ()
        | Some nodeId ->
            let node = model.graph.nodes.[nodeId]
            if ke.key = "F2" then
                ke.preventDefault()
                dispatch (StartEdit node.text)
            elif ke.key = "Enter" then
                ke.preventDefault()
                dispatch InsertSibling
            elif ke.key = "Escape" then
                ke.preventDefault()
                dispatch CancelEdit // in selection mode, this deselects
            elif isPrintableKey ke.key && not ke.ctrlKey && not ke.metaKey && not ke.altKey then
                ke.preventDefault()
                dispatch (StartEdit ke.key)
    )
    app.appendChild hiddenInput |> ignore

    // Focus management
    match model.mode with
    | Editing _ ->
        let editInput = document.getElementById "edit-input"
        if not (isNull editInput) then
            (editInput :?> HTMLInputElement).focus()
            // Place cursor at end of input
            let inp = editInput :?> HTMLInputElement
            let len = inp.value.Length
            inp.setSelectionRange(len, len)
    | Selection ->
        hiddenInput.focus()

and renderNode (model: Model) (dispatch: Msg -> unit) (depth: int) (nodeId: NodeId) : unit =
    let node = model.graph.nodes.[nodeId]
    let row = document.createElement "div"
    row.classList.add "row"

    let isSelected = model.selectedNode = Some nodeId
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
            | Editing originalText -> originalText
            | _ -> node.text
        (editInput :?> HTMLInputElement).value <- prefill
        editInput.addEventListener("keydown", fun (ev: Event) ->
            let ke = ev :?> KeyboardEvent
            if ke.key = "Enter" then
                ke.preventDefault()
                let value = (editInput :?> HTMLInputElement).value
                dispatch (CommitEdit value)
            elif ke.key = "Escape" then
                ke.preventDefault()
                dispatch CancelEdit
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
