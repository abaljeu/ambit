module Gambol.Server.Tests.StateEndpointTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Configuration
open Xunit
open Gambol.Server
open Gambol.Shared

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

/// Default test filename used in all endpoints.
let private testFile = "gambol"

let private newTempDir () =
    let dir = Path.Combine(Path.GetTempPath(), $"gambol-test-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    dir

/// Create a test client pointing at the given data directory.
let private createClientForDir (tempDir: string) =
    let factory =
        (new WebApplicationFactory<Program>())
            .WithWebHostBuilder(fun builder ->
                builder.ConfigureAppConfiguration(fun _ config ->
                    config.AddInMemoryCollection(
                        dict [
                            "DataDir", tempDir
                            "Auth:Username", ""
                            "Auth:Password", ""
                        ]
                    ) |> ignore
                ) |> ignore
            )
    factory.CreateClient()

/// Create a test client with a fresh empty data directory.
let private createClient () = newTempDir () |> createClientForDir

let private decode decoder json =
    match Decode.fromString decoder json with
    | Ok v -> v
    | Error err -> failwith $"Decode failed: {err}"

let private decodeRevision json =
    Thoth.Json.Core.Decode.object (fun get ->
        get.Required.Field "revision" Serialization.decodeRevision)
    |> decode <| json

let private decodeGraph json =
    Thoth.Json.Core.Decode.object (fun get ->
        get.Required.Field "graph" Serialization.decodeGraph)
    |> decode <| json

/// GET /ambit/state, assert 200 + JSON content type, return body string.
let private getStateJson (client: HttpClient) (_file: string) = task {
    let! resp = client.GetAsync("/ambit/state")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    Assert.Equal("application/json", resp.Content.Headers.ContentType.MediaType)
    return! resp.Content.ReadAsStringAsync()
}

let private encodeChangeBody (change: Change) =
    Encode.toString 0 (Serialization.encodeChange change)

/// POST /ambit/changes with a change and return the raw response.
let private postChange (client: HttpClient) (_file: string) (change: Change) = task {
    let body = encodeChangeBody change
    let content = new StringContent(body, Encoding.UTF8, "application/json")
    return! client.PostAsync("/ambit/changes", content)
}

/// Read a file that may be held open by a FileAgent (shared read).
let private readFileShared (path: string) =
    use fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
    use reader = new StreamReader(fs)
    reader.ReadToEnd()

// ---- GET /{file}/state tests ----

[<Fact>]
let ``GET state returns revision 0 for fresh server`` () = task {
    use client = createClient ()
    let! json = getStateJson client testFile
    Assert.Equal(Revision 0, decodeRevision json)
}

[<Fact>]
let ``GET state returns valid graph with root node`` () = task {
    use client = createClient ()
    let! json = getStateJson client testFile
    let graph = decodeGraph json
    Assert.Equal(1, graph.nodes.Count)
    Assert.True(graph.nodes.ContainsKey graph.root)
    let root = graph.nodes.[graph.root]
    Assert.Equal("", root.text)
    Assert.Empty(root.children)
}

// ---- POST /{file}/changes tests ----

[<Fact>]
let ``POST changes SetText changes root text and bumps revision`` () = task {
    use client = createClient ()
    let! json0 = getStateJson client testFile
    let rootId = (decodeGraph json0).root

    let change = { id = 0; changeId = Guid.NewGuid(); ops = [ Op.SetText(rootId, "", "hello") ] }
    let! resp = postChange client testFile change
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

    let! json = resp.Content.ReadAsStringAsync()
    Assert.Equal(Revision 1, decodeRevision json)
    let graph = decodeGraph json
    Assert.Equal("hello", graph.nodes.[rootId].text)
}

[<Fact>]
let ``POST changes NewNode+Replace adds child to root`` () = task {
    use client = createClient ()
    let! json0 = getStateJson client testFile
    let rootId = (decodeGraph json0).root
    let childId = NodeId.New()

    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, "child")
              Op.Replace(rootId, 0, [], [ childId ]) ] }

    let! resp = postChange client testFile change
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

    let! json = resp.Content.ReadAsStringAsync()
    let graph = decodeGraph json
    Assert.Equal(2, graph.nodes.Count)
    Assert.Equal<NodeId list>([ childId ], graph.nodes.[rootId].children)
    Assert.Equal("child", graph.nodes.[childId].text)
}

[<Fact>]
let ``POST changes with invalid JSON returns 400`` () = task {
    use client = createClient ()
    // First touch the file so the agent is created
    let! _ = getStateJson client testFile
    let content = new StringContent("not json", Encoding.UTF8, "application/json")
    let! resp = client.PostAsync("/ambit/changes", content)
    Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode)
}

[<Fact>]
let ``POST changes with bad op returns 400`` () = task {
    use client = createClient ()
    let! _ = getStateJson client testFile
    let bogusId = NodeId.New()
    let change = { id = 0; changeId = Guid.NewGuid(); ops = [ Op.SetText(bogusId, "wrong", "new") ] }
    let! resp = postChange client testFile change
    Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode)
}

[<Fact>]
let ``POST changes twice bumps revision to 2`` () = task {
    use client = createClient ()
    let! json0 = getStateJson client testFile
    let rootId = (decodeGraph json0).root

    let! resp1 = postChange client testFile { id = 0; changeId = Guid.NewGuid(); ops = [ Op.SetText(rootId, "", "first") ] }
    Assert.Equal(HttpStatusCode.OK, resp1.StatusCode)

    let change2 = { id = 1; changeId = Guid.NewGuid(); ops = [ Op.SetText(rootId, "first", "second") ] }
    let! resp2 = postChange client testFile change2
    Assert.Equal(HttpStatusCode.OK, resp2.StatusCode)

    let! json = resp2.Content.ReadAsStringAsync()
    Assert.Equal(Revision 2, decodeRevision json)
    Assert.Equal("second", (decodeGraph json).nodes.[rootId].text)
}

[<Fact>]
let ``POST changes persists in GET state`` () = task {
    use client = createClient ()
    let! json0 = getStateJson client testFile
    let rootId = (decodeGraph json0).root

    let! _ = postChange client testFile { id = 0; changeId = Guid.NewGuid(); ops = [ Op.SetText(rootId, "", "persisted") ] }

    let! json = getStateJson client testFile
    Assert.Equal(Revision 1, decodeRevision json)
    Assert.Equal("persisted", (decodeGraph json).nodes.[rootId].text)
}

// ---- Change log + persistence tests ----

/// Submit a NewNode+Replace that adds a child with the given text under root.
let private addChild (client: HttpClient) (file: string) (rootId: NodeId) (rev: Revision) (text: string) = task {
    let childId = NodeId.New()
    let change =
        { id = rev.Value
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, text)
              Op.Replace(rootId, 0, [], [ childId ]) ] }
    let! resp = postChange client file change
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    return childId
}

[<Fact>]
let ``POST changes creates log file`` () = task {
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir
    let! json0 = getStateJson client testFile
    let rootId = (decodeGraph json0).root

    let! _ = addChild client testFile rootId (Revision 0) "logged"

    let logPath = Path.Combine(tempDir, $"{testFile}.log")
    Assert.True(File.Exists(logPath), "Log file should exist after first change")
    let content = readFileShared logPath
    Assert.Contains("logged", content)
}

[<Fact>]
let ``Snapshot is written asynchronously after change`` () = task {
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir
    let! json0 = getStateJson client testFile
    let rootId = (decodeGraph json0).root

    let! _ = addChild client testFile rootId (Revision 0) "snapped"

    // Snapshot is async — wait briefly
    do! Task.Delay(500)

    let snapshotPath = Path.Combine(tempDir, testFile)
    Assert.True(File.Exists(snapshotPath), "Snapshot file should exist")
    let content = File.ReadAllText(snapshotPath)
    Assert.Contains("snapped", content)

    let metaPath = snapshotPath + ".meta"
    Assert.True(File.Exists(metaPath), "Meta file should exist")
    let rev = Int32.Parse(File.ReadAllText(metaPath).Trim())
    Assert.Equal(1, rev)
}

[<Fact>]
let ``Log contains valid change data after POST`` () = task {
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir
    let! json0 = getStateJson client testFile
    let rootId = (decodeGraph json0).root

    let! _ = addChild client testFile rootId (Revision 0) "logged-entry"

    // Read the log file — should contain the change JSON with "logged-entry"
    let logPath = Path.Combine(tempDir, $"{testFile}.log")
    Assert.True(File.Exists(logPath))
    let content = readFileShared logPath
    Assert.Contains("logged-entry", content)
    // Verify the 8-char numeric prefix format
    Assert.True(content.StartsWith("00000000"), "Log entry should have 8-digit padded change id prefix")
}

[<Fact>]
let ``New server uses snapshot + log replay`` () = task {
    let tempDir = newTempDir ()
    use client1 = createClientForDir tempDir
    let! json0 = getStateJson client1 testFile
    let rootId = (decodeGraph json0).root

    let! _ = addChild client1 testFile rootId (Revision 0) "first"
    do! Task.Delay(500) // let snapshot + meta write

    // Second change — will be in the log beyond the snapshot
    let! json1 = getStateJson client1 testFile
    let rootId2 = (decodeGraph json1).root
    let root = (decodeGraph json1).nodes.[rootId2]
    let firstChildId = root.children.[0]
    let! _ = postChange client1 testFile { id = 1; changeId = Guid.NewGuid(); 
        ops = [ Op.SetText(firstChildId, "first", "updated") ] }

    // New server — should load snapshot (rev 1) + replay change 1 from log
    use client2 = createClientForDir tempDir
    let! json = getStateJson client2 testFile
    let graph = decodeGraph json
    let child = graph.nodes |> Map.toSeq |> Seq.map snd |> Seq.find (fun n -> n.text = "updated")
    Assert.Equal("updated", child.text)
    Assert.Equal(Revision 2, decodeRevision json)
}
