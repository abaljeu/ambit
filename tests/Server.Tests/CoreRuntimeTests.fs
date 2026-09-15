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
        "alice"
        "secret"
        []

[<Fact>]
let ``CoreRuntime seeds Browser credential from AuthToken.deriveToken`` () =
    task {
        let runtime = fileRuntime ()
        let expected =
            Credential(AuthToken.deriveToken "alice" "secret")
        Assert.Equal(expected, runtime.browserCredential)
        let! browserLive =
            runtime.isAdmitted expected |> Async.StartAsTask
        Assert.True(browserLive)
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
let ``bound Browser Changes admits a live Browser cookie credential`` () = task {
    let runtime = fileRuntime ()
    let change = addRootChild "admitted"
    let! result =
        (runtime.browserChanges runtime.browserCredential).postChange [ change ]
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
        let dataDir = newTempDir ()
        let agent, handle, _ = createAdmittedFileWithCredentials dataDir
        try
            let bound =
                CoreAuth.bindHandle
                    { authority = Authority "Caller"
                      secret = Credential "inactive" }
                    handle
            let! before = handle.getRevision () |> Async.StartAsTask
            let body =
                Encode.toString 0 (
                    Serialization.encodeChangeBatch
                        { changes = [ addRootChild "http" ] })
            let! result =
                Api.postChange bound 10 20 body
                |> Async.StartAsTask
            let! after = handle.getRevision () |> Async.StartAsTask
            Assert.Equal("UnauthorizedHttpResult", result.GetType().Name)
            Assert.Equal(before, after)
        finally
            CoreMailbox.dispose agent
    }

[<Fact>]
let ``HTTP Adapter enqueues when Browser credential is live`` () = task {
    let dataDir = newTempDir ()
    let agent, handle, _ = createAdmittedFileWithCredentials dataDir
    try
        let change = addRootChild "http-live"
        let body =
            Encode.toString 0 (
                Serialization.encodeChangeBatch { changes = [ change ] })
        let! result =
            Api.postChange handle 10 20 body
            |> Async.StartAsTask
        Assert.False(result.GetType().Name = "UnauthorizedHttpResult")
        let! rev = handle.getRevision () |> Async.StartAsTask
        Assert.Equal(Revision 1, rev)
    finally
        CoreMailbox.dispose agent
}

[<Fact>]
let ``callers reach changes on the Core object`` () = task {
    let runtime = fileRuntime ()
    let handle = runtime.changes ()
    let! rev = handle.getRevision () |> Async.StartAsTask
    Assert.Equal(Revision 0, rev)
}

[<Fact>]
let ``bound Graph-only post does not require a live sender`` () = task {
    let runtime = fileRuntime ()
    let bound =
        (runtime.bindChanges (Credential "inactive")).postGraphOnlyChange
    let! result =
        GraphOnlyChangePost.postChunks
            bound
            (Revision 0)
            [ [ Op.NewNode(NodeId.New(), "x") ] ]
        |> Async.StartAsTask
    match result with
    | Ok () -> ()
    | Error err -> Assert.Fail($"graph-only post: {err}")
}
