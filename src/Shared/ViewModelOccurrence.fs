namespace Gambol.Shared

// ---------------------------------------------------------------------------
// Ownership / occurrence helpers
// ---------------------------------------------------------------------------

module ViewModelOccurrence =

    open ViewModelSiteMap

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
        |> List.find (fun (parentId, _, child) ->
            Node.childOwnership graph parentId child = Ownership.Owner)

    /// Reframe zoom at the owner parent of nodeId. None at graph root or without owner parent.
    let tryReframeZoomAtOwnerParent (graph: Graph) (nodeId: NodeId) (nextSiteId: SiteId)
        : (NodeId * SiteMap * SiteId * Selection option) option =
        if nodeId = graph.root then None
        else
            match Map.tryFind nodeId graph.ownerParentByChild with
            | None -> None
            | Some ownerParentId ->
                let _, index, _ = getOwnerOccurrence graph nodeId
                let siteMap, nextId = buildSiteMapFrom graph ownerParentId nextSiteId
                Some (ownerParentId, siteMap, nextId, childSelectionAt siteMap ownerParentId index)

    /// Prefer owner occurrence; else any occurrence. None at root or with no parent edge.
    let tryFocusNodeOccurrence (graph: Graph) (nodeId: NodeId) (nextSiteId: SiteId)
        : (NodeId * SiteMap * SiteId * Selection option) option =
        match tryReframeZoomAtOwnerParent graph nodeId nextSiteId with
        | Some _ as found -> found
        | None when nodeId = graph.root -> None
        | None ->
            match getAllOccurrences graph nodeId |> List.tryHead with
            | None -> None
            | Some (parentId, index, _) ->
                let siteMap, nextId = buildSiteMapFrom graph parentId nextSiteId
                Some (parentId, siteMap, nextId, childSelectionAt siteMap parentId index)

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

    /// Push ingress when zoom root changes; otherwise leave the stack unchanged.
    /// If newZoomRoot already appears as a parent on the path (e.g. zooming into an
    /// ancestor via a Ref back-edge), collapse to that prefix so zoom-out stays acyclic.
    let pushZoomIngress
        (oldZoomRoot: NodeId)
        (newZoomRoot: NodeId)
        (ingress: NodeId * int)
        (stack: (NodeId * int) list)
        : (NodeId * int) list =
        if oldZoomRoot = newZoomRoot then stack
        else
            match List.tryFindIndex (fun (pid, _) -> pid = newZoomRoot) stack with
            | Some i -> List.skip (i + 1) stack
            | None -> ingress :: stack

    /// Owner-parent ingress for nodeId, or [] when there is no owner parent.
    let ownerIngress (graph: Graph) (nodeId: NodeId) : (NodeId * int) list =
        match Map.tryFind nodeId graph.ownerParentByChild with
        | None -> []
        | Some ownerParentId ->
            let _, index, _ = getOwnerOccurrence graph nodeId
            [ ownerParentId, index ]

    /// Full owner-chain ingress from nodeId up to (but not including) graph root.
    /// Head is the immediate owner parent — same shape as a zoom-in stack.
    let ownerPathIngress (graph: Graph) (nodeId: NodeId) : (NodeId * int) list =
        let rec loop (current: NodeId) (visited: Set<NodeId>) =
            if current = graph.root || Set.contains current visited then
                []
            else
                match Map.tryFind current graph.ownerParentByChild with
                | None -> []
                | Some parentId ->
                    let _, index, _ = getOwnerOccurrence graph current
                    (parentId, index) :: loop parentId (Set.add current visited)

        loop nodeId Set.empty

    /// Zoom to a parent occurrence of nodeId and select it. Unchanged when no occurrence.
    let focusNode (nodeId: NodeId) (model: VM) : VM =
        match tryFocusNodeOccurrence model.graph nodeId model.nextSiteId with
        | None -> model
        | Some (zoomRoot, siteMap, nextId, sel) ->
            { model with
                zoomRoot = zoomRoot
                zoomIngress = ownerPathIngress model.graph zoomRoot
                siteMap = siteMap
                nextSiteId = nextId
                selectedNodes = sel
                mode = Selecting }

    /// Site-map parent (nodeId, childIndex) of an occurrence of childNodeId, if any.
    let trySiteMapParentOccurrence
        (siteMap: SiteMap)
        (childNodeId: NodeId)
        : (NodeId * int) option =
        siteMap.entries
        |> Map.toList
        |> List.tryPick (fun (_, e) ->
            if e.nodeId <> childNodeId then None
            else
                match e.parentInstanceId with
                | None -> None
                | Some pid ->
                    Map.tryFind pid siteMap.entries
                    |> Option.bind (fun parent ->
                        List.tryFindIndex ((=) e.instanceId) parent.children
                        |> Option.map (fun i -> parent.nodeId, i)))

    /// Ingress entry for a zoom-in: selection parent for non-leaves; site-map
    /// parent of the new zoom root for leaf zoom-in.
    let tryZoomInIngress
        (isLeaf: bool)
        (sel: Selection)
        (siteMap: SiteMap)
        (newZoomRoot: NodeId)
        : (NodeId * int) option =
        if isLeaf then
            trySiteMapParentOccurrence siteMap newZoomRoot
        else
            Some (sel.range.parent.nodeId, sel.focus)

    let private childIndexOf (parent: Node) (childId: NodeId) (stored: int) : int option =
        let live =
            parent.children
            |> List.tryFindIndex (fun c -> c.id = childId)
        match live with
        | None -> None
        | Some idx ->
            match List.tryItem stored parent.children with
            | Some c when c.id = childId -> Some stored
            | _ -> Some idx

    /// Prefer validated stack top for zoom-out; else parentByChild. Returns
    /// (parentId, childIndex, remainingStack).
    let resolveZoomOutParent
        (graph: Graph)
        (currentZoomRoot: NodeId)
        (stack: (NodeId * int) list)
        : (NodeId * int * (NodeId * int) list) option =
        let rec loop remaining =
            match remaining with
            | (parentId, storedIndex) :: rest ->
                match Map.tryFind parentId graph.nodes with
                | None -> loop rest
                | Some parent ->
                    match childIndexOf parent currentZoomRoot storedIndex with
                    | Some index -> Some (parentId, index, rest)
                    | None -> loop rest
            | [] ->
                Graph.tryFindParentAndIndex currentZoomRoot graph
                |> Option.map (fun (p, i) -> p, i, [])

        loop stack

    /// Node ids from ingress root toward zoomRoot (stack is parent-nearest-first).
    let zoomIngressPathIds
        (zoomRoot: NodeId)
        (stack: (NodeId * int) list)
        : NodeId list =
        (List.rev stack |> List.map fst) @ [ zoomRoot ]

    /// Jump zoom to an ancestor on the ingress path. Returns
    /// (newZoomRoot, childIndex for selection, truncated stack).
    let tryZoomToIngressPathNode
        (graph: Graph)
        (zoomRoot: NodeId)
        (stack: (NodeId * int) list)
        (targetId: NodeId)
        : (NodeId * int * (NodeId * int) list) option =
        if targetId = zoomRoot then None
        else
            match List.tryFindIndex (fun (parentId, _) -> parentId = targetId) stack with
            | None -> None
            | Some i ->
                let _, childIndex = stack.[i]
                if Map.containsKey targetId graph.nodes then
                    Some (targetId, childIndex, List.skip (i + 1) stack)
                else None

    /// Default Zoom after StateLoaded: first child of Graph root, else the root.
    let firstGraphChild (graph: Graph) : NodeId =
        match Map.tryFind graph.root graph.nodes with
        | None -> graph.root
        | Some node ->
            match List.tryHead node.children with
            | Some child -> child.id
            | None -> graph.root

    /// Keep preferred Zoom when Resident; otherwise the StateLoaded default.
    let resolveZoomRoot (graph: Graph) (preferred: NodeId) : NodeId =
        if Map.containsKey preferred graph.nodes then preferred
        else firstGraphChild graph

    /// When preferred Zoom is absent from the Graph, rebuild SiteMap from the fallback.
    /// Returns (zoomRoot, siteMap, nextSiteId, changed).
    let retargetZoomIfMissing
        (graph: Graph)
        (zoomRoot: NodeId)
        (siteMap: SiteMap)
        (nextSiteId: SiteId)
        : NodeId * SiteMap * SiteId * bool =
        let resolved = resolveZoomRoot graph zoomRoot
        if resolved = zoomRoot then
            zoomRoot, siteMap, nextSiteId, false
        else
            let sm, nid = buildSiteMapFrom graph resolved (Sid 0)
            resolved, sm, nid, true
