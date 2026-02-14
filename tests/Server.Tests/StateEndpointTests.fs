module Gambol.Server.Tests.StateEndpointTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Configuration
open Xunit
open Gambol.Server
open Gambol.Shared

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

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
                        dict [ "DataDir", tempDir ]
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

/// GET /state, assert 200 + JSON content type, return body string.
let private getStateJson (client: HttpClient) = task {
    let! resp = client.GetAsync("/state")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    Assert.Equal("application/json", resp.Content.Headers.ContentType.MediaType)
    return! resp.Content.ReadAsStringAsync()
}

let private encodeSubmitBody (clientRevision: Revision) (change: Change) =
    Encode.toString 0 (
        Thoth.Json.Core.Encode.object
            [ "clientRevision", Serialization.encodeRevision clientRevision
              "change", Serialization.encodeChange change ])

/// POST /submit with a change and return the raw response.
let private postSubmit (client: HttpClient) (clientRevision: Revision) (change: Change) = task {
    let body = encodeSubmitBody clientRevision change
    let content = new StringContent(body, Encoding.UTF8, "application/json")
    return! client.PostAsync("/submit", content)
}

/// POST /save with optional filename and return the raw response.
let private postSave (client: HttpClient) (filename: string option) = task {
    let body =
        match filename with
        | None -> ""
        | Some f ->
            Encode.toString 0 (
                Thoth.Json.Core.Encode.object
                    [ "filename", Thoth.Json.Core.Encode.string f ])
    let content = new StringContent(body, Encoding.UTF8, "application/json")
    return! client.PostAsync("/save", content)
}

[<Fact>]
let ``GET /state returns revision 0 for fresh server`` () = task {
    use client = createClient ()
    let! json = getStateJson client
    Assert.Equal(Revision 0, decodeRevision json)
}

[<Fact>]
let ``GET /state returns valid graph with root node`` () = task {
    use client = createClient ()
    let! json = getStateJson client
    let graph = decodeGraph json
    Assert.Equal(1, graph.nodes.Count)
    Assert.True(graph.nodes.ContainsKey graph.root)
    let root = graph.nodes.[graph.root]
    Assert.Equal("", root.text)
    Assert.Empty(root.children)
}

// ---- POST /submit tests ----

[<Fact>]
let ``POST /submit SetText changes root text and bumps revision`` () = task {
    use client = createClient ()
    let! json0 = getStateJson client
    let rootId = (decodeGraph json0).root

    let change = { id = 0; ops = [ Op.SetText(rootId, "", "hello") ] }
    let! resp = postSubmit client (Revision 0) change
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

    let! json = resp.Content.ReadAsStringAsync()
    Assert.Equal(Revision 1, decodeRevision json)
    let graph = decodeGraph json
    Assert.Equal("hello", graph.nodes.[rootId].text)
}

[<Fact>]
let ``POST /submit NewNode+Replace adds child to root`` () = task {
    use client = createClient ()
    let! json0 = getStateJson client
    let rootId = (decodeGraph json0).root
    let childId = NodeId.New()

    let change =
        { id = 0
          ops =
            [ Op.NewNode(childId, "child")
              Op.Replace(rootId, 0, [], [ childId ]) ] }

    let! resp = postSubmit client (Revision 0) change
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

    let! json = resp.Content.ReadAsStringAsync()
    let graph = decodeGraph json
    Assert.Equal(2, graph.nodes.Count)
    Assert.Equal<NodeId list>([ childId ], graph.nodes.[rootId].children)
    Assert.Equal("child", graph.nodes.[childId].text)
}

[<Fact>]
let ``POST /submit with invalid JSON returns 400`` () = task {
    use client = createClient ()
    let content = new StringContent("not json", Encoding.UTF8, "application/json")
    let! resp = client.PostAsync("/submit", content)
    Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode)
}

[<Fact>]
let ``POST /submit with bad op returns 400`` () = task {
    use client = createClient ()
    // SetText with wrong oldText should fail
    let bogusId = NodeId.New()
    let change = { id = 0; ops = [ Op.SetText(bogusId, "wrong", "new") ] }
    let! resp = postSubmit client (Revision 0) change
    Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode)
}

[<Fact>]
let ``POST /submit twice bumps revision to 2`` () = task {
    use client = createClient ()
    let! json0 = getStateJson client
    let rootId = (decodeGraph json0).root

    let! resp1 = postSubmit client (Revision 0) { id = 0; ops = [ Op.SetText(rootId, "", "first") ] }
    Assert.Equal(HttpStatusCode.OK, resp1.StatusCode)

    let change2 = { id = 1; ops = [ Op.SetText(rootId, "first", "second") ] }
    let! resp2 = postSubmit client (Revision 1) change2
    Assert.Equal(HttpStatusCode.OK, resp2.StatusCode)

    let! json = resp2.Content.ReadAsStringAsync()
    Assert.Equal(Revision 2, decodeRevision json)
    Assert.Equal("second", (decodeGraph json).nodes.[rootId].text)
}

[<Fact>]
let ``POST /submit persists in GET /state`` () = task {
    use client = createClient ()
    let! json0 = getStateJson client
    let rootId = (decodeGraph json0).root

    let! _ = postSubmit client (Revision 0) { id = 0; ops = [ Op.SetText(rootId, "", "persisted") ] }

    let! json = getStateJson client
    Assert.Equal(Revision 1, decodeRevision json)
    Assert.Equal("persisted", (decodeGraph json).nodes.[rootId].text)
}

// ---- POST /save tests ----

/// Submit a NewNode+Replace that adds a child with the given text under root.
let private addChild (client: HttpClient) (rootId: NodeId) (rev: Revision) (text: string) = task {
    let childId = NodeId.New()
    let change =
        { id = rev.Value
          ops =
            [ Op.NewNode(childId, text)
              Op.Replace(rootId, 0, [], [ childId ]) ] }
    let! resp = postSubmit client rev change
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    return childId
}

[<Fact>]
let ``POST /save writes default snapshot file`` () = task {
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir
    let! json0 = getStateJson client
    let rootId = (decodeGraph json0).root

    let! _ = addChild client rootId (Revision 0) "saved"

    let! resp = postSave client None
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

    let snapshotPath = Path.Combine(tempDir, "gambol-snapshot.txt")
    Assert.True(File.Exists(snapshotPath))
    let content = File.ReadAllText(snapshotPath)
    Assert.Contains("saved", content)
}

[<Fact>]
let ``POST /save with filename writes to that file`` () = task {
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir
    let! json0 = getStateJson client
    let rootId = (decodeGraph json0).root

    let! _ = addChild client rootId (Revision 0) "custom"

    let! resp = postSave client (Some "other.txt")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

    let customPath = Path.Combine(tempDir, "other.txt")
    Assert.True(File.Exists(customPath))
    let content = File.ReadAllText(customPath)
    Assert.Contains("custom", content)

    // Default file should not exist
    let defaultPath = Path.Combine(tempDir, "gambol-snapshot.txt")
    Assert.False(File.Exists(defaultPath))
}

[<Fact>]
let ``POST /save with empty body writes default file`` () = task {
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir

    let! resp = postSave client None
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

    let snapshotPath = Path.Combine(tempDir, "gambol-snapshot.txt")
    Assert.True(File.Exists(snapshotPath))
}

[<Fact>]
let ``POST /save saved file can be loaded by new server`` () = task {
    let tempDir = newTempDir ()
    use client1 = createClientForDir tempDir
    let! json0 = getStateJson client1
    let rootId = (decodeGraph json0).root

    let! _ = addChild client1 rootId (Revision 0) "reloaded"
    let! _ = postSave client1 None

    // Start a new server pointing at the same data dir
    use client2 = createClientForDir tempDir
    let! json = getStateJson client2
    let graph = decodeGraph json
    // Snapshot.read creates fresh NodeIds, so find by text
    let child = graph.nodes |> Map.toSeq |> Seq.map snd |> Seq.find (fun n -> n.text = "reloaded")
    Assert.Equal("reloaded", child.text)
}
