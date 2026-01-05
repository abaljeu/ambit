module ModelTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``Create graph has one node`` () =
    let graph = Graph.create ()
    Assert.Equal(1, Graph.nodeCount graph)

[<Fact>]
let ``Root node exists in graph`` () =
    let graph = Graph.create ()
    Assert.True(Graph.contains graph.root graph)

[<Fact>]
let ``New node increments node count`` () =
    let graph0 = Graph.create ()
    let count0 = Graph.nodeCount graph0
    let graph1, _nodeId = Graph.newNode "hello" graph0
    Assert.Equal(count0 + 1, Graph.nodeCount graph1)
    Assert.Equal(0, graph1.nodes[graph1.root].children.Length)

[<Fact>]
let ``Set text updates node when old matches`` () =
    let graph0 = Graph.create ()
    let result = Graph.setText graph0.root "" "hello" graph0

    match result with
    | Ok graph1 -> Assert.Equal("hello", graph1.nodes[graph1.root].text)
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")

[<Fact>]
let ``Replace can insert children into root`` () =
    let graph0 = Graph.create ()
    let graph1, (ids : NodeId list) = ModelBuilder.createNodes [ "a"; "b"; "c" ] graph0
    let result = Graph.replace graph1.root 0 [] ids graph1

    match result with
    | Ok (graph2 : Graph) -> 
        let children : NodeId list = graph2.nodes[graph2.root].children
        Assert.Equal<NodeId>(ids, children)
    | Error err -> Assert.True(false, $"Expected Ok, got Error: {err}")


// For Graph.replace node index oldList newList -> Result

// replace: when old span matches, parent children are updated
// replace: node count does not change
// replace: errors when parent node id is missing
// replace: errors when any new child id is missing
// replace: errors when index is out of bounds
// replace: errors when old span does not match existing children at index
// replace: supports insert (old ids empty) at index
// replace: supports delete (new ids empty) at index
