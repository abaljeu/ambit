module Gambol.Server.Tests.DbAgentTests

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Decode = Thoth.Json.Newtonsoft.Decode

let private encodeChangeBatch (changes: Ev list) =
    changes

let private emptyChange () =
    [ { id = EventId.zero
        submissionId = Guid.NewGuid()
        authority = Authority "Browser"
        commandName = ""
        body = EventBody.Change [] } ]

let private host agent = admittedHostDb agent

let private getStateFrom mailbox = async {
    match! CoreMailbox.getState mailbox with
    | Ok state -> return state
    | Error error ->
        Assert.Fail($"get state: {error}")
        return Unchecked.defaultof<_>
}

let private getState agent = getStateFrom (host agent)

let private waitUntil (timeoutMs: int) (predicate: unit -> bool) : Task<bool> = task {
    let mutable elapsed = 0
    while elapsed < timeoutMs && not (predicate ()) do
        do! Task.Delay(20)
        elapsed <- elapsed + 20
    return predicate ()
}

let private stateWithDetachedNode () =
    let orphanId = NodeId.New()
    let graph0 = Graph.create ()
    let graph =
        graph0.nodes
        |> Map.add orphanId (Node.Create(orphanId, text = "orphan"))
        |> Graph.fromNodes Graph.rootId

    { graph = graph
      eventId = EventId.fromJson 4 },
    orphanId

[<Fact>]
let ``DbAgent empty test DB has revision 0 and canonical ROOT`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let agent = DbAgent.create connStr
    let! rev = CoreMailbox.getEventId (host agent) |> Async.StartAsTask
    let! state = getState agent |> Async.StartAsTask
    Assert.Equal(EventId.zero, rev)
    let graph = state.graph
    let root = graph.nodes.[graph.root]
    Assert.Equal(4, graph.nodes.Count)
    Assert.Equal("ROOT", root.text)
    Assert.Equal(3, root.children.Length)
    Assert.Equal(Graph.workspacesId, root.children.[0].id)
    Assert.Equal("Workspaces", graph.nodes.[Graph.workspacesId].text)
    Assert.Equal(Graph.systemId, root.children.[1].id)
    Assert.Equal("System", graph.nodes.[Graph.systemId].text)
    Assert.Equal(Graph.trashId, root.children.[2].id)
    Assert.Equal("Trash", graph.nodes.[Graph.trashId].text)
}

[<Fact>]
let ``DbAgent startup sweeps and trims unreachable persisted nodes before ready`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let orphanId = NodeId.New()
    let graph0 = Graph.create ()
    let orphan = Node.Create(orphanId, text = "orphan")
    let graph =
        graph0.nodes
        |> Map.add orphanId orphan
        |> Graph.fromNodes Graph.rootId

    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do! Database.replaceGraphProjectionWithTx tx graph 9 |> Async.AwaitTask
    tx.Commit()

    let agent = DbAgent.create connStr
    let! ready = waitUntil 2000 (fun () -> CoreMailbox.isReady (host agent))
    Assert.True(ready, "Expected startup sweep to enable normal queue processing.")
    let! state = getState agent |> Async.StartAsTask
    let! revision = CoreMailbox.getEventId (host agent) |> Async.StartAsTask
    let loaded = state.graph

    Assert.Equal(EventId.fromJson 9, revision)
    Assert.False(loaded.nodes.ContainsKey orphanId)

    use checkConn = Database.getConnection connStr
    do! checkConn.OpenAsync()
    use command = checkConn.CreateCommand()
    command.CommandText <- "SELECT EXISTS (SELECT 1 FROM nodes WHERE id = @id)"
    command.Parameters.AddWithValue("id", orphanId.Value) |> ignore
    let! exists = command.ExecuteScalarAsync()
    Assert.False(unbox<bool> exists)
}

[<Fact>]
let ``DbAgent serves reads while sweep buffers FIFO mutations then trims`` () = task {
    let initialState, orphanId = stateWithDetachedNode ()
    use entered = new ManualResetEventSlim(false)
    use release = new ManualResetEventSlim(false)
    let sweep (_: Graph) =
        entered.Set()
        release.Wait()
        Ok [ orphanId.Value ]
    let agent = DbAgent.createForTest initialState sweep
    let mailbox = host agent
    Assert.True(entered.Wait(1000), "Expected startup sweep to begin.")
    Assert.False(CoreMailbox.isReady mailbox)

    let stateTask = getStateFrom mailbox |> Async.StartAsTask
    let revisionTask = CoreMailbox.getEventId mailbox |> Async.StartAsTask
    let firstPost =
        (admittedChanges mailbox).postEvents ((emptyChange ()))
        |> Async.StartAsTask
    let secondPost =
        (admittedChanges mailbox).postEvents ((emptyChange ()))
        |> Async.StartAsTask
    do! Task.Delay(100)
    Assert.True(stateTask.IsCompleted)
    Assert.True(revisionTask.IsCompleted)
    Assert.False(firstPost.IsCompleted)
    Assert.False(secondPost.IsCompleted)
    let! beforeState = stateTask
    let! beforeRevision = revisionTask
    Assert.False(CoreMailbox.isReady mailbox)
    Assert.True(beforeState.graph.nodes.ContainsKey orphanId)
    Assert.Equal(EventId.fromJson 4, beforeRevision)
    release.Set()

    let! secondResult = secondPost
    Assert.True(firstPost.IsCompleted, "Expected first queued mutation to complete first.")
    let! firstResult = firstPost
    match firstResult with
    | Error error -> Assert.Contains("Unchanged submission is rejected.", error)
    | Ok _ -> Assert.Fail("Expected invalid first mutation to fail.")
    match secondResult with
    | Error error -> Assert.Contains("Unchanged submission is rejected.", error)
    | Ok _ -> Assert.Fail("Expected invalid buffered mutation to fail.")
    let! afterState = getStateFrom mailbox |> Async.StartAsTask
    Assert.True(CoreMailbox.isReady mailbox)
    Assert.False(afterState.graph.nodes.ContainsKey orphanId)
    Assert.True(CoreMailbox.isReady mailbox)
}

[<Fact>]
let ``DbAgent startup sweep failure preserves reads and fails mutations closed`` () = task {
    let initialState, orphanId = stateWithDetachedNode ()
    let agent =
        DbAgent.createForTest initialState (fun _ ->
            Error "Startup projection sweep failed: blocked")
    do! Task.Delay(50)
    Assert.False(CoreMailbox.isReady (host agent))

    let! postResult =
        (admittedChanges (host agent)).postEvents ((emptyChange ()))
        |> Async.StartAsTask

    match postResult with
    | Error error -> Assert.Contains("Startup projection sweep failed: blocked", error)
    | Ok _ -> Assert.Fail("Expected mutation rejection after startup sweep failure.")

    let! state = getState agent |> Async.StartAsTask
    let! revision = CoreMailbox.getEventId (host agent) |> Async.StartAsTask
    Assert.True(state.graph.nodes.ContainsKey orphanId)
    Assert.Equal(EventId.fromJson 4, revision)
}

[<Fact>]
let ``DbAgent new process loads state from projection and changes after post`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let agent1 = DbAgent.create connStr
    let! state0 = getState agent1 |> Async.StartAsTask
    let rootId = state0.graph.root
    let childId = NodeId.New()

    let change =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change
            [ Op.NewNode(childId, "reload-check")
              Op.Replace(rootId, [], [ ChildNode.owner childId ]) ] }

    let body = encodeChangeBatch [ change ]
    let! posted = (admittedChanges (host agent1)).postEvents (body) |> Async.StartAsTask

    match posted with
    | Error e -> Assert.Fail($"postChange: {e}")
    | Ok _ -> ()

    let agent2 = DbAgent.create connStr
    let! rev2 = CoreMailbox.getEventId (host agent2) |> Async.StartAsTask
    let! state2 = getState agent2 |> Async.StartAsTask
    Assert.True(EventId.isAccepted rev2)
    let graph2 = state2.graph
    Assert.Equal(Graph.rootId, graph2.root)
    let root = graph2.nodes.[graph2.root]
    Assert.Equal(4, root.children.Length)
    let cid = root.children.[0].id
    Assert.Equal("reload-check", graph2.nodes.[cid].text)
    Assert.Equal(Graph.workspacesId, root.children.[1].id)
    Assert.Equal(Graph.systemId, root.children.[2].id)
    Assert.Equal(Graph.trashId, root.children.[3].id)
}


[<Fact>]
let ``DbAgent reload preserves node updateTime from projection`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let agent1 = DbAgent.create connStr
    let! state0 = getState agent1 |> Async.StartAsTask
    let rootId = state0.graph.root
    let childId = NodeId.New()

    let change =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change
            [ Op.NewNode(childId, "stamped")
              Op.Replace(rootId, [], [ ChildNode.owner childId ]) ] }

    let! postResult =
        (admittedChanges (host agent1))
            .postEvents ((encodeChangeBatch [ change ]))
        |> Async.StartAsTask

    match postResult with
    | Error e -> Assert.Fail($"postChange: {e}")
    | Ok _ -> ()

    let! state1 = getState agent1 |> Async.StartAsTask
    let stored = state1.graph.nodes.[childId].updateTime
    Assert.True(stored > NodeUpdateTime.missing)

    let agent2 = DbAgent.create connStr
    let! state2 = getState agent2 |> Async.StartAsTask
    let reloaded = state2.graph.nodes.[childId].updateTime
    Assert.Equal(NodeUpdateTime.toDbPrecision stored, reloaded)
}

[<Fact>]
let ``DbAgent change fails and state is unchanged when DB goes away after startup`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let agent = DbAgent.create connStr
    let mailbox = host agent
    let! ready = waitUntil 2000 (fun () -> CoreMailbox.isReady mailbox)
    Assert.True(ready, "Expected startup sweep to complete before closing DB.")
    let! state0 = getStateFrom mailbox |> Async.StartAsTask
    let rootId = state0.graph.root
    let childId = NodeId.New()

    let change =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change
            [ Op.NewNode(childId, "db-down")
              Op.Replace(rootId, [], [ ChildNode.owner childId ]) ] }

    try
        do! setDatabaseAllowConnections connStr false
        let body = encodeChangeBatch [ change ]
        let! postResult =
            (admittedChanges mailbox).postEvents (body)
            |> Async.StartAsTask

        match postResult with
        | Ok _ -> Assert.Fail("Expected postChange to fail while DB rejects connections.")
        | Error err -> Assert.Contains("Database error:", err)

        let! rev = CoreMailbox.getEventId mailbox |> Async.StartAsTask
        let! afterState = getStateFrom mailbox |> Async.StartAsTask
        Assert.Equal(EventId.zero, rev)
        Assert.False(afterState.graph.nodes.ContainsKey childId)
    finally
        setDatabaseAllowConnections connStr true
        |> fun t -> t.GetAwaiter().GetResult()
}

[<Fact>]
let ``rebuildFromDocumentFiles aligns DB with on-disk document`` () = task {
    let connStr = requireDbConnStr ()

    let tempRoot =
        Path.Combine(Path.GetTempPath(), "gambol-rebuild-" + Guid.NewGuid().ToString("N"))

    try
        Directory.CreateDirectory(tempRoot) |> ignore
        Directory.CreateDirectory(Bookkeeping.systemDir tempRoot) |> ignore
        File.WriteAllText(Bookkeeping.metaPath tempRoot, "0")
        File.WriteAllText(EventLogFile.eventsPath tempRoot, "")

        do! resetTestDatabase connStr
        let agent = DbAgent.create connStr
        let childId = NodeId.New()

        let change =
            { id = EventId.zero
              submissionId = Guid.NewGuid()
              authority = Authority "Browser"
              commandName = ""
              body = EventBody.Change
                [ Op.NewNode(childId, "db-only")
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

        let body = encodeChangeBatch [ change ]
        let! postR = (admittedChanges (host agent)).postEvents (body) |> Async.StartAsTask

        match postR with
        | Error e -> Assert.Fail($"postChange: {e}")
        | Ok _ -> ()

        let fileSt = DocumentLoader.loadState tempRoot
        let! dbBefore = Database.loadPersistedState connStr |> Async.AwaitTask

        let differs =
            not (GraphProjection.graphEquals fileSt.graph dbBefore.graph)
            || fileSt.eventId.Value <> dbBefore.eventId.Value

        Assert.True(differs, "expected file and DB to differ before rebuild")

        do! Database.rebuildFromDocumentFiles connStr fileSt |> Async.AwaitTask

        let! dbAfter = Database.loadPersistedState connStr |> Async.AwaitTask

        Assert.True(GraphProjection.graphEquals fileSt.graph dbAfter.graph)
        Assert.Equal(fileSt.eventId.Value, dbAfter.eventId.Value)
    finally
        if Directory.Exists(tempRoot) then
            Directory.Delete(tempRoot, true)
}

[<Fact>]
let ``loadPersistedState preserves node name`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr

    let baseGraph = Graph.create ()
    let trashNode = baseGraph.nodes.[Graph.trashId]
    let expectedName = Filename.create "TRASH"

    let graphWithName =
        baseGraph.nodes
        |> Map.add Graph.trashId { trashNode with name = expectedName }
        |> Graph.fromNodes baseGraph.root

    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do! Database.replaceGraphProjectionWithTx tx graphWithName 0 |> Async.AwaitTask
    tx.Commit()

    let! loaded = Database.loadPersistedState connStr |> Async.AwaitTask
    Assert.Equal(expectedName, loaded.graph.nodes.[Graph.trashId].name)
}

[<Fact>]
let ``loadPersistedState preserves node kind`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr

    let fileId = NodeId.New()
    let g0 = Graph.create ()
    let idx = Graph.fileTreeInsertIndex g0 Graph.rootId

    let graphWithFile =
        let change =
            { id = EventId.zero
              submissionId = Guid.NewGuid()
              authority = Authority "Browser"
              commandName = ""
              body = EventBody.Change
                [ Op.NewSpecialNode(fileId, SpecialKind.File, "file1")
                  ChildListWire.insertAt Graph.rootId g0.nodes.[Graph.rootId].children idx [ ChildNode.owner fileId ] ] }

        match
            applyChange change
                { graph = g0
                  eventId = EventId.zero }
        with
        | ApplyResult.Changed st -> st.graph
        | _ -> failwith "expected Changed"

    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do! Database.replaceGraphProjectionWithTx tx graphWithFile 0 |> Async.AwaitTask
    tx.Commit()

    let! loaded = Database.loadPersistedState connStr |> Async.AwaitTask

    match loaded.graph.nodes.[fileId].kind with
    | Special SpecialKind.File -> ()
    | k -> Assert.Fail(sprintf "expected Special File, got %A" k)
}

/// A held table lock (not an exception) during the DB commit step must not wedge the
/// mailbox forever: the handler should reject within the timeout, and the mailbox
/// must still serve a subsequent GetState/postChange afterwards.
[<Fact>]
let ``DbAgent commit hang is rejected within timeout and mailbox survives`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let agent = DbAgent.create connStr
    let mailbox = host agent
    let! ready = waitUntil 2000 (fun () -> CoreMailbox.isReady mailbox)
    Assert.True(ready, "Expected startup sweep to complete before locking events.")
    let! state0 = getStateFrom mailbox |> Async.StartAsTask
    let rootId = state0.graph.root
    let childId = NodeId.New()

    let change =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change
            [ Op.NewNode(childId, "commit-hang-check")
              Op.Replace(rootId, [], [ ChildNode.owner childId ]) ] }

    use lockConn = Database.getConnection connStr
    do! lockConn.OpenAsync()
    use lockTx = lockConn.BeginTransaction()
    use lockCmd = lockConn.CreateCommand()
    lockCmd.Transaction <- lockTx
    lockCmd.CommandText <- "LOCK TABLE events IN ACCESS EXCLUSIVE MODE"
    let! _ = lockCmd.ExecuteNonQueryAsync()

    let sw = Diagnostics.Stopwatch.StartNew()
    let! postResult =
        (admittedChanges mailbox)
            .postEvents ((encodeChangeBatch [ change ]))
        |> Async.StartAsTask
    sw.Stop()

    lockTx.Rollback()

    match postResult with
    | Ok _ -> Assert.Fail("Expected commit to time out while the table was locked.")
    | Error error -> Assert.Contains("timed out", error)
    Assert.True(
        sw.ElapsedMilliseconds < 15000L,
        $"Expected reject near the timeout bound, took {sw.ElapsedMilliseconds}ms.")

    // let the orphaned background commit finish before reusing the connection pool
    do! Task.Delay(500)

    let! rev = CoreMailbox.getEventId mailbox |> Async.StartAsTask
    let! state = getStateFrom mailbox |> Async.StartAsTask
    Assert.NotNull(state)
    Assert.True(rev.Value >= 0)
}

[<Fact>]
let ``DbAgent postChange live-saves artifacts before ack returns`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let tempRoot = newTempDir ()

    let agent = DbAgent.createWithDataDir connStr tempRoot

    let! state0 = getState agent |> Async.StartAsTask
    let rootId = state0.graph.root
    let childId = NodeId.New()

    let change =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change
            [ Op.NewNode(childId, "live-save-check")
              Op.Replace(rootId, [], [ ChildNode.owner childId ]) ] }

    let! postResult =
        (admittedChanges (host agent))
            .postEvents ((encodeChangeBatch [ change ]))
        |> Async.StartAsTask

    match postResult with
    | Error e -> Assert.Fail($"postChange: {e}")
    | Ok _ -> ()

    let ambPath = Path.Combine(tempRoot, ".amb")
    Assert.True(File.Exists ambPath)
    Assert.Contains("live-save-check", File.ReadAllText ambPath)
}

[<Fact>]
let ``DbAgent missing ROOT fails closed while reads stay available`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use command = conn.CreateCommand()
    command.CommandText <-
        """
        INSERT INTO graph (singleton, root_id, revision)
        VALUES (1, '00000000-0000-0000-0000-000000000000', 4)
        """
    let! _ = command.ExecuteNonQueryAsync()

    let agent = DbAgent.create connStr
    do! Task.Delay(200)
    Assert.False(CoreMailbox.isReady (host agent))

    let change =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.NewNode(NodeId.New(), "blocked") ] }
    let! postResult =
        (admittedChanges (host agent))
            .postEvents ((encodeChangeBatch [ change ]))
        |> Async.StartAsTask
    match postResult with
    | Error error ->
        Assert.Contains("Startup projection sweep failed", error)
    | Ok _ -> Assert.Fail("Expected mutation rejection after missing ROOT.")

    let! state = getState agent |> Async.StartAsTask
    let! revision = CoreMailbox.getEventId (host agent) |> Async.StartAsTask
    Assert.False(CoreMailbox.isReady (host agent))
    Assert.Equal(EventId.fromJson 4, revision)
}

[<Fact>]
let ``DbAgent dual-owned repair reloads ready graph from projection`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let aId = NodeId(Guid.Parse("20000000-0000-0000-0000-000000000110"))
    let uId = NodeId(Guid.Parse("20000000-0000-0000-0000-000000000111"))
    let a = Node.Create(aId, text = "A")
    let u = Node.Create(uId, text = "U", children = [ ChildNode.owner aId ])
    let graph0 = Graph.create ()
    let custom =
        graph0.nodes
        |> Map.add aId a
        |> Map.add uId u
    let ws = custom.[Graph.workspacesId]
    let root = custom.[Graph.rootId]
    let graph =
        custom
        |> Map.add Graph.workspacesId
            { ws with children = ChildNode.owner aId :: ws.children }
        |> Map.add Graph.rootId
            { root with children = ChildNode.owner uId :: root.children }
        |> Graph.fromNodes Graph.rootId

    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do! Database.replaceGraphProjectionWithTx tx graph 6 |> Async.AwaitTask
    tx.Commit()

    let agent = DbAgent.create connStr
    let! ready = waitUntil 2000 (fun () -> CoreMailbox.isReady (host agent))
    Assert.True(ready, "Expected ownership repair to enable normal processing.")
    let! state = getState agent |> Async.StartAsTask
    let readyGraph = state.graph
    let! loaded = Database.tryLoadGraphFromProjection connStr |> Async.AwaitTask
    match loaded with
    | Error e -> Assert.Fail(e)
    | Ok (projected, eventId) ->
        Assert.Equal(EventId.fromJson 6, eventId)
        Assert.True(GraphProjection.graphEquals readyGraph projected)
        Assert.Equal(
            Some Graph.workspacesId,
            Map.tryFind aId projected.ownerParentByChild)
}

