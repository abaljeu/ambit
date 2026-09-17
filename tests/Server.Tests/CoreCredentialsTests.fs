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
      getRevision = fun () -> async.Return(Gambol.Shared.EventId 0)
      getEventsSince = fun _ -> async.Return []
      isReady = fun () -> true
      postEvents = post
      postGraphOnly = fun _ -> async.Return(Result.Error "unused")
      actorStop = fun _ -> async.Return(Result.Error "unused")
      asCaller = fun _ -> Unchecked.defaultof<CoreChanges> }

let private addRootChild text =
    let childId = NodeId.New()
    { id = 0
      submissionId = System.Guid.NewGuid()
      ops =
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

[<Fact>]
let ``inactive sender is auth-refused and is not enqueued`` () = task {
    let posts = ResizeArray<Change list>()
    let enqueue changes =
        posts.Add(changes)
        async.Return(Result.Error "must not enqueue")
    let change = addRootChild "refused"
    let! result =
        CoreAuth.post false enqueue [ change ] |> Async.StartAsTask
    Assert.Equal(Error CoreAuth.refuse, result)
    Assert.Equal(
        Error(CoreAdmissionError.text CoreAdmissionError.Unauthorized),
        result)
    Assert.Empty(posts)
}

[<Fact>]
let ``live credential is enqueued`` () = task {
    let posts = ResizeArray<Change list>()
    let accepted events : CoreChangesAccepted =
        { revision = Revision 1
          events = events
          externalChanges = false
          message = None
          isReady = true }
    let enqueue changes =
        posts.Add(changes)
        async.Return(Result.Ok(accepted (changes |> List.map (fun c -> eventFromChange c))))
    let change = addRootChild "admitted"
    let! result =
        CoreAuth.post true enqueue [ change ] |> Async.StartAsTask
    let accepted = requireOk "admitted post" result
    Assert.Equal<Change list>([ change ], Assert.Single(posts))
    Assert.Equal<System.Guid list>(
        [ change.submissionId ],
        accepted.events |> List.map _.submissionId)
}

[<Fact>]
let ``Adapter cookie fail and inactive sender are the same refuse family`` () =
    task {
        let cookieFail = Results.Unauthorized()
        let handle =
            unusedHandle (fun _ -> async.Return(Error CoreAuth.refuse))
        let change = addRootChild "adapter"
        let event = eventFromChange change
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
    let change = addRootChild "system"
    let event = eventFromChange change
    let body =
        Encode.toString 0 (
            Gambol.Shared.EventJson.encodeEventBatch { events = [ event ] })
    let! result =
        Api.postEvents handle 10 20 body
        |> Async.StartAsTask
    Assert.False(result.GetType().Name = "UnauthorizedHttpResult")
}
