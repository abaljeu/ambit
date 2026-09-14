module Gambol.Server.Tests.CredentialedChangePostsTests

open System
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

/// Seam: Credentialed Change posts (arch CoreMsg before PersistHandlers).

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private addRootChild text =
    let childId = NodeId.New()
    { id = 0
      changeId = Guid.NewGuid()
      ops =
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

[<Fact>]
let ``live Browser credential is admitted and Change reaches PersistHandlers``
    () =
    task {
        let dataDir = newTempDir ()
        let agent, handle, _ = createAdmittedFileWithCredentials dataDir
        try
            let change = addRootChild "live"
            let! result =
                CoreMailbox.postChange
                    agent
                    testAuthority
                    testSecret
                    [ change ]
                |> Async.StartAsTask
            let accepted = requireOk "live post" result
            Assert.Equal(Revision 1, accepted.revision)
            Assert.Equal<Guid list>(
                [ change.changeId ],
                accepted.changes |> List.map _.changeId)
            let! rev = handle.getRevision () |> Async.StartAsTask
            Assert.Equal(Revision 1, rev)
        finally
            CoreMailbox.dispose agent
    }

[<Fact>]
let ``inactive credential is auth-refused before PersistHandlers`` () = task {
    let dataDir = newTempDir ()
    let agent, handle, _ = createAdmittedFileWithCredentials dataDir
    try
        let! before = handle.getRevision () |> Async.StartAsTask
        let! result =
            CoreMailbox.postChange
                agent
                (Authority "Browser")
                (Credential "inactive")
                [ addRootChild "nope" ]
            |> Async.StartAsTask
        let! after = handle.getRevision () |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
        Assert.Equal(before, after)
    finally
        CoreMailbox.dispose agent
}

[<Fact>]
let ``blank Authority is the same auth refuse before PersistHandlers`` () =
    task {
        let dataDir = newTempDir ()
        let agent, handle, _ = createAdmittedFileWithCredentials dataDir
        try
            let! before = handle.getRevision () |> Async.StartAsTask
            let! result =
                CoreMailbox.postChange
                    agent
                    (Authority "   ")
                    testSecret
                    [ addRootChild "blank-auth" ]
                |> Async.StartAsTask
            let! after = handle.getRevision () |> Async.StartAsTask
            Assert.Equal(Error CoreAuth.refuse, result)
            Assert.Equal(before, after)
        finally
            CoreMailbox.dispose agent
    }

[<Fact>]
let ``request-carried cookie secret is admitted; foreign secret is refused`` () =
    task {
        let runtime =
            CoreRuntime.create
                DatabaseSetup.PersistenceMode.File
                DatabaseSetup.DbStatus.Absent
                ""
                (newTempDir ())
                "alice"
                "secret"
        let cookie = runtime.browserCredential
        let change = addRootChild "cookie-post"
        let! ok =
            (runtime.browserChanges cookie).postChange [ change ]
            |> Async.StartAsTask
        let accepted = requireOk "cookie post" ok
        Assert.Equal(Revision 1, accepted.revision)
        let! refused =
            (runtime.browserChanges (Credential "not-the-cookie")).postChange
                [ addRootChild "nope" ]
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, refused)
    }

[<Fact>]
let ``missing cookie secret is the same auth refuse before PersistHandlers`` () =
    task {
        let runtime =
            CoreRuntime.create
                DatabaseSetup.PersistenceMode.File
                DatabaseSetup.DbStatus.Absent
                ""
                (newTempDir ())
                "alice"
                "secret"
        match BrowserRequestCreds.trySecretFromCookieValue None with
        | Some _ -> Assert.Fail("missing cookie must not yield a secret")
        | None -> ()
        match BrowserRequestCreds.trySecretFromCookieValue (Some "") with
        | Some _ -> Assert.Fail("blank cookie must not yield a secret")
        | None -> ()
        match BrowserRequestCreds.trySecretFromCookieValue (Some "  ") with
        | Some _ -> Assert.Fail("whitespace cookie must not yield a secret")
        | None -> ()
        let! before =
            (runtime.browserChanges runtime.browserCredential).getRevision ()
            |> Async.StartAsTask
        let! refused =
            (runtime.browserChanges (Credential "")).postChange
                [ addRootChild "missing-cookie" ]
            |> Async.StartAsTask
        let! after =
            (runtime.browserChanges runtime.browserCredential).getRevision ()
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, refused)
        Assert.Equal(before, after)
        Assert.NotEqual(Credential "", runtime.browserCredential)
    }

[<Fact>]
let ``request cookie value is admitted without closed-over browserCredential`` () =
    task {
        let runtime =
            CoreRuntime.create
                DatabaseSetup.PersistenceMode.File
                DatabaseSetup.DbStatus.Absent
                ""
                (newTempDir ())
                "alice"
                "secret"
        let cookie = runtime.browserCredential
        do!
            runtime.credentials.remove cookie
            |> Async.StartAsTask
        do!
            runtime.credentials.add cookie
            |> Async.StartAsTask
        match BrowserRequestCreds.trySecretFromCookieValue (
            Some(let (Credential s) = cookie in s)
        ) with
        | None -> Assert.Fail("cookie value must become the request secret")
        | Some secret ->
            Assert.Equal(cookie, secret)
            Assert.NotEqual(Credential "", secret)
            let! ok =
                (runtime.browserChanges secret).postChange
                    [ addRootChild "request-only" ]
                |> Async.StartAsTask
            let accepted = requireOk "request secret" ok
            Assert.Equal(Revision 1, accepted.revision)
    }
