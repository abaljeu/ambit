namespace Gambol.Shared

// ---------------------------------------------------------------------------
// Pure view-model helpers (no DOM / Fable interop)
// ---------------------------------------------------------------------------

module ViewModel =

    /// Create a placeholder SiteNode for an empty/unloaded graph.
    let emptySiteRoot : SiteNode =
        { instanceId = 0
          nodeId = NodeId(System.Guid.Empty)
          expanded = true
          children = [] }

    /// Reconcile the site tree after a graph change.  Preserves existing
    /// instanceIds and fold states for nodes that still exist; assigns new IDs
    /// (starting from nextId) for newly-added nodes.  New nodes default to
    /// collapsed.  The root is always expanded.
    let rebuildSiteTree (graph: Graph) (oldRoot: SiteNode) (nextId: int) : SiteNode * int =
        // Build lookup: NodeId -> old SiteNode (Phase 1 = 1:1)
        let oldMap =
            let rec collect (node: SiteNode) (acc: Map<NodeId, SiteNode>) =
                let acc' = Map.add node.nodeId node acc
                node.children |> List.fold (fun a child -> collect child a) acc'
            collect oldRoot Map.empty
        let mutable counter = nextId
        let freshId () =
            let id = counter
            counter <- counter + 1
            id
        let rec build (nodeId: NodeId) (isRoot: bool) : SiteNode =
            let node = graph.nodes.[nodeId]
            let (instId, expanded) =
                match Map.tryFind nodeId oldMap with
                | Some old -> (old.instanceId, old.expanded)
                | None     -> (freshId (), false)
            { instanceId = instId
              nodeId = nodeId
              expanded = if isRoot then true else expanded
              children = node.children |> List.map (fun cid -> build cid false) }
        let root = build graph.root true
        root, counter

    /// Build a full SiteNode tree from a graph.  All nodes start collapsed
    /// except the root (which is always expanded).  Returns the root SiteNode
    /// and the next available instanceId.
    let buildSiteTree (graph: Graph) : SiteNode * int =
        rebuildSiteTree graph emptySiteRoot 0

    /// Toggle the expanded flag of the SiteNode with the given instanceId.
    let toggleFold (instanceId: int) (siteRoot: SiteNode) : SiteNode =
        let rec toggle (node: SiteNode) : SiteNode =
            if node.instanceId = instanceId then
                { node with expanded = not node.expanded }
            else
                { node with children = node.children |> List.map toggle }
        toggle siteRoot

    /// Ensure the first SiteNode matching the given NodeId is expanded.
    let expandNodeId (nodeId: NodeId) (siteRoot: SiteNode) : SiteNode =
        let rec go (node: SiteNode) : SiteNode =
            if node.nodeId = nodeId then
                { node with expanded = true }
            else
                { node with children = node.children |> List.map go }
        go siteRoot

    /// Find the first SiteNode (preorder) whose nodeId matches the given NodeId.
    /// Returns None if not found (e.g. the node is not visible in the current site tree).
    let findSiteNodeByNodeId (nodeId: NodeId) (siteRoot: SiteNode) : SiteNode option =
        let rec find (node: SiteNode) : SiteNode option =
            if node.nodeId = nodeId then Some node
            else node.children |> List.tryPick find
        find siteRoot

    /// Build a single-node Selection for the given nodeId, using the graph to locate its parent
    /// and the site tree to obtain the parent SiteNode.
    /// Returns None if the node has no parent (i.e. it is the root) or if the parent is not in the site tree.
    let singleSelection (graph: Graph) (siteRoot: SiteNode) (nodeId: NodeId) : Selection option =
        Graph.tryFindParentAndIndex nodeId graph
        |> Option.bind (fun (parentId, index) ->
            findSiteNodeByNodeId parentId siteRoot
            |> Option.map (fun parentSiteNode ->
                { range = { parent = parentSiteNode; start = index; endd = index + 1 }; focus = index }))

    /// Extract the first (start) selected NodeId from a Selection.
    let firstSelectedNodeId (graph: Graph) (sel: Selection) : NodeId =
        graph.nodes.[sel.range.parent.nodeId].children.[sel.range.start]

    /// Extract the focused NodeId from a Selection (the active end, used for editing and Arrow movement).
    let focusedNodeId (graph: Graph) (sel: Selection) : NodeId =
        graph.nodes.[sel.range.parent.nodeId].children.[sel.focus]

    /// Flatten site tree into visible row order (preorder, excluding root).
    /// Respects fold state: only recurses into expanded nodes.
    let getVisibleRowIds (siteRoot: SiteNode) : NodeId list =
        let rec gather (siteNode: SiteNode) : NodeId list =
            siteNode.nodeId ::
                (if siteNode.expanded then
                     siteNode.children |> List.collect gather
                 else [])
        siteRoot.children |> List.collect gather

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
            match singleSelection model.graph model.siteRoot focusId with
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
            let rows = getVisibleRowIds model.siteRoot
            match rows |> List.tryFindIndex ((=) anchorId) with
            | None -> model
            | Some currentIndex ->
                let nextIndex = currentIndex + delta
                if nextIndex < 0 || nextIndex >= rows.Length then
                    model
                else
                    let nextId = rows[nextIndex]
                    match singleSelection model.graph model.siteRoot nextId with
                    | None -> model
                    | Some newSel -> { model with selectedNodes = Some newSel; mode = Selecting }

    /// Pure portion of MoveSelectionUp: handles the non-editing cases.
    /// When focus is not at the range start, moves focus to start (keep range).
    /// Otherwise, moves the whole selection up by one visible row.
    let applyMoveSelectionUp (model: Model) : Model =
        match model.selectedNodes with
        | Some sel when sel.focus > sel.range.start ->
            { model with selectedNodes = Some { sel with focus = sel.range.start } }
        | _ ->
            moveSelectionBy -1 model

    /// Pure portion of MoveSelectionDown: handles the non-editing cases.
    /// When focus is not at the range end, moves focus to end (keep range).
    /// Otherwise, moves the whole selection down by one visible row.
    let applyMoveSelectionDown (model: Model) : Model =
        match model.selectedNodes with
        | Some sel when sel.focus < sel.range.endd - 1 ->
            { model with selectedNodes = Some { sel with focus = sel.range.endd - 1 } }
        | _ ->
            moveSelectionBy 1 model

    // ---------------------------------------------------------------------------
    // SiteMap — flat O(log S) render-map (replaces SiteNode tree in 3c-ii)
    // ---------------------------------------------------------------------------

    module SiteMapOps =

        /// Build a full SiteMap from a graph. All nodes start collapsed except the root.
        /// Tracks ancestors per path to detect cycles; a back-edge produces an entry with
        /// children = [] (the node appears but its subtree is not re-entered on that path).
        /// Returns the SiteMap and the next available instanceId.
        let buildSiteMap (graph: Graph) (startId: int) : SiteMap * int =
            let mutable counter = startId
            let mutable acc = Map.empty<int, SiteEntry>
            let freshId () =
                let id = counter
                counter <- counter + 1
                id
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
            { rootId = rootInstId; entries = acc }, counter

        /// Reconcile a SiteMap after a graph change. Recovers instanceIds and fold states
        /// by matching children positionally (position i in old parent's children list).
        /// A position match requires the nodeId to agree; mismatches get a fresh instanceId.
        /// New nodes default to collapsed. Per-path ancestor guard detects cycles.
        /// Returns the updated SiteMap and next available instanceId.
        let reconcileSiteMap (graph: Graph) (oldMap: SiteMap) (startId: int) : SiteMap * int =
            let mutable counter = startId
            let mutable acc = Map.empty<int, SiteEntry>
            let freshId () =
                let id = counter
                counter <- counter + 1
                id
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
                        let oldChildren =
                            match oldEntryOpt with
                            | Some old -> old.children
                            | None -> []
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
            { rootId = rootInstId; entries = acc }, counter

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
                    let mutable counter = startId
                    let mutable acc = siteMap.entries
                    let freshId () =
                        let id = counter
                        counter <- counter + 1
                        id
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
                    { siteMap with entries = acc }, counter

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
