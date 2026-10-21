module Gambol.Server.Tests.FileAgentFailureTests

open System
open System.Collections.Concurrent
open System.IO
open System.Threading.Tasks
open Xunit
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

let private oldPersistTimeoutMs = 8000

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

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error error -> failwith $"{label}: {error}"

let private owned = ChildNode.owners

let private special id kind name owner =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private fileEditGraph () =
    let wsId = NodeId.New()
    let fileId = NodeId.New()
    let bodyId = NodeId.New()
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode
            (special wsId Workspace "home" Graph.workspacesId)
        |> Graph.addDetachedNode (special fileId File "a.txt" wsId)
        |> Graph.addDetachedNode
            (Node.Create(bodyId, text = "alpha", owner = fileId))
        |> Graph.replace Graph.workspacesId 0 [] (owned [ wsId ])
        |> requireOk "workspaces"
        |> Graph.replace wsId 0 [] (owned [ fileId ])
        |> requireOk "file"
        |> Graph.replace fileId 0 [] (owned [ bodyId ])
        |> requireOk "body"
    graph, fileId, bodyId

let private fileIdIn (state: State) =
    state.graph.nodes
    |> Map.pick (fun id node ->
        match node.kind, Filename.tryValue node.name with
        | Special File, Some "a.txt" -> Some id
        | _ -> None)

let private childEdit fileId =
    let childId = NodeId.New()
    let events =
        [ {
            id = EventId.zero
            submissionId = Guid.NewGuid()
            authority = Authority "Browser"
            commandName = ""
            body =
                EventBody.Change
                    [ Op.NewNode(childId, "ALPHA")
                      Op.Replace(fileId, [], [ ChildNode.owner childId ]) ]
          } ]
    childId, events

let private openThenEdit mailbox = async {
    let! stateResult = CoreMailboxApi.getState mailbox
    let state =
        match stateResult with
        | Ok value -> value
        | Error error -> failwith $"state: {error}"
    let fileId = fileIdIn state
    CoreMailboxApi.postNotice mailbox (Notice.NodeParsed fileId)
    let! parsed = CoreMailboxApi.getState mailbox
    match parsed with
    | Error error -> failwith $"parsed: {error}"
    | Ok _ -> ()
    let childId, events = childEdit fileId
    let! posted =
        CoreDoor.postEvents (admittedChanges mailbox) events
    return fileId, childId, posted
}

let private throwingPersist
    : string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string> =
    fun _ _ _ _ ->
        raise (InvalidOperationException("injected persistence failure"))

let private persistFailed events =
    events
    |> List.choose (fun event ->
        match event.body with
        | EventBody.Notice (Notice.PersistFailed (nodeId, message)) ->
            Some (nodeId, message)
        | _ -> None)

let private agentWithFailure dataDir =
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies = { defaults with persistGraphOps = throwingPersist }
    FileAgent.createWithDependencies dependencies dataDir

/// A failed document write keeps the edit. The order succeeds. The file
/// stays Unpersisted. The mailbox still serves a later read.
[<Fact>]
let ``Failed write keeps the edit`` () = task {
    let dataDir = newTempDir ()
    let graph, _fileId, _bodyId = fileEditGraph ()
    DocumentPersistWrite.writeAllDocuments dataDir graph
    |> requireOk "seed"
    |> ignore
    let agent = agentWithFailure dataDir
    let mailbox = host agent
    try
        let! fileId, childId, postResult =
            openThenEdit mailbox
            |> Async.StartAsTask
            |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        match postResult with
        | Error error -> Assert.Fail($"expected success, got {error}")
        | Ok accepted ->
            Assert.Equal(
                Some "injected persistence failure",
                accepted.message)
        let! state =
            getState agent
            |> Async.StartAsTask
            |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        Assert.Equal("ALPHA", state.graph.nodes.[childId].text)
        Assert.Equal(
            PersistState.Unpersisted,
            state.graph.nodes.[fileId].documentState.persistState)
        Assert.True(EventId.isAccepted state.eventId)
    finally
        CoreMailboxApi.dispose mailbox
}

[<Fact>]
let ``Failed write posts PersistFailed`` () = task {
    let dataDir = newTempDir ()
    let graph, _fileId, _bodyId = fileEditGraph ()
    DocumentPersistWrite.writeAllDocuments dataDir graph
    |> requireOk "seed"
    |> ignore
    let agent = agentWithFailure dataDir
    let mailbox = host agent
    try
        let! fileId, _childId, postResult =
            openThenEdit mailbox
            |> Async.StartAsTask
            |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        match postResult with
        | Error error -> Assert.Fail($"expected success, got {error}")
        | Ok _ -> ()
        let! events =
            CoreMailboxApi.getEventsSince mailbox EventId.zero
            |> Async.StartAsTask
            |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        let nodeId, message = Assert.Single(persistFailed events)
        Assert.Equal(fileId, nodeId)
        Assert.Contains("injected persistence failure", message)
    finally
        CoreMailboxApi.dispose mailbox
}

[<Fact>]
let ``Persist waits past the old timeout and continues after gate release`` () = task {
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
        Assert.False(
            pending.Wait(oldPersistTimeoutMs + 250))
        Assert.Empty(writes)
        releaseHolder.SetResult()

        let! postResult = pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        match postResult with
        | Error error -> Assert.Fail($"expected queued Persist to continue: {error}")
        | Ok _ -> ()
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
let ``soft-fail live-save still commits graph and returns could-not-save message`` () = task {
    let dataDir = newTempDir ()
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies = { defaults with persistGraphOps = softFailPersist }
    let agent = FileAgent.createWithDependencies dependencies dataDir
    try
        let! postResult =
            CoreDoor.postEvents (admittedChanges (host agent))
                ((softFailEditBody ()))
            |> Async.StartAsTask
        match postResult with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok ackJson ->
            Assert.Equal(
                Some(DocumentPersistWrite.fileCouldNotSave "SYSTEM/secret.txt"),
                decodeAckMessage ackJson)
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

let private suffixAfter (submitted: Ev) (confirmed: Ev) =
    List.skip (eventOps submitted).Length (eventOps confirmed)

[<Fact>]
let ``ACK returns stamped complete Change equal to EventLog`` () = task {
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
            Assert.Equal<Op list>(
                (eventOps change),
                List.take (eventOps change).Length (eventOps confirmed))
            let suffix = suffixAfter change confirmed
            Assert.NotEmpty(suffix)
            suffix
            |> List.iter (fun op ->
                match op with
                | Op.SetUpdateTime(nodeId, _, _) ->
                    Assert.Equal(Graph.workspacesId, nodeId)
                | _ -> failwith "expected SetUpdateTime suffix")
            let! events =
                CoreMailboxApi.getEventsSince (host agent) (EventId.zero)
                |> Async.StartAsTask
            let event = Assert.Single(changesOnly events)
            match Ev.ops event with
            | Some ops ->
                Assert.Equal<Op list>((eventOps confirmed), ops)
            | None ->
                Assert.Fail("Expected Change event")
    finally
        CoreMailboxApi.dispose (host agent)
}

