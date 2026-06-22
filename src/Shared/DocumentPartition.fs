namespace Gambol.Shared

[<RequireQualifiedAccess>]
module DocumentPartition =

    let isDocumentRootNode (graph: Graph) (nodeId: NodeId) : bool =
        if nodeId = Graph.workspacesId then
            false
        elif nodeId = Graph.rootId || nodeId = Graph.trashId then
            true
        else
            match Map.tryFind nodeId graph.nodes with
            | None -> false
            | Some node ->
                match node.kind with
                | Special (Workspace | Directory | File) -> true
                | _ -> false

    let documentRootForNode (graph: Graph) (nodeId: NodeId) : NodeId option =
        let rec walk (current: NodeId) =
            if isDocumentRootNode graph current then
                Some current
            else
                match Map.tryFind current graph.ownerParentByChild with
                | None -> None
                | Some parentId -> walk parentId

        walk nodeId

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
                            if child.ref = Ownership.Owner then
                                if isNestedDocumentRootBoundary graph documentRootId child.id then
                                    Set.add child.id acc
                                else
                                    collect child.id acc
                            else
                                acc)
                        visited'

        collect documentRootId Set.empty

    let private nearestDirectoryAncestor (graph: Graph) (nodeId: NodeId) : NodeId option =
        let rec walk (current: NodeId) =
            match Map.tryFind current graph.ownerParentByChild with
            | None -> None
            | Some parentId ->
                match Map.tryFind parentId graph.nodes with
                | None -> None
                | Some parent ->
                    match parent.kind with
                    | Special Directory -> Some parentId
                    | _ -> walk parentId

        walk nodeId

    let private enclosingWorkspace (graph: Graph) (nodeId: NodeId) : NodeId option =
        let rec walk (current: NodeId) =
            if current = Graph.rootId || current = Graph.trashId then
                Some current
            else
                match Map.tryFind current graph.nodes with
                | None -> None
                | Some node ->
                    match node.kind with
                    | Special Workspace when current <> Graph.workspacesId -> Some current
                    | _ ->
                        match Map.tryFind current graph.ownerParentByChild with
                        | None -> None
                        | Some parentId -> walk parentId

        walk nodeId

    let private workspaceDiskPrefix (graph: Graph) (workspaceId: NodeId) : string option =
        if workspaceId = Graph.rootId then
            Some ""
        elif workspaceId = Graph.trashId then
            Some "TRASH/"
        else
            match Map.tryFind workspaceId graph.nodes with
            | None -> None
            | Some node ->
                match Filename.tryValue node.name with
                | Some name -> Some ("@" + name + "/")
                | None -> None

    let rec private directoryDiskRelative (graph: Graph) (dirId: NodeId) : string option =
        if dirId = Graph.trashId then
            Some "TRASH/"
        else
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
                        enclosingWorkspace graph dirId
                        |> Option.bind (workspaceDiskPrefix graph)
                        |> Option.map (fun prefix -> prefix + dirName + "/")
                | _ -> None

    let artifactDirectoryRelative (graph: Graph) (documentRootId: NodeId) : string option =
        if documentRootId = Graph.rootId then
            None
        elif documentRootId = Graph.trashId then
            Some "TRASH/"
        else
            match Map.tryFind documentRootId graph.nodes with
            | None -> None
            | Some node ->
                match node.kind with
                | Special File -> None
                | Special Workspace ->
                    match Filename.tryValue node.name with
                    | Some name -> Some ("@" + name + "/")
                    | None -> None
                | Special Directory -> directoryDiskRelative graph documentRootId
                | _ -> None

    let artifactFileRelative (graph: Graph) (documentRootId: NodeId) : string option =
        if documentRootId = Graph.rootId then
            Some ".amb"
        elif documentRootId = Graph.trashId then
            Some "TRASH/.amb"
        else
            match Map.tryFind documentRootId graph.nodes with
            | None -> None
            | Some node ->
                match node.kind with
                | Special Workspace ->
                    match Filename.tryValue node.name with
                    | Some name -> Some ("@" + name + "/.amb")
                    | None -> None
                | Special Directory ->
                    artifactDirectoryRelative graph documentRootId
                    |> Option.map (fun dir -> dir + ".amb")
                | Special File ->
                    NodeDesktopPath.pathForNodeId graph documentRootId
                    |> Option.bind NodeDesktopPath.desktopFileToDisk
                | _ -> None
