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
        |> List.find (fun (_, _, child) -> child.ref = Ownership.Owner)

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
