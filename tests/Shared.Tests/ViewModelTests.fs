module ViewModelTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

// ---------------------------------------------------------------------------
// Test helpers
// ---------------------------------------------------------------------------

/// Build a flat graph: root has `texts.Length` children in order.
let buildFlat (texts: string list) : Graph * NodeId list =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes texts graph0
    let graph2 =
        Graph.replace graph1.root 0 [] ids graph1
        |> ModelBuilder.requireOk "buildFlat.replace"
    graph2, ids

/// Minimal Model helper — no selection, Selecting mode.
let emptyModel (graph: Graph) : Model =
    let siteRoot, nextId = ViewModel.buildSiteTree graph
    { graph = graph; revision = Revision.Zero; selectedNodes = None; mode = Selecting
      siteRoot = siteRoot; nextInstanceId = nextId; clipboard = None; linkPasteEnabled = false }

/// Model with a selection covering [start, endd) in root's children, focus at focusIdx.
let modelWithSel (graph: Graph) (start: int) (endd: int) (focusIdx: int) : Model =
    { emptyModel graph with
        selectedNodes = Some { range = { parent = graph.root; start = start; endd = endd }; focus = focusIdx } }

// ---------------------------------------------------------------------------
// singleSelection
// ---------------------------------------------------------------------------

[<Fact>]
let ``singleSelection returns Selection with focus equal to start`` () =
    let graph, ids = buildFlat ["a"; "b"; "c"]
    let nodeId = ids.[1]  // "b", index 1 in root's children
    let result = singleSelection graph nodeId
    match result with
    | None -> Assert.True(false, "Expected Some, got None")
    | Some sel ->
        Assert.Equal(sel.range.start, sel.focus)
        Assert.Equal(1, sel.range.start)
        Assert.Equal(2, sel.range.endd)
        Assert.Equal(graph.root, sel.range.parent)

[<Fact>]
let ``singleSelection returns None for root node`` () =
    let graph, _ = buildFlat ["a"]
    let result = singleSelection graph graph.root
    Assert.True(result.IsNone)

// ---------------------------------------------------------------------------
// shiftArrow — single node always extends
// ---------------------------------------------------------------------------

[<Fact>]
let ``shiftArrow +1 on single-node selection extends downward`` () =
    let graph, _ = buildFlat ["a"; "b"; "c"]
    let model = modelWithSel graph 0 1 0
    let result = shiftArrow 1 model
    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(0, sel.range.start)
        Assert.Equal(2, sel.range.endd)
        Assert.Equal(1, sel.focus)  // focus at endd-1

[<Fact>]
let ``shiftArrow -1 on single-node selection extends upward`` () =
    let graph, _ = buildFlat ["a"; "b"; "c"]
    let model = modelWithSel graph 1 2 1
    let result = shiftArrow -1 model
    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(0, sel.range.start)
        Assert.Equal(2, sel.range.endd)
        Assert.Equal(0, sel.focus)  // focus at new start

// ---------------------------------------------------------------------------
// shiftArrow — multi-node shrink / extend
// ---------------------------------------------------------------------------

[<Fact>]
let ``shiftArrow -1 with focus at start extends upward`` () =
    let graph, _ = buildFlat ["a"; "b"; "c"; "d"]
    let model = modelWithSel graph 1 3 1  // [1,3), focus at start=1
    let result = shiftArrow -1 model
    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(0, sel.range.start)
        Assert.Equal(3, sel.range.endd)
        Assert.Equal(0, sel.focus)

[<Fact>]
let ``shiftArrow -1 with focus at end shrinks from bottom`` () =
    let graph, _ = buildFlat ["a"; "b"; "c"; "d"]
    let model = modelWithSel graph 1 3 2  // [1,3), focus at endd-1=2
    let result = shiftArrow -1 model
    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(1, sel.range.start)
        Assert.Equal(2, sel.range.endd)
        Assert.Equal(1, sel.focus)  // focus = new endd-1

[<Fact>]
let ``shiftArrow +1 with focus at end extends downward`` () =
    let graph, _ = buildFlat ["a"; "b"; "c"; "d"]
    let model = modelWithSel graph 1 3 2  // focus at endd-1=2
    let result = shiftArrow 1 model
    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(1, sel.range.start)
        Assert.Equal(4, sel.range.endd)
        Assert.Equal(3, sel.focus)

[<Fact>]
let ``shiftArrow +1 with focus at start shrinks from top`` () =
    let graph, _ = buildFlat ["a"; "b"; "c"; "d"]
    let model = modelWithSel graph 1 3 1  // focus at start=1
    let result = shiftArrow 1 model
    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(2, sel.range.start)
        Assert.Equal(3, sel.range.endd)
        Assert.Equal(2, sel.focus)

// ---------------------------------------------------------------------------
// shiftArrow — no-op at bounds
// ---------------------------------------------------------------------------

[<Fact>]
let ``shiftArrow -1 is no-op when single node is at index 0`` () =
    let graph, _ = buildFlat ["a"; "b"; "c"]
    let model = modelWithSel graph 0 1 0
    let result = shiftArrow -1 model
    Assert.Equal(model.selectedNodes, result.selectedNodes)

[<Fact>]
let ``shiftArrow +1 is no-op when single node is at last index`` () =
    let graph, _ = buildFlat ["a"; "b"; "c"]
    let model = modelWithSel graph 2 3 2
    let result = shiftArrow 1 model
    Assert.Equal(model.selectedNodes, result.selectedNodes)

// ---------------------------------------------------------------------------
// applyMoveSelectionDown / applyMoveSelectionUp — multi-range focus moves
// ---------------------------------------------------------------------------

[<Fact>]
let ``applyMoveSelectionDown with 3-item range focus at start moves focus to endd-1`` () =
    let graph, _ = buildFlat ["a"; "b"; "c"; "d"; "e"]
    // range [1,4), focus at start=1
    let model = modelWithSel graph 1 4 1
    let result = applyMoveSelectionDown model
    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(1, sel.range.start)
        Assert.Equal(4, sel.range.endd)
        Assert.Equal(3, sel.focus)  // moved to endd-1

[<Fact>]
let ``applyMoveSelectionUp with 3-item range focus at end moves focus to start`` () =
    let graph, _ = buildFlat ["a"; "b"; "c"; "d"; "e"]
    // range [1,4), focus at endd-1=3
    let model = modelWithSel graph 1 4 3
    let result = applyMoveSelectionUp model
    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(1, sel.range.start)
        Assert.Equal(4, sel.range.endd)
        Assert.Equal(1, sel.focus)  // moved to start

// ---------------------------------------------------------------------------
// applyMoveSelectionDown — when focus is already at end, collapse and move
// ---------------------------------------------------------------------------

[<Fact>]
let ``applyMoveSelectionDown with focus at end collapses and moves down`` () =
    let graph, ids = buildFlat ["a"; "b"; "c"]
    // single-node at index 0, focus also at 0
    let model = modelWithSel graph 0 1 0
    let result = applyMoveSelectionDown model
    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        // Should have moved to ids.[1]
        let expectedId = graph.nodes.[graph.root].children.[1]
        let gotId = graph.nodes.[sel.range.parent].children.[sel.focus]
        Assert.Equal(expectedId, gotId)
        Assert.Equal(1, sel.range.endd - sel.range.start)  // single-node

// ---------------------------------------------------------------------------
// moveSelectionBy — advances visible row index
// ---------------------------------------------------------------------------

[<Fact>]
let ``moveSelectionBy 1 advances from first to second visible row`` () =
    let graph, ids = buildFlat ["a"; "b"; "c"]
    let model = modelWithSel graph 0 1 0  // select first child
    let result = moveSelectionBy 1 model
    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        let gotId = focusedNodeId graph sel
        Assert.Equal(ids.[1], gotId)

[<Fact>]
let ``moveSelectionBy -1 moves back to previous row`` () =
    let graph, ids = buildFlat ["a"; "b"; "c"]
    let model = modelWithSel graph 1 2 1  // select second child
    let result = moveSelectionBy -1 model
    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        let gotId = focusedNodeId graph sel
        Assert.Equal(ids.[0], gotId)

[<Fact>]
let ``moveSelectionBy 1 is no-op at last row`` () =
    let graph, _ = buildFlat ["a"; "b"; "c"]
    let model = modelWithSel graph 2 3 2  // select last child
    let result = moveSelectionBy 1 model
    Assert.Equal(model.selectedNodes, result.selectedNodes)

// ---------------------------------------------------------------------------
// buildSiteTree
// ---------------------------------------------------------------------------

/// Build a 2-level graph: root -> [a, b], a -> [a1, a2], b -> [b1]
let buildNested () : Graph * NodeId list =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes ["a"; "b"; "a1"; "a2"; "b1"] graph0
    let a  = ids.[0]
    let b  = ids.[1]
    let a1 = ids.[2]
    let a2 = ids.[3]
    let b1 = ids.[4]
    let graph2 =
        Graph.replace graph1.root 0 [] [a; b] graph1
        |> ModelBuilder.requireOk "buildNested.root"
    let graph3 =
        Graph.replace a 0 [] [a1; a2] graph2
        |> ModelBuilder.requireOk "buildNested.a"
    let graph4 =
        Graph.replace b 0 [] [b1] graph3
        |> ModelBuilder.requireOk "buildNested.b"
    graph4, ids

[<Fact>]
let ``buildSiteTree assigns unique instanceIds`` () =
    let graph, _ = buildNested ()
    let siteRoot, nextId = ViewModel.buildSiteTree graph
    // Collect all instanceIds
    let rec collectIds (node: SiteNode) : int list =
        node.instanceId :: (node.children |> List.collect collectIds)
    let allIds = collectIds siteRoot
    Assert.Equal(allIds.Length, (allIds |> List.distinct |> List.length))
    Assert.Equal(allIds.Length, nextId)

[<Fact>]
let ``buildSiteTree root is expanded, children are collapsed`` () =
    let graph, ids = buildNested ()
    let siteRoot, _ = ViewModel.buildSiteTree graph
    Assert.True(siteRoot.expanded)
    // Direct children of root should be collapsed
    for child in siteRoot.children do
        Assert.False(child.expanded)

[<Fact>]
let ``buildSiteTree preserves graph structure`` () =
    let graph, ids = buildNested ()
    let a = ids.[0]
    let b = ids.[1]
    let siteRoot, _ = ViewModel.buildSiteTree graph
    Assert.Equal(2, siteRoot.children.Length)
    Assert.Equal(a, siteRoot.children.[0].nodeId)
    Assert.Equal(b, siteRoot.children.[1].nodeId)
    Assert.Equal(2, siteRoot.children.[0].children.Length)  // a has a1, a2
    Assert.Equal(1, siteRoot.children.[1].children.Length)  // b has b1

// ---------------------------------------------------------------------------
// rebuildSiteTree
// ---------------------------------------------------------------------------

[<Fact>]
let ``rebuildSiteTree preserves instanceIds for unchanged nodes`` () =
    let graph, _ = buildNested ()
    let siteRoot, nextId = ViewModel.buildSiteTree graph
    // Rebuild with same graph — all instanceIds should be preserved
    let rebuilt, nextId2 = ViewModel.rebuildSiteTree graph siteRoot nextId
    let rec pairs (a: SiteNode) (b: SiteNode) : (int * int) list =
        (a.instanceId, b.instanceId) ::
            (List.zip a.children b.children |> List.collect (fun (ac, bc) -> pairs ac bc))
    for (oldId, newId) in pairs siteRoot rebuilt do
        Assert.Equal(oldId, newId)
    // No new IDs allocated
    Assert.Equal(nextId, nextId2)

[<Fact>]
let ``rebuildSiteTree preserves fold state`` () =
    let graph, _ = buildNested ()
    let siteRoot, nextId = ViewModel.buildSiteTree graph
    // Expand the first child (node "a")
    let expandedRoot = ViewModel.toggleFold siteRoot.children.[0].instanceId siteRoot
    Assert.True(expandedRoot.children.[0].expanded)
    // Rebuild — fold state should survive
    let rebuilt, _ = ViewModel.rebuildSiteTree graph expandedRoot nextId
    Assert.True(rebuilt.children.[0].expanded)
    Assert.False(rebuilt.children.[1].expanded)

[<Fact>]
let ``rebuildSiteTree assigns new IDs for new nodes`` () =
    let graph, ids = buildNested ()
    let siteRoot, nextId = ViewModel.buildSiteTree graph
    // Add a new child under root
    let graph2, newNodeId = Graph.newNode "c" graph
    let graph3 =
        let root = graph2.nodes.[graph2.root]
        Graph.replace graph2.root root.children.Length [] [newNodeId] graph2
        |> ModelBuilder.requireOk "rebuildSiteTree.addChild"
    let rebuilt, nextId2 = ViewModel.rebuildSiteTree graph3 siteRoot nextId
    Assert.Equal(3, rebuilt.children.Length)
    // New node should have instanceId >= nextId
    let newChild = rebuilt.children.[2]
    Assert.Equal(newNodeId, newChild.nodeId)
    Assert.True(newChild.instanceId >= nextId)
    Assert.Equal(nextId2, nextId + 1)

[<Fact>]
let ``rebuildSiteTree new nodes default to collapsed`` () =
    let graph, _ = buildNested ()
    let siteRoot, nextId = ViewModel.buildSiteTree graph
    let graph2, newNodeId = Graph.newNode "c" graph
    let graph3 =
        let root = graph2.nodes.[graph2.root]
        Graph.replace graph2.root root.children.Length [] [newNodeId] graph2
        |> ModelBuilder.requireOk "rebuildSiteTree.addChild"
    let rebuilt, _ = ViewModel.rebuildSiteTree graph3 siteRoot nextId
    let newChild = rebuilt.children.[2]
    Assert.False(newChild.expanded)

// ---------------------------------------------------------------------------
// toggleFold
// ---------------------------------------------------------------------------

[<Fact>]
let ``toggleFold toggles expanded state for matching instanceId`` () =
    let graph, _ = buildNested ()
    let siteRoot, _ = ViewModel.buildSiteTree graph
    let childId = siteRoot.children.[0].instanceId
    Assert.False(siteRoot.children.[0].expanded)
    let toggled = ViewModel.toggleFold childId siteRoot
    Assert.True(toggled.children.[0].expanded)
    let toggledBack = ViewModel.toggleFold childId toggled
    Assert.False(toggledBack.children.[0].expanded)

// ---------------------------------------------------------------------------
// getVisibleRowIds respects fold state
// ---------------------------------------------------------------------------

[<Fact>]
let ``getVisibleRowIds shows only top-level when all collapsed`` () =
    let graph, ids = buildNested ()
    let siteRoot, _ = ViewModel.buildSiteTree graph
    // All non-root nodes are collapsed by default
    let visible = ViewModel.getVisibleRowIds siteRoot
    Assert.Equal(2, visible.Length)  // just "a" and "b"
    Assert.Equal(ids.[0], visible.[0])
    Assert.Equal(ids.[1], visible.[1])

[<Fact>]
let ``getVisibleRowIds shows children of expanded node`` () =
    let graph, ids = buildNested ()
    let siteRoot, _ = ViewModel.buildSiteTree graph
    // Expand "a"
    let expanded = ViewModel.toggleFold siteRoot.children.[0].instanceId siteRoot
    let visible = ViewModel.getVisibleRowIds expanded
    // Should see: a, a1, a2, b
    Assert.Equal(4, visible.Length)
    Assert.Equal(ids.[0], visible.[0])   // a
    Assert.Equal(ids.[2], visible.[1])   // a1
    Assert.Equal(ids.[3], visible.[2])   // a2
    Assert.Equal(ids.[1], visible.[3])   // b
