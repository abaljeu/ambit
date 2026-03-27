module Gambol.Client.View

open Browser.Dom
open Browser.Types
open Fable.Core.JsInterop
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Controller
open Gambol.Client.JsInterop
open Gambol.Client.Update

let private scrollIntoViewNearest (el: HTMLElement) : unit =
    let o = createEmpty<ScrollIntoViewOptions>
    o.block <- ScrollAlignment.Nearest
    el.scrollIntoView o

// ---------------------------------------------------------------------------
// Depth helper
// ---------------------------------------------------------------------------

/// Depth of entry in the site map (root's children are at depth 0).
let private computeDepth (siteMap: SiteMap) (entry: SiteEntry) : int =
    let rec go (parentInstId: SiteId option) acc =
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
let private makeRowElement
    (model: VM) (applyOp: Op -> unit) (depth: int) (siteEntry: SiteEntry) : HTMLElement =
    let nodeId = siteEntry.nodeId
    let node = model.graph.nodes.[nodeId]
    let hasChildren = not node.children.IsEmpty
    let ownershipClass =
        match siteEntry.parentInstanceId with
        | None -> "amb-row-owned"
        | Some parentInstId ->
            match Map.tryFind parentInstId model.siteMap.entries with
            | None -> "amb-row-owned"
            | Some parentEntry ->
                let childIndex =
                    parentEntry.children
                    |> List.tryFindIndex (fun childInstId -> childInstId = siteEntry.instanceId)
                match childIndex with
                | None -> "amb-row-owned"
                | Some idx ->
                    let parentNode = model.graph.nodes.[parentEntry.nodeId]
                    match parentNode.children |> List.tryItem idx with
                    | Some child when child.ref = Ownership.Ref -> "amb-row-ref"
                    | _ -> "amb-row-owned"
    let row = document.createElement "div"
    row.classList.add "amb-row"
    row.classList.add ownershipClass
    row.setAttribute("data-node-id", node.id.Value.ToString())

    if siteEntry.parentInstanceId = None then row.classList.add "amb-view-root"
    if isEntrySelected model siteEntry then row.classList.add "amb-selected"
    if isEntryFocused  model siteEntry then row.classList.add "amb-focused"

    // Indentation
    for _ in 1 .. depth do
        let indent = document.createElement "div"
        indent.classList.add "amb-indent"
        row.appendChild indent |> ignore

    // Fold toggle indicator
    if hasChildren then
        let toggle = document.createElement "span"
        toggle.classList.add "amb-fold-toggle"
        toggle.textContent <- if siteEntry.expanded then "\u25BC" else "\u25B6"
        toggle.addEventListener("mousedown", fun (ev: Event) ->
            ev.preventDefault()
            ev.stopPropagation()
            applyOp (toggleFoldOp siteEntry.instanceId)
        )
        row.appendChild toggle |> ignore
    else
        let dot = document.createElement "span"
        dot.classList.add "amb-fold-toggle"
        dot.textContent <- "\u25CF"
        row.appendChild dot |> ignore

    // One `.amb-text` per row; new row ⇒ new div. Same node for view and edit (contentEditable).
    let textDiv = document.createElement "div"
    textDiv.classList.add "amb-text"
    for cls in CssClass.toList node.cssClasses do
        textDiv.classList.add cls
    if isEditingEntry model siteEntry then
        textDiv.id <- "edit-input"
        textDiv.classList.add "amb-edit-input"
        textDiv.contentEditable <- "true"
        textDiv.setAttribute("tabindex", "-1")
        let effectiveMode =
            match model.mode with
            | CommandPalette (_, _, ret) -> ret
            | CssClassPrompt (ret, _) -> ret
            | m -> m
        let initialValue =
            match effectiveMode with
            | Editing (text, _) -> text
            | _ -> node.text
        textDiv.textContent <- initialValue
        textDiv.addEventListener("keydown", fun (ev: Event) ->
            let key = ev :?> KeyboardEvent
            if (key.ctrlKey || key.metaKey) && key.key = "p" && not key.shiftKey then
                ev.preventDefault()
            handleKey effectiveMode key applyOp
        )
        textDiv.addEventListener("mousedown", fun (ev: Event) ->
            ev.stopPropagation()
        )
        textDiv.addEventListener("paste", fun ev -> onPaste ev applyOp)
        textDiv.addEventListener("copy", fun ev -> onCopyWhileEditing model ev applyOp)
        textDiv.addEventListener("cut", fun ev -> onCutWhileEditing model ev applyOp)
    else
        textDiv.removeAttribute "id"
        textDiv.contentEditable <- "false"
        textDiv.textContent <- node.text
    row.appendChild textDiv |> ignore

    // Diagnostic: last 8 chars of node GUID, right-justified
    let guidSuffix = node.id.Value.ToString()
    let guidTail = if guidSuffix.Length >= 8 then guidSuffix.Substring(guidSuffix.Length - 8) else guidSuffix
    let guidSpan = document.createElement "span"
    guidSpan.classList.add "amb-node-guid"
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
        applyOp (startEditAtPos offset)
    )
    row

/// Apply in-place patches to an existing row DOM element.
let private applyRowPatches (el: HTMLElement) (patches: RowPatch list) : unit =
    for patch in patches do
        match patch with
        | SetClassName cls -> el.className <- cls
        | SetText txt ->
            let textDiv = el.querySelector ".amb-text"
            if not (isNull textDiv) then (textDiv :?> HTMLElement).textContent <- txt
        | SetTextClasses classes ->
            let textDiv = el.querySelector ".amb-text"
            if not (isNull textDiv) then
                let td = textDiv :?> HTMLElement
                td.className <- "amb-text"
                if td.id = "edit-input" then td.classList.add "amb-edit-input"
                for cls in CssClass.toList classes do
                    td.classList.add cls
        | SetFoldArrow arrow ->
            let ft = el.querySelector ".amb-fold-toggle"
            if not (isNull ft) then (ft :?> HTMLElement).textContent <- arrow
        | SetNodeGuid guid ->
            el.setAttribute("data-node-id", guid.ToString())
            let guidStr = guid.ToString()
            let tail = if guidStr.Length >= 8 then guidStr.Substring(guidStr.Length - 8) else guidStr
            let g = el.querySelector ".amb-node-guid"
            if not (isNull g) then (g :?> HTMLElement).textContent <- tail

/// Resolve the row element for an instance: create, recreate, or patch as dictated by the upsert index.
/// Returns the row element and the updated cache.
let private resolveRow
    (newModel: VM) (applyOp: Op -> unit) (depth: int) (entry: SiteEntry)
    (instId: SiteId) (upsertIndex: Map<SiteId, RowMutation>) (cache: Map<SiteId, HTMLElement>)
    : HTMLElement * Map<SiteId, HTMLElement> =
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

/// Focus the correct element after each dispatch.
let manageFocus (model: VM) (rowByInstanceId: Map<SiteId, HTMLElement>) : unit =
    match model.mode with
    | CommandPalette _ | CssClassPrompt _ ->
        () // focus is handled by renderCommandPalette / renderCssClassPrompt after the element becomes visible
    | Editing _ ->
        let editEl = document.getElementById "edit-input"
        if not (isNull editEl) then
            let root = editEl
            root.focus ()
            match model.mode with
            | Editing (_, caret) ->
                match caret with
                | EditCaret.EndOfText ->
                    let t = root.textContent
                    let n = if isNull t then 0 else t.Length
                    setEditorCaret root n
                | EditCaret.Utf16Index p -> setEditorCaret root p
                | EditCaret.LastVisualLineAtClientX x ->
                    setEditorCaretToLastLineAtX root x
                | EditCaret.FirstVisualLineAtClientX x ->
                    setEditorCarentToFirstLineAtX root x
            | _ -> ()
            scrollIntoViewNearest root
    | Selecting ->
        let hiddenInput = document.getElementById "hidden-input"
        if not (isNull hiddenInput) then
            (hiddenInput :?> HTMLInputElement).focus()
        let focusedInstId =
            match model.selectedNodes with
            | None -> model.siteMap.rootId
            | Some sel -> ViewModel.focusedInstanceId sel
        Map.tryFind focusedInstId rowByInstanceId
        |> Option.iter scrollIntoViewNearest

// ---------------------------------------------------------------------------
// Command palette rendering
// ---------------------------------------------------------------------------

let private paletteWired = ref false

/// Populate the results list of a palette container, highlighting the selected item.
/// Upper-bounds selectedCommand to the list length to handle stale indices.
/// Scrolls the selected item into view so it stays visible when navigating with arrows.
let renderPalette (container: HTMLElement) (items: string list) (selectedCommand: int) : unit =
    let ul = container.querySelector ".amb-palette-results" :?> HTMLElement
    ul.innerHTML <- ""
    let clampedSel = if items.IsEmpty then 0 else min selectedCommand (items.Length - 1)
    let mutable selectedLi: Element option = None
    items |> List.iteri (fun i label ->
        let li = document.createElement "li"
        li.textContent <- label
        if i = clampedSel then
            li.classList.add "amb-palette-selected"
            selectedLi <- Some li
        ul.appendChild li |> ignore)
    selectedLi |> Option.iter (fun el -> scrollIntoViewNearest (el :?> HTMLElement))

/// Show or hide the command palette overlay and keep it up to date with the model.
/// Event listeners are wired once on the first call.
let renderCommandPalette (model: VM) (applyOp: Op -> unit) : unit =
    let container = document.getElementById "command-palette"
    if isNull container then () else

    match model.mode with
    | CommandPalette (q, selectedCommand, ret) ->
        container.classList.add "amb-palette-open"
        let input = document.getElementById "command-palette-input" :?> HTMLInputElement
        window.setTimeout((fun _ -> input.focus()), 0) |> ignore
        let items = filteredCommands ret q |> List.map (fun c -> c.name)
        renderPalette container items selectedCommand

        if not paletteWired.Value then
            paletteWired.Value <- true
            let ul = document.getElementById "command-palette-results"

            input.addEventListener("input", fun _ ->
                applyOp (paletteSetQueryOp input.value))

            input.addEventListener("keydown", fun (ev: Event) ->
                let ke = ev :?> KeyboardEvent
                if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
                    ev.preventDefault()
                handlePaletteKey ke applyOp)

            ul.addEventListener("click", fun (ev: Event) ->
                let target = ev.target :?> HTMLElement
                match target.closest "li" with
                | None -> ()
                | Some li ->
                    let lis = ul.querySelectorAll "li"
                    let mutable idx = 0
                    for i in 0 .. int lis.length - 1 do
                        if System.Object.ReferenceEquals(lis.[i], li) then idx <- i
                    applyOp (fun m d ->
                        match m.mode with
                        | CommandPalette (q, _, ret) ->
                            match List.tryItem idx (filteredCommands ret q) with
                            | None -> { m with mode = ret }
                            | Some cmd ->
                                match cmd.run () with
                                | None ->
                                    setLastKeyDisplay None None
                                    { m with mode = ret }
                                | Some op ->
                                    setLastKeyDisplay None (Some cmd.name)
                                    op { m with mode = ret } d
                        | _ -> m))

    | _ ->
        container.classList.remove "amb-palette-open"

let private cssClassPromptWired = ref false

/// Show or hide the CSS class prompt overlay. Uses in-app modal instead of window.prompt for iPad.
let renderCssClassPrompt (model: VM) (applyOp: Op -> unit) : unit =
    let container = document.getElementById "css-class-prompt"
    if isNull container then () else

    match model.mode with
    | CssClassPrompt (_, initialValue) ->
        container.classList.add "amb-palette-open"
        let input = document.getElementById "css-class-prompt-input" :?> HTMLInputElement
        if not (isNull input) then
            window.setTimeout((fun _ ->
                input.focus()
                input.value <- initialValue
            ), 0) |> ignore
            if not cssClassPromptWired.Value then
                cssClassPromptWired.Value <- true
                input.addEventListener("keydown", fun (ev: Event) ->
                    let ke = ev :?> KeyboardEvent
                    if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
                        ev.preventDefault()
                    handleCssClassPromptKey ke applyOp)
    | _ ->
        container.classList.remove "amb-palette-open"

// ---------------------------------------------------------------------------
// status indicators
// ---------------------------------------------------------------------------

/// Update the persistent status element text and style.
let renderStatus (model: VM) : unit =
    let el = document.getElementById "sync-status"
    if not (isNull el) then
        match model.syncInfo.syncState with
        | Synced  ->
            el.textContent <- "synced"
            el.className <- "amb-sync-status amb-synced"
        | Inactive ->
            el.textContent <- "Inactive"
            el.className <- "amb-sync-status amb-inactive"
        | Syncing attempt ->
            el.textContent <- $"Saving\u2026 (retry {attempt})"
            el.className <- "amb-sync-status amb-syncing"
        | Pending _ ->
            el.textContent <- "Unsaved changes \u2014 click to retry"
            el.className <- "amb-sync-status amb-pending"
        // | Conflicted ->
        //     el.textContent <- "Conflict detected \u2014 resolving\u2026"
        //     el.className <- "amb-sync-status amb-conflicted"
        | Stale ->
            el.textContent <- "Refresh the view"
            el.className <- "amb-sync-status amb-stale"

let private syncRiskAlertWired = ref false

/// Full-screen sync risk notice (Pending / Stale) until the user acknowledges.
let renderSyncRiskAlert (model: VM) (dispatch: Msg -> unit) : unit =
    let root = document.getElementById "blocking-alert"
    if isNull root then () else

    let shouldShow =
        match model.syncInfo.syncState with
        | Pending _
        | Stale -> not model.syncInfo.syncRiskAcknowledged
        | _ -> false

    if shouldShow then
        root.classList.add "amb-blocking-alert-open"
        let titleEl = document.getElementById "blocking-alert-title"
        let msgEl = document.getElementById "blocking-alert-message"
        let okBtn = document.getElementById "blocking-alert-ok" :?> HTMLButtonElement
        if not (isNull titleEl) && not (isNull msgEl) then
            match model.syncInfo.syncState with
            | Pending n ->
                titleEl.textContent <- "Could not save"
                msgEl.textContent <-
                    $"Your changes are queued but the server did not confirm (attempt {n}). "
                    + "You can retry from the status control, or reload when safe."
            | Stale ->
                titleEl.textContent <- "View is out of date"
                msgEl.textContent <-
                    "The server has newer data than this page. Use Refresh or Reload before continuing."
            | _ -> ()

        if not (isNull okBtn) then
            window.setTimeout ((fun _ -> okBtn.focus ()), 0) |> ignore
            if not syncRiskAlertWired.Value then
                syncRiskAlertWired.Value <- true
                okBtn.addEventListener ("click", fun _ -> dispatch AckSyncRisk)
                okBtn.addEventListener ("keydown", fun (ev: Event) ->
                    let ke = ev :?> KeyboardEvent
                    if ke.key = "Enter" || ke.key = " " then
                        ke.preventDefault ()
                        dispatch AckSyncRisk)
    else
        root.classList.remove "amb-blocking-alert-open"

/// Status pill plus sync-risk overlay.
let renderSyncChrome (model: VM) (dispatch: Msg -> unit) : unit =
    renderStatus model
    renderSyncRiskAlert model dispatch

/// Update the undo/redo status indicator based on history.
let renderUndoStatus (model: VM) : unit =
    let el = document.getElementById "undo-status"
    if not (isNull el) then
        let canUndo = not model.history.past.IsEmpty
        let canRedo = not model.history.future.IsEmpty
        let undoText = if canUndo then "\u21B6" else "\u2205"           // ↶ or ∅
        let redoText = if canRedo then "\u21B7" else "\u2205"           // ↷ or ∅
        el.textContent <- $"{undoText} {redoText}"
        el.className <- if canUndo || canRedo then "amb-undo-status amb-active" 
                        else "amb-undo-status"

// ---------------------------------------------------------------------------
// Full rebuild (StateLoaded)
// ---------------------------------------------------------------------------

/// Rebuild all row elements from scratch: removes existing rows (children of app
/// that precede the hidden-input sentinel), then recreates them in preorder.
/// Returns a fresh element cache keyed by instanceId.
let render (vm: VM) (applyOp: Op -> unit) (dispatch: Msg -> unit) : Map<SiteId, HTMLElement> =
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

    let mutable cache = Map.empty<SiteId, HTMLElement>
    let visible = ViewModel.getVisibleInstanceIds vm.siteMap
    for instId in visible do
        let entry = vm.siteMap.entries.[instId]
        let depth = computeDepth vm.siteMap entry
        let row = makeRowElement vm applyOp depth entry
        cache <- Map.add instId row cache
        let sentinel = document.getElementById "hidden-input"
        if isNull sentinel then app.appendChild row |> ignore
        else app.insertBefore(row, sentinel) |> ignore

    manageFocus vm cache
    renderSyncChrome vm dispatch
    cache

// ---------------------------------------------------------------------------
// Incremental DOM patch (all ops except StateLoaded)
// ---------------------------------------------------------------------------

/// Patch the DOM incrementally: diff old and new SiteMap visibility,
/// removes stale rows, creates/moves new rows, updates existing rows in-place.
/// Returns the updated element cache.
let patchDOM
        (oldModel: VM) (newModel: VM) (applyOp: Op -> unit) (dispatch: Msg -> unit)
        (cache: Map<SiteId, HTMLElement>)
        : Map<SiteId, HTMLElement> =
    let cachedInstIds = cache |> Map.toSeq |> Seq.map fst |> Set.ofSeq
    let mutations = ViewModel.planPatchDOM oldModel newModel cachedInstIds

    // Index upsert mutations by instId for O(log n) lookup below
    let upsertIndex: Map<SiteId, RowMutation> =
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

    manageFocus newModel cache'
    renderSyncChrome newModel dispatch
    cache'
