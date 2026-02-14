module Gambol.Server.Tests.StateEndpointTests

open System
open System.IO
open System.Net
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Configuration
open Xunit
open Gambol.Server
open Gambol.Shared

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

/// GET /state, assert 200 + JSON content type, return body string.
let private getStateJson () = task {
    use client = createClient ()
    let! response = client.GetAsync("/state")
    Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType)
    return! response.Content.ReadAsStringAsync()
}

let private decode decoder json =
    match Decode.fromString decoder json with
    | Ok v -> v
    | Error err -> failwith $"Decode failed: {err}"

[<Fact>]
let ``GET /state returns revision 0 for fresh server`` () = task {
    let! json = getStateJson ()
    let rev =
        Thoth.Json.Core.Decode.object (fun get ->
            get.Required.Field "revision" Serialization.decodeRevision)
        |> decode <| json
    Assert.Equal(Revision 0, rev)
}

[<Fact>]
let ``GET /state returns valid graph with root node`` () = task {
    let! json = getStateJson ()
    let graph =
        Thoth.Json.Core.Decode.object (fun get ->
            get.Required.Field "graph" Serialization.decodeGraph)
        |> decode <| json
    Assert.Equal(1, graph.nodes.Count)
    Assert.True(graph.nodes.ContainsKey graph.root)
    let root = graph.nodes.[graph.root]
    Assert.Equal("", root.text)
    Assert.Empty(root.children)
}
