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
            | Special Workspaces -> None
            | Special Workspace ->
                if node.id = Graph.rootId then
                    Some "@:"
                else
                    match Filename.tryValue node.name with
                    | Some name -> Some ("@" + name + ":")
                    | None -> None
            | Special Directory ->
                match Filename.tryValue node.name with
                | None -> None
                | Some name ->
                    match Map.tryFind node.owner graph.nodes with
                    | None -> None
                    | Some ownerNode ->
                        pathForNode graph (Set.add node.id visited) ownerNode
                        |> Option.map (fun ownerPath ->
                            let segment =
                                if ownerPath.EndsWith("/") then ownerPath + name
                                else ownerPath + "/" + name
                            segment + "/")
            | Special File ->
                match Filename.tryValue node.name with
                | None -> None
                | Some name ->
                    match Map.tryFind node.owner graph.nodes with
                    | None -> None
                    | Some ownerNode ->
                        pathForNode graph (Set.add node.id visited) ownerNode
                        |> Option.map (fun ownerPath ->
                            if ownerPath.EndsWith("/") then ownerPath + name
                            else ownerPath + "/" + name)

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
