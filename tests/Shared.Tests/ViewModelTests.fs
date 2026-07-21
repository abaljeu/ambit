module ViewModelTests

open System
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Shared.ViewModelSelection
open SpecialNodeTestHelpers
open VmTestHelpers
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
    { model with desktopCapabilities = Some (DesktopCapabilities.desktopEnabled true) }

let private withServerStatus (model: VM) : VM =
    { model with serverCapabilities = Some { canGitSave = false; canFileStatus = true } }

let private selectedModelWithText (text: string) : VM =
    let graph, cont, _ = buildFlat [ text ]
    modelWithSel graph cont 0 1 0

let private utc (y: int) (mo: int) (d: int) (h: int) (mi: int) (s: int) =
    DateTime(y, mo, d, h, mi, s, DateTimeKind.Utc)

let private fileSourceTime = utc 2024 6 1 12 0 0

let private withNodeUpdateTime (model: VM) (nodeId: NodeId) (time: DateTime) : VM =
    let node = model.graph.nodes.[nodeId]

    { model with
        graph =
            { model.graph with
                nodes = model.graph.nodes |> Map.add nodeId { node with updateTime = time } } }

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) : Node =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private graphWithWorkspaceTree () : Graph * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let wsNode = specialNode wsId Workspace "home" Graph.workspacesId
    let dirNode = specialNode dirId Directory "docs" wsId
    let fileNode = specialNode fileId File "readme.txt" dirId

    let graph1 =
        graph0.nodes
        |> Map.add wsId wsNode
        |> Map.add dirId dirNode
        |> Map.add fileId fileNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> ModelBuilder.requireOk "workspaces->ws"

    let graph3 =
        Graph.replace wsId 0 [] (owned [ dirId ]) graph2
        |> ModelBuilder.requireOk "ws->dir"

    let graph4 =
        Graph.replace dirId 0 [] (owned [ fileId ]) graph3
        |> ModelBuilder.requireOk "dir->file"

    graph4, wsId, dirId, fileId

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
let ``refreshDesktopFileIndicator leaves blank when status capability is disabled`` () =
    let enabled = DesktopCapabilities.desktopEnabled true
    let caps =
        { enabled with
            file =
                { enabled.file with
                    canStatus = false } }

    let model =
        selectedModelWithText "load [[note.txt]]"
        |> fun m -> { m with desktopCapabilities = Some caps }

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
let ``refreshDesktopFileIndicator requests status for Special Workspace path`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let wsNode =
        Node.Create(
            wsId,
            text = "home",
            name = Filename.Ok "home",
            owner = Graph.workspacesId,
            kind = Special Workspace)

    let graph1 =
        graph0.nodes
        |> Map.add wsId wsNode
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> ModelBuilder.requireOk "workspaces->ws"

    let model = modelWithSel graph2 Graph.workspacesId 0 1 0 |> withServerStatus
    let refreshed, effects = refreshDesktopFileIndicator model

    Assert.Equal(CheckingFileStatus (wsId, "//home"), refreshed.desktopFileIndicator)
    Assert.Equal<Effect list>([ RequestServerFileStatus (wsId, "//home") ], effects)

[<Fact>]
let ``refreshDesktopFileIndicator leaves workspace path blank until server status is known`` () =
    let model = selectedModelWithText "load [[//home/readme.txt]]" |> withDesktop
    let refreshed, effects = refreshDesktopFileIndicator model

    Assert.Equal(BlankFileIndicator, refreshed.desktopFileIndicator)
    Assert.Empty(effects)

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
    let updated =
        applyDesktopFileStatus stale "a.txt" ExistingFile (Some fileSourceTime) model

    Assert.Equal(BlankFileIndicator, updated.desktopFileIndicator)

[<Fact>]
let ``desktopFileIndicatorText shows sync label on active row only`` () =
    let model = selectedModelWithText "load [[note.txt]]" |> withDesktop
    let checking, _ = refreshDesktopFileIndicator model
    let nodeId = focusedNodeId checking.graph checking.selectedNodes.Value
    let nodeTime = utc 2024 6 1 10 0 0

    let checking = withNodeUpdateTime checking nodeId nodeTime

    let checkedModel =
        applyDesktopFileStatus nodeId "note.txt" ExistingFile (Some fileSourceTime) checking

    let activeEntry =
        checkedModel.siteMap.entries.[checkedModel.selectedNodes.Value.range.parent.children.[0]]

    let activeNode = checkedModel.graph.nodes.[activeEntry.nodeId]
    let rootEntry = checkedModel.siteMap.entries.[checkedModel.siteMap.rootId]
    let rootNode = checkedModel.graph.nodes.[rootEntry.nodeId]

    Assert.Equal("old", desktopFileIndicatorText checkedModel activeEntry activeNode)
    Assert.Equal("", desktopFileIndicatorText checkedModel rootEntry rootNode)

[<Fact>]
let ``desktopFileIndicatorText shows current and edited for existing file`` () =
    let model = selectedModelWithText "load [[note.txt]]" |> withDesktop
    let checking, _ = refreshDesktopFileIndicator model
    let nodeId = focusedNodeId checking.graph checking.selectedNodes.Value
    let activeEntry = checking.siteMap.entries.[checking.selectedNodes.Value.range.parent.children.[0]]
    let activeNode = checking.graph.nodes.[activeEntry.nodeId]

    let currentModel =
        checking
        |> fun m -> withNodeUpdateTime m nodeId fileSourceTime
        |> fun m -> applyDesktopFileStatus nodeId "note.txt" ExistingFile (Some fileSourceTime) m

    Assert.Equal(
        "current",
        desktopFileIndicatorText currentModel activeEntry currentModel.graph.nodes.[nodeId])

    let editedModel =
        checking
        |> fun m -> withNodeUpdateTime m nodeId (utc 2024 6 1 14 0 0)
        |> fun m -> applyDesktopFileStatus nodeId "note.txt" ExistingFile (Some fileSourceTime) m

    Assert.Equal(
        "edited",
        desktopFileIndicatorText editedModel activeEntry editedModel.graph.nodes.[nodeId])

[<Fact>]
let ``specialKindRowClass maps each SpecialKind to amb-row-special class`` () =
    let otherId = NodeId.New()
    Assert.Equal(Some "amb-row-special-workspaces", specialKindRowClass Graph.workspacesId (Special Workspaces))
    Assert.Equal(Some "amb-row-special-workspace", specialKindRowClass otherId (Special Workspace))
    Assert.Equal(Some "amb-row-special-directory", specialKindRowClass otherId (Special Directory))
    Assert.Equal(Some "amb-row-special-file", specialKindRowClass otherId (Special File))
    Assert.Equal(Some "amb-row-special-trash", specialKindRowClass Graph.trashId (Special Directory))
    Assert.Equal(None, specialKindRowClass otherId Normal)

[<Fact>]
let ``rowFileIndicatorText shows kind symbol and desktop status wins on active row`` () =
    let graph = Graph.create ()
    let wsNode = graph.nodes.[Graph.workspacesId]
    let siteMap, _ = buildSiteMap graph
    let wsEntry =
        siteMap.entries
        |> Map.toSeq
        |> Seq.map snd
        |> Seq.find (fun e -> e.nodeId = Graph.workspacesId)
    let model = emptyModel graph

    Assert.Equal("\u229E", rowFileIndicatorText model wsEntry wsNode)
    Assert.Equal("@", specialKindSymbol (NodeId.New()) (Special Workspace) |> Option.get)

    let model = selectedModelWithText "load [[note.txt]]" |> withDesktop
    let checking, _ = refreshDesktopFileIndicator model
    let nodeId = focusedNodeId checking.graph checking.selectedNodes.Value
    let nodeTime = utc 2024 6 1 10 0 0
    let checking = withNodeUpdateTime checking nodeId nodeTime

    let checkedModel =
        applyDesktopFileStatus nodeId "note.txt" ExistingFile (Some fileSourceTime) checking

    let activeEntry =
        checkedModel.siteMap.entries.[checkedModel.selectedNodes.Value.range.parent.children.[0]]

    let activeNode = checkedModel.graph.nodes.[activeEntry.nodeId]

    Assert.Equal("old", rowFileIndicatorText checkedModel activeEntry activeNode)

[<Fact>]
let ``rowFileIndicatorText shows sync label on active special file node`` () =
    let graph, wsId, _, fileId = graphWithWorkspaceTree ()
    let parentId = graph.nodes.[fileId].owner
    let parent = graph.nodes.[parentId]
    let fileIdx = parent.children |> List.findIndex (fun c -> c.id = fileId)
    let model = modelWithSel graph parentId fileIdx (fileIdx + 1) fileIdx |> withDesktop
    let checking, _ = refreshDesktopFileIndicator model
    let path = NodeDesktopPath.pathForNodeId checking.graph fileId |> Option.get
    let nodeTime = utc 2024 6 1 10 0 0
    let checking = withNodeUpdateTime checking fileId nodeTime

    let checkedModel =
        applyDesktopFileStatus fileId path ExistingFile (Some fileSourceTime) checking

    let fileEntry =
        checkedModel.siteMap.entries.[checkedModel.selectedNodes.Value.range.parent.children.[fileIdx]]

    let fileNode = checkedModel.graph.nodes.[fileId]

    Assert.Equal("old", rowFileIndicatorText checkedModel fileEntry fileNode)

[<Fact>]
let ``rowFileIndicatorText shows missing and absent class for active server status`` () =
    let model =
        selectedModelWithText "load [[//home/missing.txt]]"
        |> withServerStatus
    let checking, _ = refreshDesktopFileIndicator model
    let nodeId = focusedNodeId checking.graph checking.selectedNodes.Value

    let checkedModel =
        applyDesktopFileStatus nodeId "//home/missing.txt" MissingArtifact None checking

    let activeEntry =
        checkedModel.siteMap.entries.[checkedModel.selectedNodes.Value.range.parent.children.[0]]

    let activeNode = checkedModel.graph.nodes.[activeEntry.nodeId]

    Assert.Equal("missing", rowFileIndicatorText checkedModel activeEntry activeNode)
    Assert.True(rowArtifactAbsentClassEligible checkedModel activeEntry activeNode)

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
let ``refreshSelection adapts when parent instance no longer exists`` () =
    let graph, cont, _ = buildFlat [ "a"; "b" ]
    let model = modelWithSel graph cont 0 1 0
    let staleSel =
        match model.selectedNodes with
        | None -> failwith "Expected selected node"
        | Some sel ->
            let orphanParent = { sel.range.parent with instanceId = Sid 9_999 }
            { sel with range = { sel.range with parent = orphanParent } }
    let siteMap, _ = buildSiteMapFrom graph cont (Sid 0)
    match refreshSelection graph siteMap staleSel with
    | None -> Assert.True(false, "Expected adapted selection via instance id")
    | Some refreshed -> Assert.Equal(0, refreshed.range.start)

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
            false
            expandedRoot
            1
            2
            1
        |> Option.get

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
            false
            newParent
            0
            1
            0
        |> Option.get

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
            false
            newParentExpanded
            0
            1
            0
        |> Option.get

    Assert.Equal(b, focusedNodeId gPost sel)

let private indentUndoTopology () =
    let graphPre, cont, ids = buildFlat [ "a"; "b"; "c" ]
    let a = ids.[0]
    let b = ids.[1]
    let bChild = graphPre.nodes.[cont].children.[1]
    let gMid =
        Graph.replace cont 1 [ bChild ] [] graphPre |> ModelBuilder.requireOk "rm b"
    let gPost =
        Graph.replace a 0 [] [ bChild ] gMid |> ModelBuilder.requireOk "add b under a"
    let postSiteMap, _ = buildSiteMapFrom gPost cont (Sid 0)
    let mPre = emptyModelAt graphPre cont
    let rootPre = mPre.siteMap.entries.[mPre.siteMap.rootId]
    let newParentCollapsed =
        postSiteMap.entries |> Map.pick (fun _ e -> if e.nodeId = a then Some e else None)
    let postSiteMapExpanded, _ =
        expandEntry newParentCollapsed.instanceId gPost postSiteMap mPre.nextSiteId
    let newParentExpanded =
        postSiteMapExpanded.entries.[newParentCollapsed.instanceId]
    let postIndentSel =
        selectionAfterStructuralMove graphPre gPost postSiteMapExpanded
            { parent = rootPre; start = 1; endd = 2 }
            false
            newParentExpanded
            0
            1
            0
        |> Option.get
    let postIndentModel =
        { mPre with
            graph = gPost
            siteMap = postSiteMapExpanded
            selectedNodes = Some postIndentSel }
    graphPre, cont, mPre, postIndentModel, postIndentSel, b

let private refreshSelectionAfterSiteMap (model: VM) : VM =
    let siteMap, nextId =
        reconcileSiteMapFrom model.graph model.zoomRoot model.siteMap model.nextSiteId
    let model' = { model with siteMap = siteMap; nextSiteId = nextId }
    match model'.selectedNodes with
    | None -> model'
    | Some sel ->
        let adapted =
            refreshSelection model'.graph model'.siteMap sel
            |> Option.orElse (firstChildSelection model'.siteMap model'.zoomRoot)
        match adapted with
        | Some refreshed -> { model' with selectedNodes = Some refreshed }
        | None -> model'

[<Fact>]
let ``refreshSelection adapts selection after indent undo topology`` () =
    let graphPre, cont, _, postIndentModel, postIndentSel, b = indentUndoTopology ()
    let undoSiteMap, _ =
        reconcileSiteMapFrom graphPre cont postIndentModel.siteMap postIndentModel.nextSiteId
    match refreshSelection graphPre undoSiteMap postIndentSel with
    | None -> Assert.True(false, "Expected adapted selection")
    | Some refreshed ->
        Assert.Equal(b, focusedNodeId graphPre refreshed)
        Assert.Equal(1, refreshed.range.start)

[<Fact>]
let ``withSiteMap adapts invalid selection after indent undo`` () =
    let graphPre, _, _, postIndentModel, postIndentSel, b = indentUndoTopology ()
    let undoModel = { postIndentModel with graph = graphPre; selectedNodes = Some postIndentSel }
    let result = refreshSelectionAfterSiteMap undoModel
    match result.selectedNodes with
    | None -> Assert.True(false, "Expected adapted selection")
    | Some sel ->
        Assert.Equal(b, focusedNodeId graphPre sel)
        Assert.Equal(1, sel.range.start)

[<Fact>]
let ``activeNodeId resolves stale selection after indent undo`` () =
    let graphPre, _, _, postIndentModel, postIndentSel, b = indentUndoTopology ()
    let model = { postIndentModel with graph = graphPre; selectedNodes = Some postIndentSel }
    match activeNodeId model with
    | None -> Assert.True(false, "Expected active node")
    | Some nodeId -> Assert.Equal(b, nodeId)

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
            false
            newParent
            0
            1
            0
        |> Option.get

    Assert.Equal(b, focusedNodeId gPost sel)

[<Fact>]
let ``selectionAfterStructuralMove stayAtSource picks sibling below range at range start`` () =
    let graph, cont, ids = buildFlat [ "a"; "b"; "c" ]
    let c = ids.[2]
    let m0 = emptyModelAt graph cont
    let fromParent = m0.siteMap.entries.[m0.siteMap.rootId]
    let sel =
        selectionAfterStructuralMove graph graph m0.siteMap
            { parent = fromParent; start = 0; endd = 2 }
            true
            fromParent
            0
            0
            0
        |> Option.get

    Assert.Equal(c, focusedNodeId graph sel)

[<Fact>]
let ``selectionAfterStructuralMove stayAtSource returns none when zoom root has no siblings`` () =
    let graph, cont, _ = buildFlat [ "a" ]
    let m0 = emptyModelAt graph cont
    let fromParent = m0.siteMap.entries.[m0.siteMap.rootId]
    let sel =
        selectionAfterStructuralMove graph graph m0.siteMap
            { parent = fromParent; start = 0; endd = 1 }
            true
            fromParent
            0
            0
            0

    Assert.True(sel.IsNone)

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
// tryFindFocusedNode
// ---------------------------------------------------------------------------

[<Fact>]
let ``tryFindFocusedNode returns focused node when present`` () =
    let graph, cont, ids = buildFlat [ "a"; "b"; "c" ]
    let sel = modelWithSel graph cont 0 1 0 |> fun m -> m.selectedNodes.Value

    match tryFindFocusedNode graph sel with
    | None -> Assert.True(false, "expected Some")
    | Some (focusId, node) ->
        Assert.Equal(ids.[0], focusId)
        Assert.Equal("a", node.text)

[<Fact>]
let ``tryFindFocusedNode returns None when node missing from graph`` () =
    let graph, cont, ids = buildFlat [ "a"; "b" ]
    let sel = modelWithSel graph cont 0 1 0 |> fun m -> m.selectedNodes.Value
    let graphMissing =
        { graph with nodes = graph.nodes |> Map.remove ids.[0] }

    Assert.True(tryFindFocusedNode graphMissing sel |> Option.isNone)

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

[<Fact>]
let ``nodeHasExpandedChildren false when collapsed or leaf`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let aInstId = siteMap.entries.[siteMap.rootId].children.[0]
    Assert.False(SiteMap.nodeIsExpanded siteMap (Some aInstId))
    let sm2, _ = expandEntry aInstId graph siteMap nextId
    Assert.True(SiteMap.nodeIsExpanded sm2 (Some aInstId))
    let a1InstId = sm2.entries.[aInstId].children.[0]
    Assert.False(SiteMap.nodeIsExpanded sm2 (Some a1InstId))

[<Fact>]
let ``parentSiblingTarget expands collapsed sibling with children`` () =
    let graph, cont, _ = buildNested ()
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    let bInstId = rootEntry.children.[1]
    let siteMap, nextId = expandEntry aInstId graph siteMap nextId
    let a1InstId = siteMap.entries.[aInstId].children.[0]
    let a1Entry = siteMap.entries.[a1InstId]

    match parentSiblingTarget 1 a1Entry graph siteMap nextId cont with
    | None -> Assert.True(false, "expected sibling target")
    | Some (siteMap2, _, sibling) ->
        Assert.Equal(bInstId, sibling.instanceId)
        Assert.True(sibling.expanded)
        Assert.Single(siteMap2.entries.[bInstId].children) |> ignore

[<Fact>]
let ``parentSiblingTarget accepts collapsed leaf sibling`` () =
    let graph0 = Graph.create ()
    let graph1, contIds = ModelBuilder.createNodes [ "container" ] graph0
    let cont = contIds.[0]
    let graph2, ids = ModelBuilder.createNodes [ "a"; "b"; "a1" ] graph1
    let a = ids.[0]
    let b = ids.[1]
    let a1 = ids.[2]
    let graph3 =
        Graph.replace graph2.root 0 [] (owned [ cont ]) graph2
        |> ModelBuilder.requireOk "leafSibling.root"
    let graph4 =
        Graph.replace cont 0 [] (owned [ a; b ]) graph3
        |> ModelBuilder.requireOk "leafSibling.cont"
    let graph =
        Graph.replace a 0 [] (owned [ a1 ]) graph4
        |> ModelBuilder.requireOk "leafSibling.a"
    let siteMap, nextId = buildSiteMapFrom graph cont (Sid 0)
    let rootEntry = siteMap.entries.[siteMap.rootId]
    let aInstId = rootEntry.children.[0]
    let bInstId = rootEntry.children.[1]
    let siteMap, nextId = expandEntry aInstId graph siteMap nextId
    let a1InstId = siteMap.entries.[aInstId].children.[0]
    let a1Entry = siteMap.entries.[a1InstId]

    match parentSiblingTarget 1 a1Entry graph siteMap nextId cont with
    | None -> Assert.True(false, "expected sibling target")
    | Some (_, nextId2, sibling) ->
        Assert.Equal(bInstId, sibling.instanceId)
        Assert.False(sibling.expanded)
        Assert.Equal(nextId, nextId2)

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
// outlineDisplayText
// ---------------------------------------------------------------------------

[<Fact>]
let ``outlineDisplayText uses text for owned special directory when available`` () =
    let node =
        Node.Create(
            NodeId.New(),
            text = "folder",
            name = Filename.Ok "my-docs",
            kind = Special Directory,
            updateTime = NodeUpdateTime.now ())
    Assert.Equal("folder", outlineDisplayText node)

[<Fact>]
let ``outlineDisplayText falls back to name for owned special directory when text empty`` () =
    let node =
        Node.Create(
            NodeId.New(),
            name = Filename.Ok "my-docs",
            kind = Special Directory,
            updateTime = NodeUpdateTime.now ())
    Assert.Equal("my-docs", outlineDisplayText node)

[<Fact>]
let ``outlineDisplayText keeps trash label from text`` () =
    let graph = Graph.create ()
    let node = graph.nodes.[Graph.trashId]
    Assert.Equal("Trash", outlineDisplayText node)

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
let ``planPatchDOM name change produces SetNodeName patch`` () =
    let graph, cont, ids = buildFlat [ "a"; "b"; "c" ]
    let oldModel = emptyModelAt graph cont
    let targetId = ids.[1]
    let oldNode = graph.nodes.[targetId]

    let newNode =
        { oldNode with
            name = Filename.Ok "renamed.txt" }

    let newGraph = Graph.fromNodes graph.root (Map.add targetId newNode graph.nodes)
    let newModel = { oldModel with graph = newGraph }
    let cachedInstIds = buildCacheSet oldModel.siteMap

    let mutations = planPatchDOM oldModel newModel cachedInstIds

    let namePatches =
        mutations
        |> List.collect (fun m ->
            match m with
            | PatchRow(_, patches) ->
                patches
                |> List.filter (function
                    | SetNodeName _ -> true
                    | _ -> false)
            | _ -> [])

    Assert.Equal(1, namePatches.Length)
    match namePatches.[0] with
    | SetNodeName "renamed.txt" -> ()
    | other -> Assert.Fail(sprintf "unexpected patch %A" other)

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

[<Fact>]
let ``startEditInstanceAtPos selects instance and enters Editing with node text`` () =
    let graph, cont, ids = buildFlat [ "alpha"; "beta"; "gamma" ]
    let rootEntry = emptyModelAt graph cont |> fun m -> m.siteMap.entries.[m.siteMap.rootId]
    let betaInst = rootEntry.children.[1]
    let model = modelWithSel graph cont 0 1 0
    let result =
        startEditInstanceAtPos betaInst 3 model
        |> Option.defaultWith (fun () -> Assert.True(false); model)
    match result.selectedNodes with
    | Some sel ->
        Assert.Equal(ids.[1], focusedNodeId graph sel)
        Assert.Equal(1, sel.focus)
    | None -> Assert.True(false, "expected selection")
    match result.mode with
    | Editing (text, EditCaret.Utf16Index pos) ->
        Assert.Equal("beta", text)
        Assert.Equal(3, pos)
    | _ -> Assert.True(false, "expected Editing mode")

[<Fact>]
let ``startEditInstanceAtPos from Editing switches row and caret`` () =
    let graph, cont, ids = buildFlat [ "alpha"; "beta"; "gamma" ]
    let rootEntry = emptyModelAt graph cont |> fun m -> m.siteMap.entries.[m.siteMap.rootId]
    let alphaInst = rootEntry.children.[0]
    let betaInst = rootEntry.children.[1]
    let selectingModel = modelWithSel graph cont 0 0 0
    let editingAlpha =
        startEditInstanceAtPos alphaInst 2 selectingModel
        |> Option.defaultWith (fun () -> Assert.True(false); selectingModel)
    let result =
        startEditInstanceAtPos betaInst 5 editingAlpha
        |> Option.defaultWith (fun () -> Assert.True(false); editingAlpha)
    match result.selectedNodes with
    | Some sel ->
        Assert.Equal(ids.[1], focusedNodeId graph sel)
        Assert.Equal(1, sel.focus)
    | None -> Assert.True(false, "expected selection")
    match result.mode with
    | Editing (text, EditCaret.Utf16Index pos) ->
        Assert.Equal("beta", text)
        Assert.Equal(5, pos)
    | _ -> Assert.True(false, "expected Editing mode")

[<Fact>]
let ``startEditInstanceAtPos ignores graph root node`` () =
    let graph, _, _ = buildFlat [ "a" ]
    let model = emptyModel graph
    let result = startEditInstanceAtPos model.siteMap.rootId 0 model
    Assert.True(result.IsNone)

[<Fact>]
let ``planPatchDOM entering edit mode yields RecreateRow not SetText`` () =
    let graph, cont, _ = buildFlat [ "a"; "b"; "c" ]
    let oldModel = modelWithSel graph cont 0 1 0
    let targetInst = oldModel.siteMap.entries.[oldModel.siteMap.rootId].children.[1]
    let newModel =
        startEditInstanceAtPos targetInst 0 oldModel
        |> Option.defaultWith (fun () -> Assert.True(false); oldModel)
    let cachedInstIds = buildCacheSet oldModel.siteMap
    let mutations = planPatchDOM oldModel newModel cachedInstIds
    let recreates =
        mutations
        |> List.filter (function
            | RecreateRow id -> id = targetInst
            | _ -> false)
    let setTextOnTarget =
        mutations
        |> List.collect (function
            | PatchRow (id, patches) when id = targetInst ->
                patches
                |> List.filter (function
                    | SetText _ -> true
                    | _ -> false)
            | _ -> [])
    Assert.Equal(1, recreates.Length)
    Assert.Equal(0, setTextOnTarget.Length)

[<Fact>]
let ``planPatchDOM editing row text change produces SetText not RecreateRow`` () =
    let graph, cont, ids = buildFlat [ "a"; "b"; "c" ]
    let oldModel = modelWithSel graph cont 1 2 1
    let targetInst = oldModel.siteMap.entries.[oldModel.siteMap.rootId].children.[1]
    let editingModel =
        startEditInstanceAtPos targetInst 0 oldModel
        |> Option.defaultWith (fun () -> Assert.True(false); oldModel)
    let targetId = ids.[1]
    let newNode = { graph.nodes.[targetId] with text = "b+pasted" }
    let newGraph = Graph.fromNodes graph.root (Map.add targetId newNode graph.nodes)
    let newModel = { editingModel with graph = newGraph }
    let cachedInstIds = buildCacheSet editingModel.siteMap
    let mutations = planPatchDOM editingModel newModel cachedInstIds
    let recreates =
        mutations
        |> List.filter (function
            | RecreateRow id -> id = targetInst
            | _ -> false)
    let setTextOnTarget =
        mutations
        |> List.collect (function
            | PatchRow (id, patches) when id = targetInst ->
                patches
                |> List.choose (function
                    | SetText t -> Some t
                    | _ -> None)
            | _ -> [])
    Assert.Equal(0, recreates.Length)
    Assert.Equal(1, setTextOnTarget.Length)
    Assert.Equal("b+pasted", setTextOnTarget.[0])

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
let ``cursorLevelEnd descends into expanded children of last sibling`` () =
    let graph, cont, ids = buildNested ()
    let b1NodeId = ids.[4]
    let m = emptyModelAt graph cont
    let rootEntry = m.siteMap.entries.[m.siteMap.rootId]
    let bInst = rootEntry.children.[1]
    let sm2, nextId2 = expandEntry bInst graph m.siteMap m.nextSiteId
    let model =
        { m with
            siteMap = sm2
            nextSiteId = nextId2
            selectedNodes =
                Some { range = { parent = rootEntry; start = 0; endd = 1 }; focus = 0 } }
    let result = cursorLevelEnd model

    match result.selectedNodes with
    | Some sel ->
        let focusedInstId = sel.range.parent.children.[sel.focus]
        let focusedEntry = result.siteMap.entries.[focusedInstId]
        Assert.Equal(b1NodeId, focusedEntry.nodeId)
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

let private buildSharedRefLink () : Graph * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "ownerParent"; "refParent"; "shared" ] graph0
    let ownerParent = ids.[0]
    let refParent = ids.[1]
    let shared = ids.[2]
    let graph2 =
        Graph.replace graph0.root 0 [] (owned [ ownerParent; refParent ]) graph1
        |> ModelBuilder.requireOk "buildSharedRefLink.root"
    let graph3 =
        Graph.replace ownerParent 0 [] (owned [ shared ]) graph2
        |> ModelBuilder.requireOk "buildSharedRefLink.owner"
    let graph4 =
        Graph.replace refParent 0 [] [ { ref = Ownership.Ref; id = shared } ] graph3
        |> ModelBuilder.requireOk "buildSharedRefLink.ref"
    graph4, ownerParent, refParent, shared

[<Fact>]
let ``tryReframeZoomAtOwnerParent follows focus owner not structural parent`` () =
    let graph, ownerParent, refParent, shared = buildSharedRefLink ()
    let model = modelWithSel graph refParent 0 1 0

    Assert.Equal(refParent, model.zoomRoot)
    Assert.Equal(shared, focusedNodeId graph model.selectedNodes.Value)

    match tryReframeZoomAtOwnerParent graph shared model.nextSiteId with
    | None -> Assert.True(false, "Expected Some")
    | Some (zoomRoot, siteMap, _nextId, sel) ->
        Assert.Equal(ownerParent, zoomRoot)
        Assert.Equal(ownerParent, siteMap.entries.[siteMap.rootId].nodeId)
        let selectedId = sel |> Option.map (focusedNodeId graph)
        Assert.Equal(Some shared, selectedId)

[<Fact>]
let ``focusNode prefers owner parent over ref parent`` () =
    let graph, ownerParent, refParent, shared = buildSharedRefLink ()
    let model = emptyModelAt graph refParent
    let result = focusNode shared model
    Assert.Equal(ownerParent, result.zoomRoot)
    let selectedId =
        result.selectedNodes |> Option.map (focusedNodeId result.graph)
    Assert.Equal(Some shared, selectedId)

[<Fact>]
let ``tryFocusNodeOccurrence falls back to ref occurrence without owner`` () =
    let graph0 = Graph.create ()
    let childId = NodeId.New()
    let child = Node.Create(childId, text = "ref-only")
    let root = graph0.nodes.[Graph.rootId]
    let nodes =
        graph0.nodes
        |> Map.add Graph.rootId
            { root with
                children =
                    root.children @ [ { ref = Ownership.Ref; id = childId } ] }
        |> Map.add childId child
    let graph = Graph.fromNodes graph0.root nodes
    match tryFocusNodeOccurrence graph childId (Sid 0) with
    | None -> Assert.True(false, "Expected Some")
    | Some (zoomRoot, _siteMap, _nextId, sel) ->
        Assert.Equal(Graph.rootId, zoomRoot)
        let selectedId = sel |> Option.map (focusedNodeId graph)
        Assert.Equal(Some childId, selectedId)

let private buildSharedRefLinkNonLeaf () : Graph * NodeId * NodeId * NodeId =
    let graph0, ownerParent, refParent, shared = buildSharedRefLink ()
    let graph1, childIds = ModelBuilder.createNodes [ "under" ] graph0
    let under = childIds.[0]
    let graph2 =
        Graph.replace shared 0 [] (owned [ under ]) graph1
        |> ModelBuilder.requireOk "buildSharedRefLinkNonLeaf.under"
    graph2, ownerParent, refParent, shared

[<Fact>]
let ``zoom ingress round-trip returns to Ref parent for shared node`` () =
    let graph, ownerParent, refParent, shared = buildSharedRefLinkNonLeaf ()
    let model0 = modelWithSel graph refParent 0 1 0
    let sel = model0.selectedNodes.Value
    Assert.Equal(shared, focusedNodeId graph sel)
    Assert.False(graph.nodes.[shared].children.IsEmpty)

    let ingress =
        tryZoomInIngress false sel model0.siteMap shared |> Option.get
    Assert.Equal((refParent, 0), ingress)
    let stack =
        pushZoomIngress model0.zoomRoot shared ingress model0.zoomIngress
    Assert.Equal<(NodeId * int) list>([ (refParent, 0) ], stack)

    match resolveZoomOutParent graph shared stack with
    | None -> Assert.True(false, "Expected Some")
    | Some (parentId, index, rest) ->
        Assert.Equal(refParent, parentId)
        Assert.Equal(0, index)
        Assert.Equal<(NodeId * int) list>([], rest)
        // When parentByChild prefers Owner, ingress still restores Ref.
        match Graph.tryFindParentAndIndex shared graph with
        | Some (canonical, _) when canonical = ownerParent ->
            Assert.NotEqual(canonical, parentId)
        | _ -> ()

[<Fact>]
let ``pushZoomIngress collapses when zooming into an ancestor via Ref`` () =
    // cont -owns-> a -owns-> b -refs-> a
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "container"; "a"; "b" ] graph0
    let cont = ids.[0]
    let a = ids.[1]
    let b = ids.[2]
    let graph2 =
        Graph.replace graph0.root 0 [] (owned [ cont ]) graph1
        |> ModelBuilder.requireOk "root->cont"
    let graph3 =
        Graph.replace cont 0 [] (owned [ a ]) graph2
        |> ModelBuilder.requireOk "cont->a"
    let graph4 =
        Graph.replace a 0 [] (owned [ b ]) graph3
        |> ModelBuilder.requireOk "a->b"
    let graph5 =
        Graph.replace b 0 [] [ { ref = Ownership.Ref; id = a } ] graph4
        |> ModelBuilder.requireOk "b-ref->a"

    let stackAtA = pushZoomIngress cont a (cont, 0) []
    Assert.Equal<(NodeId * int) list>([ (cont, 0) ], stackAtA)

    let stackAtB = pushZoomIngress a b (a, 0) stackAtA
    Assert.Equal<(NodeId * int) list>([ (a, 0); (cont, 0) ], stackAtB)

    // Zoom into ancestor `a` via the Ref under `b` — must not push (b,_) or cycle.
    let stackBackAtA = pushZoomIngress b a (b, 0) stackAtB
    Assert.Equal<(NodeId * int) list>([ (cont, 0) ], stackBackAtA)

    match resolveZoomOutParent graph5 a stackBackAtA with
    | None -> Assert.True(false, "Expected Some")
    | Some (parentId, _, rest) ->
        Assert.Equal(cont, parentId)
        Assert.Equal<(NodeId * int) list>([], rest)

[<Fact>]
let ``ownerPathIngress walks owner chain to root`` () =
    let graph, cont, ids = buildNested ()
    let a = ids.[0]
    let a1 = ids.[2]
    let path = ownerPathIngress graph a1
    Assert.Equal(a, fst path.[0])
    Assert.Equal(0, snd path.[0])
    Assert.Equal(cont, fst path.[1])
    Assert.Equal(0, snd path.[1])
    Assert.Equal(graph.root, fst path.[2])
    Assert.Equal<string list>(
        [ "ROOT"; "container"; "a"; "a1" ],
        zoomIngressPathTexts graph a1 path)

[<Fact>]
let ``tryZoomToIngressPathNode jumps to middle ancestor`` () =
    let graph, cont, ids = buildNested ()
    let a = ids.[0]
    let a1 = ids.[2]
    let stack = ownerPathIngress graph a1
    match tryZoomToIngressPathNode graph a1 stack cont with
    | None -> Assert.True(false, "Expected Some")
    | Some (zoomRoot, index, rest) ->
        Assert.Equal(cont, zoomRoot)
        Assert.Equal(0, index)
        Assert.Equal(graph.root, fst rest.[0])
        match tryZoomToIngressPathNode graph a1 stack a with
        | None -> Assert.True(false, "Expected Some for a")
        | Some (zoomRoot2, index2, rest2) ->
            Assert.Equal(a, zoomRoot2)
            Assert.Equal(0, index2)
            Assert.Equal(cont, fst rest2.[0])

[<Fact>]
let ``tryZoomToIngressPathNode None for current zoom root`` () =
    let graph, cont, ids = buildNested ()
    let a1 = ids.[2]
    let stack = ownerPathIngress graph a1
    Assert.True(tryZoomToIngressPathNode graph a1 stack a1 |> Option.isNone)

[<Fact>]
let ``zoomIngressPathTexts follows ingress then zoom root`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "container"; "a"; "b" ] graph0
    let cont = ids.[0]
    let a = ids.[1]
    let b = ids.[2]
    let graph2 =
        Graph.replace graph0.root 0 [] (owned [ cont ]) graph1
        |> ModelBuilder.requireOk "root->cont"
    let graph3 =
        Graph.replace cont 0 [] (owned [ a ]) graph2
        |> ModelBuilder.requireOk "cont->a"
    let graph4 =
        Graph.replace a 0 [] (owned [ b ]) graph3
        |> ModelBuilder.requireOk "a->b"

    let stack = [ (a, 0); (cont, 0) ]
    Assert.Equal<NodeId list>([ cont; a; b ], zoomIngressPathIds b stack)
    Assert.Equal<string list>(
        [ "container"; "a"; "b" ],
        zoomIngressPathTexts graph4 b stack)

[<Fact>]
let ``searchPickSetRoot seeds owner ingress for shared zoom-out`` () =
    let graph, ownerParent, _refParent, shared = buildSharedRefLinkNonLeaf ()
    let model = emptyModelAt graph graph.root
    let result =
        ViewModelSearch.searchPickSetRoot (searchHit graph shared) model |> fst

    Assert.Equal(shared, result.zoomRoot)
    Assert.Equal<(NodeId * int) list>(
        ownerPathIngress graph shared, result.zoomIngress)

    match resolveZoomOutParent result.graph result.zoomRoot result.zoomIngress with
    | None -> Assert.True(false, "Expected Some")
    | Some (parentId, index, rest) ->
        Assert.Equal(ownerParent, parentId)
        Assert.Equal<(NodeId * int) list>(
            ownerPathIngress graph ownerParent, rest)
        let _, ownerIndex, _ = getOwnerOccurrence graph shared
        Assert.Equal(ownerIndex, index)

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
let ``Graph.create bootstraps TRASH under root as Directory with TRASH name`` () =
    let graph = Graph.create ()
    let rootNode = graph.nodes.[graph.root]
    let trashChildOpt =
        rootNode.children
        |> List.tryFind (fun c -> c.id = Graph.trashId && c.ref = Ownership.Owner)
    Assert.True(trashChildOpt.IsSome)
    let trashNode = graph.nodes.[Graph.trashId]
    match trashNode.kind with
    | Special Directory -> Assert.Equal(Filename.Ok "TRASH", trashNode.name)
    | _ -> Assert.True(false, "Trash node must have kind = Special Directory")

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
