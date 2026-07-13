module Gambol.Client.View

open Browser.Dom
open Browser.Types
open Fable.Core.JsInterop
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.Controller
open Gambol.Client.Commands
open Gambol.Client.JsInterop
open Gambol.Client.Update
open Gambol.Client.UpdateOps
open Gambol.Shared.CommandDockLayout
open Gambol.Shared.CommandCategory

module CommandMeta = Gambol.Shared.CommandEntry

let private doubleTapScrollDeferMs = 400
let mutable private deferSelectionScroll = false
let mutable private pendingSelectionScrollTimer : float option = None

let private cancelPendingSelectionScroll () : unit =
    pendingSelectionScrollTimer |> Option.iter clearTimeout
    pendingSelectionScrollTimer <- None

let private scheduleDeferredSelectionScroll (el: HTMLElement) : unit =
    cancelPendingSelectionScroll ()
    pendingSelectionScrollTimer <-
        Some (
            setTimeout
                (fun () ->
                    pendingSelectionScrollTimer <- None
                    deferSelectionScroll <- false
                    scrollIntoViewNearest el)
                doubleTapScrollDeferMs)

let private scrollFocusedRow (el: HTMLElement) : unit =
    if deferSelectionScroll then scheduleDeferredSelectionScroll el
    else scrollIntoViewNearest el

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
    (model: VM) (dispatch: Msg -> unit) (depth: int) (siteEntry: SiteEntry) : HTMLElement =
    let nodeId = siteEntry.nodeId
    let node = model.graph.nodes.[nodeId]
    let hasChildren = not node.children.IsEmpty
    let row = document.createElement "div"
    row.classList.add "amb-row"
    row.classList.add (ViewModel.rowOwnershipClass model siteEntry)
    if ViewModel.rowFileUnparsedClassEligible model siteEntry then
        row.classList.add "amb-row-file-unparsed"
    if ViewModel.rowArtifactAbsentClassEligible model siteEntry node then
        row.classList.add "amb-row-artifact-absent"
    row.setAttribute("data-node-id", node.id.Value.ToString())

    if siteEntry.parentInstanceId = None then row.classList.add "amb-view-root"
    if isEntrySelected model siteEntry then row.classList.add "amb-selected"
    if isEntryFocused  model siteEntry then row.classList.add "amb-focused"
    match ViewModel.specialKindRowClass node.id node.kind with
    | Some cls -> row.classList.add cls
    | None -> ()

    let fileIndicator = document.createElement "span"
    fileIndicator.classList.add "amb-file-indicator"
    fileIndicator.textContent <- ViewModel.rowFileIndicatorText model siteEntry node
    row.appendChild fileIndicator |> ignore

    // Indentation
    for _ in 1 .. depth do
        let indent = document.createElement "div"
        indent.classList.add "amb-indent"
        row.appendChild indent |> ignore

    // Fold toggle indicator
    let leafBullet =
        if hasChildren then
            let toggle = document.createElement "span"
            toggle.classList.add "amb-fold-toggle"
            toggle.textContent <- if siteEntry.expanded then "\u25BC" else "\u25B6"
            toggle.addEventListener("mousedown", fun (ev: Event) ->
                ev.preventDefault()
                ev.stopPropagation()
                let op =
                    if siteEntry.parentInstanceId = None then zoomOutOp
                    else toggleFoldOp siteEntry.instanceId
                dispatch (ApplyOp op)
            )
            row.appendChild toggle |> ignore
            None
        else
            let dot = document.createElement "span"
            dot.classList.add "amb-fold-toggle"
            dot.textContent <- "\u25CF"
            row.appendChild dot |> ignore
            Some dot

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
            | SearchDialog s -> s.returnTo
            | FileSearchDialog s -> s.returnTo
            | CssClassPrompt (ret, _) -> ret
            | RenamePrompt (ret, _) -> ret
            | m -> m
        let initialValue =
            match effectiveMode with
            | Editing (text, _) -> text
            | _ -> ViewModel.outlineDisplayText node
        textDiv.textContent <- initialValue
        textDiv.addEventListener("keydown", fun (ev: Event) ->
            let key = ev :?> KeyboardEvent
            if (key.ctrlKey || key.metaKey) && key.key = "p" && not key.shiftKey then
                ev.preventDefault()
            handleKey effectiveMode key dispatch
        )
        textDiv.addEventListener("mousedown", fun (ev: Event) ->
            ev.stopPropagation()
        )
        textDiv.addEventListener("dblclick", fun (ev: Event) ->
            ev.stopPropagation()
        )
        textDiv.addEventListener("paste", fun ev -> onPaste ev dispatch)
        textDiv.addEventListener("copy", fun ev -> onCopyWhileEditing model ev dispatch)
        textDiv.addEventListener("cut", fun ev -> onCutWhileEditing model ev dispatch)
    else
        textDiv.removeAttribute "id"
        textDiv.contentEditable <- "false"
        textDiv.textContent <- ViewModel.outlineDisplayText node
    row.appendChild textDiv |> ignore

    let rowTextOffset (ev: MouseEvent) : int =
        getCaretOffsetInRoot textDiv ev.clientX ev.clientY

    let activateRow (ev: Event) : unit =
        ev.preventDefault()
        ev.stopPropagation()
        let me = ev :?> MouseEvent
        deferSelectionScroll <- true
        dispatch (ApplyOp (pointerActivateRowAtPos siteEntry.instanceId (rowTextOffset me)))

    let doubleClickRow (ev: Event) : unit =
        ev.preventDefault()
        ev.stopPropagation()
        let me = ev :?> MouseEvent
        cancelPendingSelectionScroll ()
        deferSelectionScroll <- false
        dispatch (ApplyOp (doubleClickRowAtPos siteEntry.instanceId (rowTextOffset me)))

    if not (isEditingEntry model siteEntry) then
        textDiv.addEventListener("mousedown", activateRow)
        textDiv.addEventListener("dblclick", doubleClickRow)

    leafBullet
    |> Option.iter (fun dot ->
        dot.addEventListener("mousedown", activateRow)
        dot.addEventListener("dblclick", doubleClickRow))

    let nameSpan = document.createElement "span"
    nameSpan.classList.add "amb-node-guid"
    nameSpan.textContent <- ViewModel.rowNameDisplayText node.name
    row.appendChild nameSpan |> ignore
    row

/// Apply in-place patches to an existing row DOM element.
let private applyRowPatches (el: HTMLElement) (patches: RowPatch list) : unit =
    let ensureFileIndicator () =
        let indicator = el.querySelector ".amb-file-indicator"
        if isNull indicator then
            let created = document.createElement "span"
            created.classList.add "amb-file-indicator"
            let first = el.firstChild
            if isNull first then el.appendChild created |> ignore
            else el.insertBefore(created, first) |> ignore
            created
        else
            indicator :?> HTMLElement

    for patch in patches do
        match patch with
        | SetClassName cls -> el.className <- cls
        | SetText txt ->
            let textDiv = el.querySelector ".amb-text"
            if not (isNull textDiv) then
                (textDiv :?> HTMLElement).textContent <- txt
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
        | SetNodeName name ->
            let g = el.querySelector ".amb-node-guid"
            if not (isNull g) then (g :?> HTMLElement).textContent <- name
        | SetFileIndicator text ->
            (ensureFileIndicator ()).textContent <- text

/// Resolve the row element for an instance: create, recreate, or patch as dictated by the upsert index.
/// Returns the row element and the updated cache.
let private resolveRow
    (newModel: VM) (dispatch: Msg -> unit) (depth: int) (entry: SiteEntry)
    (instId: SiteId) (upsertIndex: Map<SiteId, RowMutation>) (cache: Map<SiteId, HTMLElement>)
    : HTMLElement * Map<SiteId, HTMLElement> =
    match Map.tryFind instId upsertIndex with
    | Some (RecreateRow _) ->
        let cache' =
            match Map.tryFind instId cache with
            | Some old -> old.remove(); cache
            | None -> cache
        let el = makeRowElement newModel dispatch depth entry
        (el, Map.add instId el cache')
    | Some (PatchRow (_, patches)) ->
        let el = cache.[instId]
        applyRowPatches el patches
        (el, cache)
    | _ ->  // CreateRow or missing
        let el = makeRowElement newModel dispatch depth entry
        (el, Map.add instId el cache)

// ---------------------------------------------------------------------------
// Focus management
// ---------------------------------------------------------------------------

/// Focus the correct element after each dispatch.
/// `previousModel` = model before this dispatch; None on full `render` (always apply caret).
let manageFocus
        (previousModel: VM option) (model: VM) (rowByInstanceId: Map<SiteId, HTMLElement>)
        : unit =
    let preserveEditCaret = EditingCaretPreserve.shouldPreserveDomCaret previousModel model
    match model.mode with
    | CommandPalette _ | SearchDialog _ | FileSearchDialog _ | CssClassPrompt _ | RenamePrompt _ ->
        () // focus is handled by overlay renderers after the element becomes visible
    | Editing _ ->
        cancelPendingSelectionScroll ()
        deferSelectionScroll <- false
        let editEl = document.getElementById "edit-input"
        if not (isNull editEl) then
            let root = editEl
            focusPreventScroll root
            if not preserveEditCaret then
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
            scrollElementIntoViewAboveKeyboard root
    | Selecting ->
        let hiddenInput = document.getElementById "hidden-input"
        if not (isNull hiddenInput) then
            focusPreventScroll (hiddenInput :?> HTMLInputElement)
        let focusedInstId =
            match model.selectedNodes with
            | None -> model.siteMap.rootId
            | Some sel ->
                ViewModel.focusedInstanceId sel
                |> Option.defaultValue model.siteMap.rootId
        // Only scroll when the focused row changed (navigation) or on a full render.
        // This prevents the wheel-scroll snap-back caused by non-navigation dispatches.
        let prevFocusedInstId =
            match previousModel with
            | None -> None  // full render — always scroll
            | Some prev ->
                match prev.selectedNodes with
                | None -> Some prev.siteMap.rootId
                | Some sel ->
                    ViewModel.focusedInstanceId sel
                    |> Option.orElse (Some prev.siteMap.rootId)
        if prevFocusedInstId <> Some focusedInstId then
            Map.tryFind focusedInstId rowByInstanceId
            |> Option.iter scrollFocusedRow

// ---------------------------------------------------------------------------
// Compact command dock
// ---------------------------------------------------------------------------

let mutable private activeToolSurface : DockTriggerEntry option = None
let mutable private lastDockSnapshot : string option = None
let private svgNs = "http://www.w3.org/2000/svg"

let private dockSnapshot (model: VM) =
    let ctx = commandContextMode model.mode
    let sel = paletteWasSelecting ctx
    sprintf "%A|%A|%b" activeToolSurface ctx sel

let private makeDockIcon (iconId: string) : HTMLElement =
    let svg = document.createElementNS(svgNs, "svg")
    svg.setAttribute("class", "amb-dock-icon")
    svg.setAttribute("aria-hidden", "true")
    let useEl = document.createElementNS(svgNs, "use")
    useEl.setAttribute("href", "#" + iconId)
    svg.appendChild useEl |> ignore
    svg :?> HTMLElement

let private appendDockIcon (btn: HTMLButtonElement) (iconId: string) : unit =
    btn.appendChild (makeDockIcon iconId) |> ignore

let private makeDockRow (accentClass: string) : HTMLElement =
    let row = document.createElement "div"
    row.className <- "amb-dock " + accentClass
    row

let private addGlyphClasses (btn: HTMLButtonElement) (classes: string list) : unit =
    for cls in classes do
        if cls <> "" then btn.classList.add cls

/// Keep focus and caret in `#edit-input` when tapping dock buttons while editing.
let private preventDockFocusSteal (btn: HTMLButtonElement) : unit =
    btn.addEventListener("pointerdown", fun (ev: Event) ->
        (ev :?> PointerEvent).preventDefault())

let private makeIconButton
        (label: string)
        (iconId: string)
        (extraClasses: string list)
        (onClick: unit -> unit)
        : HTMLButtonElement =
    let btn = document.createElement "button" :?> HTMLButtonElement
    btn.``type`` <- "button"
    btn.className <- "amb-dock-glyph"
    btn.title <- label
    btn.setAttribute("aria-label", label)
    appendDockIcon btn iconId
    addGlyphClasses btn extraClasses
    preventDockFocusSteal btn
    btn.addEventListener ("click", fun _ -> onClick ())
    btn

let private makeCommandIconButton
        (cmd: CommandEntry2)
        (dispatch: Msg -> unit)
        : HTMLButtonElement =
    let btn = document.createElement "button" :?> HTMLButtonElement
    btn.``type`` <- "button"
    btn.className <- "amb-dock-glyph"
    let label = CommandMeta.displayName cmd.id
    btn.title <- label
    btn.setAttribute("aria-label", label)
    match CommandMeta.commandFor cmd.id with
    | Some meta ->
        match meta.iconId with
        | Some iconId -> appendDockIcon btn iconId
        | None -> ()
    | None -> ()
    preventDockFocusSteal btn
    match cmd.run () with
    | None -> btn.classList.add "amb-inactive"
    | Some op ->
        btn.addEventListener ("click", fun _ -> dispatch (ApplyOp op))
    btn

let private appendDockSlot
        (row: HTMLElement)
        (model: VM)
        (dispatch: Msg -> unit)
        (slot: DockSlot)
        (refresh: VM -> (Msg -> unit) -> unit)
        : unit =
    match slot with
    | DockTrigger entry ->
        let isOpen = activeToolSurface = Some entry
        let extra =
            if isOpen then [ "amb-dock-trigger-open"; triggerDockCssClass entry ] else []
        let toggle =
            makeIconButton entry.name entry.iconId extra
                (fun () ->
                    activeToolSurface <- Some entry
                    refresh model dispatch)
        row.appendChild toggle |> ignore
    | DockCommand id ->
        match tryFindCommand id with
        | None -> ()
        | Some cmd ->
            row.appendChild (makeCommandIconButton cmd dispatch) |> ignore

let private renderDockSlots
        (accentClass: string)
        (slots: DockSlot list)
        (model: VM)
        (dispatch: Msg -> unit)
        (refresh: VM -> (Msg -> unit) -> unit)
        : HTMLElement =
    let row = makeDockRow accentClass
    for slot in slots do
        appendDockSlot row model dispatch slot refresh
    row

let private renderTriggerPanel
        (trigger: DockTriggerEntry)
        (model: VM)
        (dispatch: Msg -> unit)
        (refresh: VM -> (Msg -> unit) -> unit)
        : HTMLElement =
    renderDockSlots (triggerDockCssClass trigger) trigger.slots model dispatch refresh

let rec renderCommandButtons (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.querySelector ".amb-command-buttons"
    if isNull container then () else
    let snapshot = dockSnapshot model
    if lastDockSnapshot <> Some snapshot then
        lastDockSnapshot <- Some snapshot
        container.innerHTML <- ""

        let refresh = renderCommandButtons
        let baseRow = renderDockSlots (dockCssClass Primary) baseStripSlots model dispatch refresh
        container.appendChild baseRow |> ignore

        match activeToolSurface with
        | Some trigger ->
            let row = renderTriggerPanel trigger model dispatch refresh
            container.appendChild row |> ignore
        | None -> ()

// ---------------------------------------------------------------------------
// Command palette rendering
// ---------------------------------------------------------------------------

let private paletteWired = ref false

/// Populate the results list of a palette container, highlighting the selected item.
/// Upper-bounds selectedCommand to the list length to handle stale indices.
/// Scrolls the selected item into view so it stays visible when navigating with arrows.
let renderPalette (container: HTMLElement) (items: string list) (selectedCommand: int) : unit =
    let ul = container.querySelector ".amb-dialog-results" :?> HTMLElement
    ul.innerHTML <- ""
    let clampedSel = if items.IsEmpty then 0 else min selectedCommand (items.Length - 1)
    let mutable selectedLi: Element option = None
    items |> List.iteri (fun i label ->
        let li = document.createElement "li"
        li.textContent <- label
        if i = clampedSel then
            li.classList.add "amb-dialog-selected"
            selectedLi <- Some li
        ul.appendChild li |> ignore)
    selectedLi |> Option.iter (fun el -> scrollIntoViewNearest (el :?> HTMLElement))

/// Show or hide the command palette overlay and keep it up to date with the model.
/// Event listeners are wired once on the first call.
let renderCommandPalette (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.getElementById "command-palette"
    if isNull container then () else

    match model.mode with
    | CommandPalette (q, selectedCommand, ret) ->
        container.classList.add "amb-dialog-open"
        let input = document.getElementById "command-palette-input" :?> HTMLInputElement
        window.setTimeout((fun _ -> focusPreventScroll input), 0) |> ignore
        let items = filteredCommands model ret q |> List.map (fun c -> CommandMeta.displayName c.id)
        renderPalette container items selectedCommand

        if not paletteWired.Value then
            paletteWired.Value <- true
            let ul = document.getElementById "command-palette-results"

            input.addEventListener("input", fun _ ->
                dispatch (ApplyOp (paletteSetQueryOp input.value)))

            input.addEventListener("keydown", fun (ev: Event) ->
                let ke = ev :?> KeyboardEvent
                if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
                    ev.preventDefault()
                handlePaletteKey ke dispatch)

            ul.addEventListener("click", fun (ev: Event) ->
                let target = ev.target :?> HTMLElement
                match target.closest "li" with
                | None -> ()
                | Some li ->
                    let lis = ul.querySelectorAll "li"
                    let mutable idx = 0
                    for i in 0 .. int lis.length - 1 do
                        if System.Object.ReferenceEquals(lis.[i], li) then idx <- i
                    dispatch (ApplyOp (fun m ->
                        match m.mode with
                        | CommandPalette (q, _, ret) ->
                            match List.tryItem idx (filteredCommands m ret q) with
                            | None -> { m with mode = ret }, []
                            | Some cmd ->
                                match cmd.run () with
                                | None ->
                                    { m with mode = ret }, []
                                | Some op ->
                                    withDiagnostic
                                        (Some (CommandMeta.displayName cmd.id))
                                        op
                                        { m with mode = ret }
                        | _ -> m, [])))

    | _ ->
        container.classList.remove "amb-dialog-open"

let private cssClassPromptWired = ref false
let private cssClassPromptFilled = ref false

/// Show or hide the CSS class prompt overlay. Uses in-app modal instead of window.prompt for iPad.
let renderCssClassPrompt (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.getElementById "css-class-prompt"
    if isNull container then () else

    match model.mode with
    | CssClassPrompt (_, initialValue) ->
        container.classList.add "amb-dialog-open"
        let input = document.getElementById "css-class-prompt-input" :?> HTMLInputElement
        if not (isNull input) then
            if not cssClassPromptFilled.Value then
                cssClassPromptFilled.Value <- true
                input.value <- initialValue
            window.setTimeout((fun _ -> focusPreventScroll input), 0) |> ignore
            if not cssClassPromptWired.Value then
                cssClassPromptWired.Value <- true
                input.addEventListener("keydown", fun (ev: Event) ->
                    let ke = ev :?> KeyboardEvent
                    if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
                        ev.preventDefault()
                    handleCssClassPromptKey ke dispatch)
    | _ ->
        container.classList.remove "amb-dialog-open"
        cssClassPromptFilled.Value <- false
        let input = document.getElementById "css-class-prompt-input" :?> HTMLInputElement
        if not (isNull input) && input.value <> "" then
            input.value <- ""

let private renamePromptWired = ref false
let private renamePromptFilled = ref false

/// Show or hide the rename prompt overlay.
let renderRenamePrompt (model: VM) (dispatch: Msg -> unit) : unit =
    let container = document.getElementById "rename-prompt"
    if isNull container then () else

    match model.mode with
    | RenamePrompt (_, initialValue) ->
        container.classList.add "amb-dialog-open"
        let input = document.getElementById "rename-prompt-input" :?> HTMLInputElement
        if not (isNull input) then
            if not renamePromptFilled.Value then
                renamePromptFilled.Value <- true
                input.value <- initialValue
            window.setTimeout((fun _ ->
                focusPreventScroll input
                input.select()), 0) |> ignore
            if not renamePromptWired.Value then
                renamePromptWired.Value <- true
                input.addEventListener("keydown", fun (ev: Event) ->
                    let ke = ev :?> KeyboardEvent
                    if (ke.ctrlKey || ke.metaKey) && ke.key = "p" && not ke.shiftKey then
                        ev.preventDefault()
                    handleRenamePromptKey ke dispatch)
    | _ ->
        container.classList.remove "amb-dialog-open"
        renamePromptFilled.Value <- false
        let input = document.getElementById "rename-prompt-input" :?> HTMLInputElement
        if not (isNull input) && input.value <> "" then
            input.value <- ""

// ---------------------------------------------------------------------------
// status indicators
// ---------------------------------------------------------------------------

/// Update the persistent status element text and style.
let renderStatus (model: VM) : unit =
    let el = document.getElementById "sync-status"
    if not (isNull el) then
        match model.syncInfo.syncState with
        | Idle ->
            let txt = if model.syncInfo.isPollingActive then "synced" else "idle"
            el.textContent <- txt
            el.className <- "amb-sync-status amb-synced"
        | Sending 1 ->
            el.textContent <- "Saving\u2026"
            el.className <- "amb-sync-status amb-syncing"
        | Sending n ->
            el.textContent <- $"Saving\u2026 (try {n})"
            el.className <- "amb-sync-status amb-syncing"
        | Polling ->
            el.textContent <- "Checking\u2026"
            el.className <- "amb-sync-status amb-synced"
        | WaitingToRetry (n, _, _) ->
            el.textContent <- $"Unsaved \u2014 (try {n})"
            el.className <- "amb-sync-status amb-pending"
        | ServerRejected ->
            el.textContent <- "Server rejected change \u2014 reload required"
            el.className <- "amb-sync-status amb-stale"
        | CodeOutdated ->
            el.textContent <- "New version available \u2014 click to reload"
            el.className <- "amb-sync-status amb-stale"
        | DataOutdated ->
            el.textContent <- "Data changed on server \u2014 click to reload"
            el.className <- "amb-sync-status amb-stale"

    let dbEl = document.getElementById "db-status"
    if not (isNull dbEl) then
        match readDbPresent () with
        | "ok" ->
            dbEl.textContent <- "DB synced"
            dbEl.setAttribute("title", "PostgreSQL is configured and matches the file state.")
            dbEl.className <- "amb-db-status amb-db-present"
        | "mismatch1" ->
            dbEl.textContent <- "DB mismatch1"
            dbEl.setAttribute("title", "PostgreSQL mismatched the file state, was rebuilt, and now matches.")
            dbEl.className <- "amb-db-status amb-db-mismatch"
        | "mismatch2" ->
            dbEl.textContent <- "DB mismatch2"
            dbEl.setAttribute("title", "PostgreSQL still mismatches the file state after rebuild. Using file storage.")
            dbEl.className <- "amb-db-status amb-db-mismatch"
        | _ ->
            dbEl.textContent <- "Files only"
            dbEl.setAttribute("title", "PostgreSQL is not configured. Using file storage.")
            dbEl.className <- "amb-db-status amb-db-absent"

let private syncRiskAlertWired = ref false

/// Full-screen sync risk notice (ServerRejected / CodeOutdated / DataOutdated) until acknowledged.
let renderSyncRiskAlert (model: VM) (dispatch: Msg -> unit) : unit =
    let root = document.getElementById "blocking-alert"
    if isNull root then () else

    let shouldShow =
        match model.syncInfo.syncState with
        | ServerRejected | CodeOutdated | DataOutdated -> not model.syncInfo.syncRiskAcknowledged
        | _ -> false

    if shouldShow then
        root.classList.add "amb-blocking-alert-open"
        let titleEl = document.getElementById "blocking-alert-title"
        let msgEl = document.getElementById "blocking-alert-message"
        let okBtn = document.getElementById "blocking-alert-ok" :?> HTMLButtonElement
        if not (isNull titleEl) && not (isNull msgEl) then
            match model.syncInfo.syncState with
            | ServerRejected ->
                titleEl.textContent <- "Server rejected change"
                msgEl.textContent <-
                    "The server could not apply your change (revision mismatch or invalid op). "
                    + "Reload the page to resync. Your unsaved changes will be lost."
            | CodeOutdated ->
                titleEl.textContent <- "New version available"
                msgEl.textContent <-
                    "A new version of this application has been deployed. "
                    + "Reload the page to get the latest version."
            | DataOutdated ->
                titleEl.textContent <- "View is out of date"
                msgEl.textContent <-
                    "The server has newer data than this page. Reload before continuing."
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

/// Update the last-result display from the model.
let renderDiagnostics (model: VM) : unit =
    setCmdLastResultDisplay model.lastCmdResult

/// Status pill plus sync-risk overlay.
let renderSyncChrome (model: VM) (dispatch: Msg -> unit) : unit =
    renderStatus model
    renderSyncRiskAlert model dispatch
    renderDiagnostics model

// ---------------------------------------------------------------------------
// Full rebuild (StateLoaded)
// ---------------------------------------------------------------------------

/// Rebuild all row elements from scratch: removes existing rows (children of #amb-document
/// that precede the hidden-input sentinel), then recreates them in preorder.
/// Returns a fresh element cache keyed by instanceId.
let render (vm: VM) (dispatch: Msg -> unit) : Map<SiteId, HTMLElement> =
    let rowRoot =
        if isNull ambDocument then app else ambDocument
    // Remove existing rows — everything before the hidden-input sentinel
    let hiddenInput = document.getElementById "hidden-input"
    if isNull hiddenInput then
        rowRoot.innerHTML <- ""
    else
        let mutable sib = hiddenInput.previousSibling
        while not (isNull sib) do
            let prev = sib.previousSibling
            rowRoot.removeChild sib |> ignore
            sib <- prev

    let mutable cache = Map.empty<SiteId, HTMLElement>
    let visible = ViewModel.getVisibleInstanceIds vm.siteMap
    for instId in visible do
        let entry = vm.siteMap.entries.[instId]
        let depth = computeDepth vm.siteMap entry
        let row = makeRowElement vm dispatch depth entry
        cache <- Map.add instId row cache
        let sentinel = document.getElementById "hidden-input"
        if isNull sentinel then rowRoot.appendChild row |> ignore
        else rowRoot.insertBefore(row, sentinel) |> ignore

    manageFocus None vm cache
    renderSyncChrome vm dispatch
    cache

// ---------------------------------------------------------------------------
// Incremental DOM patch (all ops except StateLoaded)
// ---------------------------------------------------------------------------

/// Patch the DOM incrementally: diff old and new SiteMap visibility,
/// removes stale rows, creates/moves new rows, updates existing rows in-place.
/// Returns the updated element cache.
let patchDOM
        (oldModel: VM) (newModel: VM) (dispatch: Msg -> unit)
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

    let rowRoot =
        if isNull ambDocument then app else ambDocument

    // Apply upserts in preorder, correcting DOM position as we go
    let mutable prevNode: Browser.Types.Node option = None

    for instId in ViewModel.getVisibleInstanceIds newModel.siteMap do
        let entry = newModel.siteMap.entries.[instId]
        let depth = computeDepth newModel.siteMap entry

        let row, cache'' = resolveRow newModel dispatch depth entry instId upsertIndex cache'
        cache' <- cache''

        // Ensure the row sits in the correct DOM position (preorder sequence)
        let atCorrectPos =
            match prevNode with
            | None ->
                let first = rowRoot.firstChild
                not (isNull first) && System.Object.ReferenceEquals(first, row)
            | Some pe ->
                let ns = pe.nextSibling
                not (isNull ns) && System.Object.ReferenceEquals(ns, row)

        if not atCorrectPos then
            let anchor =
                match prevNode with
                | None -> rowRoot.firstChild
                | Some pe -> pe.nextSibling
            rowRoot.insertBefore(row, anchor) |> ignore

        prevNode <- Some (row :> Browser.Types.Node)

    manageFocus (Some oldModel) newModel cache'
    renderSyncChrome newModel dispatch
    cache'
