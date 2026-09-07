module GraphSpanTests

open Gambol.Shared
open Xunit

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private appendOwned (parentId: NodeId) (child: Node) (graph: Graph) : Graph =
    let parent = graph.nodes.[parentId]
    let nodes =
        graph.nodes
        |> Map.add child.id child
        |> Map.add parentId
            { parent with children = parent.children @ [ ChildNode.owner child.id ] }
    Graph.fromNodes graph.root nodes

[<Fact>]
let ``extract takes parent and span children plus owned descendants`` () =
    let a = NodeId.New()
    let b = NodeId.New()
    let grandchild = NodeId.New()
    let graph0 = Graph.create ()
    let graph1 = appendOwned Graph.rootId (Node.Create(a, text = "a")) graph0
    let graph2 = appendOwned Graph.rootId (Node.Create(b, text = "b")) graph1
    let graph3 =
        appendOwned a (Node.Create(grandchild, text = "g")) graph2
    let start = graph3.nodes.[Graph.rootId].children |> List.findIndex (fun c -> c.id = a)
    let span = { pnode = Graph.rootId; start = start; endd = start + 1 }
    let sub = GraphSpan.extract graph3 span |> requireOk "extract"
    Assert.Equal(Graph.rootId, sub.root)
    Assert.True(Map.containsKey a sub.nodes)
    Assert.True(Map.containsKey grandchild sub.nodes)
    Assert.False(Map.containsKey b sub.nodes)
    Assert.Equal<NodeId list>(
        [ a ],
        sub.nodes.[Graph.rootId].children |> List.map _.id)

[<Fact>]
let ``caret and missing parent are refused`` () =
    let graph = Graph.create ()
    let caret = { pnode = Graph.rootId; start = 0; endd = 0 }
    match GraphSpan.extract graph caret with
    | Error msg -> Assert.Equal(GraphSpan.emptySpan, msg)
    | Ok _ -> Assert.Fail("caret must refuse")
    let missing = { pnode = NodeId.New(); start = 0; endd = 1 }
    match GraphSpan.extract graph missing with
    | Error msg -> Assert.Equal(GraphSpan.parentMissing, msg)
    | Ok _ -> Assert.Fail("missing parent must refuse")

[<Fact>]
let ``spanIds are the child occurrences not descendants`` () =
    let a = NodeId.New()
    let grandchild = NodeId.New()
    let graph0 = Graph.create ()
    let graph1 = appendOwned Graph.rootId (Node.Create(a, text = "a")) graph0
    let graph2 = appendOwned a (Node.Create(grandchild, text = "g")) graph1
    let start = graph2.nodes.[Graph.rootId].children |> List.findIndex (fun c -> c.id = a)
    let span = { pnode = Graph.rootId; start = start; endd = start + 1 }
    let ids = GraphSpan.spanIds graph2 span |> requireOk "spanIds"
    Assert.Equal<Set<NodeId>>(Set.singleton a, ids)
    Assert.False(Set.contains grandchild ids)

[<Fact>]
let ``withLockPresent marks only the given live Nodes`` () =
    let a = NodeId.New()
    let graph = appendOwned Graph.rootId (Node.Create(a, text = "a")) (Graph.create ())
    let marked = GraphSpan.withLockPresent (Set.singleton a) graph
    Assert.True(marked.nodes.[a].lockPresent)
    Assert.False(marked.nodes.[Graph.rootId].lockPresent)

[<Fact>]
let ``projection rows drop lock-present`` () =
    let graph0 = Graph.create ()
    let locked = GraphSpan.withLockPresent (Set.singleton Graph.rootId) graph0
    Assert.True(locked.nodes.[Graph.rootId].lockPresent)
    let rows = GraphProjection.nodeRowsFromGraph locked
    let childRows = GraphProjection.childRowsFromGraph locked
    let restored =
        GraphProjection.graphFromPersistence graph0.root rows childRows
        |> requireOk "projection"
    Assert.False(restored.nodes.[Graph.rootId].lockPresent)
