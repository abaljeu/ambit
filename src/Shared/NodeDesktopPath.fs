namespace Gambol.Shared

[<RequireQualifiedAccess>]
module NodeDesktopPath =
    let rec private pathForNode (graph: Graph) (visited: Set<NodeId>) (node: Node) : string option =
        if Set.contains node.id visited then
            None
        else
            match node.kind with
            | Normal ->
                match FileReference.parseFirst node.text with
                | FileReference path -> Some path
                | _ -> None
            | Special Workspaces
            | Special Trash -> None
            | Special Workspace ->
                match Filename.tryValue node.name with
                | Some name -> Some ("@" + name + ":")
                | None -> None
            | Special Directory
            | Special File ->
                match Filename.tryValue node.name with
                | None -> None
                | Some name ->
                    match Map.tryFind node.owner graph.nodes with
                    | None -> None
                    | Some ownerNode ->
                        pathForNode graph (Set.add node.id visited) ownerNode
                        |> Option.map (fun ownerPath -> ownerPath + "/" + name)

    let pathForNodeId (graph: Graph) (nodeId: NodeId) : string option =
        Map.tryFind nodeId graph.nodes
        |> Option.bind (pathForNode graph Set.empty)

    let fileReferenceForNodeId (graph: Graph) (nodeId: NodeId) : FileReference option =
        match Map.tryFind nodeId graph.nodes with
        | None -> None
        | Some node ->
            match pathForNode graph Set.empty node with
            | Some path -> Some (FileReference path)
            | None ->
                match node.kind with
                | Normal -> Some (FileReference.parseFirst node.text)
                | _ -> Some NoFileReference
