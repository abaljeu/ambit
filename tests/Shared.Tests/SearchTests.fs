module SearchTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

let private setNodeName (nodeId: NodeId) (name: string option) (graph: Graph) : Graph =
    let node = graph.nodes.[nodeId]
    { graph with nodes = graph.nodes |> Map.add nodeId { node with name = name } }

[<Fact>]
let ``searchNodes with $query matches name first and text too`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "alpha body"; "report body"; "misc" ] graph0
    let byName = ids.[0]
    let byText = ids.[1]
    let graph2 = graph1 |> setNodeName byName (Some "report-tag")

    let results = ViewModelSearch.searchNodes "$report" graph2
    let resultIds = results |> List.map (fun r -> r.nodeId)

    Assert.Equal<NodeId>([ byName; byText ], resultIds)

[<Fact>]
let ``searchNodes plain query matches text only`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "match me"; "other text" ] graph0
    let nameOnly = ids.[1]
    let graph2 = graph1 |> setNodeName nameOnly (Some "match me")

    let results = ViewModelSearch.searchNodes "match me" graph2
    let resultIds = results |> List.map (fun r -> r.nodeId)

    Assert.Equal<NodeId>([ ids.[0] ], resultIds)

[<Fact>]
let ``searchNodes ordering is deterministic for equal-score matches`` () =
    let graph0 = Graph.create ()
    let graph1, _ = ModelBuilder.createNodes [ "same token"; "same token"; "same token" ] graph0

    let first = ViewModelSearch.searchNodes "same" graph1 |> List.map (fun r -> r.nodeId)
    let second = ViewModelSearch.searchNodes "same" graph1 |> List.map (fun r -> r.nodeId)

    Assert.Equal<NodeId list>(first, second)

[<Fact>]
let ``searchNodes empty and whitespace query returns no results`` () =
    let graph = Graph.create ()

    Assert.Empty(ViewModelSearch.searchNodes "" graph)
    Assert.Empty(ViewModelSearch.searchNodes "   " graph)
    Assert.Empty(ViewModelSearch.searchNodes "$   " graph)
