module Gambol.Server.Tests.CoreActorPoolTests

open System
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

[<Fact>]
let ``Actor handle wrap refuses a different inactive credential`` () = task {
    let dataDir = newTempDir ()
    let agent, handle, _ = createAdmittedFileWithCredentials dataDir
    try
        let bound =
            CoreDoor.forCaller
                { authority = Authority "Actor"
                  name = ""
                  secret = Credential "inactive" }
                handle
        let! state = CoreDoor.getState handle |> Async.StartAsTask
        let state =
            match state with
            | Ok value -> value
            | Error err ->
                Assert.Fail($"state: {err}")
                Unchecked.defaultof<_>
        let event =
            changeEvent
                ""
                EventId.zero
                (Guid.NewGuid())
                [ Op.NewNode(NodeId.New(), "nope")
                  Op.Replace(
                      Graph.rootId,
                      [],
                      [ ChildNode.owner (NodeId.New()) ]) ]
        let! result =
            CoreDoor.postEvents bound [ event ]
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
    finally
        CoreMailboxApi.dispose agent
}
