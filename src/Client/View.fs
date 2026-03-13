module Gambol.Client.View

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Controller
open Gambol.Client.Update

// ---------------------------------------------------------------------------
// Depth helper
// ---------------------------------------------------------------------------

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
let private makeRowElement (model: VM) (applyOp: Op -> unit) (depth: int) (siteEntry: SiteEntry) : HTMLElement =
    let nodeId = siteEntry.nodeId
    let node = model.graph.nodes.[nodeId]
    let hasChildren = not node.children.IsEmpty
    let row = document.createElement "div"
    row.classList.add "row"
    row.setAttribute("data-node-id", node.id.Value.ToString())

    if siteEntry.parentInstanceId = None then row.classList.add "view-root"
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
            applyOp (toggleFoldOp siteEntry.instanceId)
        )
        row.appendChild toggle |> ignore
    else
        let dot = document.createElement "span"
        dot.classList.add "fold-toggle"
        dot.textContent <- "\u25CF"
        row.appendChild dot |> ignore

    // Content: edit input or text div
    if isEditingEntry model siteEntry then
        let editInput = document.createElement "input"
        editInput.id <- "edit-input"
        editInput.classList.add "edit-input"
        editInput.setAttribute("tabindex", "-1")
        let prefill =
            match model.mode with
            | Editing (originalText, Some pf, _) -> pf
            | Editing (originalText, None, _)    -> originalText
            | _ -> node.text
        (editInput :?> HTMLInputElement).value <- prefill
        editInput.addEventListener("keydown", fun (ev: Event) ->
            let key = ev :?> KeyboardEvent
            let ctx =
                { keyEvent = key
                  editInput = (editInput :?> HTMLInputElement) }
            handleKey editingKeyTable ctx key applyOp
        )
        editInput.addEventListener("mousedown", fun (ev: Event) ->
            ev.stopPropagation()
        )
        editInput.addEventListener("paste", fun ev -> onPaste ev applyOp)
        row.appendChild editInput |> ignore
    else
        let textDiv = document.createElement "div"
        textDiv.classList.add "text"
        textDiv.textContent <- node.text
        row.appendChild textDiv |> ignore

    // Diagnostic: last 8 chars of node GUID, right-justified
    let guidSuffix = node.id.Value.ToString()
    let guidTail = if guidSuffix.Length >= 8 then guidSuffix.Substring(guidSuffix.Length - 8) else guidSuffix
    let guidSpan = document.createElement "span"
    guidSpan.classList.add "node-guid"
    guidSpan.textContent <- guidTail
    row.appendChild guidSpan |> ignore

    // Row click → select the exact view-line instance, not just the first occurrence of the nodeId
    row.addEventListener("mousedown", fun (ev: Event) ->
        ev.preventDefault()
        applyOp (selectInstance siteEntry.instanceId)
    )
    // Row double-click → enter edit mode with cursor at mouse position
    row.addEventListener("dblclick", fun (ev: Event) ->
        ev.preventDefault()
        let me = ev :?> MouseEvent
        let offset = getCaretOffset me.clientX me.clientY
        applyOp (startEditAtPos node.text offset)
    )
    row

/// Apply in-place patches to an existing row DOM element.
let private applyRowPatches (el: HTMLElement) (patches: RowPatch list) : unit =
    for patch in patches do
        match patch with
        | SetClassName cls -> el.className <- cls
        | SetText txt ->
            let textDiv = el.querySelector ".text"
            if not (isNull textDiv) then (textDiv :?> HTMLElement).textContent <- txt
        | SetFoldArrow arrow ->
            let ft = el.querySelector ".fold-toggle"
            if not (isNull ft) then (ft :?> HTMLElement).textContent <- arrow
        | SetNodeGuid guid ->
            el.setAttribute("data-node-id", guid.ToString())
            let guidStr = guid.ToString()
            let tail = if guidStr.Length >= 8 then guidStr.Substring(guidStr.Length - 8) else guidStr
            let g = el.querySelector ".node-guid"
            if not (isNull g) then (g :?> HTMLElement).textContent <- tail

/// Resolve the row element for an instance: create, recreate, or patch as dictated by the upsert index.
/// Returns the row element and the updated cache.
let private resolveRow
    (newModel: VM) (applyOp: Op -> unit) (depth: int) (entry: SiteEntry)
    (instId: int) (upsertIndex: Map<int, RowMutation>) (cache: Map<int, HTMLElement>)
    : HTMLElement * Map<int, HTMLElement> =
    match Map.tryFind instId upsertIndex with
    | Some (RecreateRow _) ->
        let cache' =
            match Map.tryFind instId cache with
            | Some old -> old.remove(); cache
            | None -> cache
        let el = makeRowElement newModel applyOp depth entry
        (el, Map.add instId el cache')
    | Some (PatchRow (_, patches)) ->
        let el = cache.[instId]
        applyRowPatches el patches
        (el, cache)
    | _ ->  // CreateRow or missing
        let el = makeRowElement newModel applyOp depth entry
        (el, Map.add instId el cache)

// ---------------------------------------------------------------------------
// Focus management
// ---------------------------------------------------------------------------

/// Focus the correct element (edit-input or hidden-input) after each dispatch.
let manageFocus (model: VM) : unit =
    match model.mode with
    | Editing _ ->
        let editInput = document.getElementById "edit-input"
        if not (isNull editInput) then
            let inp = editInput :?> HTMLInputElement
            inp.focus()
            let pos =
                match model.mode with
                | Editing (_, _, Some p) -> p
                | _ -> inp.value.Length
            inp.setSelectionRange(pos, pos)
    | Selecting ->
        let hiddenInput = document.getElementById "hidden-input"
        if not (isNull hiddenInput) then
            (hiddenInput :?> HTMLInputElement).focus()

// ---------------------------------------------------------------------------
// status indicators
// ---------------------------------------------------------------------------

/// Update the persistent status element text and style.
let renderStatus (model: VM) : unit =
    let el = document.getElementById "sync-status"
    if not (isNull el) then
        match model.syncState with
        | Synced  ->
            el.textContent <- "synced"
            el.className <- "sync-status"
        | Syncing ->
            el.textContent <- "Saving\u2026"
            el.className <- "sync-status syncing"
        | Pending ->
            el.textContent <- "Unsaved changes \u2014 click to retry"
            el.className <- "sync-status pending"

/// Update the undo/redo status indicator based on history.
let renderUndoStatus (model: VM) : unit =
    let el = document.getElementById "undo-status"
    if not (isNull el) then
        let canUndo = not model.history.past.IsEmpty
        let canRedo = not model.history.future.IsEmpty
        let undoText = if canUndo then "\u21B6" else "\u2205"           // ↶ or ∅
        let redoText = if canRedo then "\u21B7" else "\u2205"           // ↷ or ∅
        el.textContent <- $"{undoText} {redoText}"
        el.className <- if canUndo || canRedo then "undo-status active" else "undo-status"

// ---------------------------------------------------------------------------
// Full rebuild (StateLoaded)
// ---------------------------------------------------------------------------

/// Rebuild all row elements from scratch: removes existing rows (children of app
/// that precede the hidden-input sentinel), then recreates them in preorder.
/// Returns a fresh element cache keyed by instanceId.
let render (vm: VM) (applyOp: Op -> unit) : Map<int, HTMLElement> =
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
    let visible = ViewModel.getVisibleInstanceIds vm.siteMap
    for instId in visible do
        let entry = vm.siteMap.entries.[instId]
        let depth = computeDepth vm.siteMap entry
        let row = makeRowElement vm applyOp depth entry
        cache <- Map.add instId row cache
        let sentinel = document.getElementById "hidden-input"
        if isNull sentinel then app.appendChild row |> ignore
        else app.insertBefore(row, sentinel) |> ignore

    // Sync the settings-bar checkbox
    let cb = document.getElementById "setting-link-paste"
    if not (isNull cb) then
        (cb :?> HTMLInputElement).``checked`` <- vm.linkPasteEnabled

    manageFocus vm
    renderStatus vm
    cache

// ---------------------------------------------------------------------------
// Incremental DOM patch (all ops except StateLoaded)
// ---------------------------------------------------------------------------

/// Patch the DOM incrementally: diff old and new SiteMap visibility,
/// removes stale rows, creates/moves new rows, updates existing rows in-place.
/// Returns the updated element cache.
let patchDOM (oldModel: VM) (newModel: VM) (applyOp: Op -> unit) (cache: Map<int, HTMLElement>) : Map<int, HTMLElement> =
    let cachedInstIds = cache |> Map.toSeq |> Seq.map fst |> Set.ofSeq
    let mutations = ViewModel.planPatchDOM oldModel newModel cachedInstIds

    // Index upsert mutations by instId for O(log n) lookup below
    let upsertIndex =
        mutations |> List.choose (fun m ->
            match m with
            | RemoveRow _ -> None
            | CreateRow id  -> Some (id, m)
            | RecreateRow id -> Some (id, m)
            | PatchRow (id, _) -> Some (id, m))
        |> Map.ofList

    let mutable cache' = cache

    // Apply removals
    for mut in mutations do
        match mut with
        | RemoveRow instId ->
            match Map.tryFind instId cache' with
            | Some el -> el.remove()
            | None -> ()
            cache' <- Map.remove instId cache'
        | _ -> ()

    // Apply upserts in preorder, correcting DOM position as we go
    let mutable prevNode: Browser.Types.Node option = None

    for instId in ViewModel.getVisibleInstanceIds newModel.siteMap do
        let entry = newModel.siteMap.entries.[instId]
        let depth = computeDepth newModel.siteMap entry

        let row, cache'' = resolveRow newModel applyOp depth entry instId upsertIndex cache'
        cache' <- cache''

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
    renderStatus newModel
    cache'
