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
    match! CoreMailboxApi.graphAtEvent mailbox with
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
        Graph.addDetachedNode
            (Node.Create(orphanId, text = "orphan"))
            graph0

    { graph = graph
      eventId = EventIdFixtures.storedId 4 },
    orphanId

[<Fact>]
let ``DbAgent empty test DB has revision 0 and canonical ROOT`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let agent = DbAgent.create connStr
    let! rev = CoreMailboxApi.getEventId (host agent) |> Async.StartAsTask
    let! state = getState agent |> Async.StartAsTask
    Assert.Equal(EventId.zero, rev)
    let graph = state.graph
    let rootKids = Graph.children graph graph.root
    Assert.Equal(4, graph.nodes.Count)
    Assert.Equal("ROOT", graph.nodes.[graph.root].text)
    Assert.Equal(3, rootKids.Length)
    Assert.Equal(Graph.workspacesId, rootKids.[0].id)
    Assert.Equal("Workspaces", graph.nodes.[Graph.workspacesId].text)
    Assert.Equal(Graph.systemId, rootKids.[1].id)
    Assert.Equal("System", graph.nodes.[Graph.systemId].text)
    Assert.Equal(Graph.trashId, rootKids.[2].id)
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
        Graph.addDetachedNode orphan graph0

    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do! Database.replaceGraphProjectionWithTx tx graph
            (EventId.toJson (EventIdFixtures.storedId 9))
        |> Async.AwaitTask
    tx.Commit()

    let agent = DbAgent.create connStr
    let! ready = waitUntil 2000 (fun () -> CoreMailboxApi.isReady (host agent))
    Assert.True(ready, "Expected startup sweep to enable normal queue processing.")
    let! state = getState agent |> Async.StartAsTask
    let! revision = CoreMailboxApi.getEventId (host agent) |> Async.StartAsTask
    let loaded = state.graph

    Assert.Equal(EventIdFixtures.storedId 9, revision)
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
    Assert.False(CoreMailboxApi.isReady mailbox)

    let stateTask = getStateFrom mailbox |> Async.StartAsTask
    let revisionTask = CoreMailboxApi.getEventId mailbox |> Async.StartAsTask
    let firstPost =
        CoreDoor.postEvents (admittedChanges mailbox) ((emptyChange ()))
        |> Async.StartAsTask
    let secondPost =
        CoreDoor.postEvents (admittedChanges mailbox) ((emptyChange ()))
        |> Async.StartAsTask
    do! Task.Delay(100)
    Assert.True(stateTask.IsCompleted)
    Assert.True(revisionTask.IsCompleted)
    Assert.False(firstPost.IsCompleted)
    Assert.False(secondPost.IsCompleted)
    let! beforeState = stateTask
    let! beforeRevision = revisionTask
    Assert.False(CoreMailboxApi.isReady mailbox)
    Assert.True(beforeState.graph.nodes.ContainsKey orphanId)
    Assert.Equal(EventIdFixtures.storedId 4, beforeRevision)
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
    Assert.True(CoreMailboxApi.isReady mailbox)
    Assert.False(afterState.graph.nodes.ContainsKey orphanId)
    Assert.True(CoreMailboxApi.isReady mailbox)
}

[<Fact>]
let ``DbAgent startup sweep failure preserves reads and fails mutations closed`` () = task {
    let initialState, orphanId = stateWithDetachedNode ()
    let agent =
        DbAgent.createForTest initialState (fun _ ->
            Error "Startup projection sweep failed: blocked")
    do! Task.Delay(50)
    Assert.False(CoreMailboxApi.isReady (host agent))

    let! postResult =
        CoreDoor.postEvents (admittedChanges (host agent)) ((emptyChange ()))
        |> Async.StartAsTask

    match postResult with
    | Error error -> Assert.Contains("Startup projection sweep failed: blocked", error)
    | Ok _ -> Assert.Fail("Expected mutation rejection after startup sweep failure.")

    let! state = getState agent |> Async.StartAsTask
    let! revision = CoreMailboxApi.getEventId (host agent) |> Async.StartAsTask
    Assert.True(state.graph.nodes.ContainsKey orphanId)
    Assert.Equal(EventIdFixtures.storedId 4, revision)
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
    let! posted = CoreDoor.postEvents (admittedChanges (host agent1)) (body) |> Async.StartAsTask

    match posted with
    | Error e -> Assert.Fail($"postChange: {e}")
    | Ok _ -> ()

    let agent2 = DbAgent.create connStr
    let! rev2 = CoreMailboxApi.getEventId (host agent2) |> Async.StartAsTask
    let! state2 = getState agent2 |> Async.StartAsTask
    Assert.True(EventId.isAccepted rev2)
    let graph2 = state2.graph
    Assert.Equal(Graph.rootId, graph2.root)
    let rootKids = Graph.children graph2 graph2.root
    Assert.Equal(4, rootKids.Length)
    let cid = rootKids.[0].id
    Assert.Equal("reload-check", graph2.nodes.[cid].text)
    Assert.Equal(Graph.workspacesId, rootKids.[1].id)
    Assert.Equal(Graph.systemId, rootKids.[2].id)
    Assert.Equal(Graph.trashId, rootKids.[3].id)
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
        CoreDoor.postEvents
            (admittedChanges (host agent1))
            ((encodeChangeBatch [ change ]))
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
    let! ready = waitUntil 2000 (fun () -> CoreMailboxApi.isReady mailbox)
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
            CoreDoor.postEvents (admittedChanges mailbox) (body)
            |> Async.StartAsTask

        match postResult with
        | Ok _ -> Assert.Fail("Expected postChange to fail while DB rejects connections.")
        | Error err -> Assert.Contains("Database error:", err)

        let! rev = CoreMailboxApi.getEventId mailbox |> Async.StartAsTask
        let! afterState = getStateFrom mailbox |> Async.StartAsTask
        Assert.Equal(EventId.zero, rev)
        Assert.False(afterState.graph.nodes.ContainsKey childId)
    finally
        setDatabaseAllowConnections connStr true
        |> fun t -> t.GetAwaiter().GetResult()
}

/// Waits until ROOT persist notices stop arriving.
let rec private settleRootPersisted (readLog: unit -> Task<Ev list>) (previous: int) (started: DateTime) : Task<unit> = task {
    let! events = readLog ()
    do! Task.Delay(30)
    let! again = readLog ()
    let rootPersisted =
        again
        |> List.exists (fun event ->
            match event.body with
            | EventBody.Notice (Notice.NodePersisted (id, _, _)) ->
                id = Graph.rootId
            | _ -> false)
    let quiet =
        rootPersisted
        && again.Length = events.Length
        && again.Length = previous
    if quiet then
        return ()
    elif DateTime.UtcNow - started > TimeSpan.FromSeconds(2.0) then
        Assert.Fail("ROOT persist notices did not settle")
    else
        return! settleRootPersisted readLog again.Length started
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
        let mailbox = host agent
        let! postR =
            CoreDoor.postEvents (admittedChanges mailbox) body
            |> Async.StartAsTask

        match postR with
        | Error e -> Assert.Fail($"postChange: {e}")
        | Ok _ -> ()

        (DbAgent.persist agent).dispose ()
        let readLog () =
            CoreMailboxApi.getEventsSince mailbox EventId.zero
            |> Async.StartAsTask
        do! settleRootPersisted readLog -1 DateTime.UtcNow

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
        Graph.fromNodes
            baseGraph.root
            (baseGraph.nodes
             |> Map.add Graph.trashId { trashNode with name = expectedName })
            baseGraph.childMap

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
                  ChildListWire.insertAt
                      Graph.rootId
                      (Graph.children g0 Graph.rootId)
                      idx
                      [ ChildNode.owner fileId ] ] }

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
    let! ready = waitUntil 2000 (fun () -> CoreMailboxApi.isReady mailbox)
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
        CoreDoor.postEvents
            (admittedChanges mailbox)
            ((encodeChangeBatch [ change ]))
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

    let! rev = CoreMailboxApi.getEventId mailbox |> Async.StartAsTask
    let! state = getStateFrom mailbox |> Async.StartAsTask
    Assert.NotNull(state)
    Assert.True(rev.Value >= 0)
}

[<Fact>]
let ``DbAgent postChange live-saves artifacts after the persist completes`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let tempRoot = newTempDir ()

    let agent = DbAgent.createWithDataDir connStr tempRoot
    let mailbox = host agent

    let! state0 = getStateFrom mailbox |> Async.StartAsTask
    let rootId = state0.graph.root
    let childId = NodeId.New()
    let! beforeId = CoreMailboxApi.getEventId mailbox |> Async.StartAsTask

    let change =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change
            [ Op.NewNode(childId, "live-save-check")
              Op.Replace(rootId, [], [ ChildNode.owner childId ]) ] }

    let! postResult =
        CoreDoor.postEvents
            (admittedChanges mailbox)
            ((encodeChangeBatch [ change ]))
        |> Async.StartAsTask

    match postResult with
    | Error e -> Assert.Fail($"postChange: {e}")
    | Ok _ -> ()

    let persisted (events: Ev list) =
        events
        |> List.exists (fun event ->
            match event.body with
            | EventBody.Notice (Notice.NodePersisted _) -> true
            | _ -> false)

    let! events =
        waitForNotices
            (fun () ->
                CoreMailboxApi.getEventsSince mailbox beforeId
                |> Async.StartAsTask)
            persisted

    Assert.True(persisted events, "Expected NodePersisted after the live-save write.")

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
        $"""
        INSERT INTO graph (singleton, root_id, revision)
        VALUES (1, '00000000-0000-0000-0000-000000000000',
            {EventId.toJson (EventIdFixtures.storedId 4)})
        """
    let! _ = command.ExecuteNonQueryAsync()

    let agent = DbAgent.create connStr
    do! Task.Delay(200)
    Assert.False(CoreMailboxApi.isReady (host agent))

    let change =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.NewNode(NodeId.New(), "blocked") ] }
    let! postResult =
        CoreDoor.postEvents
            (admittedChanges (host agent))
            ((encodeChangeBatch [ change ]))
        |> Async.StartAsTask
    match postResult with
    | Error error ->
        Assert.Contains("Startup projection sweep failed", error)
    | Ok _ -> Assert.Fail("Expected mutation rejection after missing ROOT.")

    let! state = getState agent |> Async.StartAsTask
    let! revision = CoreMailboxApi.getEventId (host agent) |> Async.StartAsTask
    Assert.False(CoreMailboxApi.isReady (host agent))
    Assert.Equal(EventIdFixtures.storedId 4, revision)
}

[<Fact>]
let ``DbAgent dual-owned repair reloads ready graph from projection`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let aId = NodeId(Guid.Parse("20000000-0000-0000-0000-000000000110"))
    let uId = NodeId(Guid.Parse("20000000-0000-0000-0000-000000000111"))
    let a = Node.Create(aId, text = "A")
    let u = Node.Create(uId, text = "U")
    let graph0 = Graph.create ()
    let graph1 =
        graph0
        |> Graph.addDetachedNode a
        |> Graph.addDetachedNode u
    let graph =
        graph1
        |> fun g ->
            Graph.fromNodes
                g.root
                g.nodes
                (g.childMap
                 |> Map.add uId [ ChildNode.owner aId ]
                 |> Map.add
                    Graph.workspacesId
                    (ChildNode.owner aId
                     :: Graph.children g Graph.workspacesId)
                 |> Map.add
                    Graph.rootId
                    (ChildNode.owner uId :: Graph.children g Graph.rootId))

    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do! Database.replaceGraphProjectionWithTx tx graph
            (EventId.toJson (EventIdFixtures.storedId 6))
        |> Async.AwaitTask
    tx.Commit()

    let agent = DbAgent.create connStr
    let! ready = waitUntil 2000 (fun () -> CoreMailboxApi.isReady (host agent))
    Assert.True(ready, "Expected ownership repair to enable normal processing.")
    let! state = getState agent |> Async.StartAsTask
    let readyGraph = state.graph
    let! loaded = Database.tryLoadGraphFromProjection connStr |> Async.AwaitTask
    match loaded with
    | Error e -> Assert.Fail(e)
    | Ok (projected, eventId) ->
        Assert.Equal(EventIdFixtures.storedId 6, eventId)
        Assert.True(GraphProjection.graphEquals readyGraph projected)
        Assert.Equal(
            Some Graph.workspacesId,
            Map.tryFind aId projected.ownerParentByChild)
}

[<Fact>]
let ``DbAgent startup sweep repair logs a PROJECTION-REPAIR entry`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let tempRoot = newTempDir ()
    let orphanId = NodeId.New()
    let orphan = Node.Create(orphanId, text = "orphan")
    let graph = Graph.addDetachedNode orphan (Graph.create ())

    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do! Database.replaceGraphProjectionWithTx tx graph
            (EventId.toJson (EventIdFixtures.storedId 9))
        |> Async.AwaitTask
    tx.Commit()

    let agent = DbAgent.createWithDataDir connStr tempRoot
    let! ready = waitUntil 2000 (fun () -> CoreMailboxApi.isReady (host agent))
    Assert.True(ready, "Expected startup sweep to enable normal queue processing.")

    let text = File.ReadAllText(ErrorLog.logPath tempRoot)
    Assert.Contains("PROJECTION-REPAIR", text)
    Assert.Contains("deleted=1", text)
    Assert.Contains(string orphanId.Value, text)
}

let private oneLogLine (text: string) =
    text.Replace("\r\n", "\\n").Replace("\n", "\\n")

let private logLines (kind: string) (text: string) =
    text.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.filter (fun line -> line.Contains(kind))

let private decodeFailure (payload: string) =
    match EventLogFile.decodeEvent payload with
    | Error error -> error
    | Ok _ -> failwith "expected EventLogFile.decodeEvent to reject the payload"

let private assertOneSummary (kind: string) (text: string) (parts: string list) =
    let line = Assert.Single(logLines kind text)
    parts |> List.iter (fun part -> Assert.Contains(part, line))

[<Fact>]
let ``loadPersistedState logs the fallback reason`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let dataDir = newTempDir ()
    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use command = conn.CreateCommand()
    command.CommandText <-
        """
        INSERT INTO graph (singleton, root_id, revision)
        VALUES (1, '00000000-0000-0000-0000-000000000000', 0);
        INSERT INTO nodes (id, text, css_classes)
        VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'orphan', '[]');
        """
    let! _ = command.ExecuteNonQueryAsync()

    let! loaded =
        Database.loadPersistedStateFor connStr (Some dataDir)
        |> Async.AwaitTask

    Assert.Equal(EventId.zero, loaded.eventId)
    Assert.True(GraphProjection.graphEquals (Graph.create ()) loaded.graph)
    let text = File.ReadAllText(ErrorLog.logPath dataDir)
    Assert.Contains("LOAD-FALLBACK", text)
    Assert.Contains("graphFromPersistence: root id missing from nodes", text)
}

[<Fact>]
let ``DbAgent logs an EventLogFile.decodeEvent drop`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let dataDir = newTempDir ()
    let eventId = 7
    let payload = "not-json"
    let decodeError = decodeFailure payload
    do! Database.appendEvent connStr eventId (Guid.NewGuid()) payload

    let agent = DbAgent.createWithDataDir connStr dataDir
    try
        let text = File.ReadAllText(ErrorLog.logPath dataDir)
        assertOneSummary
            "EVENT-DECODE-DROP"
            text
            [ "count=1"
              $"firstEventId={eventId}"
              $"lastEventId={eventId}"
              oneLogLine decodeError ]
    finally
        (DbAgent.persist agent).dispose ()
}

[<Fact>]
let ``DbAgent logs one summary for many EventLogFile.decodeEvent drops`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let dataDir = newTempDir ()
    let firstId = 7
    let lastId = 9
    let firstPayload =
        """{"eventId":7,"submissionId":"00000000-0000-0000-0000-000000000007","authority":"Browser","commandName":"Edit","body":{"kind":"change"}}"""
    let lastPayload = "not-json"
    let firstError = decodeFailure firstPayload
    let lastError = decodeFailure lastPayload
    do! Database.appendEvent connStr firstId (Guid.NewGuid()) firstPayload
    do! Database.appendEvent connStr lastId (Guid.NewGuid()) lastPayload

    let agent = DbAgent.createWithDataDir connStr dataDir
    try
        let text = File.ReadAllText(ErrorLog.logPath dataDir)
        assertOneSummary
            "EVENT-DECODE-DROP"
            text
            [ "count=2"
              $"firstEventId={firstId}"
              $"lastEventId={lastId}"
              oneLogLine firstError ]
        let line = Assert.Single(logLines "EVENT-DECODE-DROP" text)
        Assert.DoesNotContain(oneLogLine lastError, line)
        let! revision =
            CoreMailboxApi.getEventId (host agent) |> Async.StartAsTask
        Assert.Equal(EventId.zero, revision)
    finally
        (DbAgent.persist agent).dispose ()
}

[<Fact>]
let ``DbAgent logs an ApplyResult.Invalid skip from EventLog.recover`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let dataDir = newTempDir ()
    let eventId = 4
    let reason = "cannot NewNode with canonical root id"
    let event =
        { id = EventId.fromJson eventId
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = "Edit"
          body = EventBody.Change [ Op.NewNode(Graph.rootId, "nope") ] }
    do! Database.appendEvent
            connStr
            eventId
            event.submissionId
            (EventLogFile.encodeEvent event)

    let agent = DbAgent.createWithDataDir connStr dataDir
    try
        let text = File.ReadAllText(ErrorLog.logPath dataDir)
        assertOneSummary
            "RECOVER-INVALID"
            text
            [ "count=1"
              $"firstEventId={eventId}"
              $"lastEventId={eventId}"
              reason ]
        let! revision =
            CoreMailboxApi.getEventId (host agent) |> Async.StartAsTask
        Assert.Equal(EventId.fromJson eventId, revision)
    finally
        (DbAgent.persist agent).dispose ()
}

let private startupWindow = 1024
let private keptFloorId = 1000
let private keptTipId = keptFloorId + startupWindow - 1
let private olderEventIds = [ 1; 2; 3 ]
let private keptText = "window-kept"
let private keptNodeId =
    NodeId(Guid.Parse "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")

let private changeEvent (eventId: int) (ops: Op list) =
    { id = EventId.fromJson eventId
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = "Edit"
      body = EventBody.Change ops }

let private encodedRow (event: Ev) =
    EventId.toJson event.id, event.submissionId, EventLogFile.encodeEvent event

let private insertEventRows
    (connStr: string)
    (rows: (int * Guid * string) list)
    =
    task {
        use conn = Database.getConnection connStr
        do! conn.OpenAsync()
        use tx = conn.BeginTransaction()
        use cmd = conn.CreateCommand()
        cmd.Transaction <- tx
        cmd.CommandText <-
            "INSERT INTO events (event_id, submission_id, payload) "
            + "VALUES (@event_id, @submission_id, @payload)"
        let idParam = cmd.Parameters.AddWithValue("event_id", 0)
        let submissionParam =
            cmd.Parameters.AddWithValue("submission_id", Guid.Empty)
        let payloadParam = cmd.Parameters.AddWithValue("payload", "")
        for eventId, submissionId, payload in rows do
            idParam.Value <- box eventId
            submissionParam.Value <- box submissionId
            payloadParam.Value <- box payload
            let! _ = cmd.ExecuteNonQueryAsync()
            ()
        do! tx.CommitAsync()
    }

let private keptRows (tipOps: Op list) =
    let older =
        olderEventIds
        |> List.map (fun eventId -> eventId, Guid.NewGuid(), "not-json")
    let kept =
        [ keptFloorId .. keptTipId - 1 ]
        |> List.map (fun eventId -> encodedRow (changeEvent eventId []))
    let tip = encodedRow (changeEvent keptTipId tipOps)
    older @ kept @ [ tip ]

let private fillWindow (connStr: string) (tipOps: Op list) =
    insertEventRows connStr (keptRows tipOps)

let private keptNodeOp () =
    Op.NewNode(keptNodeId, keptText)

let private saveProjection (connStr: string) (revision: int) = task {
    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use tx = conn.BeginTransaction()
    do!
        Database.replaceGraphProjectionWithTx tx (Graph.create ()) revision
        |> Async.AwaitTask
    tx.Commit()
}

let private withAgent
    (connStr: string)
    (dataDir: string option)
    (body: MailboxHost -> Task)
    =
    task {
        let agent =
            match dataDir with
            | None -> DbAgent.create connStr
            | Some dir -> DbAgent.createWithDataDir connStr dir
        let mailbox = host agent
        try
            do! body mailbox
        finally
            CoreMailboxApi.dispose mailbox
    }

let private whenReady (mailbox: MailboxHost) = task {
    let! ready = waitUntil 2000 (fun () -> CoreMailboxApi.isReady mailbox)
    Assert.True(ready, "Expected startup to become ready.")
}

let private keptNodeText (graph: Graph) =
    match Map.tryFind keptNodeId graph.nodes with
    | Some node -> Some node.text
    | None -> None

[<Fact>]
let ``startup load keeps the newest 1024 events newest-first`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    do! fillWindow connStr []
    do! withAgent connStr None (fun mailbox -> task {
        do! whenReady mailbox
        let! events =
            CoreMailboxApi.getEventsSince mailbox EventId.zero
            |> Async.StartAsTask
        let ids = events |> List.map (fun event -> EventId.toJson event.id)
        Assert.Equal(startupWindow, ids.Length)
        Assert.Equal<int list>([ keptTipId .. -1 .. keptFloorId ], ids)
        let! history =
            CoreMailboxApi.eventHistory mailbox |> Async.StartAsTask
        Assert.Equal(EventId.fromJson (keptTipId + 1), history.nextId)
        let! eventId =
            CoreMailboxApi.getEventId mailbox |> Async.StartAsTask
        Assert.Equal(EventId.zero, eventId)
    })
}

[<Fact>]
let ``startup load leaves event rows older than the window in the table`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    do! fillWindow connStr []
    do! withAgent connStr None (fun mailbox -> task {
        do! whenReady mailbox
        let! stored = Database.getEvents connStr
        let ids = stored |> List.map (fun row -> row.event_id)
        Assert.Equal<int list>(
            olderEventIds @ [ keptFloorId .. keptTipId ],
            ids)
    })
}

[<Fact>]
let ``startup recover applies a kept event after a checkpoint inside the window`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let checkpoint = 1500
    do! fillWindow connStr [ keptNodeOp () ]
    do! saveProjection connStr checkpoint
    do! withAgent connStr None (fun mailbox -> task {
        do! whenReady mailbox
        let! state = getStateFrom mailbox |> Async.StartAsTask
        Assert.Equal(Some keptText, keptNodeText state.graph)
        let! eventId =
            CoreMailboxApi.getEventId mailbox |> Async.StartAsTask
        Assert.Equal(EventId.fromJson keptTipId, eventId)
    })
}

[<Fact>]
let ``startup leaves a projection older than the window as loaded`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let checkpoint = 5
    do! fillWindow connStr [ keptNodeOp () ]
    do! saveProjection connStr checkpoint
    do! withAgent connStr None (fun mailbox -> task {
        do! whenReady mailbox
        let! state = getStateFrom mailbox |> Async.StartAsTask
        Assert.Equal(None, keptNodeText state.graph)
        Assert.Equal(EventId.fromJson checkpoint, state.eventId)
        let! events =
            CoreMailboxApi.getEventsSince mailbox EventId.zero
            |> Async.StartAsTask
        Assert.Equal(startupWindow, events.Length)
        let! history =
            CoreMailboxApi.eventHistory mailbox |> Async.StartAsTask
        Assert.Equal(EventId.fromJson (keptTipId + 1), history.nextId)
    })
}

[<Fact>]
let ``startup recover applies a full table of exactly 1024 events`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let tip = startupWindow
    let rows =
        [ 1 .. tip - 1 ]
        |> List.map (fun eventId -> encodedRow (changeEvent eventId []))
    let tipRow = encodedRow (changeEvent tip [ keptNodeOp () ])
    do! insertEventRows connStr (rows @ [ tipRow ])
    do! withAgent connStr None (fun mailbox -> task {
        do! whenReady mailbox
        let! state = getStateFrom mailbox |> Async.StartAsTask
        Assert.Equal(Some keptText, keptNodeText state.graph)
        Assert.Equal(EventId.fromJson tip, state.eventId)
        let! events =
            CoreMailboxApi.getEventsSince mailbox EventId.zero
            |> Async.StartAsTask
        Assert.Equal(startupWindow, events.Length)
    })
}

[<Fact>]
let ``startup leaves a LOAD-FALLBACK graph and still logs the fallback`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let dataDir = newTempDir ()
    use conn = Database.getConnection connStr
    do! conn.OpenAsync()
    use command = conn.CreateCommand()
    command.CommandText <-
        """
        INSERT INTO graph (singleton, root_id, revision)
        VALUES (1, '00000000-0000-0000-0000-000000000000', 0);
        INSERT INTO nodes (id, text, css_classes)
        VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'orphan', '[]');
        """
    let! _ = command.ExecuteNonQueryAsync()
    do! fillWindow connStr [ keptNodeOp () ]
    do! withAgent connStr (Some dataDir) (fun mailbox -> task {
        let! _ = waitUntil 2000 (fun () -> CoreMailboxApi.isReady mailbox)
        let! state = getStateFrom mailbox |> Async.StartAsTask
        Assert.Equal(EventId.zero, state.eventId)
        Assert.True(GraphProjection.graphEquals (Graph.create ()) state.graph)
        let text = File.ReadAllText(ErrorLog.logPath dataDir)
        Assert.Contains("LOAD-FALLBACK", text)
        let! events =
            CoreMailboxApi.getEventsSince mailbox EventId.zero
            |> Async.StartAsTask
        Assert.Equal(startupWindow, events.Length)
    })
}
