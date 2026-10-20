module Gambol.Server.Tests.CredentialedChangePostsTests

open System
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

/// Seam: Browser admission before the core order door.

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private addRootChild text = addRootChildEvent text |> snd

let private fileRuntime () =
    CoreRuntime.create
        {
            DbStatus = DatabaseSetup.DbStatus.Absent
            DbConnectionString = ""
            DataDir = newTempDir ()
            AuthUser = "alice"
            AuthPass = "secret"
            Actors = []
        }
        (fun _ -> ())

[<Fact>]
let ``live Browser credential is admitted and Change reaches PersistHandlers``
    () =
    task {
        let dataDir = newTempDir ()
        let agent, handle, _ = createAdmittedFileWithCredentials dataDir
        try
            let event = addRootChild "live"
            let! result =
                CoreMailboxApi.postEvents agent testCaller [ event ]
                |> Async.StartAsTask
            let accepted = requireOk "live post" result
            Assert.True(EventId.isAccepted accepted.eventId)
            Assert.Equal<Guid list>(
                [ event.submissionId ],
                accepted.events |> List.map _.submissionId)
            let! rev = CoreDoor.getEventId handle |> Async.StartAsTask
            Assert.True(EventId.isAccepted rev)
        finally
            CoreMailboxApi.dispose agent
    }

[<Fact>]
let ``inactive Browser credential is refused at the entry`` () = task {
    let dataDir = newTempDir ()
    let agent, handle, _ = createAdmittedFileWithCredentials dataDir
    try
        let! before = CoreDoor.getEventId handle |> Async.StartAsTask
        let caller =
            { authority = Authority "Browser"
              name = ""
              secret = Credential "inactive" }
        let! admitted =
            CoreMailboxApi.isAdmitted agent caller
            |> Async.StartAsTask
        let! after = CoreDoor.getEventId handle |> Async.StartAsTask
        Assert.False(admitted)
        Assert.Equal(before, after)
    finally
        CoreMailboxApi.dispose agent
}

[<Fact>]
let ``blank Authority is refused at the entry`` () =
    task {
        let dataDir = newTempDir ()
        let agent, handle, _ = createAdmittedFileWithCredentials dataDir
        try
            let! before = CoreDoor.getEventId handle |> Async.StartAsTask
            let caller =
                { authority = Authority "   "
                  name = testCaller.name
                  secret = testSecret }
            let! admitted =
                CoreMailboxApi.isAdmitted agent caller
                |> Async.StartAsTask
            let! after = CoreDoor.getEventId handle |> Async.StartAsTask
            Assert.False(admitted)
            Assert.Equal(before, after)
        finally
            CoreMailboxApi.dispose agent
    }

[<Fact>]
let ``request-carried cookie secret is admitted and foreign secret is refused at entry`` () =
    task {
        let runtime = fileRuntime ()
        let caller = browserCallerFromAuth "alice" "secret"
        let handle = CoreDoor.create true runtime.host caller
        let event = addRootChild "cookie-post"
        let! admitted =
            CoreMailboxApi.isAdmitted runtime.host caller
            |> Async.StartAsTask
        Assert.True(admitted)
        let! ok =
            CoreDoor.postEvents handle [ event ]
            |> Async.StartAsTask
        let accepted = requireOk "cookie post" ok
        Assert.True(EventId.isAccepted accepted.eventId)
        let foreign =
            BrowserRequestCreds.callerFromSecret
                (Credential "not-the-cookie")
        let! foreignAdmitted =
            CoreMailboxApi.isAdmitted runtime.host foreign
            |> Async.StartAsTask
        Assert.False(foreignAdmitted)
    }

[<Fact>]
let ``missing cookie secret is refused at the entry`` () =
    task {
        let runtime = fileRuntime ()
        let cookie =
            Credential(AuthToken.deriveToken "alice" "secret")
        let handle =
            CoreDoor.create true
                runtime.host
                (browserCallerFromAuth "alice" "secret")
        match BrowserRequestCreds.trySecretFromCookieValue None with
        | Some _ -> Assert.Fail("missing cookie must not yield a secret")
        | None -> ()
        match BrowserRequestCreds.trySecretFromCookieValue (Some "") with
        | Some _ -> Assert.Fail("blank cookie must not yield a secret")
        | None -> ()
        match BrowserRequestCreds.trySecretFromCookieValue (Some "  ") with
        | Some _ -> Assert.Fail("whitespace cookie must not yield a secret")
        | None -> ()
        let! before = CoreDoor.getEventId handle |> Async.StartAsTask
        let missing =
            BrowserRequestCreds.callerFromSecret (Credential "")
        let! admitted =
            CoreMailboxApi.isAdmitted runtime.host missing
            |> Async.StartAsTask
        let! after = CoreDoor.getEventId handle |> Async.StartAsTask
        Assert.False(admitted)
        Assert.Equal(before, after)
        Assert.NotEqual(Credential "", cookie)
    }

[<Fact>]
let ``request cookie value is admitted without closed-over browserCredential`` () =
    task {
        let runtime = fileRuntime ()
        let cookie =
            Credential(AuthToken.deriveToken "alice" "secret")
        match BrowserRequestCreds.trySecretFromCookieValue (
            Some(let (Credential s) = cookie in s)
        ) with
        | None -> Assert.Fail("cookie value must become the request secret")
        | Some secret ->
            Assert.Equal(cookie, secret)
            Assert.NotEqual(Credential "", secret)
            let! ok =
                CoreDoor.postEvents
                    (CoreDoor.create true
                        runtime.host
                        (browserCallerFromAuth "alice" "secret"))
                    [ addRootChild "request-only" ]
                |> Async.StartAsTask
            let accepted = requireOk "request secret" ok
            Assert.True(EventId.isAccepted accepted.eventId)
    }
