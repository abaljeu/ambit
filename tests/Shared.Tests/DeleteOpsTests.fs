module DeleteOpsTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private owned = ChildNode.owners

let private ref_ = ChildNode.reference

/// Build graph: root -> [a(owner), b(owner)]. Returns graph, a, b.
let private buildTwoSiblings () : Graph * NodeId * NodeId =
    let g0 = Graph.create ()
    let g1, ids = ModelBuilder.createNodes [ "a"; "b" ] g0
    let a = ids.[0]
    let b = ids.[1]
    let g2 =
        Graph.replace g1.root 0 [] (owned [ a; b ]) g1
        |> ModelBuilder.requireOk "root->[a,b]"
    g2, a, b

/// Build initial State from graph.
let private stateOf (graph: Graph) : State =
    { graph = graph; history = History.empty; revision = Revision.Zero }

/// SiteNodeRange for root's children [start, endd).
let private rootRange (graph: Graph) (start: int) (endd: int) : SiteNodeRange =
    let siteMap, _ = buildSiteMap graph
    { parent = siteMap.entries.[siteMap.rootId]; start = start; endd = endd }

/// SiteNodeRange for TRASH's children [start, endd).
let private trashRange (graph: Graph) (start: int) (endd: int) : SiteNodeRange =
    let siteMap, _ = buildSiteMap graph
    let trashEntry =
        siteMap.entries
        |> Map.values
        |> Seq.find (fun e -> e.nodeId = Graph.trashId)
    { parent = trashEntry; start = start; endd = endd }

/// SiteNodeRange for Workspaces' children [start, endd).
let private workspacesRange (graph: Graph) (start: int) (endd: int) : SiteNodeRange =
    let siteMap, _ = buildSiteMap graph
    let workspacesEntry =
        siteMap.entries
        |> Map.values
        |> Seq.find (fun e -> e.nodeId = Graph.workspacesId)
    { parent = workspacesEntry; start = start; endd = endd }

// ---------------------------------------------------------------------------
// Multi-sibling MoveToTrash
// ---------------------------------------------------------------------------

[<Fact>]
let ``planDeleteOps multi-sibling: change applies and both nodes land under TRASH`` () =
    let graph, a, b = buildTwoSiblings ()
    let range = rootRange graph 0 2
    let classified = ViewModelDeleteOps.classifyDeleteForSelection graph range
    Assert.Equal(2, classified.Length)
    let ops = ViewModelDeleteOps.planDeleteOps graph range classified
    let change = { id = 0; changeId = System.Guid.NewGuid(); ops = ops }
    let result = History.applyChange change (stateOf graph)
    match result with
    | ApplyResult.Invalid(_, msg) -> Assert.True(false, $"Invalid: {msg}")
    | ApplyResult.Unchanged _ -> Assert.True(false, "Expected Changed")
    | ApplyResult.Changed s ->
        let trashChildren = s.graph.nodes.[Graph.trashId].children
        let aUnderTrash = trashChildren |> List.exists (fun c -> c.id = a)
        let bUnderTrash = trashChildren |> List.exists (fun c -> c.id = b)
        Assert.True(aUnderTrash, "a should be under TRASH")
        Assert.True(bUnderTrash, "b should be under TRASH")
        let rootChildren = s.graph.nodes.[s.graph.root].children
        Assert.False(rootChildren |> List.exists (fun c -> c.id = a), "a should not be under root")
        Assert.False(rootChildren |> List.exists (fun c -> c.id = b), "b should not be under root")

// ---------------------------------------------------------------------------
// LocalDeleteWithPromotion
// ---------------------------------------------------------------------------

[<Fact>]
let ``planDeleteOps promotion: ref promoted to owner, original owner row removed`` () =
    // graph: root -> [a(owner), x(owner)], x -> [a(ref)]
    let g0 = Graph.create ()
    let g1, ids = ModelBuilder.createNodes [ "a"; "x" ] g0
    let a = ids.[0]
    let x = ids.[1]
    let g2 =
        Graph.replace g1.root 0 [] (owned [ a; x ]) g1
        |> ModelBuilder.requireOk "root->[a,x]"
    let g3 =
        Graph.replace x 0 [] [ ref_ a ] g2
        |> ModelBuilder.requireOk "x->[a(ref)]"
    // select only a under root (index 0)
    let range = rootRange g3 0 1
    let classified = ViewModelDeleteOps.classifyDeleteForSelection g3 range
    Assert.Equal(1, classified.Length)
    Assert.Equal(ViewModelDeleteOps.LocalDeleteWithPromotion, classified.[0].action)
    let ops = ViewModelDeleteOps.planDeleteOps g3 range classified
    let change = { id = 0; changeId = System.Guid.NewGuid(); ops = ops }
    let result = History.applyChange change (stateOf g3)
    match result with
    | ApplyResult.Invalid(_, msg) -> Assert.True(false, $"Invalid: {msg}")
    | ApplyResult.Unchanged _ -> Assert.True(false, "Expected Changed")
    | ApplyResult.Changed s ->
        // a no longer under root
        let rootChildren = s.graph.nodes.[s.graph.root].children
        Assert.False(rootChildren |> List.exists (fun c -> c.id = a), "a should not be under root")
        // x's a-child is now Owner
        let xChildren = s.graph.nodes.[x].children
        let aInX = xChildren |> List.tryFind (fun c -> c.id = a)
        match aInX with
        | None -> Assert.True(false, "a should still exist under x")
        | Some c -> Assert.Equal(Ownership.Owner, c.ref)

// ---------------------------------------------------------------------------
// TRASH node in range → all-or-nothing cancel
// ---------------------------------------------------------------------------

[<Fact>]
let ``classifyDeleteForSelection returns empty when TRASH is in range`` () =
    // TRASH is always a child of root; insert 'a' before it so range [0,2) covers [a, TRASH].
    let g0 = Graph.create ()
    let g1, ids = ModelBuilder.createNodes [ "a" ] g0
    let a = ids.[0]
    let g2 =
        Graph.replace g1.root 0 [] (owned [ a ]) g1
        |> ModelBuilder.requireOk "root->[a, ...TRASH]"
    // root.children = [a(Owner), WORKSPACES, SYSTEM, TRASH]; range covers all
    let range = rootRange g2 0 3
    let classified = ViewModelDeleteOps.classifyDeleteForSelection g2 range
    // TRASH in range must cancel entire classification (all-or-nothing)
    Assert.Empty(classified)

[<Fact>]
let ``classifyDeleteForSelection returns empty for workspace delete`` () =
    let ws = NodeId.New()
    let g0 = Graph.create ()
    let g1 =
        History.applyChange
            { id = 0
              changeId = System.Guid.NewGuid()
              ops =
                [ Op.NewSpecialNode(ws, Workspace, "home")
                  Op.Replace(Graph.workspacesId, 0, [], owned [ ws ]) ] }
            (stateOf g0)
    match g1 with
    | ApplyResult.Changed s ->
        let range = workspacesRange s.graph 0 1
        let classified = ViewModelDeleteOps.classifyDeleteForSelection s.graph range
        Assert.Empty(classified)
    | ApplyResult.Invalid(_, msg) -> Assert.True(false, $"Invalid: {msg}")
    | ApplyResult.Unchanged _ -> Assert.True(false, "Expected Changed")

[<Fact>]
let ``classifyDeleteForSelection cancels whole selection when workspace is in range`` () =
    let ws = NodeId.New()
    let other = NodeId.New()
    let g0 = Graph.create ()
    let g1 =
        History.applyChange
            { id = 0
              changeId = System.Guid.NewGuid()
              ops =
                [ Op.NewSpecialNode(ws, Workspace, "home")
                  Op.NewNode(other, "docs")
                  Op.Replace(Graph.workspacesId, 0, [], owned [ ws; other ]) ] }
            (stateOf g0)
    match g1 with
    | ApplyResult.Changed s ->
        let range = workspacesRange s.graph 0 2
        let classified = ViewModelDeleteOps.classifyDeleteForSelection s.graph range
        Assert.Empty(classified)
    | ApplyResult.Invalid(_, msg) -> Assert.True(false, $"Invalid: {msg}")
    | ApplyResult.Unchanged _ -> Assert.True(false, "Expected Changed")

// ---------------------------------------------------------------------------
// Single-node regression
// ---------------------------------------------------------------------------

[<Fact>]
let ``planDeleteOps single node MoveToTrash still works`` () =
    let g0 = Graph.create ()
    let g1, ids = ModelBuilder.createNodes [ "a" ] g0
    let a = ids.[0]
    let g2 =
        Graph.replace g1.root 0 [] (owned [ a ]) g1
        |> ModelBuilder.requireOk "root->[a]"
    let range = rootRange g2 0 1
    let classified = ViewModelDeleteOps.classifyDeleteForSelection g2 range
    Assert.Equal(1, classified.Length)
    let ops = ViewModelDeleteOps.planDeleteOps g2 range classified
    let change = { id = 0; changeId = System.Guid.NewGuid(); ops = ops }
    let result = History.applyChange change (stateOf g2)
    match result with
    | ApplyResult.Invalid(_, msg) -> Assert.True(false, $"Invalid: {msg}")
    | ApplyResult.Unchanged _ -> Assert.True(false, "Expected Changed")
    | ApplyResult.Changed s ->
        let trashChildren = s.graph.nodes.[Graph.trashId].children
        Assert.True(trashChildren |> List.exists (fun c -> c.id = a), "a should be under TRASH")
        let rootChildren = s.graph.nodes.[s.graph.root].children
        Assert.False(rootChildren |> List.exists (fun c -> c.id = a), "a should not be under root")

// ---------------------------------------------------------------------------
// Hard-delete from TRASH (permanent deletion)
// ---------------------------------------------------------------------------

[<Fact>]
let ``planDeleteOps single item hard-delete from TRASH: item removed permanently`` () =
    // Put a single node under TRASH directly, then hard-delete it.
    let g0 = Graph.create ()
    let g1, ids = ModelBuilder.createNodes [ "a" ] g0
    let a = ids.[0]
    let g2 =
        let trashLen = g1.nodes.[Graph.trashId].children.Length
        Graph.replace Graph.trashId trashLen [] [ ChildNode.owner a ] g1
        |> ModelBuilder.requireOk "trash->[a]"
    let range = trashRange g2 0 1
    let classified = ViewModelDeleteOps.classifyDeleteForSelection g2 range
    Assert.Equal(1, classified.Length)
    Assert.Equal(ViewModelDeleteOps.HardDeleteSubtreeInTrash, classified.[0].action)
    let ops = ViewModelDeleteOps.planDeleteOps g2 range classified
    let change = { id = 0; changeId = System.Guid.NewGuid(); ops = ops }
    let result = History.applyChange change (stateOf g2)
    match result with
    | ApplyResult.Invalid(_, msg) -> Assert.True(false, $"Invalid: {msg}")
    | ApplyResult.Unchanged _ -> Assert.True(false, "Expected Changed, not Unchanged")
    | ApplyResult.Changed s ->
        let trashChildren = s.graph.nodes.[Graph.trashId].children
        Assert.False(trashChildren |> List.exists (fun c -> c.id = a), "a should be gone from TRASH")

[<Fact>]
let ``planDeleteOps multi-item hard-delete from TRASH: both items removed permanently`` () =
    // Put two nodes under TRASH, then hard-delete both via multi-selection.
    let g0 = Graph.create ()
    let g1, ids = ModelBuilder.createNodes [ "a"; "b" ] g0
    let a = ids.[0]
    let b = ids.[1]
    let g2 =
        let trashLen = g1.nodes.[Graph.trashId].children.Length
        Graph.replace Graph.trashId trashLen []
            [ ChildNode.owner a; ChildNode.owner b ] g1
        |> ModelBuilder.requireOk "trash->[a,b]"
    let range = trashRange g2 0 2
    let classified = ViewModelDeleteOps.classifyDeleteForSelection g2 range
    Assert.Equal(2, classified.Length)
    Assert.True(
        classified |> List.forall (fun c -> c.action = ViewModelDeleteOps.HardDeleteSubtreeInTrash),
        "all items should be HardDeleteSubtreeInTrash")
    let ops = ViewModelDeleteOps.planDeleteOps g2 range classified
    let change = { id = 0; changeId = System.Guid.NewGuid(); ops = ops }
    let result = History.applyChange change (stateOf g2)
    match result with
    | ApplyResult.Invalid(_, msg) -> Assert.True(false, $"Invalid: {msg}")
    | ApplyResult.Unchanged _ -> Assert.True(false, "Expected Changed, not Unchanged")
    | ApplyResult.Changed s ->
        let trashChildren = s.graph.nodes.[Graph.trashId].children
        Assert.False(trashChildren |> List.exists (fun c -> c.id = a), "a should be gone from TRASH")
        Assert.False(trashChildren |> List.exists (fun c -> c.id = b), "b should be gone from TRASH")

// ---------------------------------------------------------------------------
// Special Directory MoveToTrash
// ---------------------------------------------------------------------------

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) : Node =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private workspaceDirectoryGraph () : Graph * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let graph1 =
        graph0.nodes
        |> Map.add wsId (specialNode wsId Workspace "home" Graph.workspacesId)
        |> Map.add dirId (specialNode dirId Directory "docs" wsId)
        |> fun nodes -> Graph.fromNodes graph0.root nodes
    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> ModelBuilder.requireOk "workspaces->ws"
    let graph3 =
        Graph.replace wsId 0 [] (owned [ dirId ]) graph2
        |> ModelBuilder.requireOk "ws->dir"
    graph3, wsId, dirId

let private parentRange (parentId: NodeId) (start: int) (endd: int) : SiteNodeRange =
    { parent =
        { instanceId = Sid 0
          nodeId = parentId
          parentInstanceId = None
          childIndex = 0
          expanded = true
          childrenStale = false
          children = [] }
      start = start
      endd = endd }

[<Fact>]
let ``planDeleteOps Directory under workspace moves to TRASH`` () =
    let graph, wsId, dirId = workspaceDirectoryGraph ()
    let range = parentRange wsId 0 1
    let classified = ViewModelDeleteOps.classifyDeleteForSelection graph range
    Assert.Equal(1, classified.Length)
    Assert.Equal(ViewModelDeleteOps.MoveToTrash, classified.[0].action)
    let ops = ViewModelDeleteOps.planDeleteOps graph range classified
    let change = { id = 0; changeId = System.Guid.NewGuid(); ops = ops }
    let result = History.applyChange change (stateOf graph)
    match result with
    | ApplyResult.Invalid(_, msg) -> Assert.True(false, $"Invalid: {msg}")
    | ApplyResult.Unchanged _ -> Assert.True(false, "Expected Changed")
    | ApplyResult.Changed s ->
        let trashChildren = s.graph.nodes.[Graph.trashId].children
        Assert.True(
            trashChildren |> List.exists (fun c -> c.id = dirId && c.ref = Ownership.Owner),
            "directory should be under TRASH")
        Assert.False(
            s.graph.nodes.[wsId].children |> List.exists (fun c -> c.id = dirId),
            "directory should leave workspace")

[<Fact>]
let ``planDeleteOps second Directory with same name as one already in TRASH applies`` () =
    let graph0, wsId, dir1 = workspaceDirectoryGraph ()
    let range1 = parentRange wsId 0 1
    let ops1 =
        ViewModelDeleteOps.planDeleteOps
            graph0
            range1
            (ViewModelDeleteOps.classifyDeleteForSelection graph0 range1)
    let graph1 =
        match History.applyChange { id = 0; changeId = System.Guid.NewGuid(); ops = ops1 } (stateOf graph0) with
        | ApplyResult.Changed s -> s.graph
        | r -> failwithf "first delete failed: %A" r
    let dir2 = NodeId.New()
    let graph2 =
        graph1.nodes
        |> Map.add dir2 (specialNode dir2 Directory "docs" wsId)
        |> fun nodes -> Graph.fromNodes graph1.root nodes
        |> fun g -> Graph.replace wsId 0 [] (owned [ dir2 ]) g
        |> ModelBuilder.requireOk "ws->dir2"
    let range2 = parentRange wsId 0 1
    let ops2 =
        ViewModelDeleteOps.planDeleteOps
            graph2
            range2
            (ViewModelDeleteOps.classifyDeleteForSelection graph2 range2)
    let result =
        History.applyChange { id = 1; changeId = System.Guid.NewGuid(); ops = ops2 } (stateOf graph2)
    match result with
    | ApplyResult.Invalid(_, msg) -> Assert.True(false, $"Invalid: {msg}")
    | ApplyResult.Unchanged _ -> Assert.True(false, "Expected Changed")
    | ApplyResult.Changed s ->
        let trashChildren = s.graph.nodes.[Graph.trashId].children
        Assert.True(trashChildren |> List.exists (fun c -> c.id = dir1), "dir1 stays in TRASH")
        Assert.True(trashChildren |> List.exists (fun c -> c.id = dir2), "dir2 should join TRASH")
        Assert.Equal(Filename.Ok "docs", s.graph.nodes.[dir1].name)
        Assert.Equal(Filename.Ok "docs1", s.graph.nodes.[dir2].name)
