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

    let private contentNodeId op =
        match op with
        | Op.SetText(id, _, _) -> Some id
        | Op.SetClasses(id, _, _) -> Some id
        | Op.Replace(id, _, _) -> Some id
        | _ -> None

    let private fileCarrier (node: Node) =
        node.kind = Special File && Node.carriesStateAxes node

    /// Files whose content these ops edit. A Parse change is not an edit.
    let editedFileIds
        (graph: Graph)
        (commandName: string)
        (ops: Op list)
        : NodeId list =
        if commandName = "Parse" then
            []
        else
            ops
            |> List.choose contentNodeId
            |> List.choose (fun id ->
                GraphQuery.enclosing graph fileCarrier id)
            |> List.distinct

    /// Node id an op names, when the op names one.
    let opAnchor (op: Op) : NodeId option =
        match op with
        | Op.NewNode(id, _)
        | Op.SetText(id, _, _)
        | Op.SetClasses(id, _, _)
        | Op.NewSpecialNode(id, _, _)
        | Op.SetName(id, _, _)
        | Op.SetDocumentState(id, _, _)
        | Op.Replace(id, _, _) -> Some id
        | Op.SetUpdateTime _ -> None

    let private ownerOf (graph: Graph) (op: Op) : NodeId option =
        opAnchor op
        |> Option.bind (
            GraphQuery.enclosing graph Node.carriesStateAxes)

    /// Owning special nodes for these ops, nearest axis carrier first.
    let owningSpecials (graph: Graph) (ops: Op list) : NodeId list =
        ops
        |> List.choose (ownerOf graph)
        |> List.distinct

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
