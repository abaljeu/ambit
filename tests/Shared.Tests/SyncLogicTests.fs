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
      pendingChanges = []
      syncState = Synced }

let private modelWithPending (graph: Graph) (pending: Change list) (syncState: SyncState) : VM =
    let m = emptyModel graph
    { m with pendingChanges = pending; syncState = syncState }

let private mkChange id = { id = id; changeId = System.Guid.NewGuid(); ops = [] }

// ---------------------------------------------------------------------------
// shouldReportStale
// ---------------------------------------------------------------------------

[<Fact>]
let ``shouldReportStale with pending returns false - server ahead but our in-flight change`` () =
    let poll =
        { revision = 6
          buildEpochSec = 1
          pageBuildEpochSec = 1 }
    let client =
        { ClientPollContext.revision = 5
          pendingCount = 1
          buildEpochSec = 1
          pageBuildEpochSec = 1 }
    Assert.False(SyncLogic.shouldReportStale poll client)

[<Fact>]
let ``shouldReportStale without pending and server ahead returns true`` () =
    let poll =
        { revision = 6
          buildEpochSec = 1
          pageBuildEpochSec = 1 }
    let client =
        { ClientPollContext.revision = 5
          pendingCount = 0
          buildEpochSec = 1
          pageBuildEpochSec = 1 }
    Assert.True(SyncLogic.shouldReportStale poll client)

[<Fact>]
let ``shouldReportStale when clientBuild 0 and clientPage 0 skips serverNewer - no false Stale`` () =
    let poll =
        { revision = 5
          buildEpochSec = 99
          pageBuildEpochSec = 99 }
    let client =
        { ClientPollContext.revision = 5
          pendingCount = 0
          buildEpochSec = 0
          pageBuildEpochSec = 0 }
    Assert.False(SyncLogic.shouldReportStale poll client)

[<Fact>]
let ``shouldReportStale when build/page differ and client stamps non-zero returns true`` () =
    let poll =
        { revision = 5
          buildEpochSec = 2
          pageBuildEpochSec = 2 }
    let client =
        { ClientPollContext.revision = 5
          pendingCount = 0
          buildEpochSec = 1
          pageBuildEpochSec = 1 }
    Assert.True(SyncLogic.shouldReportStale poll client)

// ---------------------------------------------------------------------------
// applySubmitRejected
// ---------------------------------------------------------------------------

[<Fact>]
let ``applySubmitRejected with empty pending returns model unchanged`` () =
    let graph = ModelBuilder.createDag12 ()
    let model = emptyModel graph
    let result = SyncLogic.applySubmitRejected model
    Assert.Same(model, result)
    Assert.Equal(Synced, result.syncState)

[<Fact>]
let ``applySubmitNoResponse with Syncing 1 and non-empty pending transitions to Syncing 2`` () =
    let graph = ModelBuilder.createDag12 ()
    let pending = [ mkChange 0 ]
    let model = modelWithPending graph pending (Syncing 1)
    let result = SyncLogic.applySubmitNoResponse model
    Assert.Equal(Syncing 2, result.syncState)
    Assert.Equal(1, result.pendingChanges.Length)

[<Fact>]
let ``applySubmitNoResponse with Syncing 9 and non-empty pending transitions to Syncing 10`` () =
    let graph = ModelBuilder.createDag12 ()
    let pending = [ mkChange 0 ]
    let model = modelWithPending graph pending (Syncing 9)
    let result = SyncLogic.applySubmitNoResponse model
    Assert.Equal(Syncing 10, result.syncState)
    Assert.Equal(1, result.pendingChanges.Length)

[<Fact>]
let ``applySubmitNoResponse with Syncing 10 and non-empty pending transitions to Pending 10`` () =
    let graph = ModelBuilder.createDag12 ()
    let pending = [ mkChange 0 ]
    let model = modelWithPending graph pending (Syncing 10)
    let result = SyncLogic.applySubmitNoResponse model
    Assert.Equal(Pending 10, result.syncState)
    Assert.Equal(1, result.pendingChanges.Length)

[<Fact>]
let ``applySubmitRejected with Syncing 1 and non-empty pending transitions to Stale`` () =
    let graph = ModelBuilder.createDag12 ()
    let pending = [ mkChange 0 ]
    let model = modelWithPending graph pending (Syncing 1)
    let result = SyncLogic.applySubmitRejected model
    Assert.Equal(Stale, result.syncState)
    Assert.Equal(1, result.pendingChanges.Length)

[<Fact>]
let ``applySubmitRejected when Stale returns model unchanged`` () =
    let graph = ModelBuilder.createDag12 ()
    let model = { emptyModel graph with syncState = Stale }
    let result = SyncLogic.applySubmitRejected model
    Assert.Same(model, result)
    Assert.Equal(Stale, result.syncState)

// ---------------------------------------------------------------------------
// applyServerAhead
// ---------------------------------------------------------------------------

[<Fact>]
let ``applyServerAhead transitions syncState to Stale`` () =
    let graph = ModelBuilder.createDag12 ()
    let model = emptyModel graph
    let result = SyncLogic.applyServerAhead (Revision 10) model
    Assert.Equal(Stale, result.syncState)
    Assert.Equal(model.graph, result.graph)
