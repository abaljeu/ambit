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
open Thoth.Json.Newtonsoft

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private decodeStateResponse json =
    Decode.fromString ApiResponseSerialization.decodeStateResponseDecoder json

let private handleWithGetState
    (getState: unit -> Async<Result<string, string>>)
    : AgentHandle =
    { getState = getState
      getRevision = fun () -> async.Return 0
      getChangesSince = fun _ -> async.Return []
      isReady = fun () -> true
      postChange = fun _ -> async.Return(Result.Error "unused")
      postGraphOnlyChange = fun _ -> async.Return(Result.Error "unused") }

let private defaultStateRequest () =
    DefaultHttpContext().Request

let private minimalStateJson () =
    let graph = Graph.create ()
    Encode.toString 0 (
        ApiResponseSerialization.encodeStateResponse
            { graph = graph
              revision = Revision 0
              isReady = true })

/// Nested named Workspace with one Directory child (canonical full graph JSON).
let private nestedWorkspaceStateJson () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let wsNode =
        Node.Create(
            wsId,
            text = "home",
            name = Filename.Ok "home",
            kind = Special Workspace,
            owner = Graph.workspacesId)
    let dirNode =
        Node.Create(
            dirId,
            text = "docs",
            name = Filename.Ok "docs",
            kind = Special Directory,
            owner = wsId)
    let workspaces = graph0.nodes.[Graph.workspacesId]
    let nodes =
        graph0.nodes
        |> Map.add wsId wsNode
        |> Map.add dirId dirNode
        |> Map.add
            Graph.workspacesId
            { workspaces with
                children =
                    workspaces.children @ [ ChildNode.owner wsId ] }
    let graph1 = Graph.fromNodes graph0.root nodes
    let graph2 =
        Graph.replace wsId 0 [] [ ChildNode.owner dirId ] graph1
        |> function
            | Ok g -> g
            | Error err -> failwith err
    let json =
        Encode.toString 0 (
            ApiResponseSerialization.encodeStateResponse
                { graph = graph2
                  revision = Revision 1
                  isReady = true })
    json, wsId, dirId

[<Fact>]
let ``getState returns 500 text body when agent fails`` () = task {
    let err =
        "Internal server error in FileAgent GetState (dataDir=C:\\data)."
    let handle =
        handleWithGetState (fun () -> async.Return(Result.Error err))
    let! result = Api.getState handle (defaultStateRequest()) |> Async.StartAsTask
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
    let json = minimalStateJson ()
    let handle =
        handleWithGetState (fun () -> async.Return(Result.Ok json))
    let! result = Api.getState handle (defaultStateRequest()) |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        Assert.Equal("application/json", content.ContentType)
        match decodeStateResponse content.ResponseContent with
        | Error err -> failwith err
        | Ok response ->
            Assert.Equal(Revision 0, response.revision)
            Assert.True(response.isReady)
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
}

[<Fact>]
let ``getState scope full skips bootstrap projection`` () = task {
    let json, _, dirId = nestedWorkspaceStateJson ()
    let handle =
        handleWithGetState (fun () -> async.Return(Result.Ok json))
    let req = DefaultHttpContext().Request
    req.QueryString <- QueryString.Create("scope", "full")
    let! result = Api.getState handle req |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        match decodeStateResponse content.ResponseContent with
        | Error err -> failwith err
        | Ok response ->
            Assert.True(response.graph.nodes.ContainsKey dirId)
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
}

[<Fact>]
let ``getState zoom outside ROOT adds owning Workspace`` () = task {
    let json, wsId, dirId = nestedWorkspaceStateJson ()
    let handle =
        handleWithGetState (fun () -> async.Return(Result.Ok json))
    let req = DefaultHttpContext().Request
    let (NodeId zoomGuid) = dirId
    req.QueryString <- QueryString.Create("zoom", zoomGuid.ToString())
    let! result = Api.getState handle req |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        match decodeStateResponse content.ResponseContent with
        | Error err -> failwith err
        | Ok response ->
            Assert.True(response.graph.nodes.ContainsKey dirId)
            Assert.Equal(Loaded, response.graph.nodes.[wsId].childrenStatus)
            Assert.Equal(Revision 1, response.revision)
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
}

[<Fact>]
let ``getState without zoom keeps nested Workspace Unloaded`` () = task {
    let json, wsId, dirId = nestedWorkspaceStateJson ()
    let handle =
        handleWithGetState (fun () -> async.Return(Result.Ok json))
    let! result = Api.getState handle (defaultStateRequest()) |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        match decodeStateResponse content.ResponseContent with
        | Error err -> failwith err
        | Ok response ->
            Assert.True(response.graph.nodes.ContainsKey wsId)
            Assert.Equal(Unloaded, response.graph.nodes.[wsId].childrenStatus)
            Assert.False(response.graph.nodes.ContainsKey dirId)
    | other ->
        Assert.Fail($"Expected ContentHttpResult, got {other.GetType().FullName}")
}

[<Fact>]
let ``getState returns 500 text body when getState throws`` () = task {
    let handle =
        handleWithGetState (fun () -> async {
            return failwith "injected GetState boom"
        })
    let! result = Api.getState handle (defaultStateRequest()) |> Async.StartAsTask
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
