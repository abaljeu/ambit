module GraphChildMapHelpers

open Gambol.Shared

/// Rebuild with `parentId` Loaded to `kids` (including `[]`).
let setChildren parentId kids (graph: Graph) =
    Graph.fromNodes
        graph.root
        graph.nodes
        (Map.add parentId kids graph.childMap)

/// Rebuild with `parentId` Unloaded (key absent).
let unload parentId (graph: Graph) =
    Graph.fromNodes
        graph.root
        graph.nodes
        (Map.remove parentId graph.childMap)

/// Attach each node as Loaded empty without changing parent lists.
let addDetachedMany (nodes: Node list) (graph: Graph) =
    nodes |> List.fold (fun g n -> Graph.addDetachedNode n g) graph

/// Rebuild using `nodes` and the existing graph childMap.
let fromExisting (graph: Graph) (nodes: Map<NodeId, Node>) =
    Graph.fromNodes graph.root nodes graph.childMap

/// Append `kids` to an already-Loaded parent list.
let appendKids parentId kids (graph: Graph) =
    setChildren
        parentId
        (Graph.children graph parentId @ kids)
        graph

/// Attach `child` as a new Owner under an already-Loaded parent.
let addUnder parentId (child: Node) (graph: Graph) =
    Graph.addDetachedNode child graph
    |> appendKids parentId [ ChildNode.owner child.id ]

/// Append a Ref edge under an already-Loaded parent.
let addRef parentId targetId (graph: Graph) =
    appendKids parentId [ ChildNode.reference targetId ] graph

/// Merge Loaded lists onto `graph.childMap`; other keys stay as they are.
let setChildMap
    (pairs: (NodeId * ChildNode list) list)
    (graph: Graph)
    =
    let childMap =
        pairs
        |> List.fold
            (fun acc (parentId, kids) -> Map.add parentId kids acc)
            graph.childMap
    Graph.fromNodes graph.root graph.nodes childMap
