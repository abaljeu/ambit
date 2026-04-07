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
      nodes: Map<NodeId, Node> }


[<RequireQualifiedAccess>]
module Graph =

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
        let emptyGraph = { root = NodeId.New(); nodes = Map.empty }
        let graph, rootId = newNode "" emptyGraph
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
                    Ok { graph with nodes = nodes }

    let tryFindParentAndIndex (targetId: NodeId) (graph: Graph) : (NodeId * int) option =
        graph.nodes
        |> Map.toSeq
        |> Seq.tryPick (fun (parentId, parent) ->
            parent.children
            |> List.tryFindIndex (fun child -> child.id = targetId)
            |> Option.map (fun index -> parentId, index))

    /// Parent along the canonical `Ownership.Owner` edge. `None` id -> `None`.
    let owner (graph: Graph) (id: NodeId option) : NodeId option =
        id
        |> Option.bind (fun nid ->
            graph.nodes
            |> Map.toSeq
            |> Seq.tryPick (fun (parentId, parent) ->
                parent.children
                |> List.tryPick (fun child ->
                    if child.id = nid && child.ref = Ownership.Owner then
                        Some parentId
                    else
                        None)))

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
