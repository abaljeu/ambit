namespace Gambol.Shared

open System

[<Struct>]
type NodeId =
    | NodeId of Guid

    member this.Value =
        let (NodeId value) = this
        value

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


type Node =
    { id         : NodeId
      text       : string
      name       : string option
      children   : ChildNode list
      cssClasses : CssClasses }


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

    /// Build a graph with recomputed parent indexes (use for decode, snapshots, tests).
    let fromNodes (root: NodeId) (nodes: Map<NodeId, Node>) : Graph =
        let pbc, opc = buildParentMaps nodes
        { root = root
          nodes = nodes
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
              cssClasses = CssClass.empty }
        let nodes = graph.nodes |> Map.add nodeId node
        { graph with nodes = nodes }, nodeId

    let create () : Graph =
        let placeholderRoot = NodeId.New()
        let g0 = fromNodes placeholderRoot Map.empty
        let graph, rootId = newNode "" g0
        { graph with root = rootId }

    let setText
        (nodeId: NodeId)
        (oldText: string)
        (newText: string)
        (graph: Graph)
        : Result<Graph, string>
        =
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
        | None -> Error "parent not found"
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
                    let updatedParent =
                        { parent with
                            children = prefix @ newChildren @ suffix }

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
