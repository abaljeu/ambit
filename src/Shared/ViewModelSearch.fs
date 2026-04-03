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

    let private ownerPathFromRoot (graph: Graph) (targetNodeId: NodeId) : NodeId list option =
        let rec loop (current: NodeId) (acc: NodeId list) =
            if current = graph.root then Some (graph.root :: acc)
            else
                match Graph.tryFindParentAndIndex current graph with
                | None -> None
                | Some (parentId, _) -> loop parentId (current :: acc)
        if not (Map.containsKey targetNodeId graph.nodes) then None
        else loop targetNodeId []

    let private tryFindChildInstanceByNodeId
        (siteMap: SiteMap)
        (parentInstId: SiteId)
        (childNodeId: NodeId)
        : SiteId option =
        Map.tryFind parentInstId siteMap.entries
        |> Option.bind (fun parent ->
            parent.children
            |> List.tryPick (fun childInstId ->
                match Map.tryFind childInstId siteMap.entries with
                | Some child when child.nodeId = childNodeId -> Some childInstId
                | _ -> None))

    let private expandPathToNode
        (graph: Graph)
        (path: NodeId list)
        (siteMap: SiteMap)
        (nextId: SiteId)
        : SiteId option * SiteMap * SiteId =
        let rec walk (remaining: NodeId list) (parentInstId: SiteId) (sm: SiteMap) (nid: SiteId) =
            match remaining with
            | [] -> Some parentInstId, sm, nid
            | childNodeId :: rest ->
                let smExpanded, nidExpanded = ViewModel.expandEntry parentInstId graph sm nid
                match tryFindChildInstanceByNodeId smExpanded parentInstId childNodeId with
                | None -> None, smExpanded, nidExpanded
                | Some childInstId -> walk rest childInstId smExpanded nidExpanded
        match path with
        | [] -> None, siteMap, nextId
        | _ :: children -> walk children siteMap.rootId siteMap nextId

    let selectNodeFromSearch (targetNodeId: NodeId) (model: VM) : VM =
        if targetNodeId = model.graph.root then
            { model with selectedNodes = None; mode = Selecting }
        else
            match ownerPathFromRoot model.graph targetNodeId with
            | None -> model
            | Some path ->
                let baseMap, baseNext =
                    ViewModel.buildSiteMapFrom model.graph model.graph.root model.nextSiteId
                let targetInstOpt, siteMap', nextId' =
                    expandPathToNode model.graph path baseMap baseNext
                let selected =
                    targetInstOpt
                    |> Option.bind (ViewModel.singleSelectionForInstance siteMap')
                { model with
                    zoomRoot = None
                    selectedNodes = selected
                    mode = Selecting
                    siteMap = siteMap'
                    nextSiteId = nextId' }
