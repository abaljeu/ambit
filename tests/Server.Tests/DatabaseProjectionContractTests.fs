module Gambol.Server.Tests.DatabaseProjectionContractTests

open System
open System.Data.Common
open Xunit
open Gambol.Server
open Gambol.Shared
open Npgsql
open Gambol.Server.Tests.TestBackend

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private id value =
    NodeId(Guid.Parse($"20000000-0000-0000-0000-{value:D12}"))

let private stamp value =
    DateTime(2026, 7, 24, 12, value, 0, DateTimeKind.Utc)

let private change ops =
    { id = 0
      changeId = Guid.NewGuid()
      ops = ops }

let private graphWithCustomNodes nodes =
    let customNodes = nodes |> List.map (fun (node: Node) -> node.id, node)

    (Graph.create ()).nodes
    |> Map.toList
    |> List.append customNodes
    |> Map.ofList
    |> Graph.fromNodes Graph.rootId

let private replaceProjection connStr graph revision = task {
    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do! Database.replaceGraphProjectionWithTx tx graph revision |> Async.AwaitTask
    tx.Commit()
}

let private persistPatch connStr graph revision changes = task {
    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    let patch = DatabaseProjection.plan graph revision changes
    do! DatabaseProjection.persistWithTx tx graph patch |> Async.AwaitTask
    tx.Commit()
}

let private sweep connStr = task {
    let! result =
        DatabaseProjection.startupSweepPatch
        |> DatabaseProjection.maintenanceCommand
        |> DatabaseProjection.executeMaintenance connStr
    match result with
    | Ok r -> return r.deletedIds
    | Error e -> return failwith e
}

let private scalarById<'a> connStr sql idValue = task {
    use conn = new NpgsqlConnection(connStr)
    do! conn.OpenAsync()
    use command = new NpgsqlCommand(sql, conn)
    command.Parameters.AddWithValue("id", idValue) |> ignore
    let! result = command.ExecuteScalarAsync()
    return unbox<'a> result
}

let private scalar<'a> connStr sql = task {
    use conn = new NpgsqlConnection(connStr)
    do! conn.OpenAsync()
    use command = new NpgsqlCommand(sql, conn)
    let! result = command.ExecuteScalarAsync()
    return unbox<'a> result
}

let private encodeBatch (changes: Change list) = changes

let private exec connStr sql (parameters: (string * obj) list) = task {
    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use command = new NpgsqlCommand(sql, conn)
    parameters
    |> List.iter (fun (name, value) ->
        command.Parameters.AddWithValue(name, value) |> ignore)
    let! _ = command.ExecuteNonQueryAsync()
    return ()
}

let private readChildRows (reader: DbDataReader) =
    let rec loop acc =
        if reader.Read() then
            loop ((reader.GetGuid 0, reader.GetInt32 1, reader.GetString 2) :: acc)
        else
            List.rev acc
    loop []

let private readRootChildren connStr = task {
    use conn = new NpgsqlConnection(connStr)
    do! conn.OpenAsync()
    use command =
        new NpgsqlCommand(
            """
            SELECT child_id, ordinal, ownership
            FROM node_children
            WHERE parent_id = '00000000-0000-0000-0000-000000000000'
            ORDER BY ordinal
            """,
            conn)
    use! reader = command.ExecuteReaderAsync()
    return readChildRows reader
}
[<Fact>]
let ``writer upserts complete nodes children revision and reloads`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    do! replaceProjection connStr (Graph.create ()) 0

    let parentId, firstId, secondId = id 50, id 51, id 52
    let first = Node.Create(firstId, text = "first")
    let second = Node.Create(secondId, text = "second")
    let firstRef = ChildNode.owner firstId
    let secondRef = ChildNode.reference secondId

    let initial =
        Node.Create(
            parentId,
            text = "initial",
            name = Filename.create "initial.amb",
            children = [ firstRef; secondRef ],
            kind = Special Directory,
            updateTime = stamp 2)

    let initialGraph = graphWithCustomNodes [ initial; first; second ]
    let createOps =
        [ Op.NewSpecialNode(parentId, Directory, "initial.amb")
          Op.NewNode(firstId, "first")
          Op.NewNode(secondId, "second")
          Op.Replace(parentId, [], initial.children) ]
    do! persistPatch connStr initialGraph 1 [ change createOps ]

    let final =
        { initial with
            text = "renamed.amb"
            name = Filename.create "renamed.amb"
            children = [ secondRef; firstRef ]
            cssClasses = CssClass.ofList [ "new-class" ]
            documentState = Unparsed
            updateTime = stamp 4 }
    let finalGraph = graphWithCustomNodes [ final; first; second ]
    let updateOps =
        [ Op.SetName(parentId, "initial.amb", "renamed.amb")
          Op.SetClasses(parentId, CssClass.empty, final.cssClasses)
          Op.SetDocumentState(parentId, Current, Unparsed)
          Op.SetUpdateTime(parentId, stamp 2, stamp 4)
          Op.Replace(parentId, initial.children, final.children) ]
    do! persistPatch connStr finalGraph 2 [ change updateOps ]

    use conn = new NpgsqlConnection(connStr)
    do! conn.OpenAsync()
    use command = conn.CreateCommand()
    command.CommandText <-
        """
        SELECT text, name, kind, document_state, css_classes::text, update_time
        FROM nodes WHERE id = @id
        """
    command.Parameters.AddWithValue("id", parentId.Value) |> ignore
    use! reader = command.ExecuteReaderAsync()
    Assert.True(reader.Read())
    Assert.Equal("renamed.amb", reader.GetString(0))
    Assert.Equal("renamed.amb", reader.GetString(1))
    Assert.Equal("directory", reader.GetString(2))
    Assert.Equal("unparsed", reader.GetString(3))
    Assert.Equal("[\"new-class\"]", reader.GetString(4))
    Assert.Equal(stamp 4, reader.GetDateTime(5))
    do! reader.DisposeAsync()

    let! childCount =
        scalarById<int64> connStr
            "SELECT count(*) FROM node_children WHERE parent_id = @id"
            parentId.Value
    let! revision = scalar<int> connStr "SELECT revision FROM graph WHERE singleton = 1"
    Assert.Equal(2L, childCount)
    Assert.Equal(2, revision)

    let! loaded = Database.tryLoadGraphFromProjection connStr |> Async.AwaitTask
    match loaded with
    | Error error -> Assert.Fail(error)
    | Ok (graph, loadedRevision) ->
        Assert.Equal(2, loadedRevision)
        Assert.True(GraphProjection.graphEquals finalGraph graph)
}

[<Fact>]
let ``writer clears one parent without rewriting unrelated rows and rolls back`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let parentAId, parentBId = id 60, id 61
    let childAId, childBId = id 62, id 63
    let childA, childB = Node.Create(childAId), Node.Create(childBId)
    let edgeA = ChildNode.owner childAId
    let edgeB = ChildNode.owner childBId
    let parentA = Node.Create(parentAId, text = "before", children = [ edgeA ])
    let parentB = Node.Create(parentBId, text = "unrelated", children = [ edgeB ])
    let initial = graphWithCustomNodes [ parentA; parentB; childA; childB ]
    do! replaceProjection connStr initial 5

    let xminSql table whereClause =
        $"SELECT xmin::text FROM {table} WHERE {whereClause}"
    let! nodeXminBefore =
        scalarById<string> connStr (xminSql "nodes" "id = @id") parentBId.Value
    let! edgeXminBefore =
        scalarById<string> connStr
            (xminSql "node_children" "parent_id = @id AND ordinal = 0")
            parentBId.Value

    let clearedA = { parentA with text = "after"; children = [] }
    let final = graphWithCustomNodes [ clearedA; parentB; childA; childB ]
    let ops =
        [ Op.SetText(parentAId, "before", "after")
          Op.Replace(parentAId, [ edgeA ], []) ]
    do! persistPatch connStr final 6 [ change ops ]

    let! remaining =
        scalarById<int64> connStr
            "SELECT count(*) FROM node_children WHERE parent_id = @id"
            parentAId.Value
    let! nodeXminAfter =
        scalarById<string> connStr (xminSql "nodes" "id = @id") parentBId.Value
    let! edgeXminAfter =
        scalarById<string> connStr
            (xminSql "node_children" "parent_id = @id AND ordinal = 0")
            parentBId.Value
    Assert.Equal(0L, remaining)
    Assert.Equal(nodeXminBefore, nodeXminAfter)
    Assert.Equal(edgeXminBefore, edgeXminAfter)

    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    let rolledBack =
        graphWithCustomNodes [ { clearedA with text = "rolled-back" }; parentB; childA; childB ]
    let patch =
        DatabaseProjection.plan rolledBack 7
            [ change [ Op.SetText(parentAId, "after", "rolled-back") ] ]
    do! DatabaseProjection.persistWithTx tx rolledBack patch |> Async.AwaitTask
    tx.Rollback()

    let! storedText =
        scalarById<string> connStr "SELECT text FROM nodes WHERE id = @id" parentAId.Value
    let! revision = scalar<int> connStr "SELECT revision FROM graph WHERE singleton = 1"
    Assert.Equal("after", storedText)
    Assert.Equal(6, revision)
}

[<Fact>]
let ``DbAgent bootstrap duplicate returns stored Change and rejects no-op`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let agent = DbAgent.create connStr
    let childId = id 70

    let accepted =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, "bootstrap")
              Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

    let core = DbAgent.coreChanges agent
    let! first = core.postChange (encodeBatch [ accepted ]) |> Async.StartAsTask
    let firstAck =
        match first with
        | Ok ack -> ack
        | Error err -> failwith err
    Assert.Equal(accepted.changeId, Assert.Single(firstAck.changes).changeId)
    let! xminAfterFirst =
        scalar<string> connStr "SELECT xmin::text FROM graph WHERE singleton = 1"

    let! duplicate =
        core.postChange (encodeBatch [ accepted ]) |> Async.StartAsTask
    match duplicate with
    | Ok ack ->
        Assert.Equal<Change list>(firstAck.changes, ack.changes)
    | Error err -> failwith err

    let noOp =
        { id = 1
          changeId = Guid.NewGuid()
          ops = [] }
    let! unchanged = core.postChange (encodeBatch [ noOp ]) |> Async.StartAsTask
    match unchanged with
    | Ok _ -> Assert.Fail("unchanged submission must be rejected")
    | Error err -> Assert.Contains("Unchanged", err)

    let! xminAfterNoWrites =
        scalar<string> connStr "SELECT xmin::text FROM graph WHERE singleton = 1"
    let! changeCount = scalar<int64> connStr "SELECT count(*) FROM changes"
    let! revision = scalar<int> connStr "SELECT revision FROM graph WHERE singleton = 1"
    Assert.Equal(xminAfterFirst, xminAfterNoWrites)
    Assert.Equal(1L, changeCount)
    Assert.Equal(1, revision)
}

[<Fact>]
let ``startup sweep deletes unreachable rows without rewriting reachable projection`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let reachableId, orphanId, orphanChildId = id 80, id 81, id 82
    let reachable =
        Node.Create(
            reachableId,
            text = "reachable",
            children = [ ChildNode.reference Graph.rootId ])
    let orphanChild = Node.Create(orphanChildId, text = "orphan child")
    let orphan =
        Node.Create(
            orphanId,
            text = "orphan",
            children = [ ChildNode.owner orphanChildId ])
    let graph0 = graphWithCustomNodes [ reachable; orphan; orphanChild ]
    let root = graph0.nodes.[Graph.rootId]
    let graph =
        graph0.nodes
        |> Map.add Graph.rootId
            { root with
                children =
                    ChildNode.owner reachableId :: root.children }
        |> Graph.fromNodes Graph.rootId
    do! replaceProjection connStr graph 12

    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do!
        Database.appendChangeWithTx tx 12 11 (Guid.NewGuid()) "{}"
        |> Async.AwaitTask
    tx.Commit()

    let! nodeXminBefore =
        scalarById<string> connStr
            "SELECT xmin::text FROM nodes WHERE id = @id"
            reachableId.Value
    let! edgeXminBefore =
        scalarById<string> connStr
            """
            SELECT xmin::text FROM node_children
            WHERE child_id = @id
            """
            reachableId.Value
    let! deleted = sweep connStr

    Assert.Equal<Guid list>(
        [ orphanId.Value; orphanChildId.Value ] |> List.sort,
        deleted |> List.sort)
    let! orphanRows =
        scalar<int64> connStr
            """
            SELECT count(*) FROM nodes
            WHERE id IN (
                '20000000-0000-0000-0000-000000000081',
                '20000000-0000-0000-0000-000000000082'
            )
            """
    let! incidentEdges =
        scalar<int64> connStr
            """
            SELECT count(*) FROM node_children
            WHERE parent_id = '20000000-0000-0000-0000-000000000081'
               OR child_id = '20000000-0000-0000-0000-000000000081'
            """
    let! nodeXminAfter =
        scalarById<string> connStr
            "SELECT xmin::text FROM nodes WHERE id = @id"
            reachableId.Value
    let! edgeXminAfter =
        scalarById<string> connStr
            """
            SELECT xmin::text FROM node_children
            WHERE child_id = @id
            """
            reachableId.Value
    let! revision = scalar<int> connStr "SELECT revision FROM graph WHERE singleton = 1"
    let! changeCount = scalar<int64> connStr "SELECT count(*) FROM changes"

    Assert.Equal(0L, orphanRows)
    Assert.Equal(0L, incidentEdges)
    Assert.Equal(nodeXminBefore, nodeXminAfter)
    Assert.Equal(edgeXminBefore, edgeXminAfter)
    Assert.Equal(12, revision)
    Assert.Equal(1L, changeCount)
}

[<Fact>]
let ``startup sweep is a no-op without graph singleton or orphans`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let! emptyDeleted = sweep connStr
    Assert.Empty(emptyDeleted)

    let graph = Graph.create ()
    do! replaceProjection connStr graph 3
    let! graphXminBefore =
        scalar<string> connStr "SELECT xmin::text FROM graph WHERE singleton = 1"
    let! noOrphansDeleted = sweep connStr
    let! graphXminAfter =
        scalar<string> connStr "SELECT xmin::text FROM graph WHERE singleton = 1"

    Assert.Empty(noOrphansDeleted)
    Assert.Equal(graphXminBefore, graphXminAfter)
}

[<Fact>]
let ``startup sweep preserves persisted reachable nodes absent from loaded subset`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let persistedId = id 90
    let persisted = Node.Create(persistedId, text = "not resident")
    let graph0 = graphWithCustomNodes [ persisted ]
    let root = graph0.nodes.[Graph.rootId]
    let persistedGraph =
        graph0.nodes
        |> Map.add Graph.rootId
            { root with
                children =
                    ChildNode.owner persistedId :: root.children }
        |> Graph.fromNodes Graph.rootId
    do! replaceProjection connStr persistedGraph 5

    let loadedSubset = Graph.create ()
    Assert.False(loadedSubset.nodes.ContainsKey persistedId)
    let! deleted = sweep connStr
    let trimmed =
        DatabaseProjection.trimDeletedNodes
            (deleted |> List.map NodeId)
            loadedSubset
    let! stillPersisted =
        scalarById<bool> connStr
            "SELECT EXISTS (SELECT 1 FROM nodes WHERE id = @id)"
            persistedId.Value

    Assert.Empty(deleted)
    Assert.False(trimmed.nodes.ContainsKey persistedId)
    Assert.True(stillPersisted)
}

[<Fact>]
let ``ownership repair does not bump revision or append changes`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let aId, uId = id 100, id 101
    let a = Node.Create(aId, text = "A")
    let u = Node.Create(uId, text = "U", children = [ ChildNode.owner aId ])
    let graph0 = graphWithCustomNodes [ a; u ]
    let ws = graph0.nodes.[Graph.workspacesId]
    let root = graph0.nodes.[Graph.rootId]
    let graph =
        graph0.nodes
        |> Map.add Graph.workspacesId
            { ws with children = ChildNode.owner aId :: ws.children }
        |> Map.add Graph.rootId
            { root with children = ChildNode.owner uId :: root.children }
        |> Graph.fromNodes Graph.rootId
    do! replaceProjection connStr graph 8

    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do!
        Database.appendChangeWithTx tx 8 7 (Guid.NewGuid()) "{}"
        |> Async.AwaitTask
    tx.Commit()

    let! deleted = sweep connStr
    Assert.Empty(deleted)
    let! wsOwnership =
        scalarById<string> connStr
            """
            SELECT ownership FROM node_children
            WHERE parent_id = '00000000-0000-0000-0000-000000000002'
              AND child_id = @id
            """
            aId.Value
    let! uOwnership =
        scalarById<string> connStr
            """
            SELECT ownership FROM node_children
            WHERE parent_id = '20000000-0000-0000-0000-000000000101'
              AND child_id = @id
            """
            aId.Value
    let! revision = scalar<int> connStr "SELECT revision FROM graph WHERE singleton = 1"
    let! changeCount = scalar<int64> connStr "SELECT count(*) FROM changes"
    Assert.Equal("owner", wsOwnership)
    Assert.Equal("ref", uOwnership)
    Assert.Equal(8, revision)
    Assert.Equal(1L, changeCount)
}

[<Fact>]
let ``ownership repair inserts canonicals without node_children_pkey clash`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let u1Id, u2Id = id 120, id 121
    let u1 = Node.Create(u1Id, text = "U1")
    let u2 = Node.Create(u2Id, text = "U2")
    let graph0 = graphWithCustomNodes [ u1; u2 ]
    let root = graph0.nodes.[Graph.rootId]
    let graph =
        graph0.nodes
        |> Map.add Graph.rootId
            { root with
                children =
                    root.children
                    @ [ ChildNode.owner u1Id; ChildNode.owner u2Id ] }
        |> Graph.fromNodes Graph.rootId
    do! replaceProjection connStr graph 9

    do!
        exec connStr
            """
            DELETE FROM nodes
            WHERE id IN (
                '00000000-0000-0000-0000-000000000002',
                '00000000-0000-0000-0000-000000000003'
            )
            """
            []
    do!
        exec connStr
            """
            DELETE FROM node_children
            WHERE parent_id = '00000000-0000-0000-0000-000000000000'
            """
            []
    do!
        exec connStr
            """
            INSERT INTO node_children
                (parent_id, ordinal, child_id, ownership)
            VALUES
                ('00000000-0000-0000-0000-000000000000', 0, @u1, 'owner'),
                ('00000000-0000-0000-0000-000000000000', 1, @u2, 'owner'),
                ('00000000-0000-0000-0000-000000000000', 2,
                 '00000000-0000-0000-0000-000000000001', 'owner')
            """
            [ "u1", box u1Id.Value; "u2", box u2Id.Value ]

    let! result =
        DatabaseProjection.startupSweepPatch
        |> DatabaseProjection.maintenanceCommand
        |> DatabaseProjection.executeMaintenance connStr
    match result with
    | Error e -> Assert.Fail($"executeMaintenance: {e}")
    | Ok _ -> ()

    let! kids = readRootChildren connStr
    let ids = kids |> List.map (fun (childId, _, _) -> childId)
    let ords = kids |> List.map (fun (_, ordinal, _) -> ordinal)
    Assert.Equal<int list>([ 0; 1; 2; 3; 4 ], ords)
    Assert.Equal(u1Id.Value, ids.[0])
    Assert.Equal(u2Id.Value, ids.[1])
    Assert.Equal(Graph.workspacesId.Value, ids.[2])
    Assert.Equal(Graph.systemId.Value, ids.[3])
    Assert.Equal(Graph.trashId.Value, ids.[4])
    Assert.All(kids, fun (_, _, ownership) -> Assert.Equal("owner", ownership))
}

[<Fact>]
let ``ownership repair shifts root with owner-and-ref sibling without pkey clash`` () =
    task {
        let connStr = requireDbConnStr ()
        do! resetTestDatabase connStr
        let u1Id = id 122
        let u1 = Node.Create(u1Id, text = "U1")
        let graph0 = graphWithCustomNodes [ u1 ]
        let root = graph0.nodes.[Graph.rootId]
        let graph =
            graph0.nodes
            |> Map.add Graph.rootId
                { root with
                    children = root.children @ [ ChildNode.owner u1Id ] }
            |> Graph.fromNodes Graph.rootId
        do! replaceProjection connStr graph 9
        do!
            exec connStr
                """
                DELETE FROM nodes
                WHERE id IN (
                    '00000000-0000-0000-0000-000000000002',
                    '00000000-0000-0000-0000-000000000003'
                )
                """
                []
        do!
            exec connStr
                """
                DELETE FROM node_children
                WHERE parent_id = '00000000-0000-0000-0000-000000000000'
                """
                []
        do!
            exec connStr
                """
                INSERT INTO node_children
                    (parent_id, ordinal, child_id, ownership)
                VALUES
                    ('00000000-0000-0000-0000-000000000000', 0, @u1, 'owner'),
                    ('00000000-0000-0000-0000-000000000000', 1, @u1, 'ref'),
                    ('00000000-0000-0000-0000-000000000000', 2,
                     '00000000-0000-0000-0000-000000000001', 'owner')
                """
                [ "u1", box u1Id.Value ]

        let! result =
            DatabaseProjection.startupSweepPatch
            |> DatabaseProjection.maintenanceCommand
            |> DatabaseProjection.executeMaintenance connStr
        match result with
        | Error e -> Assert.Fail($"executeMaintenance: {e}")
        | Ok _ -> ()

        let! kids = readRootChildren connStr
        let ords = kids |> List.map (fun (_, ordinal, _) -> ordinal)
        Assert.Equal<int list>([ 0; 1; 2; 3; 4 ], ords)
        Assert.Equal(u1Id.Value, kids.[0] |> fun (id, _, _) -> id)
        Assert.Equal("owner", kids.[0] |> fun (_, _, o) -> o)
        Assert.Equal(u1Id.Value, kids.[1] |> fun (id, _, _) -> id)
        Assert.Equal("ref", kids.[1] |> fun (_, _, o) -> o)
        Assert.Equal(Graph.workspacesId.Value, kids.[2] |> fun (id, _, _) -> id)
        Assert.Equal(Graph.systemId.Value, kids.[3] |> fun (id, _, _) -> id)
        Assert.Equal(Graph.trashId.Value, kids.[4] |> fun (id, _, _) -> id)
    }

