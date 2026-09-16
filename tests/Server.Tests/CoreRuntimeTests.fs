module Gambol.Server.Tests.CoreRuntimeTests

open System
open System.Reflection
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
        {
            PersistenceMode = DatabaseSetup.PersistenceMode.File
            DbStatus = DatabaseSetup.DbStatus.Absent
            DbConnectionString = ""
            DataDir = dataDir
            AuthUser = "alice"
            AuthPass = "secret"
            Actors = []
        }

let private browserCaller user pass =
    BrowserRequestCreds.callerFromSecret (
        Credential(AuthToken.deriveToken user pass))

let private browserHandle runtime user pass =
    CoreMailbox.coreChanges runtime.host (browserCaller user pass)

[<Fact>]
let ``CoreRuntime seeds Browser credential from AuthToken.deriveToken`` () =
    task {
        let runtime = fileRuntime ()
        let expected = browserCaller "alice" "secret"
        let! browserLive =
            CoreMailbox.isAdmitted runtime.host expected
            |> Async.StartAsTask
        Assert.True(browserLive)
    }

[<Fact>]
let ``bound Changes refuses an inactive sender and does not enqueue`` () =
    task {
        let runtime = fileRuntime ()
        let bound =
            CoreMailbox.coreChanges
                runtime.host
                (BrowserRequestCreds.callerFromSecret (Credential "inactive"))
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
        (browserHandle runtime "alice" "secret").postChange [ change ]
        |> Async.StartAsTask
    let accepted = requireOk "browser post" result
    Assert.Equal<Guid list>(
        [ change.changeId ],
        accepted.events |> List.map (_.submissionId))
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
                      name = ""
                      secret = Credential "inactive" }
                    handle
            let! before = handle.getRevision () |> Async.StartAsTask
            let change = addRootChild "http"
            let event = eventFromChange change
            let body =
                Encode.toString 0 (
                    Gambol.Shared.EventJson.encodeEventBatch
                        { events = [ event ] })
            let! result =
                Api.postEvents bound 10 20 body
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
        let event = eventFromChange change
        let body =
            Encode.toString 0 (
                Gambol.Shared.EventJson.encodeEventBatch { events = [ event ] })
        let! result =
            Api.postEvents handle 10 20 body
            |> Async.StartAsTask
        Assert.False(result.GetType().Name = "UnauthorizedHttpResult")
        let! rev = handle.getRevision () |> Async.StartAsTask
        Assert.Equal(Gambol.Shared.EventId 1, rev)
    finally
        CoreMailbox.dispose agent
}

[<Fact>]
let ``callers reach changes on the mailbox Core door`` () = task {
    let runtime = fileRuntime ()
    let! rev =
        CoreMailbox.getRevision runtime.host |> Async.StartAsTask
    Assert.Equal(Gambol.Shared.EventId 0, rev)
}

[<Fact>]
let ``Graph-only post refuses an inactive Caller`` () = task {
    let runtime = fileRuntime ()
    let bound =
        CoreMailbox.coreChanges
            runtime.host
            (BrowserRequestCreds.callerFromSecret (Credential "inactive"))
    let! result =
        GraphOnlyChangePost.postChunks
            bound.postGraphOnlyChange
            (Revision 0)
            [ [ Op.NewNode(NodeId.New(), "x") ] ]
        |> Async.StartAsTask
    match result with
    | Error err -> Assert.Equal(CoreAuth.refuse, err)
    | Ok () -> Assert.Fail("inactive Graph-only must be refused")
}

[<Fact>]
let ``CoreRuntime seeds a Parse process Caller distinct from Browser cookie`` () =
    task {
        let runtime = fileRuntime ()
        let cookie = browserCaller "alice" "secret"
        Assert.NotEqual(cookie.secret, runtime.parseCaller.secret)
        Assert.Equal(Authority "Parse", runtime.parseCaller.authority)
        let! parseLive =
            CoreMailbox.isAdmitted runtime.host runtime.parseCaller
            |> Async.StartAsTask
        let change = addRootChild "parse-process"
        let! posted =
            CoreMailbox.postGraphOnlyChange
                runtime.host
                runtime.parseCaller
                change
            |> Async.StartAsTask
        requireOk "parse Graph-only" posted |> ignore
        Assert.True(parseLive)
    }

[<Fact>]
let ``CoreRuntime is not a second credential factory`` () =
    let names =
        typeof<CoreRuntime>.GetMembers(
            BindingFlags.Public ||| BindingFlags.Instance)
        |> Array.map (fun m -> m.Name)
        |> Set.ofArray
    Assert.False(Set.contains "changes" names)
    Assert.False(Set.contains "bindChanges" names)
    Assert.False(Set.contains "browserChanges" names)
    Assert.False(Set.contains "browserCredential" names)
