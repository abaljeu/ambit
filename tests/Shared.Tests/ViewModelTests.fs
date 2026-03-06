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
    let m = emptyModel graph
    { m with
        selectedNodes = Some { range = { parent = m.siteRoot; start = start; endd = endd }; focus = focusIdx } }

// ---------------------------------------------------------------------------
// singleSelection
// ---------------------------------------------------------------------------

[<Fact>]
let ``singleSelection returns Selection with focus equal to start`` () =
    let graph, ids = buildFlat ["a"; "b"; "c"]
    let nodeId = ids.[1]  // "b", index 1 in root's children
    let siteRoot, _ = ViewModel.buildSiteTree graph
    let result = singleSelection graph siteRoot nodeId
    match result with
    | None -> Assert.True(false, "Expected Some, got None")
    | Some sel ->
        Assert.Equal(sel.range.start, sel.focus)
        Assert.Equal(1, sel.range.start)
        Assert.Equal(2, sel.range.endd)
        Assert.Equal(graph.root, sel.range.parent.nodeId)

[<Fact>]
let ``singleSelection returns None for root node`` () =
    let graph, _ = buildFlat ["a"]
    let siteRoot, _ = ViewModel.buildSiteTree graph
    let result = singleSelection graph siteRoot graph.root
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
        let gotId = graph.nodes.[sel.range.parent.nodeId].children.[sel.focus]
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

// ---------------------------------------------------------------------------
// SiteMapOps.buildSiteMap
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteMap buildSiteMap assigns unique instanceIds`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = ViewModel.SiteMapOps.buildSiteMap graph 0
    let allIds = siteMap.entries |> Map.toList |> List.map fst
    Assert.Equal(allIds.Length, allIds |> List.distinct |> List.length)
    // nextId should equal number of entries allocated
    Assert.Equal(siteMap.entries.Count, nextId)

[<Fact>]
let ``SiteMap buildSiteMap root is expanded, children collapsed`` () =
    let graph, _ = buildNested ()
    let siteMap, _ = ViewModel.SiteMapOps.buildSiteMap graph 0
    let root = siteMap.entries.[siteMap.rootId]
    Assert.True(root.expanded)
    for childInstId in root.children do
        Assert.False(siteMap.entries.[childInstId].expanded)

[<Fact>]
let ``SiteMap buildSiteMap preserves graph structure`` () =
    let graph, ids = buildNested ()
    let a = ids.[0]
    let b = ids.[1]
    let siteMap, _ = ViewModel.SiteMapOps.buildSiteMap graph 0
    let root = siteMap.entries.[siteMap.rootId]
    Assert.Equal(2, root.children.Length)
    Assert.Equal(a, siteMap.entries.[root.children.[0]].nodeId)
    Assert.Equal(b, siteMap.entries.[root.children.[1]].nodeId)
    Assert.Equal(2, siteMap.entries.[root.children.[0]].children.Length)  // a has a1, a2
    Assert.Equal(1, siteMap.entries.[root.children.[1]].children.Length)  // b has b1

[<Fact>]
let ``SiteMap buildSiteMap cycle guard terminates and emits entry with no children`` () =
    // Build a cyclic graph: root -> A -> B -> A (back-edge)
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes ["a"; "b"] graph0
    let a = ids.[0]
    let b = ids.[1]
    let graph2 = Graph.replace graph1.root 0 [] [a] graph1 |> ModelBuilder.requireOk "root->a"
    let graph3 = Graph.replace a 0 [] [b] graph2 |> ModelBuilder.requireOk "a->b"
    let graph4 = Graph.replace b 0 [] [a] graph3 |> ModelBuilder.requireOk "b->a (cycle)"
    // Must terminate
    let siteMap, _ = ViewModel.SiteMapOps.buildSiteMap graph4 0
    // B's child entry for A (back-edge) should have children = []
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aEntry = siteMap.entries.[rootEntry.children.[0]]
    let bEntry = siteMap.entries.[aEntry.children.[0]]
    // bEntry's child is A (back-edge) — should have children = []
    let aBackEdgeEntry = siteMap.entries.[bEntry.children.[0]]
    Assert.Equal(a, aBackEdgeEntry.nodeId)
    Assert.Equal(0, aBackEdgeEntry.children.Length)

// ---------------------------------------------------------------------------
// SiteMapOps.reconcileSiteMap
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteMap reconcileSiteMap preserves instanceIds for unchanged nodes`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = ViewModel.SiteMapOps.buildSiteMap graph 0
    let rebuilt, nextId2 = ViewModel.SiteMapOps.reconcileSiteMap graph siteMap nextId
    // All instanceIds should be the same
    for KeyValue(instId, entry) in siteMap.entries do
        Assert.True(rebuilt.entries.ContainsKey instId)
        Assert.Equal(entry.nodeId, rebuilt.entries.[instId].nodeId)
    Assert.Equal(nextId, nextId2)  // No new IDs allocated

[<Fact>]
let ``SiteMap reconcileSiteMap preserves fold state`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = ViewModel.SiteMapOps.buildSiteMap graph 0
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    // Expand "a"
    let expanded = ViewModel.SiteMapOps.toggleFold aInstId siteMap
    Assert.True(expanded.entries.[aInstId].expanded)
    // Reconcile — fold state for "a" should survive
    let rebuilt, _ = ViewModel.SiteMapOps.reconcileSiteMap graph expanded nextId
    Assert.True(rebuilt.entries.[aInstId].expanded)
    Assert.False(rebuilt.entries.[rootEntry.children.[1]].expanded)  // "b" still collapsed

[<Fact>]
let ``SiteMap reconcileSiteMap assigns new IDs for added nodes`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = ViewModel.SiteMapOps.buildSiteMap graph 0
    // Add a new child under root
    let graph2, newNodeId = Graph.newNode "c" graph
    let rootChildren = graph2.nodes.[graph2.root].children
    let graph3 = Graph.replace graph2.root rootChildren.Length [] [newNodeId] graph2 |> ModelBuilder.requireOk "add c"
    let rebuilt, nextId2 = ViewModel.SiteMapOps.reconcileSiteMap graph3 siteMap nextId
    let rootEntry = rebuilt.entries.[rebuilt.rootId]
    Assert.Equal(3, rootEntry.children.Length)
    let newEntry = rebuilt.entries.[rootEntry.children.[2]]
    Assert.Equal(newNodeId, newEntry.nodeId)
    Assert.True(newEntry.instanceId >= nextId)
    Assert.Equal(nextId + 1, nextId2)

[<Fact>]
let ``SiteMap reconcileSiteMap two occurrences of same NodeId get distinct instanceIds`` () =
    // Build DAG: root -> [A, B], A -> [C], B -> [C]  (C shared)
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes ["a"; "b"; "c"] graph0
    let a = ids.[0]
    let b = ids.[1]
    let c = ids.[2]
    let graph2 = Graph.replace graph1.root 0 [] [a; b] graph1 |> ModelBuilder.requireOk "root"
    let graph3 = Graph.replace a 0 [] [c] graph2 |> ModelBuilder.requireOk "a->c"
    let graph4 = Graph.replace b 0 [] [c] graph3 |> ModelBuilder.requireOk "b->c"
    let siteMap, nextId = ViewModel.SiteMapOps.buildSiteMap graph4 0
    let rebuilt, _ = ViewModel.SiteMapOps.reconcileSiteMap graph4 siteMap nextId
    // Find the two occurrences of C in the rebuilt map
    let cEntries = rebuilt.entries |> Map.toList |> List.filter (fun (_, e) -> e.nodeId = c)
    Assert.Equal(2, cEntries.Length)
    let cIds = cEntries |> List.map fst
    Assert.Equal(2, cIds |> List.distinct |> List.length)  // distinct instanceIds

[<Fact>]
let ``SiteMap reconcileSiteMap two occurrences have independent fold state`` () =
    let graph0 = Graph.create ()
    // C has a child D so we can toggle fold on C
    let graph1, ids = ModelBuilder.createNodes ["a"; "b"; "c"; "d"] graph0
    let a = ids.[0]
    let b = ids.[1]
    let c = ids.[2]
    let d = ids.[3]
    let graph2 = Graph.replace graph1.root 0 [] [a; b] graph1 |> ModelBuilder.requireOk "root"
    let graph3 = Graph.replace a 0 [] [c] graph2 |> ModelBuilder.requireOk "a->c"
    let graph4 = Graph.replace b 0 [] [c] graph3 |> ModelBuilder.requireOk "b->c"
    let graph5 = Graph.replace c 0 [] [d] graph4 |> ModelBuilder.requireOk "c->d"
    let siteMap, _ = ViewModel.SiteMapOps.buildSiteMap graph5 0
    let occurrenceIndex = ViewModel.SiteMapOps.buildOccurrenceIndex siteMap
    let cInstIds = occurrenceIndex.[c]
    Assert.Equal(2, cInstIds.Length)
    // Toggle fold on only the first occurrence of C
    let siteMap2 = ViewModel.SiteMapOps.toggleFold cInstIds.[0] siteMap
    // One C is expanded, the other is not
    let e0 = siteMap2.entries.[cInstIds.[0]]
    let e1 = siteMap2.entries.[cInstIds.[1]]
    Assert.NotEqual(e0.expanded, e1.expanded)

// ---------------------------------------------------------------------------
// SiteMapOps.toggleFold
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteMap toggleFold flips expanded for matching instanceId`` () =
    let graph, _ = buildNested ()
    let siteMap, _ = ViewModel.SiteMapOps.buildSiteMap graph 0
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    Assert.False(siteMap.entries.[aInstId].expanded)
    let toggled = ViewModel.SiteMapOps.toggleFold aInstId siteMap
    Assert.True(toggled.entries.[aInstId].expanded)
    let toggledBack = ViewModel.SiteMapOps.toggleFold aInstId toggled
    Assert.False(toggledBack.entries.[aInstId].expanded)

[<Fact>]
let ``SiteMap toggleFold is no-op for unknown instanceId`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = ViewModel.SiteMapOps.buildSiteMap graph 0
    let result = ViewModel.SiteMapOps.toggleFold nextId siteMap  // nextId not in map
    Assert.Equal(siteMap.entries.Count, result.entries.Count)

// ---------------------------------------------------------------------------
// SiteMapOps.expandEntry
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteMap expandEntry sets expanded true`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = ViewModel.SiteMapOps.buildSiteMap graph 0
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    Assert.False(siteMap.entries.[aInstId].expanded)
    let expanded, _ = ViewModel.SiteMapOps.expandEntry aInstId graph siteMap nextId
    Assert.True(expanded.entries.[aInstId].expanded)

[<Fact>]
let ``SiteMap expandEntry on already-expanded entry is a no-op`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = ViewModel.SiteMapOps.buildSiteMap graph 0
    let rootId = siteMap.rootId
    // Root is already expanded
    let result, nextId2 = ViewModel.SiteMapOps.expandEntry rootId graph siteMap nextId
    Assert.Equal(nextId, nextId2)
    Assert.Equal(siteMap.entries.Count, result.entries.Count)

[<Fact>]
let ``SiteMap expandEntry inserts missing child entries`` () =
    // Start with a partially-built siteMap that does NOT have child entries for aInstId
    let graph, ids = buildNested ()
    let a = ids.[0]
    let siteMap, nextId = ViewModel.SiteMapOps.buildSiteMap graph 0
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    let aEntry = siteMap.entries.[aInstId]
    // Manually strip child entries from the map to simulate "missing" state
    let stripped = { siteMap with entries = siteMap.entries |> Map.add aInstId { aEntry with children = [] } }
    let expanded, _ = ViewModel.SiteMapOps.expandEntry aInstId graph stripped nextId
    let aExpanded = expanded.entries.[aInstId]
    Assert.True(aExpanded.expanded)
    // Children for a (a1, a2) should now be in the map
    Assert.Equal(2, aExpanded.children.Length)
    for childInstId in aExpanded.children do
        Assert.True(expanded.entries.ContainsKey childInstId)

// ---------------------------------------------------------------------------
// SiteMapOps.buildOccurrenceIndex
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteMap buildOccurrenceIndex maps each nodeId to its instanceIds`` () =
    let graph, ids = buildNested ()
    let siteMap, _ = ViewModel.SiteMapOps.buildSiteMap graph 0
    let index = ViewModel.SiteMapOps.buildOccurrenceIndex siteMap
    // Every node in the graph should appear in index exactly once (tree, no sharing)
    for nodeId in graph.nodes |> Map.toSeq |> Seq.map fst do
        Assert.True(index.ContainsKey nodeId)
        Assert.Equal(1, index.[nodeId].Length)

// ---------------------------------------------------------------------------
// SiteMapOps.getVisibleRowIds
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteMap getVisibleRowIds shows only top-level when all collapsed`` () =
    let graph, ids = buildNested ()
    let siteMap, _ = ViewModel.SiteMapOps.buildSiteMap graph 0
    let visible = ViewModel.SiteMapOps.getVisibleRowIds siteMap
    Assert.Equal(2, visible.Length)
    Assert.Equal(ids.[0], visible.[0])
    Assert.Equal(ids.[1], visible.[1])

[<Fact>]
let ``SiteMap getVisibleRowIds shows children of expanded node`` () =
    let graph, ids = buildNested ()
    let siteMap, _ = ViewModel.SiteMapOps.buildSiteMap graph 0
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    let expanded = ViewModel.SiteMapOps.toggleFold aInstId siteMap
    let visible = ViewModel.SiteMapOps.getVisibleRowIds expanded
    // a, a1, a2, b
    Assert.Equal(4, visible.Length)
    Assert.Equal(ids.[0], visible.[0])
    Assert.Equal(ids.[2], visible.[1])
    Assert.Equal(ids.[3], visible.[2])
    Assert.Equal(ids.[1], visible.[3])
