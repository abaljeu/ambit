namespace Gambol.Shared

// ---------------------------------------------------------------------------
// Site-map construction and visible-row walks
// ---------------------------------------------------------------------------

module ViewModelSiteMap =

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
    let reconcileSiteMapFrom
        (graph: Graph)
        (rootNodeId: NodeId)
        (oldMap: SiteMap)
        (startId: SiteId)
        : SiteMap * SiteId =
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

    /// If any frontier entry is expanded, collapse every frontier entry and return
    /// Some siteMap. Otherwise None (caller may navigate).
    let foldExpandedMembers
        (siteMap: SiteMap)
        (frontier: SiteId list)
        : SiteMap option =
        let anyExpanded =
            frontier
            |> List.exists (fun id ->
                match Map.tryFind id siteMap.entries with
                | Some entry -> entry.expanded
                | None -> false)
        if not anyExpanded then None
        else
            frontier
            |> List.fold (fun sm id -> toggleFold id sm) siteMap
            |> Some

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

    /// Adjacent sibling of `me`'s parent when that sibling is already open (or a leaf).
    /// Collapsed siblings with children return None so callers can fall back to move-beside-parent
    /// without auto-unfolding.
    let parentSiblingTarget
        (delta: int)
        (me: SiteEntry)
        (graph: Graph)
        (siteMap: SiteMap)
        (nextSiteId: SiteId)
        (zoomRoot: NodeId)
        : (SiteMap * SiteId * SiteEntry) option =
        let siblingStep =
            if delta = -1 then Some Site.prev
            elif delta = 1 then Some Site.next
            else None

        match siblingStep, me.parentInstanceId with
        | None, _ -> None
        | _, None -> None
        | Some step, Some parentId ->
            match Map.tryFind parentId siteMap.entries with
            | None -> None
            | Some parentEntry ->
                let isMeRoot = me.nodeId = zoomRoot
                let isParentRoot = parentEntry.nodeId = zoomRoot

                if isMeRoot || isParentRoot then
                    None
                else
                    Site.at siteMap (Some parentEntry.instanceId)
                    |> step
                    |> Site.current
                    |> Option.bind (fun siblingId ->
                        match Map.tryFind siblingId siteMap.entries with
                        | None -> None
                        | Some sibling when sibling.expanded ->
                            Some (siteMap, nextSiteId, sibling)
                        | Some sibling ->
                            let hasChildren =
                                Map.tryFind sibling.nodeId graph.nodes
                                |> Option.exists (fun node -> not node.children.IsEmpty)

                            if not hasChildren then
                                Some (siteMap, nextSiteId, sibling)
                            else
                                None)
    let private isFolded (graph: Graph) (entry: SiteEntry) : bool =
        match Map.tryFind entry.nodeId graph.nodes with
        | Some node when not node.children.IsEmpty && not entry.expanded -> true
        | _ -> false

    /// Iterative-deepening unfold over a frontier of instance ids.
    /// If any frontier entry is folded (has graph children, not expanded), expand those
    /// and stop. Otherwise recurse on the site-map children of the frontier.
    /// Does not change selection.
    let iterativeDeepenUnfold
        (graph: Graph)
        (siteMap: SiteMap)
        (startId: SiteId)
        (frontier: SiteId list)
        : SiteMap * SiteId =
        let rec loop sm nextId frontier =
            match frontier with
            | [] -> sm, nextId
            | _ ->
                let folded =
                    frontier
                    |> List.filter (fun id ->
                        match Map.tryFind id sm.entries with
                        | Some entry -> isFolded graph entry
                        | None -> false)
                if not folded.IsEmpty then
                    folded
                    |> List.fold
                        (fun (sm', nid) id -> expandEntry id graph sm' nid)
                        (sm, nextId)
                else
                    let children =
                        frontier
                        |> List.collect (fun id ->
                            match Map.tryFind id sm.entries with
                            | Some e -> e.children
                            | None -> [])
                    loop sm nextId children
        loop siteMap startId frontier

    /// Restore fold state from a saved set of expanded NodeIds.
    /// Walks the siteMap in BFS order, expanding each entry whose nodeId is in
    /// expandedNodeIds.  Parent-before-child ordering ensures that children only
    /// become visible after their parent is expanded.
    /// Returns the updated SiteMap and next available instanceId.
    let applyFoldSession
        (expandedNodeIds: Set<NodeId>)
        (graph: Graph)
        (siteMap: SiteMap)
        (startId: SiteId)
        : SiteMap * SiteId =
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

    let private visiblePreorder (siteMap: SiteMap) (instId: SiteId) : SiteEntry list =
        let entries = siteMap.entries

        let rec gather (instId: SiteId) : SiteEntry list =
            match Map.tryFind instId entries with
            | None -> []
            | Some entry ->
                entry ::
                    (if entry.expanded then entry.children |> List.collect gather
                     else [])

        gather instId

    /// Preorder walk of visible entries, returning NodeIds in display order (excluding root).
    /// Respects fold state: unexpanded entries do not contribute their children.
    let getVisibleRowIds (siteMap: SiteMap) : NodeId list =
        match Map.tryFind siteMap.rootId siteMap.entries with
        | None -> []
        | Some root ->
            root.children
            |> List.collect (visiblePreorder siteMap)
            |> List.map (fun entry -> entry.nodeId)

    /// Preorder walk of visible entries, returning instanceIds in display order (excluding root).
    /// Mirrors getVisibleRowIds but keyed by instanceId. Use this for instance-aware navigation
    /// so that duplicate NodeIds are treated as distinct positions.
    let getVisibleRowInstanceIds (siteMap: SiteMap) : SiteId list =
        match Map.tryFind siteMap.rootId siteMap.entries with
        | None -> []
        | Some root ->
            root.children
            |> List.collect (visiblePreorder siteMap)
            |> List.map (fun entry -> entry.instanceId)

    /// Preorder walk of visible entries, returning instanceIds in display order (including root).
    /// Mirrors getVisibleRowIds but keyed by instanceId for DOM-cache lookups.
    let getVisibleInstanceIds (siteMap: SiteMap) : SiteId list =
        match Map.tryFind siteMap.rootId siteMap.entries with
        | None -> []
        | Some root ->
            visiblePreorder siteMap root.instanceId
            |> List.map (fun entry -> entry.instanceId)
