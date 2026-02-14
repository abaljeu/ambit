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

/// Create a test client with an empty data directory (no snapshot).
let private createClient () =
    let tempDir = Path.Combine(Path.GetTempPath(), $"gambol-test-{Guid.NewGuid()}")
    Directory.CreateDirectory(tempDir) |> ignore
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
