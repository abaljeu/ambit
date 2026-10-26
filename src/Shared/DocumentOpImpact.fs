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

    let private documentStateTransition op =
        match op with
        | Op.SetDocumentState(id, oldState, newState) ->
            Some(id, oldState, newState)
        | _ -> None

    /// Notice whose document-state write matches this SetDocumentState.
    /// PullingFile to Unparsed is arrival. Any other Unparsed mark is
    /// MarkUnparsed. A parsed mark waits until the change is not an
    /// upload or an arrival.
    let noticesBeside (ops: Op list) : Notice list =
        let states = ops |> List.choose documentStateTransition
        let uploadOrArrival =
            states
            |> List.exists (function
                | _, _, PullingFile
                | _, _, Unparsed -> true
                | _ -> false)
        let pick f = states |> List.choose f
        pick (function
            | id, _, PullingFile -> Some(Notice.NodeUploading id)
            | _ -> None)
        @ pick (function
            | id, PullingFile, Unparsed -> Some(Notice.NodeReceived id)
            | _ -> None)
        @ pick (function
            | id, PullingFile, _ -> None
            | id, _, Unparsed -> Some(Notice.MarkUnparsed id)
            | _ -> None)
        @ (if uploadOrArrival then
               []
           else
               pick (function
                   | id, Unparsed, Current -> Some(Notice.NodeParsed id)
                   | _ -> None))

    let private unparsedFileRoot (graph: Graph) (nodeId: NodeId) =
        match DocumentPartition.documentRootForNode graph nodeId with
        | None -> None
        | Some rootId ->
            match Map.tryFind rootId graph.nodes with
            | Some node when
                node.kind = Special File
                && Node.documentStateEnum node = Unparsed ->
                Some rootId
            | _ -> None

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

    /// Parse ops inside an Unparsed File infer a lead NodeParsed for that file.
    let inferNotices
        (graph: Graph)
        (commandName: string)
        (ops: Op list)
        : Notice list =
        if commandName <> "Parse" then
            []
        else
            ops
            |> List.choose (fun op ->
                opAnchor op |> Option.bind (unparsedFileRoot graph))
            |> List.distinct
            |> List.map Notice.NodeParsed

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
