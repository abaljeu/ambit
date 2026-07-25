namespace Gambol.Shared

[<RequireQualifiedAccess>]
module DocumentOpImpact =

    let private ownedChildIds children =
        children
        |> List.choose (fun child ->
            if child.ref = Ownership.Owner then Some child.id else None)

    let private touchedNodeIds op =
        match op with
        | Op.NewNode(nodeId, _)
        | Op.SetText(nodeId, _, _)
        | Op.SetClasses(nodeId, _, _)
        | Op.NewSpecialNode(nodeId, _, _)
        | Op.SetName(nodeId, _, _)
        | Op.SetDocumentState(nodeId, _, _) -> [ nodeId ]
        | Op.Replace(parentId, _, oldChildren, newChildren) ->
            parentId :: (ownedChildIds oldChildren @ ownedChildIds newChildren)
        | Op.SetUpdateTime _ -> []

    /// Current writable document roots dirtied by accepted operations and path moves.
    let documentRootsAffectedByOps
        (preGraph: Graph)
        (postGraph: Graph)
        (ops: Op list)
        (pathMoveNodeIds: NodeId list)
        : Set<NodeId> =
        let touchedIds = ops |> List.collect touchedNodeIds
        DocumentPartition.documentRootsAffectedByNodeIds
            preGraph
            postGraph
            touchedIds
            pathMoveNodeIds
