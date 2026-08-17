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

    /// Fable maps IndexOf poorly with StringComparison; fold each haystack for JS and .NET.
    let private containsNormalized (needle: string) (haystack: string) : bool =
        haystack.ToLowerInvariant().IndexOf(needle) >= 0

    let private nodeMatchesPart (normalizedPart: string) (node: Node) : bool =
        let textOk = containsNormalized normalizedPart node.text
        let nameOk =
            node.name
            |> Filename.tryValue
            |> Option.exists (containsNormalized normalizedPart)
        textOk || nameOk

    let private nodeToSearchResult (node: Node) : NodeSearchResult =
        { nodeId = node.id
          text = node.text
          name = node.name }

    type private IdQueue = { front: NodeId list; back: NodeId list }

    let private emptyQueue = { front = []; back = [] }

    let private snocMany (q: IdQueue) (ids: NodeId list) : IdQueue =
        List.fold (fun acc id -> { acc with back = id :: acc.back }) q ids

    let private tryDequeue (q: IdQueue) : (NodeId * IdQueue) option =
        match q.front with
        | u :: rest -> Some (u, { front = rest; back = q.back })
        | [] ->
            match q.back with
            | [] -> None
            | _ ->
                let front = List.rev q.back
                match front with
                | u :: rest -> Some (u, { front = rest; back = [] })
                | [] -> None

    type private DiscoveryPhase =
        | ZoomPhase
        | RootPhase

    type private DiscoveryState =
        { phase: DiscoveryPhase
          visited: Set<NodeId>
          queue: IdQueue }

    let rec private nextDiscoveryNode
        (graph: Graph)
        (state: DiscoveryState)
        : (Node * DiscoveryState) option =
        match tryDequeue state.queue with
        | None ->
            match state.phase with
            | RootPhase -> None
            | ZoomPhase ->
                nextDiscoveryNode graph
                    { state with
                        phase = RootPhase
                        queue = snocMany emptyQueue [ graph.root ] }
        | Some (nodeId, queue) when Set.contains nodeId state.visited ->
            nextDiscoveryNode graph { state with queue = queue }
        | Some (nodeId, queue) ->
            let visited = Set.add nodeId state.visited
            match Map.tryFind nodeId graph.nodes with
            | None -> nextDiscoveryNode graph { state with visited = visited; queue = queue }
            | Some node ->
                let childIds = node.children |> List.map (fun child -> child.id)
                Some(
                    node,
                    { state with
                        visited = visited
                        queue = snocMany queue childIds })

    type private PartFilter =
        { normalizedPart: string
          refIds: Set<NodeId> }

    let private buildPartFilter (ctx: RefContext) (part: string) (graph: Graph) : PartFilter =
        let refIds =
            match RefExpr.parse part with
            | Error _ -> Set.empty
            | Ok expr ->
                RefExpr.match_ ctx graph expr
                |> List.map (fun r -> r.nodeId)
                |> Set.ofList
        { normalizedPart = part.ToLowerInvariant()
          refIds = refIds }

    let private nodeMatchesPartFilter (pf: PartFilter) (nodeId: NodeId) (node: Node) : bool =
        nodeMatchesPart pf.normalizedPart node || Set.contains nodeId pf.refIds

    let private nodeMatchesAllParts (parts: PartFilter list) (nodeId: NodeId) (node: Node) : bool =
        parts |> List.forall (fun pf -> nodeMatchesPartFilter pf nodeId node)

    type SearchCursor =
        private
            { graph: Graph
              filters: PartFilter list
              discovery: DiscoveryState }

    let startSearch
        (query: string)
        (zoomRoot: NodeId)
        (graph: Graph)
        : SearchCursor option =
        match parseSearchParts query with
        | None -> None
        | Some parts ->
            let ctx = RefExpr.refContext zoomRoot graph
            let filters = parts |> List.map (fun part -> buildPartFilter ctx part graph)
            Some
                { graph = graph
                  filters = filters
                  discovery =
                    { phase = ZoomPhase
                      visited = Set.empty
                      queue = snocMany emptyQueue [ zoomRoot ] } }

    let takeResults
        (count: int)
        (cursor: SearchCursor)
        : NodeSearchResult list * SearchCursor option =
        let rec collect remaining resultsRev discovery =
            if remaining <= 0 then
                List.rev resultsRev, Some { cursor with discovery = discovery }
            else
                match nextDiscoveryNode cursor.graph discovery with
                | None -> List.rev resultsRev, None
                | Some (node, nextDiscovery) ->
                    if nodeMatchesAllParts cursor.filters node.id node then
                        collect
                            (remaining - 1)
                            (nodeToSearchResult node :: resultsRev)
                            nextDiscovery
                    else
                        collect remaining resultsRev nextDiscovery
        collect count [] cursor.discovery

    let searchNodes (query: string) (zoomRoot: NodeId) (graph: Graph) : NodeSearchResult list =
        match startSearch query zoomRoot graph with
        | None -> []
        | Some cursor -> takeResults Int32.MaxValue cursor |> fst

    let private tryResultAtDisplayIndex
        (selectedIndex: int)
        (results: NodeSearchResult list)
        : NodeSearchResult option =
        if results.IsEmpty then
            None
        else
            selectedIndex
            |> min (results.Length - 1)
            |> max 0
            |> fun index -> List.tryItem index results

    /// Same index rule as the search UI: clamp to [0 .. count-1].
    let trySearchResultAtDisplayIndex
        (query: string)
        (zoomRoot: NodeId)
        (graph: Graph)
        (selectedIndex: int)
        : NodeSearchResult option =
        searchNodes query zoomRoot graph
        |> tryResultAtDisplayIndex selectedIndex

    /// Find (/): after picking a search hit, re-root like prior zoom-in framing (hit if it has
    /// children, else structural parent). On that leaf/parent fallback, select the hit — not the
    /// first child under the zoom root.
    let searchPickSetRoot (hit: NodeSearchResult) (model: VM) : VM * Effect list =
        let node = model.graph.nodes.[hit.nodeId]
        let zoomId, leafTargetIndex =
            if node.children.IsEmpty then
                match Graph.tryFindParentAndIndex hit.nodeId model.graph with
                | Some (parentId, index) -> parentId, Some index
                | None -> hit.nodeId, None
            else
                hit.nodeId, None
        let siteMap, nextId =
            ViewModel.buildSiteMapFrom model.graph zoomId model.nextSiteId
        let selectedNodes =
            match leafTargetIndex with
            | Some index -> ViewModel.childSelectionAt siteMap zoomId index
            | None -> ViewModel.firstChildSelection siteMap zoomId
        { model with
            zoomRoot = zoomId
            zoomIngress = ViewModel.ownerPathIngress model.graph zoomId
            siteMap = siteMap
            nextSiteId = nextId
            selectedNodes = selectedNodes
            mode = Selecting }, []
