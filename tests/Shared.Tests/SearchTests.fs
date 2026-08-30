module SearchTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open RefExprTestTree
open Xunit

let private setNodeName (nodeId: NodeId) (nameStr: string option) (graph: Graph) : Graph =
    let name =
        match nameStr with
        | None -> Filename.Empty
        | Some s ->
            let f = Filename.create s
            if f = Filename.Invalid s then failwith $"setNodeName: invalid filename '{s}'"
            f
    let node = graph.nodes.[nodeId]
    Graph.fromNodes graph.root (graph.nodes |> Map.add nodeId { node with name = name })

let private ownedRootChildren (ids: NodeId list) (graph: Graph) : Graph =
    let ch = ChildNode.owners ids
    match Graph.replace graph.root 0 [] ch graph with
    | Ok g -> g
    | Error e -> failwith e

[<Fact>]
let ``searchNodes dollar prefix strips like plain term name or text`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "alpha body"; "report body"; "misc" ] graph0
    let byName = ids.[0]
    let byText = ids.[1]
    let graph2 = graph1 |> setNodeName byName (Some "report-tag") |> ownedRootChildren ids
    let z = graph2.root
    let withDollar = ViewModelSearch.searchNodes "report" z graph2 |> List.map (fun r -> r.nodeId)
    let plain = ViewModelSearch.searchNodes "report" z graph2 |> List.map (fun r -> r.nodeId)
    Assert.Equal<NodeId>([ byName; byText ], withDollar)
    Assert.Equal<NodeId>(plain, withDollar)

[<Fact>]
let ``searchNodes matches name or text under root BFS order`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "match-me"; "other text" ] graph0
    let nameOnly = ids.[1]
    let graph2 = graph1 |> setNodeName nameOnly (Some "match-me") |> ownedRootChildren ids
    let z = graph2.root
    let resultIds = ViewModelSearch.searchNodes "match-me" z graph2 |> List.map (fun r -> r.nodeId)
    Assert.Equal<NodeId>([ ids.[0]; nameOnly ], resultIds)

[<Fact>]
let ``searchNodes ordering is deterministic for equal matches under root`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "same token"; "same token"; "same token" ] graph0
    let graph2 = ownedRootChildren ids graph1
    let z = graph2.root
    let first = ViewModelSearch.searchNodes "same" z graph2 |> List.map (fun r -> r.nodeId)
    let second = ViewModelSearch.searchNodes "same" z graph2 |> List.map (fun r -> r.nodeId)
    Assert.Equal<NodeId list>(first, second)

[<Fact>]
let ``searchNodes phase A then B puts zoom subtree before rest of root tree`` () =
    let graph0 = Graph.create ()
    let graph1, ids =
        ModelBuilder.createNodes [ "z"; "za"; "hit here"; "hit branch" ] graph0
    let zId = ids.[0]
    let zaId = ids.[1]
    let hitUnderZa = ids.[2]
    let hitUnderRoot = ids.[3]
    let chZ = [ ChildNode.owner zaId ]
    let chZa = [ ChildNode.owner hitUnderZa ]
    let graph2 =
        match Graph.replace zId 0 [] chZ graph1 with
        | Ok g -> g
        | Error e -> failwith e
    let graph3 =
        match Graph.replace zaId 0 [] chZa graph2 with
        | Ok g -> g
        | Error e -> failwith e
    let graph4 = ownedRootChildren [ zId; hitUnderRoot ] graph3
    let zRoot = graph4.root
    let got = ViewModelSearch.searchNodes "hit" zaId graph4 |> List.map (fun r -> r.nodeId)
    Assert.Equal<NodeId>([ hitUnderZa; hitUnderRoot ], got)

[<Fact>]
let ``searchNodes empty and whitespace query returns no results`` () =
    let graph = Graph.create ()
    let z = graph.root
    Assert.Empty(ViewModelSearch.searchNodes "" z graph)
    Assert.Empty(ViewModelSearch.searchNodes "   " z graph)

[<Fact>]
let ``searchNodes matches text and name ignoring ASCII case`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "CamelCase Body"; "plain" ] graph0
    let bodyId = ids.[0]
    let namedId = ids.[1]
    let graph2 =
        graph1 |> setNodeName namedId (Some "UPPER-TAG") |> ownedRootChildren ids
    let z = graph2.root
    let lowerQueryHits =
        ViewModelSearch.searchNodes "camelcase" z graph2 |> List.map (fun r -> r.nodeId)
    let upperNameHits =
        ViewModelSearch.searchNodes "upper-tag" z graph2 |> List.map (fun r -> r.nodeId)
    Assert.Equal<NodeId>([ bodyId ], lowerQueryHits)
    Assert.Equal<NodeId>([ namedId ], upperNameHits)

[<Fact>]
let ``searchNodes requires every whitespace-separated part in name or text`` () =
    let graph0 = Graph.create ()
    let graph1, ids =
        ModelBuilder.createNodes [ "alpha only"; "alpha with gamma tail"; "gamma first alpha second" ] graph0
    let aOnly = ids.[0]
    let aGamma = ids.[1]
    let ga = ids.[2]
    let graph2 = ownedRootChildren ids graph1
    let z = graph2.root
    let hits = ViewModelSearch.searchNodes "alpha gamma" z graph2 |> List.map (fun r -> r.nodeId)
    Assert.Equal<NodeId>([ aGamma; ga ], hits)
    Assert.DoesNotContain(aOnly, hits)

[<Fact>]
let ``searchNodes splits parts can match name and text on same node`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "2024 filings"; "unrelated" ] graph0
    let taxId = ids.[0]
    let graph2 = graph1 |> setNodeName taxId (Some "IRS-tax") |> ownedRootChildren ids
    let z = graph2.root
    let hits = ViewModelSearch.searchNodes "tax 2024" z graph2 |> List.map (fun r -> r.nodeId)
    Assert.Equal<NodeId>([ taxId ], hits)

[<Fact>]
let ``searchNodes extra whitespace between parts same as single space`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "one two" ] graph0
    let graph2 = ownedRootChildren ids graph1
    let z = graph2.root
    let compact = ViewModelSearch.searchNodes "one two" z graph2 |> List.map (fun r -> r.nodeId)
    let loose = ViewModelSearch.searchNodes "  one    two  " z graph2 |> List.map (fun r -> r.nodeId)
    Assert.Equal<NodeId>(compact, loose)

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
    let ch = [ ChildNode.owner ids.[1]; ChildNode.owner ids.[2] ]
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
    let graph1, ids = ModelBuilder.createNodes [ "ax"; "bx"; "cx" ] graph0
    let graph2 = ownedRootChildren ids graph1
    let z = graph2.root
    let ordered = ViewModelSearch.searchNodes "x" z graph2
    Assert.Equal(3, ordered.Length)
    let expectLast = ordered.[2].nodeId
    let got =
        ViewModelSearch.trySearchResultAtDisplayIndex "x" z graph2 999
        |> Option.map (fun r -> r.nodeId)
    Assert.Equal(Some expectLast, got)
    Assert.Equal(ids.[2], expectLast)

[<Fact>]
let ``trySearchResultAtDisplayIndex empty results is None`` () =
    let graph = Graph.create ()
    Assert.Equal(None, ViewModelSearch.trySearchResultAtDisplayIndex "nope" graph.root graph 0)

[<Fact>]
let ``searchNodes path word matches RefExpr under root`` () =
    let t = build ()
    let got =
        ViewModelSearch.searchNodes "//bobby/src/" t.graph.root t.graph
        |> List.map (fun r -> r.nodeId)
    Assert.Equal<NodeId>([ t.bobbySrc ], got)

[<Fact>]
let ``searchNodes mixed text and path words require same node`` () =
    let t = build ()
    let got =
        ViewModelSearch.searchNodes "readme //bobby/docs/readme.md" t.graph.root t.graph
        |> List.map (fun r -> r.nodeId)
    Assert.Equal<NodeId>([ t.readmeMd ], got)

[<Fact>]
let ``search cursor resumes pages with searchNodes ordering`` () =
    let graph0 = Graph.create ()
    let labels = [ 1..7 ] |> List.map (fun i -> $"ToKeN {i}")
    let graph1, ids = ModelBuilder.createNodes labels graph0
    let graph2 = ownedRootChildren ids graph1
    let cursor = ViewModelSearch.startSearch "TOKEN" graph2.root graph2

    let first, afterFirst =
        cursor |> Option.map (ViewModelSearch.takeResults 3) |> Option.defaultValue ([], None)
    let second, afterSecond =
        afterFirst
        |> Option.map (ViewModelSearch.takeResults 3)
        |> Option.defaultValue ([], None)
    let third, finished =
        afterSecond
        |> Option.map (ViewModelSearch.takeResults 3)
        |> Option.defaultValue ([], None)

    let paged = first @ second @ third |> List.map (fun result -> result.nodeId)
    let unlimited =
        ViewModelSearch.searchNodes "TOKEN" graph2.root graph2
        |> List.map (fun result -> result.nodeId)
    Assert.Equal<NodeId>(ids, paged)
    Assert.Equal<NodeId>(unlimited, paged)
    Assert.Equal(None, finished)

[<Fact>]
let ``search cursor terminates and deduplicates a cycle`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "cycle token" ] graph0
    let childId = ids.[0]
    let graph2 = ownedRootChildren ids graph1
    let child = graph2.nodes.[childId]
    let cycle = [ ChildNode.owner graph2.root ]
    let graph3 =
        Graph.fromNodes graph2.root
            (graph2.nodes |> Map.add childId { child with children = cycle })

    let results, finished =
        ViewModelSearch.startSearch "token" graph3.root graph3
        |> Option.map (ViewModelSearch.takeResults 10)
        |> Option.defaultValue ([], None)

    Assert.Equal<NodeId>([ childId ], results |> List.map (fun result -> result.nodeId))
    Assert.Equal(None, finished)
