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
      zoomRoot = None
      clipboard = None
      syncInfo = SyncInfo.initial }

let private mkChange id = { id = id; changeId = System.Guid.NewGuid(); ops = [] }

let private mkContext build page =
    { ClientPollContext.buildEpochSec = build
      pageBuildEpochSec = page }

let private mkPoll rev build page =
    { revision = rev; buildEpochSec = build; pageBuildEpochSec = page }

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
