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
