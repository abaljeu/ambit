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
            StartActor(
                testCaller,
                Unchecked.defaultof<_>,
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

let private logEvents host = task {
    let! log =
        CoreMailboxApi.eventsSince host EventId.zero
        |> Async.StartAsTask
    return log.events
}

let private postChange host ops = task {
    let event = GraphOnlyChangePost.mint "seed" ops
    let! posted =
        CoreMailboxApi.postGraphOnly host testCaller event
        |> Async.StartAsTask
    return requireOk "seed" posted
}

let private fileHost () =
    let dataDir = newTempDir ()
    let host =
        CoreMailbox.host
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create dataDir))
            admittedCredentials
    dataDir, host

let private seedFile host = task {
    let! state0 =
        CoreMailboxApi.getState host |> Async.StartAsTask
    let state0 = requireOk "state0" state0
    let workspaceId, wsOps =
        FileNodeOps.planCreateWorkspace state0.graph "home"
    let! _ = postChange host wsOps
    let! state1 =
        CoreMailboxApi.getState host |> Async.StartAsTask
    let state1 = requireOk "state1" state1
    let fileId, fileOps =
        FileNodeOps.planCreateOwnedFile
            state1.graph
            workspaceId
            "note.txt"
    let! _ = postChange host fileOps
    return fileId
}

let private waitUntil pred timeoutMs = task {
    let start = DateTime.UtcNow
    while
        not (pred ())
        && (DateTime.UtcNow - start).TotalMilliseconds
           < float timeoutMs do
        do! Task.Delay 10
    return pred ()
}

[<Fact>]
let ``loadNode logs Order.Load`` () =
    task {
        let _, host = fileHost ()
        try
            let! fileId = seedFile host
            let! loaded =
                CoreMailboxApi.mbLoadNode host testCaller fileId
                |> Async.StartAsTask
            let eventId = requireOk "load" loaded
            let! events = logEvents host
            let stored =
                events
                |> List.find (fun event -> event.id = eventId)
            Assert.Equal(EventBody.Load fileId, stored.body)
            match MailboxLog.ofEv stored with
            | Ok logged ->
                match logged.body with
                | MailboxLog.EventBody.Order placed ->
                    Assert.Equal(Order.Load fileId, placed.order)
                | MailboxLog.EventBody.Notice _ ->
                    Assert.Fail("expected Order")
            | Error err -> Assert.Fail(err)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``loadNode pushes the subject`` () =
    task {
        let dataDir = newTempDir ()
        let pushed = ResizeArray<NodeId>()
        let stackPush, consumer = ParseStack.create ()
        let push nodeId =
            pushed.Add nodeId
            stackPush nodeId
        let parsing =
            CoreMailbox.hostWithParsePush
                (CoreActorPool.create ())
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
                push
        let parseHandle = CoreDoor.create true parsing testCaller
        try
            let! fileId = seedFile parsing
            let diskPath = Path.Combine(dataDir, "home", "note.txt")
            Directory.CreateDirectory(Path.GetDirectoryName diskPath)
            |> ignore
            File.WriteAllText(diskPath, "HELLO\n")
            let! loaded =
                CoreMailboxApi.mbLoadNode parsing testCaller fileId
                |> Async.StartAsTask
            requireOk "load" loaded |> ignore
            Assert.Equal<NodeId>(fileId, pushed.[0])
            Assert.Equal(1, pushed.Count)
            ParseThread.start
                { dataDir = dataDir
                  consumer = consumer
                  push = stackPush
                  getGraph = ParseThread.graphFromHost parsing
                  postOps = ParseThread.postParseOps parseHandle
                  markUnparsed = ignore
                  finishParse =
                    fun nodeId ->
                        CoreMailboxApi.postNotice
                            parsing
                            (Notice.NodeParsed nodeId) }
            let! parsed =
                waitUntil
                    (fun () ->
                        CoreMailboxApi.eventsSince parsing EventId.zero
                        |> Async.RunSynchronously
                        |> fun log ->
                            log.events
                            |> List.exists (fun event ->
                                event.body =
                                    EventBody.Notice
                                        (Notice.NodeParsed fileId)))
                    2000
            Assert.True(parsed, "parse reaches Notice.NodeParsed")
        finally
            CoreMailboxApi.dispose parsing
    }

[<Fact>]
let ``loadNode of a plain Node replies Error`` () =
    task {
        let _, host = fileHost ()
        try
            let childId = NodeId.New()
            let! _ =
                postChange host [
                    Op.NewNode(childId, "plain")
                    Op.Replace(
                        Graph.rootId,
                        [],
                        [ ChildNode.owner childId ]) ]
            let! before = logEvents host
            let! loaded =
                CoreMailboxApi.mbLoadNode host testCaller childId
                |> Async.StartAsTask
            Assert.Equal(
                Error
                    "Load subject is not a Workspace, Directory, or File node",
                loaded)
            let! after = logEvents host
            let added =
                after
                |> List.filter (fun event ->
                    before
                    |> List.exists (fun prior -> prior.id = event.id)
                    |> not)
            Assert.Empty(added)
        finally
            CoreMailboxApi.dispose host
    }
