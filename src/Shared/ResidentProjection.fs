namespace Gambol.Shared

/// Projected Graph transitions for a resident (possibly partial) client Graph.
[<RequireQualifiedAccess>]
module ResidentProjection =

    /// Apply one Op under Loaded rules: header facts only when Resident;
    /// structural Replace only when the parent child list is Loaded.
    let applyOp (op: Op) (state: State) : ApplyResult =
        match op with
        | Op.SetText(nodeId, _, _)
        | Op.SetClasses(nodeId, _, _)
        | Op.SetName(nodeId, _, _)
        | Op.SetDocumentState(nodeId, _, _)
        | Op.SetUpdateTime(nodeId, _, _) ->
            if Map.containsKey nodeId state.graph.nodes then
                Op.apply op state
            else
                ApplyResult.Unchanged state
        | Op.Replace(parentId, _, _) ->
            match Map.tryFind parentId state.graph.nodes with
            | Some _ when GraphChildren.isLoaded state.graph parentId ->
                Op.apply op state
            | _ ->
                ApplyResult.Unchanged state
        | Op.NewNode _
        | Op.NewSpecialNode _ ->
            Op.apply op state

    let applyOps (ops: Op list) (state: State) : ApplyResult =
        let step (accState, hasChanged) op =
            match applyOp op accState with
            | ApplyResult.Invalid _ as err -> Error err
            | ApplyResult.Unchanged s' -> Ok(s', hasChanged)
            | ApplyResult.Changed s' -> Ok(s', true)

        let result =
            ops
            |> List.fold
                (fun acc op ->
                    match acc with
                    | Error err -> Error err
                    | Ok (s, changed) -> step (s, changed) op)
                (Ok(state, false))

        match result with
        | Error (ApplyResult.Invalid(_, message)) ->
            ApplyResult.Invalid(state, message)
        | Error err -> err
        | Ok (s, false) -> ApplyResult.Unchanged s
        | Ok (s, true) -> ApplyResult.Changed s

    /// Merge authoritative package Nodes and their Loaded child lists.
    /// Package node ids missing from `packageChildMap` become Unloaded.
    let installPackages
        (packages: Node list)
        (packageChildMap: Map<NodeId, ChildNode list>)
        (graph: Graph)
        : Graph =
        if List.isEmpty packages then
            graph
        else
            let packageIds =
                packages |> List.map (fun n -> n.id) |> Set.ofList
            let mergedNodes =
                packages
                |> List.fold
                    (fun nodes node -> Map.add node.id node nodes)
                    graph.nodes
            let withoutPackage =
                packageIds
                |> Set.fold (fun m id -> Map.remove id m) graph.childMap
            let mergedChildMap =
                packageChildMap
                |> Map.fold (fun acc k v -> Map.add k v acc) withoutPackage
            Graph.fromNodes graph.root mergedNodes mergedChildMap

    let private isNamedWorkspaceBoundary (packageRootId: NodeId) (node: Node) : bool =
        match node.kind with
        | Special Workspace when node.id <> packageRootId -> true
        | _ -> false

    /// Owner-closure of a Workspace package; stops at nested named Workspace headers.
    let private collectOwnedIds (graph: Graph) (packageRootId: NodeId) : Set<NodeId> =
        let rec loop (nodeId: NodeId) (visited: Set<NodeId>) =
            if Set.contains nodeId visited then
                visited
            else
                match Map.tryFind nodeId graph.nodes with
                | None -> visited
                | Some node ->
                    let visited' = Set.add nodeId visited
                    if isNamedWorkspaceBoundary packageRootId node then
                        visited'
                    else
                        GraphChildren.get graph nodeId
                        |> List.choose (fun c ->
                            if Node.childOwnership graph nodeId c = Ownership.Owner then
                                Some c.id
                            else
                                None)
                        |> List.fold (fun visited id -> loop id visited) visited'

        loop packageRootId Set.empty

    /// Ref targets from the owned package that lie outside that package.
    let private collectRefHeaderIds (graph: Graph) (ownedIds: Set<NodeId>) : Set<NodeId> =
        ownedIds
        |> Set.toList
        |> List.collect (fun id ->
            match Map.tryFind id graph.nodes with
            | None -> []
            | Some _ ->
                GraphChildren.get graph id
                |> List.choose (fun c ->
                    if
                        Node.childOwnership graph id c = Ownership.Ref
                        && not (Set.contains c.id ownedIds)
                    then
                        Some c.id
                    else
                        None))
        |> Set.ofList

    /// Projected Nodes and Loaded child lists for one Workspace package root.
    let private projectWorkspaceSlice
        (graph: Graph)
        (packageRootId: NodeId)
        : Map<NodeId, Node> * Map<NodeId, ChildNode list> =
        let ownedIds = collectOwnedIds graph packageRootId
        let refHeaderIds = collectRefHeaderIds graph ownedIds
        let residentIds = Set.union ownedIds refHeaderIds

        let projectNode (nodeId: NodeId) =
            match Map.tryFind nodeId graph.nodes with
            | None -> None
            | Some node ->
                let headerOnly =
                    Set.contains nodeId refHeaderIds
                    || isNamedWorkspaceBoundary packageRootId node

                if headerOnly then
                    Some(node, None)
                else
                    let children =
                        GraphChildren.get graph nodeId
                        |> List.filter (fun c -> Set.contains c.id residentIds)
                    Some(node, Some children)

        residentIds
        |> Set.toList
        |> List.fold
            (fun (nodes, childMap) id ->
                match projectNode id with
                | None -> nodes, childMap
                | Some (node, None) -> Map.add id node nodes, childMap
                | Some (node, Some kids) ->
                    Map.add id node nodes, Map.add id kids childMap)
            (Map.empty, Map.empty)

    /// Workspace subgraph as a Node list for SyncResponse.packages / LoadResponse.
    let workspaceSubgraphNodes (graph: Graph) (workspaceId: NodeId) : Node list =
        projectWorkspaceSlice graph workspaceId
        |> fst
        |> Map.toList
        |> List.map snd

    let workspaceSubgraph
        (graph: Graph)
        (workspaceId: NodeId)
        : Node list * Map<NodeId, ChildNode list> =
        let nodes, childMap = projectWorkspaceSlice graph workspaceId
        nodes |> Map.toList |> List.map snd, childMap

    [<RequireQualifiedAccess>]
    type LoadRefuse =
        | MultiWorkspace

    let private distinctOwningWorkspaces
        (graph: Graph)
        (targetIds: NodeId list)
        : NodeId list =
        targetIds
        |> List.choose (fun id ->
            if Map.containsKey id graph.nodes then
                GraphQuery.enclosingWorkspace graph id
            else
                None)
        |> List.distinct

    /// True when selected targets resolve to more than one owning Workspace.
    let selectionSpansMultipleWorkspaces
        (graph: Graph)
        (targetIds: NodeId list)
        : bool =
        distinctOwningWorkspaces graph targetIds
        |> List.length > 1

    /// Optional owning-Workspace subgraph for one Load target.
    /// Missing target → empty (Change catch-up only).
    let packagesForTarget
        (graph: Graph)
        (targetId: NodeId)
        (includeWorkspace: bool)
        : Node list =
        if not includeWorkspace then
            []
        elif not (Map.containsKey targetId graph.nodes) then
            []
        else
            match GraphQuery.enclosingWorkspace graph targetId with
            | None -> []
            | Some wsId -> workspaceSubgraphNodes graph wsId

    /// Deduplicated packages for a full selection; refuses multi-Workspace.
    let packagesForTargets
        (graph: Graph)
        (targets: LoadTarget list)
        : Result<Node list * Map<NodeId, ChildNode list>, LoadRefuse> =
        let targetIds = targets |> List.map (fun t -> t.targetId)
        if selectionSpansMultipleWorkspaces graph targetIds then
            Error LoadRefuse.MultiWorkspace
        else
            let packageIds =
                targets
                |> List.choose (fun t ->
                    if t.includeWorkspace then Some t.targetId else None)
            match distinctOwningWorkspaces graph packageIds with
            | [ wsId ] -> Ok(workspaceSubgraph graph wsId)
            | _ -> Ok([], Map.empty)

    /// Capture LoadResponse fields at one EventId (events + optional subgraph).
    let captureLoadResponse
        (eventId: EventId)
        (buildEpochSec: int)
        (pageBuildEpochSec: int)
        (isReady: bool)
        (events: Ev list)
        (graph: Graph)
        (targets: LoadTarget list)
        : Result<LoadResponse, LoadRefuse> =
        match packagesForTargets graph targets with
        | Error refuse -> Error refuse
        | Ok (packages, packageChildMap) ->
            Ok
                { eventId = eventId
                  buildEpochSec = buildEpochSec
                  pageBuildEpochSec = pageBuildEpochSec
                  apiVersion = ApiVersion.current
                  isReady = isReady
                  events = events
                  packages = packages
                  packageChildMap = packageChildMap }

    /// Scoped resident graph for fresh-session bootstrap: complete ROOT Workspace,
    /// nested named Workspace headers Unloaded, reachable Ref headers without children.
    let rootBootstrapGraph (graph: Graph) : Graph =
        let nodes, childMap = projectWorkspaceSlice graph graph.root
        Graph.fromNodes graph.root nodes childMap

    let private outsideRootWorkspace (graph: Graph) (nodeId: NodeId) : bool =
        Map.containsKey nodeId graph.nodes
        && match GraphQuery.enclosingWorkspace graph nodeId with
           | Some wsId when wsId <> graph.root -> true
           | _ -> false

    /// Node id for `/state?zoom=` widen only (not UI zoom restore).
    /// Prefer zoomRoot when it already lies outside ROOT; otherwise use focus
    /// when the focused node identifies a named Workspace (Load without Zoom).
    let sessionBootstrapTarget
        (graph: Graph)
        (zoomRoot: NodeId)
        (focusId: NodeId option)
        : NodeId =
        if outsideRootWorkspace graph zoomRoot then
            zoomRoot
        else
            match focusId with
            | Some fid when outsideRootWorkspace graph fid -> fid
            | _ -> zoomRoot

    /// Persist zoom restore and bootstrap widen as separate ids.
    let sessionTargets
        (graph: Graph)
        (zoomRoot: NodeId)
        (focusId: NodeId option)
        : NodeId * NodeId =
        zoomRoot, sessionBootstrapTarget graph zoomRoot focusId

    /// Extra named Workspace to include when saved zoom lies outside ROOT.
    let private extraZoomWorkspace (graph: Graph) (savedZoom: NodeId option) : NodeId option =
        savedZoom
        |> Option.bind (fun zoomId ->
            if not (outsideRootWorkspace graph zoomId) then
                None
            else
                GraphQuery.enclosingWorkspace graph zoomId)

    /// Merge package nodes into an existing bootstrap graph (Loaded wins over Unloaded headers).
    let private mergePackageNodes
        (baseGraph: Graph)
        (extraNodes: Map<NodeId, Node>)
        (extraChildMap: Map<NodeId, ChildNode list>)
        : Graph =
        let mergedNodes, mergedChildMap =
            extraNodes
            |> Map.fold
                (fun (nodes, childMap) id node ->
                    let existingLoaded = Map.containsKey id childMap
                    let extraLoaded = Map.containsKey id extraChildMap
                    if existingLoaded && not extraLoaded then
                        nodes, childMap
                    else
                        let childMap' =
                            match Map.tryFind id extraChildMap with
                            | Some kids -> Map.add id kids childMap
                            | None -> Map.remove id childMap
                        Map.add id node nodes, childMap')
                (baseGraph.nodes, baseGraph.childMap)
        Graph.fromNodes baseGraph.root mergedNodes mergedChildMap

    let bootstrapGraph
        (scope: BootstrapScope)
        (savedZoom: NodeId option)
        (graph: Graph)
        : Graph =
        match scope with
        | BootstrapScope.FullGraph -> graph
        | BootstrapScope.RootClosure ->
            let rootScoped = rootBootstrapGraph graph
            match extraZoomWorkspace graph savedZoom with
            | None -> rootScoped
            | Some wsId ->
                let extraNodes, extraChildMap = projectWorkspaceSlice graph wsId
                mergePackageNodes rootScoped extraNodes extraChildMap

    let bootstrapStateResponse
        (scope: BootstrapScope)
        (savedZoom: NodeId option)
        (response: StateResponse)
        : StateResponse =
        { response with
            graph = bootstrapGraph scope savedZoom response.graph }
