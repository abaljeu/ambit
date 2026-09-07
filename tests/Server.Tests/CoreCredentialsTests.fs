module Gambol.Server.Tests.CoreCredentialsTests

open Microsoft.AspNetCore.Http
open Xunit
open Gambol.Server
open Gambol.Shared

module Encode = Thoth.Json.Newtonsoft.Encode

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private unusedHandle
    (post: Change list -> Async<Result<CoreChangesAccepted, string>>)
    : CoreChanges =
    { getState = fun () -> async.Return(Result.Error "unused")
      getRevision = fun () -> async.Return(Revision 0)
      getChangesSince = fun _ -> async.Return []
      isReady = fun () -> true
      postChange = post
      postGraphOnlyChange = fun _ -> async.Return(Result.Error "unused") }

let private addRootChild text =
    let childId = NodeId.New()
    { id = 0
      changeId = System.Guid.NewGuid()
      ops =
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

[<Fact>]
let ``Core holds a credential until Core removes it`` () = task {
    let credentials = CoreCredentials.create ()
    let sender = Credential "job-1"
    let! missing = credentials.contains sender |> Async.StartAsTask
    Assert.False(missing)
    do! credentials.add sender |> Async.StartAsTask
    let! live = credentials.contains sender |> Async.StartAsTask
    Assert.True(live)
    do! credentials.remove sender |> Async.StartAsTask
    let! gone = credentials.contains sender |> Async.StartAsTask
    Assert.False(gone)
}

[<Fact>]
let ``inactive sender is auth-refused and is not enqueued`` () = task {
    let credentials = CoreCredentials.create ()
    let posts = ResizeArray<Change list>()
    let enqueue changes =
        posts.Add(changes)
        async.Return(Result.Error "must not enqueue")
    let change = addRootChild "refused"
    let! result =
        CoreAuth.post credentials (Credential "inactive") enqueue [ change ]
        |> Async.StartAsTask
    Assert.Equal(Error CoreAuth.refuse, result)
    Assert.Equal(
        Error(CoreAdmissionError.text CoreAdmissionError.Unauthorized),
        result)
    Assert.Empty(posts)
}

[<Fact>]
let ``live credential is enqueued`` () = task {
    let credentials = CoreCredentials.create ()
    let sender = Credential "live"
    do! credentials.add sender |> Async.StartAsTask
    let posts = ResizeArray<Change list>()
    let accepted changes : CoreChangesAccepted =
        { revision = Revision 1
          changes = changes
          externalChanges = false
          message = None
          isReady = true }
    let enqueue changes =
        posts.Add(changes)
        async.Return(Result.Ok(accepted changes))
    let change = addRootChild "admitted"
    let! result =
        CoreAuth.post credentials sender enqueue [ change ]
        |> Async.StartAsTask
    let accepted = requireOk "admitted post" result
    Assert.Equal<Change list>([ change ], Assert.Single(posts))
    Assert.Equal<Change list>([ change ], accepted.changes)
}

[<Fact>]
let ``Adapter cookie fail and inactive sender are the same refuse family`` () =
    task {
        let cookieFail = Results.Unauthorized()
        let handle =
            unusedHandle (fun _ -> async.Return(Error CoreAuth.refuse))
        let change = addRootChild "adapter"
        let body =
            Encode.toString 0 (
                Serialization.encodeChangeBatch { changes = [ change ] })
        let! coreFail =
            Api.postChange handle 10 20 body
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
    let body =
        Encode.toString 0 (
            Serialization.encodeChangeBatch { changes = [ change ] })
    let! result =
        Api.postChange handle 10 20 body
        |> Async.StartAsTask
    Assert.False(result.GetType().Name = "UnauthorizedHttpResult")
}
