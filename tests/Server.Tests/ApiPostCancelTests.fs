module Gambol.Server.Tests.ApiPostCancelTests

open System.Net
open System.Net.Http
open System.Text
open Microsoft.AspNetCore.Http.HttpResults
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend
open Thoth.Json.Newtonsoft

module Encode = Thoth.Json.Newtonsoft.Encode

let private encodeCancel (focusId: NodeId) =
    Encode.toString 0 (EventJson.encodeCancelRequest focusId)

let private jsonContent body =
    new StringContent(body, Encoding.UTF8, "application/json")

[<Fact>]
let ``postCancel decodes Focus NodeId and calls cancelByFocus`` () = task {
    let focusId = NodeId.New()
    let seen = ResizeArray<NodeId>()
    let cancelByFocus received =
        seen.Add received
        async.Return(Result.Ok())
    let! result =
        Api.postCancel cancelByFocus (encodeCancel focusId)
        |> Async.StartAsTask
    Assert.Equal<NodeId list>([ focusId ], List.ofSeq seen)
    match box result with
    | :? ContentHttpResult as content ->
        Assert.Contains("\"ok\":true", content.ResponseContent)
    | other ->
        failwith $"expected JSON content, got {other.GetType().Name}"
}

[<Fact>]
let ``postCancel invalid JSON does not call cancelByFocus`` () = task {
    let seen = ResizeArray<NodeId>()
    let cancelByFocus received =
        seen.Add received
        async.Return(Result.Ok())
    let! result =
        Api.postCancel cancelByFocus "{}"
        |> Async.StartAsTask
    Assert.Empty(seen)
    match box result with
    | :? BadRequest<obj> -> ()
    | other ->
        failwith $"expected BadRequest, got {other.GetType().Name}"
}

[<Fact>]
let ``postCancel cancelByFocus Error is not ok`` () = task {
    let focusId = NodeId.New()
    let cancelByFocus _ = async.Return(Result.Error "not live")
    let! result =
        Api.postCancel cancelByFocus (encodeCancel focusId)
        |> Async.StartAsTask
    match box result with
    | :? BadRequest<obj> -> ()
    | other ->
        failwith $"expected BadRequest, got {other.GetType().Name}"
}

[<Fact>]
let ``POST cancel without cookie is unauthorized`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDirWithoutCookie dataDir
    use body = jsonContent (encodeCancel (NodeId.New()))
    let! response = client.PostAsync("/ambit/cancel", body)
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
}

[<Fact>]
let ``POST cancel with cookie reaches cancelByFocus`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDir dataDir
    use body = jsonContent (encodeCancel (NodeId.New()))
    let! response = client.PostAsync("/ambit/cancel", body)
    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode)
    Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode)
    Assert.True(response.IsSuccessStatusCode)
}
