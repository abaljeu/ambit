namespace Gambol.Shared

[<RequireQualifiedAccess>]
module DocumentOpImpact =

    let private ownedChildIds (graph: Graph) (parentId: NodeId) children =
        children
        |> List.choose (fun child ->
            if Node.childOwnership graph parentId child = Ownership.Owner then
                Some child.id
            else
                None)

    let private introducedChildren (oldChildren: ChildNode list) (newChildren: ChildNode list) =
        newChildren
        |> List.filter (fun nc ->
            oldChildren
            |> List.exists (fun oc -> oc.id = nc.id && oc.ref = nc.ref)
            |> not)

    let private removedChildren (oldChildren: ChildNode list) (newChildren: ChildNode list) =
        oldChildren
        |> List.filter (fun oc ->
            newChildren
            |> List.exists (fun nc -> nc.id = oc.id && nc.ref = oc.ref)
            |> not)

    let private childListDelta (oldChildren: ChildNode list) (newChildren: ChildNode list) =
        (removedChildren oldChildren newChildren)
        @ (introducedChildren oldChildren newChildren)

    let private touchedNodeIds (graph: Graph) op =
        match op with
        | Op.NewNode(nodeId, _)
        | Op.SetText(nodeId, _, _)
        | Op.SetClasses(nodeId, _, _)
        | Op.NewSpecialNode(nodeId, _, _)
        | Op.SetName(nodeId, _, _)
        | Op.SetDocumentState(nodeId, _, _) -> [ nodeId ]
        | Op.Replace(parentId, oldChildren, newChildren) ->
            parentId
            :: (childListDelta oldChildren newChildren
                |> ownedChildIds graph parentId)
        | Op.SetUpdateTime _ -> []

    /// Current writable document roots dirtied by accepted operations and path moves.
    let documentRootsAffectedByOps
        (preGraph: Graph)
        (postGraph: Graph)
        (ops: Op list)
        (pathMoveNodeIds: NodeId list)
        : Set<NodeId> =
        let touchedIds = ops |> List.collect (touchedNodeIds preGraph)
        DocumentPartition.documentRootsAffectedByNodeIds
            preGraph
            postGraph
            touchedIds
            pathMoveNodeIds
