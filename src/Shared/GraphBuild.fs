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
        let nodesWithTrash = ensureTrashNode nodesWithWorkspaces
        let pbc, opc = buildParentMaps nodesWithTrash
        let nodesWithOwner = applyOwnerField root opc nodesWithTrash
        { root = root
          nodes = nodesWithOwner
          parentByChild = pbc
          ownerParentByChild = opc }

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
