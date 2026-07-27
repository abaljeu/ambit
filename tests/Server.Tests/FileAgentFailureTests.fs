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
                        [ { ref = Ownership.Owner; id = childId } ])
                ]
        }
    Encode.toString 0 (
        Serialization.encodeChangeBatch { changes = [ change ] })

let private softFailPersist : string -> Graph -> Graph -> Op list -> Result<PersistGraphOk, string> =
    fun _ _ postGraph _ ->
        Ok {
            graph = postGraph
            message = Some(DocumentPersistence.fileCouldNotSave "SYSTEM/secret.txt")
        }

let private decodeAckMessage (json: string) : string option =
    match Decode.fromString Serialization.decodeChangeBatchAck json with
    | Ok ack -> ack.message
    | Error err -> failwith $"decode ack: {err}"

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
                        [ { ref = Ownership.Owner; id = childId } ])
                ]
        }
    Encode.toString 0 (
        Serialization.encodeChangeBatch { changes = [ change ] })

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
let ``soft-fail graph edit survives FileAgent restart via log replay`` () = task {
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

    // Meta checkpoint stays behind after soft-fail; disk has no probe text.
    Assert.Equal(Revision 0, Bookkeeping.readRevision dataDir)
    let agent2 = FileAgent.createWithDependencies dependencies dataDir
    try
        let! stateJson =
            FileAgent.getState agent2 |> Async.StartAsTask
        Assert.Contains("soft-fail-probe", stateJson)
        Assert.Contains("\"revision\":1", stateJson)
    finally
        FileAgent.dispose agent2
}
