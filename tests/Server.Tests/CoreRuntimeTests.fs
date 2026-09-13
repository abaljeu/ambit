module Gambol.Server.Tests.CoreRuntimeTests

open System
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

let private addRootChild text =
    let childId = NodeId.New()
    { id = 0
      changeId = System.Guid.NewGuid()
      ops =
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

let private fileRuntime () =
    let dataDir = newTempDir ()
    CoreRuntime.create
        DatabaseSetup.PersistenceMode.File
        DatabaseSetup.DbStatus.Absent
        ""
        dataDir

let private recordingHandle (posts: ResizeArray<Change list>) : CoreChanges =
    let accepted changes : CoreChangesAccepted =
        { revision = Revision 1
          changes = changes
          externalChanges = false
          message = None
          isReady = true }
    { getState = fun () -> async.Return(Result.Error "unused")
      getRevision = fun () -> async.Return(Revision 0)
      getChangesSince = fun _ -> async.Return []
      isReady = fun () -> true
      postChange =
        fun changes ->
            posts.Add(changes)
            async.Return(Result.Ok(accepted changes))
      postGraphOnlyChange = fun _ -> async.Return(Result.Error "unused") }

[<Fact>]
let ``CoreRuntime holds process-lifetime Browser and Parse credentials`` () =
    task {
        let runtime = fileRuntime ()
        let! browserLive =
            runtime.credentials.contains runtime.browserCredential
            |> Async.StartAsTask
        let! parseLive =
            runtime.credentials.contains runtime.parseCredential
            |> Async.StartAsTask
        Assert.True(browserLive)
        Assert.True(parseLive)
    }

[<Fact>]
let ``bound Changes refuses an inactive sender and does not enqueue`` () =
    task {
        let runtime = fileRuntime ()
        let bound = runtime.bindChanges (Credential "inactive")
        let! before = bound.getRevision () |> Async.StartAsTask
        let! result =
            bound.postChange [ addRootChild "refused" ]
            |> Async.StartAsTask
        let! after = bound.getRevision () |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
        Assert.Equal(before, after)
    }

[<Fact>]
let ``bound Browser Changes admits a live Browser credential`` () = task {
    let runtime = fileRuntime ()
    let change = addRootChild "admitted"
    let! result =
        (runtime.browserChanges ()).postChange [ change ]
        |> Async.StartAsTask
    let accepted = requireOk "browser post" result
    Assert.Equal<Guid list>(
        [ change.changeId ],
        accepted.changes |> List.map (_.changeId))
}

[<Fact>]
let ``HTTP Adapter refuses inactive Core sender with 401 and does not enqueue``
    () =
    task {
        let posts = ResizeArray<Change list>()
        let handle = recordingHandle posts
        let credentials = CoreCredentials.create ()
        let bound =
            CoreAuth.bindHandle
                credentials
                (Credential "inactive")
                handle
        let body =
            Encode.toString 0 (
                Serialization.encodeChangeBatch
                    { changes = [ addRootChild "http" ] })
        let! result =
            Api.postChange bound 10 20 body
            |> Async.StartAsTask
        Assert.Equal("UnauthorizedHttpResult", result.GetType().Name)
        Assert.Empty(posts)
    }

[<Fact>]
let ``HTTP Adapter enqueues when Browser credential is live`` () = task {
    let posts = ResizeArray<Change list>()
    let handle = recordingHandle posts
    let credentials = CoreCredentials.create ()
    let sender = Credential "browser"
    do! credentials.add sender |> Async.StartAsTask
    let bound = CoreAuth.bindHandle credentials sender handle
    let change = addRootChild "http-live"
    let body =
        Encode.toString 0 (
            Serialization.encodeChangeBatch { changes = [ change ] })
    let! result =
        Api.postChange bound 10 20 body
        |> Async.StartAsTask
    Assert.False(result.GetType().Name = "UnauthorizedHttpResult")
    Assert.Equal<Change list>([ change ], Assert.Single(posts))
}

[<Fact>]
let ``callers reach changes and command on the Core object`` () = task {
    let runtime = fileRuntime ()
    let handle = runtime.changes ()
    let! rev = handle.getRevision () |> Async.StartAsTask
    Assert.Equal(Revision 0, rev)
    let missing = runtime.command.query (PublicNumber 1)
    Assert.Equal(Error CoreActorPool.unknownJob, missing)
    Assert.False(CoreAuth.isAuthRefuse CoreActorPool.unknownJob)
    Assert.False(CoreAuth.isAuthRefuse CoreActorPool.overlap)
}

[<Fact>]
let ``bound Graph-only post refuses an inactive sender`` () = task {
    let runtime = fileRuntime ()
    let bound =
        (runtime.bindChanges (Credential "inactive")).postGraphOnlyChange
    let! result =
        GraphOnlyChangePost.postChunks
            bound
            (Revision 0)
            [ [ Op.NewNode(NodeId.New(), "x") ] ]
        |> Async.StartAsTask
    Assert.Equal(Error CoreAuth.refuse, result)
}
