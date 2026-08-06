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
        | Op.Replace(parentId, _, _, _) ->
            match Map.tryFind parentId state.graph.nodes with
            | Some parent when parent.childrenStatus = Loaded ->
                Op.apply op state
            | _ ->
                ApplyResult.Unchanged state
        | Op.NewNode _
        | Op.NewSpecialNode _ ->
            Op.apply op state

    let applyChange (change: Change) (state: State) : ApplyResult =
        let step (accState, hasChanged) op =
            match applyOp op accState with
            | ApplyResult.Invalid _ as err -> Error err
            | ApplyResult.Unchanged s' -> Ok(s', hasChanged)
            | ApplyResult.Changed s' -> Ok(s', true)

        let result =
            change.ops
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

    /// Merge authoritative package Nodes and rebuild Loaded-only indexes.
    let installPackages (packages: Node list) (graph: Graph) : Graph =
        if List.isEmpty packages then
            graph
        else
            let merged =
                packages
                |> List.fold
                    (fun nodes node -> Map.add node.id node nodes)
                    graph.nodes
            Graph.fromNodes graph.root merged

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
                        node.children
                        |> List.choose (fun c ->
                            if c.ref = Ownership.Owner then Some c.id else None)
                        |> List.fold (fun visited id -> loop id visited) visited'

        loop packageRootId Set.empty

    /// Ref targets from the owned package that lie outside that package.
    let private collectRefHeaderIds (graph: Graph) (ownedIds: Set<NodeId>) : Set<NodeId> =
        ownedIds
        |> Set.toList
        |> List.collect (fun id ->
            match Map.tryFind id graph.nodes with
            | None -> []
            | Some node ->
                node.children
                |> List.choose (fun c ->
                    if c.ref = Ownership.Ref && not (Set.contains c.id ownedIds) then
                        Some c.id
                    else
                        None))
        |> Set.ofList

    /// Projected Nodes for one Workspace package root (not a full Graph).
    let private projectWorkspaceNodes
        (graph: Graph)
        (packageRootId: NodeId)
        : Map<NodeId, Node> =
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
                    Some
                        { node with
                            children = []
                            childrenStatus = Unloaded }
                else
                    let children =
                        node.children
                        |> List.filter (fun c -> Set.contains c.id residentIds)

                    Some
                        { node with
                            children = children
                            childrenStatus = Loaded }

        residentIds
        |> Set.toList
        |> List.choose (fun id ->
            projectNode id |> Option.map (fun node -> id, node))
        |> Map.ofList

    /// Workspace subgraph as a Node list for SyncResponse.packages / LoadResponse.
    let workspaceSubgraphNodes (graph: Graph) (workspaceId: NodeId) : Node list =
        projectWorkspaceNodes graph workspaceId
        |> Map.toList
        |> List.map snd

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
        : Result<Node list, LoadRefuse> =
        let targetIds = targets |> List.map (fun t -> t.targetId)
        if selectionSpansMultipleWorkspaces graph targetIds then
            Error LoadRefuse.MultiWorkspace
        else
            let packageIds =
                targets
                |> List.choose (fun t ->
                    if t.includeWorkspace then Some t.targetId else None)
            match distinctOwningWorkspaces graph packageIds with
            | [ wsId ] -> Ok(workspaceSubgraphNodes graph wsId)
            | _ -> Ok []

    /// Capture LoadResponse fields at one Revision (changes + optional subgraph).
    let captureLoadResponse
        (revision: int)
        (buildEpochSec: int)
        (pageBuildEpochSec: int)
        (isReady: bool)
        (changes: Change list)
        (graph: Graph)
        (targets: LoadTarget list)
        : Result<LoadResponse, LoadRefuse> =
        match packagesForTargets graph targets with
        | Error refuse -> Error refuse
        | Ok packages ->
            Ok
                { revision = revision
                  buildEpochSec = buildEpochSec
                  pageBuildEpochSec = pageBuildEpochSec
                  isReady = isReady
                  changes = changes
                  packages = packages }

    /// Scoped resident graph for fresh-session bootstrap: complete ROOT Workspace,
    /// nested named Workspace headers Unloaded, reachable Ref headers without children.
    let rootBootstrapGraph (graph: Graph) : Graph =
        Graph.fromNodes graph.root (projectWorkspaceNodes graph graph.root)

    /// Extra named Workspace to include when saved zoom lies outside ROOT.
    let private extraZoomWorkspace (graph: Graph) (savedZoom: NodeId option) : NodeId option =
        savedZoom
        |> Option.bind (fun zoomId ->
            if not (Map.containsKey zoomId graph.nodes) then
                None
            else
                match GraphQuery.enclosingWorkspace graph zoomId with
                | Some wsId when wsId <> graph.root -> Some wsId
                | _ -> None)

    /// Merge package nodes into an existing bootstrap graph (Loaded wins over Unloaded headers).
    let private mergePackageNodes
        (baseGraph: Graph)
        (extra: Map<NodeId, Node>)
        : Graph =
        let merged =
            extra
            |> Map.fold
                (fun nodes id node ->
                    match Map.tryFind id nodes with
                    | Some existing when
                        existing.childrenStatus = Loaded
                        && node.childrenStatus = Unloaded ->
                        nodes
                    | _ -> Map.add id node nodes)
                baseGraph.nodes
        Graph.fromNodes baseGraph.root merged

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
                mergePackageNodes rootScoped (projectWorkspaceNodes graph wsId)

    let bootstrapStateResponse
        (scope: BootstrapScope)
        (savedZoom: NodeId option)
        (response: StateResponse)
        : StateResponse =
        { response with
            graph = bootstrapGraph scope savedZoom response.graph }
