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

let private addRootChild text = addRootChildEvent text |> snd

let private fileRuntime () =
    let dataDir = newTempDir ()
    CoreRuntime.create
        {
            DbStatus = DatabaseSetup.DbStatus.Absent
            DbConnectionString = ""
            DataDir = dataDir
            AuthUser = "alice"
            AuthPass = "secret"
            Actors = []
        }
        (fun _ -> ())

let private browserHandle (runtime: CoreRuntime) user pass : CoreChangesDoor =
    CoreDoor.create true runtime.host (browserCallerFromAuth user pass)

[<Fact>]
let ``CoreRuntime seeds Browser credential from AuthToken.deriveToken`` () =
    task {
        let runtime = fileRuntime ()
        let expected = browserCallerFromAuth "alice" "secret"
        let! browserLive =
            CoreMailboxApi.isAdmitted runtime.host expected
            |> Async.StartAsTask
        Assert.True(browserLive)
    }

[<Fact>]
let ``bound Changes does not repeat entry admission`` () =
    task {
        let runtime = fileRuntime ()
        let bound =
            CoreDoor.create true
                runtime.host
                (BrowserRequestCreds.callerFromSecret (Credential "inactive"))
        let! before = CoreDoor.getEventId bound |> Async.StartAsTask
        let! admitted =
            CoreMailboxApi.isAdmitted runtime.host bound.caller
            |> Async.StartAsTask
        Assert.False(admitted)
        let event = addRootChild "refused"
        let! result =
            CoreDoor.postEvents bound [ event ] 
            |> Async.StartAsTask
        let! after = CoreDoor.getEventId bound |> Async.StartAsTask
        requireOk "direct core post" result |> ignore
        Assert.True(after > before)
    }

[<Fact>]
let ``bound Browser Changes admits a live Browser cookie credential`` () = task {
    let runtime = fileRuntime ()
    let event = addRootChild "admitted"
    let! result =
        CoreDoor.postEvents (browserHandle runtime "alice" "secret") [ event ]
        |> Async.StartAsTask
    let accepted = requireOk "browser post" result
    Assert.Equal<Guid list>(
        [ event.submissionId ],
        accepted.events |> List.map (_.submissionId))
}

[<Fact>]
let ``API order adapter does not repeat entry admission``
    () =
    task {
        let dataDir = newTempDir ()
        let agent, handle, _ = createAdmittedFileWithCredentials dataDir
        try
            let bound =
                CoreDoor.forCaller
                    { authority = Authority "Caller"
                      name = ""
                      secret = Credential "inactive" }
                    handle
            let! before = CoreDoor.getEventId handle |> Async.StartAsTask
            let event = addRootChild "http"
            let body =
                Encode.toString 0 (
                    ApiResponseSerialization.encodeChangeRequest
                        { events = [ event ]; want = [] })
            let! result =
                Api.postEvents bound 10 20 body
                |> Async.StartAsTask
            let! after = CoreDoor.getEventId handle |> Async.StartAsTask
            Assert.Equal("ContentHttpResult", result.GetType().Name)
            Assert.True(after > before)
        finally
            CoreMailboxApi.dispose agent
    }

[<Fact>]
let ``HTTP Adapter enqueues when Browser credential is live`` () = task {
    let dataDir = newTempDir ()
    let agent, handle, _ = createAdmittedFileWithCredentials dataDir
    try
        let event = addRootChild "http-live"
        let body =
            Encode.toString 0 (
                ApiResponseSerialization.encodeChangeRequest
                    { events = [ event ]; want = [] })
        let! result =
            Api.postEvents handle 10 20 body
            |> Async.StartAsTask
        Assert.False(result.GetType().Name = "UnauthorizedHttpResult")
        let! rev = CoreDoor.getEventId handle |> Async.StartAsTask
        Assert.True(EventId.isAccepted rev)
    finally
        CoreMailboxApi.dispose agent
}

[<Fact>]
let ``callers reach changes on the mailbox Core door`` () = task {
    let runtime = fileRuntime ()
    let! rev =
        CoreMailboxApi.getEventId runtime.host |> Async.StartAsTask
    Assert.Equal(EventId.zero, rev)
}

[<Fact>]
let ``Graph-only post does not repeat entry admission`` () = task {
    let runtime = fileRuntime ()
    let bound =
        CoreDoor.create true
            runtime.host
            (BrowserRequestCreds.callerFromSecret (Credential "inactive"))
    let! result =
        GraphOnlyChangePost.postChunks
            (CoreDoor.postGraphOnly bound)
            "Parse"
            [ [ Op.NewNode(NodeId.New(), "x") ] ]
        |> Async.StartAsTask
    match result with
    | Error err -> Assert.Fail(err)
    | Ok () -> ()
}

[<Fact>]
let ``CoreRuntime seeds a Parse process Caller distinct from Browser cookie`` () =
    task {
        let runtime = fileRuntime ()
        let cookie = browserCallerFromAuth "alice" "secret"
        Assert.NotEqual(cookie.secret, runtime.parseCaller.secret)
        Assert.Equal(Authority "Parse", runtime.parseCaller.authority)
        let! parseLive =
            CoreMailboxApi.isAdmitted runtime.host runtime.parseCaller
            |> Async.StartAsTask
        let event = addRootChild "parse-process"
        let! posted =
            CoreMailboxApi.postGraphOnly
                runtime.host
                runtime.parseCaller
                event
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
