namespace Gambol.Shared

open System

module ViewModelSearch =

    let parseSearchTerm (query: string) : string option =
        let q = if isNull query then "" else query.Trim()
        if q = "" then
            None
        else
            Some q

    /// Whitespace-separated pieces; each must match (name or text). None if no effective parts.
    let private parseSearchParts (query: string) : string list option =
        match parseSearchTerm query with
        | None -> None
        | Some term ->
            let parts =
                term.Split([| ' '; '\t'; '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
                |> Array.toList
            if parts.IsEmpty then None else Some parts

    /// Fable maps IndexOf poorly with StringComparison; fold case for JS and .NET.
    let private containsCaseInsensitive (needle: string) (haystack: string) : bool =
        haystack.ToLowerInvariant().IndexOf(needle.ToLowerInvariant()) >= 0

    let private nodeMatchesPart (part: string) (node: Node) : bool =
        let textOk = containsCaseInsensitive part node.text
        let nameOk = node.name |> Filename.tryValue |> Option.exists (containsCaseInsensitive part)
        textOk || nameOk

    let private nodeToSearchResult (node: Node) : NodeSearchResult =
        { nodeId = node.id
          text = node.text
          name = node.name }

    let private textMatchesPart
        (part: string)
        (discoveryOrder: NodeId list)
        (graph: Graph)
        : NodeSearchResult list =
        discoveryOrder
        |> List.choose (fun nid ->
            let node = graph.nodes.[nid]

            if nodeMatchesPart part node then
                Some(nodeToSearchResult node)
            else
                None)

    let private refMatchesPart
        (ctx: RefContext)
        (part: string)
        (discoverySet: Set<NodeId>)
        (graph: Graph)
        : NodeSearchResult list =
        match RefExpr.parse part with
        | Error _ -> []
        | Ok expr ->
            RefExpr.match_ ctx graph expr
            |> List.filter (fun r -> Set.contains r.nodeId discoverySet)

    let private mergePartResults
        (refHits: NodeSearchResult list)
        (textHits: NodeSearchResult list)
        : NodeSearchResult list =
        let rec dedupeSearch seen (items: NodeSearchResult list) : NodeSearchResult list =
            match items with
            | [] -> []
            | r :: rest ->
                if Set.contains r.nodeId seen then
                    dedupeSearch seen rest
                else
                    r :: dedupeSearch (Set.add r.nodeId seen) rest

        dedupeSearch Set.empty (refHits @ textHits)

    let private intersectByNodeId (lists: NodeSearchResult list list) : Set<NodeId> =
        match lists with
        | [] -> Set.empty
        | first :: rest ->
            let firstSet = first |> List.map (fun r -> r.nodeId) |> Set.ofList

            rest
            |> List.fold (fun acc lst ->
                Set.intersect acc (lst |> List.map (fun r -> r.nodeId) |> Set.ofList))
                firstSet

    let private tryStructuralChildIds (graph: Graph) (nid: NodeId) : NodeId list =
        graph.nodes
        |> Map.tryFind nid
        |> Option.map (fun n -> n.children |> List.map (fun c -> c.id))
        |> Option.defaultValue []

    /// Breadth-first over structural `children`; skips ids missing from `graph.nodes`.
    let private bfsAppendOrder (graph: Graph) (visited: Set<NodeId>) (queue: NodeId list) : NodeId list * Set<NodeId> =
        let rec go visited accOrder q =
            match q with
            | [] -> List.rev accOrder, visited
            | u :: rest ->
                if Set.contains u visited then
                    go visited accOrder rest
                else
                    let visited2 = Set.add u visited
                    let acc2 = u :: accOrder
                    let next =
                        tryStructuralChildIds graph u
                        |> List.filter (fun c -> Map.containsKey c graph.nodes)
                    go visited2 acc2 (rest @ next)
        go visited [] queue

    /// Phase A from `zoomRoot`, then phase B from `graph.root` with the same visited set.
    let private searchDiscoveryOrder (zoomRoot: NodeId) (graph: Graph) : NodeId list =
        let o1, v1 =
            if Map.containsKey zoomRoot graph.nodes then
                bfsAppendOrder graph Set.empty [ zoomRoot ]
            else
                [], Set.empty
        let o2, _ =
            if Map.containsKey graph.root graph.nodes then
                bfsAppendOrder graph v1 [ graph.root ]
            else
                [], v1
        o1 @ o2

    let searchNodes (query: string) (zoomRoot: NodeId) (graph: Graph) : NodeSearchResult list =
        match parseSearchParts query with
        | None -> []
        | Some parts ->
            let discoveryOrder = searchDiscoveryOrder zoomRoot graph
            let discoverySet = Set.ofList discoveryOrder
            let ctx = RefExpr.refContext zoomRoot graph

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
                    Some(nodeToSearchResult graph.nodes.[nid])
                else
                    None)

    /// Same index rule as the search UI: clamp to [0 .. count-1].
    let trySearchResultAtDisplayIndex
        (query: string)
        (zoomRoot: NodeId)
        (graph: Graph)
        (selectedIndex: int)
        : NodeSearchResult option =
        let results = searchNodes query zoomRoot graph
        if results.IsEmpty then
            None
        else
            let i =
                selectedIndex
                |> min (results.Length - 1)
                |> max 0
            List.tryItem i results

    /// Find (/): after picking a search hit, re-root the site map at the hit, or at its parent
    /// when the hit is a leaf (same framing as zoom-in on a leaf).
    let searchPickSetRoot (hit: NodeSearchResult) (model: VM) : VM * Effect list =
        let node = model.graph.nodes.[hit.nodeId]
        let zoomId =
            if node.children.IsEmpty then
                match Graph.tryFindParentAndIndex hit.nodeId model.graph with
                | Some (parentId, _) -> parentId
                | None -> hit.nodeId
            else
                hit.nodeId
        let siteMap, nextId =
            ViewModel.buildSiteMapFrom model.graph zoomId model.nextSiteId
        { model with
            zoomRoot = zoomId
            zoomIngress = ViewModel.ownerIngress model.graph zoomId
            siteMap = siteMap
            nextSiteId = nextId
            selectedNodes = ViewModel.firstChildSelection siteMap zoomId
            mode = Selecting }, []
