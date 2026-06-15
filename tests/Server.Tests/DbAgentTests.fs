module Gambol.Server.Tests.DbAgentTests

open System
open System.IO
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Decode = Thoth.Json.Newtonsoft.Decode
module Encode = Thoth.Json.Newtonsoft.Encode

let private decodeChange (s: string) =
    Decode.fromString Serialization.decodeChange s

let private decodeGraph (json: string) : Graph =
    let decoder =
        Thoth.Json.Core.Decode.object (fun get ->
            get.Required.Field "graph" Serialization.decodeGraph)

    match Decode.fromString decoder json with
    | Ok g -> g
    | Error e -> failwith $"Decode graph: {e}"

let private encodeChangeBatch (changes: Change list) =
    Encode.toString 0 (Serialization.encodeChangeBatch { changes = changes })

[<Fact>]
let ``DbAgent empty test DB has revision 0 and canonical ROOT`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let agent = DbAgent.create connStr
    let! rev = DbAgent.getRevision agent |> Async.StartAsTask
    let! json = DbAgent.getState agent |> Async.StartAsTask
    Assert.Equal(0, rev)
    let graph = decodeGraph json
    let root = graph.nodes.[graph.root]
    Assert.Equal(3, graph.nodes.Count)
    Assert.Equal("ROOT", root.text)
    Assert.Equal(2, root.children.Length)
    Assert.Equal(Graph.workspacesId, root.children.[0].id)
    Assert.Equal("Workspaces", graph.nodes.[Graph.workspacesId].text)
    Assert.Equal(Graph.trashId, root.children.[1].id)
    Assert.Equal("Trash", graph.nodes.[Graph.trashId].text)
}

[<Fact>]
let ``DbAgent new process loads state from projection and changes after post`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let agent1 = DbAgent.create connStr
    let! json0 = DbAgent.getState agent1 |> Async.StartAsTask
    let rootId = (decodeGraph json0).root
    let childId = NodeId.New()

    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, "reload-check")
              Op.Replace(rootId, 0, [], [ { ref = Ownership.Owner; id = childId } ]) ] }

    let body = encodeChangeBatch [ change ]
    let! postResult = DbAgent.postChange agent1 body |> Async.StartAsTask

    match postResult with
    | Error e -> Assert.Fail($"postChange: {e}")
    | Ok _ -> ()

    let agent2 = DbAgent.create connStr
    let! rev2 = DbAgent.getRevision agent2 |> Async.StartAsTask
    let! json2 = DbAgent.getState agent2 |> Async.StartAsTask
    Assert.Equal(1, rev2)
    let graph2 = decodeGraph json2
    Assert.Equal(Graph.rootId, graph2.root)
    let root = graph2.nodes.[graph2.root]
    Assert.Equal(3, root.children.Length)
    let cid = root.children.[0].id
    Assert.Equal("reload-check", graph2.nodes.[cid].text)
    Assert.Equal(Graph.workspacesId, root.children.[1].id)
    Assert.Equal(Graph.trashId, root.children.[2].id)
}

[<Fact>]
let ``DbAgent reload preserves node updateTime from projection`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let agent1 = DbAgent.create connStr
    let! json0 = DbAgent.getState agent1 |> Async.StartAsTask
    let rootId = (decodeGraph json0).root
    let childId = NodeId.New()

    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, "stamped")
              Op.Replace(rootId, 0, [], [ { ref = Ownership.Owner; id = childId } ]) ] }

    let! postResult =
        DbAgent.postChange agent1 (encodeChangeBatch [ change ]) |> Async.StartAsTask

    match postResult with
    | Error e -> Assert.Fail($"postChange: {e}")
    | Ok _ -> ()

    let! json1 = DbAgent.getState agent1 |> Async.StartAsTask
    let stored = (decodeGraph json1).nodes.[childId].updateTime
    Assert.True(stored > NodeUpdateTime.missing)

    let agent2 = DbAgent.create connStr
    let! json2 = DbAgent.getState agent2 |> Async.StartAsTask
    let reloaded = (decodeGraph json2).nodes.[childId].updateTime
    Assert.Equal(NodeUpdateTime.toDbPrecision stored, reloaded)
}

[<Fact>]
let ``DbAgent change fails and state is unchanged when DB goes away after startup`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let agent = DbAgent.create connStr
    let! json0 = DbAgent.getState agent |> Async.StartAsTask
    let rootId = (decodeGraph json0).root
    let childId = NodeId.New()

    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, "db-down")
              Op.Replace(rootId, 0, [], [ { ref = Ownership.Owner; id = childId } ]) ] }

    try
        do! setDatabaseAllowConnections connStr false
        let body = encodeChangeBatch [ change ]
        let! postResult = DbAgent.postChange agent body |> Async.StartAsTask

        match postResult with
        | Ok _ -> Assert.Fail("Expected postChange to fail while DB rejects connections.")
        | Error err -> Assert.Contains("Database error:", err)

        let! rev = DbAgent.getRevision agent |> Async.StartAsTask
        let! jsonAfter = DbAgent.getState agent |> Async.StartAsTask
        Assert.Equal(0, rev)
        Assert.False((decodeGraph jsonAfter).nodes.ContainsKey childId)
    finally
        setDatabaseAllowConnections connStr true
        |> fun t -> t.GetAwaiter().GetResult()
}

[<Fact>]
let ``rebuildFromDocumentFiles aligns DB with on-disk document`` () = task {
    let connStr = requireDbConnStr ()

    let tempRoot =
        Path.Combine(Path.GetTempPath(), "gambol-rebuild-" + Guid.NewGuid().ToString("N"))

    try
        Directory.CreateDirectory(tempRoot) |> ignore
        File.WriteAllText(Path.Combine(tempRoot, "gambol"), Snapshot.write (Graph.create ()))
        File.WriteAllText(Path.Combine(tempRoot, "gambol.meta"), "0")
        File.WriteAllText(Path.Combine(tempRoot, "gambol.log"), "")

        do! resetTestDatabase connStr
        let agent = DbAgent.create connStr
        let childId = NodeId.New()

        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(childId, "db-only")
                  Op.Replace(Graph.rootId, 0, [], [ { ref = Ownership.Owner; id = childId } ]) ] }

        let body = encodeChangeBatch [ change ]
        let! postR = DbAgent.postChange agent body |> Async.StartAsTask

        match postR with
        | Error e -> Assert.Fail($"postChange: {e}")
        | Ok _ -> ()

        let fileSt = DocumentLoader.loadState tempRoot "gambol"
        let! dbBefore = Database.loadPersistedState connStr decodeChange |> Async.AwaitTask

        let differs =
            not (GraphProjection.graphEquals fileSt.graph dbBefore.graph)
            || fileSt.revision.Value <> dbBefore.revision.Value

        Assert.True(differs, "expected file and DB to differ before rebuild")

        do! Database.rebuildFromDocumentFiles connStr fileSt |> Async.AwaitTask

        let! dbAfter = Database.loadPersistedState connStr decodeChange |> Async.AwaitTask

        Assert.True(GraphProjection.graphEquals fileSt.graph dbAfter.graph)
        Assert.Equal(fileSt.revision.Value, dbAfter.revision.Value)
    finally
        if Directory.Exists(tempRoot) then
            Directory.Delete(tempRoot, true)
}

[<Fact>]
let ``loadPersistedState preserves node name`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr

    let baseGraph = Graph.create ()
    let trashNode = baseGraph.nodes.[Graph.trashId]
    let expectedName = Filename.create "trash-node"

    let graphWithName =
        baseGraph.nodes
        |> Map.add Graph.trashId { trashNode with name = expectedName }
        |> Graph.fromNodes baseGraph.root

    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do! Database.replaceGraphProjectionWithTx tx graphWithName 0 |> Async.AwaitTask
    tx.Commit()

    let! loaded = Database.loadPersistedState connStr decodeChange |> Async.AwaitTask
    Assert.Equal(expectedName, loaded.graph.nodes.[Graph.trashId].name)
}

[<Fact>]
let ``loadPersistedState preserves node kind`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr

    let fileId = NodeId.New()
    let g0 = Graph.create ()
    let idx = Graph.fileTreeInsertIndex g0 Graph.rootId

    let graphWithFile =
        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewSpecialNode(fileId, SpecialKind.File, "file1")
                  Op.Replace(
                      Graph.rootId,
                      idx,
                      [],
                      [ { ref = Ownership.Owner; id = fileId } ]) ] }

        match
            History.applyChange change
                { graph = g0
                  history = History.empty
                  revision = Revision 0 }
        with
        | ApplyResult.Changed st -> st.graph
        | _ -> failwith "expected Changed"

    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do! Database.replaceGraphProjectionWithTx tx graphWithFile 0 |> Async.AwaitTask
    tx.Commit()

    let! loaded = Database.loadPersistedState connStr decodeChange |> Async.AwaitTask

    match loaded.graph.nodes.[fileId].kind with
    | Special SpecialKind.File -> ()
    | k -> Assert.Fail(sprintf "expected Special File, got %A" k)
}
