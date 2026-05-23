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
      name       : string option
      children   : ChildNode list
      cssClasses : CssClasses
      owner      : NodeId
      kind       : NodeKind }


// Span of child indices [start, endd) under graph node `pnode` (parent NodeId).
type NodeRange =
    { pnode: NodeId
      start: int
      endd : int } 

/// One row from node search (Ctrl+F); shared by ViewModelSearch and SearchDialog onPick.
type NodeSearchResult =
    { nodeId: NodeId
      text: string
      name: string option }

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

    /// Initial root node: fixed label, no user-editable fields on root.
    let rootPlaceholder: Node =
        { id = rootId
          text = "ROOT"
          name = None
          children = []
          cssClasses = CssClass.empty
          owner = rootId
          kind = Normal }

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
                      name = None
                      children = []
                      cssClasses = CssClass.empty
                      owner = rootId
                      kind = Special Trash }

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
        let nodesWithTrash = ensureTrashNode nodes
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
              name = None
              children = []
              cssClasses = CssClass.empty
              owner = rootId
              kind = Normal }
        let nodes = graph.nodes |> Map.add nodeId node
        { graph with nodes = nodes }, nodeId

    let create () : Graph =
        fromNodes rootId (Map.ofList [ rootId, rootPlaceholder ])

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
        else
            match graph.nodes |> Map.tryFind nodeId with
            | None -> Error "node not found"
            | Some node ->
                if node.text <> oldText then
                    Error "old text does not match"
                else
                    let updatedNode = { node with text = newText }
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
        else
            match graph.nodes |> Map.tryFind nodeId with
            | None -> Error "node not found"
            | Some node ->
                if node.cssClasses <> oldClasses then
                    Error "old classes do not match"
                else
                    let updatedNode = { node with cssClasses = newClasses }
                    let nodes = graph.nodes |> Map.add nodeId updatedNode
                    Ok { graph with nodes = nodes }

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

                    if parentId = rootId then
                        let hadTrashOwner =
                            children
                            |> List.exists (fun c -> c.id = trashId && c.ref = Ownership.Owner)
                        let hasTrashOwnerAfter =
                            updatedChildren
                            |> List.exists (fun c -> c.id = trashId && c.ref = Ownership.Owner)

                        if hadTrashOwner && not hasTrashOwnerAfter then
                            Error "cannot remove trash owner child from root"
                        elif
                            updatedChildren
                            |> List.filter (fun c -> c.id = trashId && c.ref = Ownership.Owner)
                            |> List.length
                            <> 1
                        then
                            Error "trash must appear exactly once as an Owner child of root"
                        else
                            let updatedParent = { parent with children = updatedChildren }
                            let nodes = graph.nodes |> Map.add parentId updatedParent
                            Ok (fromNodes graph.root nodes)
                    elif
                        updatedChildren
                        |> List.exists (fun c -> c.id = trashId)
                    then
                        Error "trash may not be a child of any non-root parent"
                    else
                        let updatedParent = { parent with children = updatedChildren }
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
