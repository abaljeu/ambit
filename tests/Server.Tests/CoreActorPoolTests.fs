module Gambol.Server.Tests.CoreActorPoolTests

open System
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

[<Fact>]
let ``Actor handle with an inactive secret is dropped`` () = task {
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
        Assert.Equal(Error "authority dropped", result)
    finally
        CoreMailboxApi.dispose agent
}
