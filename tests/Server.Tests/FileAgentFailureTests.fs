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
        id = 0
        submissionId = Guid.NewGuid()
        ops =
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
        id = 0
        submissionId = Guid.NewGuid()
        ops =
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
            (admittedChanges (host agent)).postChange (changedBody ())
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
        Assert.Equal(Revision 0, state.revision)
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
            (admittedChanges (host agent)).postChange (changedBody ())
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
        Assert.Equal(Revision 0, state.revision)
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
            (admittedChanges (host agent)).postChange (softFailEditBody ())
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
        Assert.Equal(Revision 1, state.revision)
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
            (admittedChanges (host agent1)).postChange (softFailEditBody ())
            |> Async.StartAsTask
        match postResult with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok _ -> ()
    finally
        CoreMailbox.dispose (host agent1)

    // Meta checkpoint stays behind after soft-fail; restart trusts that checkpoint.
    Assert.Equal(Revision 0, Bookkeeping.readRevision dataDir)
    let agent2 = FileAgent.createWithDependencies dependencies dataDir
    try
        let! state =
            getState agent2 |> Async.StartAsTask
        Assert.False(
            state.graph.nodes
            |> Map.exists (fun _ n -> n.text = "soft-fail-probe"))
        Assert.Equal(Revision 0, state.revision)
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

let private addChildChange rev text =
    let childId = NodeId.New()
    { id = rev
      submissionId = Guid.NewGuid()
      ops =
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

let private suffixAfter (submitted: Change) (confirmed: Change) =
    List.skip submitted.ops.Length confirmed.ops

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
            (admittedChanges (host agent)).postChange [ change ]
            |> Async.StartAsTask
        match postResult with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok ackJson ->
            let ack = decodeAck ackJson
            let confirmed =
                Assert.Single(ack.events) |> Ev.asChange
            Assert.Equal(change.submissionId, confirmed.submissionId)
            Assert.Equal<Op list>(
                change.ops,
                List.take change.ops.Length confirmed.ops)
            let suffix = suffixAfter change confirmed
            Assert.NotEmpty(suffix)
            suffix
            |> List.iter (fun op ->
                match op with
                | Op.SetUpdateTime(nodeId, _, _) ->
                    Assert.Equal(Graph.workspacesId, nodeId)
                | _ -> failwith "expected SetUpdateTime suffix")
            let! events =
                CoreMailbox.getEventsSince (host agent) (Gambol.Shared.EventId 0)
                |> Async.StartAsTask
            Assert.Single(events) |> ignore
            let event = events.[0]
            match Ev.ops event with
            | Some ops ->
                Assert.Equal<Op list>(confirmed.ops, ops)
            | None ->
                Assert.Fail("Expected Change event")
    finally
        CoreMailbox.dispose (host agent)
}

[<Fact>]
let ``trailing duplicate keeps stamps on last new Change`` () = task {
    let dataDir = newTempDir ()
    let count = ref 0
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies =
        { defaults with persistGraphOps = incrementingStampPersist count }
    let agent = FileAgent.createWithDependencies dependencies dataDir
    try
        let first = addChildChange 0 "first-new"
        let! firstResult =
            (admittedChanges (host agent)).postChange [ first ]
            |> Async.StartAsTask
        let firstConfirmed =
            match firstResult with
            | Ok json ->
                Assert.Single((decodeAck json).events)
                |> Ev.asChange
            | Error err -> failwith err
        let second = addChildChange 1 "second-new"
        // Multi-Change persist batch stays on PersistHandlers until 42.
        match
            (FileAgent.persist agent).handlers.postChange [ second; first ]
        with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok ack ->
            Assert.Equal(2, ack.events.Length)
            let secondConfirmed, trailingDup =
                Ev.asChange ack.events.[0],
                Ev.asChange ack.events.[1]
            Assert.Equal(firstConfirmed.submissionId, trailingDup.submissionId)
            Assert.Equal<Op list>(firstConfirmed.ops, trailingDup.ops)
            Assert.Equal(second.submissionId, secondConfirmed.submissionId)
            Assert.Equal<Op list>(
                second.ops,
                List.take second.ops.Length secondConfirmed.ops)
            let secondSuffix = suffixAfter second secondConfirmed
            Assert.NotEmpty(secondSuffix)
            Assert.NotEqual<Op list>(
                suffixAfter first firstConfirmed,
                secondSuffix)
            let! events =
                CoreMailbox.getEventsSince (host agent) (Gambol.Shared.EventId 0)
                |> Async.StartAsTask
            // Direct handlers.postChange skips the postEvent door; EventLog
            // only has the mailbox-admitted first Change.
            Assert.Equal(1, events.Length)
            match Ev.ops events.[0] with
            | Some ops1 ->
                Assert.Equal<Op list>(firstConfirmed.ops, ops1)
            | None ->
                Assert.Fail("Expected Change event")
    finally
        CoreMailbox.dispose (host agent)
}
