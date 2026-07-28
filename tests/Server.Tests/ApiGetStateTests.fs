module Gambol.Server.Tests.ApiGetStateTests

open System
open System.IO
open System.Text
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.HttpResults
open Microsoft.Extensions.DependencyInjection
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private handleWithGetState
    (getState: unit -> Async<Result<string, string>>)
    : AgentHandle =
    { getState = getState
      getRevision = fun () -> async.Return 0
      getChangesSince = fun _ -> async.Return []
      isReady = fun () -> true
      postChange = fun _ -> async.Return(Result.Error "unused")
      postGraphOnlyChange = fun _ -> async.Return(Result.Error "unused") }

[<Fact>]
let ``getState returns 500 text body when agent fails`` () = task {
    let err =
        "Internal server error in FileAgent GetState (dataDir=C:\\data)."
    let handle =
        handleWithGetState (fun () -> async.Return(Result.Error err))
    let! result = Api.getState handle |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        Assert.Equal(Nullable 500, content.StatusCode)
        Assert.Equal(err, content.ResponseContent)
        Assert.Contains("text/plain", content.ContentType)
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
}

[<Fact>]
let ``getState returns JSON content when agent succeeds`` () = task {
    let json =
        """{"revision":0,"graph":{"root":"00000000-0000-0000-0000-000000000000","nodes":[]},"ready":true}"""
    let handle =
        handleWithGetState (fun () -> async.Return(Result.Ok json))
    let! result = Api.getState handle |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        Assert.Equal(json, content.ResponseContent)
        Assert.Equal("application/json", content.ContentType)
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
}

[<Fact>]
let ``getState returns 500 text body when getState throws`` () = task {
    let handle =
        handleWithGetState (fun () -> async {
            return failwith "injected GetState boom"
        })
    let! result = Api.getState handle |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        Assert.Equal(Nullable 500, content.StatusCode)
        Assert.False(String.IsNullOrWhiteSpace content.ResponseContent)
        Assert.Contains("injected GetState boom", content.ResponseContent)
        let services = ServiceCollection()
        services.AddLogging() |> ignore
        use sp = services.BuildServiceProvider()
        let ctx = DefaultHttpContext()
        ctx.RequestServices <- sp
        ctx.Response.Body <- new MemoryStream()
        do! content.ExecuteAsync(ctx)
        Assert.Equal(500, ctx.Response.StatusCode)
        ctx.Response.Body.Position <- 0L
        use reader = new StreamReader(ctx.Response.Body)
        let body = reader.ReadToEnd()
        Assert.False(String.IsNullOrWhiteSpace body)
        Assert.Contains("injected GetState boom", body)
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
}

[<Fact>]
let ``500 text error body survives CaptureStream over non-seekable inner`` () = task {
    let services = ServiceCollection()
    services.AddLogging() |> ignore
    use sp = services.BuildServiceProvider()
    let dataDir = newTempDir ()
    let logPath = HttpResponseLog.logPath dataDir
    HttpResponseLog.prepareFresh logPath
    let sink = new MemoryStream()
    // Kestrel response streams are non-seekable; Length/Position must not touch inner.
    let original =
        { new Stream() with
            member _.CanRead = false
            member _.CanSeek = false
            member _.CanWrite = true
            member _.Length = raise (NotSupportedException())
            member _.Position
                with get () = raise (NotSupportedException())
                and set _ = raise (NotSupportedException())
            member _.Flush() = sink.Flush()
            member _.Read(_, _, _) = raise (NotSupportedException())
            member _.Seek(_, _) = raise (NotSupportedException())
            member _.SetLength(_) = raise (NotSupportedException())
            member _.Write(buffer, offset, count) =
                sink.Write(buffer, offset, count)
            member _.WriteAsync(buffer, offset, count, ct) =
                sink.WriteAsync(buffer, offset, count, ct)
            member _.WriteAsync(buffer: ReadOnlyMemory<byte>, ct) =
                sink.WriteAsync(buffer, ct) }
    let ctx = DefaultHttpContext()
    ctx.RequestServices <- sp
    ctx.Request.Method <- "GET"
    ctx.Request.Path <- PathString("/ambit/state")
    ctx.Response.Body <- original
    let detail =
        "Internal server error in FileAgent GetState (dataDir=C:\\data)."
    do!
        HttpResponseLog.invokeLifecycle
            logPath
            ctx
            (RequestDelegate(fun endpointCtx ->
                task {
                    let result =
                        Results.Content(
                            detail,
                            "text/plain; charset=utf-8",
                            statusCode = 500)
                    return! result.ExecuteAsync(endpointCtx)
                } :> Task))
    Assert.Equal(500, ctx.Response.StatusCode)
    let body = Encoding.UTF8.GetString(sink.ToArray())
    Assert.Equal(detail, body)
}
