namespace Gambol.Shared

/// Authoritative Sync install: ordered Change tail plus optional resident packages.
type SyncResponse =
    { changes: Change list
      /// Complete Workspace / child-list snapshots at the response revision.
      packages: Node list }

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

    let private isNamedWorkspaceBoundary (rootId: NodeId) (node: Node) : bool =
        match node.kind with
        | Special Workspace when node.id <> rootId -> true
        | _ -> false

    /// Owner-closure of ROOT; stops at nested named Workspace headers (included).
    let private collectRootOwnedIds (graph: Graph) : Set<NodeId> =
        let rec loop (nodeId: NodeId) (visited: Set<NodeId>) =
            if Set.contains nodeId visited then
                visited
            else
                match Map.tryFind nodeId graph.nodes with
                | None -> visited
                | Some node ->
                    let visited' = Set.add nodeId visited
                    if isNamedWorkspaceBoundary graph.root node then
                        visited'
                    else
                        node.children
                        |> List.choose (fun c ->
                            if c.ref = Ownership.Owner then Some c.id else None)
                        |> List.fold (fun visited id -> loop id visited) visited'

        loop graph.root Set.empty

    /// Ref targets from the owned ROOT closure that lie outside that closure.
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

    /// Scoped resident graph for fresh-session bootstrap: complete ROOT Workspace,
    /// nested named Workspace headers Unloaded, reachable Ref headers without children.
    let rootBootstrapGraph (graph: Graph) : Graph =
        let ownedIds = collectRootOwnedIds graph
        let refHeaderIds = collectRefHeaderIds graph ownedIds
        let residentIds = Set.union ownedIds refHeaderIds

        let projectNode (nodeId: NodeId) =
            match Map.tryFind nodeId graph.nodes with
            | None -> None
            | Some node ->
                let headerOnly =
                    Set.contains nodeId refHeaderIds
                    || isNamedWorkspaceBoundary graph.root node

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

        let nodes =
            residentIds
            |> Set.toList
            |> List.choose (fun id ->
                projectNode id |> Option.map (fun node -> id, node))
            |> Map.ofList

        Graph.fromNodes graph.root nodes

    let bootstrapGraph (scope: BootstrapScope) (graph: Graph) : Graph =
        match scope with
        | BootstrapScope.FullGraph -> graph
        | BootstrapScope.RootClosure -> rootBootstrapGraph graph

    let bootstrapStateResponse (scope: BootstrapScope) (response: StateResponse) : StateResponse =
        { response with
            graph = bootstrapGraph scope response.graph }
