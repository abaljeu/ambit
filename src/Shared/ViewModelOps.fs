namespace Gambol.Shared

// ---------------------------------------------------------------------------
// Pure view-model helpers (no DOM / Fable interop)
// ---------------------------------------------------------------------------

module ViewModel =

    let buildParentInstanceIndex (entries: Map<SiteId, SiteEntry>) : Map<SiteId, SiteId> =
        entries
        |> Map.toSeq
        |> Seq.choose (fun (_, e) ->
            e.parentInstanceId |> Option.map (fun p -> e.instanceId, p))
        |> Map.ofSeq

    let emptySiteMap : SiteMap =
        let rootEntry = { instanceId = Sid 0; nodeId = Graph.rootId
                          parentInstanceId = None; expanded = true; childrenStale = false; children = [] }
        let entries = Map.ofList [Sid 0, rootEntry]
        { rootId = Sid 0
          entries = entries
          parentByInstanceId = buildParentInstanceIndex entries }

    // Returns (freshId generator, counter getter) for sequencing SiteIds.
    let private makeCounter (start: SiteId) =
        let (Sid n) = start
        let mutable curr = n
        (fun () -> let id = curr in curr <- curr + 1; Sid id), (fun () -> Sid curr)

    let private shouldExpandChildren (isRoot: bool) (expanded: bool) : bool =
        isRoot || expanded


    let rec private walkReconciled
        (graph: Graph)
        (oldMap: SiteMap)
        (freshId: unit -> SiteId)
        (acc: Map<SiteId, SiteEntry> ref)
        (nodeId: NodeId)
        (parentInstId: SiteId option)
        (isRoot: bool)
        (oldInstIdOpt: SiteId option) : SiteId =
        let oldEntryOpt =
            oldInstIdOpt
            |> Option.bind (fun id -> Map.tryFind id oldMap.entries)
            |> Option.bind (fun e -> if e.nodeId = nodeId then Some e else None)
        let instId, expanded =
            match oldEntryOpt with
            | Some old -> old.instanceId, old.expanded
            | None -> freshId (), false
        let childInstIds =
            resolveChildInstIds graph oldMap freshId acc nodeId isRoot expanded oldEntryOpt instId
        let entry =
            { instanceId = instId; nodeId = nodeId; parentInstanceId = parentInstId
              expanded = if isRoot then true else expanded
              childrenStale = false; children = childInstIds }
        acc.Value <- Map.add instId entry acc.Value
        instId

    and private resolveChildInstIds
        (graph: Graph)
        (oldMap: SiteMap)
        (freshId: unit -> SiteId)
        (acc: Map<SiteId, SiteEntry> ref)
        (nodeId: NodeId)
        (isRoot: bool)
        (expanded: bool)
        (oldEntryOpt: SiteEntry option)
        (instId: SiteId) : SiteId list =
        if shouldExpandChildren isRoot expanded then
            let node = graph.nodes.[nodeId]
            let oldChildren = oldEntryOpt |> Option.map (fun o -> o.children) |> Option.defaultValue []
            let usedIds = ref Set.empty<SiteId>
            node.children |> List.mapi (fun i child ->
                let oldChildOpt =
                    let positional =
                        List.tryItem i oldChildren
                        |> Option.bind (fun oid -> Map.tryFind oid oldMap.entries)
                        |> Option.bind (fun e -> if e.nodeId = child.id
                                                 then Some e
                                                 else None)
                    match positional with
                    | Some old when not (Set.contains old.instanceId usedIds.Value) ->
                        usedIds.Value <- Set.add old.instanceId usedIds.Value
                        Some old
                    | Some _ -> None
                    | None ->
                        // Positional match failed (e.g. nodes reordered, or duplicate refs).
                        // Fall back to searching old children by nodeId — but only reuse if
                        // that instance hasn't already been assigned (avoids duplicate
                        // instanceIds when the same NodeId appears multiple times as references).
                        oldChildren
                        |> List.tryPick (fun oid ->
                            Map.tryFind oid oldMap.entries
                            |> Option.bind (fun e ->
                                if e.nodeId = child.id && not (Set.contains e.instanceId usedIds.Value)
                                then usedIds.Value <- Set.add e.instanceId usedIds.Value; Some e
                                else None))
                match oldChildOpt with
                | Some old when old.expanded ->
                    walkReconciled graph oldMap freshId acc child.id 
                        (Some instId) false (Some old.instanceId)
                | Some old ->
                    acc.Value <- Map.add old.instanceId 
                        { old with childrenStale = true; children = [] } 
                        acc.Value
                    old.instanceId
                | None ->
                    let newId = freshId ()
                    acc.Value <- Map.add newId { instanceId = newId; nodeId = child.id
                                                 parentInstanceId = Some instId
                                                 expanded = false; 
                                                 childrenStale = true; 
                                                 children = [] 
                                                 } 
                                            acc.Value
                    newId)
        else []

    /// Build a SiteMap rooted at rootNodeId. The root is expanded; its immediate children are
    /// collapsed with children = [] and childrenStale = true, populated on demand via expandEntry.
    /// Cycle termination is implicit: new entries start collapsed with no children, so expanding
    /// an ancestor reachable via a back-edge produces a collapsed leaf that stops the recursion.
    /// Returns the SiteMap and the next available instanceId.
    let buildSiteMapFrom (graph: Graph) (rootNodeId: NodeId) (startId: SiteId) : SiteMap * SiteId =
        let freshId, endCount = makeCounter startId
        let mutable acc = Map.empty<SiteId, SiteEntry>
        let rootInstId = freshId ()
        let rootNode = graph.nodes.[rootNodeId]
        let childInstIds =
            rootNode.children |> List.map (fun child ->
                let childId = freshId ()
                acc <- Map.add childId { instanceId = childId; nodeId = child.id
                                         parentInstanceId = Some rootInstId
                                         expanded = false; childrenStale = true; children = [] } acc
                childId)
        acc <- Map.add rootInstId { instanceId = rootInstId; nodeId = rootNodeId
                                    parentInstanceId = None
                                    expanded = true; childrenStale = false; children = childInstIds } acc
        { rootId = rootInstId
          entries = acc
          parentByInstanceId = buildParentInstanceIndex acc }, endCount ()

    /// Build a SiteMap from the graph root. See buildSiteMapFrom for details.
    let buildSiteMap (graph: Graph) : SiteMap * SiteId =
        buildSiteMapFrom graph graph.root (Sid 0)

    /// Selection spanning the first child under the site-map entry for `rootNodeId`.
    /// Returns None if that entry has no children.
    let firstChildSelection (siteMap: SiteMap) (rootNodeId: NodeId) : Selection option =
        siteMap.entries
        |> Map.tryPick (fun _ e -> if e.nodeId = rootNodeId then Some e else None)
        |> Option.bind (fun rootEntry ->
            if rootEntry.children.IsEmpty then None
            else Some { range = { parent = rootEntry; start = 0; endd = 1 }; focus = 0 })

    /// Selection spanning the child at `index` under the site-map entry for `rootNodeId`.
    /// Clamps `index` to [0, children.Length - 1]. Returns None if that entry has no children.
    let childSelectionAt (siteMap: SiteMap) (rootNodeId: NodeId) (index: int) : Selection option =
        siteMap.entries
        |> Map.tryPick (fun _ e -> if e.nodeId = rootNodeId then Some e else None)
        |> Option.bind (fun rootEntry ->
            if rootEntry.children.IsEmpty then None
            else
                let i = max 0 (min index (rootEntry.children.Length - 1))
                Some { range = { parent = rootEntry; start = i; endd = i + 1 }; focus = i })

    /// Reconcile a SiteMap rooted at rootNodeId after a graph change. Walks only expanded entries,
    /// syncing their children lists from the graph. Collapsed children of expanded entries are
    /// reused by position (nodeId must match) with childrenStale = true and children = []; they
    /// are not recursed into. Orphaned entries from removed or now-unexpanded paths are dropped.
    /// Returns the updated SiteMap and next available instanceId.
    let reconcileSiteMapFrom (graph: Graph) (rootNodeId: NodeId) (oldMap: SiteMap) (startId: SiteId) : SiteMap * SiteId =
        let freshId, endCount = makeCounter startId
        let acc = ref Map.empty<SiteId, SiteEntry>
        let rootInstId = walkReconciled graph oldMap freshId acc rootNodeId None true (Some oldMap.rootId)
        let entries = acc.Value
        { rootId = rootInstId
          entries = entries
          parentByInstanceId = buildParentInstanceIndex entries }, endCount ()

    /// Reconcile a SiteMap from the graph root after a graph change. See reconcileSiteMapFrom for details.
    let reconcileSiteMap (graph: Graph) (oldMap: SiteMap) (startId: SiteId) : SiteMap * SiteId =
        reconcileSiteMapFrom graph graph.root oldMap startId

    /// Collapse the entry with the given instanceId, marking its children as stale. O(log S).
    /// The children list is preserved so that a subsequent expandEntry can reuse instanceIds
    /// positionally (restoring nested fold state) when no structural op has intervened.
    /// For expanding, use expandEntry which re-syncs children from the graph.
    let toggleFold (instanceId: SiteId) (siteMap: SiteMap) : SiteMap =
        match Map.tryFind instanceId siteMap.entries with
        | None -> siteMap
        | Some entry ->
            { siteMap with entries = Map.add instanceId { entry with expanded = false; childrenStale = true } siteMap.entries }

    /// Expand a collapsed entry, inserting or re-syncing immediate child SiteEntries from the graph.
    /// Children are matched positionally by nodeId to preserve existing instanceIds and fold state
    /// (useful when re-expanding after a simple collapse with no intervening structural op).
    /// New children start collapsed with childrenStale = true and children = [].
    /// Returns the updated SiteMap and next available instanceId.
    let expandEntry (instanceId: SiteId) (graph: Graph) (siteMap: SiteMap) (startId: SiteId) : SiteMap * SiteId =
        match Map.tryFind instanceId siteMap.entries with
        | None -> siteMap, startId
        | Some entry ->
            if entry.expanded then siteMap, startId
            else
                let freshId, endCount = makeCounter startId
                let mutable acc = siteMap.entries
                let node = graph.nodes.[entry.nodeId]
                let childInstIds =
                    node.children |> List.mapi (fun i child ->
                        // Reuse existing child entry at this position if nodeId matches
                        match List.tryItem i entry.children |> Option.bind (fun oid -> Map.tryFind oid acc) with
                        | Some existing when existing.nodeId = child.id -> existing.instanceId
                        | _ ->
                            let newId = freshId ()
                            acc <- Map.add newId { instanceId = newId; nodeId = child.id
                                                   parentInstanceId = Some instanceId
                                                   expanded = false; childrenStale = true; children = [] } acc
                            newId)
                let updated = { entry with expanded = true; childrenStale = false; children = childInstIds }
                acc <- Map.add instanceId updated acc
                { siteMap with
                    entries = acc
                    parentByInstanceId = buildParentInstanceIndex acc }, endCount ()

    /// Restore fold state from a saved set of expanded NodeIds.
    /// Walks the siteMap in BFS order, expanding each entry whose nodeId is in
    /// expandedNodeIds.  Parent-before-child ordering ensures that children only
    /// become visible after their parent is expanded.
    /// Returns the updated SiteMap and next available instanceId.
    let applyFoldSession (expandedNodeIds: Set<NodeId>) (graph: Graph) (siteMap: SiteMap) (startId: SiteId) : SiteMap * SiteId =
        if Set.isEmpty expandedNodeIds then siteMap, startId
        else
            let mutable sm = siteMap
            let mutable nextId = startId
            let queue = System.Collections.Generic.Queue<SiteId>()
            queue.Enqueue(sm.rootId)
            while queue.Count > 0 do
                let instId = queue.Dequeue()
                match Map.tryFind instId sm.entries with
                | None -> ()
                | Some entry ->
                    if Set.contains entry.nodeId expandedNodeIds && not entry.expanded then
                        let sm', nextId' = expandEntry instId graph sm nextId
                        sm <- sm'
                        nextId <- nextId'
                    // Re-read after potential expansion to enqueue the (now visible) children.
                    match Map.tryFind instId sm.entries with
                    | Some e when e.expanded ->
                        for childId in e.children do queue.Enqueue(childId)
                    | _ -> ()
            sm, nextId

    /// Build an index from NodeId to all instanceIds (all occurrences). O(S log S).
    let buildOccurrenceIndex (siteMap: SiteMap) : Map<NodeId, SiteId list> =
        siteMap.entries
        |> Map.fold (fun acc _ entry ->
            let existing = acc |> Map.tryFind entry.nodeId |> Option.defaultValue []
            Map.add entry.nodeId (entry.instanceId :: existing) acc)
            Map.empty

    /// Preorder walk of visible entries, returning NodeIds in display order (excluding root).
    /// Respects fold state: unexpanded entries do not contribute their children.
    let getVisibleRowIds (siteMap: SiteMap) : NodeId list =
        let entries = siteMap.entries
        let rec gather (instId: SiteId) : NodeId list =
            match Map.tryFind instId entries with
            | None -> []
            | Some entry ->
                entry.nodeId ::
                    (if entry.expanded then entry.children |> List.collect gather
                     else [])
        match Map.tryFind siteMap.rootId entries with
        | None -> []
        | Some root -> root.children |> List.collect gather

    /// Preorder walk of visible entries, returning instanceIds in display order (excluding root).
    /// Mirrors getVisibleRowIds but keyed by instanceId. Use this for instance-aware navigation
    /// so that duplicate NodeIds are treated as distinct positions.
    let getVisibleRowInstanceIds (siteMap: SiteMap) : SiteId list =
        let entries = siteMap.entries
        let rec gather (instId: SiteId) : SiteId list =
            match Map.tryFind instId entries with
            | None -> []
            | Some entry ->
                entry.instanceId ::
                    (if entry.expanded then entry.children |> List.collect gather
                     else [])
        match Map.tryFind siteMap.rootId entries with
        | None -> []
        | Some root -> root.children |> List.collect gather

    /// Preorder walk of visible entries, returning instanceIds in display order (including root).
    /// Mirrors getVisibleRowIds but keyed by instanceId for DOM-cache lookups.
    let getVisibleInstanceIds (siteMap: SiteMap) : SiteId list =
        let entries = siteMap.entries
        let rec gather (instId: SiteId) : SiteId list =
            match Map.tryFind instId entries with
            | None -> []
            | Some entry ->
                entry.instanceId ::
                    (if entry.expanded then entry.children |> List.collect gather
                     else [])
        match Map.tryFind siteMap.rootId entries with
        | None -> []
        | Some root ->
            root.instanceId ::
                (root.children |> List.collect gather)

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
    /// Returns None when the parent occurrence is gone or range bounds are invalid.
    let refreshSelection (graph: Graph) (siteMap: SiteMap) (sel: Selection) : Selection option =
        let parentOpt = Map.tryFind sel.range.parent.instanceId siteMap.entries
        let mkRefreshed parent =
            let visibleCount = parent.children.Length
            let graphCount = graph.nodes.[parent.nodeId].children.Length
            let count = min visibleCount graphCount
            let start = sel.range.start
            let finish = sel.range.endd
            let focus = sel.focus
            if start < 0 || finish < 0 || focus < 0 then
                None
            elif start >= finish then
                None
            elif finish > count then
                None
            elif focus < start || focus >= finish then
                None
            else
                Some
                    { range = { parent = parent; start = start; endd = finish }
                      focus = focus }
        match parentOpt with
        | Some parent when parent.nodeId = sel.range.parent.nodeId -> mkRefreshed parent
        | _ -> None

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

// ---------------------------------------------------------------------------
// Selection / focus / edit helpers (pure — no Browser interop)
// ---------------------------------------------------------------------------

    /// True when entry is directly within the selected index range AND is a child
    /// of the exact same parent instance that the selection was made on.
    /// Prevents sibling occurrences of the same NodeId (DIGRAPH links) from lighting up.
    let private isInstanceDirectlySelected (sel: Selection) (siteMap: SiteMap) (entry: SiteEntry) : bool =
        match entry.parentInstanceId with
        | Some parentInstId when parentInstId = sel.range.parent.instanceId ->
            match Map.tryFind parentInstId siteMap.entries with
            | None -> false
            | Some parentEntry ->
                match parentEntry.children |> List.tryFindIndex ((=) entry.instanceId) with
                | Some idx -> idx >= sel.range.start && idx < sel.range.endd
                | None -> false
        | _ -> false

    /// True when entry is at the focused index AND is a child of the exact same
    /// parent instance that the selection was made on.
    let private isInstanceFocused (sel: Selection) (siteMap: SiteMap) (entry: SiteEntry) : bool =
        match entry.parentInstanceId with
        | Some parentInstId when parentInstId = sel.range.parent.instanceId ->
            match Map.tryFind parentInstId siteMap.entries with
            | None -> false
            | Some parentEntry ->
                match parentEntry.children |> List.tryFindIndex ((=) entry.instanceId) with
                | Some idx -> idx = sel.focus
                | None -> false
        | _ -> false

    /// Walk up the parentInstanceId chain: true if entry or any ancestor satisfies pred.
    let private ancestorMatch (siteMap: SiteMap) (entry: SiteEntry) (pred: SiteEntry -> bool) : bool =
        let rec go parentInstId =
            match parentInstId with
            | None -> false
            | Some pid ->
                match Map.tryFind pid siteMap.entries with
                | None -> false
                | Some pe -> pred pe || go pe.parentInstanceId
        pred entry || go entry.parentInstanceId

    let isEntrySelected (model: VM) (entry: SiteEntry) =
        if model.selectedNodes = None && entry.parentInstanceId = None then true
        else
            match model.selectedNodes with
            | None -> false
            | Some sel -> ancestorMatch model.siteMap entry (isInstanceDirectlySelected sel model.siteMap)

    let isEntryFocused (model: VM) (entry: SiteEntry) =
        if model.selectedNodes = None && entry.parentInstanceId = None then true
        else
            match model.selectedNodes with
            | None -> false
            | Some sel -> ancestorMatch model.siteMap entry (isInstanceFocused sel model.siteMap)

    let isEditingEntry (model: VM) (entry: SiteEntry) : bool =
        let effectiveMode =
            match model.mode with
            | CommandPalette (_, _, ret) -> ret
            | SearchDialog s -> s.returnTo
            | CssClassPrompt (ret, _) -> ret
            | m -> m
        match effectiveMode, model.selectedNodes with
        | Editing _, None    -> entry.parentInstanceId = None
        | Editing _, Some sel -> isInstanceFocused sel model.siteMap entry
        | _ -> false

    let isActiveEntry (model: VM) (entry: SiteEntry) : bool =
        match model.selectedNodes with
        | None -> entry.instanceId = model.siteMap.rootId
        | Some sel -> focusedInstanceId sel = Some entry.instanceId

    let activeNodeId (model: VM) : NodeId option =
        match model.selectedNodes with
        | None ->
            model.siteMap.entries
            |> Map.tryFind model.siteMap.rootId
            |> Option.map (fun entry -> entry.nodeId)
        | Some sel ->
            Some (focusedNodeId model.graph sel)

    let tryFindFocusedPath (graph: Graph) (sel: Selection) : (NodeId * string) option =
        let focusId = focusedNodeId graph sel

        NodeDesktopPath.pathForNodeId graph focusId
        |> Option.map (fun path -> focusId, path)

    let activeFileReference (model: VM) : (NodeId * FileReference) option =
        activeNodeId model
        |> Option.bind (fun nodeId ->
            NodeDesktopPath.fileReferenceForNodeId model.graph nodeId
            |> Option.map (fun fileRef -> nodeId, fileRef))

    let private indicatorMatches nodeId path =
        function
        | CheckingFileStatus (indicatorNodeId, indicatorPath)
        | FileStatusIndicator (indicatorNodeId, indicatorPath, _, _) ->
            indicatorNodeId = nodeId && indicatorPath = path
        | _ -> false

    let refreshDesktopFileIndicator (model: VM) : VM * Effect list =
        match activeFileReference model with
        | None
        | Some (_, NoFileReference) ->
            { model with desktopFileIndicator = BlankFileIndicator }, []
        | Some (_, InvalidFileReference) ->
            { model with desktopFileIndicator = InvalidFileReferenceIndicator }, []
        | Some (nodeId, FileReference path) ->
            match model.desktopCapabilities with
            | None -> { model with desktopFileIndicator = BlankFileIndicator }, []
            | Some { file = { canStatus = false } } ->
                { model with desktopFileIndicator = BlankFileIndicator }, []
            | Some _ when indicatorMatches nodeId path model.desktopFileIndicator -> model, []
            | Some { file = { canStatus = true } } ->
                { model with desktopFileIndicator = CheckingFileStatus (nodeId, path) },
                [ RequestDesktopFileStatus (nodeId, path) ]

    let applyDesktopFileStatus
        (nodeId: NodeId)
        (path: string)
        (status: DesktopFileStatus)
        (sourceModifiedUtc: System.DateTime option)
        (model: VM)
        : VM =
        match activeFileReference model with
        | Some (activeNodeId, FileReference activePath)
            when activeNodeId = nodeId && activePath = path ->
            { model with
                desktopFileIndicator =
                    FileStatusIndicator (nodeId, path, status, sourceModifiedUtc) }
        | _ -> model

    let desktopFileIndicatorText (model: VM) (entry: SiteEntry) (node: Node) : string =
        if not (isActiveEntry model entry) then ""
        else
            match model.desktopFileIndicator with
            | BlankFileIndicator -> ""
            | CheckingFileStatus _ -> "..."
            | InvalidFileReferenceIndicator -> "invalid"
            | FileStatusIndicator (_, _, status, sourceModifiedUtc) ->
                FileSyncIndicator.indicatorTextForStatus node.updateTime status sourceModifiedUtc

    let specialKindRowClass (kind: NodeKind) : string option =
        match kind with
        | Normal -> None
        | Special Workspaces -> Some "amb-row-special-workspaces"
        | Special Workspace -> Some "amb-row-special-workspace"
        | Special Directory -> Some "amb-row-special-directory"
        | Special File -> Some "amb-row-special-file"
        | Special Trash -> Some "amb-row-special-trash"

    let specialKindSymbol (kind: NodeKind) : string option =
        match kind with
        | Normal -> None
        | Special Workspaces -> Some "\u229E"
        | Special Workspace -> Some "@"
        | Special Directory -> Some "\u25A4"
        | Special File -> Some "\u2261"
        | Special Trash -> Some "\u00D7"

    let rowFileIndicatorText (model: VM) (entry: SiteEntry) (node: Node) : string =
        let desktop = desktopFileIndicatorText model entry node
        if desktop <> "" then desktop
        else specialKindSymbol node.kind |> Option.defaultValue ""

    let private addSpecialKindRowClass (kind: NodeKind) (className: string) : string =
        match specialKindRowClass kind with
        | Some sk -> CssClass.add sk className
        | None -> className

// ---------------------------------------------------------------------------
// DOM mutation plan (pure — no Browser interop)
// ---------------------------------------------------------------------------

    type RowPatch =
        | SetClassName of newClass: string
        | SetText of newText: string
        | SetTextClasses of classes: CssClasses
        | SetFoldArrow of arrow: string   // "▼" or "▶" (has children); "●" (no children, no behavior)
        | SetNodeGuid of guid: System.Guid   // diagnostic: node identity on row
        | SetFileIndicator of text: string

    type RowMutation =
        | RemoveRow of instId: SiteId
        | CreateRow of instId: SiteId
        | RecreateRow of instId: SiteId
        | PatchRow of instId: SiteId * patches: RowPatch list

    /// Compute the minimal set of DOM mutations needed to transition from oldModel to newModel.
    /// cachedInstIds is the set of instanceIds currently held in the element cache.
    /// Returns removals followed by visible-row operations in preorder display order.
    let planPatchDOM (oldModel: VM) (newModel: VM) (cachedInstIds: Set<SiteId>) : RowMutation list =
        let ownershipClass (model: VM) (entry: SiteEntry) =
            match entry.parentInstanceId with
            | None -> "amb-row-owned"
            | Some parentInstId ->
                match Map.tryFind parentInstId model.siteMap.entries with
                | None -> "amb-row-owned"
                | Some parentEntry ->
                    let childIndex =
                        parentEntry.children
                        |> List.tryFindIndex (fun childInstId -> childInstId = entry.instanceId)
                    match childIndex with
                    | None -> "amb-row-owned"
                    | Some idx ->
                        let parentNode = model.graph.nodes.[parentEntry.nodeId]
                        match parentNode.children |> List.tryItem idx with
                        | Some child when child.ref = Ownership.Ref -> "amb-row-ref"
                        | _ -> "amb-row-owned"

        let newVisible = getVisibleInstanceIds newModel.siteMap
        let newVisibleSet = Set.ofList newVisible

        let removals =
            cachedInstIds
            |> Set.filter (fun id -> not (Set.contains id newVisibleSet))
            |> Set.toList
            |> List.map RemoveRow

        let upserts =
            newVisible |> List.map (fun instId ->
                let entry = newModel.siteMap.entries.[instId]
                if Set.contains instId cachedInstIds then
                    let wasEditing = isEditingEntry oldModel entry
                    let nowEditing = isEditingEntry newModel entry
                    let oldHasChildren =
                        oldModel.graph.nodes
                        |> Map.tryFind entry.nodeId
                        |> Option.map (fun n -> not n.children.IsEmpty)
                        |> Option.defaultValue false
                    let newHasChildren = not (newModel.graph.nodes.[entry.nodeId].children.IsEmpty)
                    if wasEditing <> nowEditing || oldHasChildren <> newHasChildren then
                        RecreateRow instId
                    else
                        let oldEntry = Map.tryFind instId oldModel.siteMap.entries
                        let patches = [
                            let nodeGuid = newModel.graph.nodes.[entry.nodeId].id.Value
                            yield SetNodeGuid nodeGuid
                            let sel = isEntrySelected newModel entry
                            let foc = isEntryFocused newModel entry
                            let isRoot = entry.instanceId = newModel.siteMap.rootId
                            let newNode = newModel.graph.nodes.[entry.nodeId]
                            let oldNode = oldModel.graph.nodes |> Map.tryFind entry.nodeId
                            let newClass =
                                "amb-row"
                                |> CssClass.add (ownershipClass newModel entry)
                                |> addSpecialKindRowClass newNode.kind
                                |> CssClass.addIf isRoot "amb-view-root"
                                |> CssClass.addIf sel "amb-selected"
                                |> CssClass.addIf foc "amb-focused"
                            let oldSel = oldEntry |> Option.map (isEntrySelected oldModel) |> Option.defaultValue false
                            let oldFoc = oldEntry |> Option.map (isEntryFocused oldModel) |> Option.defaultValue false
                            let oldClass =
                                "amb-row"
                                |> CssClass.add (oldEntry |> Option.map (ownershipClass oldModel) |> Option.defaultValue "amb-row-owned")
                                |> (fun s ->
                                    match oldNode with
                                    | Some n -> addSpecialKindRowClass n.kind s
                                    | None -> s)
                                |> CssClass.addIf isRoot "amb-view-root"
                                |> CssClass.addIf oldSel "amb-selected"
                                |> CssClass.addIf oldFoc "amb-focused"
                            if newClass <> oldClass then yield SetClassName newClass
                            let newIndicator = rowFileIndicatorText newModel entry newNode
                            yield SetFileIndicator newIndicator
                            // Sync row text on any graph text change (editing row included — e.g. paste).
                            let newText = newNode.text
                            let oldText = oldNode |> Option.map (fun n -> n.text) |> Option.defaultValue ""
                            if newText <> oldText then yield SetText newText
                            let newClasses = newNode.cssClasses
                            let oldClasses = oldNode |> Option.map (fun n -> n.cssClasses) |> Option.defaultValue CssClass.empty
                            if newClasses <> oldClasses then yield SetTextClasses newClasses
                            if newHasChildren then
                                let oldExpanded = oldEntry |> Option.map (fun e -> e.expanded) |> Option.defaultValue false
                                if entry.expanded <> oldExpanded then
                                    yield SetFoldArrow (if entry.expanded then "\u25BC" else "\u25B6")
                        ]
                        PatchRow (instId, patches)
                else
                    CreateRow instId)

        removals @ upserts

    // -----------------------------------------------------------------------
    // Ownership / occurrence helpers (shared between client and server logic)
    // -----------------------------------------------------------------------

    /// All occurrences (parent, index, child) of the given nodeId in the graph.
    let getAllOccurrences (graph: Graph) (nodeId: NodeId) : (NodeId * int * ChildNode) list =
        graph.nodes
        |> Map.toList
        |> List.collect (fun (parentId, node) ->
            node.children
            |> List.mapi (fun index child ->
                if child.id = nodeId then
                    Some(parentId, index, child)
                else
                    None)
            |> List.choose id)

    /// The unique owner occurrence (parent, index, child) for nodeId, assuming invariants hold.
    let getOwnerOccurrence (graph: Graph) (nodeId: NodeId) : (NodeId * int * ChildNode) =
        getAllOccurrences graph nodeId
        |> List.find (fun (_, _, child) -> child.ref = Ownership.Owner)

    /// True when the unique owner's ancestor chain includes TRASH between the node and ROOT.
    let isOwnerUnderTrash (graph: Graph) (nodeId: NodeId) : bool =
        let ownerParent, _, _ = getOwnerOccurrence graph nodeId

        let rec loop (current: NodeId) =
            if current = graph.root then
                false
            elif current = Graph.trashId then
                true
            else
                graph.ownerParentByChild
                |> Map.tryFind current
                |> Option.map loop
                |> Option.defaultValue false

        loop ownerParent

    /// All occurrences of nodeId that are not within the given SiteNodeRange span.
    let occurrencesOutsideSelection
        (graph: Graph)
        (range: SiteNodeRange)
        (nodeId: NodeId)
        : (NodeId * int * ChildNode) list
        =
        let all = getAllOccurrences graph nodeId
        all
        |> List.filter (fun (parentId, index, _) ->
            if parentId <> range.parent.nodeId then
                true
            else
                index < range.start || index >= range.endd)

