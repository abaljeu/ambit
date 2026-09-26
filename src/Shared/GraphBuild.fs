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

    /// True when a Special node is an owned child of SYSTEM (not SYSTEM itself).
    let isSpecialSystemDirectoryMember (graph: Graph) (nodeId: NodeId) : bool =
        Map.tryFind nodeId graph.ownerParentByChild = Some systemId
        && match Map.tryFind nodeId graph.nodes with
           | Some { kind = Special _ } -> true
           | _ -> false

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

    let tryGetChildren (graph: Graph) (id: NodeId) =
        GraphChildren.tryGet graph id

    let getChildren (graph: Graph) (id: NodeId) = GraphChildren.get graph id

    let isLoaded (graph: Graph) (id: NodeId) = GraphChildren.isLoaded graph id

    let childrenStatus (graph: Graph) (id: NodeId) =
        GraphChildren.status graph id

    let private kidsOf
        (childMap: Map<NodeId, ChildNode list>)
        (parentId: NodeId)
        : ChildNode list =
        Map.tryFind parentId childMap |> Option.defaultValue []

    let private addStructuralEdges parentId (kids: ChildNode list) acc =
        kids
        |> List.mapi (fun i c -> i, c.id)
        |> List.fold
            (fun a (i, cid) ->
                if Map.containsKey cid a then a else Map.add cid (parentId, i) a)
            acc

    let private addOwnerEdges parentId (kids: ChildNode list) acc =
        // Edge.ref is the write-side source until ChildNode is removed (Phase C).
        // Do not use Node.childOwnership here: fromNodes applies owner fields after
        // these maps are built.
        kids
        |> List.fold
            (fun a child ->
                match child.ref with
                | Ownership.Owner -> Map.add child.id parentId a
                | Ownership.Ref -> a)
            acc

    let private buildParentMaps (childMap: Map<NodeId, ChildNode list>) =
        let structural =
            childMap
            |> Map.fold (fun acc pid kids -> addStructuralEdges pid kids acc) Map.empty
        let owners =
            childMap
            |> Map.fold (fun acc pid kids -> addOwnerEdges pid kids acc) Map.empty
        structural, owners

    let private withLoadedEmpty
        (id: NodeId)
        (childMap: Map<NodeId, ChildNode list>)
        =
        if Map.containsKey id childMap then
            childMap
        else
            Map.add id [] childMap

    let private hasOwnerChild parentId childId childMap =
        kidsOf childMap parentId
        |> List.exists (fun c -> c.id = childId && c.ref = Ownership.Owner)

    let private insertOwnerBeforeTrash parentId childId childMap =
        let child = ChildNode.owner childId
        let without =
            kidsOf childMap parentId |> List.filter (fun c -> c.id <> childId)
        let beforeTrash, afterTrash =
            match without |> List.tryFindIndex (fun c -> c.id = trashId) with
            | Some i -> List.take i without, List.skip i without
            | None -> without, []
        Map.add parentId (beforeTrash @ [ child ] @ afterTrash) childMap

    let private appendOwnerChild parentId childId childMap =
        let child = ChildNode.owner childId
        let kids = kidsOf childMap parentId
        if kids |> List.exists (fun c -> c.id = childId) then
            Map.add parentId kids childMap
        else
            Map.add parentId (kids @ [ child ]) childMap

    let private ensureTrashNode nodes childMap =
        let trashNode =
            Node.Create(
                trashId,
                text = "Trash",
                name = Filename.Ok "TRASH",
                kind = Special Directory)
        let nodes, childMap =
            if Map.containsKey trashId nodes then
                nodes, childMap
            else
                nodes |> Map.add trashId trashNode,
                childMap
                |> withLoadedEmpty trashId
                |> appendOwnerChild rootId trashId
        let childMap =
            if hasOwnerChild rootId trashId childMap then
                childMap
            else
                let without =
                    kidsOf childMap rootId
                    |> List.filter (fun c -> c.id <> trashId)
                Map.add
                    rootId
                    (without @ [ ChildNode.owner trashId ])
                    childMap
        match Map.tryFind trashId nodes with
        | None -> nodes, childMap
        | Some trash ->
            nodes
            |> Map.add
                trashId
                { trash with
                    kind = Special Directory
                    name = Filename.Ok "TRASH" },
            childMap

    let private ensureWorkspacesNode nodes childMap =
        let workspacesNode =
            Node.Create(
                workspacesId,
                text = "Workspaces",
                kind = Special Workspaces)
        let nodes, childMap =
            if Map.containsKey workspacesId nodes then
                nodes, childMap
            else
                nodes |> Map.add workspacesId workspacesNode,
                childMap
                |> withLoadedEmpty workspacesId
                |> appendOwnerChild rootId workspacesId
        if hasOwnerChild rootId workspacesId childMap then
            nodes, childMap
        else
            nodes, insertOwnerBeforeTrash rootId workspacesId childMap

    let private ensureSystemNode nodes childMap =
        let systemNode =
            Node.Create(
                systemId,
                text = "System",
                name = Filename.Ok "SYSTEM",
                kind = Special Directory)
        let nodes, childMap =
            if Map.containsKey systemId nodes then
                nodes, childMap
            else
                nodes |> Map.add systemId systemNode,
                childMap
                |> withLoadedEmpty systemId
                |> appendOwnerChild rootId systemId
        let childMap =
            if hasOwnerChild rootId systemId childMap then
                childMap
            else
                insertOwnerBeforeTrash rootId systemId childMap
        match Map.tryFind systemId nodes with
        | None -> nodes, childMap
        | Some system ->
            nodes
            |> Map.add
                systemId
                { system with
                    kind = Special Directory
                    name = Filename.Ok "SYSTEM" },
            childMap

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
                    |> Option.defaultValue node.owner
                { node with owner = ownerParent })

    /// Build a graph with recomputed parent indexes (use for decode, snapshots, tests).
    /// `childMap` keys are Loaded lists; absent parent ids stay Unloaded.
    let fromNodes
        (root: NodeId)
        (nodes: Map<NodeId, Node>)
        (childMap: Map<NodeId, ChildNode list>)
        : Graph =
        let nodesWithRoot = ensureRootKind nodes
        let nodesW, childW = ensureWorkspacesNode nodesWithRoot childMap
        let nodesS, childS = ensureSystemNode nodesW childW
        let nodesT, childT = ensureTrashNode nodesS childS
        let pbc, opc = buildParentMaps childT
        let nodesWithOwner = applyOwnerField root opc nodesT
        { root = root
          nodes = nodesWithOwner
          childMap = childT
          parentByChild = pbc
          ownerParentByChild = opc
          focus = None }

    /// Rebuild indexes from an existing graph's nodes and childMap.
    let reindex (graph: Graph) : Graph =
        fromNodes graph.root graph.nodes graph.childMap

    /// Build a Graph from an extracted node set without injecting canonical folders.
    let fromExtracted
        (root: NodeId)
        (nodes: Map<NodeId, Node>)
        (childMap: Map<NodeId, ChildNode list>)
        : Graph =
        let pbc, opc = buildParentMaps childMap
        let nodesWithOwner = applyOwnerField root opc nodes
        { root = root
          nodes = nodesWithOwner
          childMap = childMap
          parentByChild = pbc
          ownerParentByChild = opc
          focus = None }

    /// Insert a fresh, childless, not-yet-attached node. Such a node contributes no
    /// parent edges, so the indexes are unchanged and bulk inserts (a parse tail is
    /// thousands of NewNode ops) avoid a whole-graph rebuild per op.
    /// New nodes are Loaded empty (`childMap` key present with []).
    let addDetachedNode (node: Node) (graph: Graph) : Graph =
        if Map.containsKey node.id graph.nodes then
            fromNodes
                graph.root
                (graph.nodes |> Map.add node.id node)
                graph.childMap
        else
            { graph with
                nodes =
                    graph.nodes
                    |> Map.add node.id { node with owner = graph.root }
                childMap = graph.childMap |> Map.add node.id [] }

    /// Index update for children appended at the end of a parent's list. Nothing is
    /// removed and no sibling index shifts, so every existing edge stays valid and only
    /// the appended children need entries — a parse tail is a long run of such appends,
    /// and rebuilding the whole graph for each one is quadratic.
    /// `parentByChild` keeps the lowest-keyed parent and `ownerParentByChild` the
    /// highest-keyed owner, matching the fold order `fromNodes` uses.
    let appendChildren
        (parentId: NodeId)
        (appended: ChildNode list)
        (updatedChildren: ChildNode list)
        (updatedParent: Node)
        (graph: Graph)
        : Graph =
        let firstIndex = updatedChildren.Length - appended.Length
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
          childMap = Map.add parentId updatedChildren graph.childMap
          parentByChild = parentByChild
          ownerParentByChild = ownerParentByChild
          focus = graph.focus }

    let nodeCount (graph: Graph) =
        graph.nodes.Count

    let contains (nodeId: NodeId) (graph: Graph) =
        graph.nodes.ContainsKey nodeId

    let newNode (text: string) (graph: Graph) : Graph * NodeId =
        let nodeId = NodeId.New()
        let node =
            Node.Create(nodeId, text = text, updateTime = NodeUpdateTime.now ())
        { graph with
            nodes = graph.nodes |> Map.add nodeId node
            childMap = graph.childMap |> Map.add nodeId [] },
        nodeId

    let create () : Graph =
        fromNodes
            rootId
            (Map.ofList [ rootId, rootPlaceholder ])
            (Map.ofList [ rootId, [] ])
