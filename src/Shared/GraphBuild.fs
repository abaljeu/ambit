namespace Gambol.Shared

open System

/// Graph construction: canonical ids, ensure*, fromNodes, create.
module GraphBuild =

    /// Canonical document root; stable across snapshot load and replay (not stored in outline).
    let rootId: NodeId = NodeId Guid.Empty

    /// Canonical trash node id; stable across snapshot load and replay.
    let trashId: NodeId = NodeId(Guid.Parse "00000000-0000-0000-0000-000000000001")

    /// Canonical workspaces node id; stable across snapshot load and replay.
    let workspacesId: NodeId = NodeId(Guid.Parse "00000000-0000-0000-0000-000000000002")

    /// Canonical system node id; stable across snapshot load and replay.
    let systemId: NodeId = NodeId(Guid.Parse "00000000-0000-0000-0000-000000000003")

    /// Workspaces, SYSTEM, and TRASH — fixed system folder nodes under ROOT (id, error label).
    let systemFolderNodes: (NodeId * string) list =
        [ trashId, "trash"
          workspacesId, "workspaces"
          systemId, "system" ]

    /// True for Workspaces, SYSTEM, or TRASH.
    let isSystemFolderNode (nodeId: NodeId) : bool =
        systemFolderNodes |> List.exists (fun (id, _) -> id = nodeId)

    /// TRASH and SYSTEM — Directory-kind system folders (excludes the Workspaces container).
    let isSystemDirectoryNode (nodeId: NodeId) : bool =
        nodeId = trashId || nodeId = systemId

    /// ROOT, TRASH, and SYSTEM — fixed document roots under the shared data directory
    /// (not named-workspace folders; Workspaces is excluded).
    let isCanonicalDataRoot (nodeId: NodeId) : bool =
        nodeId = rootId || isSystemDirectoryNode nodeId

    /// Any fixed bootstrap id: ROOT or a system folder node.
    let isCanonicalNode (nodeId: NodeId) : bool =
        nodeId = rootId || isSystemFolderNode nodeId

    /// Initial root node: fixed label, no user-editable fields on root.
    let rootPlaceholder: Node =
        Node.Create(rootId, text = "ROOT", kind = Special Workspace)

    let private addStructuralEdges (parentId: NodeId) (parent: Node) acc =
        parent.children
        |> List.mapi (fun i c -> i, c.id)
        |> List.fold
            (fun a (i, cid) ->
                if Map.containsKey cid a then a else Map.add cid (parentId, i) a)
            acc

    let private addOwnerEdges (parentId: NodeId) (parent: Node) acc =
        parent.children
        |> List.fold
            (fun a child ->
                match child.ref with
                | Ownership.Owner -> Map.add child.id parentId a
                | Ownership.Ref -> a)
            acc

    let private buildParentMaps (nodes: Map<NodeId, Node>) =
        let structural =
            nodes |> Map.fold (fun acc pid p -> addStructuralEdges pid p acc) Map.empty
        let owners =
            nodes |> Map.fold (fun acc pid p -> addOwnerEdges pid p acc) Map.empty
        structural, owners

    let private ensureTrashNode (nodes: Map<NodeId, Node>) : Map<NodeId, Node> =
        let hasTrash = Map.containsKey trashId nodes
        let nodesWithTrash =
            if hasTrash then
                nodes
            else
                let rootNode = nodes.[rootId]
                let trashNode: Node =
                    Node.Create(
                        trashId,
                        text = "Trash",
                        name = Filename.Ok "TRASH",
                        kind = Special Directory)

                let trashChild: ChildNode =
                    { ref = Ownership.Owner
                      id = trashId }

                let rootChildren =
                    if rootNode.children |> List.exists (fun c -> c.id = trashId) then
                        rootNode.children
                    else
                        rootNode.children @ [ trashChild ]

                nodes
                |> Map.add rootId { rootNode with children = rootChildren }
                |> Map.add trashId trashNode

        let rootNode = nodesWithTrash.[rootId]
        let hasTrashOwner =
            rootNode.children
            |> List.exists (fun c -> c.id = trashId && c.ref = Ownership.Owner)

        let nodesFixed =
            if hasTrashOwner then
                nodesWithTrash
            else
                // Missing Owner under ROOT — append (repair load / partial graphs).
                let trashChild = { ref = Ownership.Owner; id = trashId }
                let withoutTrash =
                    rootNode.children |> List.filter (fun c -> c.id <> trashId)
                nodesWithTrash
                |> Map.add rootId
                    { rootNode with children = withoutTrash @ [ trashChild ] }

        match Map.tryFind trashId nodesFixed with
        | None -> nodesFixed
        | Some trash ->
            nodesFixed
            |> Map.add trashId
                { trash with
                    kind = Special Directory
                    name = Filename.Ok "TRASH" }

    let private ensureWorkspacesNode (nodes: Map<NodeId, Node>) : Map<NodeId, Node> =
        let hasWorkspaces = Map.containsKey workspacesId nodes

        let nodesWithWorkspaces =
            if hasWorkspaces then
                nodes
            else
                let rootNode = nodes.[rootId]

                let workspacesNode: Node =
                    Node.Create(
                        workspacesId,
                        text = "Workspaces",
                        kind = Special Workspaces)

                let workspacesChild: ChildNode =
                    { ref = Ownership.Owner
                      id = workspacesId }

                let rootChildren =
                    if rootNode.children |> List.exists (fun c -> c.id = workspacesId) then
                        rootNode.children
                    else
                        rootNode.children @ [ workspacesChild ]

                nodes
                |> Map.add rootId { rootNode with children = rootChildren }
                |> Map.add workspacesId workspacesNode

        let rootNode = nodesWithWorkspaces.[rootId]
        let hasWorkspacesOwner =
            rootNode.children
            |> List.exists (fun c ->
                c.id = workspacesId && c.ref = Ownership.Owner)

        if hasWorkspacesOwner then
            nodesWithWorkspaces
        else
            // Missing Owner under ROOT — insert before TRASH when present.
            let workspacesChild = { ref = Ownership.Owner; id = workspacesId }
            let withoutWorkspaces =
                rootNode.children |> List.filter (fun c -> c.id <> workspacesId)
            let beforeTrash, afterTrashStart =
                match withoutWorkspaces |> List.tryFindIndex (fun c -> c.id = trashId) with
                | Some i ->
                    withoutWorkspaces |> List.take i,
                    withoutWorkspaces |> List.skip i
                | None ->
                    withoutWorkspaces, []
            let fixedRootChildren =
                beforeTrash @ [ workspacesChild ] @ afterTrashStart
            nodesWithWorkspaces
            |> Map.add rootId { rootNode with children = fixedRootChildren }

    let private ensureSystemNode (nodes: Map<NodeId, Node>) : Map<NodeId, Node> =
        let hasSystem = Map.containsKey systemId nodes
        let nodesWithSystem =
            if hasSystem then
                nodes
            else
                let rootNode = nodes.[rootId]
                let systemNode: Node =
                    Node.Create(
                        systemId,
                        text = "System",
                        name = Filename.Ok "SYSTEM",
                        kind = Special Directory)

                let systemChild: ChildNode =
                    { ref = Ownership.Owner
                      id = systemId }

                let rootChildren =
                    if rootNode.children |> List.exists (fun c -> c.id = systemId) then
                        rootNode.children
                    else
                        rootNode.children @ [ systemChild ]

                nodes
                |> Map.add rootId { rootNode with children = rootChildren }
                |> Map.add systemId systemNode

        let rootNode = nodesWithSystem.[rootId]
        let hasSystemOwner =
            rootNode.children
            |> List.exists (fun c -> c.id = systemId && c.ref = Ownership.Owner)

        let nodesFixed =
            if hasSystemOwner then
                nodesWithSystem
            else
                // Missing Owner under ROOT — insert before TRASH when present.
                let systemChild = { ref = Ownership.Owner; id = systemId }
                let withoutSystem =
                    rootNode.children |> List.filter (fun c -> c.id <> systemId)
                let beforeTrash, afterTrashStart =
                    match withoutSystem |> List.tryFindIndex (fun c -> c.id = trashId) with
                    | Some i ->
                        withoutSystem |> List.take i,
                        withoutSystem |> List.skip i
                    | None ->
                        withoutSystem, []
                let fixedRootChildren =
                    beforeTrash @ [ systemChild ] @ afterTrashStart
                nodesWithSystem
                |> Map.add rootId { rootNode with children = fixedRootChildren }

        match Map.tryFind systemId nodesFixed with
        | None -> nodesFixed
        | Some system ->
            nodesFixed
            |> Map.add systemId
                { system with
                    kind = Special Directory
                    name = Filename.Ok "SYSTEM" }

    let private ensureRootKind (nodes: Map<NodeId, Node>) : Map<NodeId, Node> =
        match Map.tryFind rootId nodes with
        | None -> nodes
        | Some rootNode ->
            match rootNode.kind with
            | Special Workspace -> nodes
            | _ -> nodes |> Map.add rootId { rootNode with kind = Special Workspace }

    let private applyOwnerField
        (root: NodeId)
        (ownerParentByChild: Map<NodeId, NodeId>)
        (nodes: Map<NodeId, Node>)
        : Map<NodeId, Node>
        =
        nodes
        |> Map.map (fun nid node ->
            if nid = root then
                { node with owner = root }
            else
                let ownerParent =
                    ownerParentByChild
                    |> Map.tryFind nid
                    |> Option.defaultValue root
                { node with owner = ownerParent })

    /// Build a graph with recomputed parent indexes (use for decode, snapshots, tests).
    let fromNodes (root: NodeId) (nodes: Map<NodeId, Node>) : Graph =
        let nodesWithRoot = ensureRootKind nodes
        let nodesWithWorkspaces = ensureWorkspacesNode nodesWithRoot
        let nodesWithSystem = ensureSystemNode nodesWithWorkspaces
        let nodesWithTrash = ensureTrashNode nodesWithSystem
        let pbc, opc = buildParentMaps nodesWithTrash
        let nodesWithOwner = applyOwnerField root opc nodesWithTrash
        { root = root
          nodes = nodesWithOwner
          parentByChild = pbc
          ownerParentByChild = opc }

    /// Insert a fresh, childless, not-yet-attached node. Such a node contributes no
    /// parent edges, so the indexes are unchanged and bulk inserts (a parse tail is
    /// thousands of NewNode ops) avoid a whole-graph rebuild per op.
    let addDetachedNode (node: Node) (graph: Graph) : Graph =
        if Map.containsKey node.id graph.nodes then
            fromNodes graph.root (graph.nodes |> Map.add node.id node)
        else
            { graph with
                nodes = graph.nodes |> Map.add node.id { node with owner = graph.root } }

    /// Index update for children appended at the end of a parent's list. Nothing is
    /// removed and no sibling index shifts, so every existing edge stays valid and only
    /// the appended children need entries — a parse tail is a long run of such appends,
    /// and rebuilding the whole graph for each one is quadratic.
    /// `parentByChild` keeps the lowest-keyed parent and `ownerParentByChild` the
    /// highest-keyed owner, matching the fold order `fromNodes` uses.
    let appendChildren
        (parentId: NodeId)
        (appended: ChildNode list)
        (updatedParent: Node)
        (graph: Graph)
        : Graph =
        let firstIndex = updatedParent.children.Length - appended.Length
        let parentByChild =
            appended
            |> List.indexed
            |> List.fold
                (fun acc (i, child) ->
                    match Map.tryFind child.id acc with
                    | Some(existing, _) when existing <= parentId -> acc
                    | _ -> Map.add child.id (parentId, firstIndex + i) acc)
                graph.parentByChild
        let ownerParentByChild =
            appended
            |> List.fold
                (fun acc child ->
                    match child.ref with
                    | Ownership.Ref -> acc
                    | Ownership.Owner ->
                        match Map.tryFind child.id acc with
                        | Some existing when existing >= parentId -> acc
                        | _ -> Map.add child.id parentId acc)
                graph.ownerParentByChild
        let nodes =
            appended
            |> List.fold
                (fun (acc: Map<NodeId, Node>) child ->
                    let owner =
                        Map.tryFind child.id ownerParentByChild
                        |> Option.defaultValue graph.root
                    match Map.tryFind child.id acc with
                    | Some node when node.owner <> owner ->
                        Map.add child.id { node with owner = owner } acc
                    | _ -> acc)
                (graph.nodes |> Map.add parentId updatedParent)
        { root = graph.root
          nodes = nodes
          parentByChild = parentByChild
          ownerParentByChild = ownerParentByChild }

    let nodeCount (graph: Graph) =
        graph.nodes.Count

    let contains (nodeId: NodeId) (graph: Graph) =
        graph.nodes.ContainsKey nodeId

    let newNode (text: string) (graph: Graph) : Graph * NodeId =
        let nodeId = NodeId.New()
        let node =
            Node.Create(nodeId, text = text, updateTime = NodeUpdateTime.now ())
        let nodes = graph.nodes |> Map.add nodeId node
        { graph with nodes = nodes }, nodeId

    let create () : Graph =
        fromNodes rootId (Map.ofList [ rootId, rootPlaceholder ])
