module Gambol.Client.View

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Controller

// ---------------------------------------------------------------------------
// Selection / focus / depth helpers
// ---------------------------------------------------------------------------

let private isNodeDirectlySelected (model: Model) (nodeId: NodeId) : bool =
    match model.selectedNodes with
    | None -> false
    | Some sel ->
        let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
        parentNode.children
        |> List.indexed
        |> List.exists (fun (i, id) -> id = nodeId && i >= sel.range.start && i < sel.range.endd)

/// True when entry is directly selected OR any ancestor is directly selected.
let private isEntrySelected (model: Model) (entry: SiteEntry) : bool =
    if isNodeDirectlySelected model entry.nodeId then true
    else
        let siteMap = model.siteMap
        let rec go (parentInstId: int option) =
            match parentInstId with
            | None -> false
            | Some pid ->
                match Map.tryFind pid siteMap.entries with
                | None -> false
                | Some pe ->
                    if isNodeDirectlySelected model pe.nodeId then true
                    else go pe.parentInstanceId
        go entry.parentInstanceId

let private isNodeFocused (model: Model) (nodeId: NodeId) : bool =
    match model.selectedNodes with
    | None -> false
    | Some sel -> focusedNodeId model.graph sel = nodeId

/// True when entry is the focus node OR any ancestor is the focus node.
let private isEntryFocused (model: Model) (entry: SiteEntry) : bool =
    if isNodeFocused model entry.nodeId then true
    else
        let siteMap = model.siteMap
        let rec go (parentInstId: int option) =
            match parentInstId with
            | None -> false
            | Some pid ->
                match Map.tryFind pid siteMap.entries with
                | None -> false
                | Some pe ->
                    if isNodeFocused model pe.nodeId then true
                    else go pe.parentInstanceId
        go entry.parentInstanceId

let private isEditingEntry (model: Model) (entry: SiteEntry) : bool =
    match model.mode with
    | Editing _ -> isNodeFocused model entry.nodeId
    | Selecting -> false

/// Depth of entry in the site map (root's children are at depth 0).
let private computeDepth (siteMap: SiteMap) (entry: SiteEntry) : int =
    let rec go (parentInstId: int option) acc =
        match parentInstId with
        | None -> acc
        | Some pid ->
            match Map.tryFind pid siteMap.entries with
            | None -> acc
            | Some pe -> go pe.parentInstanceId (acc + 1)
    go entry.parentInstanceId 0

// ---------------------------------------------------------------------------
// Row element creation
// ---------------------------------------------------------------------------

/// Create a fresh DOM row for the given SiteEntry at the given depth.
let private makeRowElement (model: Model) (dispatch: Msg -> unit) (depth: int) (siteEntry: SiteEntry) : HTMLElement =
    let nodeId = siteEntry.nodeId
    let node = model.graph.nodes.[nodeId]
    let hasChildren = not siteEntry.children.IsEmpty
    let row = document.createElement "div"
    row.classList.add "row"

    if isEntrySelected model siteEntry then row.classList.add "selected"
    if isEntryFocused  model siteEntry then row.classList.add "focused"

    // Indentation
    for _ in 1 .. depth do
        let indent = document.createElement "div"
        indent.classList.add "indent"
        row.appendChild indent |> ignore

    // Fold toggle indicator
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

    // Content: edit input or text div
    if isEditingEntry model siteEntry then
        let editInput = document.createElement "input"
        editInput.id <- "edit-input"
        editInput.classList.add "edit-input"
        editInput.setAttribute("tabindex", "-1")
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

    // Row click → select
    row.addEventListener("mousedown", fun (ev: Event) ->
        ev.preventDefault()
        dispatch (SelectRow nodeId)
    )
    row

// ---------------------------------------------------------------------------
// In-place row update
// ---------------------------------------------------------------------------

/// Update an existing cached row element in-place.
/// Returns false if the element must be fully recreated (editing mode toggled or
/// hasChildren changed), true if the in-place patch was sufficient.
let private updateRowElement (oldModel: Model) (newModel: Model) (instId: int) (el: HTMLElement) : bool =
    match Map.tryFind instId newModel.siteMap.entries with
    | None -> false
    | Some entry ->
        let wasEditing = isEditingEntry oldModel entry
        let nowEditing = isEditingEntry newModel entry
        if wasEditing <> nowEditing then false
        else
            // If hasChildren changed, fold toggle must appear/disappear → recreate
            let oldHasChildren =
                Map.tryFind instId oldModel.siteMap.entries
                |> Option.map (fun e -> not e.children.IsEmpty)
                |> Option.defaultValue (not entry.children.IsEmpty)
            if oldHasChildren <> not entry.children.IsEmpty then false
            else
                // Update CSS classes
                let sel = isEntrySelected newModel entry
                let foc = isEntryFocused newModel entry
                let newClass = "row" + (if sel then " selected" else "") + (if foc then " focused" else "")
                if el.className <> newClass then el.className <- newClass

                // Update text content when not editing
                if not nowEditing then
                    let node = newModel.graph.nodes.[entry.nodeId]
                    let textDiv = el.querySelector ".text"
                    if not (isNull textDiv) then
                        let td = textDiv :?> HTMLElement
                        if td.textContent <> node.text then td.textContent <- node.text

                // Update fold-toggle indicator
                if not entry.children.IsEmpty then
                    let foldToggle = el.querySelector ".fold-toggle"
                    if not (isNull foldToggle) then
                        let ft = foldToggle :?> HTMLElement
                        let expected = if entry.expanded then "\u25BC" else "\u25B6"
                        if ft.textContent <> expected then ft.textContent <- expected

                true

// ---------------------------------------------------------------------------
// Focus management
// ---------------------------------------------------------------------------

/// Focus the correct element (edit-input or hidden-input) after each dispatch.
let manageFocus (model: Model) : unit =
    match model.mode with
    | Editing _ ->
        let editInput = document.getElementById "edit-input"
        if not (isNull editInput) then
            let inp = editInput :?> HTMLInputElement
            inp.focus()
            let pos =
                match model.mode with
                | Editing (_, Some p) -> p
                | _ -> inp.value.Length
            inp.setSelectionRange(pos, pos)
    | Selecting ->
        let hiddenInput = document.getElementById "hidden-input"
        if not (isNull hiddenInput) then
            (hiddenInput :?> HTMLInputElement).focus()

// ---------------------------------------------------------------------------
// Full rebuild (StateLoaded)
// ---------------------------------------------------------------------------

/// Rebuild all row elements from scratch: removes existing rows (children of app
/// that precede the hidden-input sentinel), then recreates them in preorder.
/// Returns a fresh element cache keyed by instanceId.
let render (model: Model) (dispatch: Msg -> unit) : Map<int, HTMLElement> =
    // Remove existing rows — everything before the hidden-input sentinel
    let hiddenInput = document.getElementById "hidden-input"
    if isNull hiddenInput then
        app.innerHTML <- ""
    else
        let mutable sib = hiddenInput.previousSibling
        while not (isNull sib) do
            let prev = sib.previousSibling
            app.removeChild sib |> ignore
            sib <- prev

    let mutable cache = Map.empty<int, HTMLElement>
    let visible = ViewModel.getVisibleInstanceIds model.siteMap
    for instId in visible do
        let entry = model.siteMap.entries.[instId]
        let depth = computeDepth model.siteMap entry
        let row = makeRowElement model dispatch depth entry
        cache <- Map.add instId row cache
        let sentinel = document.getElementById "hidden-input"
        if isNull sentinel then app.appendChild row |> ignore
        else app.insertBefore(row, sentinel) |> ignore

    // Sync the settings-bar checkbox
    let cb = document.getElementById "setting-link-paste"
    if not (isNull cb) then
        (cb :?> HTMLInputElement).``checked`` <- model.linkPasteEnabled

    manageFocus model
    cache

// ---------------------------------------------------------------------------
// Incremental DOM patch (all dispatches except StateLoaded)
// ---------------------------------------------------------------------------

/// Patch the DOM incrementally: diff old and new SiteMap visibility,
/// removes stale rows, creates/moves new rows, updates existing rows in-place.
/// Returns the updated element cache.
let patchDOM (oldModel: Model) (newModel: Model) (dispatch: Msg -> unit) (cache: Map<int, HTMLElement>) : Map<int, HTMLElement> =
    let newVisible = ViewModel.getVisibleInstanceIds newModel.siteMap
    let newVisibleSet = newVisible |> Set.ofList

    // Remove entries no longer visible from DOM and cache
    let mutable cache' =
        cache |> Map.filter (fun instId el ->
            if Set.contains instId newVisibleSet then true
            else el.remove(); false)

    let mutable prevNode: Browser.Types.Node option = None

    for instId in newVisible do
        let entry = newModel.siteMap.entries.[instId]
        let depth = computeDepth newModel.siteMap entry

        let row : HTMLElement =
            match Map.tryFind instId cache' with
            | Some existingEl ->
                let ok = updateRowElement oldModel newModel instId existingEl
                if not ok then
                    existingEl.remove()
                    let newEl = makeRowElement newModel dispatch depth entry
                    cache' <- Map.add instId newEl cache'
                    newEl
                else existingEl
            | None ->
                let newEl = makeRowElement newModel dispatch depth entry
                cache' <- Map.add instId newEl cache'
                newEl

        // Ensure the row sits in the correct DOM position (preorder sequence)
        let atCorrectPos =
            match prevNode with
            | None ->
                let first = app.firstChild
                not (isNull first) && System.Object.ReferenceEquals(first, row)
            | Some pe ->
                let ns = pe.nextSibling
                not (isNull ns) && System.Object.ReferenceEquals(ns, row)

        if not atCorrectPos then
            let anchor =
                match prevNode with
                | None -> app.firstChild
                | Some pe -> pe.nextSibling
            app.insertBefore(row, anchor) |> ignore

        prevNode <- Some (row :> Browser.Types.Node)

    // Sync the settings-bar checkbox
    let cb = document.getElementById "setting-link-paste"
    if not (isNull cb) then
        (cb :?> HTMLInputElement).``checked`` <- newModel.linkPasteEnabled

    manageFocus newModel
    cache'
