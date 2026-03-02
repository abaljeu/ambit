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
      siteRoot = siteRoot; nextInstanceId = nextId; clipboard = None }

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
