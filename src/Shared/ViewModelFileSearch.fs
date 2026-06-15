namespace Gambol.Shared

open System

type FileSearchResult =
    { nodeId: NodeId
      text: string
      name: Filename
      pathLabel: string }

[<RequireQualifiedAccess>]
module ViewModelFileSearch =

    let private nodeToFileResult (graph: Graph) (node: Node) : FileSearchResult =
        let pathLabel =
            NodeDesktopPath.pathForNodeId graph node.id
            |> Option.defaultWith (fun () ->
                node.name
                |> Filename.tryValue
                |> Option.defaultValue node.text)

        { nodeId = node.id
          text = node.text
          name = node.name
          pathLabel = pathLabel }

    let private isFileNode (node: Node) : bool =
        match node.kind with
        | Special File -> true
        | _ -> false

    let private containsCaseInsensitive (needle: string) (haystack: string) : bool =
        haystack.ToLowerInvariant().IndexOf(needle.ToLowerInvariant()) >= 0

    let private nodeMatchesPart (part: string) (node: Node) : bool =
        let nameOk = node.name |> Filename.tryValue |> Option.exists (containsCaseInsensitive part)
        nameOk

    let private textMatchesPart
        (part: string)
        (discoveryOrder: NodeId list)
        (graph: Graph)
        : FileSearchResult list =
        discoveryOrder
        |> List.choose (fun nid ->
            let node = graph.nodes.[nid]

            if isFileNode node && nodeMatchesPart part node then
                Some(nodeToFileResult graph node)
            else
                None)

    let private refMatchesPart
        (ctx: RefContext)
        (part: string)
        (discoverySet: Set<NodeId>)
        (graph: Graph)
        : FileSearchResult list =
        match RefExpr.parse part with
        | Error _ -> []
        | Ok expr ->
            RefExpr.match_ ctx graph expr
            |> List.filter (fun r -> Set.contains r.nodeId discoverySet)
            |> List.choose (fun r ->
                let node = graph.nodes.[r.nodeId]

                if isFileNode node then
                    Some(nodeToFileResult graph node)
                else
                    None)

    let private mergePartResults
        (refHits: FileSearchResult list)
        (textHits: FileSearchResult list)
        : FileSearchResult list =
        let rec dedupe seen (items: FileSearchResult list) : FileSearchResult list =
            match items with
            | [] -> []
            | r :: rest ->
                if Set.contains r.nodeId seen then
                    dedupe seen rest
                else
                    r :: dedupe (Set.add r.nodeId seen) rest

        dedupe Set.empty (refHits @ textHits)

    let private intersectByNodeId (lists: FileSearchResult list list) : Set<NodeId> =
        match lists with
        | [] -> Set.empty
        | first :: rest ->
            let firstSet = first |> List.map (fun r -> r.nodeId) |> Set.ofList

            rest
            |> List.fold (fun acc lst ->
                Set.intersect acc (lst |> List.map (fun r -> r.nodeId) |> Set.ofList))
                firstSet

    let private ownerChildIds (graph: Graph) (parentId: NodeId) : NodeId list =
        graph.nodes.[parentId].children
        |> List.choose (fun child ->
            if child.ref = Ownership.Owner then
                Some child.id
            else
                None)

    let private bfsOwnerFiles (graph: Graph) (visited: Set<NodeId>) (queue: NodeId list) : NodeId list * Set<NodeId> =
        let rec go visited accOrder q =
            match q with
            | [] -> List.rev accOrder, visited
            | u :: rest ->
                if Set.contains u visited then
                    go visited accOrder rest
                else
                    let node = graph.nodes.[u]
                    let visited2 = Set.add u visited
                    let acc2 =
                        if isFileNode node then
                            u :: accOrder
                        else
                            accOrder

                    let next =
                        ownerChildIds graph u
                        |> List.filter (fun c -> Map.containsKey c graph.nodes)

                    go visited2 acc2 (rest @ next)

        go visited [] queue

    let private fileDiscoveryOrder (focusNodeId: NodeId) (graph: Graph) : NodeId list =
        let ctx = RefExpr.refContext focusNodeId graph

        let anchors =
            [ ctx.fileDir
              ctx.workspaceRoot
              Some Graph.workspacesId ]
            |> List.choose id
            |> List.distinct

        let rec phases visited accOrder remaining =
            match remaining with
            | [] -> accOrder
            | anchor :: rest ->
                if not (Map.containsKey anchor graph.nodes) then
                    phases visited accOrder rest
                else
                    let order, visited2 = bfsOwnerFiles graph visited [ anchor ]
                    phases visited2 (accOrder @ order) rest

        phases Set.empty [] anchors

    let searchFiles (query: string) (focusNodeId: NodeId) (graph: Graph) : FileSearchResult list =
        match ViewModelSearch.parseSearchTerm query with
        | None -> []
        | Some term ->
            let parts =
                term.Split([| ' '; '\t'; '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
                |> Array.toList

            if parts.IsEmpty then
                []
            else
                let discoveryOrder = fileDiscoveryOrder focusNodeId graph
                let discoverySet = Set.ofList discoveryOrder
                let ctx = RefExpr.refContext focusNodeId graph

                let perPart =
                    parts
                    |> List.map (fun part ->
                        mergePartResults
                            (refMatchesPart ctx part discoverySet graph)
                            (textMatchesPart part discoveryOrder graph))

                let hitIds = intersectByNodeId perPart

                discoveryOrder
                |> List.choose (fun nid ->
                    if Set.contains nid hitIds then
                        Some(nodeToFileResult graph graph.nodes.[nid])
                    else
                        None)

    let tryFileResultAtDisplayIndex
        (query: string)
        (focusNodeId: NodeId)
        (graph: Graph)
        (selectedIndex: int)
        : FileSearchResult option =
        let results = searchFiles query focusNodeId graph

        if results.IsEmpty then
            None
        else
            let i =
                selectedIndex
                |> min (results.Length - 1)
                |> max 0

            List.tryItem i results
