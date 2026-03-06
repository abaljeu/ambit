module Gambol.Client.View

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Controller

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

        let row : HTMLElement =
            match Map.tryFind instId upsertIndex with
            | Some (RecreateRow _) ->
                match Map.tryFind instId cache' with
                | Some old -> old.remove()
                | None -> ()
                let el = makeRowElement newModel dispatch depth entry
                cache' <- Map.add instId el cache'
                el
            | Some (PatchRow (_, patches)) ->
                let el = cache'.[instId]
                for patch in patches do
                    match patch with
                    | SetClassName cls -> el.className <- cls
                    | SetText txt ->
                        let textDiv = el.querySelector ".text"
                        if not (isNull textDiv) then (textDiv :?> HTMLElement).textContent <- txt
                    | SetFoldArrow arrow ->
                        let ft = el.querySelector ".fold-toggle"
                        if not (isNull ft) then (ft :?> HTMLElement).textContent <- arrow
                el
            | _ ->  // CreateRow or missing
                let el = makeRowElement newModel dispatch depth entry
                cache' <- Map.add instId el cache'
                el

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
