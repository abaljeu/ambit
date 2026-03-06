namespace Gambol.Shared

// ---------------------------------------------------------------------------
// Pure view-model helpers (no DOM / Fable interop)
// ---------------------------------------------------------------------------

module ViewModel =

    let emptySiteMap : SiteMap =
        let rootEntry = { instanceId = 0; nodeId = NodeId(System.Guid.Empty)
                          parentInstanceId = None; expanded = true; children = [] }
        { rootId = 0; entries = Map.ofList [0, rootEntry] }

    // Returns (freshId generator, counter getter) for sequencing integer IDs.
    let private makeCounter (start: int) =
        let mutable n = start
        (fun () -> let id = n in n <- n + 1; id), (fun () -> n)

    /// Build a full SiteMap from a graph. All nodes start collapsed except the root.
    /// Tracks ancestors per path to detect cycles; a back-edge produces an entry with
    /// children = [] (the node appears but its subtree is not re-entered on that path).
    /// Returns the SiteMap and the next available instanceId.
    let buildSiteMap (graph: Graph) (startId: int) : SiteMap * int =
        let freshId, endCount = makeCounter startId
        let mutable acc = Map.empty<int, SiteEntry>
        let rec walk (nodeId: NodeId) (parentInstId: int option) (isRoot: bool) (ancestors: Set<NodeId>) : int =
            let isCycle = ancestors.Contains nodeId
            let instId = freshId ()
            let childInstIds =
                if isCycle then []
                else
                    let node = graph.nodes.[nodeId]
                    let ancestors' = ancestors.Add nodeId
                    node.children |> List.map (fun cid -> walk cid (Some instId) false ancestors')
            let entry : SiteEntry =
                { instanceId = instId
                  nodeId = nodeId
                  parentInstanceId = parentInstId
                  expanded = isRoot && not isCycle
                  children = childInstIds }
            acc <- Map.add instId entry acc
            instId
        let rootInstId = walk graph.root None true Set.empty
        { rootId = rootInstId; entries = acc }, endCount ()

    /// Reconcile a SiteMap after a graph change. Recovers instanceIds and fold states
    /// by matching children positionally (position i in old parent's children list).
    /// A position match requires the nodeId to agree; mismatches get a fresh instanceId.
    /// New nodes default to collapsed. Per-path ancestor guard detects cycles.
    /// Returns the updated SiteMap and next available instanceId.
    let reconcileSiteMap (graph: Graph) (oldMap: SiteMap) (startId: int) : SiteMap * int =
        let freshId, endCount = makeCounter startId
        let mutable acc = Map.empty<int, SiteEntry>
        let rec walk (nodeId: NodeId) (parentInstId: int option) (isRoot: bool) (ancestors: Set<NodeId>) (oldInstIdOpt: int option) : int =
            let isCycle = ancestors.Contains nodeId
            // Recover old entry only if it exists and its nodeId matches
            let oldEntryOpt =
                oldInstIdOpt
                |> Option.bind (fun id -> Map.tryFind id oldMap.entries)
                |> Option.bind (fun e -> if e.nodeId = nodeId then Some e else None)
            let (instId, expanded) =
                match oldEntryOpt with
                | Some old -> (old.instanceId, old.expanded)
                | None -> (freshId (), false)
            let childInstIds =
                if isCycle then []
                else
                    let node = graph.nodes.[nodeId]
                    let ancestors' = ancestors.Add nodeId
                    let oldChildren = oldEntryOpt |> Option.map (fun o -> o.children) |> Option.defaultValue []
                    node.children |> List.mapi (fun i cid ->
                        // Position-based match: old child at position i must have matching nodeId
                        let oldChildInstIdOpt =
                            List.tryItem i oldChildren
                            |> Option.bind (fun oid ->
                                Map.tryFind oid oldMap.entries
                                |> Option.bind (fun e -> if e.nodeId = cid then Some oid else None))
                        walk cid (Some instId) false ancestors' oldChildInstIdOpt)
            let entry : SiteEntry =
                { instanceId = instId
                  nodeId = nodeId
                  parentInstanceId = parentInstId
                  expanded = if isRoot then true else expanded
                  children = childInstIds }
            acc <- Map.add instId entry acc
            instId
        let rootInstId = walk graph.root None true Set.empty (Some oldMap.rootId)
        { rootId = rootInstId; entries = acc }, endCount ()

    /// Toggle the expanded flag of the entry with the given instanceId. O(log S).
    let toggleFold (instanceId: int) (siteMap: SiteMap) : SiteMap =
        match Map.tryFind instanceId siteMap.entries with
        | None -> siteMap
        | Some entry ->
            { siteMap with entries = Map.add instanceId { entry with expanded = not entry.expanded } siteMap.entries }

    /// Expand a collapsed entry, inserting any missing immediate child SiteEntries.
    /// Does not recurse into grandchildren — children of the new entries start collapsed
    /// with children = [] and are populated on their own expansion.
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
                            let childEntry : SiteEntry =
                                { instanceId = newId
                                  nodeId = cid
                                  parentInstanceId = Some instanceId
                                  expanded = false
                                  children = [] }
                            acc <- Map.add newId childEntry acc
                            newId)
                let updated = { entry with expanded = true; children = childInstIds }
                acc <- Map.add instanceId updated acc
                { siteMap with entries = acc }, endCount ()

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
        | Some root -> root.children |> List.collect gather

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

    /// Extract the first (start) selected NodeId from a Selection.
    let firstSelectedNodeId (graph: Graph) (sel: Selection) : NodeId =
        graph.nodes.[sel.range.parent.nodeId].children.[sel.range.start]

    /// Extract the focused NodeId from a Selection (the active end, used for editing and Arrow movement).
    let focusedNodeId (graph: Graph) (sel: Selection) : NodeId =
        graph.nodes.[sel.range.parent.nodeId].children.[sel.focus]

    /// Shift-Arrow: move the focused end of the range by delta (-1 = up, +1 = down).
    /// For a single-node selection, always extends. For multi-node, the focused end moves.
    /// Focus follows the moved end. No-op if the move would exceed parent bounds.
    let shiftArrow (delta: int) (model: Model) : Model =
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
    let collapseToFocus (model: Model) : Model =
        match model.selectedNodes with
        | None -> model
        | Some sel ->
            let focusId = focusedNodeId model.graph sel
            match singleSelection model.graph model.siteMap focusId with
            | None -> model
            | Some newSel -> { model with selectedNodes = Some newSel }

    /// Move current selection by delta (-1 for up, +1 for down) in visible row order.
    /// Collapses any multi-node selection to the focus node, then moves from there.
    /// The resulting selection is always a single-node Selection.
    let moveSelectionBy (delta: int) (model: Model) : Model =
        match model.selectedNodes with
        | None -> model
        | Some sel ->
            let anchorId = focusedNodeId model.graph sel
            let rows = getVisibleRowIds model.siteMap
            match rows |> List.tryFindIndex ((=) anchorId) with
            | None -> model
            | Some currentIndex ->
                let nextIndex = currentIndex + delta
                if nextIndex < 0 || nextIndex >= rows.Length then
                    model
                else
                    let nextId = rows[nextIndex]
                    match singleSelection model.graph model.siteMap nextId with
                    | None -> model
                    | Some newSel -> { model with selectedNodes = Some newSel; mode = Selecting }

    let private applyMoveSelection (delta: int) (model: Model) : Model =
        match model.selectedNodes with
        | Some sel ->
            let focusEnd = if delta < 0 then sel.range.start else sel.range.endd - 1
            if sel.focus <> focusEnd then { model with selectedNodes = Some { sel with focus = focusEnd } }
            else moveSelectionBy delta model
        | None -> model

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

    let isNodeDirectlySelected (model: Model) (nodeId: NodeId) : bool =
        match model.selectedNodes with
        | None -> false
        | Some sel ->
            let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
            parentNode.children
            |> List.indexed
            |> List.exists (fun (i, id) -> id = nodeId && i >= sel.range.start && i < sel.range.endd)

    let isNodeFocused (model: Model) (nodeId: NodeId) : bool =
        match model.selectedNodes with
        | None -> false
        | Some sel -> focusedNodeId model.graph sel = nodeId

    let private ancestorMatch (model: Model) (entry: SiteEntry) (pred: NodeId -> bool) : bool =
        let rec go parentInstId =
            match parentInstId with
            | None -> false
            | Some pid ->
                match Map.tryFind pid model.siteMap.entries with
                | None -> false
                | Some pe -> pred pe.nodeId || go pe.parentInstanceId
        pred entry.nodeId || go entry.parentInstanceId

    let isEntrySelected (model: Model) (entry: SiteEntry) =
        ancestorMatch model entry (isNodeDirectlySelected model)

    let isEntryFocused (model: Model) (entry: SiteEntry) =
        ancestorMatch model entry (isNodeFocused model)

    let isEditingEntry (model: Model) (entry: SiteEntry) : bool =
        match model.mode with
        | Editing _ -> isNodeFocused model entry.nodeId
        | Selecting -> false

// ---------------------------------------------------------------------------
// DOM mutation plan (pure — no Browser interop)
// ---------------------------------------------------------------------------

    type RowPatch =
        | SetClassName of newClass: string
        | SetText of newText: string
        | SetFoldArrow of arrow: string   // "▼" or "▶"

    type RowMutation =
        | RemoveRow of instId: int
        | CreateRow of instId: int
        | RecreateRow of instId: int
        | PatchRow of instId: int * patches: RowPatch list

    /// Compute the minimal set of DOM mutations needed to transition from oldModel to newModel.
    /// cachedInstIds is the set of instanceIds currently held in the element cache.
    /// Returns removals followed by visible-row operations in preorder display order.
    let planPatchDOM (oldModel: Model) (newModel: Model) (cachedInstIds: Set<int>) : RowMutation list =
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
                        Map.tryFind instId oldModel.siteMap.entries
                        |> Option.map (fun e -> not e.children.IsEmpty)
                        |> Option.defaultValue (not entry.children.IsEmpty)
                    let newHasChildren = not entry.children.IsEmpty
                    if wasEditing <> nowEditing || oldHasChildren <> newHasChildren then
                        RecreateRow instId
                    else
                        let oldEntry = Map.tryFind instId oldModel.siteMap.entries
                        let patches = [
                            let sel = isEntrySelected newModel entry
                            let foc = isEntryFocused newModel entry
                            let newClass = "row" + (if sel then " selected" else "") + (if foc then " focused" else "")
                            let oldSel = oldEntry |> Option.map (isEntrySelected oldModel) |> Option.defaultValue false
                            let oldFoc = oldEntry |> Option.map (isEntryFocused oldModel) |> Option.defaultValue false
                            let oldClass = "row" + (if oldSel then " selected" else "") + (if oldFoc then " focused" else "")
                            if newClass <> oldClass then yield SetClassName newClass
                            if not nowEditing then
                                let newText = newModel.graph.nodes.[entry.nodeId].text
                                let oldText =
                                    oldModel.graph.nodes
                                    |> Map.tryFind entry.nodeId
                                    |> Option.map (fun n -> n.text)
                                    |> Option.defaultValue ""
                                if newText <> oldText then yield SetText newText
                            if newHasChildren then
                                let oldExpanded = oldEntry |> Option.map (fun e -> e.expanded) |> Option.defaultValue false
                                if entry.expanded <> oldExpanded then
                                    yield SetFoldArrow (if entry.expanded then "\u25BC" else "\u25B6")
                        ]
                        PatchRow (instId, patches)
                else
                    CreateRow instId)

        removals @ upserts

