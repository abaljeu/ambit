module Gambol.Server.Tests.CoreActorPoolTests

open System
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

[<Fact>]
let ``Actor handle wrap does not repeat live-secret admission`` () = task {
    let dataDir = newTempDir ()
    let agent, handle, _ = createAdmittedFileWithCredentials dataDir
    try
        let bound =
            CoreDoor.forCaller
                { authority = Authority "Actor"
                  name = ""
                  secret = Credential "inactive" }
                handle
        let childId = NodeId.New()
        let event =
            changeEvent
                ""
                EventId.zero
                (Guid.NewGuid())
                [ Op.NewNode(childId, "direct Actor post")
                  Op.Replace(
                      Graph.rootId,
                      [],
                      [ ChildNode.owner childId ]) ]
        let! result =
            CoreDoor.postEvents bound [ event ]
            |> Async.StartAsTask
        match result with
        | Error err -> Assert.Fail(err)
        | Ok accepted ->
            Assert.Equal(Authority "Actor", accepted.events.Head.authority)
    finally
        CoreMailboxApi.dispose agent
}
