module Gambol.Server.Tests.DbAgentTests

open System
open System.IO
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestDbConfigTests

module Decode = Thoth.Json.Newtonsoft.Decode
module Encode = Thoth.Json.Newtonsoft.Encode

let private decodeChange (s: string) =
    Decode.fromString Serialization.decodeChange s

let private testConnEnv = "TEST_DB_CONNECTION_STRING"

let private connStrOrEmpty () =
    TestDbConfig.resolveFrom
        (fun () -> Environment.GetEnvironmentVariable(testConnEnv) |> Option.ofObj)
        AppContext.BaseDirectory
    |> Option.defaultValue ""

let private resetTestDatabase (connStr: string) =
    task {
        do! Database.initSchema connStr |> Async.AwaitTask
        use conn = new Npgsql.NpgsqlConnection(connStr)
        do! conn.OpenAsync()
        use cmd = conn.CreateCommand()

        cmd.CommandText <-
            "TRUNCATE TABLE changes, node_children, nodes, graph RESTART IDENTITY CASCADE;"

        let! _ = cmd.ExecuteNonQueryAsync()
        return ()
    }

let private decodeGraph (json: string) : Graph =
    let decoder =
        Thoth.Json.Core.Decode.object (fun get ->
            get.Required.Field "graph" Serialization.decodeGraph)

    match Decode.fromString decoder json with
    | Ok g -> g
    | Error e -> failwith $"Decode graph: {e}"

[<SkippableFact>]
let ``DbAgent empty test DB has revision 0 and canonical ROOT`` () = task {
    let connStr = connStrOrEmpty ()
    Skip.If(String.IsNullOrWhiteSpace(connStr), $"Set {testConnEnv} for PostgreSQL tests.")
    do! resetTestDatabase connStr
    let agent = DbAgent.create connStr
    let! rev = DbAgent.getRevision agent |> Async.StartAsTask
    let! json = DbAgent.getState agent |> Async.StartAsTask
    Assert.Equal(0, rev)
    let graph = decodeGraph json
    let root = graph.nodes.[graph.root]
    Assert.Equal(2, graph.nodes.Count)
    Assert.Equal("ROOT", root.text)
    Assert.Equal(1, root.children.Length)
    Assert.Equal(Graph.trashId, root.children.[0].id)
    Assert.Equal("Trash", graph.nodes.[Graph.trashId].text)
}

[<SkippableFact>]
let ``DbAgent new process loads state from projection and changes after post`` () = task {
    let connStr = connStrOrEmpty ()
    Skip.If(String.IsNullOrWhiteSpace(connStr), $"Set {testConnEnv} for PostgreSQL tests.")
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

    let body = Encode.toString 0 (Serialization.encodeChange change)
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
    Assert.Equal(2, root.children.Length)
    let cid = root.children.[0].id
    Assert.Equal("reload-check", graph2.nodes.[cid].text)
    Assert.Equal(Graph.trashId, root.children.[1].id)
}

[<SkippableFact>]
let ``rebuildFromDocumentFiles aligns DB with on-disk document`` () = task {
    let connStr = connStrOrEmpty ()
    Skip.If(String.IsNullOrWhiteSpace(connStr), $"Set {testConnEnv} for PostgreSQL tests.")

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

        let body = Encode.toString 0 (Serialization.encodeChange change)
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

        do! Database.rebuildFromDocumentFiles connStr tempRoot "gambol" |> Async.AwaitTask

        let! dbAfter = Database.loadPersistedState connStr decodeChange |> Async.AwaitTask

        Assert.True(GraphProjection.graphEquals fileSt.graph dbAfter.graph)
        Assert.Equal(fileSt.revision.Value, dbAfter.revision.Value)
    finally
        if Directory.Exists(tempRoot) then
            Directory.Delete(tempRoot, true)
}

[<SkippableFact>]
let ``loadPersistedState preserves empty string node name`` () = task {
    let connStr = connStrOrEmpty ()
    Skip.If(String.IsNullOrWhiteSpace(connStr), $"Set {testConnEnv} for PostgreSQL tests.")
    do! resetTestDatabase connStr

    let baseGraph = Graph.create ()
    let trashNode = baseGraph.nodes.[Graph.trashId]

    let graphWithEmptyName =
        baseGraph.nodes
        |> Map.add Graph.trashId { trashNode with name = Some "" }
        |> Graph.fromNodes baseGraph.root

    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do! Database.replaceGraphProjectionWithTx tx graphWithEmptyName 0 |> Async.AwaitTask
    tx.Commit()

    let! loaded = Database.loadPersistedState connStr decodeChange |> Async.AwaitTask
    Assert.Equal(Some "", loaded.graph.nodes.[Graph.trashId].name)
}
