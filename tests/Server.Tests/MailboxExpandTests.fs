module Gambol.Server.Tests.MailboxExpandTests

open System
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private mailboxKind (message: Mailbox) =
    match message with
    | Mailbox.CoreMsg _ -> "CoreMsg"
    | Mailbox.PlaceOrder _ -> "PlaceOrder"
    | Mailbox.Ask _ -> "Ask"

[<Fact>]
let ``Mailbox is CoreMsg, PlaceOrder, and Ask`` () =
    let core =
        Mailbox.CoreMsg (
            LoadNode(testCaller, Graph.rootId, Unchecked.defaultof<_>))
    let place =
        Mailbox.PlaceOrder (
            testCaller,
            Guid.Empty,
            None,
            Order.Load Graph.rootId,
            Unchecked.defaultof<_>)
    let ask =
        Mailbox.Ask (Query.LastEventId (Unchecked.defaultof<_>))
    Assert.Equal("CoreMsg", mailboxKind core)
    Assert.Equal("PlaceOrder", mailboxKind place)
    Assert.Equal("Ask", mailboxKind ask)
    match CoreQueue.Public core with
    | CoreQueue.Public (Mailbox.CoreMsg _) -> ()
    | CoreQueue.Public _ -> Assert.Fail("Public carries Mailbox")
    | CoreQueue.Notice _ -> Assert.Fail("Public is not Notice")
    | CoreQueue.DropAuthority _ ->
        Assert.Fail("Public is not DropAuthority")
    | CoreQueue.Shutdown _ ->
        Assert.Fail("Public is not Shutdown")
    let serverPeer =
        typeof<ActorName>.Assembly.GetType("Gambol.Server.PeerActorName")
    Assert.True(isNull serverPeer)

[<Fact>]
let ``postEvent runs through Mailbox CoreMsg`` () =
    task {
        let dataDir = newTempDir ()
        let host =
            CoreMailbox.host
                (CoreActorPool.create ())
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
        try
            let childId = NodeId.New()
            let event = {
                id = EventId.zero
                submissionId = Guid.NewGuid()
                authority = testAuthority
                commandName = "Set text"
                body =
                    EventBody.Change [
                        Op.NewNode(childId, "posted")
                        Op.Replace(
                            Graph.rootId,
                            [],
                            [ ChildNode.owner childId ]) ] }
            let! posted =
                CoreMailboxApi.postEvent host testCaller event
                |> Async.StartAsTask
            let stored = requireOk "postEvent" posted
            Assert.Equal(event.submissionId, stored.submissionId)
            Assert.Equal(event.commandName, stored.commandName)
            let! tail =
                CoreMailboxApi.eventsSince host EventId.zero
                |> Async.StartAsTask
            Assert.Contains(stored, tail.events)
        finally
            CoreMailboxApi.dispose host
    }
