module Gambol.Server.Tests.MailboxExpandTests

open System
open System.IO
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
            CancelActor(
                testCaller,
                Graph.rootId,
                Unchecked.defaultof<_>))
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

let private waitUntil pred timeoutMs =
    task {
        let start = DateTime.UtcNow
        while
            not (pred ())
            && (DateTime.UtcNow - start).TotalMilliseconds
               < float timeoutMs do
            do! Task.Delay 10
        return pred ()
    }

let private postSeed host ops =
    let event = GraphOnlyChangePost.mint "seed" ops
    CoreMailboxApi.postGraphOnly host testCaller event
    |> Async.RunSynchronously
    |> requireOk "seed"
    |> ignore

let private seedFile host =
    task {
        let! state0 =
            CoreMailboxApi.getState host |> Async.StartAsTask
        let state0 = requireOk "state" state0
        let workspaceId, wsOps =
            FileNodeOps.planCreateWorkspace state0.graph "home"
        postSeed host wsOps
        let! state1 =
            CoreMailboxApi.getState host |> Async.StartAsTask
        let state1 = requireOk "state" state1
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                state1.graph
                workspaceId
                "note.txt"
        postSeed host fileOps
        return fileId
    }

let private fileHost dataDir =
    CoreMailbox.host
        (CoreActorPool.create ())
        (FileAgent.persist (FileAgent.create dataDir))
        admittedCredentials

[<Fact>]
let ``loadNode logs Order.Load`` () =
    task {
        let dataDir = newTempDir ()
        let host = fileHost dataDir
        try
            let! fileId = seedFile host
            let! loaded =
                CoreMailboxApi.mbLoadNode host testCaller fileId
                |> Async.StartAsTask
            let eventId = requireOk "load" loaded
            let! tail =
                CoreMailboxApi.eventsSince host EventId.zero
                |> Async.StartAsTask
            let stored =
                tail.events
                |> List.find (fun event -> event.id = eventId)
            Assert.Equal(EventBody.Load fileId, stored.body)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``loadNode pushes the subject`` () =
    task {
        let dataDir = newTempDir ()
        let stackPush, consumer = ParseStack.create ()
        let pushed = ResizeArray<NodeId>()
        let push nodeId =
            pushed.Add nodeId
            stackPush nodeId
        let host =
            CoreMailbox.hostWithParsePush
                (CoreActorPool.create ())
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
                push
        try
            let! fileId = seedFile host
            let diskPath = Path.Combine(dataDir, "home", "note.txt")
            Directory.CreateDirectory(Path.GetDirectoryName diskPath)
            |> ignore
            File.WriteAllText(diskPath, "HELLO\n")
            let! loaded =
                CoreMailboxApi.mbLoadNode host testCaller fileId
                |> Async.StartAsTask
            requireOk "load" loaded |> ignore
            Assert.Equal<NodeId list>([ fileId ], pushed |> Seq.toList)
            Assert.Equal(Some fileId, consumer ())
            stackPush fileId
            let parseHandle = CoreDoor.create true host testCaller
            ParseThread.start {
                dataDir = dataDir
                consumer = consumer
                push = push
                getGraph = ParseThread.graphFromHost host
                postOps = ParseThread.postParseOps parseHandle
                markUnparsed = ignore
                finishParse =
                    fun nodeId ->
                        CoreMailboxApi.postNotice
                            host
                            (Notice.NodeParsed nodeId) }
            let parsed () =
                let log =
                    CoreMailboxApi.eventsSince host EventId.zero
                    |> Async.RunSynchronously
                log.events
                |> List.exists (fun event ->
                    event.body =
                        EventBody.Notice(Notice.NodeParsed fileId))
            let! reached = waitUntil parsed 2000
            Assert.True(reached, "parsing reaches NodeParsed")
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``loadNode of a plain Node replies Error`` () =
    task {
        let dataDir = newTempDir ()
        let host = fileHost dataDir
        try
            let plainId = NodeId.New()
            let event = {
                id = EventId.zero
                submissionId = Guid.NewGuid()
                authority = testAuthority
                commandName = "New"
                body =
                    EventBody.Change [
                        Op.NewNode(plainId, "plain")
                        Op.Replace(
                            Graph.rootId,
                            [],
                            [ ChildNode.owner plainId ]) ] }
            let! posted =
                CoreMailboxApi.postEvent host testCaller event
                |> Async.StartAsTask
            requireOk "post" posted |> ignore
            let! loaded =
                CoreMailboxApi.mbLoadNode host testCaller plainId
                |> Async.StartAsTask
            Assert.Equal(
                Error
                    "Load subject is not a Workspace, Directory, or File node",
                loaded)
            let! after =
                CoreMailboxApi.eventsSince host EventId.zero
                |> Async.StartAsTask
            let loads =
                after.events
                |> List.filter (fun event ->
                    match event.body with
                    | EventBody.Load _ -> true
                    | _ -> false)
            Assert.Empty(loads)
        finally
            CoreMailboxApi.dispose host
    }
