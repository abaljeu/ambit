module ModelBuilderTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``Create nodes from text list`` () =
    let graph0 = Graph.create ()
    let graph1, nodeIds = ModelBuilder.createNodes [ "a"; "b" ] graph0

    Assert.Equal(Graph.nodeCount graph0 + 2, Graph.nodeCount graph1)
    Assert.Equal(2, nodeIds.Length)
    Assert.Equal("a", graph1.nodes[nodeIds[0]].text)
    Assert.Equal("b", graph1.nodes[nodeIds[1]].text)

[<Fact>]
let ``CreateDag12 builds a 12 node dag with depth 3`` () =
    let graph = ModelBuilder.createDag12 ()
    Assert.Equal(12, Graph.nodeCount graph)

    let rec maxDepthFrom nodeId depth visited =
        if Set.contains nodeId visited then
            depth
        else
            let node = graph.nodes[nodeId]
            match node.children with
            | [] -> depth
            | children ->
                let visited2 = Set.add nodeId visited
                children
                |> List.map (fun childId -> maxDepthFrom childId (depth + 1) visited2)
                |> List.max

    let depth = maxDepthFrom graph.root 0 Set.empty
    Assert.Equal(3, depth)
