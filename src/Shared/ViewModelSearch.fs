namespace Gambol.Shared

open System

module ViewModelSearch =

    /// Trims; strips a leading `$` and trims again. None when the effective term is empty.
    let parseSearchTerm (query: string) : string option =
        let q = if isNull query then "" else query.Trim()
        if q = "" then
            None
        else
            let t =
                if q.StartsWith "$" then
                    q.Substring(1).Trim()
                else
                    q
            if t = "" then None else Some t

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
        let nameOk = node.name |> Option.exists (fun f -> containsCaseInsensitive part f.Value)
        textOk || nameOk

    let private nodeMatchesAllParts (parts: string list) (node: Node) : bool =
        parts |> List.forall (fun p -> nodeMatchesPart p node)

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
            searchDiscoveryOrder zoomRoot graph
            |> List.choose (fun nid ->
                let node = graph.nodes.[nid]
                if nodeMatchesAllParts parts node then
                    Some
                        { nodeId = node.id
                          text = node.text
                          name = node.name }
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
            siteMap = siteMap
            nextSiteId = nextId
            selectedNodes = ViewModel.firstChildSelection siteMap zoomId
            mode = Selecting }, []
