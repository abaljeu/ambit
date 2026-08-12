namespace Gambol.Shared

type DocumentPathMove = {
    nodeId: NodeId
    oldPath: string
    newPath: string
}

[<RequireQualifiedAccess>]
module DocumentPathMove =

    let private joinParentName (parentPath: string) (name: string) : string =
        if parentPath.EndsWith("/") then parentPath + name
        else parentPath + "/" + name

    let private nameString (name: Filename) : string =
        match name with
        | Filename.Ok s -> s
        | _ -> ""

    let private documentRootIds (graph: Graph) : Set<NodeId> =
        graph.nodes
        |> Map.toSeq
        |> Seq.choose (fun (id, _) ->
            if DocumentPartition.isDocumentRootNode graph id then Some id else None)
        |> Set.ofSeq

    let private pathForDocumentRoot (graph: Graph) (nodeId: NodeId) : string option =
        Map.tryFind nodeId graph.nodes
        |> Option.bind (fun node ->
            if DocumentPartition.isDocumentRootNode graph nodeId then
                NodeDesktopPath.expandedPathForNodeId graph nodeId
            else
                None)

    /// Illicit `.amb`-named nodes must not drive DataDir move/delete/write.
    let private skipsArtifactFs (graph: Graph) (nodeId: NodeId) : bool =
        match Map.tryFind nodeId graph.nodes with
        | Some node -> Filename.isDirectoryFileFilename node.name
        | None -> false

    let private moveSourceRelative (graph: Graph) (move: DocumentPathMove) =
        match Map.tryFind move.nodeId graph.nodes with
        | None -> None
        | Some node when Filename.isDirectoryFileFilename node.name -> None
        | Some node ->
            match node.kind with
            | Special File ->
                DocumentPartition.artifactFileRelative graph move.nodeId
                |> Option.map (fun path -> false, path)
            | kind when NodeKind.container kind ->
                DocumentPartition.artifactDirectoryRelative graph move.nodeId
                |> Option.map (fun path -> true, path)
            | _ -> None

    let private isStrictPrefix (prefix: string) (path: string) =
        path.Length > prefix.Length && path.StartsWith(prefix, System.StringComparison.Ordinal)

    let planPathMovesBetweenGraphs (preGraph: Graph) (postGraph: Graph) : DocumentPathMove list =
        Set.union (documentRootIds preGraph) (documentRootIds postGraph)
        |> Set.toList
        |> List.choose (fun nodeId ->
            if skipsArtifactFs preGraph nodeId || skipsArtifactFs postGraph nodeId then
                None
            else
                match pathForDocumentRoot preGraph nodeId, pathForDocumentRoot postGraph nodeId with
                | Some oldPath, Some newPath when oldPath <> newPath ->
                    Some { nodeId = nodeId; oldPath = oldPath; newPath = newPath }
                | _ -> None)
        |> List.sortBy (fun move -> move.nodeId.Value)

    let coalescePathMoves (preGraph: Graph) (moves: DocumentPathMove list) : DocumentPathMove list =
        let sortable =
            moves
            |> List.choose (fun move ->
                moveSourceRelative preGraph move
                |> Option.map (fun (isDirectory, oldRelative) -> move, isDirectory, oldRelative))
            |> List.sortBy (fun (move, _, oldRelative) ->
                oldRelative.Split([| '/'; '\\' |], System.StringSplitOptions.RemoveEmptyEntries).Length,
                move.nodeId.Value)

        sortable
        |> List.fold
            (fun kept (move, isDirectory, oldRelative) ->
                let covered =
                    kept
                    |> List.exists (fun (_, keptIsDirectory, keptOldRelative) ->
                        keptIsDirectory && isStrictPrefix keptOldRelative oldRelative)

                if covered then kept else kept @ [ move, isDirectory, oldRelative ])
            []
        |> List.map (fun (move, _, _) -> move)

    let planPathMoveForSetName
        (graph: Graph)
        (nodeId: NodeId)
        (newName: string)
        : DocumentPathMove option =
        match Map.tryFind nodeId graph.nodes with
        | None -> None
        | Some node when not (DocumentPartition.isDocumentRootNode graph nodeId) -> None
        | Some node when Filename.isDirectoryFileFilename node.name -> None
        | Some node ->
            match pathForDocumentRoot graph nodeId with
            | None -> None
            | Some oldPath ->
                match Filename.create newName with
                | Filename.Ok validName ->
                    let newPath =
                        match node.kind with
                        | Special Workspace ->
                            Some (NodeDesktopPath.rootPrefix + validName)
                        | Special Directory ->
                            let parentId =
                                graph.ownerParentByChild
                                |> Map.tryFind nodeId
                                |> Option.defaultValue Graph.rootId
                            let parentPath =
                                if parentId = Graph.rootId then
                                    Some NodeDesktopPath.rootPrefix
                                else
                                    NodeDesktopPath.pathForNodeId graph parentId
                            parentPath |> Option.map (fun prefix -> joinParentName prefix validName + "/")
                        | Special File ->
                            let parentId =
                                graph.ownerParentByChild
                                |> Map.tryFind nodeId
                                |> Option.defaultValue Graph.rootId
                            let parentPath =
                                if parentId = Graph.rootId then
                                    Some NodeDesktopPath.rootPrefix
                                else
                                    NodeDesktopPath.pathForNodeId graph parentId
                            parentPath |> Option.map (fun prefix -> joinParentName prefix validName)
                        | _ -> None
                    newPath
                    |> Option.map (fun path ->
                        { nodeId = nodeId; oldPath = oldPath; newPath = path })
                | _ -> None

    let planPathMoveForReparent
        (graph: Graph)
        (nodeId: NodeId)
        (newParentId: NodeId)
        : DocumentPathMove option =
        match Map.tryFind nodeId graph.nodes with
        | None -> None
        | Some node when not (DocumentPartition.isDocumentRootNode graph nodeId) -> None
        | Some node when Filename.isDirectoryFileFilename node.name -> None
        | Some node when node.kind = Special Workspace && newParentId <> Graph.workspacesId ->
            None
        | Some node ->
            match pathForDocumentRoot graph nodeId with
            | None -> None
            | Some oldPath ->
                match Filename.tryValue node.name with
                | None -> None
                | Some name ->
                    let newParentPath =
                        if newParentId = Graph.rootId then
                            Some NodeDesktopPath.rootPrefix
                        else
                            NodeDesktopPath.pathForNodeId graph newParentId
                    let newPath =
                        match node.kind with
                        | Special Directory ->
                            newParentPath |> Option.map (fun prefix -> joinParentName prefix name + "/")
                        | _ ->
                            newParentPath |> Option.map (fun prefix -> joinParentName prefix name)
                    newPath
                    |> Option.map (fun path ->
                        { nodeId = nodeId; oldPath = oldPath; newPath = path })

[<RequireQualifiedAccess>]
module NodeRenameOps =

    let private nameString (name: Filename) : string =
        match name with
        | Filename.Ok s -> s
        | _ -> ""

    let isRenameAllowed (graph: Graph) (nodeId: NodeId) : bool =
        nodeId <> Graph.rootId
        && not (Graph.isSystemFolderNode nodeId)
        && not (Graph.isSpecialSystemDirectoryMember graph nodeId)
        && match graph.nodes |> Map.tryFind nodeId with
           | Some { kind = Special Workspace } -> false
           | Some _ -> true
           | None -> false

    let planRenameNode
        (graph: Graph)
        (nodeId: NodeId)
        (newName: string)
        : Result<Op list * DocumentPathMove option, string> =
        if not (isRenameAllowed graph nodeId) then
            Error "cannot rename this node"
        else
            let node = graph.nodes.[nodeId]
            let oldName = nameString node.name
            match Filename.create newName with
            | Filename.Ok validName when validName = oldName ->
                Ok ([], None)
            | Filename.Invalid _ | Filename.Empty ->
                Error "new name is not a valid filename"
            | Filename.Ok validName ->
                match Graph.setName nodeId oldName validName graph with
                | Error msg -> Error msg
                | Ok _ ->
                    let op = Op.SetName(nodeId, oldName, validName)
                    let pathMove = DocumentPathMove.planPathMoveForSetName graph nodeId validName
                    Ok ([ op ], pathMove)
