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

    let private pathForDocumentRoot (graph: Graph) (nodeId: NodeId) : string option =
        Map.tryFind nodeId graph.nodes
        |> Option.bind (fun node ->
            if DocumentPartition.isDocumentRootNode graph nodeId then
                NodeDesktopPath.pathForNodeId graph nodeId
            else
                None)

    let planPathMoveForSetName
        (graph: Graph)
        (nodeId: NodeId)
        (newName: string)
        : DocumentPathMove option =
        match Map.tryFind nodeId graph.nodes with
        | None -> None
        | Some node when not (DocumentPartition.isDocumentRootNode graph nodeId) -> None
        | Some node ->
            match pathForDocumentRoot graph nodeId with
            | None -> None
            | Some oldPath ->
                match Filename.create newName with
                | Filename.Ok validName ->
                    let newPath =
                        match node.kind with
                        | Special Workspace -> Some ("@" + validName + ":")
                        | Special Directory ->
                            let parentId =
                                graph.ownerParentByChild
                                |> Map.tryFind nodeId
                                |> Option.defaultValue Graph.rootId
                            let parentPath =
                                if parentId = Graph.rootId then
                                    Some "@:"
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
                                    Some "@:"
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
                            Some "@:"
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
        && nodeId <> Graph.trashId
        && nodeId <> Graph.workspacesId
        && Map.containsKey nodeId graph.nodes

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
