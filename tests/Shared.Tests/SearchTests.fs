module SearchTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

let private setNodeName (nodeId: NodeId) (name: string option) (graph: Graph) : Graph =
    let node = graph.nodes.[nodeId]
    Graph.fromNodes graph.root (graph.nodes |> Map.add nodeId { node with name = name })

let private ownedRootChildren (ids: NodeId list) (graph: Graph) : Graph =
    let ch = ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })
    match Graph.replace graph.root 0 [] ch graph with
    | Ok g -> g
    | Error e -> failwith e

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

[<Fact>]
let ``makeNodeRangeForInsertingUnder appends after existing children`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "a"; "b"; "c" ] graph0
    let graph2 = ownedRootChildren ids graph1
    let got = Graph.makeNodeRangeForInsertingUnder ids.[1] graph2
    let expect = Some { pnode = ids.[1]; start = 0; endd = 0 }
    Assert.Equal(expect, got)

[<Fact>]
let ``makeNodeRangeForInsertingUnder node with children appends at end`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "parent"; "child1"; "child2" ] graph0
    let graph2 = ownedRootChildren [ ids.[0] ] graph1
    let ch = [ { ref = Ownership.Owner; id = ids.[1] }; { ref = Ownership.Owner; id = ids.[2] } ]
    let graph3 =
        match Graph.replace ids.[0] 0 [] ch graph2 with
        | Ok g -> g
        | Error e -> failwith e
    let got = Graph.makeNodeRangeForInsertingUnder ids.[0] graph3
    let expect = Some { pnode = ids.[0]; start = 2; endd = 2 }
    Assert.Equal(expect, got)

[<Fact>]
let ``makeNodeRangeForInsertingUnder unknown node is None`` () =
    let graph = Graph.create ()
    Assert.Equal(None, Graph.makeNodeRangeForInsertingUnder (NodeId.New()) graph)

[<Fact>]
let ``trySearchResultAtDisplayIndex clamps high index to last row`` () =
    let graph0 = Graph.create ()
    let graph1, _ = ModelBuilder.createNodes [ "ax"; "bx"; "cx" ] graph0
    let ordered = ViewModelSearch.searchNodes "x" graph1
    Assert.Equal(3, ordered.Length)
    let expectLast = ordered.[2].nodeId
    let got =
        ViewModelSearch.trySearchResultAtDisplayIndex "x" graph1 999
        |> Option.map (fun r -> r.nodeId)
    Assert.Equal(Some expectLast, got)

[<Fact>]
let ``trySearchResultAtDisplayIndex empty results is None`` () =
    let graph = Graph.create ()
    Assert.Equal(None, ViewModelSearch.trySearchResultAtDisplayIndex "nope" graph 0)
