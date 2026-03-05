module Gambol.Shared.Tests.PasteTests

open System
open Xunit
open Gambol.Shared
open Gambol.Shared.Paste
open Gambol.Shared.ViewModel

let private nl = Environment.NewLine

let private requireOk label = function
    | Ok v -> v
    | Error (e: string) -> failwith $"{label}: {e}"

/// Build graph: root → ids in order.
let private buildFlat (texts: string list) : Graph * NodeId list =
    let g0 = Graph.create ()
    let g1, ids = ModelBuilder.createNodes texts g0
    let g2 = Graph.replace g1.root 0 [] ids g1 |> requireOk "buildFlat"
    g2, ids

/// Build graph: root → [parentText → childTexts].
let private buildNested (parentText: string) (childTexts: string list) : Graph * NodeId * NodeId list =
    let g0 = Graph.create ()
    let g1, parentIds = ModelBuilder.createNodes [parentText] g0
    let parentId = parentIds.[0]
    let g2, childIds = ModelBuilder.createNodes childTexts g1
    let g3 = Graph.replace g2.root 0 [] [parentId] g2 |> requireOk "buildNested.root"
    let g4 = Graph.replace parentId 0 [] childIds g3 |> requireOk "buildNested.parent"
    g4, parentId, childIds

/// Find the instanceId of the first SiteNode whose nodeId matches.
let private findInstanceId (nodeId: NodeId) (siteRoot: SiteNode) : int =
    let rec find (sn: SiteNode) =
        if sn.nodeId = nodeId then Some sn.instanceId
        else sn.children |> List.tryPick find
    find siteRoot |> Option.defaultWith (fun () -> failwith $"instanceId not found for {nodeId}")

// ---------------------------------------------------------------------------
// serializeSubtree
// ---------------------------------------------------------------------------

[<Fact>]
let ``serializeSubtree single node no children`` () =
    let graph, ids = buildFlat ["hello"]
    let id = ids.[0]
    let siteRoot, _ = buildSiteTree graph
    let result = serializeSubtree graph siteRoot [id]
    Assert.Equal("hello" + nl, result)

[<Fact>]
let ``serializeSubtree two sibling nodes`` () =
    let graph, ids = buildFlat ["alpha"; "beta"]
    let siteRoot, _ = buildSiteTree graph
    let result = serializeSubtree graph siteRoot ids
    Assert.Equal("alpha" + nl + "beta" + nl, result)

[<Fact>]
let ``serializeSubtree node with collapsed children omits children`` () =
    let graph, parentId, _childIds = buildNested "parent" ["child1"; "child2"]
    let siteRoot, _ = buildSiteTree graph
    // parent is collapsed by default
    let result = serializeSubtree graph siteRoot [parentId]
    Assert.Equal("parent" + nl, result)

[<Fact>]
let ``serializeSubtree node with expanded children includes children`` () =
    let graph, parentId, _childIds = buildNested "parent" ["child1"; "child2"]
    let siteRoot, _ = buildSiteTree graph
    let parentInstanceId = findInstanceId parentId siteRoot
    let siteRoot' = toggleFold parentInstanceId siteRoot
    let result = serializeSubtree graph siteRoot' [parentId]
    Assert.Equal("parent" + nl + "\tchild1" + nl + "\tchild2" + nl, result)

[<Fact>]
let ``serializeSubtree multi-level expanded`` () =
    // root → a → b → c
    let g0 = Graph.create ()
    let g1, abcIds = ModelBuilder.createNodes ["a"; "b"; "c"] g0
    let aId, bId, cId = abcIds.[0], abcIds.[1], abcIds.[2]
    let g2 = Graph.replace g1.root 0 [] [aId] g1 |> requireOk "root"
    let g3 = Graph.replace aId 0 [] [bId] g2 |> requireOk "a"
    let g4 = Graph.replace bId 0 [] [cId] g3 |> requireOk "b"
    let siteRoot, _ = buildSiteTree g4
    // Expand a and b
    let aInst = findInstanceId aId siteRoot
    let siteRoot' = toggleFold aInst siteRoot
    let bInst = findInstanceId bId siteRoot'
    let siteRoot'' = toggleFold bInst siteRoot'
    let result = serializeSubtree g4 siteRoot'' [aId]
    Assert.Equal("a" + nl + "\tb" + nl + "\t\tc" + nl, result)

[<Fact>]
let ``serializeSubtree partial expand: only expanded level included`` () =
    // root → a → b → c; expand a but not b
    let g0 = Graph.create ()
    let g1, abcIds = ModelBuilder.createNodes ["a"; "b"; "c"] g0
    let aId, bId, cId = abcIds.[0], abcIds.[1], abcIds.[2]
    let g2 = Graph.replace g1.root 0 [] [aId] g1 |> requireOk "root"
    let g3 = Graph.replace aId 0 [] [bId] g2 |> requireOk "a"
    let g4 = Graph.replace bId 0 [] [cId] g3 |> requireOk "b"
    let siteRoot, _ = buildSiteTree g4
    let aInst = findInstanceId aId siteRoot
    let siteRoot' = toggleFold aInst siteRoot  // expand a only
    let result = serializeSubtree g4 siteRoot' [aId]
    // b is collapsed → c not included
    Assert.Equal("a" + nl + "\tb" + nl, result)

// ---------------------------------------------------------------------------
// collectSubtree
// ---------------------------------------------------------------------------

[<Fact>]
let ``collectSubtree single node no children`` () =
    let graph, ids = buildFlat ["x"]
    let id = ids.[0]
    let siteRoot, _ = buildSiteTree graph
    let cb = collectSubtree graph siteRoot [id]
    Assert.Equal<NodeId list>([id], cb.topLevelIds)
    Assert.Equal(1, cb.nodes.Count)
    Assert.Equal("x", cb.nodes.[id].text)
    Assert.Empty(cb.nodes.[id].children)

[<Fact>]
let ``collectSubtree collapsed node excludes children`` () =
    let graph, parentId, _childIds = buildNested "p" ["c1"; "c2"]
    let siteRoot, _ = buildSiteTree graph
    // parent collapsed (default)
    let cb = collectSubtree graph siteRoot [parentId]
    Assert.Equal<NodeId list>([parentId], cb.topLevelIds)
    Assert.Equal(1, cb.nodes.Count)
    Assert.Empty(cb.nodes.[parentId].children)

[<Fact>]
let ``collectSubtree expanded node includes children`` () =
    let graph, parentId, childIds = buildNested "p" ["c1"; "c2"]
    let siteRoot, _ = buildSiteTree graph
    let parentInst = findInstanceId parentId siteRoot
    let siteRoot' = toggleFold parentInst siteRoot
    let cb = collectSubtree graph siteRoot' [parentId]
    Assert.Equal<NodeId list>([parentId], cb.topLevelIds)
    Assert.Equal(3, cb.nodes.Count)  // parent + 2 children
    Assert.Equal<NodeId list>(childIds, cb.nodes.[parentId].children)
    for cid in childIds do
        Assert.True(cb.nodes.ContainsKey cid)

[<Fact>]
let ``collectSubtree multiple top-level nodes`` () =
    let graph, ids = buildFlat ["a"; "b"; "c"]
    let siteRoot, _ = buildSiteTree graph
    let cb = collectSubtree graph siteRoot ids
    Assert.Equal<NodeId list>(ids, cb.topLevelIds |> List.ofSeq)
    Assert.Equal(3, cb.nodes.Count)

// ---------------------------------------------------------------------------
// buildPasteOpsFromClipboard
// ---------------------------------------------------------------------------

let private applyOps (ops: Op list) (graph: Graph) : Graph =
    ops |> List.fold (fun g op ->
        let state = { graph = g; history = History.empty; revision = Revision.Zero }
        match Op.apply op state with
        | ApplyResult.Changed s -> s.graph
        | _ -> failwith "op failed") graph

[<Fact>]
let ``buildPasteOpsFromClipboard single node gets fresh id and same text`` () =
    let oldId = NodeId.New()
    let cb =
        { topLevelIds = [oldId]
          nodes = Map.ofList [oldId, { id = oldId; text = "hello"; name = None; children = [] }] }
    let newTopIds, ops = buildPasteOpsFromClipboard cb
    Assert.Equal(1, newTopIds.Length)
    Assert.NotEqual(oldId, newTopIds.[0])
    let graph = applyOps ops (Graph.create ())
    let newNode = graph.nodes.[newTopIds.[0]]
    Assert.Equal("hello", newNode.text)
    Assert.Empty(newNode.children)

[<Fact>]
let ``buildPasteOpsFromClipboard remaps parent-child relationship`` () =
    let aId = NodeId.New()
    let bId = NodeId.New()
    let cb =
        { topLevelIds = [aId]
          nodes = Map.ofList
            [ aId, { id = aId; text = "a"; name = None; children = [bId] }
              bId, { id = bId; text = "b"; name = None; children = [] } ] }
    let newTopIds, ops = buildPasteOpsFromClipboard cb
    let graph = applyOps ops (Graph.create ())
    let newAId = newTopIds.[0]
    Assert.NotEqual(aId, newAId)
    let newANode = graph.nodes.[newAId]
    Assert.Equal("a", newANode.text)
    Assert.Equal(1, newANode.children.Length)
    let newBId = newANode.children.[0]
    Assert.NotEqual(bId, newBId)
    let newBNode = graph.nodes.[newBId]
    Assert.Equal("b", newBNode.text)

[<Fact>]
let ``buildPasteOpsFromClipboard multiple top-level nodes`` () =
    let id1 = NodeId.New()
    let id2 = NodeId.New()
    let cb =
        { topLevelIds = [id1; id2]
          nodes = Map.ofList
            [ id1, { id = id1; text = "x"; name = None; children = [] }
              id2, { id = id2; text = "y"; name = None; children = [] } ] }
    let newTopIds, ops = buildPasteOpsFromClipboard cb
    Assert.Equal(2, newTopIds.Length)
    Assert.NotEqual(id1, newTopIds.[0])
    Assert.NotEqual(id2, newTopIds.[1])
    let graph = applyOps ops (Graph.create ())
    Assert.Equal("x", graph.nodes.[newTopIds.[0]].text)
    Assert.Equal("y", graph.nodes.[newTopIds.[1]].text)

[<Fact>]
let ``buildPasteOpsFromClipboard all old ids absent from new graph keys`` () =
    let oldId = NodeId.New()
    let cb =
        { topLevelIds = [oldId]
          nodes = Map.ofList [oldId, { id = oldId; text = "z"; name = None; children = [] }] }
    let _, ops = buildPasteOpsFromClipboard cb
    let graph = applyOps ops (Graph.create ())
    Assert.False(graph.nodes.ContainsKey oldId)
