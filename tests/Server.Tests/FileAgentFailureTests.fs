module Gambol.Server.Tests.FileAgentFailureTests

open System
open System.Collections.Concurrent
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Http.HttpResults
open Xunit
open Thoth.Json.Newtonsoft
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private changedBody () =
    let childId = NodeId.New()
    [ {
        id = EventId.zero
        submissionId = Guid.NewGuid()
        authority = Authority "Browser"
        commandName = ""
        body = EventBody.Change
            [
                Op.NewNode(childId, "failure probe")
                Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ])
            ]
    } ]

let private host agent = admittedHostFile agent

let private getState agent = async {
    match! CoreMailboxApi.getState (host agent) with
    | Ok state -> return state
    | Error error ->
        Assert.Fail($"get state: {error}")
        return Unchecked.defaultof<_>
}

let private softFailPersist : string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string> =
    fun _ _ postGraph _ ->
        Ok {
            graph = postGraph
            message = Some(DocumentPersistWrite.fileCouldNotSave "SYSTEM/secret.txt")
        }

let private decodeAck (accepted: CoreChangesAccepted) =
    accepted

let private decodeAckMessage (accepted: CoreChangesAccepted) =
    accepted.message

/// Insert a normal child at ROOT index 0 (old span [] = insert).
let private softFailEditBody () =
    let childId = NodeId.New()
    [ {
        id = EventId.zero
        submissionId = Guid.NewGuid()
        authority = Authority "Browser"
        commandName = ""
        body = EventBody.Change
            [
                Op.NewNode(childId, "soft-fail-probe")
                Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ])
            ]
    } ]

let private hasFail message notice =
    match notice with
    | Notice.PersistFailed failure -> failure.message = message
    | _ -> false

let rec private waitNotice host pred spins = task {
    match! CoreMailboxApi.getState host with
    | Error err ->
        Assert.Fail(err)
        return false
    | Ok _ ->
        let found =
            MailboxHost.loggedNotices host |> List.exists pred
        if found || spins <= 0 then
            return found
        else
            do! Task.Delay 10
            return! waitNotice host pred (spins - 1)
}

[<Fact>]
let ``persistence failure leaves the edit and the mailbox survives`` () = task {
    let dataDir = newTempDir ()
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies =
        {
            defaults with
                persistGraphOps =
                    fun _ _ _ _ ->
                        raise (InvalidOperationException("injected persistence failure"))
        }
    let agent = FileAgent.createWithDependencies dependencies dataDir
    let mailbox = host agent
    try
        let! postResult =
            CoreDoor.postEvents (admittedChanges mailbox)
                ((changedBody ()))
            |> Async.StartAsTask
        match postResult with
        | Error error -> Assert.Fail(error)
        | Ok accepted -> Assert.Equal(None, accepted.message)
        let! found =
            waitNotice
                mailbox
                (hasFail "injected persistence failure")
                100
        Assert.True(found)
        let! state = getState agent |> Async.StartAsTask
        Assert.True(EventId.isAccepted state.eventId)
        Assert.True(
            state.graph.nodes
            |> Map.exists (fun _ n -> n.text = "failure probe"))
    finally
        CoreMailboxApi.dispose mailbox
}

[<Fact>]
let ``Edit reply returns while the persist thread still holds the write`` () = task {
    let dataDir = newTempDir ()
    let workspaceRoot = Path.Combine(dataDir, "home")
    let holderEntered = TaskCompletionSource<unit>()
    let releaseHolder = TaskCompletionSource<unit>()
    let persistAttempted = TaskCompletionSource<unit>()
    let writes = ConcurrentQueue<string>()
    let holder =
        Task.Run(fun () ->
            WorkspaceGit.withWorkTreeGate workspaceRoot (fun () ->
                holderEntered.SetResult()
                releaseHolder.Task.Wait()
                Ok ()))
    holderEntered.Task.Wait()

    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies =
        {
            defaults with
                persistGraphOps =
                    fun _ _ postGraph _ ->
                        persistAttempted.SetResult()
                        WorkspaceGit.withWorkTreeGate workspaceRoot (fun () ->
                            writes.Enqueue("persisted")
                            Ok { graph = postGraph; message = None })
        }
    let agent = FileAgent.createWithDependencies dependencies dataDir
    try
        let pending =
            CoreDoor.postEvents (admittedChanges (host agent))
                ((changedBody ()))
            |> Async.StartAsTask
        persistAttempted.Task.Wait()
        let! postResult = pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        match postResult with
        | Error error -> Assert.Fail(error)
        | Ok accepted -> Assert.Equal(None, accepted.message)
        Assert.Empty(writes)
        releaseHolder.SetResult()
        let! wrote =
            waitNotice
                (host agent)
                (function
                    | Notice.NodeStamped _ -> true
                    | _ -> false)
                100
        Assert.True(wrote)
        Assert.Single(writes) |> ignore

        let! state =
            getState agent
            |> Async.StartAsTask
            |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        Assert.True(EventId.isAccepted state.eventId)
    finally
        releaseHolder.TrySetResult() |> ignore
        holder.Wait()
        CoreMailboxApi.dispose (host agent)
}

[<Fact>]
let ``soft-fail commits the graph and the Notice carries the message`` () = task {
    let dataDir = newTempDir ()
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies = { defaults with persistGraphOps = softFailPersist }
    let agent = FileAgent.createWithDependencies dependencies dataDir
    let couldNot =
        DocumentPersistWrite.fileCouldNotSave "SYSTEM/secret.txt"
    try
        let! postResult =
            CoreDoor.postEvents (admittedChanges (host agent))
                ((softFailEditBody ()))
            |> Async.StartAsTask
        match postResult with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok ackJson -> Assert.Equal(None, decodeAckMessage ackJson)
        let! stamped =
            waitNotice
                (host agent)
                (function
                    | Notice.NodeStamped stamp -> stamp.message = Some couldNot
                    | _ -> false)
                100
        Assert.True(stamped)
        let! state =
            getState agent |> Async.StartAsTask
        Assert.True(
            state.graph.nodes
            |> Map.exists (fun _ n -> n.text = "soft-fail-probe"))
        Assert.True(EventId.isAccepted state.eventId)
    finally
        CoreMailboxApi.dispose (host agent)
}

[<Fact>]
let ``soft-fail log is replayed into FileAgent state after restart`` () = task {
    let dataDir = newTempDir ()
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies = { defaults with persistGraphOps = softFailPersist }
    let agent1 = FileAgent.createWithDependencies dependencies dataDir
    try
        let! postResult =
            CoreDoor.postEvents (admittedChanges (host agent1))
                ((softFailEditBody ()))
            |> Async.StartAsTask
        match postResult with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok _ -> ()
        let! stamped =
            waitNotice
                (host agent1)
                (function
                    | Notice.NodeStamped stamp -> stamp.message.IsSome
                    | _ -> false)
                100
        Assert.True(stamped)
    finally
        CoreMailboxApi.dispose (host agent1)

    // Meta stays behind after soft-fail; EventLog is authority and recover replays.
    Assert.Equal(EventId.zero, Bookkeeping.readEventId dataDir)
    let agent2 = FileAgent.createWithDependencies dependencies dataDir
    try
        let! state =
            getState agent2 |> Async.StartAsTask
        Assert.True(
            state.graph.nodes
            |> Map.exists (fun _ n -> n.text = "soft-fail-probe"))
        Assert.Equal(EventId.fromJson 1, state.eventId)
    finally
        CoreMailboxApi.dispose (host agent2)
}

let private stampBase = DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc)

let private incrementingStampPersist (count: int ref) =
    fun _ _ (postGraph: Graph) _ ->
        count.Value <- count.Value + 1
        let stampedTime = stampBase.AddMinutes(float count.Value)
        let node = postGraph.nodes.[Graph.workspacesId]
        let graph =
            { postGraph with
                nodes =
                    Map.add
                        Graph.workspacesId
                        { node with updateTime = stampedTime }
                        postGraph.nodes }
        Ok { graph = graph; message = None }

let private addChildChange _rev text =
    let childId = NodeId.New()
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = ""
      body = EventBody.Change
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

[<Fact>]
let ``ACK has no stamp ops and the Notice carries them`` () = task {
    let dataDir = newTempDir ()
    let count = ref 0
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies =
        { defaults with persistGraphOps = incrementingStampPersist count }
    let agent = FileAgent.createWithDependencies dependencies dataDir
    try
        let change = addChildChange 0 "stamp-prefix"
        let! postResult =
            CoreDoor.postEvents (admittedChanges (host agent))
                [ change ]
            |> Async.StartAsTask
        match postResult with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok ackJson ->
            let ack = decodeAck ackJson
            let confirmed =
                Assert.Single(ack.events) 
            Assert.Equal(change.submissionId, confirmed.submissionId)
            Assert.Equal<Op list>((eventOps change), (eventOps confirmed))
            Assert.Equal(None, ack.message)
            let! stamped =
                waitNotice
                    (host agent)
                    (function
                        | Notice.NodeStamped stamp ->
                            stamp.ops
                            |> List.exists (function
                                | Op.SetUpdateTime(nodeId, _, _) ->
                                    nodeId = Graph.workspacesId
                                | _ -> false)
                        | _ -> false)
                    100
            Assert.True(stamped)
            let! events =
                CoreMailboxApi.getEventsSince (host agent) (EventId.zero)
                |> Async.StartAsTask
            let changeEvent =
                events
                |> List.find (fun event ->
                    event.submissionId = change.submissionId)
            match Ev.ops changeEvent with
            | Some ops ->
                Assert.Equal<Op list>((eventOps confirmed), ops)
            | None -> Assert.Fail("Expected Change event")
            Assert.True(
                events
                |> List.exists (function
                    | { body = EventBody.Notice (Notice.NodeStamped _) } ->
                        true
                    | _ -> false))
            let body =
                ApiResponseSerialization.encodePollRequest
                    { eventId = EventId.zero; want = [] }
                |> Encode.toString 0
            let! pollResult =
                Api.postPoll (admittedChanges (host agent)) 10 20 body
                |> Async.StartAsTask
            match box pollResult with
            | :? ContentHttpResult as content ->
                match
                    Decode.fromString
                        ApiResponseSerialization.decodeChangeSuccessResponseDecoder
                        content.ResponseContent
                with
                | Error err -> Assert.Fail(err)
                | Ok response ->
                    Assert.True(
                        response.events
                        |> List.exists (function
                            | { body = EventBody.Notice (Notice.NodeStamped _) } ->
                                true
                            | _ -> false))
            | other ->
                Assert.Fail($"poll failed: {other.GetType().Name}")
    finally
        CoreMailboxApi.dispose (host agent)
}

