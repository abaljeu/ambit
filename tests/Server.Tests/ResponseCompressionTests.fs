module Gambol.Server.Tests.ResponseCompressionTests

open System
open System.IO
open System.IO.Compression
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Configuration
open Xunit
open Gambol.Server
open Gambol.Server.Tests.TestBackend

let private createRawClient (tempDir: string) : HttpClient * IDisposable =
    let priorDb = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    try
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", null)
        let factory =
            (new WebApplicationFactory<Program>())
                .WithWebHostBuilder(fun builder ->
                    builder.ConfigureAppConfiguration(fun _ config ->
                        config.AddInMemoryCollection(
                            dict [
                                "DataDir", tempDir
                                "Persistence:Mode", "file"
                                "DB_CONNECTION_STRING", ""
                                "Auth:Username", ""
                                "Auth:Password", ""
                            ]
                        ) |> ignore
                    ) |> ignore
                )
        let handler = factory.Server.CreateHandler()
        let client = new HttpClient(handler, disposeHandler = true)
        client.BaseAddress <- factory.Server.BaseAddress
        client, (factory :> IDisposable)
    finally
        if isNull priorDb then
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", null)
        else
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", priorDb)

let private gzipDecompress (bytes: byte[]) =
    use input = new MemoryStream(bytes)
    use gzip = new GZipStream(input, CompressionMode.Decompress)
    use output = new MemoryStream()
    gzip.CopyTo(output)
    output.ToArray()

let private brotliDecompress (bytes: byte[]) =
    use input = new MemoryStream(bytes)
    use brotli = new BrotliStream(input, CompressionMode.Decompress)
    use output = new MemoryStream()
    brotli.CopyTo(output)
    output.ToArray()

[<Fact>]
let ``getState returns gzip when Accept-Encoding gzip`` () = task {
    let client, factory = createRawClient (newTempDir ())
    use _ = factory
    use client = client
    use req = new HttpRequestMessage(HttpMethod.Get, "/ambit/state")
    req.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip") |> ignore
    let! resp = client.SendAsync(req)
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    Assert.Equal("gzip", resp.Content.Headers.ContentEncoding |> Seq.head)
    let! raw = resp.Content.ReadAsByteArrayAsync()
    let json = Encoding.UTF8.GetString(gzipDecompress raw)
    Assert.StartsWith("{", json)
    Assert.Contains("graph", json)
}

[<Fact>]
let ``getState returns brotli when Accept-Encoding br`` () = task {
    let client, factory = createRawClient (newTempDir ())
    use _ = factory
    use client = client
    use req = new HttpRequestMessage(HttpMethod.Get, "/ambit/state")
    req.Headers.TryAddWithoutValidation("Accept-Encoding", "br") |> ignore
    let! resp = client.SendAsync(req)
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    Assert.Equal("br", resp.Content.Headers.ContentEncoding |> Seq.head)
    let! raw = resp.Content.ReadAsByteArrayAsync()
    let json = Encoding.UTF8.GetString(brotliDecompress raw)
    Assert.StartsWith("{", json)
    Assert.Contains("graph", json)
}

[<Fact>]
let ``getState stays uncompressed without Accept-Encoding`` () = task {
    let client, factory = createRawClient (newTempDir ())
    use _ = factory
    use client = client
    use req = new HttpRequestMessage(HttpMethod.Get, "/ambit/state")
    let! resp = client.SendAsync(req)
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    Assert.True(resp.Content.Headers.ContentEncoding |> Seq.isEmpty)
    let! json = resp.Content.ReadAsStringAsync()
    Assert.StartsWith("{", json)
    Assert.Contains("graph", json)
}
