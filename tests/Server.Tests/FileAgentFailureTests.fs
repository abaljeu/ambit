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

let private getState agent = async {
    match! CoreMailboxApi.getState (host agent) with
    | Ok state -> return state
    | Error error ->
        Assert.Fail($"get state: {error}")
        return Unchecked.defaultof<_>
}

let private softFailPersist
    : string -> Graph -> NodeId -> Result<DateTime, string> =
    fun _ _ _ ->
        Error(DocumentPersistWrite.fileCouldNotSave "SYSTEM/secret.txt")

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
    : string -> Graph -> NodeId -> Result<DateTime, string> =
    fun _ _ _ ->
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
    let dependencies = { defaults with persistNode = throwingPersist }
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
            Assert.Equal(None, accepted.message)
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
            waitForNotices
                (fun () ->
                    CoreMailboxApi.getEventsSince mailbox EventId.zero
                    |> Async.StartAsTask)
                (fun events ->
                    events
                    |> List.exists (fun event ->
                        match event.body with
                        | EventBody.Notice (Notice.PersistFailed _) -> true
                        | _ -> false))
        let nodeId, message = Assert.Single(persistFailed events)
        Assert.Equal(fileId, nodeId)
        Assert.Contains("injected persistence failure", message)
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
                persistNode =
                    fun _ _ _ ->
                        persistAttempted.SetResult()
                        WorkspaceGit.withWorkTreeGate workspaceRoot (fun () ->
                            writes.Enqueue("persisted")
                            Ok ())
                        |> Result.map (fun () -> DateTime.UtcNow)
        }
    let agent = FileAgent.createWithDependencies dependencies dataDir
    try
        let pending =
            CoreDoor.postEvents (admittedChanges (host agent))
                ((changedBody ()))
            |> Async.StartAsTask
        let! postResult = pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        match postResult with
        | Error error -> Assert.Fail($"expected success while the write is held: {error}")
        | Ok accepted -> Assert.Equal(None, accepted.message)
        Assert.True(persistAttempted.Task.Wait(2000))
        Assert.Empty(writes)
        releaseHolder.SetResult()
        let released = System.Threading.SpinWait.SpinUntil(
            (fun () -> not writes.IsEmpty),
            2000)
        Assert.True(released)
        Assert.Single(writes) |> ignore
    finally
        releaseHolder.TrySetResult() |> ignore
        holder.Wait()
        CoreMailboxApi.dispose (host agent)
}

let private waitForPersistFailure agent = task {
    let! events =
        waitForNotices
            (fun () ->
                CoreMailboxApi.getEventsSince (host agent) EventId.zero
                |> Async.StartAsTask)
            (fun events ->
                events
                |> List.exists (fun event ->
                    match event.body with
                    | EventBody.Notice (Notice.PersistFailed _) -> true
                    | _ -> false))
    match
        events
        |> List.tryPick (fun event ->
            match event.body with
            | EventBody.Notice (Notice.PersistFailed (_, message)) ->
                Some message
            | _ -> None)
    with
    | Some message -> return message
    | None ->
        Assert.Fail("expected PersistFailed")
        return ""
}

[<Fact>]
let ``soft-fail live-save still commits graph and returns could-not-save message`` () = task {
    let dataDir = newTempDir ()
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies = { defaults with persistNode = softFailPersist }
    let agent = FileAgent.createWithDependencies dependencies dataDir
    try
        let! postResult =
            CoreDoor.postEvents (admittedChanges (host agent))
                ((softFailEditBody ()))
            |> Async.StartAsTask
        match postResult with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok ackJson ->
            Assert.Equal(None, decodeAckMessage ackJson)
            let! message = waitForPersistFailure agent
            Assert.Equal(
                DocumentPersistWrite.fileCouldNotSave "SYSTEM/secret.txt",
                message)
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
let ``soft-fail Edit is not in the graph after restart`` () = task {
    let dataDir = newTempDir ()
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies = { defaults with persistNode = softFailPersist }
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

    let agent2 = FileAgent.createWithDependencies dependencies dataDir
    try
        let! state =
            getState agent2 |> Async.StartAsTask
        Assert.False(
            state.graph.nodes
            |> Map.exists (fun _ n -> n.text = "soft-fail-probe"))
        Assert.Equal(EventId.fromJson 2, state.eventId)
    finally
        CoreMailboxApi.dispose (host agent2)
}

let private countingPersist (count: int ref) =
    fun _ _ (_: NodeId) ->
        count.Value <- count.Value + 1
        Ok DateTime.UtcNow

let private addChildChange _rev text =
    let childId = NodeId.New()
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = ""
      body = EventBody.Change
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

let private waitForPersisted agent = task {
    let! events =
        waitForNotices
            (fun () ->
                CoreMailboxApi.getEventsSince (host agent) EventId.zero
                |> Async.StartAsTask)
            (fun events ->
                events
                |> List.exists (fun event ->
                    match event.body with
                    | EventBody.Notice (Notice.NodePersisted _) -> true
                    | _ -> false))
    return events
}

[<Fact>]
let ``ACK keeps the Edit events and NodePersisted follows the write`` () = task {
    let dataDir = newTempDir ()
    let count = ref 0
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies =
        { defaults with persistNode = countingPersist count }
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
            Assert.Equal(None, ack.message)
            let confirmed = Assert.Single(ack.events)
            Assert.Equal(change.submissionId, confirmed.submissionId)
            Assert.Equal<Op list>((eventOps change), (eventOps confirmed))
            let! events = waitForPersisted agent
            Assert.Contains(
                events,
                fun event ->
                    match event.body with
                    | EventBody.Notice (Notice.NodePersisted _) -> true
                    | _ -> false)
            let storedChange = Assert.Single(changesOnly events)
            match Ev.ops storedChange with
            | Some stored ->
                Assert.Equal<Op list>((eventOps change), stored)
            | None -> Assert.Fail("Expected Change event")
            Assert.True(count.Value >= 1)
    finally
        CoreMailboxApi.dispose (host agent)
}

/// A new special node starts Persisted. Mark it Unpersisted before
/// the persist push so the first file write is not skipped. That
/// mark does not post NodePersisting.
[<Fact>]
let ``new special node writes its file without NodePersisting`` () = task {
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    let mailbox = host agent
    try
        let! state =
            CoreMailboxApi.getState mailbox |> Async.StartAsTask
        let state = requireOk "state" state
        let wsId, ops =
            FileNodeOps.planCreateWorkspace state.graph "home"
        let event =
            { id = EventId.zero
              submissionId = Guid.NewGuid()
              authority = Authority "Browser"
              commandName = "Create workspace"
              body = EventBody.Change ops }
        let! posted =
            CoreMailboxApi.postEvents mailbox testCaller [ event ]
            |> Async.StartAsTask
        requireOk "post" posted |> ignore
        let! events =
            waitForNotices
                (fun () ->
                    CoreMailboxApi.getEventsSince mailbox EventId.zero
                    |> Async.StartAsTask)
                (fun events ->
                    events
                    |> List.exists (fun event ->
                        match event.body with
                        | EventBody.Notice (Notice.NodePersisted (id, _, _)) ->
                            id = wsId
                        | _ -> false))
        let hasNotice pred =
            events
            |> List.exists (fun event ->
                match event.body with
                | EventBody.Notice notice -> pred notice
                | _ -> false)
        Assert.False(
            hasNotice (function
                | Notice.NodePersisting id -> id = wsId
                | _ -> false),
            $"NodePersisting was posted, got {events}")
        Assert.True(
            hasNotice (function
                | Notice.NodePersisted (id, _, _) -> id = wsId
                | _ -> false),
            $"NodePersisted missing, got {events}")
        let! after =
            CoreMailboxApi.getState mailbox |> Async.StartAsTask
        let after = requireOk "after" after
        let path =
            DocumentPersistPath.resolveArtifactPath dataDir after.graph wsId
            |> requireOk "path"
        Assert.True(File.Exists path, path)
    finally
        CoreMailboxApi.dispose mailbox
}

