namespace Gambol.Shared

/// Turn a DocumentFormat artifact read into history Ops for one document root.
[<RequireQualifiedAccess>]
module DocumentParseOps =

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

    let private isOwnedSpecial (graph: Graph) (child: ChildNode) =
        child.ref = Ownership.Owner
        && match Map.tryFind child.id graph.nodes with
           | Some { kind = Special (File | Directory) } -> true
           | _ -> false

    /// Keep owned File/Directory stubs unless the outline already owns them.
    /// Outline may Ref a nested document root without an Owner edge; dropping the
    /// prior Owner occurrence then fails History ownership validation. When that
    /// Owner already covers the artifact, drop the outline Ref — Owned suffices.
    let private withPreservedSpecials
        (before: Graph)
        (documentRootId: NodeId)
        (outlineChildren: ChildNode list)
        =
        let existingOwnedSpecials =
            match Map.tryFind documentRootId before.nodes with
            | None -> []
            | Some root ->
                root.children |> List.filter (isOwnedSpecial before)
        let existingOwnedIds =
            existingOwnedSpecials
            |> List.map (fun child -> child.id)
            |> Set.ofList
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

    let private replaceOps
        (before: Graph)
        (after: Graph)
        (documentRootId: NodeId)
        (nodeId: NodeId)
        : Op list =
        let oldChildren =
            match Map.tryFind nodeId before.nodes with
            | Some node -> node.children
            | None -> []
        let afterChildren = after.nodes.[nodeId].children
        let newChildren =
            if nodeId = documentRootId then
                withPreservedSpecials before documentRootId afterChildren
            else
                afterChildren
        if oldChildren = newChildren then
            []
        else
            [ Op.Replace(nodeId, 0, oldChildren, newChildren) ]

    /// Parse one `.amb` (or plain) artifact into ops. Document stays Current.
    /// When `previousText` is present, warm-reconcile via DiffPlex helpers.
    let planApplyArtifact
        (graph: Graph)
        (documentRootId: NodeId)
        (relativePath: string)
        (text: string)
        (previousText: string option)
        : Result<Op list, string> =
        DocumentFormat.readArtifact
            relativePath
            text
            documentRootId
            graph
            previousText
        |> Result.map (fun after ->
            let overlay =
                overlayMemberIds after documentRootId
                |> Set.toList
            let nodeOps =
                overlay
                |> List.collect (fun nodeId ->
                    createOrUpdateOps graph after.nodes.[nodeId])
            let childOps =
                overlay
                |> List.collect (replaceOps graph after documentRootId)
            nodeOps @ childOps)
