module SyncLogicTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

// ---------------------------------------------------------------------------
// Test helpers
// ---------------------------------------------------------------------------

let private emptyModel (graph: Graph) : VM =
    let siteMap, nextId = buildSiteMap graph

    { graph = graph
      revision = Revision.Zero
      history = History.empty
      selectedNodes = None
      mode = Selecting
      siteMap = siteMap
      nextSiteId = nextId
      zoomRoot = graph.root
      clipboard = None
      syncInfo = SyncInfo.initial
      lastSuccessfulKey = ""
      lastSuccessfulOp = "" }

let private mkChange id = { id = id; changeId = System.Guid.NewGuid(); ops = [] }

let private mkContext build page =
    { ClientPollContext.buildEpochSec = build
      pageBuildEpochSec = page }

let private mkPoll rev build page =
    { revision = rev; buildEpochSec = build; pageBuildEpochSec = page; changes = [] }

// ---------------------------------------------------------------------------
// getPollOutcome — data outdated
// ---------------------------------------------------------------------------

[<Fact>]
let ``getPollOutcome returns DataOutdated when server revision is ahead`` () =
    let poll = mkPoll 6 1 1
    let ctx = mkContext 1 1
    Assert.Equal(Some DataOutdated, SyncLogic.getPollOutcome poll 5 ctx)

[<Fact>]
let ``getPollOutcome returns None when server revision equals client`` () =
    let poll = mkPoll 5 1 1
    let ctx = mkContext 1 1
    Assert.Equal(None, SyncLogic.getPollOutcome poll 5 ctx)

// ---------------------------------------------------------------------------
// getPollOutcome — code outdated
// ---------------------------------------------------------------------------

[<Fact>]
let ``getPollOutcome returns CodeOutdated when build stamps differ and client stamps non-zero`` () =
    let poll = mkPoll 5 2 2
    let ctx = mkContext 1 1
    Assert.Equal(Some CodeOutdated, SyncLogic.getPollOutcome poll 5 ctx)

[<Fact>]
let ``getPollOutcome returns None when client build stamp is 0 (stamps not yet injected)`` () =
    let poll = mkPoll 5 99 99
    let ctx = mkContext 0 0
    Assert.Equal(None, SyncLogic.getPollOutcome poll 5 ctx)

[<Fact>]
let ``getPollOutcome returns None when client page stamp is 0`` () =
    let poll = mkPoll 5 99 99
    let ctx = mkContext 99 0
    Assert.Equal(None, SyncLogic.getPollOutcome poll 5 ctx)

// ---------------------------------------------------------------------------
// getPollOutcome — priority: CodeOutdated beats DataOutdated
// ---------------------------------------------------------------------------

[<Fact>]
let ``getPollOutcome returns CodeOutdated when both code and data are outdated`` () =
    let poll = mkPoll 6 2 2
    let ctx = mkContext 1 1
    Assert.Equal(Some CodeOutdated, SyncLogic.getPollOutcome poll 5 ctx)

// ---------------------------------------------------------------------------
// SyncInfo helpers
// ---------------------------------------------------------------------------

[<Fact>]
let ``SyncInfo withPendingChanges replaces pending list`` () =
    let pending = [ mkChange 0 ]
    let si = SyncInfo.initial
    let si2 = SyncInfo.withPendingChanges pending si
    Assert.Equal(1, si2.pendingChanges.Length)

[<Fact>]
let ``SyncInfo withSyncState clears ack when entering risk state`` () =
    let si = { SyncInfo.initial with syncRiskAcknowledged = true }
    let si2 = SyncInfo.withSyncState ServerRejected si
    Assert.False(si2.syncRiskAcknowledged)

[<Fact>]
let ``SyncInfo withSyncState keeps ack within risk states`` () =
    let si = { SyncInfo.initial with syncState = ServerRejected; syncRiskAcknowledged = true }
    let si2 = SyncInfo.withSyncState DataOutdated si
    Assert.True(si2.syncRiskAcknowledged)

[<Fact>]
let ``SyncInfo withSyncState clears ack when leaving risk state`` () =
    let si = { SyncInfo.initial with syncState = CodeOutdated; syncRiskAcknowledged = true }
    let si2 = SyncInfo.withSyncState Idle si
    Assert.False(si2.syncRiskAcknowledged)

[<Fact>]
let ``SyncInfo withSyncState keeps ack within non-risk states`` () =
    let si = { SyncInfo.initial with syncState = Sending 1; syncRiskAcknowledged = true }
    let si2 = SyncInfo.withSyncState (WaitingToRetry 1) si
    Assert.True(si2.syncRiskAcknowledged)

// ---------------------------------------------------------------------------
// applyServerTail
// ---------------------------------------------------------------------------

let private emptyState () : State =
    { graph = Graph.create ()
      history = History.empty
      revision = Revision 5 }

let private stateWithNode text : State * NodeId =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode text graph0
    { emptyState() with graph = graph1; revision = Revision 3 }, nodeId

[<Fact>]
let ``applyServerTail empty list returns Ok with state unchanged`` () =
    let st = emptyState ()
    match SyncLogic.applyServerTail [] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(st.revision, result.revision)
        Assert.Equal(st.graph.root, result.graph.root)

[<Fact>]
let ``applyServerTail advances revision by one per change`` () =
    let st = emptyState ()
    let changes = [ mkChange 5; mkChange 6; mkChange 7 ]
    match SyncLogic.applyServerTail changes st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result -> Assert.Equal(Revision 8, result.revision)

[<Fact>]
let ``applyServerTail applies graph mutations`` () =
    let st, nodeId = stateWithNode "before"
    let change =
        { id = 3
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(nodeId, "before", "after") ] }
    match SyncLogic.applyServerTail [ change ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal("after", result.graph.nodes.[nodeId].text)
        Assert.Equal(Revision 4, result.revision)

[<Fact>]
let ``applyServerTail returns Error on first invalid change`` () =
    let st = emptyState ()
    let badChange =
        { id = 5
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(NodeId.New(), "old", "new") ] }
    let goodChange = mkChange 6
    match SyncLogic.applyServerTail [ badChange; goodChange ] st with
    | Ok _ -> failwith "Expected Error but got Ok"
    | Error _ -> ()

[<Fact>]
let ``applyServerTail short-circuits: state unchanged after invalid change`` () =
    let st, nodeId = stateWithNode "original"
    let badChange =
        { id = 3
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(NodeId.New(), "x", "y") ] }
    let goodChange =
        { id = 4
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(nodeId, "original", "modified") ] }
    match SyncLogic.applyServerTail [ badChange; goodChange ] st with
    | Ok _ -> failwith "Expected Error but got Ok"
    | Error _ ->
        Assert.Equal("original", st.graph.nodes.[nodeId].text)
