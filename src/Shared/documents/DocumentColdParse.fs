namespace Gambol.Shared

/// Fable-safe cold document parse → ops. No DiffPlex / previousText.
[<RequireQualifiedAccess>]
module DocumentColdParse =

    /// Default synthetic path for paste / document text import → Plain via classifyCodec.
    [<Literal>]
    let PasteRelativePath = "__paste__.txt"

    let private overlayMemberIds (graph: Graph) (documentRootId: NodeId) =
        DocumentPartition.memberNodeIds graph documentRootId
        |> Set.filter (fun nodeId ->
            nodeId = documentRootId
            || not (
                DocumentPartition.isNestedDocumentRootBoundary
                    graph
                    documentRootId
                    nodeId))

    let private createOrUpdateOps (before: Graph) (node: Node) : Op list =
        match Map.tryFind node.id before.nodes with
        | None ->
            match node.kind, Filename.tryValue node.name with
            | Special kind, Some name ->
                [ Op.NewSpecialNode(node.id, kind, name) ]
            | _ ->
                let nameOps =
                    match Filename.tryValue node.name with
                    | Some name ->
                        [ Op.SetName(node.id, "", name) ]
                    | None -> []

                let classOps =
                    if node.cssClasses = CssClass.empty then
                        []
                    else
                        [ Op.SetClasses(node.id, CssClass.empty, node.cssClasses) ]

                Op.NewNode(node.id, node.text) :: nameOps @ classOps
        | Some old ->
            let textOps =
                if old.text = node.text then
                    []
                else
                    [ Op.SetText(node.id, old.text, node.text) ]

            let nameOps =
                match Filename.tryValue old.name, Filename.tryValue node.name with
                | Some o, Some n when o <> n ->
                    [ Op.SetName(node.id, o, n) ]
                | None, Some n ->
                    [ Op.SetName(node.id, "", n) ]
                | _ -> []

            let classOps =
                if old.cssClasses = node.cssClasses then
                    []
                else
                    [ Op.SetClasses(node.id, old.cssClasses, node.cssClasses) ]

            textOps @ nameOps @ classOps

    let private isOwnedSpecial
        (graph: Graph)
        (parentId: NodeId)
        (child: ChildNode)
        =
        Node.childOwnership graph parentId child = Ownership.Owner
        && match Map.tryFind child.id graph.nodes with
           | Some { kind = Special (File | Directory) } -> true
           | _ -> false

    /// Keep owned File/Directory stubs unless the outline already owns them.
    let private withPreservedSpecials
        (before: Graph)
        (documentRootId: NodeId)
        (outlineChildren: ChildNode list)
        =
        let existingOwnedSpecials =
            match Map.tryFind documentRootId before.nodes with
            | None -> []
            | Some root ->
                root.children
                |> List.filter (isOwnedSpecial before documentRootId)

        let existingOwnedIds =
            existingOwnedSpecials
            |> List.map (fun child -> child.id)
            |> Set.ofList

        // Outline edges are proposed (pre-apply): use edge.ref, not childOwnership.
        // Phase B owner=parent would treat a Ref to an already-owned special as Owner
        // and keep the Ref edge instead of restoring the preserved Owner.
        let outlinedOwners =
            outlineChildren
            |> List.choose (fun child ->
                if child.ref = Ownership.Owner then Some child.id else None)
            |> Set.ofList

        let outlineWithoutDupRefs =
            outlineChildren
            |> List.filter (fun child ->
                not (
                    child.ref = Ownership.Ref
                    && Set.contains child.id existingOwnedIds))
        let preserved =
            existingOwnedSpecials
            |> List.filter (fun child ->
                not (Set.contains child.id outlinedOwners))

        outlineWithoutDupRefs @ preserved

    /// Omit foreign Owner claims; keep restored Refs (text codecs emit Owner).
    let private omitForeignOwnedClaims
        (before: Graph)
        (overlay: Set<NodeId>)
        (parentId: NodeId)
        (children: ChildNode list)
        : ChildNode list =
        children
        |> List.filter (fun child ->
            // Planned edge claim is edge.ref; ownerParentByChild is live before-graph.
            match child.ref with
            | Ownership.Ref -> true
            | Ownership.Owner ->
                match Map.tryFind child.id before.ownerParentByChild with
                | Some existing when
                    existing <> parentId
                    && not (Set.contains existing overlay) ->
                    false
                | _ -> true)
    /// Text projection lacks Owner/Ref — preserve prior matching Ref edges by id.
    let private restoreMatchingRefs
        (graph: Graph)
        (parentId: NodeId)
        (oldChildren: ChildNode list)
        (outlined: ChildNode list)
        : ChildNode list =
        let priorRefIds =
            oldChildren
            |> List.choose (fun c ->
                if Node.childOwnership graph parentId c = Ownership.Ref then
                    Some c.id
                else
                    None)
            |> Set.ofList

        outlined
        |> List.map (fun child ->
            if Set.contains child.id priorRefIds then
                { child with ref = Ownership.Ref }
            else
                child)

    let private plannedChildren
        (before: Graph)
        (after: Graph)
        (documentRootId: NodeId)
        (overlay: Set<NodeId>)
        (nodeId: NodeId)
        : ChildNode list * ChildNode list =
        let oldChildren =
            match Map.tryFind nodeId before.nodes with
            | Some node -> node.children
            | None -> []

        let afterChildren = after.nodes.[nodeId].children

        let outlined =
            if nodeId = documentRootId then
                withPreservedSpecials before documentRootId afterChildren
            else
                afterChildren

        let withRefs =
            restoreMatchingRefs before nodeId oldChildren outlined

        let newChildren =
            omitForeignOwnedClaims before overlay nodeId withRefs

        oldChildren, newChildren

    let private droppedOwnedIds
        (graph: Graph)
        (parentId: NodeId)
        (oldChildren: ChildNode list)
        (newChildren: ChildNode list)
        : Set<NodeId> =
        let newIds =
            newChildren |> List.map (fun c -> c.id) |> Set.ofList

        oldChildren
        |> List.choose (fun c ->
            if
                Node.childOwnership graph parentId c = Ownership.Owner
                && not (Set.contains c.id newIds)
            then
                Some c.id
            else
                None)
        |> Set.ofList

    let private replaceOps
        (oldChildren: ChildNode list)
        (newChildren: ChildNode list)
        (dropIds: Set<NodeId>)
        (nodeId: NodeId)
        : Op list =
        let remainingOld =
            oldChildren
            |> List.filter (fun c -> not (Set.contains c.id dropIds))

        if remainingOld = newChildren then
            []
        else
            [ Op.Replace(nodeId, 0, remainingOld, newChildren) ]

    /// classifyCodecForRead → codec readCold → mergeReadResult. Never readWarm.
    let readArtifactCold
        (relativePath: string)
        (text: string)
        (documentRootId: NodeId)
        (context: Graph)
        : Result<Graph, string> =
        DocumentFormat.readArtifactCold
            relativePath
            text
            documentRootId
            context

    /// before/after graph → Op list (pure; shared with DotNet warm path via
    /// DocumentParseOps / DocumentWarm — module name is historical; this planner
    /// is not cold-only). Unmatched owned children use Delete → TRASH.
    let planOpsFromGraphs
        (before: Graph)
        (documentRootId: NodeId)
        (after: Graph)
        : Op list =
        let overlay =
            overlayMemberIds after documentRootId

        let overlayList = Set.toList overlay

        let nodeOps =
            overlayList
            |> List.collect (fun nodeId ->
                createOrUpdateOps before after.nodes.[nodeId])

        let planned =
            overlayList
            |> List.map (fun nodeId ->
                let oldC, newC =
                    plannedChildren
                        before after documentRootId overlay nodeId
                nodeId,
                oldC,
                newC,
                droppedOwnedIds before nodeId oldC newC)

        // Owner claimed under another overlay parent is a reparent via Replace,
        // not an unmatched drop — Delete→trash would dual-Own with reclaim.
        let reclaimedOwned =
            planned
            |> List.collect (fun (_, _, newC, _) ->
                newC
                |> List.choose (fun c ->
                    if c.ref = Ownership.Owner then Some c.id else None))
            |> Set.ofList

        let dropByParent =
            planned
            |> List.choose (fun (nodeId, _, _, drops) ->
                let unmatched = Set.difference drops reclaimedOwned
                if Set.isEmpty unmatched then None
                else Some(nodeId, unmatched))
            |> Map.ofList

        let deleteOps =
            ViewModelDeleteOps.planDeleteDroppedOwnedMany before dropByParent

        let childOps =
            planned
            |> List.collect (fun (nodeId, oldC, newC, drops) ->
                let dropIds = Set.difference drops reclaimedOwned
                replaceOps oldC newC dropIds nodeId)

        nodeOps @ deleteOps @ childOps

    let private targetsDocumentRoot (documentRootId: NodeId) =
        function
        | Op.Replace(id, _, _, _) when id = documentRootId -> true
        | Op.SetText(id, _, _) when id = documentRootId -> true
        | Op.SetName(id, _, _) when id = documentRootId -> true
        | Op.SetClasses(id, _, _) when id = documentRootId -> true
        | Op.NewSpecialNode(id, _, _) when id = documentRootId -> true
        | Op.SetDocumentState(id, _, _) when id = documentRootId -> true
        | Op.SetUpdateTime(id, _, _) when id = documentRootId -> true
        | _ -> false

    /// Drop root-targeting ops; return pasted top-level child ids + nested ops.
    let peelDocumentRootOps
        (documentRootId: NodeId)
        (ops: Op list)
        : NodeId list * Op list =
        let topLevelIds =
            ops
            |> List.tryPick (function
                | Op.Replace(id, _, _, children) when id = documentRootId ->
                    Some(children |> List.map (fun c -> c.id))
                | _ -> None)
            |> Option.defaultValue []

        let nested =
            ops |> List.filter (targetsDocumentRoot documentRootId >> not)

        topLevelIds, nested

    /// Cold read + plan ops. Client Paste / ImportText document branch call this.
    let planApplyCold
        (graph: Graph)
        (documentRootId: NodeId)
        (relativePath: string)
        (text: string)
        : Result<Op list, string> =
        readArtifactCold relativePath text documentRootId graph
        |> Result.map (planOpsFromGraphs graph documentRootId)

    let private stubPasteGraph (documentRootId: NodeId) : Graph =
        let graph0 = Graph.create ()
        let name = PasteRelativePath

        let file =
            Node.Create(
                documentRootId,
                text = name,
                name = Filename.create name,
                owner = graph0.root,
                kind = Special File,
                documentState = Unparsed)

        graph0.nodes
        |> Map.add documentRootId file
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    /// External paste: cold-plan on a disposable stub root, then peel.
    /// Do not plan against the live insertion parent — existing siblings produce
    /// delete Replaces that peelDocumentRootOps mis-reads as empty top-level ids.
    let planPasteOps (text: string) : Result<NodeId list * Op list, string> =
        let documentRootId = NodeId.New()
        let graph = stubPasteGraph documentRootId

        planApplyCold graph documentRootId PasteRelativePath text
        |> Result.bind (fun ops ->
            let topLevelIds, nested =
                peelDocumentRootOps documentRootId ops

            if topLevelIds.IsEmpty then
                Error "paste cold parse produced no nodes"
            else
                Ok(topLevelIds, nested))
