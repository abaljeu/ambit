module SyncLogicTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

// ---------------------------------------------------------------------------
// Test helpers
// ---------------------------------------------------------------------------

let private emptyModel = VmTestHelpers.emptyModel

let private mkChange id = { id = id; changeId = System.Guid.NewGuid(); ops = [] }

let private mkContext build page =
    { ClientPollContext.buildEpochSec = build
      pageBuildEpochSec = page }

let private mkPoll rev build page : ChangeSuccessResponse =
    { revision = Revision rev
      buildEpochSec = build
      pageBuildEpochSec = page
      isReady = true
      externalChanges = false
      changes = []
      message = None
      bootstrapHash = None }

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
let ``getPollOutcome sends an existing page through CodeOutdated after server restart`` () =
    let pageProcessStart = 1_700_000_000
    let restartedProcessStart = pageProcessStart + 1
    let pageBuild = 1_699_999_000
    let poll = mkPoll 5 restartedProcessStart pageBuild
    let context = mkContext pageProcessStart pageBuild
    Assert.Equal(
        Some CodeOutdated,
        SyncLogic.getPollOutcome poll 5 context)

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
let ``SyncInfo readiness follows state and poll responses`` () =
    let starting = SyncInfo.initial
    let ready = starting |> SyncInfo.withServerReady true
    let startingAgain = ready |> SyncInfo.withServerReady false

    Assert.False(starting.isServerReady)
    Assert.True(ready.isServerReady)
    Assert.False(startingAgain.isServerReady)

[<Fact>]
let ``SyncInfo withPendingChanges replaces pending list`` () =
    let pending = [ PendingChange.ofChange (mkChange 0) ]
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
    let si2 = SyncInfo.withSyncState (WaitingToRetry (1, 0, [])) si
    Assert.True(si2.syncRiskAcknowledged)

// ---------------------------------------------------------------------------
// applyServerTail
// ---------------------------------------------------------------------------

let private emptyState () : ClientSyncState =
    { graph = Graph.create ()
      history = ClientHistory.clear ()
      revision = Revision 5 }

let private ofState (st: State) : ClientSyncState =
    { graph = st.graph
      revision = st.revision
      history = ClientHistory.clear () }

let private withRecorded (change: Change) (state: ClientSyncState) =
    let history, _ = ClientHistory.record "test" change state.history
    { state with history = history }

let private stateWithNode text : ClientSyncState * NodeId =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode text graph0
    { emptyState() with graph = graph1; revision = Revision 3 }, nodeId

[<Fact>]
let ``applyServerTail empty list returns Ok with state unchanged`` () =
    let past = mkChange 4
    let st = emptyState () |> withRecorded past
    match SyncLogic.applyServerTail [] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(st.revision, result.revision)
        Assert.Equal(st.graph.root, result.graph.root)
        Assert.Equal(st.history, result.history)

[<Fact>]
let ``applyServerTail non-empty tail preserves History`` () =
    let st, nodeId = stateWithNode "before"
    let local = mkChange 2
    let withHistory = st |> withRecorded local
    let upstream =
        { id = 3
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(nodeId, "before", "after") ] }
    match SyncLogic.applyServerTail [ upstream ] withHistory with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(withHistory.history, result.history)
        Assert.Equal("after", result.graph.nodes.[nodeId].text)
        Assert.Equal(Revision 4, result.revision)
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
let ``applyServerTail carries SetUpdateTime after SetText as poll stamp path`` () =
    let st, nodeId = stateWithNode "before"
    let stamp = System.DateTime(2026, 7, 22, 18, 0, 0, System.DateTimeKind.Utc)
    let change =
        { id = 3
          changeId = System.Guid.NewGuid()
          ops =
              [ Op.SetText(nodeId, "before", "after")
                Op.SetUpdateTime(nodeId, NodeUpdateTime.missing, stamp) ] }
    match SyncLogic.applyServerTail [ change ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal("after", result.graph.nodes.[nodeId].text)
        Assert.Equal(
            NodeUpdateTime.toDbPrecision stamp,
            result.graph.nodes.[nodeId].updateTime)

[<Fact>]
let ``applyServerTail returns Error on first invalid change`` () =
    let st, nodeId = stateWithNode "original"
    let badChange =
        { id = 5
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(nodeId, "wrong-old", "new") ] }
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
          ops = [ Op.SetText(nodeId, "wrong-old", "y") ] }
    let goodChange =
        { id = 4
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(nodeId, "original", "modified") ] }
    match SyncLogic.applyServerTail [ badChange; goodChange ] st with
    | Ok _ -> failwith "Expected Error but got Ok"
    | Error _ ->
        Assert.Equal("original", st.graph.nodes.[nodeId].text)

[<Fact>]
let ``applyServerTail consumes Change on Absent Header without graph effect`` () =
    let st = emptyState ()
    let absentId = NodeId.New()
    let change =
        { id = 5
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(absentId, "old", "new") ] }
    match SyncLogic.applyServerTail [ change ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(Revision 6, result.revision)
        Assert.False(result.graph.nodes.ContainsKey absentId)
        Assert.Equal(ClientHistory.clear (), result.history)

[<Fact>]
let ``applyServerTail skips structural Replace on Unloaded parent`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let childId = NodeId.New()
    let ws =
        Node.Create(
            wsId,
            text = "ws",
            name = Filename.Ok "ws",
            kind = Special Workspace,
            childrenStatus = Unloaded,
            owner = Graph.workspacesId)
    let child =
        Node.Create(childId, text = "child", owner = wsId)
    let root = graph0.nodes.[graph0.root]
    let workspaces = graph0.nodes.[Graph.workspacesId]
    let nodes =
        graph0.nodes
        |> Map.add wsId ws
        |> Map.add childId child
        |> Map.add
            Graph.workspacesId
            { workspaces with
                children =
                    workspaces.children
                    @ [ ChildNode.owner wsId ] }
    let graph = Graph.fromNodes graph0.root nodes
    let st: ClientSyncState =
        { graph = graph
          history = ClientHistory.clear ()
          revision = Revision 3 }
    let change =
        { id = 4
          changeId = System.Guid.NewGuid()
          ops =
              [ Op.Replace(wsId, [], [ ChildNode.owner childId ]) ] }
    match SyncLogic.applyServerTail [ change ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(Revision 4, result.revision)
        Assert.Equal(Unloaded, result.graph.nodes.[wsId].childrenStatus)
        Assert.Equal<ChildNode list>([], result.graph.nodes.[wsId].children)

[<Fact>]
let ``applyServerTail applies header facts on Unloaded resident Node`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let ws =
        Node.Create(
            wsId,
            text = "before",
            name = Filename.Ok "ws",
            kind = Special Workspace,
            childrenStatus = Unloaded,
            owner = Graph.workspacesId)
    let workspaces = graph0.nodes.[Graph.workspacesId]
    let nodes =
        graph0.nodes
        |> Map.add wsId ws
        |> Map.add
            Graph.workspacesId
            { workspaces with
                children =
                    workspaces.children
                    @ [ ChildNode.owner wsId ] }
    let st: ClientSyncState =
        { graph = Graph.fromNodes graph0.root nodes
          history = ClientHistory.clear ()
          revision = Revision 2 }
    let change =
        { id = 3
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(wsId, "before", "after") ] }
    match SyncLogic.applyServerTail [ change ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal("after", result.graph.nodes.[wsId].text)
        Assert.Equal(Unloaded, result.graph.nodes.[wsId].childrenStatus)

[<Fact>]
let ``applySyncResponse installs complete child list as Loaded and preserves owner`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let childId = NodeId.New()
    let ownerWs = NodeId.New()
    let markerId = NodeId.New()
    let wsHeader =
        Node.Create(
            wsId,
            text = "ws",
            name = Filename.Ok "ws",
            kind = Special Workspace,
            childrenStatus = Unloaded,
            owner = Graph.workspacesId)
    let marker =
        Node.Create(markerId, text = "marker", owner = Graph.rootId)
    let workspaces = graph0.nodes.[Graph.workspacesId]
    let root = graph0.nodes.[graph0.root]
    let nodes0 =
        graph0.nodes
        |> Map.add wsId wsHeader
        |> Map.add markerId marker
        |> Map.add
            Graph.workspacesId
            { workspaces with
                children =
                    workspaces.children
                    @ [ ChildNode.owner wsId ] }
        |> Map.add
            graph0.root
            { root with
                children =
                    root.children
                    @ [ ChildNode.owner markerId ] }
    let st: ClientSyncState =
        { graph = Graph.fromNodes graph0.root nodes0
          history =
            ClientHistory.record "test" (mkChange 1) (ClientHistory.clear ())
            |> fst
          revision = Revision 5 }
    let child =
        Node.Create(childId, text = "leaf", owner = wsId)
    let loadedWs =
        { wsHeader with
            children = [ ChildNode.owner childId ]
            childrenStatus = Loaded }
    // External resident header whose owner edge lives only in an Unloaded list.
    let external =
        Node.Create(
            ownerWs,
            text = "ext",
            name = Filename.Ok "ext",
            kind = Special Workspace,
            childrenStatus = Unloaded,
            owner = wsId)
    // Change touches a Loaded root child; package then installs ws at response revision.
    let response =
        { SyncResponse.changes =
              [ { id = 6
                  changeId = System.Guid.NewGuid()
                  ops = [ Op.SetText(markerId, "marker", "marker-tail") ] } ]
          packages = [ loadedWs; child; external ] }
    match SyncLogic.applySyncResponse response st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(st.history, result.history)
        Assert.Equal(Revision 6, result.revision)
        Assert.Equal(Loaded, result.graph.nodes.[wsId].childrenStatus)
        Assert.Equal(1, result.graph.nodes.[wsId].children.Length)
        Assert.Equal("marker-tail", result.graph.nodes.[markerId].text)
        Assert.Equal(wsId, result.graph.nodes.[external.id].owner)
        Assert.Equal(Some wsId, result.graph.ownerParentByChild |> Map.tryFind childId)
        Assert.Equal(None, result.graph.ownerParentByChild |> Map.tryFind ownerWs)

[<Fact>]
let ``applyServerTail multi-change tail advances revision and graph`` () =
    let state0 =
        { ModelBuilder.createState12 () with revision = Revision 10 }
    let root = state0.graph.nodes.[state0.graph.root]
    let nodeA = state0.graph.nodes.[root.children.[0].id]
    let nodeB = state0.graph.nodes.[root.children.[1].id]
    let change1 =
        { id = 1
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(nodeA.id, nodeA.text, nodeA.text + "1") ] }
    let change2 =
        { id = 2
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(nodeB.id, nodeB.text, nodeB.text + "2") ] }
    match SyncLogic.applyServerTail [ change1; change2 ] (ofState state0) with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(Revision 12, result.revision)
        Assert.Equal(nodeA.text + "1", result.graph.nodes.[nodeA.id].text)
        Assert.Equal(nodeB.text + "2", result.graph.nodes.[nodeB.id].text)

[<Fact>]
let ``applySyncResponse empty packages and empty changes preserves History`` () =
    let past = mkChange 4
    let st = emptyState () |> withRecorded past
    match SyncLogic.applySyncResponse { changes = []; packages = [] } st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(st.history, result.history)
        Assert.Equal(st.revision, result.revision)

[<Fact>]
let ``applySyncResponse empty Loaded child list marks Loaded without History clear`` () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let ws =
        Node.Create(
            wsId,
            text = "empty-ws",
            name = Filename.Ok "empty-ws",
            kind = Special Workspace,
            childrenStatus = Unloaded,
            owner = Graph.workspacesId)
    let workspaces = graph0.nodes.[Graph.workspacesId]
    let nodes =
        graph0.nodes
        |> Map.add wsId ws
        |> Map.add
            Graph.workspacesId
            { workspaces with
                children =
                    workspaces.children
                    @ [ ChildNode.owner wsId ] }
    let past = mkChange 2
    let st: ClientSyncState =
        { graph = Graph.fromNodes graph0.root nodes
          history =
            ClientHistory.record "test" past (ClientHistory.clear ()) |> fst
          revision = Revision 4 }
    let loadedEmpty = { ws with children = []; childrenStatus = Loaded }
    match
        SyncLogic.applySyncResponse
            { changes = []; packages = [ loadedEmpty ] }
            st
    with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(st.history, result.history)
        Assert.Equal(Revision 4, result.revision)
        Assert.Equal(Loaded, result.graph.nodes.[wsId].childrenStatus)
        Assert.Equal<ChildNode list>([], result.graph.nodes.[wsId].children)

[<Fact>]
let ``applyServerTail trusts server tails without ownership re-check`` () =
    // Server-accepted tails are trusted: poll apply must not reject on ownership.
    let state0 = ModelBuilder.createState12 ()
    let rootId = state0.graph.root
    let root = state0.graph.nodes.[rootId]
    let childA = root.children.[0]
    let nodeA = state0.graph.nodes.[childA.id]
    let childB = nodeA.children.[0]
    let originalBChildren = state0.graph.nodes.[childB.id].children
    let nodeC = state0.graph.nodes.[root.children.[1].id]
    let goodChange =
        { id = 1
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(nodeC.id, nodeC.text, "ok") ] }
    let ownershipBreakingChange =
        { id = 2
          changeId = System.Guid.NewGuid()
          ops =
            [ Op.Replace(childB.id, originalBChildren, originalBChildren @ [ childA ]) ] }
    match SyncLogic.applyServerTail [ goodChange; ownershipBreakingChange ] (ofState state0) with
    | Error msg -> failwith $"Expected Ok (no ownership re-check), got Error: {msg}"
    | Ok result ->
        Assert.Equal(Revision (state0.revision.Value + 2), result.revision)
        Assert.Equal("ok", result.graph.nodes.[nodeC.id].text)
        match History.validateOwnership result.graph with
        | Ok () -> failwith "Expected ownership to fail on result (proves check was skipped)"
        | Error msg -> Assert.Contains("ownership", msg)

// ---------------------------------------------------------------------------
// External-changes consume — rewind and replay
// ---------------------------------------------------------------------------

let private textChange id nodeId oldText newText : Change =
    { id = id
      changeId = System.Guid.NewGuid()
      ops = [ Op.SetText(nodeId, oldText, newText) ] }

let private seededEditState () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange 0 nodeId "before" "after"
    let state0 : ClientSyncState =
        { graph = graph1
          revision = Revision 0
          history = ClientHistory.clear () }
    match SyncLogic.applyLocalChange "Edit node" change state0 with
    | Error msg -> failwith msg
    | Ok (state, pending) -> nodeId, state, pending, change

[<Fact>]
let ``consumeCatchUpPoll rewinds to baseline and preserves History`` () =
    let nodeId, optimistic, pending, _ = seededEditState ()
    let baselineGraph =
        match Graph.setText nodeId "after" "before" optimistic.graph with
        | Ok graph -> graph
        | Error msg -> failwith msg
    let baseline =
        { revision = Revision 0
          graph = baselineGraph }
    let serverChange =
        { id = 1
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(nodeId, "before", "server") ] }
    match
        SyncLogic.consumeCatchUpPoll
            baseline
            [ serverChange ]
            (Revision 1)
            optimistic
    with
    | Error msg -> failwith msg
    | Ok result ->
        Assert.Equal("server", result.graph.nodes.[nodeId].text)
        Assert.Equal(Revision 1, result.revision)
        Assert.Equal(optimistic.history, result.history)
        Assert.NotEqual(pending.change.changeId, serverChange.changeId)

[<Fact>]
let ``applyServerTail with changes preserves History`` () =
    let past = mkChange 4
    let st = emptyState () |> withRecorded past
    let state0 = ModelBuilder.createState12 ()
    let root = state0.graph.nodes.[state0.graph.root]
    let nodeA = state0.graph.nodes.[root.children.[0].id]
    let change =
        { id = 1
          changeId = System.Guid.NewGuid()
          ops = [ Op.SetText(nodeA.id, nodeA.text, nodeA.text + "!") ] }
    let client : ClientSyncState =
        { graph = state0.graph
          revision = Revision state0.revision.Value
          history = st.history }
    match SyncLogic.applyServerTail [ change ] client with
    | Error msg -> failwith msg
    | Ok result -> Assert.Equal(st.history, result.history)
