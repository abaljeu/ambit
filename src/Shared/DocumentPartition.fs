namespace Gambol.Shared

[<RequireQualifiedAccess>]
module DocumentPartition =

    let isDocumentRootNode (graph: Graph) (nodeId: NodeId) : bool =
        match Map.tryFind nodeId graph.nodes with
        | None -> false
        | Some node ->
            match node.kind with
            | Special (Workspace | Directory | File) -> true
            | _ -> false

    /// Current document roots write; exact `.amb` node names never do.
    /// Legitimate Workspace/Directory `xx` still writes `xx/.amb`.
    let shouldWriteDocumentRoot (node: Node) : bool =
        node.documentState = Current
        && not (Filename.isAmbMarkerFilename node.name)

    let documentRootForNode (graph: Graph) (nodeId: NodeId) : NodeId option =
        GraphQuery.enclosing graph (fun node -> isDocumentRootNode graph node.id) nodeId

    let isNestedDocumentRootBoundary
        (graph: Graph)
        (documentRootId: NodeId)
        (nodeId: NodeId)
        : bool =
        isDocumentRootNode graph nodeId && nodeId <> documentRootId

    let memberNodeIds (graph: Graph) (documentRootId: NodeId) : Set<NodeId> =
        let rec collect (nodeId: NodeId) (visited: Set<NodeId>) =
            if Set.contains nodeId visited then
                visited
            else
                let visited' = Set.add nodeId visited
                match Map.tryFind nodeId graph.nodes with
                | None -> visited'
                | Some node ->
                    node.children
                    |> List.fold
                        (fun acc child ->
                            if Node.childOwnership graph nodeId child = Ownership.Owner then
                                if isNestedDocumentRootBoundary graph documentRootId child.id then
                                    Set.add child.id acc
                                else
                                    collect child.id acc
                            else
                                acc)
                        visited'

        collect documentRootId Set.empty

    /// Document root owning `nodeId`, plus the parent package when `nodeId` is itself a nested root.
    let containingDocumentRootIds (graph: Graph) (nodeId: NodeId) : NodeId list =
        let primary = documentRootForNode graph nodeId |> Option.toList
        let enclosing =
            if isDocumentRootNode graph nodeId then
                graph.ownerParentByChild
                |> Map.tryFind nodeId
                |> Option.bind (documentRootForNode graph)
                |> Option.toList
            else
                []
        primary @ enclosing |> List.distinct

    let private childNodeContentEquals (a: ChildNode) (b: ChildNode) : bool =
        a.ref = b.ref && a.id = b.id

    /// Persisted artifact content; ignores `updateTime` (stamp-only ops must not rewrite disk).
    let private nodeContentEquals (a: Node) (b: Node) : bool =
        a.text = b.text
        && a.name = b.name
        && a.kind = b.kind
        && a.documentState = b.documentState
        && a.owner = b.owner
        && CssClass.toList a.cssClasses = CssClass.toList b.cssClasses
        && List.length a.children = List.length b.children
        && List.forall2 childNodeContentEquals a.children b.children

    /// Writable post-graph roots containing touched nodes, plus known moved roots.
    let documentRootsAffectedByNodeIds
        (preGraph: Graph)
        (postGraph: Graph)
        (touchedNodeIds: NodeId list)
        (pathMoveNodeIds: NodeId list)
        : Set<NodeId> =
        let fromTouchedNodes =
            touchedNodeIds
            |> List.collect (fun id ->
                containingDocumentRootIds preGraph id
                @ containingDocumentRootIds postGraph id)

        fromTouchedNodes @ pathMoveNodeIds
        |> Set.ofList
        |> Set.filter (fun rootId ->
            match Map.tryFind rootId postGraph.nodes with
            | Some node ->
                isDocumentRootNode postGraph rootId
                && shouldWriteDocumentRoot node
            | None -> false)

    /// Current document roots on `postGraph` dirtied by pre→post node diffs and/or path moves.
    let documentRootsAffectedByGraphChange
        (preGraph: Graph)
        (postGraph: Graph)
        (pathMoveNodeIds: NodeId list)
        : Set<NodeId> =
        let allIds =
            Set.union
                (preGraph.nodes |> Map.toSeq |> Seq.map fst |> Set.ofSeq)
                (postGraph.nodes |> Map.toSeq |> Seq.map fst |> Set.ofSeq)

        let changedIds =
            allIds
            |> Set.filter (fun id ->
                match Map.tryFind id preGraph.nodes, Map.tryFind id postGraph.nodes with
                | Some a, Some b -> not (nodeContentEquals a b)
                | _ -> true)

        documentRootsAffectedByNodeIds
            preGraph
            postGraph
            (Set.toList changedIds)
            pathMoveNodeIds

    let private isMemberOfDocumentState state (graph: Graph) (nodeId: NodeId) =
        containingDocumentRootIds graph nodeId
        |> List.exists (fun rootId ->
            match Map.tryFind rootId graph.nodes with
            | Some node -> node.documentState = state
            | None -> false)

    let isMemberOfUnparsedDocument (graph: Graph) (nodeId: NodeId) : bool =
        isMemberOfDocumentState Unparsed graph nodeId

    let isMemberOfInaccessibleDocument (graph: Graph) (nodeId: NodeId) : bool =
        isMemberOfUnparsedDocument graph nodeId
        || isMemberOfDocumentState NoServerFile graph nodeId

    let isMemberOfUnparsedFile (graph: Graph) (nodeId: NodeId) : bool =
        containingDocumentRootIds graph nodeId
        |> List.exists (fun rootId ->
            match Map.tryFind rootId graph.nodes with
            | Some node -> node.kind = Special File && node.documentState = Unparsed
            | None -> false)

    let isMemberOfNoServerFile (graph: Graph) (nodeId: NodeId) : bool =
        containingDocumentRootIds graph nodeId
        |> List.exists (fun rootId ->
            match Map.tryFind rootId graph.nodes with
            | Some node ->
                node.kind = Special File
                && node.documentState = NoServerFile
            | None -> false)

    let private isDirectoryNode (node: Node) : bool =
        match node.kind with
        | Special Directory -> true
        | _ -> false

    let private nearestDirectoryAncestor (graph: Graph) (nodeId: NodeId) : NodeId option =
        Map.tryFind nodeId graph.ownerParentByChild
        |> Option.bind (GraphQuery.enclosing graph isDirectoryNode)

    let private workspaceDiskPrefix (graph: Graph) (workspaceId: NodeId) : string option =
        if workspaceId = Graph.rootId then
            Some ""
        else
            match Map.tryFind workspaceId graph.nodes with
            | None -> None
            | Some node ->
                match Filename.tryValue node.name with
                | Some name -> Some (name + "/")
                | None -> None

    let rec private directoryDiskRelative (graph: Graph) (dirId: NodeId) : string option =
        match Map.tryFind dirId graph.nodes with
        | None -> None
        | Some node ->
            match node.kind, Filename.tryValue node.name with
            | Special Directory, Some dirName ->
                match nearestDirectoryAncestor graph dirId with
                | Some ancestorId ->
                    directoryDiskRelative graph ancestorId
                    |> Option.map (fun ancestorPath -> ancestorPath + dirName + "/")
                | None ->
                    GraphQuery.enclosingWorkspace graph dirId
                    |> Option.bind (workspaceDiskPrefix graph)
                    |> Option.map (fun prefix -> prefix + dirName + "/")
            | _ -> None

    let artifactDirectoryRelative (graph: Graph) (documentRootId: NodeId) : string option =
        if documentRootId = Graph.rootId then
            None
        else
                match Map.tryFind documentRootId graph.nodes with
                | None -> None
                | Some node ->
                    match node.kind with
                    | Special File -> None
                    | Special Workspace ->
                        match Filename.tryValue node.name with
                        | Some name -> Some (name + "/")
                        | None -> None
                    | Special Directory -> directoryDiskRelative graph documentRootId
                    | _ -> None

    let artifactFileRelative (graph: Graph) (documentRootId: NodeId) : string option =
        if documentRootId = Graph.rootId then
            Some ".amb"
        else
                match Map.tryFind documentRootId graph.nodes with
                | None -> None
                | Some node ->
                    match node.kind with
                    | Special Workspace ->
                        match Filename.tryValue node.name with
                        | Some name -> Some (name + "/.amb")
                        | None -> None
                    | Special Directory ->
                        artifactDirectoryRelative graph documentRootId
                        |> Option.map (fun dir -> dir + ".amb")
                    | Special File ->
                        NodeDesktopPath.pathForNodeId graph documentRootId
                        |> Option.bind NodeDesktopPath.desktopFileToDisk
                    | _ -> None

    let rec ownedSubtreeHasReservedArtifactPath graph visited nodeId =
        if Set.contains nodeId visited then
            false
        else
            match Map.tryFind nodeId graph.nodes with
            | None -> false
            | Some node ->
                let invalidHere =
                    match node.kind with
                    | Special (Workspace | Directory | File) ->
                        artifactFileRelative graph nodeId
                        |> Option.exists (fun path ->
                            path.Split('/')
                            |> Array.exists Filename.isReservedSystemName)
                    | _ -> false
                invalidHere
                || (node.children
                    |> List.exists (fun child ->
                        Node.childOwnership graph nodeId child = Ownership.Owner
                        && ownedSubtreeHasReservedArtifactPath
                            graph (Set.add nodeId visited) child.id))
