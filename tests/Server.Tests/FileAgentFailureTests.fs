module Gambol.Server.Tests.FileAgentFailureTests

open System
open System.IO
open System.Threading
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private changedBody () =
    let childId = NodeId.New()
    let change =
        {
            id = 0
            changeId = Guid.NewGuid()
            ops =
                [
                    Op.NewNode(childId, "failure probe")
                    Op.Replace(
                        Graph.rootId,
                        0,
                        [],
                        [ ChildNode.owner childId ])
                ]
        }
    Encode.toString 0 (
        Serialization.encodeChangeBatch
            { changes = [ change ] })

let private softFailPersist : string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string> =
    fun _ _ postGraph _ ->
        Ok {
            graph = postGraph
            message = Some(DocumentPersistence.fileCouldNotSave "SYSTEM/secret.txt")
        }

let private decodeAck (json: string) =
    match
        Decode.fromString
            ApiResponseSerialization.decodeChangeSuccessResponseDecoder
            json
    with
    | Ok response -> response
    | Error err -> failwith $"decode ack: {err}"

let private decodeAckMessage (json: string) : string option =
    (decodeAck json).message

/// Insert a normal child at ROOT index 0 (old span [] = insert).
let private softFailEditBody () =
    let childId = NodeId.New()
    let change =
        {
            id = 0
            changeId = Guid.NewGuid()
            ops =
                [
                    Op.NewNode(childId, "soft-fail-probe")
                    Op.Replace(
                        Graph.rootId,
                        0,
                        [],
                        [ ChildNode.owner childId ])
                ]
        }
    Encode.toString 0 (
        Serialization.encodeChangeBatch
            { changes = [ change ] })

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
            FileAgent.postChange agent (changedBody ())
            |> Async.StartAsTask
            |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        match postResult with
        | Ok _ -> Assert.Fail("Expected persistence failure.")
        | Error error ->
            Assert.Contains("Internal server error in FileAgent PostChange", error)
            Assert.Contains($"(dataDir={dataDir})", error)

        let log = File.ReadAllText logPath
        Assert.Contains("EXCEPTION source=FileAgent operation=PostChange", log)
        Assert.Contains("context=bodyLength=", log)
        Assert.Contains("type=System.InvalidOperationException", log)
        Assert.Contains("message=injected persistence failure", log)
        Assert.Contains("stack=", log)

        let! stateJson =
            FileAgent.getState agent
            |> Async.StartAsTask
            |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        Assert.Contains("\"revision\":0", stateJson)
    finally
        FileAgent.dispose agent
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
            FileAgent.postChange agent (changedBody ())
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

        let! stateJson =
            FileAgent.getState agent
            |> Async.StartAsTask
            |> fun pending -> pending.WaitAsync(TimeSpan.FromSeconds(2.0))
        Assert.Contains("\"revision\":0", stateJson)
    finally
        // let the orphaned background task finish before disposing shared resources
        Thread.Sleep(hangMs)
        FileAgent.dispose agent
}

[<Fact>]
let ``soft-fail live-save still commits graph and returns could-not-save message`` () = task {
    let dataDir = newTempDir ()
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies = { defaults with persistGraphOps = softFailPersist }
    let agent = FileAgent.createWithDependencies dependencies dataDir
    try
        let! postResult =
            FileAgent.postChange agent (softFailEditBody ())
            |> Async.StartAsTask
        match postResult with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok ackJson ->
            Assert.Equal(
                Some(DocumentPersistence.fileCouldNotSave "SYSTEM/secret.txt"),
                decodeAckMessage ackJson)
        let! stateJson =
            FileAgent.getState agent |> Async.StartAsTask
        Assert.Contains("soft-fail-probe", stateJson)
        Assert.Contains("\"revision\":1", stateJson)
    finally
        FileAgent.dispose agent
}

[<Fact>]
let ``soft-fail log is not replayed into FileAgent state after restart`` () = task {
    let dataDir = newTempDir ()
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies = { defaults with persistGraphOps = softFailPersist }
    let agent1 = FileAgent.createWithDependencies dependencies dataDir
    try
        let! postResult =
            FileAgent.postChange agent1 (softFailEditBody ())
            |> Async.StartAsTask
        match postResult with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok _ -> ()
    finally
        FileAgent.dispose agent1

    // Meta checkpoint stays behind after soft-fail; restart trusts that checkpoint.
    Assert.Equal(Revision 0, Bookkeeping.readRevision dataDir)
    let agent2 = FileAgent.createWithDependencies dependencies dataDir
    try
        let! stateJson =
            FileAgent.getState agent2 |> Async.StartAsTask
        Assert.DoesNotContain("soft-fail-probe", stateJson)
        Assert.Contains("\"revision\":0", stateJson)
        Assert.Empty(agent2.initialState.history.past)
        Assert.Empty(agent2.initialState.history.future)
    finally
        FileAgent.dispose agent2
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

let private encodeBatch (changes: Change list) =
    Encode.toString 0 (Serialization.encodeChangeBatch { changes = changes })

let private addChildChange rev text =
    let childId = NodeId.New()
    { id = rev
      changeId = Guid.NewGuid()
      ops =
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, 0, [], [ ChildNode.owner childId ]) ] }

let private suffixAfter (submitted: Change) (confirmed: Change) =
    List.skip submitted.ops.Length confirmed.ops

[<Fact>]
let ``ACK returns stamped complete Change equal to ChangeLog`` () = task {
    let dataDir = newTempDir ()
    let count = ref 0
    let defaults = FileAgent.defaultDependencies dataDir
    let dependencies =
        { defaults with persistGraphOps = incrementingStampPersist count }
    let agent = FileAgent.createWithDependencies dependencies dataDir
    try
        let change = addChildChange 0 "stamp-prefix"
        let! postResult =
            FileAgent.postChange agent (encodeBatch [ change ])
            |> Async.StartAsTask
        match postResult with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok ackJson ->
            let ack = decodeAck ackJson
            let confirmed = Assert.Single(ack.changes)
            Assert.Equal(change.changeId, confirmed.changeId)
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
            let! logged =
                FileAgent.getChangesSince agent 0 |> Async.StartAsTask
            Assert.Equal<Change list>([ confirmed ], logged)
    finally
        FileAgent.dispose agent
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
            FileAgent.postChange agent (encodeBatch [ first ])
            |> Async.StartAsTask
        let firstConfirmed =
            match firstResult with
            | Ok json -> Assert.Single((decodeAck json).changes)
            | Error err -> failwith err
        let second = addChildChange 1 "second-new"
        let! batchResult =
            FileAgent.postChange agent (encodeBatch [ second; first ])
            |> Async.StartAsTask
        match batchResult with
        | Error err -> Assert.Fail($"expected Ok ack, got Error {err}")
        | Ok ackJson ->
            let ack = decodeAck ackJson
            Assert.Equal(2, ack.changes.Length)
            let secondConfirmed, trailingDup = ack.changes.[0], ack.changes.[1]
            Assert.Equal(firstConfirmed, trailingDup)
            Assert.Equal(second.changeId, secondConfirmed.changeId)
            Assert.Equal<Op list>(
                second.ops,
                List.take second.ops.Length secondConfirmed.ops)
            let secondSuffix = suffixAfter second secondConfirmed
            Assert.NotEmpty(secondSuffix)
            Assert.NotEqual<Op list>(
                suffixAfter first firstConfirmed,
                secondSuffix)
            let! logged =
                FileAgent.getChangesSince agent 0 |> Async.StartAsTask
            Assert.Equal<Change list>(
                [ firstConfirmed; secondConfirmed ],
                logged)
    finally
        FileAgent.dispose agent
}
