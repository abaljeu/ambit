namespace Gambol.Shared

[<RequireQualifiedAccess>]
module FileExpand =

    let pathExprText (graph: Graph) (nodeId: NodeId) : string option =
        NodeDesktopPath.pathForNodeId graph nodeId
        |> Option.map (fun path -> "[[" + path + "]]")

    let needsParse (graph: Graph) (nodeId: NodeId) : bool =
        match Map.tryFind nodeId graph.nodes with
        | Some { kind = Special File; fileState = FileState.Unparsed } -> true
        | _ -> false

    let isStale (graph: Graph) (nodeId: NodeId) (diskMtimeUtc: int64) : bool =
        match Map.tryFind nodeId graph.nodes with
        | Some { kind = Special File; fileState = state } -> FileState.isStale diskMtimeUtc state
        | _ -> false

    let private newNodeOp (node: Node) : Op =
        match node.kind with
        | Normal -> Op.NewNode(node.id, node.text)
        | Special kind ->
            match node.name with
            | Filename.Ok name -> Op.NewSpecialNode(node.id, kind, name)
            | _ -> Op.NewSpecialNode(node.id, kind, "file")

    let private collectNewNodeOps (before: Graph) (after: Graph) : Op list =
        after.nodes
        |> Map.toList
        |> List.choose (fun (nid, node) ->
            if Map.containsKey nid before.nodes then None else Some(newNodeOp node))

    let private collectChildReplaceOps (before: Graph) (after: Graph) (rootIds: Set<NodeId>) : Op list =
        rootIds
        |> Set.toList
        |> List.choose (fun parentId ->
            match Map.tryFind parentId before.nodes, Map.tryFind parentId after.nodes with
            | Some oldNode, Some newNode when oldNode.children <> newNode.children ->
                Some(Op.Replace(parentId, 0, oldNode.children, newNode.children))
            | _ -> None)

    let private subtreeIds (graph: Graph) (rootId: NodeId) : Set<NodeId> =
        let rec walk acc pending =
            match pending with
            | [] -> acc
            | nid :: rest when Set.contains nid acc -> walk acc rest
            | nid :: rest ->
                let children =
                    graph.nodes
                    |> Map.tryFind nid
                    |> Option.map (fun n -> n.children |> List.map (fun c -> c.id))
                    |> Option.defaultValue []
                walk (Set.add nid acc) (children @ rest)

        walk Set.empty [ rootId ]

    /// Plan ops to parse one file's disk content into graph children and mark it parsed.
    let planParseFile
        (graph: Graph)
        (fileNodeId: NodeId)
        (relativePath: string)
        (fileText: string)
        (diskMtimeUtc: int64)
        : Result<Op list * StatusMessage option, string> =
        match Map.tryFind fileNodeId graph.nodes with
        | None -> Error "file node not found"
        | Some node when node.kind <> Special File -> Error "node is not a Special File"
        | Some node ->
            let oldState = node.fileState

            match DocumentFormat.readArtifact relativePath fileText fileNodeId graph with
            | Error msg -> Error msg
            | Ok parsedGraph ->
                let scope = subtreeIds parsedGraph fileNodeId
                let newOps = collectNewNodeOps graph parsedGraph
                let replaceOps = collectChildReplaceOps graph parsedGraph scope
                let setStateOp = Op.SetFileState(fileNodeId, oldState, FileState.Parsed diskMtimeUtc)
                let ops = newOps @ replaceOps @ [ setStateOp ]

                let status =
                    if FileState.isStale diskMtimeUtc oldState then
                        Some(StatusMessage.warn "File changed on disk — reparse to refresh")
                    else
                        None

                Ok(ops, status)
