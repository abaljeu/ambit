module Gambol.Client.RowView

open Browser.Dom
open Browser.Types
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Shared.LogText
open Gambol.Client.Controller
open Gambol.Client.JsInterop
open Gambol.Client.Update
open Gambol.Client.UpdateOps

/// Physical row / zoom-path DOM: structure, classes, text, indicators. No listeners.
module Layout =

    /// Depth of entry in the site map (root's children are at depth 0).
    let internal computeDepth (siteMap: SiteMap) (entry: SiteEntry) : int =
        let rec go (parentInstId: SiteId option) acc =
            match parentInstId with
            | None -> acc
            | Some pid ->
                match Map.tryFind pid siteMap.entries with
                | None -> acc
                | Some pe -> go pe.parentInstanceId (acc + 1)
        go entry.parentInstanceId 0

    let private zoomPathEl (rowRoot: HTMLElement) : HTMLElement option =
        let el = rowRoot.querySelector ":scope > .amb-zoom-path"
        if isNull el then None else Some (el :?> HTMLElement)

    /// Rebuild zoom-path DOM (structure/text only). Hidden when path length < 2.
    let internal rebuildZoomPath (vm: VM) (rowRoot: HTMLElement) : HTMLElement option =
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
                    seg.className <- "amb-zoom-path-seg"
                    seg.textContent <- label
                el.appendChild seg |> ignore
            if not (isNull rowRoot.firstChild)
               && not (System.Object.ReferenceEquals(rowRoot.firstChild, el)) then
                rowRoot.insertBefore (el, rowRoot.firstChild) |> ignore
            Some el

    let internal firstRowAnchor (rowRoot: HTMLElement) : Browser.Types.Node =
        match zoomPathEl rowRoot with
        | Some zp -> zp.nextSibling
        | None -> rowRoot.firstChild

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
    let internal applyRowPatches (el: HTMLElement) (patches: RowPatch list) : unit =
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

    /// Row skeleton: classes, indents, fold/leaf, text, name, file indicator. No listeners.
    let internal buildRowElement
        (model: VM) (depth: int) (siteEntry: SiteEntry)
        : HTMLElement * HTMLElement * HTMLElement * bool * RowChildrenIndicator =
        let nodeId = siteEntry.nodeId
        let node = model.graph.nodes.[nodeId]
        let childrenIndicator = ViewModel.rowChildrenIndicator node
        let hasChildren = childrenIndicator = RowChildrenIndicator.FoldChevron
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

        for _ in 1 .. depth do
            let indent = document.createElement "div"
            indent.classList.add "amb-indent"
            row.appendChild indent |> ignore

        let leafBullet =
            match childrenIndicator with
            | RowChildrenIndicator.FoldChevron ->
                let toggle = document.createElement "span"
                toggle.classList.add "amb-fold-toggle"
                toggle.textContent <- if siteEntry.expanded then "\u25BC" else "\u25B6"
                row.appendChild toggle |> ignore
                toggle
            | RowChildrenIndicator.SolidCircle ->
                let dot = document.createElement "span"
                dot.classList.add "amb-fold-toggle"
                dot.classList.add "amb-leaf-dot"
                row.appendChild dot |> ignore
                dot
            | RowChildrenIndicator.HollowCircle ->
                let dot = document.createElement "span"
                dot.classList.add "amb-fold-toggle"
                dot.classList.add "amb-leaf-hollow"
                row.appendChild dot |> ignore
                dot
        let cssClasses = CssClass.toList node.cssClasses
        for cls in cssClasses do
            leafBullet.classList.add cls

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
        else
            textDiv.removeAttribute "id"
            textDiv.contentEditable <- "false"
            textDiv.textContent <- ViewModel.outlineDisplayText node
        row.appendChild textDiv |> ignore

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
        row, textDiv, leafBullet, hasChildren, childrenIndicator

/// Listeners, scroll-defer / fold-toggle timers, dispatch wiring.
module Behavior =

    let private doubleTapScrollDeferMs = 400
    let mutable internal deferSelectionScroll = false
    let mutable private pendingSelectionScrollTimer : float option = None
    let mutable private pendingFoldToggleTimer : float option = None

    let internal cancelPendingSelectionScroll () : unit =
        pendingSelectionScrollTimer |> Option.iter clearTimeout
        pendingSelectionScrollTimer <- None

    let private cancelPendingFoldToggle () : unit =
        pendingFoldToggleTimer |> Option.iter clearTimeout
        pendingFoldToggleTimer <- None

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

    let internal scrollFocusedRow (el: HTMLElement) : unit =
        if deferSelectionScroll then scheduleDeferredSelectionScroll el
        else scrollIntoViewNearest el

    let internal wireZoomPath
        (dispatch: Msg -> unit) (ids: NodeId list) (el: HTMLElement) : unit =
        let segs = el.querySelectorAll ".amb-zoom-path-seg"
        for i in 0 .. int segs.length - 1 do
            let targetId = ids.[i]
            let seg = segs.[i] :?> HTMLElement
            seg.addEventListener("click", fun (ev: Event) ->
                ev.preventDefault()
                ev.stopPropagation()
                dispatch (ApplyOp (zoomToIngressPathOp targetId)))

    let private wireFoldToggle
        (dispatch: Msg -> unit) (siteEntry: SiteEntry) (toggle: HTMLElement) : unit =
        toggle.addEventListener("mousedown", fun (ev: Event) ->
            ev.preventDefault()
            ev.stopPropagation()
            let instId = siteEntry.instanceId
            cancelPendingFoldToggle ()
            pendingFoldToggleTimer <-
                Some (
                    setTimeout
                        (fun () ->
                            pendingFoldToggleTimer <- None
                            let op =
                                if siteEntry.parentInstanceId = None then zoomOutOp
                                else toggleFoldOp instId
                            dispatch (ApplyOp op))
                        doubleTapScrollDeferMs)
        )
        toggle.addEventListener("dblclick", fun (ev: Event) ->
            ev.preventDefault()
            ev.stopPropagation()
            cancelPendingFoldToggle ()
            let instId = siteEntry.instanceId
            dispatch (ApplyOp (fun model ->
                let m, effs = selectInstance instId model
                let m2, effs2 = zoomInOp m
                m2, effs @ effs2))
        )

    let private wireEditText
        (model: VM) (dispatch: Msg -> unit) (textDiv: HTMLElement) : unit =
        let effectiveMode =
            match model.mode with
            | CommandPalette (_, _, ret) -> ret
            | SearchDialog s -> s.returnTo
            | FileSearchDialog s -> s.returnTo
            | CssClassPrompt (ret, _) -> ret
            | RenamePrompt (ret, _) -> ret
            | m -> m
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

    let private wireSelectingActivate
        (dispatch: Msg -> unit) (siteEntry: SiteEntry)
        (textDiv: HTMLElement) (leafBullet: HTMLElement) (hasChildren: bool) : unit =
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

        textDiv.addEventListener("mousedown", activateRow)
        textDiv.addEventListener("dblclick", doubleClickRow)
        if not hasChildren then
            leafBullet.addEventListener("mousedown", activateRow)
            leafBullet.addEventListener("dblclick", doubleClickRow)

    /// Attach row listeners and scroll-defer flag writes to a layout-built row.
    let internal wireRow
        (model: VM) (dispatch: Msg -> unit) (siteEntry: SiteEntry)
        (textDiv: HTMLElement) (leafBullet: HTMLElement)
        (hasChildren: bool) (childrenIndicator: RowChildrenIndicator) : unit =
        match childrenIndicator with
        | RowChildrenIndicator.FoldChevron ->
            wireFoldToggle dispatch siteEntry leafBullet
        | _ -> ()
        if isEditingEntry model siteEntry then
            wireEditText model dispatch textDiv
        else
            wireSelectingActivate dispatch siteEntry textDiv leafBullet hasChildren

// ---------------------------------------------------------------------------
// Public compose / re-exports (View + FocusView call sites stay name-stable)
// ---------------------------------------------------------------------------

let internal computeDepth = Layout.computeDepth
let internal firstRowAnchor = Layout.firstRowAnchor
let internal cancelPendingSelectionScroll = Behavior.cancelPendingSelectionScroll
let internal scrollFocusedRow = Behavior.scrollFocusedRow

/// Sync the breadcrumb above the zoom-root row. Hidden when path length < 2.
let internal syncZoomPath
    (vm: VM) (dispatch: Msg -> unit) (rowRoot: HTMLElement) : HTMLElement option =
    let ids = ViewModel.zoomIngressPathIds vm.zoomRoot vm.zoomIngress
    match Layout.rebuildZoomPath vm rowRoot with
    | None -> None
    | Some el ->
        Behavior.wireZoomPath dispatch ids el
        Some el

/// Create a fresh DOM row for the given SiteEntry at the given depth.
let internal makeRowElement
    (model: VM) (dispatch: Msg -> unit) (depth: int) (siteEntry: SiteEntry) : HTMLElement =
    let row, textDiv, leafBullet, hasChildren, childrenIndicator =
        Layout.buildRowElement model depth siteEntry
    Behavior.wireRow model dispatch siteEntry textDiv leafBullet hasChildren childrenIndicator
    row

/// Resolve the row element for an instance: create, recreate, or patch as dictated by the upsert index.
/// Returns the row element and the updated cache.
let internal resolveRow
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
        Layout.applyRowPatches el patches
        (el, cache)
    | _ ->  // CreateRow or missing
        let el = makeRowElement newModel dispatch depth entry
        (el, Map.add instId el cache)
