namespace Gambol.Shared

/// Span extract for Core Command launch: parent plus child occurrences.
[<RequireQualifiedAccess>]
module GraphSpan =

    [<Literal>]
    let emptySpan = "empty span"

    [<Literal>]
    let parentMissing = "parent not found"

    [<Literal>]
    let outOfRange = "span out of range"

    let private trySlice
        (graph: Graph)
        (span: NodeRange)
        : Result<Node * ChildNode list, string> =
        if span.start < 0 || span.endd <= span.start then
            Error emptySpan
        else
            match Map.tryFind span.pnode graph.nodes with
            | None -> Error parentMissing
            | Some parent ->
                if span.endd > parent.children.Length then
                    Error outOfRange
                else
                    let sliced =
                        parent.children
                        |> List.skip span.start
                        |> List.take (span.endd - span.start)
                    Ok(parent, sliced)

    let rec private addOwned
        (graph: Graph)
        (nodeId: NodeId)
        (acc: Map<NodeId, Node>)
        : Map<NodeId, Node> =
        if Map.containsKey nodeId acc then
            acc
        else
            match Map.tryFind nodeId graph.nodes with
            | None -> acc
            | Some node ->
                let acc = Map.add nodeId node acc
                node.children
                |> List.fold
                    (fun acc child ->
                        if child.ref = Ownership.Owner then
                            addOwned graph child.id acc
                        else
                            acc)
                    acc

    let spanIds (graph: Graph) (span: NodeRange) : Result<Set<NodeId>, string> =
        match trySlice graph span with
        | Error err -> Error err
        | Ok(_, children) ->
            children |> List.map _.id |> Set.ofList |> Ok

    let extract (graph: Graph) (span: NodeRange) : Result<Graph, string> =
        match trySlice graph span with
        | Error err -> Error err
        | Ok(parent, children) ->
            let parent' = { parent with children = children }
            let nodes =
                children
                |> List.fold
                    (fun acc child -> addOwned graph child.id acc)
                    (Map.add span.pnode parent' Map.empty)
            Ok(GraphBuild.fromExtracted span.pnode nodes)

    let withLockPresent (ids: Set<NodeId>) (graph: Graph) : Graph =
        if Set.isEmpty ids then
            graph
        else
            let nodes =
                ids
                |> Set.fold
                    (fun nodes id ->
                        match Map.tryFind id nodes with
                        | None -> nodes
                        | Some node ->
                            Map.add id { node with lockPresent = true } nodes)
                    graph.nodes
            { graph with nodes = nodes }
