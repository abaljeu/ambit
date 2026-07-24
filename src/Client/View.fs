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
open Gambol.Client.StatusView
open Gambol.Shared.CommandDockLayout
open Gambol.Shared.CommandCategory
open Gambol.Shared.LogText

module CommandMeta = Gambol.Shared.CommandEntry

let private doubleTapScrollDeferMs = 400
let mutable private deferSelectionScroll = false
let mutable private pendingSelectionScrollTimer : float option = None
let mutable private pendingFoldToggleTimer : float option = None
let mutable private pendingFoldToggleInst : SiteId option = None

let private cancelPendingSelectionScroll () : unit =
    pendingSelectionScrollTimer |> Option.iter clearTimeout
    pendingSelectionScrollTimer <- None

let private cancelPendingFoldToggle () : unit =
    pendingFoldToggleTimer |> Option.iter clearTimeout
    pendingFoldToggleTimer <- None
    pendingFoldToggleInst <- None

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
// Zoom ingress path (clickable ancestors, above view-root row)
// ---------------------------------------------------------------------------

let private zoomPathEl (rowRoot: HTMLElement) : HTMLElement option =
    let el = rowRoot.querySelector ":scope > .amb-zoom-path"
    if isNull el then None else Some (el :?> HTMLElement)

/// Sync the breadcrumb above the zoom-root row. Hidden when path length < 2.
let private syncZoomPath (vm: VM) (dispatch: Msg -> unit) (rowRoot: HTMLElement) : HTMLElement option =
    let ids = ViewModel.zoomIngressPathIds vm.zoomRoot vm.zoomIngress
    let texts = ViewModel.zoomIngressPathTexts vm.graph vm.zoomRoot vm.zoomIngress
    match ids, texts with
    | [], _ | [ _ ], _ | _, [] | _, [ _ ] ->
        zoomPathEl rowRoot |> Option.iter (fun e -> e.remove())
        None
    | _ ->
        let el =
            match zoomPathEl rowRoot with
            | Some e -> e
            | None ->
                let e = document.createElement "div"
                e.className <- "amb-zoom-path"
                rowRoot.insertBefore (e, rowRoot.firstChild) |> ignore
                e
        while not (isNull el.firstChild) do
            el.removeChild el.firstChild |> ignore
        for i in 0 .. ids.Length - 1 do
            if i > 0 then
                let sep = document.createElement "span"
                sep.className <- "amb-zoom-path-sep"
                sep.textContent <- " \u203A "
                el.appendChild sep |> ignore
            let label = truncateForDisplay 13 texts.[i]
            let seg = document.createElement "span"
            if i = ids.Length - 1 then
                seg.className <- "amb-zoom-path-current"
                seg.textContent <- label
            else
                let targetId = ids.[i]
                seg.className <- "amb-zoom-path-seg"
                seg.textContent <- label
                seg.addEventListener("click", fun (ev: Event) ->
                    ev.preventDefault()
                    ev.stopPropagation()
                    dispatch (ApplyOp (zoomToIngressPathOp targetId)))
            el.appendChild seg |> ignore
        if not (isNull rowRoot.firstChild)
           && not (System.Object.ReferenceEquals(rowRoot.firstChild, el)) then
            rowRoot.insertBefore (el, rowRoot.firstChild) |> ignore
        Some el

let private firstRowAnchor (rowRoot: HTMLElement) : Browser.Types.Node =
    match zoomPathEl rowRoot with
    | Some zp -> zp.nextSibling
    | None -> rowRoot.firstChild

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
    match ViewModel.rowWorkspacePathSyncClass model siteEntry node with
    | Some cls -> row.classList.add cls
    | None -> ()
    if ViewModel.rowArtifactAbsentClassEligible model siteEntry node then
        row.classList.add "amb-row-artifact-absent"
    row.setAttribute("data-node-id", node.id.Value.ToString())

    if siteEntry.parentInstanceId = None then row.classList.add "amb-view-root"
    if isEntrySelected model siteEntry then row.classList.add "amb-selected"
    if isEntryFocused  model siteEntry then row.classList.add "amb-focused"
    match ViewModel.specialKindRowClass node.id node.kind with
    | Some cls -> row.classList.add cls
    | None -> ()

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
                let instId = siteEntry.instanceId
                match pendingFoldToggleInst with
                | Some prev when prev = instId ->
                    cancelPendingFoldToggle ()
                    dispatch (ApplyOp (fun model ->
                        let m, effs = selectInstance instId model
                        let m2, effs2 = zoomInOp m
                        m2, effs @ effs2))
                | _ ->
                    cancelPendingFoldToggle ()
                    pendingFoldToggleInst <- Some instId
                    pendingFoldToggleTimer <-
                        Some (
                            setTimeout
                                (fun () ->
                                    pendingFoldToggleTimer <- None
                                    pendingFoldToggleInst <- None
                                    let op =
                                        if siteEntry.parentInstanceId = None then zoomOutOp
                                        else toggleFoldOp instId
                                    dispatch (ApplyOp op))
                                doubleTapScrollDeferMs)
            )
            row.appendChild toggle |> ignore
            toggle
        else
            let dot = document.createElement "span"
            dot.classList.add "amb-fold-toggle"
            dot.classList.add "amb-leaf-dot"
            row.appendChild dot |> ignore
            dot
    let cssClasses = CssClass.toList node.cssClasses
    for cls in cssClasses do
        leafBullet.classList.add cls

    // One `.amb-text` per row; new row ⇒ new div. Same node for view and edit (contentEditable).
    let textDiv = document.createElement "div"
    textDiv.classList.add "amb-text"
    for cls in cssClasses do
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

    if not hasChildren then
        leafBullet.addEventListener("mousedown", activateRow)
        leafBullet.addEventListener("dblclick", doubleClickRow)

    let nameSpan = document.createElement "span"
    nameSpan.classList.add "amb-node-guid"
    nameSpan.textContent <- ViewModel.rowNameDisplayText node.name
    row.appendChild nameSpan |> ignore

    let fileIndicator = document.createElement "span"
    fileIndicator.classList.add "amb-file-indicator"
    let display, title = ViewModel.rowFileIndicator model siteEntry node
    fileIndicator.textContent <- display
    match title with
    | Some t -> fileIndicator.setAttribute("title", t)
    | None -> fileIndicator.removeAttribute "title"
    row.appendChild fileIndicator |> ignore
    row

/// Replace user classes on `el`: keep `amb-*`, drop everything else, add `cssClasses`.
let private syncUserCssClasses (el: HTMLElement) (classes: CssClasses) : unit =
    let stale =
        [ for i in 0 .. int el.classList.length - 1 do
            let c = el.classList.[i]
            if not (isNull c) && not (c.StartsWith "amb-") then yield c ]
    for c in stale do
        el.classList.remove c
    for cls in CssClass.toList classes do
        el.classList.add cls

/// Apply in-place patches to an existing row DOM element.
let private applyRowPatches (el: HTMLElement) (patches: RowPatch list) : unit =
    let ensureFileIndicator () =
        let indicator = el.querySelector ".amb-file-indicator"
        if isNull indicator then
            let created = document.createElement "span"
            created.classList.add "amb-file-indicator"
            let guid = el.querySelector ".amb-node-guid"
            if isNull guid then
                el.appendChild created |> ignore
            else
                let parent = guid.parentNode
                if isNull parent then el.appendChild created |> ignore
                else parent.insertBefore(created, guid.nextSibling) |> ignore
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
                syncUserCssClasses (textDiv :?> HTMLElement) classes
            let ft = el.querySelector ".amb-fold-toggle"
            if not (isNull ft) then
                syncUserCssClasses (ft :?> HTMLElement) classes
        | SetFoldArrow arrow ->
            let ft = el.querySelector ".amb-fold-toggle"
            if not (isNull ft) then (ft :?> HTMLElement).textContent <- arrow
        | SetNodeName name ->
            let g = el.querySelector ".amb-node-guid"
            if not (isNull g) then (g :?> HTMLElement).textContent <- name
        | SetFileIndicator (text, title) ->
            let indicator = ensureFileIndicator ()
            indicator.textContent <- text
            match title with
            | Some t -> indicator.setAttribute("title", t)
            | None -> indicator.removeAttribute "title"

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

/// Focus the correct element after a focus-relevant transition (`ManageFocus.shouldInvoke`).
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
            let alreadyFocused =
                not (isNull document.activeElement)
                && System.Object.ReferenceEquals(document.activeElement, root)
            // Re-focusing an unfocused contenteditable places the caret at start. When preserving
            // the live caret (same Editing mode ref), skip focus if we already own it.
            if not (preserveEditCaret && alreadyFocused) then
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
        let focusedInstId = ManageFocus.focusedSiteId model
        // Only scroll when the focused row changed (navigation) or on a full render.
        // This prevents the wheel-scroll snap-back caused by non-navigation dispatches.
        let prevFocusedInstId =
            match previousModel with
            | None -> None  // full render — always scroll
            | Some prev -> Some (ManageFocus.focusedSiteId prev)
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
        let wasOpen = container.classList.contains "amb-dialog-open"
        container.classList.add "amb-dialog-open"
        let input = document.getElementById "command-palette-input" :?> HTMLInputElement
        if input.value <> q then input.value <- q
        if not wasOpen then
            window.setTimeout((fun _ ->
                focusPreventScroll input
                input.select()), 0) |> ignore
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
        let input = document.getElementById "command-palette-input" :?> HTMLInputElement
        if not (isNull input) then input.value <- ""

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
            // Fill + focus/select only on open. Poll/sync re-renders must not call
            // input.select() or they reset the caret while the user is typing.
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
    syncZoomPath vm dispatch rowRoot |> ignore
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
    let preserveEditCaret =
        EditingCaretPreserve.shouldPreserveDomCaret (Some oldModel) newModel
    // Capture live caret before row patches; class/indicator writes can clear selection.
    // Restored below when preserveEditCaret (manageFocus may be skipped entirely).
    let savedEditCaret =
        if not preserveEditCaret then None
        else
            let el = document.getElementById "edit-input"
            if isNull el then None
            else Some (getContentEditableCaretOffset el)

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

    syncZoomPath newModel dispatch rowRoot |> ignore

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
                let first = firstRowAnchor rowRoot
                not (isNull first) && System.Object.ReferenceEquals(first, row)
            | Some pe ->
                let ns = pe.nextSibling
                not (isNull ns) && System.Object.ReferenceEquals(ns, row)

        if not atCorrectPos then
            let anchor =
                match prevNode with
                | None -> firstRowAnchor rowRoot
                | Some pe -> pe.nextSibling
            rowRoot.insertBefore(row, anchor) |> ignore

        prevNode <- Some (row :> Browser.Types.Node)

    if ManageFocus.shouldInvoke (Some oldModel) newModel then
    //if false then
        manageFocus (Some oldModel) newModel cache'
    //if preserveEditCaret then
    if false then
        match savedEditCaret with
        | Some pos ->
            let el = document.getElementById "edit-input"
            if not (isNull el) then setEditorCaret el pos
        | None -> ()
    renderSyncChrome newModel dispatch
    cache'
