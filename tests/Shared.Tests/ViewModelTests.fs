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

/// Minimal VM helper — no selection, Selecting mode.
let emptyModel (graph: Graph) : VM =
    let siteMap, nextId = buildSiteMap graph 0

    { graph = graph
      revision = Revision.Zero
      history = History.empty
      selectedNodes = None
      mode = Selecting
      siteMap = siteMap
      nextInstanceId = nextId
      zoomRoot = None
      clipboard = None
      pendingChanges = []
      syncState = Synced }

/// VM with a selection covering [start, endd) in root's children, focus at focusIdx.
let modelWithSel (graph: Graph) (start: int) (endd: int) (focusIdx: int) : VM =
    let m = emptyModel graph

    { m with
        selectedNodes =
            Some
                { range =
                    { parent = m.siteMap.entries.[m.siteMap.rootId]
                      start = start
                      endd = endd }
                  focus = focusIdx } }

// ---------------------------------------------------------------------------
// singleSelection
// ---------------------------------------------------------------------------

[<Fact>]
let ``singleSelection returns Selection with focus equal to start`` () =
    let graph, ids = buildFlat [ "a"; "b"; "c" ]
    let nodeId = ids.[1] // "b", index 1 in root's children
    let siteMap, _ = buildSiteMap graph 0
    let result = singleSelection graph siteMap nodeId

    match result with
    | None -> Assert.True(false, "Expected Some, got None")
    | Some sel ->
        Assert.Equal(sel.range.start, sel.focus)
        Assert.Equal(1, sel.range.start)
        Assert.Equal(2, sel.range.endd)
        Assert.Equal(graph.root, sel.range.parent.nodeId)

[<Fact>]
let ``singleSelection returns None for root node`` () =
    let graph, _ = buildFlat [ "a" ]
    let siteMap, _ = buildSiteMap graph 0
    let result = singleSelection graph siteMap graph.root
    Assert.True(result.IsNone)

// ---------------------------------------------------------------------------
// shiftArrow — single node always extends
// ---------------------------------------------------------------------------

[<Fact>]
let ``shiftArrow +1 on single-node selection extends downward`` () =
    let graph, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph 0 1 0
    let result = shiftArrow 1 model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(0, sel.range.start)
        Assert.Equal(2, sel.range.endd)
        Assert.Equal(1, sel.focus) // focus at endd-1

[<Fact>]
let ``shiftArrow -1 on single-node selection extends upward`` () =
    let graph, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph 1 2 1
    let result = shiftArrow -1 model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(0, sel.range.start)
        Assert.Equal(2, sel.range.endd)
        Assert.Equal(0, sel.focus) // focus at new start

// ---------------------------------------------------------------------------
// shiftArrow — multi-node shrink / extend
// ---------------------------------------------------------------------------

[<Fact>]
let ``shiftArrow -1 with focus at start extends upward`` () =
    let graph, _ = buildFlat [ "a"; "b"; "c"; "d" ]
    let model = modelWithSel graph 1 3 1 // [1,3), focus at start=1
    let result = shiftArrow -1 model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(0, sel.range.start)
        Assert.Equal(3, sel.range.endd)
        Assert.Equal(0, sel.focus)

[<Fact>]
let ``shiftArrow -1 with focus at end shrinks from bottom`` () =
    let graph, _ = buildFlat [ "a"; "b"; "c"; "d" ]
    let model = modelWithSel graph 1 3 2 // [1,3), focus at endd-1=2
    let result = shiftArrow -1 model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(1, sel.range.start)
        Assert.Equal(2, sel.range.endd)
        Assert.Equal(1, sel.focus) // focus = new endd-1

[<Fact>]
let ``shiftArrow +1 with focus at end extends downward`` () =
    let graph, _ = buildFlat [ "a"; "b"; "c"; "d" ]
    let model = modelWithSel graph 1 3 2 // focus at endd-1=2
    let result = shiftArrow 1 model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(1, sel.range.start)
        Assert.Equal(4, sel.range.endd)
        Assert.Equal(3, sel.focus)

[<Fact>]
let ``shiftArrow +1 with focus at start shrinks from top`` () =
    let graph, _ = buildFlat [ "a"; "b"; "c"; "d" ]
    let model = modelWithSel graph 1 3 1 // focus at start=1
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
    let graph, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph 0 1 0
    let result = shiftArrow -1 model
    Assert.Equal(model.selectedNodes, result.selectedNodes)

[<Fact>]
let ``shiftArrow +1 is no-op when single node is at last index`` () =
    let graph, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph 2 3 2
    let result = shiftArrow 1 model
    Assert.Equal(model.selectedNodes, result.selectedNodes)

// ---------------------------------------------------------------------------
// applyMoveSelectionDown / applyMoveSelectionUp — multi-range focus moves
// ---------------------------------------------------------------------------

[<Fact>]
let ``applyMoveSelectionDown with 3-item range focus at start moves focus to endd-1`` () =
    let graph, _ = buildFlat [ "a"; "b"; "c"; "d"; "e" ]
    // range [1,4), focus at start=1
    let model = modelWithSel graph 1 4 1
    let result = applyMoveSelectionDown model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(1, sel.range.start)
        Assert.Equal(4, sel.range.endd)
        Assert.Equal(3, sel.focus) // moved to endd-1

[<Fact>]
let ``applyMoveSelectionUp with 3-item range focus at end moves focus to start`` () =
    let graph, _ = buildFlat [ "a"; "b"; "c"; "d"; "e" ]
    // range [1,4), focus at endd-1=3
    let model = modelWithSel graph 1 4 3
    let result = applyMoveSelectionUp model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(1, sel.range.start)
        Assert.Equal(4, sel.range.endd)
        Assert.Equal(1, sel.focus) // moved to start

// ---------------------------------------------------------------------------
// applyMoveSelectionDown — when focus is already at end, collapse and move
// ---------------------------------------------------------------------------

[<Fact>]
let ``applyMoveSelectionDown with focus at end collapses and moves down`` () =
    let graph, ids = buildFlat [ "a"; "b"; "c" ]
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
        Assert.Equal(1, sel.range.endd - sel.range.start) // single-node

// ---------------------------------------------------------------------------
// moveSelectionBy — advances visible row index
// ---------------------------------------------------------------------------

[<Fact>]
let ``moveSelectionBy 1 advances from first to second visible row`` () =
    let graph, ids = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph 0 1 0 // select first child
    let result = moveSelectionBy 1 model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        let gotId = focusedNodeId graph sel
        Assert.Equal(ids.[1], gotId)

[<Fact>]
let ``moveSelectionBy -1 moves back to previous row`` () =
    let graph, ids = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph 1 2 1 // select second child
    let result = moveSelectionBy -1 model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        let gotId = focusedNodeId graph sel
        Assert.Equal(ids.[0], gotId)

[<Fact>]
let ``moveSelectionBy 1 is no-op at last row`` () =
    let graph, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph 2 3 2 // select last child
    let result = moveSelectionBy 1 model
    Assert.Equal(model.selectedNodes, result.selectedNodes)

// ---------------------------------------------------------------------------
// buildSiteMap
// ---------------------------------------------------------------------------

/// Build a 2-level graph: root -> [a, b], a -> [a1, a2], b -> [b1]
let buildNested () : Graph * NodeId list =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "a"; "b"; "a1"; "a2"; "b1" ] graph0
    let a = ids.[0]
    let b = ids.[1]
    let a1 = ids.[2]
    let a2 = ids.[3]
    let b1 = ids.[4]

    let graph2 =
        Graph.replace graph1.root 0 [] [ a; b ] graph1
        |> ModelBuilder.requireOk "buildNested.root"

    let graph3 =
        Graph.replace a 0 [] [ a1; a2 ] graph2 |> ModelBuilder.requireOk "buildNested.a"

    let graph4 =
        Graph.replace b 0 [] [ b1 ] graph3 |> ModelBuilder.requireOk "buildNested.b"

    graph4, ids

[<Fact>]
let ``SiteMap buildSiteMap assigns unique instanceIds`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = buildSiteMap graph 0
    let allIds = siteMap.entries |> Map.toList |> List.map fst
    Assert.Equal(allIds.Length, allIds |> List.distinct |> List.length)
    // nextId should equal number of entries allocated
    Assert.Equal(siteMap.entries.Count, nextId)

[<Fact>]
let ``SiteMap buildSiteMap root is expanded, children collapsed`` () =
    let graph, _ = buildNested ()
    let siteMap, _ = buildSiteMap graph 0
    let root = siteMap.entries.[siteMap.rootId]
    Assert.True(root.expanded)

    for childInstId in root.children do
        Assert.False(siteMap.entries.[childInstId].expanded)

[<Fact>]
let ``SiteMap buildSiteMap root children have correct NodeIds and are stale`` () =
    let graph, ids = buildNested ()
    let a = ids.[0]
    let b = ids.[1]
    let siteMap, _ = buildSiteMap graph 0
    let root = siteMap.entries.[siteMap.rootId]
    Assert.Equal(2, root.children.Length)
    Assert.Equal(a, siteMap.entries.[root.children.[0]].nodeId)
    Assert.Equal(b, siteMap.entries.[root.children.[1]].nodeId)
    // Grandchildren not yet populated — children start collapsed and stale
    Assert.Equal(0, siteMap.entries.[root.children.[0]].children.Length)
    Assert.Equal(0, siteMap.entries.[root.children.[1]].children.Length)
    Assert.True(siteMap.entries.[root.children.[0]].childrenStale)
    Assert.True(siteMap.entries.[root.children.[1]].childrenStale)

[<Fact>]
let ``SiteMap buildSiteMap terminates on cyclic graph`` () =
    // Lazy expansion means no recursion in buildSiteMap — only root + direct children are created
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "a"; "b" ] graph0
    let a = ids.[0]
    let b = ids.[1]

    let graph2 =
        Graph.replace graph1.root 0 [] [ a ] graph1 |> ModelBuilder.requireOk "root->a"

    let graph3 = Graph.replace a 0 [] [ b ] graph2 |> ModelBuilder.requireOk "a->b"

    let graph4 =
        Graph.replace b 0 [] [ a ] graph3 |> ModelBuilder.requireOk "b->a (cycle)"
    // Must terminate — buildSiteMap only creates root + direct children, no recursion
    let siteMap, _ = buildSiteMap graph4 0
    Assert.Equal(2, siteMap.entries.Count) // root + a (collapsed, stale)
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aEntry = siteMap.entries.[rootEntry.children.[0]]
    Assert.Equal(a, aEntry.nodeId)
    Assert.False(aEntry.expanded)
    Assert.True(aEntry.childrenStale)
    // Expanding into a cycle: expand a → get b (collapsed); expand b → get a again (collapsed leaf)
    let siteMap2, nextId2 = expandEntry aEntry.instanceId graph4 siteMap 2
    let bInstId = siteMap2.entries.[aEntry.instanceId].children.[0]
    let siteMap3, _ = expandEntry bInstId graph4 siteMap2 nextId2
    // The back-edge 'a' entry is a new collapsed leaf — no infinite recursion
    let bEntry = siteMap3.entries.[bInstId]
    let aBackInstId = bEntry.children.[0]
    let aBackEntry = siteMap3.entries.[aBackInstId]
    Assert.Equal(a, aBackEntry.nodeId)
    Assert.False(aBackEntry.expanded) // started collapsed — recursion stops

// ---------------------------------------------------------------------------
// SiteMapOps.reconcileSiteMap
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteMap reconcileSiteMap preserves instanceIds for unchanged nodes`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = buildSiteMap graph 0
    let rebuilt, nextId2 = reconcileSiteMap graph siteMap nextId
    // All instanceIds should be the same
    for KeyValue(instId, entry) in siteMap.entries do
        Assert.True(rebuilt.entries.ContainsKey instId)
        Assert.Equal(entry.nodeId, rebuilt.entries.[instId].nodeId)

    Assert.Equal(nextId, nextId2) // No new IDs allocated

[<Fact>]
let ``SiteMap reconcileSiteMap preserves fold state`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = buildSiteMap graph 0
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    // Expand "a" using expandEntry
    let expanded, nextId' = expandEntry aInstId graph siteMap nextId
    Assert.True(expanded.entries.[aInstId].expanded)
    // Reconcile — fold state for "a" should survive
    let rebuilt, _ = reconcileSiteMap graph expanded nextId'
    Assert.True(rebuilt.entries.[aInstId].expanded)
    Assert.False(rebuilt.entries.[rootEntry.children.[1]].expanded) // "b" still collapsed

[<Fact>]
let ``SiteMap reconcileSiteMap assigns new IDs for added nodes`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = buildSiteMap graph 0
    // Add a new child under root
    let graph2, newNodeId = Graph.newNode "c" graph
    let rootChildren = graph2.nodes.[graph2.root].children

    let graph3 =
        Graph.replace graph2.root rootChildren.Length [] [ newNodeId ] graph2
        |> ModelBuilder.requireOk "add c"

    let rebuilt, nextId2 = reconcileSiteMap graph3 siteMap nextId
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
    let graph1, ids = ModelBuilder.createNodes [ "a"; "b"; "c" ] graph0
    let a = ids.[0]
    let b = ids.[1]
    let c = ids.[2]

    let graph2 =
        Graph.replace graph1.root 0 [] [ a; b ] graph1 |> ModelBuilder.requireOk "root"

    let graph3 = Graph.replace a 0 [] [ c ] graph2 |> ModelBuilder.requireOk "a->c"
    let graph4 = Graph.replace b 0 [] [ c ] graph3 |> ModelBuilder.requireOk "b->c"
    let siteMap, nextId = buildSiteMap graph4 0
    // Expand both A and B so C appears twice in the map
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    let bInstId = rootEntry.children.[1]
    let siteMap2, nextId2 = expandEntry aInstId graph4 siteMap nextId
    let siteMap3, nextId3 = expandEntry bInstId graph4 siteMap2 nextId2
    let rebuilt, _ = reconcileSiteMap graph4 siteMap3 nextId3
    // Find the two occurrences of C in the rebuilt map
    let cEntries =
        rebuilt.entries |> Map.toList |> List.filter (fun (_, e) -> e.nodeId = c)

    Assert.Equal(2, cEntries.Length)
    let cIds = cEntries |> List.map fst
    Assert.Equal(2, cIds |> List.distinct |> List.length) // distinct instanceIds

[<Fact>]
let ``SiteMap reconcileSiteMap two occurrences have independent fold state`` () =
    let graph0 = Graph.create ()
    // C has a child D so we can toggle fold on C
    let graph1, ids = ModelBuilder.createNodes [ "a"; "b"; "c"; "d" ] graph0
    let a = ids.[0]
    let b = ids.[1]
    let c = ids.[2]
    let d = ids.[3]

    let graph2 =
        Graph.replace graph1.root 0 [] [ a; b ] graph1 |> ModelBuilder.requireOk "root"

    let graph3 = Graph.replace a 0 [] [ c ] graph2 |> ModelBuilder.requireOk "a->c"
    let graph4 = Graph.replace b 0 [] [ c ] graph3 |> ModelBuilder.requireOk "b->c"
    let graph5 = Graph.replace c 0 [] [ d ] graph4 |> ModelBuilder.requireOk "c->d"
    let siteMap, nextId = buildSiteMap graph5 0
    // Expand both A and B so C appears twice in the map
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    let bInstId = rootEntry.children.[1]
    let siteMap2, nextId2 = expandEntry aInstId graph5 siteMap nextId
    let siteMap3, nextId3 = expandEntry bInstId graph5 siteMap2 nextId2
    let occurrenceIndex = buildOccurrenceIndex siteMap3
    let cInstIds = occurrenceIndex.[c]
    Assert.Equal(2, cInstIds.Length)
    // Expand only the first occurrence of C
    let siteMap4, _ = expandEntry cInstIds.[0] graph5 siteMap3 nextId3
    // One C is expanded, the other is not
    let e0 = siteMap4.entries.[cInstIds.[0]]
    let e1 = siteMap4.entries.[cInstIds.[1]]
    Assert.NotEqual(e0.expanded, e1.expanded)

// ---------------------------------------------------------------------------
// SiteMapOps.toggleFold
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteMap toggleFold collapses entry and marks children stale`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = buildSiteMap graph 0
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    // Expand "a" first
    let expanded, _ = expandEntry aInstId graph siteMap nextId
    Assert.True(expanded.entries.[aInstId].expanded)
    Assert.False(expanded.entries.[aInstId].childrenStale)
    // Collapse "a" via toggleFold
    let collapsed = toggleFold aInstId expanded
    Assert.False(collapsed.entries.[aInstId].expanded)
    Assert.True(collapsed.entries.[aInstId].childrenStale)
    // Children list is preserved (for positional reuse on re-expand)
    Assert.Equal<int list>(expanded.entries.[aInstId].children, collapsed.entries.[aInstId].children)

[<Fact>]
let ``SiteMap toggleFold is no-op for unknown instanceId`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = buildSiteMap graph 0
    let result = toggleFold nextId siteMap // nextId not in map
    Assert.Equal(siteMap.entries.Count, result.entries.Count)

// ---------------------------------------------------------------------------
// SiteMapOps.expandEntry
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteMap expandEntry sets expanded true`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = buildSiteMap graph 0
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    Assert.False(siteMap.entries.[aInstId].expanded)
    let expanded, _ = expandEntry aInstId graph siteMap nextId
    Assert.True(expanded.entries.[aInstId].expanded)

[<Fact>]
let ``SiteMap expandEntry on already-expanded entry is a no-op`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = buildSiteMap graph 0
    let rootId = siteMap.rootId
    // Root is already expanded
    let result, nextId2 = expandEntry rootId graph siteMap nextId
    Assert.Equal(nextId, nextId2)
    Assert.Equal(siteMap.entries.Count, result.entries.Count)

[<Fact>]
let ``SiteMap expandEntry inserts child entries`` () =
    // After buildSiteMap, "a" starts collapsed with children = [] (stale)
    let graph, _ = buildNested ()
    let siteMap, nextId = buildSiteMap graph 0
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    let expanded, _ = expandEntry aInstId graph siteMap nextId
    let aExpanded = expanded.entries.[aInstId]
    Assert.True(aExpanded.expanded)
    Assert.False(aExpanded.childrenStale)
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
    let siteMap, _ = buildSiteMap graph 0
    let index = buildOccurrenceIndex siteMap
    // After buildSiteMap only root + direct children (a, b) are in the map
    Assert.Equal(3, index.Count) // root, a, b
    Assert.True(index.ContainsKey graph.root)
    Assert.True(index.ContainsKey ids.[0]) // a
    Assert.True(index.ContainsKey ids.[1]) // b
    // Each appears exactly once
    for KeyValue(_, instIds) in index do
        Assert.Equal(1, instIds.Length)

// ---------------------------------------------------------------------------
// SiteMapOps.getVisibleRowIds
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteMap getVisibleRowIds shows only top-level when all collapsed`` () =
    let graph, ids = buildNested ()
    let siteMap, _ = buildSiteMap graph 0
    let visible = getVisibleRowIds siteMap
    Assert.Equal(2, visible.Length)
    Assert.Equal(ids.[0], visible.[0])
    Assert.Equal(ids.[1], visible.[1])

[<Fact>]
let ``SiteMap getVisibleRowIds shows children of expanded node`` () =
    let graph, ids = buildNested ()
    let siteMap, nextId = buildSiteMap graph 0
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    let expanded, _ = expandEntry aInstId graph siteMap nextId
    let visible = getVisibleRowIds expanded
    // a, a1, a2, b
    Assert.Equal(4, visible.Length)
    Assert.Equal(ids.[0], visible.[0])
    Assert.Equal(ids.[2], visible.[1])
    Assert.Equal(ids.[3], visible.[2])
    Assert.Equal(ids.[1], visible.[3])

// ---------------------------------------------------------------------------
// planPatchDOM
// ---------------------------------------------------------------------------

/// Build a cache set containing all currently visible instanceIds.
let buildCacheSet (siteMap: SiteMap) : Set<int> =
    getVisibleInstanceIds siteMap |> Set.ofList

[<Fact>]
let ``planPatchDOM text change produces SetText patch and no CreateRow`` () =
    let graph, ids = buildFlat [ "a"; "b"; "c" ]
    let oldModel = emptyModel graph
    // Change the text of the second node
    let targetId = ids.[1]

    let newNode =
        { graph.nodes.[targetId] with
            text = "b-edited" }

    let newGraph =
        { graph with
            nodes = Map.add targetId newNode graph.nodes }

    let newModel = { oldModel with graph = newGraph }
    let cachedInstIds = buildCacheSet oldModel.siteMap

    let mutations = planPatchDOM oldModel newModel cachedInstIds

    let textPatches =
        mutations
        |> List.collect (fun m ->
            match m with
            | PatchRow(_, patches) ->
                patches
                |> List.filter (function
                    | SetText _ -> true
                    | _ -> false)
            | _ -> [])

    let creates =
        mutations
        |> List.filter (function
            | CreateRow _ -> true
            | _ -> false)

    Assert.Equal(1, textPatches.Length) // exactly K=1 DOM text update
    Assert.Equal(0, creates.Length) // no new elements created

[<Fact>]
let ``planPatchDOM expand inserts correct child count`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = buildSiteMap graph 0
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0] // "a" has 2 children: a1, a2

    let oldModel = emptyModel graph // "a" collapsed
    // Expand "a" in the new model
    let newSiteMap, newNextId = expandEntry aInstId graph siteMap nextId

    let newModel =
        { oldModel with
            siteMap = newSiteMap
            nextInstanceId = newNextId }

    let cachedInstIds = buildCacheSet oldModel.siteMap

    let mutations = planPatchDOM oldModel newModel cachedInstIds

    let creates =
        mutations
        |> List.filter (function
            | CreateRow _ -> true
            | _ -> false)

    Assert.Equal(2, creates.Length) // a1 and a2

[<Fact>]
let ``planPatchDOM collapse removes stale cache entries`` () =
    let graph, _ = buildNested ()
    let siteMap, nextId = buildSiteMap graph 0
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]

    // Start with "a" expanded
    let expandedSiteMap, newNextId = expandEntry aInstId graph siteMap nextId

    let oldModel =
        { emptyModel graph with
            siteMap = expandedSiteMap
            nextInstanceId = newNextId }
    // Collapse "a" for the new model
    let collapsedSiteMap = toggleFold aInstId expandedSiteMap

    let newModel =
        { oldModel with
            siteMap = collapsedSiteMap }

    let cachedInstIds = buildCacheSet oldModel.siteMap

    let mutations = planPatchDOM oldModel newModel cachedInstIds

    let removes =
        mutations
        |> List.filter (function
            | RemoveRow _ -> true
            | _ -> false)

    Assert.Equal(2, removes.Length) // a1 and a2 evicted
