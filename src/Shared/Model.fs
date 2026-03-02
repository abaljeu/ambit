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
    static member One = Revision 1


type Node =
    { id: NodeId
      text: string
      name: string option
      children: NodeId list }


// defines span of nodes in parent where start <= index in children < end
// and start<end.
type NodeRange = 
    { parent : NodeId
      start: int
      endd : int } 

type Graph =
    { root: NodeId
      nodes: Map<NodeId, Node> }


[<RequireQualifiedAccess>]
module Graph =
    let create () : Graph =
        let rootId = NodeId.New()

        let rootNode: Node = // depth 0 node
            { id = rootId
              text = ""
              name = None
              children = [] }

        { root = rootId
          nodes = Map.ofList [ rootId, rootNode ] }

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
              children = [] }

        let nodes = graph.nodes |> Map.add nodeId node
        { graph with nodes = nodes }, nodeId

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

    let replace
        (parentId: NodeId)
        (index: int)
        (oldIds: NodeId list)
        (newIds: NodeId list)
        (graph: Graph)
        : Result<Graph, string>
        =
        let parentOpt = graph.nodes |> Map.tryFind parentId

        match parentOpt with
        | None -> Error "parent not found"
        | Some parent ->
            let children = parent.children
            let childCount = List.length children
            let oldCount = List.length oldIds

            if index < 0 || index > childCount then
                Error "index out of bounds"
            elif index + oldCount > childCount then
                Error "old span out of bounds"
            elif
                newIds
                |> List.exists (fun nodeId -> not (graph.nodes.ContainsKey nodeId))
            then
                Error "new child not found"
            else
                let existing =
                    children
                    |> List.skip index
                    |> List.take oldCount

                if existing <> oldIds then
                    Error "old span does not match"
                else
                    let prefix = children |> List.take index
                    let suffix = children |> List.skip (index + oldCount)
                    let updatedParent =
                        { parent with
                            children = prefix @ newIds @ suffix }

                    let nodes = graph.nodes |> Map.add parentId updatedParent
                    Ok { graph with nodes = nodes }