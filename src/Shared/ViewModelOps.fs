namespace Gambol.Shared

// ---------------------------------------------------------------------------
// Pure view-model helpers (no DOM / Fable interop)
// ---------------------------------------------------------------------------

module ViewModel =

    let emptySiteMap : SiteMap =
        let rootEntry = { instanceId = 0; nodeId = NodeId(System.Guid.Empty)
                          parentInstanceId = None; expanded = true; childrenStale = false; children = [] }
        { rootId = 0; entries = Map.ofList [0, rootEntry] }

    // Returns (freshId generator, counter getter) for sequencing integer IDs.
    let private makeCounter (start: int) =
        let mutable n = start
        (fun () -> let id = n in n <- n + 1; id), (fun () -> n)

    /// Build a SiteMap rooted at rootNodeId. The root is expanded; its immediate children are
    /// collapsed with children = [] and childrenStale = true, populated on demand via expandEntry.
    /// Cycle termination is implicit: new entries start collapsed with no children, so expanding
    /// an ancestor reachable via a back-edge produces a collapsed leaf that stops the recursion.
    /// Returns the SiteMap and the next available instanceId.
    let buildSiteMapFrom (graph: Graph) (rootNodeId: NodeId) (startId: int) : SiteMap * int =
        let freshId, endCount = makeCounter startId
        let mutable acc = Map.empty<int, SiteEntry>
        let rootInstId = freshId ()
        let rootNode = graph.nodes.[rootNodeId]
        let childInstIds =
            rootNode.children |> List.map (fun cid ->
                let childId = freshId ()
                acc <- Map.add childId { instanceId = childId; nodeId = cid
                                         parentInstanceId = Some rootInstId
                                         expanded = false; childrenStale = true; children = [] } acc
                childId)
        acc <- Map.add rootInstId { instanceId = rootInstId; nodeId = rootNodeId
                                    parentInstanceId = None
                                    expanded = true; childrenStale = false; children = childInstIds } acc
        { rootId = rootInstId; entries = acc }, endCount ()

    /// Build a SiteMap from the graph root. See buildSiteMapFrom for details.
    let buildSiteMap (graph: Graph) (startId: int) : SiteMap * int =
        buildSiteMapFrom graph graph.root startId

    /// Reconcile a SiteMap rooted at rootNodeId after a graph change. Walks only expanded entries,
    /// syncing their children lists from the graph. Collapsed children of expanded entries are
    /// reused by position (nodeId must match) with childrenStale = true and children = []; they
    /// are not recursed into. Orphaned entries from removed or now-unexpanded paths are dropped.
    /// Returns the updated SiteMap and next available instanceId.
    let reconcileSiteMapFrom (graph: Graph) (rootNodeId: NodeId) (oldMap: SiteMap) (startId: int) : SiteMap * int =
        let freshId, endCount = makeCounter startId
        let mutable acc = Map.empty<int, SiteEntry>
        let rec walk (nodeId: NodeId) (parentInstId: int option) (isRoot: bool) (oldInstIdOpt: int option) : int =
            let oldEntryOpt =
                oldInstIdOpt
                |> Option.bind (fun id -> Map.tryFind id oldMap.entries)
                |> Option.bind (fun e -> if e.nodeId = nodeId then Some e else None)
            let instId, expanded =
                match oldEntryOpt with
                | Some old -> old.instanceId, old.expanded
                | None -> freshId (), false
            let childInstIds =
                if isRoot || expanded then
                    let node = graph.nodes.[nodeId]
                    let oldChildren = oldEntryOpt |> Option.map (fun o -> o.children) |> Option.defaultValue []
                    let usedIds = ref Set.empty<int>
                    node.children |> List.mapi (fun i cid ->
                        let oldChildOpt =
                            let positional =
                                List.tryItem i oldChildren
                                |> Option.bind (fun oid -> Map.tryFind oid oldMap.entries)
                                |> Option.bind (fun e -> if e.nodeId = cid then Some e else None)
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
                                        if e.nodeId = cid && not (Set.contains e.instanceId usedIds.Value)
                                        then usedIds.Value <- Set.add e.instanceId usedIds.Value; Some e
                                        else None))
                        match oldChildOpt with
                        | Some old when old.expanded -> walk cid (Some instId) false (Some old.instanceId)
                        | Some old ->
                            acc <- Map.add old.instanceId { old with childrenStale = true; children = [] } acc
                            old.instanceId
                        | None ->
                            let newId = freshId ()
                            acc <- Map.add newId { instanceId = newId; nodeId = cid
                                                   parentInstanceId = Some instId
                                                   expanded = false; childrenStale = true; children = [] } acc
                            newId)
                else []
            let entry =
                { instanceId = instId; nodeId = nodeId; parentInstanceId = parentInstId
                  expanded = if isRoot then true else expanded
                  childrenStale = false; children = childInstIds }
            acc <- Map.add instId entry acc
            instId
        let rootInstId = walk rootNodeId None true (Some oldMap.rootId)
        { rootId = rootInstId; entries = acc }, endCount ()

    /// Reconcile a SiteMap from the graph root after a graph change. See reconcileSiteMapFrom for details.
    let reconcileSiteMap (graph: Graph) (oldMap: SiteMap) (startId: int) : SiteMap * int =
        reconcileSiteMapFrom graph graph.root oldMap startId

    /// Collapse the entry with the given instanceId, marking its children as stale. O(log S).
    /// The children list is preserved so that a subsequent expandEntry can reuse instanceIds
    /// positionally (restoring nested fold state) when no structural op has intervened.
    /// For expanding, use expandEntry which re-syncs children from the graph.
    let toggleFold (instanceId: int) (siteMap: SiteMap) : SiteMap =
        match Map.tryFind instanceId siteMap.entries with
        | None -> siteMap
        | Some entry ->
            { siteMap with entries = Map.add instanceId { entry with expanded = false; childrenStale = true } siteMap.entries }

    /// Expand a collapsed entry, inserting or re-syncing immediate child SiteEntries from the graph.
    /// Children are matched positionally by nodeId to preserve existing instanceIds and fold state
    /// (useful when re-expanding after a simple collapse with no intervening structural op).
    /// New children start collapsed with childrenStale = true and children = [].
    /// Returns the updated SiteMap and next available instanceId.
    let expandEntry (instanceId: int) (graph: Graph) (siteMap: SiteMap) (startId: int) : SiteMap * int =
        match Map.tryFind instanceId siteMap.entries with
        | None -> siteMap, startId
        | Some entry ->
            if entry.expanded then siteMap, startId
            else
                let freshId, endCount = makeCounter startId
                let mutable acc = siteMap.entries
                let node = graph.nodes.[entry.nodeId]
                let childInstIds =
                    node.children |> List.mapi (fun i cid ->
                        // Reuse existing child entry at this position if nodeId matches
                        match List.tryItem i entry.children |> Option.bind (fun oid -> Map.tryFind oid acc) with
                        | Some existing when existing.nodeId = cid -> existing.instanceId
                        | _ ->
                            let newId = freshId ()
                            acc <- Map.add newId { instanceId = newId; nodeId = cid
                                                   parentInstanceId = Some instanceId
                                                   expanded = false; childrenStale = true; children = [] } acc
                            newId)
                let updated = { entry with expanded = true; childrenStale = false; children = childInstIds }
                acc <- Map.add instanceId updated acc
                { siteMap with entries = acc }, endCount ()

    /// Restore fold state from a saved set of expanded NodeIds.
    /// Walks the siteMap in BFS order, expanding each entry whose nodeId is in
    /// expandedNodeIds.  Parent-before-child ordering ensures that children only
    /// become visible after their parent is expanded.
    /// Returns the updated SiteMap and next available instanceId.
    let applyFoldSession (expandedNodeIds: Set<NodeId>) (graph: Graph) (siteMap: SiteMap) (startId: int) : SiteMap * int =
        if Set.isEmpty expandedNodeIds then siteMap, startId
        else
            let mutable sm = siteMap
            let mutable nextId = startId
            let queue = System.Collections.Generic.Queue<int>()
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
    let buildOccurrenceIndex (siteMap: SiteMap) : Map<NodeId, int list> =
        siteMap.entries
        |> Map.fold (fun acc _ entry ->
            let existing = acc |> Map.tryFind entry.nodeId |> Option.defaultValue []
            Map.add entry.nodeId (entry.instanceId :: existing) acc)
            Map.empty

    /// Preorder walk of visible entries, returning NodeIds in display order (excluding root).
    /// Respects fold state: unexpanded entries do not contribute their children.
    let getVisibleRowIds (siteMap: SiteMap) : NodeId list =
        let entries = siteMap.entries
        let rec gather (instId: int) : NodeId list =
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
    let getVisibleRowInstanceIds (siteMap: SiteMap) : int list =
        let entries = siteMap.entries
        let rec gather (instId: int) : int list =
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
    let getVisibleInstanceIds (siteMap: SiteMap) : int list =
        let entries = siteMap.entries
        let rec gather (instId: int) : int list =
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
    let singleSelectionForInstance (siteMap: SiteMap) (instanceId: int) : Selection option =
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

    /// Extract the first (start) selected NodeId from a Selection.
    let firstSelectedNodeId (graph: Graph) (sel: Selection) : NodeId =
        graph.nodes.[sel.range.parent.nodeId].children.[sel.range.start]

    /// Extract the focused NodeId from a Selection (the active end, used for editing and Arrow movement).
    let focusedNodeId (graph: Graph) (sel: Selection) : NodeId =
        graph.nodes.[sel.range.parent.nodeId].children.[sel.focus]

    /// Extract the focused instanceId from a Selection. Since SiteEntry.children is an instanceId list,
    /// this gives the exact view-line of the focused node rather than its graph identity.
    let focusedInstanceId (sel: Selection) : int =
        sel.range.parent.children.[sel.focus]

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
            let instId = focusedInstanceId sel
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
            let instId = focusedInstanceId sel
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
                    | Some newSel -> { model with selectedNodes = Some newSel; mode = Selecting }

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
            | CssClassPrompt (ret, _) -> ret
            | m -> m
        match effectiveMode, model.selectedNodes with
        | Editing _, None    -> entry.parentInstanceId = None
        | Editing _, Some sel -> isInstanceFocused sel model.siteMap entry
        | _ -> false

// ---------------------------------------------------------------------------
// DOM mutation plan (pure — no Browser interop)
// ---------------------------------------------------------------------------

    type RowPatch =
        | SetClassName of newClass: string
        | SetText of newText: string
        | SetTextClasses of classes: CssClasses
        | SetFoldArrow of arrow: string   // "▼" or "▶" (has children); "●" (no children, no behavior)
        | SetNodeGuid of guid: System.Guid   // diagnostic: node identity on row

    type RowMutation =
        | RemoveRow of instId: int
        | CreateRow of instId: int
        | RecreateRow of instId: int
        | PatchRow of instId: int * patches: RowPatch list

    /// Compute the minimal set of DOM mutations needed to transition from oldModel to newModel.
    /// cachedInstIds is the set of instanceIds currently held in the element cache.
    /// Returns removals followed by visible-row operations in preorder display order.
    let planPatchDOM (oldModel: VM) (newModel: VM) (cachedInstIds: Set<int>) : RowMutation list =
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
                            let newClass =
                                "amb-row"
                                |> CssClass.addIf isRoot "amb-view-root"
                                |> CssClass.addIf sel "amb-selected"
                                |> CssClass.addIf foc "amb-focused"
                            let oldSel = oldEntry |> Option.map (isEntrySelected oldModel) |> Option.defaultValue false
                            let oldFoc = oldEntry |> Option.map (isEntryFocused oldModel) |> Option.defaultValue false
                            let oldClass =
                                "amb-row"
                                |> CssClass.addIf isRoot "amb-view-root"
                                |> CssClass.addIf oldSel "amb-selected"
                                |> CssClass.addIf oldFoc "amb-focused"
                            if newClass <> oldClass then yield SetClassName newClass
                            let newNode = newModel.graph.nodes.[entry.nodeId]
                            let oldNode = oldModel.graph.nodes |> Map.tryFind entry.nodeId
                            if not nowEditing then
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

