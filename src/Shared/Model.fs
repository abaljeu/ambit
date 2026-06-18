namespace Gambol.Shared

open System

[<Struct>]
type NodeId =
    | NodeId of Guid

    member this.Value =
        let (NodeId value) = this
        value

    /// Last 8 chars of `Guid.ToString()`; matches DOM `.amb-node-guid` display.
    static member GuidTail8 (guid: Guid) : string =
        let s = guid.ToString()
        if s.Length >= 8 then s.Substring(s.Length - 8) else s

    static member New() = NodeId(Guid.NewGuid())


[<Struct>]
type Revision =
    | Revision of int

    member this.Value =
        let (Revision value) = this
        value

    static member Zero = Revision 0

type Ownership =
    | Ref
    | Owner

// For each id:NodeId exactly one will have ref: Owner.
type ChildNode =
    { ref: Ownership
      id: NodeId }

    static member New() : ChildNode =
        { ref = Ownership.Owner
          id = NodeId.New() }


type SpecialKind =
    | Workspaces
    | Workspace
    | Directory
    | File
    | Trash

type NodeKind =
    | Normal
    | Special of SpecialKind


type Node =
    { id         : NodeId
      text       : string
      name       : Filename
      children   : ChildNode list
      cssClasses : CssClasses
      owner      : NodeId
      kind       : NodeKind
      updateTime : DateTime }


[<RequireQualifiedAccess>]
module NodeUpdateTime =
    /// Canonical nodes and JSON without `updateTime`.
    /// UTC kind so `toDbPrecision` does not shift through PostgreSQL `timestamptz`.
    let missing = DateTime(0L, DateTimeKind.Utc)

    /// PostgreSQL `timestamptz` stores microseconds; align before DB round-trip.
    let private ticksPerMicrosecond = 10L

    let toDbPrecision (time: DateTime) : DateTime =
        let utc =
            match time.Kind with
            | DateTimeKind.Utc -> time
            | DateTimeKind.Local -> time.ToUniversalTime()
            | DateTimeKind.Unspecified ->
                // PostgreSQL `timestamptz` via Npgsql/Dapper: UTC clock, Unspecified kind.
                DateTime.SpecifyKind(time, DateTimeKind.Utc)
            | _ -> time.ToUniversalTime()
        DateTime(utc.Ticks - utc.Ticks % ticksPerMicrosecond, DateTimeKind.Utc)

    let now () = DateTime.UtcNow |> toDbPrecision

    let touch (node: Node) : Node = { node with updateTime = now () }


// Span of child indices [start, endd) under graph node `pnode` (parent NodeId).
type NodeRange =
    { pnode: NodeId
      start: int
      endd : int } 

/// One row from node search (Ctrl+F); shared by ViewModelSearch and SearchDialog onPick.
type NodeSearchResult =
    { nodeId: NodeId
      text: string
      name: Filename }

type Graph =
    { root: NodeId
      nodes: Map<NodeId, Node>
      /// Child id -> structural parent and index (min parent NodeId wins when shared).
      parentByChild: Map<NodeId, NodeId * int>
      /// Child id -> graph parent along the single Ownership.Owner edge.
      ownerParentByChild: Map<NodeId, NodeId> }


[<RequireQualifiedAccess>]
module Graph =

    /// Canonical document root; stable across snapshot load and replay (not stored in outline).
    let rootId: NodeId = NodeId Guid.Empty

    /// Canonical trash node id; stable across snapshot load and replay.
    let trashId: NodeId = NodeId(Guid.Parse "00000000-0000-0000-0000-000000000001")

    /// Canonical workspaces node id; stable across snapshot load and replay.
    let workspacesId: NodeId = NodeId(Guid.Parse "00000000-0000-0000-0000-000000000002")

    /// Initial root node: fixed label, no user-editable fields on root.
    let rootPlaceholder: Node =
        { id = rootId
          text = "ROOT"
          name = Filename.Empty
          children = []
          cssClasses = CssClass.empty
          owner = rootId
          kind = Special Workspace
          updateTime = NodeUpdateTime.missing }

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
                    { id = trashId
                      text = "Trash"
                      name = Filename.Empty
                      children = []
                      cssClasses = CssClass.empty
                      owner = rootId
                      kind = Special Trash
                      updateTime = NodeUpdateTime.missing }

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

        // If a Trash node exists but is not an Owner child of ROOT, fix it up.
        let rootNode = nodesWithTrash.[rootId]

        let rootChildrenWithoutTrash, trashOccurrences =
            rootNode.children
            |> List.partition (fun c -> c.id <> trashId)

        let trashChild =
            match trashOccurrences |> List.tryFind (fun c -> c.ref = Ownership.Owner) with
            | Some ownerChild -> ownerChild
            | None ->
                { ref = Ownership.Owner
                  id = trashId }

        let fixedRootChildren = rootChildrenWithoutTrash @ [ trashChild ]

        nodesWithTrash
        |> Map.add rootId { rootNode with children = fixedRootChildren }

    let private ensureWorkspacesNode (nodes: Map<NodeId, Node>) : Map<NodeId, Node> =
        let hasWorkspaces = Map.containsKey workspacesId nodes

        let nodesWithWorkspaces =
            if hasWorkspaces then
                nodes
            else
                let rootNode = nodes.[rootId]

                let workspacesNode: Node =
                    { id = workspacesId
                      text = "Workspaces"
                      name = Filename.Empty
                      children = []
                      cssClasses = CssClass.empty
                      owner = rootId
                      kind = Special Workspaces
                      updateTime = NodeUpdateTime.missing }

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

        let withoutWorkspaces, workspacesOccurrences =
            rootNode.children |> List.partition (fun c -> c.id <> workspacesId)

        let workspacesChild =
            match workspacesOccurrences |> List.tryFind (fun c -> c.ref = Ownership.Owner) with
            | Some ownerChild -> ownerChild
            | None ->
                { ref = Ownership.Owner
                  id = workspacesId }

        let beforeTrash, afterTrashStart =
            match withoutWorkspaces |> List.tryFindIndex (fun c -> c.id = trashId) with
            | Some i ->
                withoutWorkspaces |> List.take i,
                withoutWorkspaces |> List.skip i
            | None ->
                withoutWorkspaces, []

        let fixedRootChildren = beforeTrash @ [ workspacesChild ] @ afterTrashStart

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
        let node: Node =
            { id = nodeId
              text = text
              name = Filename.Empty
              children = []
              cssClasses = CssClass.empty
              owner = rootId
              kind = Normal
              updateTime = NodeUpdateTime.now () }
        let nodes = graph.nodes |> Map.add nodeId node
        { graph with nodes = nodes }, nodeId

    let create () : Graph =
        fromNodes rootId (Map.ofList [ rootId, rootPlaceholder ])

    let fileTreeInsertIndex (graph: Graph) (parentId: NodeId) : int =
        if parentId <> rootId then
            graph.nodes.[parentId].children.Length
        else
            graph.nodes.[parentId].children
            |> List.tryFindIndex (fun c -> c.id = workspacesId || c.id = trashId)
            |> Option.defaultValue (graph.nodes.[parentId].children.Length)

    let setText
        (nodeId: NodeId)
        (oldText: string)
        (newText: string)
        (graph: Graph)
        : Result<Graph, string>
        =
        if nodeId = rootId then
            Error "cannot modify canonical root text"
        elif nodeId = trashId then
            Error "cannot modify trash node text"
        elif nodeId = workspacesId then
            Error "cannot modify workspaces node text"
        else
            match graph.nodes |> Map.tryFind nodeId with
            | None -> Error "node not found"
            | Some node ->
                if node.text <> oldText then
                    Error "old text does not match"
                else
                    let updatedNode = NodeUpdateTime.touch { node with text = newText }
                    let nodes = graph.nodes |> Map.add nodeId updatedNode
                    Ok { graph with nodes = nodes }

    let setClasses
        (nodeId: NodeId)
        (oldClasses: CssClasses)
        (newClasses: CssClasses)
        (graph: Graph)
        : Result<Graph, string>
        =
        if nodeId = rootId then
            Error "cannot set classes on canonical root"
        elif nodeId = trashId then
            Error "cannot set classes on trash node"
        elif nodeId = workspacesId then
            Error "cannot set classes on workspaces node"
        else
            match graph.nodes |> Map.tryFind nodeId with
            | None -> Error "node not found"
            | Some node ->
                if node.cssClasses <> oldClasses then
                    Error "old classes do not match"
                else
                    let updatedNode = NodeUpdateTime.touch { node with cssClasses = newClasses }
                    let nodes = graph.nodes |> Map.add nodeId updatedNode
                    Ok { graph with nodes = nodes }

    let setName
        (nodeId: NodeId)
        (oldName: string)
        (newName: string)
        (graph: Graph)
        : Result<Graph, string>
        =
        if nodeId = rootId then
            Error "cannot modify canonical root name"
        elif nodeId = trashId then
            Error "cannot modify trash node name"
        elif nodeId = workspacesId then
            Error "cannot modify workspaces node name"
        else
            match graph.nodes |> Map.tryFind nodeId with
            | None -> Error "node not found"
            | Some node ->
                if node.name <> Filename.Ok oldName then
                    Error "old name does not match"
                else
                    match Filename.create newName with
                    | Filename.Invalid _ | Filename.Empty ->
                        Error "new name is not a valid filename"
                    | Filename.Ok _ ->
                        let newNameLower = newName.ToLowerInvariant()
                        let hasConflict =
                            match graph.ownerParentByChild |> Map.tryFind nodeId with
                            | None -> false
                            | Some parentId ->
                                graph.nodes.[parentId].children
                                |> List.exists (fun c ->
                                    c.ref = Ownership.Owner
                                    && c.id <> nodeId
                                    && (graph.nodes
                                        |> Map.tryFind c.id
                                        |> Option.bind (fun s ->
                                            match s.name with
                                            | Filename.Ok n -> Some(n.ToLowerInvariant())
                                            | _ -> None)
                                        |> Option.map (fun n -> n = newNameLower)
                                        |> Option.defaultValue false))
                        if hasConflict then
                            Error "sibling name conflict"
                        else
                            let updatedNode =
                                NodeUpdateTime.touch
                                    { node with name = Filename.Ok newName; text = newName }
                            Ok { graph with nodes = graph.nodes |> Map.add nodeId updatedNode }

    let replace
        (parentId: NodeId)
        (index: int)
        (oldChildren: ChildNode list)
        (newChildren: ChildNode list)
        (graph: Graph)
        : Result<Graph, string>
        =
        let parentOpt = graph.nodes |> Map.tryFind parentId

        match parentOpt with
        | None -> Error $"parent not found {NodeId.GuidTail8 parentId.Value}"
        | Some parent ->
            let children = parent.children
            let childCount = List.length children
            let oldCount = List.length oldChildren

            if index < 0 || index > childCount then
                Error "index out of bounds"
            elif index + oldCount > childCount then
                Error "old span out of bounds"
            elif
                newChildren
                |> List.exists (fun child -> not (graph.nodes.ContainsKey child.id))
            then
                Error "new child not found"
            else
                let placementError =
                    newChildren
                    |> List.tryPick (fun child ->
                        let childNode = graph.nodes.[child.id]

                        match childNode.kind with
                        | Special Workspace when parentId <> workspacesId ->
                            Some "Workspace nodes may only be placed under Workspaces"
                        | _ -> None)

                match placementError with
                | Some msg -> Error msg
                | None ->
                    let existing =
                        children
                        |> List.skip index
                        |> List.take oldCount

                    if existing <> oldChildren then
                        Error "old span does not match"
                    else
                        let prefix = children |> List.take index
                        let suffix = children |> List.skip (index + oldCount)
                        let updatedChildren = prefix @ newChildren @ suffix

                        let ownerNames =
                            updatedChildren
                            |> List.choose (fun c ->
                                if c.ref = Ownership.Owner then
                                    graph.nodes
                                    |> Map.tryFind c.id
                                    |> Option.bind (fun n ->
                                        match n.name with
                                        | Filename.Ok s -> Some(s.ToLowerInvariant())
                                        | _ -> None)
                                else None)

                        if ownerNames.Length <> (ownerNames |> List.distinct).Length then
                            Error "sibling name conflict"
                        elif parentId = rootId then
                            let hadTrashOwner =
                                children
                                |> List.exists (fun c -> c.id = trashId && c.ref = Ownership.Owner)
                            let hasTrashOwnerAfter =
                                updatedChildren
                                |> List.exists (fun c -> c.id = trashId && c.ref = Ownership.Owner)
                            let hadWorkspacesOwner =
                                children
                                |> List.exists (fun c -> c.id = workspacesId && c.ref = Ownership.Owner)
                            let hasWorkspacesOwnerAfter =
                                updatedChildren
                                |> List.exists (fun c -> c.id = workspacesId && c.ref = Ownership.Owner)

                            if hadTrashOwner && not hasTrashOwnerAfter then
                                Error "cannot remove trash owner child from root"
                            elif hadWorkspacesOwner && not hasWorkspacesOwnerAfter then
                                Error "cannot remove workspaces owner child from root"
                            elif
                                updatedChildren
                                |> List.filter (fun c -> c.id = trashId && c.ref = Ownership.Owner)
                                |> List.length
                                <> 1
                            then
                                Error "trash must appear exactly once as an Owner child of root"
                            elif
                                updatedChildren
                                |> List.filter (fun c -> c.id = workspacesId && c.ref = Ownership.Owner)
                                |> List.length
                                <> 1
                            then
                                Error "workspaces must appear exactly once as an Owner child of root"
                            else
                                let updatedParent =
                                    NodeUpdateTime.touch { parent with children = updatedChildren }
                                let nodes = graph.nodes |> Map.add parentId updatedParent
                                Ok (fromNodes graph.root nodes)
                        elif
                            updatedChildren
                            |> List.exists (fun c -> c.id = trashId || c.id = workspacesId)
                        then
                            Error "trash and workspaces may not be children of any non-root parent"
                        else
                            let updatedParent =
                                NodeUpdateTime.touch { parent with children = updatedChildren }
                            let nodes = graph.nodes |> Map.add parentId updatedParent
                            Ok (fromNodes graph.root nodes)

    let tryFindParentAndIndex (targetId: NodeId) (graph: Graph) : (NodeId * int) option =
        Map.tryFind targetId graph.parentByChild

    /// Parent along the canonical `Ownership.Owner` edge. `None` id -> `None`.
    let owner (graph: Graph) (id: NodeId option) : NodeId option =
        id |> Option.bind (fun nid -> Map.tryFind nid graph.ownerParentByChild)

    let nodeFirstChild (graph: Graph) (id: NodeId option) : NodeId option =
        id
        |> Option.bind (fun nid ->
            Map.tryFind nid graph.nodes
            |> Option.bind (fun node ->
                node.children |> List.tryHead |> Option.map (fun c -> c.id)))

    let nodeLastChild (graph: Graph) (id: NodeId option) : NodeId option =
        id
        |> Option.bind (fun nid ->
            Map.tryFind nid graph.nodes
            |> Option.bind (fun node ->
                let n = List.length node.children
                if n = 0 then
                    None
                else
                    List.tryItem (n - 1) node.children |> Option.map (fun c -> c.id)))

    /// Insert position as the last child of nodeId.
    let makeNodeRangeForInsertingUnder (nodeId: NodeId) (graph: Graph) : NodeRange option =
        match Map.tryFind nodeId graph.nodes with
        | None -> None
        | Some node ->
            let childCount = List.length node.children
            Some { pnode = nodeId; start = childCount; endd = childCount }

/// Carries a fixed `Graph` and current `NodeId`; steps compose like `SiteNav`.
type NodeNav = NodeNav of Graph * NodeId option

[<RequireQualifiedAccess>]
module Node =
    let at (graph: Graph) (id: NodeId option) : NodeNav = NodeNav(graph, id)
    let current (NodeNav(_, id)) : NodeId option = id

    let private step f (NodeNav(g, id)) = NodeNav(g, f g id)

    let owner = step Graph.owner
    let firstChild = step Graph.nodeFirstChild
    let lastChild = step Graph.nodeLastChild
