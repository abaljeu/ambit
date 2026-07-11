namespace Gambol.Shared

// ---------------------------------------------------------------------------
// Selection / navigation helpers
// ---------------------------------------------------------------------------

module ViewModelSelection =

    open ViewModelSiteMap

    /// Build a single-node Selection for the given nodeId, using the graph to locate its parent
    /// and the site map to obtain the parent SiteEntry.
    /// Returns None if the node has no parent (i.e. it is the root) or if the parent is not in the site map.
    let singleSelection (graph: Graph) (siteMap: SiteMap) (nodeId: NodeId) : Selection option =
        Graph.tryFindParentAndIndex nodeId graph
        |> Option.bind (fun (parentId, index) ->
            siteMap.entries
            |> Map.tryPick (fun _ e -> if e.nodeId = parentId then Some e else None)
            |> Option.map (fun parentEntry ->
                { range = { parent = parentEntry; start = index; endd = index + 1 }; focus = index }))

    /// Build a single-node Selection for the given instanceId directly, without searching by NodeId.
    /// Use this for instance-aware navigation when a NodeId may appear multiple times.
    /// Returns None if the entry has no parent (i.e. it is the root) or if the parent is not in the site map.
    let singleSelectionForInstance (siteMap: SiteMap) (instanceId: SiteId) : Selection option =
        match Map.tryFind instanceId siteMap.entries with
        | None -> None
        | Some entry ->
            match entry.parentInstanceId with
            | None -> None
            | Some parentInstId ->
                match Map.tryFind parentInstId siteMap.entries with
                | None -> None
                | Some parentEntry ->
                    match parentEntry.children |> List.tryFindIndex ((=) instanceId) with
                    | None -> None
                    | Some idx -> Some { range = { parent = parentEntry; start = idx; endd = idx + 1 }; focus = idx }

    /// Refreshes a Selection against the current site map and graph.
    /// Clamps out-of-range indices under the same parent when possible; otherwise
    /// relocates via instance ids from the stale parent snapshot.
    let refreshSelection (graph: Graph) (siteMap: SiteMap) (sel: Selection) : Selection option =
        let clampUnderParent (parent: SiteEntry) (count: int) : Selection option =
            if count <= 0 then None
            else
                let start = max 0 (min sel.range.start (count - 1))
                let endd = max (start + 1) (min sel.range.endd count)
                let focus = max start (min sel.focus (endd - 1))
                Some
                    { range = { parent = parent; start = start; endd = endd }
                      focus = focus }

        let countUnderParent (parent: SiteEntry) =
            let visibleCount = parent.children.Length
            let graphCount =
                match Map.tryFind parent.nodeId graph.nodes with
                | None -> 0
                | Some node -> node.children.Length
            min visibleCount graphCount

        let relocateViaGraphFocus () =
            match Map.tryFind sel.range.parent.nodeId graph.nodes with
            | None -> None
            | Some parentNode ->
                parentNode.children
                |> List.tryItem sel.focus
                |> Option.map (fun child -> child.id)
                |> Option.bind (singleSelection graph siteMap)

        let relocateViaInstances () =
            let tryInst instId = singleSelectionForInstance siteMap instId
            let fromFocus =
                sel.range.parent.children
                |> List.tryItem sel.focus
                |> Option.bind tryInst
            let fromRange =
                [ sel.range.start .. sel.range.endd - 1 ]
                |> List.tryPick (fun i ->
                    List.tryItem i sel.range.parent.children |> Option.bind tryInst)
            fromFocus |> Option.orElse fromRange |> Option.orElseWith relocateViaGraphFocus

        let tryRefreshUnderParent (parent: SiteEntry) =
            clampUnderParent parent (countUnderParent parent)
            |> Option.orElseWith relocateViaInstances

        match Map.tryFind sel.range.parent.instanceId siteMap.entries with
        | Some parent when parent.nodeId = sel.range.parent.nodeId ->
            tryRefreshUnderParent parent
        | _ -> relocateViaInstances ()

    let private tryOriginalAdjacentNodeId (preGraph: Graph) (fromRange: SiteNodeRange) : NodeId option =
        let pid = fromRange.parent.nodeId
        let kids = preGraph.nodes.[pid].children
        if fromRange.start > 0 then Some kids.[fromRange.start - 1].id
        elif fromRange.endd < kids.Length then Some kids.[fromRange.endd].id
        else None

    /// Selection after a structural move. `stayAtSource` (Move Selected): sibling or parent at
    /// the old location, else None at view root with no sibling. Otherwise selection follows
    /// the moved block when `newParent` is expanded, or a bordering sibling / parent.
    let selectionAfterStructuralMove
            (preGraph: Graph)
            (postGraph: Graph)
            (postSiteMap: SiteMap)
            (fromRange: SiteNodeRange)
            (stayAtSource: bool)
            (newParent: SiteEntry)
            (insertIdx: int)
            (count: int)
            (focusOffset: int)
            : Selection option =
        if stayAtSource then
            let parent = postSiteMap.entries.[fromRange.parent.instanceId]
            let parentSel () =
                singleSelectionForInstance postSiteMap parent.instanceId
                |> Option.orElse (singleSelection postGraph postSiteMap parent.nodeId)
            tryOriginalAdjacentNodeId preGraph fromRange
            |> Option.bind (fun nid -> singleSelection postGraph postSiteMap nid)
            |> Option.orElseWith (fun () ->
                if parent.instanceId = postSiteMap.rootId then None else parentSel ())
        else
            let movedBlock () =
                let lo = min (max 0 focusOffset) (max 0 (count - 1))
                { range =
                      { parent = newParent
                        start = insertIdx
                        endd = insertIdx + count }
                  focus = insertIdx + lo }
            let sel =
                if newParent.expanded then movedBlock ()
                else
                    let parentSel () =
                        singleSelection postGraph postSiteMap fromRange.parent.nodeId
                    tryOriginalAdjacentNodeId preGraph fromRange
                    |> Option.bind (fun nid -> singleSelection postGraph postSiteMap nid)
                    |> Option.orElseWith parentSel
                    |> Option.defaultWith movedBlock
            Some sel

    /// Extract the first (start) selected NodeId from a Selection.
    let firstSelectedNodeId (graph: Graph) (sel: Selection) : NodeId =
        graph.nodes.[sel.range.parent.nodeId].children.[sel.range.start].id

    /// Extract the focused NodeId from a Selection (the active end, used for editing and Arrow movement).
    let focusedNodeId (graph: Graph) (sel: Selection) : NodeId =
        graph.nodes.[sel.range.parent.nodeId].children.[sel.focus].id

    /// Focused NodeId when parent and focus index are still valid in `graph`.
    let tryFocusedNodeId (graph: Graph) (sel: Selection) : NodeId option =
        match Map.tryFind sel.range.parent.nodeId graph.nodes with
        | None -> None
        | Some parent ->
            parent.children
            |> List.tryItem sel.focus
            |> Option.map (fun child -> child.id)

    /// Focused graph node for `sel`, if it still exists in `graph.nodes`.
    let tryFindFocusedNode (graph: Graph) (sel: Selection) : (NodeId * Node) option =
        let focusId = focusedNodeId graph sel
        Map.tryFind focusId graph.nodes |> Option.map (fun node -> focusId, node)

    /// Focused instance under `sel.range.parent` at `sel.focus`, if in range of that snapshot list.
    let focusedInstanceId (sel: Selection) : SiteId option =
        List.tryItem sel.focus sel.range.parent.children

    /// Shift-Arrow: move the focused end of the range by delta (-1 = up, +1 = down).
    /// For a single-node selection, always extends. For multi-node, the focused end moves.
    /// Focus follows the moved end. No-op if the move would exceed parent bounds.
    let shiftArrow (delta: int) (model: VM) : VM =
        match model.selectedNodes with
        | None -> model
        | Some sel ->
            let range = sel.range
            let childCount = model.graph.nodes.[range.parent.nodeId].children.Length
            let update r f = { model with selectedNodes = Some { range = r; focus = f } }
            let single = range.endd - range.start = 1
            let focusAtStart = sel.focus = range.start
            if delta < 0 then
                if focusAtStart then
                    // extend upward (single-node always lands here since focus = start)
                    let s = range.start - 1
                    if s < 0 then model else update { range with start = s } s
                else
                    // shrink from bottom
                    let e = range.endd - 1
                    if e <= range.start then model else update { range with endd = e } (e - 1)
            else
                if single || not focusAtStart then
                    // extend downward (single-node always extends; multi-node when focus is at end)
                    let e = range.endd + 1
                    if e > childCount then model else update { range with endd = e } (e - 1)
                else
                    // shrink from top
                    let s = range.start + 1
                    if s >= range.endd then model else update { range with start = s } s

    /// Collapse a multi-node selection to a single-node selection at the focus node, without moving.
    let collapseToFocus (model: VM) : VM =
        match model.selectedNodes with
        | None -> model
        | Some sel ->
            match focusedInstanceId sel with
            | None -> model
            | Some instId ->
                match singleSelectionForInstance model.siteMap instId with
                | None -> model
                | Some newSel -> { model with selectedNodes = Some newSel }

    /// Move current selection by delta (-1 for up, +1 for down) in visible row order.
    /// Collapses any multi-node selection to the focus node, then moves from there.
    /// Uses instanceId-based navigation so that duplicate NodeIds are treated as distinct positions.
    /// The resulting selection is always a single-node Selection.
    let moveSelectionBy (delta: int) (model: VM) : VM =
        match model.selectedNodes with
        | None -> model
        | Some sel ->
            match focusedInstanceId sel with
            | None -> model
            | Some instId ->
                let rows = getVisibleRowInstanceIds model.siteMap
                match rows |> List.tryFindIndex ((=) instId) with
                | None -> model
                | Some currentIndex ->
                    let nextIndex = currentIndex + delta
                    if nextIndex < 0 then
                        { model with selectedNodes = None; mode = Selecting }
                    elif nextIndex >= rows.Length then
                        model
                    else
                        let nextInstId = rows[nextIndex]
                        match singleSelectionForInstance model.siteMap nextInstId with
                        | None -> model
                        | Some newSel ->
                            { model with selectedNodes = Some newSel; mode = Selecting }

    let private applyMoveSelection (delta: int) (model: VM) : VM =
        match model.selectedNodes with
        | Some sel ->
            let focusEnd = if delta < 0 then sel.range.start else sel.range.endd - 1
            if sel.focus <> focusEnd then { model with selectedNodes = Some { sel with focus = focusEnd } }
            else moveSelectionBy delta model
        | None ->
            if delta > 0 then
                match getVisibleRowInstanceIds model.siteMap with
                | firstInstId :: _ ->
                    match singleSelectionForInstance model.siteMap firstInstId with
                    | Some sel -> { model with selectedNodes = Some sel; mode = Selecting }
                    | None -> model
                | [] -> model
            else model

    /// Pure portion of MoveSelectionUp: handles the non-editing cases.
    /// When focus is not at the range start, moves focus to start (keep range).
    /// Otherwise, moves the whole selection up by one visible row.
    let applyMoveSelectionUp = applyMoveSelection -1

    /// Pure portion of MoveSelectionDown: handles the non-editing cases.
    /// When focus is not at the range end, moves focus to end (keep range).
    /// Otherwise, moves the whole selection down by one visible row.
    let applyMoveSelectionDown = applyMoveSelection 1

    /// Single-node selection on the child at `childIndex` under `parent` (site-map occurrence).
    let private selectChildIndexUnderParent (model: VM) (parent: SiteEntry) (childIndex: int) : VM =
        let childCount = model.graph.nodes.[parent.nodeId].children.Length
        if childIndex < 0 || childIndex >= childCount then model
        else
            let instId = parent.children.[childIndex]
            match singleSelectionForInstance model.siteMap instId with
            | None -> model
            | Some s -> { model with selectedNodes = Some s; mode = Selecting }

    /// Cursor to the first sibling under the current selection's parent (no graph move).
    let cursorLevelStart (model: VM) : VM =
        match model.selectedNodes with
        | None -> model
        | Some sel -> selectChildIndexUnderParent model sel.range.parent 0

    /// Descend through last children while expanded, returning the deepest visible instance.
    let rec private lastVisibleDescendantOrSelf (siteMap: SiteMap) (instId: SiteId) : SiteId =
        match Map.tryFind instId siteMap.entries with
        | None -> instId
        | Some entry when not entry.expanded || entry.children.IsEmpty -> instId
        | Some entry -> lastVisibleDescendantOrSelf siteMap (List.last entry.children)

    /// Cursor to the last sibling under the current selection's parent, then recursively
    /// descend into expanded children to reach the last visible node.
    let cursorLevelEnd (model: VM) : VM =
        match model.selectedNodes with
        | None -> model
        | Some sel ->
            let p = sel.range.parent
            let n = model.graph.nodes.[p.nodeId].children.Length
            if n = 0 then model
            else
                let lastSibInstId = p.children.[n - 1]
                let targetInstId = lastVisibleDescendantOrSelf model.siteMap lastSibInstId
                match singleSelectionForInstance model.siteMap targetInstId with
                | None -> model
                | Some s -> { model with selectedNodes = Some s; mode = Selecting }

    /// Shift+PgDown / Shift+PgUp: extend the sibling range to the level end or start in one step
    /// (focus on last or first child under the same parent).
    let private shiftPgByDelta (delta: int) (model: VM) : VM =
        match model.selectedNodes with
        | None -> model
        | Some sel ->
            let r = sel.range
            let childCount = model.graph.nodes.[r.parent.nodeId].children.Length
            if childCount <= 0 || r.start >= childCount || r.endd > childCount then
                model
            elif delta > 0 then
                { model with
                    selectedNodes =
                        Some { range = { r with endd = childCount }
                               focus = childCount - 1 } }
            else
                { model with
                    selectedNodes = Some { range = { r with start = 0 }; focus = 0 } }

    /// Like repeated Shift+ArrowDown until the level boundary.
    let shiftPgDown = shiftPgByDelta 1

    /// Like repeated Shift+ArrowUp until the level boundary.
    let shiftPgUp = shiftPgByDelta -1

    /// Cursor to the first direct child of the view root (siteMap.rootId).
    let cursorViewRootFirstChild (model: VM) : VM =
        match Map.tryFind model.siteMap.rootId model.siteMap.entries with
        | None -> model
        | Some root when root.children.IsEmpty -> model
        | Some root -> selectChildIndexUnderParent model root 0

    /// Cursor to the last direct child of the view root (siteMap.rootId).
    let cursorViewRootLastChild (model: VM) : VM =
        match Map.tryFind model.siteMap.rootId model.siteMap.entries with
        | None -> model
        | Some root when root.children.IsEmpty -> model
        | Some root -> selectChildIndexUnderParent model root (root.children.Length - 1)
