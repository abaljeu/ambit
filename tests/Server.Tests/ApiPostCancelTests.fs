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
module Decode = Thoth.Json.Newtonsoft.Decode

let private encodeCancel (focusId: NodeId) =
    Encode.toString 0 (
        EventJson.encodeCancelRequest
            { focusId = focusId; eventId = EventId.zero })

let private unusedHandle
    (state: State)
    (events: Ev list)
    (latestId: EventId)
    : CoreChanges =
    { getState = fun () -> async.Return(Result.Ok state)
      getEventId = fun () -> async.Return latestId
      getEventsSince = fun _ -> async.Return events
      isReady = fun () -> true
      postEvents = fun _ -> async.Return(Result.Error "unused")
      postGraphOnly = fun _ -> async.Return(Result.Error "unused")
      actorStop = fun _ -> async.Return(Result.Error "unused")
      asCaller = fun _ -> Unchecked.defaultof<CoreChanges> }

let private emptyHandle =
    unusedHandle
        { graph = Graph.create (); eventId = EventId.zero }
        []
        EventId.zero

let private decodeUniversal json =
    Decode.fromString
        ApiResponseSerialization.decodeUniversalResponseDecoder
        json

let private cancelledStop focusId : Ev =
    { id = EventId.fromJson 2
      submissionId = System.Guid.NewGuid()
      authority = Authority "Browser"
      commandName = ""
      body = EventBody.ActorStop(focusId, ActorCancelled) }

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
        Api.postCancel cancelByFocus emptyHandle (encodeCancel focusId)
        |> Async.StartAsTask
    Assert.Equal<NodeId list>([ focusId ], List.ofSeq seen)
    match box result with
    | :? ContentHttpResult as content ->
        match decodeUniversal content.ResponseContent with
        | Error err -> failwith err
        | Ok (response: UniversalResponse) ->
            Assert.Empty(response.events)
    | other ->
        failwith $"expected JSON content, got {other.GetType().Name}"
}

[<Fact>]
let ``postCancel success encodes Cancelled ActorStop Events`` () = task {
    let focusId = NodeId.New()
    let stop = cancelledStop focusId
    let handle =
        unusedHandle
            { graph = Graph.create (); eventId = EventId.fromJson 2 }
            [ stop ]
            (EventId.fromJson 2)
    let cancelByFocus _ = async.Return(Result.Ok())
    let! result =
        Api.postCancel cancelByFocus handle (encodeCancel focusId)
        |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult as content ->
        match decodeUniversal content.ResponseContent with
        | Error err -> failwith err
        | Ok (response: UniversalResponse) ->
            Assert.Equal(EventId.fromJson 2, response.latestId)
            Assert.Equal(1, response.events.Length)
            match response.events.[0].body with
            | EventBody.ActorStop(stoppedId, ActorCancelled) ->
                Assert.Equal(focusId, stoppedId)
            | other -> failwith $"expected ActorStop Cancelled, {other}"
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
        Api.postCancel cancelByFocus emptyHandle "{}"
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
        Api.postCancel cancelByFocus emptyHandle (encodeCancel focusId)
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
