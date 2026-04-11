module Gambol.Server.Tests.DbAgentTests

open System
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared

module Decode = Thoth.Json.Newtonsoft.Decode
module Encode = Thoth.Json.Newtonsoft.Encode

let private testConnEnv = "TEST_DB_CONNECTION_STRING"

let private connStrOrEmpty () =
    Environment.GetEnvironmentVariable(testConnEnv)
    |> Option.ofObj
    |> Option.defaultValue ""

let private resetTestDatabase (connStr: string) =
    task {
        do! Database.initSchema connStr |> Async.AwaitTask
        use conn = new Npgsql.NpgsqlConnection(connStr)
        do! conn.OpenAsync()
        use cmd = conn.CreateCommand()
        cmd.CommandText <- "TRUNCATE TABLE changes, snapshots RESTART IDENTITY;"
        let! _ = cmd.ExecuteNonQueryAsync()
        return ()
    }

let private snapshotRowCount (connStr: string) =
    task {
        use conn = new Npgsql.NpgsqlConnection(connStr)
        do! conn.OpenAsync()
        use cmd = conn.CreateCommand()
        cmd.CommandText <- "SELECT COUNT(*)::int FROM snapshots"
        let! o = cmd.ExecuteScalarAsync()
        return o :?> int
    }

/// Async snapshot uses `Task.Run`; poll until a row exists (bounded wait).
let private waitForAtLeastOneSnapshot (connStr: string) =
    let rec loop (attempt: int) =
        task {
            if attempt >= 50 then
                failwith "Timed out waiting for snapshots row."

            let! n = snapshotRowCount connStr

            if n >= 1 then
                return ()
            else
                do! Task.Delay 100
                return! loop (attempt + 1)
        }

    loop 0

let private decodeGraph (json: string) : Graph =
    let decoder =
        Thoth.Json.Core.Decode.object (fun get ->
            get.Required.Field "graph" Serialization.decodeGraph)

    match Decode.fromString decoder json with
    | Ok g -> g
    | Error e -> failwith $"Decode graph: {e}"

[<SkippableFact>]
let ``DbAgent empty test DB has revision 0 and blank root`` () = task {
    let connStr = connStrOrEmpty ()
    Skip.If(String.IsNullOrWhiteSpace(connStr), $"Set {testConnEnv} for PostgreSQL tests (gambol_test).")
    do! resetTestDatabase connStr
    let agent = DbAgent.create connStr
    let! rev = DbAgent.getRevision agent |> Async.StartAsTask
    let! json = DbAgent.getState agent |> Async.StartAsTask
    Assert.Equal(0, rev)
    let graph = decodeGraph json
    Assert.Equal(1, graph.nodes.Count)
    Assert.Equal("", graph.nodes.[graph.root].text)
}

[<SkippableFact>]
let ``DbAgent new process loads state from snapshot after change`` () = task {
    let connStr = connStrOrEmpty ()
    Skip.If(String.IsNullOrWhiteSpace(connStr), $"Set {testConnEnv} for PostgreSQL tests (gambol_test).")
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

    do! waitForAtLeastOneSnapshot connStr
    let agent2 = DbAgent.create connStr
    let! rev2 = DbAgent.getRevision agent2 |> Async.StartAsTask
    let! json2 = DbAgent.getState agent2 |> Async.StartAsTask
    Assert.Equal(1, rev2)
    let graph2 = decodeGraph json2
    let root = graph2.nodes.[graph2.root]
    Assert.Equal(1, root.children.Length)
    let cid = root.children.[0].id
    Assert.Equal("reload-check", graph2.nodes.[cid].text)
}
