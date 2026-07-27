namespace Gambol.Shared

open System

module ViewModelSearch =

    /// Dialog UI shows at most this many selectable rows.
    let searchDialogResultLimit = 100

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

    /// Lazy breadth-first discovery: zoom subtree first, then the rest of the root graph.
    let private discoveryNodes (zoomRoot: NodeId) (graph: Graph) : Node seq =
        let rec next state =
            match tryDequeue state.queue with
            | None ->
                match state.phase with
                | RootPhase -> None
                | ZoomPhase ->
                    next
                        { state with
                            phase = RootPhase
                            queue = snocMany emptyQueue [ graph.root ] }
            | Some (nodeId, queue) when Set.contains nodeId state.visited ->
                next { state with queue = queue }
            | Some (nodeId, queue) ->
                let visited = Set.add nodeId state.visited
                match Map.tryFind nodeId graph.nodes with
                | None -> next { state with visited = visited; queue = queue }
                | Some node ->
                    let childIds = node.children |> List.map (fun child -> child.id)
                    let nextState =
                        { state with
                            visited = visited
                            queue = snocMany queue childIds }
                    Some(node, nextState)

        { phase = ZoomPhase
          visited = Set.empty
          queue = snocMany emptyQueue [ zoomRoot ] }
        |> Seq.unfold next

    type private PartFilter = { part: string; refIds: Set<NodeId> }

    let private buildPartFilter (ctx: RefContext) (part: string) (graph: Graph) : PartFilter =
        let refIds =
            match RefExpr.parse part with
            | Error _ -> Set.empty
            | Ok expr ->
                RefExpr.match_ ctx graph expr
                |> List.map (fun r -> r.nodeId)
                |> Set.ofList
        { part = part; refIds = refIds }

    let private nodeMatchesPartFilter (pf: PartFilter) (nodeId: NodeId) (node: Node) : bool =
        nodeMatchesPart pf.part node || Set.contains nodeId pf.refIds

    let private nodeMatchesAllParts (parts: PartFilter list) (nodeId: NodeId) (node: Node) : bool =
        parts |> List.forall (fun pf -> nodeMatchesPartFilter pf nodeId node)

    let private searchResultSeq
        (query: string)
        (zoomRoot: NodeId)
        (graph: Graph)
        : NodeSearchResult seq =
        match parseSearchParts query with
        | None -> Seq.empty
        | Some parts ->
            let ctx = RefExpr.refContext zoomRoot graph
            let filters = parts |> List.map (fun part -> buildPartFilter ctx part graph)
            discoveryNodes zoomRoot graph
            |> Seq.choose (fun node ->
                if nodeMatchesAllParts filters node.id node then
                    Some(nodeToSearchResult node)
                else
                    None)

    let private searchNodesWithLimit
        (limit: int option)
        (query: string)
        (zoomRoot: NodeId)
        (graph: Graph)
        : NodeSearchResult list =
        let results = searchResultSeq query zoomRoot graph
        match limit with
        | None -> results
        | Some count -> results |> Seq.truncate count
        |> Seq.toList

    let searchNodes (query: string) (zoomRoot: NodeId) (graph: Graph) : NodeSearchResult list =
        searchNodesWithLimit None query zoomRoot graph

    let searchNodesBounded (query: string) (zoomRoot: NodeId) (graph: Graph) : NodeSearchResult list =
        searchNodesWithLimit (Some searchDialogResultLimit) query zoomRoot graph

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

    let trySearchResultAtDisplayIndexBounded
        (query: string)
        (zoomRoot: NodeId)
        (graph: Graph)
        (selectedIndex: int)
        : NodeSearchResult option =
        searchNodesBounded query zoomRoot graph
        |> tryResultAtDisplayIndex selectedIndex

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
            zoomIngress = ViewModel.ownerPathIngress model.graph zoomId
            siteMap = siteMap
            nextSiteId = nextId
            selectedNodes = ViewModel.firstChildSelection siteMap zoomId
            mode = Selecting }, []
