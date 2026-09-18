module Gambol.Server.Tests.FileAgentFailureTests

open System
open System.IO
open System.Threading
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
    match! CoreMailbox.getState (host agent) with
    | Ok state -> return state
    | Error error ->
        Assert.Fail($"get state: {error}")
        return Unchecked.defaultof<_>
}

let private softFailPersist : string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string> =
    fun _ _ postGraph _ ->
        Ok {
            graph = postGraph
            message = Some(DocumentPersistence.fileCouldNotSave "SYSTEM/secret.txt")
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

[<Fact>]
let ``persistence exception is logged replied and mailbox survives`` () = task {
    let dataDir = newTempDir ()
    let logPath = HttpResponseLog.logPath dataDir
    HttpResponseLog.prepareFresh logPath
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies =
        {
            defaults with
                persistGraphOps =
                    fun _ _ _ _ ->
                        raise (InvalidOperationException("injected persistence failure"))
                appendException =
                    fun operation context ex ->
                        defaults.appendException operation context ex
                        raise (IOException("injected logger failure"))
        }
    let agent = FileAgent.createWithDependencies dependencies dataDir
    try
        let! postResult =
            (admittedChanges (host agent)).postEvents
                ((changedBody ()))
            |> Async.StartAsTask
            |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        match postResult with
        | Ok _ -> Assert.Fail("Expected persistence failure.")
        | Error error ->
            Assert.Contains("Internal server error in FileAgent PostEvent", error)
            Assert.Contains($"(dataDir={dataDir})", error)

        let log = File.ReadAllText logPath
        Assert.Contains("EXCEPTION source=FileAgent operation=PostEvent", log)
        Assert.Contains("type=System.InvalidOperationException", log)
        Assert.Contains("message=injected persistence failure", log)
        Assert.Contains("stack=", log)

        let! state =
            getState agent
            |> Async.StartAsTask
            |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        Assert.Equal(EventId.zero, state.eventId)
    finally
        CoreMailbox.dispose (host agent)
}

/// A hang (not an exception) in the persist step must not wedge the mailbox forever:
/// the handler should reject within the (test-shortened) timeout, and the mailbox
/// must still serve a subsequent GetState request afterwards.
[<Fact>]
let ``persist step hang is rejected within timeout and mailbox survives`` () = task {
    let dataDir = newTempDir ()
    let defaults = FileAgent.defaultDependencies dataDir
    let hangMs = 500
    let dependencies =
        {
            defaults with
                persistGraphOps =
                    fun _ preGraph _ _ ->
                        Thread.Sleep(hangMs)
                        Ok { graph = preGraph; message = None }
                changeProcessingTimeoutMs = 50
        }
    let agent = FileAgent.createWithDependencies dependencies dataDir
    try
        let sw = Diagnostics.Stopwatch.StartNew()
        let! postResult =
            (admittedChanges (host agent)).postEvents
                ((changedBody ()))
            |> Async.StartAsTask
            |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        sw.Stop()
        match postResult with
        | Ok _ -> Assert.Fail("Expected change processing to time out.")
        | Error error ->
            Assert.Contains("timed out", error)
        Assert.True(
            sw.ElapsedMilliseconds < int64 hangMs,
            $"Expected reject before the {hangMs}ms hang completed, took {sw.ElapsedMilliseconds}ms.")

        let! state =
            getState agent
            |> Async.StartAsTask
            |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        Assert.Equal(EventId.zero, state.eventId)
    finally
        // let the orphaned background task finish before disposing shared resources
        Thread.Sleep(hangMs)
        CoreMailbox.dispose (host agent)
}

[<Fact>]
let ``soft-fail live-save still commits graph and returns could-not-save message`` () = task {
    let dataDir = newTempDir ()
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies = { defaults with persistGraphOps = softFailPersist }
    let agent = FileAgent.createWithDependencies dependencies dataDir
    try
        let! postResult =
            (admittedChanges (host agent)).postEvents
                ((softFailEditBody ()))
            |> Async.StartAsTask
        match postResult with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok ackJson ->
            Assert.Equal(
                Some(DocumentPersistence.fileCouldNotSave "SYSTEM/secret.txt"),
                decodeAckMessage ackJson)
        let! state =
            getState agent |> Async.StartAsTask
        Assert.True(
            state.graph.nodes
            |> Map.exists (fun _ n -> n.text = "soft-fail-probe"))
        Assert.NotEqual(EventId.zero, state.eventId)
    finally
        CoreMailbox.dispose (host agent)
}

[<Fact>]
let ``soft-fail log is not replayed into FileAgent state after restart`` () = task {
    let dataDir = newTempDir ()
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies = { defaults with persistGraphOps = softFailPersist }
    let agent1 = FileAgent.createWithDependencies dependencies dataDir
    try
        let! postResult =
            (admittedChanges (host agent1)).postEvents
                ((softFailEditBody ()))
            |> Async.StartAsTask
        match postResult with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok _ -> ()
    finally
        CoreMailbox.dispose (host agent1)

    // Meta checkpoint stays behind after soft-fail; restart trusts that checkpoint.
    Assert.Equal(EventId.zero, Bookkeeping.readEventId dataDir)
    let agent2 = FileAgent.createWithDependencies dependencies dataDir
    try
        let! state =
            getState agent2 |> Async.StartAsTask
        Assert.False(
            state.graph.nodes
            |> Map.exists (fun _ n -> n.text = "soft-fail-probe"))
        Assert.Equal(EventId.zero, state.eventId)
    finally
        CoreMailbox.dispose (host agent2)
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
            (admittedChanges (host agent)).postEvents
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
                CoreMailbox.getEventsSince (host agent) (EventId.zero)
                |> Async.StartAsTask
            Assert.Single(events) |> ignore
            let event = events.[0]
            match Ev.ops event with
            | Some ops ->
                Assert.Equal<Op list>((eventOps confirmed), ops)
            | None ->
                Assert.Fail("Expected Change event")
    finally
        CoreMailbox.dispose (host agent)
}

