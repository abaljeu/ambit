module Gambol.Server.Tests.CoreCredentialsTests

open Microsoft.AspNetCore.Http
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Encode = Thoth.Json.Newtonsoft.Encode

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private unusedHandle
    (post: Ev list -> Async<Result<CoreChangesAccepted, string>>)
    : CoreChanges =
    { getState = fun () -> async.Return(Result.Error "unused")
      getEventId = fun () -> async.Return(EventId.zero)
      getEventsSince = fun _ -> async.Return []
      isReady = fun () -> true
      postEvents = post
      postGraphOnly = fun _ -> async.Return(Result.Error "unused")
      actorStop = fun _ -> async.Return(Result.Error "unused")
      asCaller = fun _ -> Unchecked.defaultof<CoreChanges> }

let private addRootChild text = addRootChildEvent text |> snd

[<Fact>]
let ``inactive sender is auth-refused and is not enqueued`` () = task {
    let posts = ResizeArray<Ev list>()
    let enqueue events =
        posts.Add(events)
        async.Return(Result.Error "must not enqueue")
    let event = addRootChild "refused"
    let! result =
        CoreAuth.post false enqueue [ event ] |> Async.StartAsTask
    Assert.Equal(Error CoreAuth.refuse, result)
    Assert.Equal(
        Error(CoreAdmissionError.text CoreAdmissionError.Unauthorized),
        result)
    Assert.Empty(posts)
}

[<Fact>]
let ``live credential is enqueued`` () = task {
    let posts = ResizeArray<Ev list>()
    let accepted events : CoreChangesAccepted =
        { eventId = EventIdFixtures.storedId 1
          events = events
          externalChanges = false
          message = None
          isReady = true }
    let enqueue events =
        posts.Add(events)
        async.Return(Result.Ok(accepted events))
    let event = addRootChild "admitted"
    let! result =
        CoreAuth.post true enqueue [ event ] |> Async.StartAsTask
    let accepted = requireOk "admitted post" result
    Assert.Equal<Ev list>([ event ], Assert.Single(posts))
    Assert.Equal<System.Guid list>(
        [ event.submissionId ],
        accepted.events |> List.map _.submissionId)
}

[<Fact>]
let ``Adapter cookie fail and inactive sender are the same refuse family`` () =
    task {
        let cookieFail = Results.Unauthorized()
        let handle =
            unusedHandle (fun _ -> async.Return(Error CoreAuth.refuse))
        let event = addRootChild "adapter"
        let body =
            Encode.toString 0 (
                Gambol.Shared.EventJson.encodeEventBatch { events = [ event ] })
        let! coreFail =
            Api.postEvents handle 10 20 body
            |> Async.StartAsTask
        Assert.Equal(cookieFail.GetType(), coreFail.GetType())
        Assert.Equal("UnauthorizedHttpResult", coreFail.GetType().Name)
    }

[<Fact>]
let ``TCP or Database failure is not that auth refuse`` () = task {
    let unavailable =
        "Database persistence is unavailable; file fallback is read-only."
    let tcp = "connection refused"
    Assert.False(CoreAuth.isAuthRefuse unavailable)
    Assert.False(CoreAuth.isAuthRefuse tcp)
    Assert.True(CoreAuth.isAuthRefuse CoreAuth.refuse)
    let handle = unusedHandle (fun _ -> async.Return(Error unavailable))
    let event = addRootChild "system"
    let body =
        Encode.toString 0 (
            Gambol.Shared.EventJson.encodeEventBatch { events = [ event ] })
    let! result =
        Api.postEvents handle 10 20 body
        |> Async.StartAsTask
    Assert.False(result.GetType().Name = "UnauthorizedHttpResult")
}
