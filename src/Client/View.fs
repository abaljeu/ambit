module Gambol.Client.View

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Controller

// ---------------------------------------------------------------------------
// Render
// ---------------------------------------------------------------------------

/// Render the full outline from the model.
/// Wires event handlers that call dispatch.
let rec render (model: Model) (dispatch: Msg -> unit) : unit =
    app.innerHTML <- ""

    let rootEntry = model.siteMap.entries.[model.siteMap.rootId]
    for childInstId in rootEntry.children do
        renderNode model dispatch 0 model.siteMap.entries.[childInstId] false false

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
    hiddenInput.addEventListener("copy",  fun ev -> onCopy  model ev dispatch)
    hiddenInput.addEventListener("cut",   fun ev -> onCut   model ev dispatch)
    app.appendChild hiddenInput |> ignore

    // Settings bar
    let settingsBar = document.createElement "div"
    settingsBar.id <- "settings-bar"
    let cbId = "setting-link-paste"
    let label = document.createElement "label"
    label.setAttribute("for", cbId)
    let cb = document.createElement "input"
    cb.id <- cbId
    cb.setAttribute("type", "checkbox")
    (cb :?> HTMLInputElement).``checked`` <- model.linkPasteEnabled
    cb.addEventListener("change", fun _ -> dispatch ToggleLinkPaste)
    label.appendChild cb |> ignore
    label.appendChild (document.createTextNode " Copy/Paste reference to original") |> ignore
    settingsBar.appendChild label |> ignore
    app.appendChild settingsBar |> ignore

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

and renderNode (model: Model) (dispatch: Msg -> unit) (depth: int) (siteEntry: SiteEntry) (inSelectedSubtree: bool) (inFocusedSubtree: bool) : unit =
    let nodeId = siteEntry.nodeId
    let node = model.graph.nodes.[nodeId]
    let hasChildren = not siteEntry.children.IsEmpty
    let row = document.createElement "div"
    row.classList.add "row"

    // True when nodeId is one of the direct children in the SiteNodeRange.
    let isDirectlySelected =
        match model.selectedNodes with
        | None -> false
        | Some sel ->
            let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
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

    // Fold toggle indicator (▶ collapsed / ▼ expanded) for nodes with children
    if hasChildren then
        let toggle = document.createElement "span"
        toggle.classList.add "fold-toggle"
        toggle.textContent <- if siteEntry.expanded then "\u25BC" else "\u25B6"
        toggle.addEventListener("mousedown", fun (ev: Event) ->
            ev.preventDefault()
            ev.stopPropagation()
            dispatch (ToggleFold siteEntry.instanceId)
        )
        row.appendChild toggle |> ignore

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

    // Recurse into children only when expanded
    if siteEntry.expanded then
        for childInstId in siteEntry.children do
            renderNode model dispatch (depth + 1) model.siteMap.entries.[childInstId] (isDirectlySelected || inSelectedSubtree) (isFocusNode || inFocusedSubtree)
