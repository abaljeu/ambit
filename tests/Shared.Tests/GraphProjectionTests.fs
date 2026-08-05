module Gambol.Shared.Tests.GraphProjectionTests

open System
open Xunit
open Gambol.Shared

let private owned (ids: NodeId list) =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private applyChange (ops: Op list) (graph: Graph) : Graph =
    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops = ops }

    match History.applyChange change { graph = graph; history = History.empty; revision = Revision 0 } with
    | ApplyResult.Changed st -> st.graph
    | _ -> failwith "expected Changed"

let private specialFileUnderRoot () : Graph * NodeId =
    let fileId = NodeId.New()
    let g0 = Graph.create ()
    let idx = Graph.fileTreeInsertIndex g0 Graph.rootId
    let g =
        applyChange
            [ Op.NewSpecialNode(fileId, File, "file1")
              Op.Replace(Graph.rootId, idx, [], owned [ fileId ]) ]
            g0
    g, fileId

let private workspaceWithDirectory () : Graph * NodeId * NodeId =
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let g0 = Graph.create ()
    let g1 =
        applyChange
            [ Op.NewSpecialNode(wsId, Workspace, "my-ws")
              Op.Replace(Graph.workspacesId, 0, [], owned [ wsId ]) ]
            g0
    let g2 =
        applyChange
            [ Op.NewSpecialNode(dirId, Directory, "subdir")
              Op.Replace(wsId, 0, [], owned [ dirId ]) ]
            g1
    g2, wsId, dirId

let private assertKind (expected: SpecialKind) (node: Node) =
    match node.kind with
    | Special k when k = expected -> ()
    | k -> Assert.Fail(sprintf "expected Special %A, got %A" expected k)

[<Fact>]
let ``graphRoundTrip matches snapshot read of written empty graph`` () =
    let g0 = Graph.create ()
    let g1 = Snapshot.write g0 |> Snapshot.read

    match GraphProjection.graphRoundTrip g1 with
    | Error e -> Assert.Fail(e)
    | Ok g2 -> Assert.True(GraphProjection.graphEquals g1 g2)

[<Fact>]
let ``graphEquals is true for same graph`` () =
    let g = Graph.create ()
    Assert.True(GraphProjection.graphEquals g g)

[<Fact>]
let ``graphEquals distinguishes Unloaded from Loaded empty`` () =
    let g0 = Graph.create ()
    let id = NodeId.New()
    let unloaded = Node.Create(id, text = "hollow", childrenStatus = Unloaded)
    let loadedEmpty = Node.Create(id, text = "hollow")
    let gUnloaded = Graph.fromNodes g0.root (g0.nodes |> Map.add id unloaded)
    let gLoaded = Graph.fromNodes g0.root (g0.nodes |> Map.add id loadedEmpty)
    Assert.False(GraphProjection.graphEquals gUnloaded gLoaded)

[<Fact>]
let ``graphRoundTrip rebuilds Unloaded as Loaded`` () =
    let g0 = Graph.create ()
    let id = NodeId.New()
    let unloaded = Node.Create(id, text = "hollow", childrenStatus = Unloaded)
    let g1 = Graph.fromNodes g0.root (g0.nodes |> Map.add id unloaded)
    match GraphProjection.graphRoundTrip g1 with
    | Error e -> Assert.Fail(e)
    | Ok g2 ->
        Assert.Equal(Loaded, g2.nodes.[id].childrenStatus)
        Assert.True(g2.nodes |> Map.forall (fun _ n -> n.childrenStatus = Loaded))

[<Fact>]
let ``graphRoundTrip preserves default graph`` () =
    let g = Graph.create ()

    match GraphProjection.graphRoundTrip g with
    | Error e -> Assert.Fail(e)
    | Ok g2 ->
        Assert.True(GraphProjection.graphEquals g g2)

        match g2.nodes.[Graph.trashId].kind with
        | Special Directory -> ()
        | k -> Assert.Fail(sprintf "trash kind after SQL round-trip: %A" k)

[<Fact>]
let ``graphRoundTrip preserves graph with child`` () =
    let g0 = Graph.create ()
    let childId = NodeId.New()

    let change =
        { id = 0
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, "x")
              Op.Replace(Graph.rootId, 0, [], [ { ref = Ownership.Owner; id = childId } ]) ] }

    match History.applyChange change { graph = g0; history = History.empty; revision = Revision 0 } with
    | ApplyResult.Changed st ->
        match GraphProjection.graphRoundTrip st.graph with
        | Error e -> Assert.Fail(e)
        | Ok g2 -> Assert.True(GraphProjection.graphEquals st.graph g2)
    | _ -> Assert.Fail("expected Changed")

[<Fact>]
let ``toDbPrecision preserves missing sentinel`` () =
    Assert.Equal(NodeUpdateTime.missing, NodeUpdateTime.toDbPrecision NodeUpdateTime.missing)

[<Fact>]
let ``toDbPrecision treats Unspecified timestamptz as UTC clock`` () =
    let utc = DateTime(2025, 3, 15, 10, 30, 0, DateTimeKind.Utc)
    let stored = NodeUpdateTime.toDbPrecision utc
    let npgsqlStyle = DateTime(stored.Ticks, DateTimeKind.Unspecified)
    Assert.Equal(stored, NodeUpdateTime.toDbPrecision npgsqlStyle)

[<Fact>]
let ``graphRoundTrip preserves updateTime`` () =
    let stamp = DateTime(2025, 3, 15, 10, 30, 0, DateTimeKind.Utc)
    let g0 = Graph.create ()
    let childId = NodeId.New()

    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, "stamped")
              Op.Replace(Graph.rootId, 0, [], [ { ref = Ownership.Owner; id = childId } ]) ] }

    match History.applyChange change { graph = g0; history = History.empty; revision = Revision 0 } with
    | ApplyResult.Changed st ->
        let stamped =
            { st.graph with
                  nodes =
                      st.graph.nodes
                      |> Map.add childId { st.graph.nodes.[childId] with updateTime = stamp } }

        match GraphProjection.graphRoundTrip stamped with
        | Error e -> Assert.Fail(e)
        | Ok g2 -> Assert.Equal(stamp, g2.nodes.[childId].updateTime)
    | _ -> Assert.Fail("expected Changed")

[<Fact>]
let ``graphEquals is false when text differs`` () =
    let g0 = Graph.create ()
    let childId = NodeId.New()

    let change =
        { id = 0
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, "alpha")
              Op.Replace(Graph.rootId, 0, [], [ { ref = Ownership.Owner; id = childId } ]) ] }

    match History.applyChange change { graph = g0; history = History.empty; revision = Revision 0 } with
    | ApplyResult.Changed st ->
        match Graph.setText childId "alpha" "beta" st.graph with
        | Ok g1 -> Assert.False(GraphProjection.graphEquals st.graph g1)
        | Error e -> Assert.Fail(e)
    | _ -> Assert.Fail("expected Changed")

[<Fact>]
let ``graphFromPersistence fails when root missing from nodes`` () =
    let root = Graph.rootId
    let rows: GraphProjection.NodePersistenceRow list = []

    let err =
        GraphProjection.graphFromPersistence root rows []

    match err with
    | Error _ -> ()
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``graphFromPersistence fails when ordinals not dense`` () =
    let root = Graph.rootId
    let cid = System.Guid.NewGuid()

    let nr: GraphProjection.NodePersistenceRow list =
        [ { id = root.Value
            text = "ROOT"
            name = None
            kind = "normal"
            documentState = "current"
            cssClassNames = []
            updateTime = NodeUpdateTime.missing }
          { id = cid
            text = "c"
            name = None
            kind = "normal"
            documentState = "current"
            cssClassNames = []
            updateTime = NodeUpdateTime.missing } ]

    let cr: GraphProjection.ChildPersistenceRow list =
        [ { parentId = root.Value
            ordinal = 1
            childId = cid
            ownership = Ownership.Owner } ]

    match GraphProjection.graphFromPersistence root nr cr with
    | Error _ -> ()
    | Ok _ -> Assert.Fail("expected Error")

[<Fact>]
let ``graphRoundTrip preserves Special File under root`` () =
    let original, fileId = specialFileUnderRoot ()

    match GraphProjection.graphRoundTrip original with
    | Error e -> Assert.Fail(e)
    | Ok g2 ->
        assertKind File g2.nodes.[fileId]
        Assert.Equal(Filename.create "file1", g2.nodes.[fileId].name)

[<Fact>]
let ``graphRoundTrip preserves Special Workspace and Directory`` () =
    let original, wsId, dirId = workspaceWithDirectory ()

    match GraphProjection.graphRoundTrip original with
    | Error e -> Assert.Fail(e)
    | Ok g2 ->
        assertKind Workspace g2.nodes.[wsId]
        assertKind Directory g2.nodes.[dirId]
        Assert.Equal(Filename.create "my-ws", g2.nodes.[wsId].name)
        Assert.Equal(Filename.create "subdir", g2.nodes.[dirId].name)

[<Fact>]
let ``graphRoundTrip preserves unparsed document state`` () =
    let original, fileId = specialFileUnderRoot ()
    let unparsed =
        { original with
              nodes =
                  original.nodes
                  |> Map.add
                      fileId
                      { original.nodes.[fileId] with
                          documentState = Unparsed } }
    match GraphProjection.graphRoundTrip unparsed with
    | Error err -> Assert.Fail(err)
    | Ok graph -> Assert.Equal(Unparsed, graph.nodes.[fileId].documentState)

[<Fact>]
let ``graphRoundTrip preserves no-server-file document state`` () =
    let original, fileId = specialFileUnderRoot ()
    let absent =
        { original with
              nodes =
                  original.nodes
                  |> Map.add
                      fileId
                      { original.nodes.[fileId] with
                          documentState = NoServerFile } }
    match GraphProjection.graphRoundTrip absent with
    | Error err -> Assert.Fail(err)
    | Ok graph -> Assert.Equal(NoServerFile, graph.nodes.[fileId].documentState)

[<Fact>]
let ``graphEquals is false when kind differs`` () =
    let g0, fileId = specialFileUnderRoot ()
    let normalNode = { g0.nodes.[fileId] with kind = Normal }
    let g1 = { g0 with nodes = g0.nodes |> Map.add fileId normalNode }
    Assert.False(GraphProjection.graphEquals g0 g1)

[<Fact>]
let ``graphFromPersistence legacy normal kind maps canonical trash to Directory`` () =
    let root = Graph.rootId
    let trash = Graph.trashId

    let nr: GraphProjection.NodePersistenceRow list =
        [ { id = root.Value
            text = "ROOT"
            name = None
            kind = "workspace"
            documentState = "current"
            cssClassNames = []
            updateTime = NodeUpdateTime.missing }
          { id = Graph.workspacesId.Value
            text = "Workspaces"
            name = None
            kind = "workspaces"
            documentState = "current"
            cssClassNames = []
            updateTime = NodeUpdateTime.missing }
          { id = trash.Value
            text = "Trash"
            name = None
            kind = "normal"
            documentState = "current"
            cssClassNames = []
            updateTime = NodeUpdateTime.missing } ]

    let cr: GraphProjection.ChildPersistenceRow list =
        [ { parentId = root.Value
            ordinal = 0
            childId = Graph.workspacesId.Value
            ownership = Ownership.Owner }
          { parentId = root.Value
            ordinal = 1
            childId = trash.Value
            ownership = Ownership.Owner } ]

    match GraphProjection.graphFromPersistence root nr cr with
    | Error e -> Assert.Fail(e)
    | Ok g ->
        assertKind Directory g.nodes.[trash]
        Assert.Equal(Filename.Ok "TRASH", g.nodes.[trash].name)
