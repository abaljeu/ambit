module ViewModelTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open SpecialNodeTestHelpers
open Xunit

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

// ---------------------------------------------------------------------------
// Test helpers
// ---------------------------------------------------------------------------

let private assertParentIndexMatchesEntries (siteMap: SiteMap) =
    siteMap.entries
    |> Map.iter (fun _ e ->
        if e.instanceId <> siteMap.rootId then
            match e.parentInstanceId with
            | None -> Assert.True(false, "non-root entry must have parentInstanceId")
            | Some p ->
                Assert.Equal(Some p, Map.tryFind e.instanceId siteMap.parentByInstanceId))

/// Build a flat graph: root -> container -> ids.
let buildFlat (texts: string list) : Graph * NodeId * NodeId list =
    let graph0 = Graph.create ()
    let graph1, contIds = ModelBuilder.createNodes [ "container" ] graph0
    let cont = contIds.[0]
    let graph2, ids = ModelBuilder.createNodes texts graph1
    let graph3 =
        Graph.replace graph2.root 0 [] (owned [ cont ]) graph2
        |> ModelBuilder.requireOk "buildFlat.root"
    let graph4 =
        Graph.replace cont 0 [] (owned ids) graph3
        |> ModelBuilder.requireOk "buildFlat.cont"
    graph4, cont, ids

/// Minimal VM helper — no selection, Selecting mode.
let emptyModel (graph: Graph) : VM =
    let siteMap, nextId = buildSiteMap graph

    { graph = graph
      revision = Revision.Zero
      history = History.empty
      selectedNodes = None
      mode = Selecting
      siteMap = siteMap
      nextSiteId = nextId
      zoomRoot = graph.root
      clipboard = None
      desktopCapabilities = None
      desktopFileIndicator = BlankFileIndicator
      syncInfo = SyncInfo.initial
      lastSuccessfulKey = ""
      lastSuccessfulOp = "" }

/// VM scoped to viewRoot as the display root (siteMap built from viewRoot).
let emptyModelAt (graph: Graph) (viewRoot: NodeId) : VM =
    let siteMap, nextId = buildSiteMapFrom graph viewRoot (Sid 0)

    { graph = graph
      revision = Revision.Zero
      history = History.empty
      selectedNodes = None
      mode = Selecting
      siteMap = siteMap
      nextSiteId = nextId
      zoomRoot = viewRoot
      clipboard = None
      desktopCapabilities = None
      desktopFileIndicator = BlankFileIndicator
      syncInfo = SyncInfo.initial
      lastSuccessfulKey = ""
      lastSuccessfulOp = "" }

/// VM with a selection covering [start, endd) in parentNodeId's children, focus at focusIdx.
let modelWithSel (graph: Graph) (parentNodeId: NodeId) (start: int) (endd: int) (focusIdx: int) : VM =
    let m = emptyModelAt graph parentNodeId

    { m with
        selectedNodes =
            Some
                { range =
                    { parent = m.siteMap.entries.[m.siteMap.rootId]
                      start = start
                      endd = endd }
                  focus = focusIdx } }

let private withDesktop (model: VM) : VM =
    { model with desktopCapabilities = Some DesktopCapabilities.disabled }

let private selectedModelWithText (text: string) : VM =
    let graph, cont, _ = buildFlat [ text ]
    modelWithSel graph cont 0 1 0

// ---------------------------------------------------------------------------
// desktop file indicator
// ---------------------------------------------------------------------------

[<Fact>]
let ``refreshDesktopFileIndicator leaves blank when active node has no reference`` () =
    let model = selectedModelWithText "plain node" |> withDesktop
    let refreshed, effects = refreshDesktopFileIndicator model

    Assert.Equal(BlankFileIndicator, refreshed.desktopFileIndicator)
    Assert.Empty(effects)

[<Fact>]
let ``refreshDesktopFileIndicator marks invalid reference without desktop effect`` () =
    let model = selectedModelWithText "broken [[reference" |> withDesktop
    let refreshed, effects = refreshDesktopFileIndicator model

    Assert.Equal(InvalidFileReferenceIndicator, refreshed.desktopFileIndicator)
    Assert.Empty(effects)

[<Fact>]
let ``refreshDesktopFileIndicator requests status for valid active reference`` () =
    let model = selectedModelWithText "load [[note.txt]]" |> withDesktop
    let refreshed, effects = refreshDesktopFileIndicator model
    let nodeId = focusedNodeId refreshed.graph refreshed.selectedNodes.Value

    Assert.Equal(CheckingFileStatus (nodeId, "note.txt"), refreshed.desktopFileIndicator)
    Assert.Equal<Effect list>([ RequestDesktopFileStatus (nodeId, "note.txt") ], effects)

[<Fact>]
let ``refreshDesktopFileIndicator does not repeat matching status request`` () =
    let model = selectedModelWithText "load [[note.txt]]" |> withDesktop
    let checkedModel, _ = refreshDesktopFileIndicator model
    let refreshed, effects = refreshDesktopFileIndicator checkedModel

    Assert.Equal(checkedModel.desktopFileIndicator, refreshed.desktopFileIndicator)
    Assert.Empty(effects)

[<Fact>]
let ``applyDesktopFileStatus ignores stale active node response`` () =
    let graph, cont, ids = buildFlat [ "load [[a.txt]]"; "load [[b.txt]]" ]
    let model = modelWithSel graph cont 1 2 1
    let stale = ids.[0]
    let updated = applyDesktopFileStatus stale "a.txt" ExistingFile model

    Assert.Equal(BlankFileIndicator, updated.desktopFileIndicator)

[<Fact>]
let ``desktopFileIndicatorText shows status on active row only`` () =
    let model = selectedModelWithText "load [[note.txt]]" |> withDesktop
    let checking, _ = refreshDesktopFileIndicator model
    let nodeId = focusedNodeId checking.graph checking.selectedNodes.Value
    let checkedModel = applyDesktopFileStatus nodeId "note.txt" ExistingFile checking
    let activeEntry = checkedModel.siteMap.entries.[checkedModel.selectedNodes.Value.range.parent.children.[0]]
    let rootEntry = checkedModel.siteMap.entries.[checkedModel.siteMap.rootId]

    Assert.Equal("file", desktopFileIndicatorText checkedModel activeEntry)
    Assert.Equal("", desktopFileIndicatorText checkedModel rootEntry)

// ---------------------------------------------------------------------------
// singleSelection
// ---------------------------------------------------------------------------

[<Fact>]
let ``singleSelection returns Selection with focus equal to start`` () =
    let graph, cont, ids = buildFlat [ "a"; "b"; "c" ]
    let nodeId = ids.[1] // "b", index 1 in cont's children
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let result = singleSelection graph siteMap nodeId

    match result with
    | None -> Assert.True(false, "Expected Some, got None")
    | Some sel ->
        Assert.Equal(sel.range.start, sel.focus)
        Assert.Equal(1, sel.range.start)
        Assert.Equal(2, sel.range.endd)
        Assert.Equal(cont, sel.range.parent.nodeId)

[<Fact>]
let ``singleSelection returns None for root node`` () =
    let graph, cont, _ = buildFlat [ "a" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let result = singleSelection graph siteMap graph.root
    Assert.True(result.IsNone)

[<Fact>]
let ``refreshSelection rebinds stale parent snapshot to current site map`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 1 2 1
    let staleSel =
        match model.selectedNodes with
        | None -> failwith "Expected selected node"
        | Some sel ->
            let staleParent = { sel.range.parent with children = [] }
            { sel with range = { sel.range with parent = staleParent } }
    let rebuiltMap, _ = buildSiteMapFrom graph cont (Sid 0)
    match refreshSelection graph rebuiltMap staleSel with
    | None -> Assert.True(false, "Expected refreshed selection")
    | Some refreshed ->
        Assert.Equal(1, refreshed.range.start)
        Assert.Equal(2, refreshed.range.endd)
        Assert.Equal(3, refreshed.range.parent.children.Length)

[<Fact>]
let ``refreshSelection returns None when parent instance no longer exists`` () =
    let graph, cont, _ = buildFlat [ "a"; "b" ]
    let model = modelWithSel graph cont 0 1 0
    let staleSel =
        match model.selectedNodes with
        | None -> failwith "Expected selected node"
        | Some sel ->
            let orphanParent = { sel.range.parent with instanceId = Sid 9_999 }
            { sel with range = { sel.range with parent = orphanParent } }
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    Assert.True((refreshSelection graph siteMap staleSel).IsNone)

[<Fact>]
let ``selectionAfterStructuralMove expanded parent spans moved nodes`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let m0 = emptyModelAt graph cont
    let rootEntry = m0.siteMap.entries.[m0.siteMap.rootId]
    let expandedRoot = { rootEntry with expanded = true }
    let fromParent = m0.siteMap.entries.[m0.siteMap.rootId]
    let sel =
        selectionAfterStructuralMove graph graph m0.siteMap
            { parent = fromParent; start = 0; endd = 2 }
            expandedRoot
            1
            2
            1

    Assert.Equal(1, sel.range.start)
    Assert.Equal(3, sel.range.endd)
    Assert.Equal(2, sel.focus)

[<Fact>]
let ``selectionAfterStructuralMove collapsed parent picks original sibling above`` () =
    let graphPre, cont, ids = buildFlat [ "a"; "b"; "c" ]
    let a = ids.[0]
    let b = ids.[1]
    let bChild = graphPre.nodes.[cont].children.[1]
    let gMid =
        Graph.replace cont 1 [ bChild ] [] graphPre |> ModelBuilder.requireOk "rm b"
    let gPost =
        Graph.replace a 0 [] [ bChild ] gMid |> ModelBuilder.requireOk "add b under a"
    let siteMap, _ = buildSiteMapFrom gPost cont (Sid 0)
    let mPre = emptyModelAt graphPre cont
    let rootPre = mPre.siteMap.entries.[mPre.siteMap.rootId]

    let newParent =
        siteMap.entries
        |> Map.pick (fun _ e -> if e.nodeId = a then Some e else None)

    let sel =
        selectionAfterStructuralMove graphPre gPost siteMap
            { parent = rootPre; start = 1; endd = 2 }
            newParent
            0
            1
            0

    Assert.Equal(a, focusedNodeId gPost sel)

[<Fact>]
let ``selectionAfterStructuralMove expanded newParent focuses moved node indent topology`` () =
    let graphPre, cont, ids = buildFlat [ "a"; "b"; "c" ]
    let a = ids.[0]
    let b = ids.[1]
    let bChild = graphPre.nodes.[cont].children.[1]
    let gMid =
        Graph.replace cont 1 [ bChild ] [] graphPre |> ModelBuilder.requireOk "rm b"
    let gPost =
        Graph.replace a 0 [] [ bChild ] gMid |> ModelBuilder.requireOk "add b under a"
    let siteMap, _ = buildSiteMapFrom gPost cont (Sid 0)
    let mPre = emptyModelAt graphPre cont
    let rootPre = mPre.siteMap.entries.[mPre.siteMap.rootId]

    let newParentCollapsed =
        siteMap.entries |> Map.pick (fun _ e -> if e.nodeId = a then Some e else None)
    let newParentExpanded = { newParentCollapsed with expanded = true }

    let sel =
        selectionAfterStructuralMove graphPre gPost siteMap
            { parent = rootPre; start = 1; endd = 2 }
            newParentExpanded
            0
            1
            0

    Assert.Equal(b, focusedNodeId gPost sel)

[<Fact>]
let ``selectionAfterStructuralMove collapsed parent picks original sibling below at range start`` () =
    let graphPre, cont, ids = buildFlat [ "a"; "b"; "c" ]
    let a = ids.[0]
    let b = ids.[1]
    let aChild = graphPre.nodes.[cont].children.[0]
    let gMid =
        Graph.replace cont 0 [ aChild ] [] graphPre |> ModelBuilder.requireOk "rm a"
    let gPost =
        Graph.replace b 0 [] [ aChild ] gMid |> ModelBuilder.requireOk "add a under b"
    let siteMap, _ = buildSiteMapFrom gPost cont (Sid 0)
    let mPre = emptyModelAt graphPre cont
    let rootPre = mPre.siteMap.entries.[mPre.siteMap.rootId]

    let newParent =
        siteMap.entries |> Map.pick (fun _ e -> if e.nodeId = b then Some e else None)

    let sel =
        selectionAfterStructuralMove graphPre gPost siteMap
            { parent = rootPre; start = 0; endd = 1 }
            newParent
            0
            1
            0

    Assert.Equal(b, focusedNodeId gPost sel)

// ---------------------------------------------------------------------------
// shiftArrow — single node always extends
// ---------------------------------------------------------------------------

[<Fact>]
let ``shiftArrow +1 on single-node selection extends downward`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 0 1 0
    let result = shiftArrow 1 model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(0, sel.range.start)
        Assert.Equal(2, sel.range.endd)
        Assert.Equal(1, sel.focus) // focus at endd-1

[<Fact>]
let ``shiftArrow -1 on single-node selection extends upward`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 1 2 1
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
    let graph, cont, _ = buildFlat [ "a"; "b"; "c"; "d" ]
    let model = modelWithSel graph cont 1 3 1 // [1,3), focus at start=1
    let result = shiftArrow -1 model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(0, sel.range.start)
        Assert.Equal(3, sel.range.endd)
        Assert.Equal(0, sel.focus)

[<Fact>]
let ``shiftArrow -1 with focus at end shrinks from bottom`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c"; "d" ]
    let model = modelWithSel graph cont 1 3 2 // [1,3), focus at endd-1=2
    let result = shiftArrow -1 model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(1, sel.range.start)
        Assert.Equal(2, sel.range.endd)
        Assert.Equal(1, sel.focus) // focus = new endd-1

[<Fact>]
let ``shiftArrow +1 with focus at end extends downward`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c"; "d" ]
    let model = modelWithSel graph cont 1 3 2 // focus at endd-1=2
    let result = shiftArrow 1 model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(1, sel.range.start)
        Assert.Equal(4, sel.range.endd)
        Assert.Equal(3, sel.focus)

[<Fact>]
let ``shiftArrow +1 with focus at start shrinks from top`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c"; "d" ]
    let model = modelWithSel graph cont 1 3 1 // focus at start=1
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
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 0 1 0
    let result = shiftArrow -1 model
    Assert.Equal(model.selectedNodes, result.selectedNodes)

[<Fact>]
let ``shiftArrow +1 is no-op when single node is at last index`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 2 3 2
    let result = shiftArrow 1 model
    Assert.Equal(model.selectedNodes, result.selectedNodes)

// ---------------------------------------------------------------------------
// collapseToFocus
// ---------------------------------------------------------------------------

[<Fact>]
let ``collapseToFocus narrows multi-node selection to focused row`` () =
    let graph, cont, ids = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 0 2 1
    let result = collapseToFocus model

    match result.selectedNodes with
    | None -> Assert.True(false, "expected selection")
    | Some sel ->
        Assert.Equal(1, sel.range.start)
        Assert.Equal(2, sel.range.endd)
        Assert.Equal(1, sel.focus)
        Assert.Equal(ids.[1], focusedNodeId graph sel)

// ---------------------------------------------------------------------------
// applyMoveSelectionDown / applyMoveSelectionUp — multi-range focus moves
// ---------------------------------------------------------------------------

[<Fact>]
let ``applyMoveSelectionDown with 3-item range focus at start moves focus to endd-1`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c"; "d"; "e" ]
    // range [1,4), focus at start=1
    let model = modelWithSel graph cont 1 4 1
    let result = applyMoveSelectionDown model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        Assert.Equal(1, sel.range.start)
        Assert.Equal(4, sel.range.endd)
        Assert.Equal(3, sel.focus) // moved to endd-1

[<Fact>]
let ``applyMoveSelectionUp with 3-item range focus at end moves focus to start`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c"; "d"; "e" ]
    // range [1,4), focus at endd-1=3
    let model = modelWithSel graph cont 1 4 3
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
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    // single-node at index 0, focus also at 0
    let model = modelWithSel graph cont 0 1 0
    let result = applyMoveSelectionDown model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        // Should have moved to ids.[1]
        let expectedId = graph.nodes.[cont].children.[1].id
        let gotId = graph.nodes.[sel.range.parent.nodeId].children.[sel.focus].id
        Assert.Equal(expectedId, gotId)
        Assert.Equal(1, sel.range.endd - sel.range.start) // single-node

// ---------------------------------------------------------------------------
// moveSelectionBy — advances visible row index
// ---------------------------------------------------------------------------

[<Fact>]
let ``moveSelectionBy 1 advances from first to second visible row`` () =
    let graph, cont, ids = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 0 1 0 // select first child
    let result = moveSelectionBy 1 model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        let gotId = focusedNodeId graph sel
        Assert.Equal(ids.[1], gotId)

[<Fact>]
let ``moveSelectionBy -1 moves back to previous row`` () =
    let graph, cont, ids = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 1 2 1 // select second child
    let result = moveSelectionBy -1 model

    match result.selectedNodes with
    | None -> Assert.True(false, "Expected Some")
    | Some sel ->
        let gotId = focusedNodeId graph sel
        Assert.Equal(ids.[0], gotId)

[<Fact>]
let ``moveSelectionBy 1 is no-op at last row`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 2 3 2 // select last child
    let result = moveSelectionBy 1 model
    Assert.Equal(model.selectedNodes, result.selectedNodes)

// ---------------------------------------------------------------------------
// buildSiteMap
// ---------------------------------------------------------------------------

/// Build a 2-level graph: root -> container -> [a, b], a -> [a1, a2], b -> [b1].
let buildNested () : Graph * NodeId * NodeId list =
    let graph0 = Graph.create ()
    let graph1, contIds = ModelBuilder.createNodes [ "container" ] graph0
    let cont = contIds.[0]
    let graph2, ids = ModelBuilder.createNodes [ "a"; "b"; "a1"; "a2"; "b1" ] graph1
    let a = ids.[0]
    let b = ids.[1]
    let a1 = ids.[2]
    let a2 = ids.[3]
    let b1 = ids.[4]
    let graph3 =
        Graph.replace graph2.root 0 [] (owned [ cont ]) graph2
        |> ModelBuilder.requireOk "buildNested.root"
    let graph4 =
        Graph.replace cont 0 [] (owned [ a; b ]) graph3
        |> ModelBuilder.requireOk "buildNested.cont"
    let graph5 =
        Graph.replace a 0 [] (owned [ a1; a2 ]) graph4
        |> ModelBuilder.requireOk "buildNested.a"
    let graph6 =
        Graph.replace b 0 [] (owned [ b1 ]) graph5
        |> ModelBuilder.requireOk "buildNested.b"
    graph6, cont, ids

// ---------------------------------------------------------------------------
// SiteMap.siteParent
// ---------------------------------------------------------------------------

[<Fact>]
let ``siteParent None returns None`` () =
    let graph, cont, _ = buildFlat [ "a" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    Assert.True(SiteMap.siteParent siteMap None |> Option.isNone)

[<Fact>]
let ``siteParent at root returns None`` () =
    let graph, cont, _ = buildFlat [ "a" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let rootId = siteMap.rootId
    Assert.True(SiteMap.siteParent siteMap (Some rootId) |> Option.isNone)

[<Fact>]
let ``siteParent of child is Some root instanceId`` () =
    let graph, cont, _ = buildFlat [ "a"; "b" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let childInst = rootEntry.children.[0]
    let parentOpt = SiteMap.siteParent siteMap (Some childInst)
    Assert.Equal(Some siteMap.rootId, parentOpt)

[<Fact>]
let ``siteParent composed twice matches two explicit steps`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let aInstId = siteMap.entries.[siteMap.rootId].children.[0]
    let sm2, nextId2 = expandEntry aInstId graph siteMap nextId
    let a1InstId = sm2.entries.[aInstId].children.[0]
    let p = SiteMap.siteParent sm2
    let viaCompose = (p >> p) (Some a1InstId)
    let viaExplicit = SiteMap.siteParent sm2 (SiteMap.siteParent sm2 (Some a1InstId))
    Assert.Equal(viaExplicit, viaCompose)

[<Fact>]
let ``siteParent composed twice from None stays None`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let aInstId = siteMap.entries.[siteMap.rootId].children.[0]
    let sm2, _ = expandEntry aInstId graph siteMap nextId
    let p = SiteMap.siteParent sm2
    Assert.True((p >> p) None |> Option.isNone)

[<Fact>]
let ``SiteMap parentByInstanceId matches entries after build and expand`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    assertParentIndexMatchesEntries siteMap
    let aInstId = siteMap.entries.[siteMap.rootId].children.[0]
    let sm2, _ = expandEntry aInstId graph siteMap nextId
    assertParentIndexMatchesEntries sm2

// ---------------------------------------------------------------------------
// SiteMap.siteFirstChild / siteLastChild / siteNext / sitePrev
// ---------------------------------------------------------------------------

[<Fact>]
let ``siteFirstChild None returns None`` () =
    let graph, cont, _ = buildFlat [ "a" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    Assert.True(SiteMap.siteFirstChild siteMap None |> Option.isNone)

[<Fact>]
let ``siteFirstChild on root returns first child instance`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let root = siteMap.rootId
    let expected = Some siteMap.entries.[root].children.[0]
    Assert.Equal(expected, SiteMap.siteFirstChild siteMap (Some root))

[<Fact>]
let ``siteFirstChild on leaf with empty children returns None`` () =
    let graph, cont, _ = buildFlat [ "a" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let childInst = siteMap.entries.[siteMap.rootId].children.[0]
    Assert.True(SiteMap.siteFirstChild siteMap (Some childInst) |> Option.isNone)

[<Fact>]
let ``siteLastChild on root returns last child instance`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let root = siteMap.rootId
    let n = siteMap.entries.[root].children.Length
    let expected = Some siteMap.entries.[root].children.[n - 1]
    Assert.Equal(expected, SiteMap.siteLastChild siteMap (Some root))

[<Fact>]
let ``siteLastChild on empty children returns None`` () =
    let graph, cont, _ = buildFlat [ "a" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let childInst = siteMap.entries.[siteMap.rootId].children.[0]
    Assert.True(SiteMap.siteLastChild siteMap (Some childInst) |> Option.isNone)

// ---------------------------------------------------------------------------
// SiteMap.siteChildIndex
// ---------------------------------------------------------------------------

[<Fact>]
let ``siteChildIndex None parent or child returns None`` () =
    let graph, cont, _ = buildFlat [ "a"; "b" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let root = siteMap.rootId
    let c0 = siteMap.entries.[root].children.[0]
    Assert.True(SiteMap.siteChildIndex siteMap None (Some c0) |> Option.isNone)
    Assert.True(SiteMap.siteChildIndex siteMap (Some root) None |> Option.isNone)

[<Fact>]
let ``siteChildIndex matches child position under root`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let root = siteMap.rootId
    let c = siteMap.entries.[root].children
    Assert.Equal(Some 0, SiteMap.siteChildIndex siteMap (Some root) (Some c.[0]))
    Assert.Equal(Some 1, SiteMap.siteChildIndex siteMap (Some root) (Some c.[1]))
    Assert.Equal(Some 2, SiteMap.siteChildIndex siteMap (Some root) (Some c.[2]))

[<Fact>]
let ``siteChildIndex not a child of parent returns None`` () =
    let graph, cont, _ = buildFlat [ "a"; "b" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let root = siteMap.rootId
    let c = siteMap.entries.[root].children
    Assert.True(SiteMap.siteChildIndex siteMap (Some c.[0]) (Some c.[1]) |> Option.isNone)

[<Fact>]
let ``siteChildIndex under expanded node matches a1 a2 order`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let aInst = siteMap.entries.[siteMap.rootId].children.[0]
    let sm2, _ = expandEntry aInst graph siteMap nextId
    let a1 = sm2.entries.[aInst].children.[0]
    let a2 = sm2.entries.[aInst].children.[1]
    Assert.Equal(Some 0, SiteMap.siteChildIndex sm2 (Some aInst) (Some a1))
    Assert.Equal(Some 1, SiteMap.siteChildIndex sm2 (Some aInst) (Some a2))

[<Fact>]
let ``siteChildIndex on unknown parent instanceId returns None`` () =
    let graph, cont, _ = buildFlat [ "a" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let root = siteMap.rootId
    let c0 = siteMap.entries.[root].children.[0]
    let bogus = Sid 999_999
    Assert.True(SiteMap.siteChildIndex siteMap (Some bogus) (Some c0) |> Option.isNone)

[<Fact>]
let ``siteFirstChild after expand returns first grandchild instance`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let aInst = siteMap.entries.[siteMap.rootId].children.[0]
    let sm2, _ = expandEntry aInst graph siteMap nextId
    let a1 = Some sm2.entries.[aInst].children.[0]
    Assert.Equal(a1, SiteMap.siteFirstChild sm2 (Some aInst))

[<Fact>]
let ``siteNext moves to next root child`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let c = siteMap.entries.[siteMap.rootId].children
    Assert.Equal(Some c.[1], SiteMap.siteNext siteMap (Some c.[0]))
    Assert.Equal(Some c.[2], SiteMap.siteNext siteMap (Some c.[1]))

[<Fact>]
let ``siteNext on last sibling returns None`` () =
    let graph, cont, _ = buildFlat [ "a"; "b" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let last = siteMap.entries.[siteMap.rootId].children.[1]
    Assert.True(SiteMap.siteNext siteMap (Some last) |> Option.isNone)

[<Fact>]
let ``sitePrev moves to previous root child`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let c = siteMap.entries.[siteMap.rootId].children
    Assert.Equal(Some c.[0], SiteMap.sitePrev siteMap (Some c.[1]))
    Assert.Equal(Some c.[1], SiteMap.sitePrev siteMap (Some c.[2]))

[<Fact>]
let ``sitePrev on first sibling returns None`` () =
    let graph, cont, _ = buildFlat [ "a"; "b" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let first = siteMap.entries.[siteMap.rootId].children.[0]
    Assert.True(SiteMap.sitePrev siteMap (Some first) |> Option.isNone)

[<Fact>]
let ``siteNext and sitePrev on root return None`` () =
    let graph, cont, _ = buildFlat [ "a"; "b" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let root = Some siteMap.rootId
    Assert.True(SiteMap.siteNext siteMap root |> Option.isNone)
    Assert.True(SiteMap.sitePrev siteMap root |> Option.isNone)

[<Fact>]
let ``siteNext composed from first reaches last`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let c = siteMap.entries.[siteMap.rootId].children
    let n = SiteMap.siteNext siteMap
    Assert.Equal(Some c.[2], (n >> n) (Some c.[0]))

[<Fact>]
let ``sitePrev composed from last reaches first`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let c = siteMap.entries.[siteMap.rootId].children
    let p = SiteMap.sitePrev siteMap
    Assert.Equal(Some c.[0], (p >> p) (Some c.[2]))

// ---------------------------------------------------------------------------
// SiteNav — carry (SiteMap, SiteId option); compose steps with >>
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteNav parent twice matches explicit siteParent chain`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let aInstId = siteMap.entries.[siteMap.rootId].children.[0]
    let sm2, _ = expandEntry aInstId graph siteMap nextId
    let a1InstId = sm2.entries.[aInstId].children.[0]
    let via = Site.at sm2 (Some a1InstId) |> (Site.parent >> Site.parent) |> Site.current
    let viaExplicit = SiteMap.siteParent sm2 (SiteMap.siteParent sm2 (Some a1InstId))
    Assert.Equal(viaExplicit, via)

[<Fact>]
let ``SiteNav prevCousin from first root branch grandchild is None`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let aInst = siteMap.entries.[siteMap.rootId].children.[0]
    let sm2, _ = expandEntry aInst graph siteMap nextId
    let a1Inst = sm2.entries.[aInst].children.[0]
    let prevCousin = Site.parent >> Site.prev >> Site.lastChild
    let result = Site.at sm2 (Some a1Inst) |> prevCousin |> Site.current
    Assert.True(result |> Option.isNone)

[<Fact>]
let ``SiteNav prevCousin from second root branch child is last grandchild of first branch`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let root = siteMap.rootId
    let aInst = siteMap.entries.[root].children.[0]
    let bInst = siteMap.entries.[root].children.[1]
    let sm2, nextId2 = expandEntry aInst graph siteMap nextId
    let sm3, _ = expandEntry bInst graph sm2 nextId2
    let b1Inst = sm3.entries.[bInst].children.[0]
    let a2Inst = sm3.entries.[aInst].children.[1]
    let prevCousin = Site.parent >> Site.prev >> Site.lastChild
    let result = Site.at sm3 (Some b1Inst) |> prevCousin |> Site.current
    Assert.Equal(Some a2Inst, result)

// ---------------------------------------------------------------------------
// Site.childIndex
// ---------------------------------------------------------------------------

[<Fact>]
let ``Site.childIndex at root is None`` () =
    let graph, cont, _ = buildFlat [ "a" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let root = siteMap.rootId
    Assert.True(Site.at siteMap (Some root) |> Site.childIndex |> Option.isNone)

[<Fact>]
let ``Site.childIndex on root children matches 0 1 2`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let c = siteMap.entries.[siteMap.rootId].children
    Assert.Equal(Some 0, Site.at siteMap (Some c.[0]) |> Site.childIndex)
    Assert.Equal(Some 1, Site.at siteMap (Some c.[1]) |> Site.childIndex)
    Assert.Equal(Some 2, Site.at siteMap (Some c.[2]) |> Site.childIndex)

[<Fact>]
let ``Site.childIndex after expand on nested a1 a2 is 0 and 1`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let aInst = siteMap.entries.[siteMap.rootId].children.[0]
    let sm2, _ = expandEntry aInst graph siteMap nextId
    let a1 = sm2.entries.[aInst].children.[0]
    let a2 = sm2.entries.[aInst].children.[1]
    Assert.Equal(Some 0, Site.at sm2 (Some a1) |> Site.childIndex)
    Assert.Equal(Some 1, Site.at sm2 (Some a2) |> Site.childIndex)

[<Fact>]
let ``Site.childIndex None when current is None`` () =
    let graph, cont, _ = buildFlat [ "a" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    Assert.True(Site.at siteMap None |> Site.childIndex |> Option.isNone)

// ---------------------------------------------------------------------------
// VisibleSite / VisiNav — fold-aware; matches `Site` API shape
// ---------------------------------------------------------------------------

[<Fact>]
let ``VisibleSite at root is visible unknown SiteId is None`` () =
    let graph, cont, _ = buildFlat [ "a" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let root = siteMap.rootId

    Assert.Equal(Some root, VisibleSite.at siteMap (Some root) |> VisibleSite.current)
    Assert.True(VisibleSite.at siteMap (Some (Sid 999_999)) |> VisibleSite.current |> Option.isNone)

[<Fact>]
let ``VisibleSite at root child is None when root collapsed`` () =
    let graph, cont, _ = buildFlat [ "a"; "b" ]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let childInst = siteMap.entries.[siteMap.rootId].children.[0]
    let collapsed = toggleFold siteMap.rootId siteMap
    Assert.Equal(None, VisibleSite.at collapsed (Some childInst) |> VisibleSite.current)

[<Fact>]
let ``VisibleSite parent chain matches Site when path expanded`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let aInstId = siteMap.entries.[siteMap.rootId].children.[0]
    let sm2, _ = expandEntry aInstId graph siteMap nextId
    let a1InstId = sm2.entries.[aInstId].children.[0]
    let steps = VisibleSite.parent >> VisibleSite.parent
    let vis = VisibleSite.at sm2 (Some a1InstId) |> steps |> VisibleSite.current

    let plain =
        Site.at sm2 (Some a1InstId) |> (Site.parent >> Site.parent) |> Site.current

    Assert.Equal(plain, vis)

[<Fact>]
let ``VisibleSite prevCousin matches Site when branches expanded`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let root = siteMap.rootId
    let aInst = siteMap.entries.[root].children.[0]
    let bInst = siteMap.entries.[root].children.[1]
    let sm2, nextId2 = expandEntry aInst graph siteMap nextId
    let sm3, _ = expandEntry bInst graph sm2 nextId2
    let b1Inst = sm3.entries.[bInst].children.[0]
    let a2Inst = sm3.entries.[aInst].children.[1]
    let visiPrevCousin = VisibleSite.parent >> VisibleSite.prev >> VisibleSite.lastChild
    let sitePrevCousin = Site.parent >> Site.prev >> Site.lastChild

    let vis =
        VisibleSite.at sm3 (Some b1Inst) |> visiPrevCousin |> VisibleSite.current

    let plain =
        Site.at sm3 (Some b1Inst) |> sitePrevCousin |> Site.current

    Assert.Equal(plain, vis)
    Assert.Equal(Some a2Inst, vis)

[<Fact>]
let ``SiteMap buildSiteMap assigns unique instanceIds`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, Sid nextId = buildSiteMapFrom graph cont (Sid 0)
    let allIds = siteMap.entries |> Map.toList |> List.map fst
    Assert.Equal(allIds.Length, allIds |> List.distinct |> List.length)
    // nextId should equal number of entries allocated
    Assert.Equal(siteMap.entries.Count, nextId)

[<Fact>]
let ``SiteMap buildSiteMap root is expanded, children collapsed`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let root = siteMap.entries.[siteMap.rootId]
    Assert.True(root.expanded)

    for childInstId in root.children do
        Assert.False(siteMap.entries.[childInstId].expanded)

[<Fact>]
let ``SiteMap buildSiteMap root children have correct NodeIds and are stale`` () =
    let graph, cont, ids = buildNested ()
    let a = ids.[0]
    let b = ids.[1]
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
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
    let graph0 = Graph.create ()
    let graph1, contIds = ModelBuilder.createNodes [ "container"; "a"; "b" ] graph0
    let cont = contIds.[0]
    let a = contIds.[1]
    let b = contIds.[2]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ cont ]) graph1
        |> ModelBuilder.requireOk "root->cont"
    let graph3 =
        Graph.replace cont 0 [] (owned [ a ]) graph2
        |> ModelBuilder.requireOk "cont->a"
    let graph4 = Graph.replace a 0 [] (owned [ b ]) graph3 |> ModelBuilder.requireOk "a->b"
    let graph5 =
        Graph.replace b 0 [] (owned [ a ]) graph4 |> ModelBuilder.requireOk "b->a (cycle)"
    let siteMap, _ = buildSiteMapFrom graph5 cont (Sid 0)
    Assert.Equal(2, siteMap.entries.Count) // cont + a (collapsed, stale)
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aEntry = siteMap.entries.[rootEntry.children.[0]]
    Assert.Equal(a, aEntry.nodeId)
    Assert.False(aEntry.expanded)
    Assert.True(aEntry.childrenStale)
    let siteMap2, nextId2 = expandEntry aEntry.instanceId graph5 siteMap (Sid 2)
    let bInstId = siteMap2.entries.[aEntry.instanceId].children.[0]
    let siteMap3, _ = expandEntry bInstId graph5 siteMap2 nextId2
    let bEntry = siteMap3.entries.[bInstId]
    let aBackInstId = bEntry.children.[0]
    let aBackEntry = siteMap3.entries.[aBackInstId]
    Assert.Equal(a, aBackEntry.nodeId)
    Assert.False(aBackEntry.expanded)

// ---------------------------------------------------------------------------
// SiteMapOps.reconcileSiteMap
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteMap reconcileSiteMap preserves instanceIds for unchanged nodes`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let rebuilt, nextId2 = reconcileSiteMapFrom graph cont siteMap nextId
    // All instanceIds should be the same
    for KeyValue(instId, entry) in siteMap.entries do
        Assert.True(rebuilt.entries.ContainsKey instId)
        Assert.Equal(entry.nodeId, rebuilt.entries.[instId].nodeId)

    Assert.Equal(nextId, nextId2) // No new IDs allocated

[<Fact>]
let ``SiteMap reconcileSiteMap preserves fold state`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    // Expand "a" using expandEntry
    let expanded, nextId' = expandEntry aInstId graph siteMap nextId
    Assert.True(expanded.entries.[aInstId].expanded)
    // Reconcile — fold state for "a" should survive
    let rebuilt, _ = reconcileSiteMapFrom graph cont expanded nextId'
    Assert.True(rebuilt.entries.[aInstId].expanded)
    Assert.False(rebuilt.entries.[rootEntry.children.[1]].expanded) // "b" still collapsed

[<Fact>]
let ``SiteMap reconcileSiteMap assigns new IDs for added nodes`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let graph2, newNodeId = Graph.newNode "c" graph
    let contChildren = graph2.nodes.[cont].children

    let graph3 =
        Graph.replace cont contChildren.Length [] (owned [ newNodeId ]) graph2
        |> ModelBuilder.requireOk "add c"

    let rebuilt, nextId2 = reconcileSiteMapFrom graph3 cont siteMap nextId
    let rootEntry = rebuilt.entries.[rebuilt.rootId]
    Assert.Equal(3, rootEntry.children.Length)
    let newEntry = rebuilt.entries.[rootEntry.children.[2]]
    Assert.Equal(newNodeId, newEntry.nodeId)
    Assert.True(newEntry.instanceId >= nextId)

[<Fact>]
let ``SiteMap reconcileSiteMap two occurrences of same NodeId get distinct instanceIds`` () =
    // Build DAG: root -> cont -> [A, B], A -> [C], B -> [C]  (C shared)
    let graph0 = Graph.create ()
    let graph1, contIds = ModelBuilder.createNodes [ "container"; "a"; "b"; "c" ] graph0
    let cont = contIds.[0]
    let a = contIds.[1]
    let b = contIds.[2]
    let c = contIds.[3]

    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ cont ]) graph1 |> ModelBuilder.requireOk "root"

    let graph3 =
        Graph.replace cont 0 [] (owned [ a; b ]) graph2 |> ModelBuilder.requireOk "cont"

    let graph4 = Graph.replace a 0 [] (owned [ c ]) graph3 |> ModelBuilder.requireOk "a->c"
    let graph5 = Graph.replace b 0 [] (owned [ c ]) graph4 |> ModelBuilder.requireOk "b->c"
    let siteMap, nextId = buildSiteMapFrom graph5 cont (Sid 0)
    // Expand both A and B so C appears twice in the map
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    let bInstId = rootEntry.children.[1]
    let siteMap2, nextId2 = expandEntry aInstId graph5 siteMap nextId
    let siteMap3, nextId3 = expandEntry bInstId graph5 siteMap2 nextId2
    let rebuilt, _ = reconcileSiteMapFrom graph5 cont siteMap3 nextId3
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
    let graph1, contIds =
        ModelBuilder.createNodes [ "container"; "a"; "b"; "c"; "d" ] graph0
    let cont = contIds.[0]
    let a = contIds.[1]
    let b = contIds.[2]
    let c = contIds.[3]
    let d = contIds.[4]

    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ cont ]) graph1 |> ModelBuilder.requireOk "root"

    let graph3 =
        Graph.replace cont 0 [] (owned [ a; b ]) graph2 |> ModelBuilder.requireOk "cont"

    let graph4 = Graph.replace a 0 [] (owned [ c ]) graph3 |> ModelBuilder.requireOk "a->c"
    let graph5 = Graph.replace b 0 [] (owned [ c ]) graph4 |> ModelBuilder.requireOk "b->c"
    let graph6 = Graph.replace c 0 [] (owned [ d ]) graph5 |> ModelBuilder.requireOk "c->d"
    let siteMap, nextId = buildSiteMapFrom graph6 cont (Sid 0)
    // Expand both A and B so C appears twice in the map
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    let bInstId = rootEntry.children.[1]
    let siteMap2, nextId2 = expandEntry aInstId graph6 siteMap nextId
    let siteMap3, nextId3 = expandEntry bInstId graph6 siteMap2 nextId2
    let occurrenceIndex = buildOccurrenceIndex siteMap3
    let cInstIds = occurrenceIndex.[c]
    Assert.Equal(2, cInstIds.Length)
    // Expand only the first occurrence of C
    let siteMap4, _ = expandEntry cInstIds.[0] graph6 siteMap3 nextId3
    // One C is expanded, the other is not
    let e0 = siteMap4.entries.[cInstIds.[0]]
    let e1 = siteMap4.entries.[cInstIds.[1]]
    Assert.NotEqual(e0.expanded, e1.expanded)

// ---------------------------------------------------------------------------
// SiteMapOps.toggleFold
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteMap toggleFold collapses entry and marks children stale`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
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
    Assert.Equal<SiteId list>(
        expanded.entries.[aInstId].children,
        collapsed.entries.[aInstId].children)

[<Fact>]
let ``SiteMap toggleFold is no-op for unknown instanceId`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let result = toggleFold nextId siteMap // nextId not in map
    Assert.Equal(siteMap.entries.Count, result.entries.Count)

// ---------------------------------------------------------------------------
// SiteMapOps.expandEntry
// ---------------------------------------------------------------------------

[<Fact>]
let ``SiteMap expandEntry sets expanded true`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    Assert.False(siteMap.entries.[aInstId].expanded)
    let expanded, _ = expandEntry aInstId graph siteMap nextId
    Assert.True(expanded.entries.[aInstId].expanded)

[<Fact>]
let ``SiteMap expandEntry on already-expanded entry is a no-op`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let rootId = siteMap.rootId
    // Root is already expanded
    let result, nextId2 = expandEntry rootId graph siteMap nextId
    Assert.Equal(nextId, nextId2)
    Assert.Equal(siteMap.entries.Count, result.entries.Count)

[<Fact>]
let ``SiteMap expandEntry inserts child entries`` () =
    // After buildSiteMapFrom, "a" starts collapsed with children = [] (stale)
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
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
    let graph, cont, ids = buildNested ()
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let index = buildOccurrenceIndex siteMap
    Assert.Equal(3, index.Count) // cont, a, b
    Assert.True(index.ContainsKey cont)
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
    let graph, cont, ids = buildNested ()
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    let visible = getVisibleRowIds siteMap
    Assert.Equal(2, visible.Length)
    Assert.Equal(ids.[0], visible.[0])
    Assert.Equal(ids.[1], visible.[1])

[<Fact>]
let ``SiteMap getVisibleRowIds shows children of expanded node`` () =
    let graph, cont, ids = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
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
let buildCacheSet (siteMap: SiteMap) : Set<SiteId> =
    getVisibleInstanceIds siteMap |> Set.ofList

[<Fact>]
let ``planPatchDOM text change produces SetText patch and no CreateRow`` () =
    let graph, cont, ids = buildFlat [ "a"; "b"; "c" ]
    let oldModel = emptyModelAt graph cont
    // Change the text of the second node
    let targetId = ids.[1]

    let newNode =
        { graph.nodes.[targetId] with
            text = "b-edited" }

    let newGraph = Graph.fromNodes graph.root (Map.add targetId newNode graph.nodes)

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
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0] // "a" has 2 children: a1, a2

    let oldModel = emptyModelAt graph cont // "a" collapsed
    // Expand "a" in the new model
    let newSiteMap, newNextId = expandEntry aInstId graph siteMap nextId

    let newModel =
        { oldModel with
            siteMap = newSiteMap
            nextSiteId = newNextId }

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
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]

    // Start with "a" expanded
    let expandedSiteMap, newNextId = expandEntry aInstId graph siteMap nextId

    let oldModel =
        { emptyModelAt graph cont with
            siteMap = expandedSiteMap
            nextSiteId = newNextId }
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

// ---------------------------------------------------------------------------
// Page / Home — cursorLevel*, shiftPg*, cursorViewRoot*
// ---------------------------------------------------------------------------

[<Fact>]
let ``cursorLevelEnd selects last sibling under same parent`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 0 1 0
    let result = cursorLevelEnd model

    match result.selectedNodes with
    | Some sel ->
        Assert.Equal(2, sel.range.start)
        Assert.Equal(3, sel.range.endd)
        Assert.Equal(2, sel.focus)
    | None -> Assert.True(false, "Expected Some")

[<Fact>]
let ``cursorLevelStart selects first sibling under same parent`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 2 3 2
    let result = cursorLevelStart model

    match result.selectedNodes with
    | Some sel ->
        Assert.Equal(0, sel.range.start)
        Assert.Equal(1, sel.range.endd)
        Assert.Equal(0, sel.focus)
    | None -> Assert.True(false, "Expected Some")

[<Fact>]
let ``shiftPgDown reaches full sibling span`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 0 1 0
    let result = shiftPgDown model

    match result.selectedNodes with
    | Some sel ->
        Assert.Equal(0, sel.range.start)
        Assert.Equal(3, sel.range.endd)
        Assert.Equal(2, sel.focus)
    | None -> Assert.True(false, "Expected Some")

[<Fact>]
let ``cursorViewRootFirstChild selects first root child`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 1 2 1
    let result = cursorViewRootFirstChild model

    match result.selectedNodes with
    | Some sel ->
        Assert.Equal(0, sel.range.start)
        Assert.Equal(1, sel.range.endd)
    | None -> Assert.True(false, "Expected Some")

[<Fact>]
let ``cursorViewRootLastChild selects last root child`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let model = modelWithSel graph cont 0 1 0
    let result = cursorViewRootLastChild model

    match result.selectedNodes with
    | Some sel ->
        Assert.Equal(2, sel.range.start)
        Assert.Equal(3, sel.range.endd)
    | None -> Assert.True(false, "Expected Some")

// ---------------------------------------------------------------------------
// ViewModelSearch.searchPickSetRoot (Find /)
// ---------------------------------------------------------------------------

let private searchHit (graph: Graph) (nodeId: NodeId) : NodeSearchResult =
    let n = graph.nodes.[nodeId]
    { nodeId = nodeId; text = n.text; name = n.name }

[<Fact>]
let ``searchPickSetRoot reframes at parent and selects first child for a leaf hit`` () =
    let graph, cont, ids = buildNested ()
    let a = ids.[0]
    let a1 = ids.[2]
    let model = emptyModelAt graph cont
    let result = ViewModelSearch.searchPickSetRoot (searchHit graph a1) model |> fst

    Assert.Equal(a, result.zoomRoot)
    Assert.Equal(a, result.siteMap.entries.[result.siteMap.rootId].nodeId)
    Assert.True(result.siteMap.entries.[result.siteMap.rootId].expanded)
    let selectedId =
        result.selectedNodes |> Option.map (focusedNodeId result.graph)
    Assert.Equal(Some a1, selectedId)

[<Fact>]
let ``searchPickSetRoot reframes outside prior zoom when hit is not under zoom root`` () =
    let graph, cont, ids = buildNested ()
    let b = ids.[1]
    let a = ids.[0]
    let a1 = ids.[2]
    let zoomSiteMap, nextId = buildSiteMapFrom graph b (Sid 0)
    let model =
        { (emptyModelAt graph cont) with
            zoomRoot = b
            siteMap = zoomSiteMap
            nextSiteId = nextId }

    let result = ViewModelSearch.searchPickSetRoot (searchHit graph a1) model |> fst

    Assert.Equal(a, result.zoomRoot)
    let selectedId =
        result.selectedNodes |> Option.map (focusedNodeId result.graph)
    Assert.Equal(Some a1, selectedId)

// ---------------------------------------------------------------------------
// EditingCaretPreserve — sync-only updates must not reset contenteditable caret
// ---------------------------------------------------------------------------

[<Fact>]
let ``EditingCaretPreserve None means apply caret from model`` () =
    let graph, _, _ = buildFlat [ "hi" ]
    let m = emptyModel graph
    let editing =
        { m with mode = Editing ("hi", EditCaret.Utf16Index 1) }
    Assert.False(EditingCaretPreserve.shouldPreserveDomCaret None editing)

[<Fact>]
let ``EditingCaretPreserve false when not editing`` () =
    let graph, _, _ = buildFlat [ "hi" ]
    let prev = emptyModel graph
    let next = { prev with syncInfo = { prev.syncInfo with isPollingActive = true } }
    Assert.False(EditingCaretPreserve.shouldPreserveDomCaret (Some prev) next)

[<Fact>]
let ``EditingCaretPreserve true when only sync fields change`` () =
    let graph, _, _ = buildFlat [ "hi" ]
    let prev =
        { emptyModel graph with mode = Editing ("hi", EditCaret.Utf16Index 1) }
    let next =
        { prev with
            syncInfo =
                { prev.syncInfo with
                    isPollingActive = true
                    syncState = Polling } }
    Assert.True(EditingCaretPreserve.shouldPreserveDomCaret (Some prev) next)

[<Fact>]
let ``EditingCaretPreserve false when EditCaret in model changes`` () =
    let graph, _, _ = buildFlat [ "hi" ]
    let prev =
        { emptyModel graph with mode = Editing ("hi", EditCaret.Utf16Index 1) }
    let next = { prev with mode = Editing ("hi", EditCaret.Utf16Index 2) }
    Assert.False(EditingCaretPreserve.shouldPreserveDomCaret (Some prev) next)

[<Fact>]
let ``EditingCaretPreserve false when graph reference changes`` () =
    let graph, _, _ = buildFlat [ "hi" ]
    let prev =
        { emptyModel graph with mode = Editing ("hi", EditCaret.Utf16Index 1) }
    let g2 = Graph.fromNodes graph.root graph.nodes
    let next = { prev with graph = g2 }
    Assert.False(EditingCaretPreserve.shouldPreserveDomCaret (Some prev) next)

// ---------------------------------------------------------------------------
// TRASH semantics – bootstrap, delete classification, and behaviour
// ---------------------------------------------------------------------------

[<Fact>]
let ``Graph.create bootstraps TRASH under root with special kind`` () =
    let graph = Graph.create ()
    let rootNode = graph.nodes.[graph.root]
    let trashChildOpt =
        rootNode.children
        |> List.tryFind (fun c -> c.id = Graph.trashId && c.ref = Ownership.Owner)
    Assert.True(trashChildOpt.IsSome)
    let trashNode = graph.nodes.[Graph.trashId]
    match trashNode.kind with
    | Special Trash -> ()
    | _ -> Assert.True(false, "Trash node must have kind = Special Trash")

[<Fact>]
let ``Graph.replace rejects wiping all root children and leaves root unchanged`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "a" ] graph0
    let a = ids.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ a ]) graph1
        |> ModelBuilder.requireOk "root->a"
    let rootId = graph2.root
    let rootChildrenBefore = graph2.nodes.[rootId].children
    let wipe = Graph.replace rootId 0 rootChildrenBefore [] graph2
    match wipe with
    | Ok _ -> Assert.True(false, "expected Error when removing trash owner from root")
    | Error msg ->
        Assert.Contains("cannot remove trash owner child from root", msg)
        let rootChildrenAfter = graph2.nodes.[rootId].children
        Assert.Equal<ChildNode list>(rootChildrenBefore, rootChildrenAfter)

[<Fact>]
let ``Graph.replace clears all children under buildFlat cont`` () =
    let graph, cont, _ids = buildFlat [ "a"; "b"; "c" ]
    let contChildren = graph.nodes.[cont].children
    let graph2 =
        Graph.replace cont 0 contChildren [] graph
        |> ModelBuilder.requireOk "cont wipe"
    Assert.Empty(graph2.nodes.[cont].children)

[<Fact>]
let ``classifyDeleteForSelection marks last non-trash owner as MoveToTrash`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "a" ] graph0
    let a = ids.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ a ]) graph1
        |> ModelBuilder.requireOk "root->a"
    let model = modelWithSel graph2 graph2.root 0 1 0
    let sel =
        match model.selectedNodes with
        | Some s -> s
        | None -> failwith "expected selection"
    let classified = ViewModelDeleteOps.classifyDeleteForSelection model.graph sel.range
    Assert.Equal(1, classified.Length)
    let item = classified.Head
    Assert.Equal(a, item.child.id)
    Assert.Equal(ViewModelDeleteOps.MoveToTrash, item.action)

[<Fact>]
let ``MoveToTrash ops apply successfully and node lands under TRASH`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "a" ] graph0
    let a = ids.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] (owned [ a ]) graph1
        |> ModelBuilder.requireOk "root->a"
    let state0 : State =
        { graph = graph2; history = History.empty; revision = Revision.Zero }

    let removeOp = Op.Replace(graph2.root, 0, owned [ a ], [])

    let trashChildren = graph2.nodes.[Graph.trashId].children
    let newTrashChildren = trashChildren @ [ { ref = Ownership.Owner; id = a } ]
    let addToTrashOp = Op.Replace(Graph.trashId, 0, trashChildren, newTrashChildren)

    let change =
        { id = 0; changeId = System.Guid.NewGuid(); ops = [ removeOp; addToTrashOp ] }

    let result = History.applyChange change state0

    match result with
    | ApplyResult.Invalid(_, msg) ->
        Assert.True(false, sprintf "Expected Changed but got Invalid: %s" msg)
    | ApplyResult.Unchanged _ ->
        Assert.True(false, "Expected Changed but got Unchanged")
    | ApplyResult.Changed newState ->
        let trashNode = newState.graph.nodes.[Graph.trashId]
        let aUnderTrash =
            trashNode.children
            |> List.exists (fun c -> c.id = a && c.ref = Ownership.Owner)
        Assert.True(aUnderTrash, "node 'a' should be an Owner child of TRASH")
        let rootNode = newState.graph.nodes.[graph2.root]
        let aUnderRoot =
            rootNode.children |> List.exists (fun c -> c.id = a)
        Assert.False(aUnderRoot, "node 'a' should no longer be under root")
