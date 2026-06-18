namespace Gambol.Shared

type DocumentPathMove = {
    nodeId: NodeId
    oldPath: string
    newPath: string
}

[<RequireQualifiedAccess>]
module DocumentPathMove =

    let private isDocumentRoot (kind: NodeKind) : bool =
        match kind with
        | Special (Workspace | Directory | File) -> true
        | _ -> false

    let private nameString (name: Filename) : string =
        match name with
        | Filename.Ok s -> s
        | _ -> ""

    let private pathForDocumentRoot (graph: Graph) (nodeId: NodeId) : string option =
        Map.tryFind nodeId graph.nodes
        |> Option.bind (fun node ->
            if isDocumentRoot node.kind then
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
        | Some node when not (isDocumentRoot node.kind) -> None
        | Some node ->
            match pathForDocumentRoot graph nodeId with
            | None -> None
            | Some oldPath ->
                match Filename.create newName with
                | Filename.Ok validName ->
                    let newPath =
                        match node.kind with
                        | Special Workspace -> Some ("@" + validName + ":")
                        | Special (Directory | File) ->
                            let parentId =
                                graph.ownerParentByChild
                                |> Map.tryFind nodeId
                                |> Option.defaultValue Graph.rootId
                            let parentPath =
                                if parentId = Graph.rootId then
                                    Some "@:"
                                else
                                    NodeDesktopPath.pathForNodeId graph parentId
                            parentPath |> Option.map (fun prefix -> prefix + "/" + validName)
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
        | Some node when not (isDocumentRoot node.kind) -> None
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
                    newParentPath
                    |> Option.map (fun prefix -> prefix + "/" + name)
                    |> Option.map (fun newPath ->
                        { nodeId = nodeId; oldPath = oldPath; newPath = newPath })

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
            match Graph.setName nodeId oldName newName graph with
            | Error msg -> Error msg
            | Ok _ ->
                let op = Op.SetName(nodeId, oldName, newName)
                let pathMove = DocumentPathMove.planPathMoveForSetName graph nodeId newName
                Ok ([ op ], pathMove)
