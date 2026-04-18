namespace Gambol.Shared

module ViewModelSearch =

    type NodeSearchQuery =
        { term: string
          preferName: bool }

    let parseNodeSearchQuery (query: string) : NodeSearchQuery option =
        let q = if isNull query then "" else query.Trim()
        if q = "" then None
        elif q.StartsWith "$" then
            let term = q.Substring(1).Trim()
            if term = "" then None
            else Some { term = term; preferName = true }
        else
            Some { term = q; preferName = false }

    let private containsCaseInsensitive (needle: string) (haystack: string) : bool =
        haystack.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0

    let private nodeSearchRank (query: NodeSearchQuery) (node: Node) : int option =
        let textMatch = containsCaseInsensitive query.term node.text
        let nameMatch =
            node.name
            |> Option.map (containsCaseInsensitive query.term)
            |> Option.defaultValue false
        if query.preferName then
            if nameMatch then Some 0
            elif textMatch then Some 1
            else None
        else
            if textMatch then Some 0
            else None

    let searchNodes (query: string) (graph: Graph) : NodeSearchResult list =
        match parseNodeSearchQuery query with
        | None -> []
        | Some parsed ->
            graph.nodes
            |> Map.toList
            |> List.choose (fun (_, node) ->
                nodeSearchRank parsed node
                |> Option.map (fun rank ->
                    rank,
                    { nodeId = node.id
                      text = node.text
                      name = node.name }))
            |> List.sortBy (fun (rank, r) ->
                rank,
                r.text.ToLowerInvariant(),
                (r.name |> Option.defaultValue "" |> fun n -> n.ToLowerInvariant()),
                r.nodeId.Value.ToString("N"))
            |> List.map snd

    /// Same index rule as the search UI: clamp to [0 .. count-1].
    let trySearchResultAtDisplayIndex
        (query: string)
        (graph: Graph)
        (selectedIndex: int)
        : NodeSearchResult option =
        let results = searchNodes query graph
        if results.IsEmpty then None
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
            else hit.nodeId
        let siteMap, nextId =
            ViewModel.buildSiteMapFrom model.graph zoomId model.nextSiteId
        { model with
            zoomRoot = zoomId
            siteMap = siteMap
            nextSiteId = nextId
            selectedNodes = ViewModel.firstChildSelection siteMap zoomId
            mode = Selecting }, []

