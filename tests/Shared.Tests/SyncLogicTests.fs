module SyncLogicTests

open Gambol.Shared
open Gambol.Shared
open Gambol.Shared.ViewModel
open GraphChildMapHelpers
open Xunit

// ---------------------------------------------------------------------------
// Test helpers
// ---------------------------------------------------------------------------

let private emptyModel = VmTestHelpers.emptyModel

let private mkChange n =
    SpecialNodeTestHelpers.changeEvent
        "fixture"
        (EventIdFixtures.storedId n)
        (System.Guid.NewGuid())
        []

let private mkPoll rev build page : ChangeSuccessResponse =
    { eventId = EventIdFixtures.storedId rev
      buildEpochSec = build
      pageBuildEpochSec = page
      apiVersion = ApiVersion.current
      isReady = true
      externalChanges = false
      events = []
      message = None
      bootstrapHash = None
      nodes = []
      childMap = Map.empty }

// ---------------------------------------------------------------------------
// getPollOutcome — data outdated
// ---------------------------------------------------------------------------

[<Fact>]
let ``getPollOutcome returns DataOutdated when server revision is ahead`` () =
    let poll = mkPoll 6 1 1
    Assert.Equal(Some DataOutdated, SyncLogic.getPollOutcome poll (EventIdFixtures.storedId 5))

[<Fact>]
let ``getPollOutcome returns None when server revision equals client`` () =
    let poll = mkPoll 5 1 1
    Assert.Equal(None, SyncLogic.getPollOutcome poll (EventIdFixtures.storedId 5))

// ---------------------------------------------------------------------------
// getPollOutcome — API version, not process/page stamps
// ---------------------------------------------------------------------------

[<Fact>]
let ``getPollOutcome does not CodeOutdated an existing page after server restart with the same API`` () =
    let pageProcessStart = 1_700_000_000
    let restartedProcessStart = pageProcessStart + 1
    let pageBuild = 1_699_999_000
    let poll = mkPoll 5 restartedProcessStart pageBuild
    Assert.Equal(None, SyncLogic.getPollOutcome poll (EventIdFixtures.storedId 5))

[<Fact>]
let ``serverProcessRestarted detects DeployEpochSec change without CodeOutdated`` () =
    let pageProcessStart = 1_700_000_000
    let restarted = pageProcessStart + 1
    let poll = mkPoll 5 restarted 1_699_999_000
    Assert.True(
        SyncLogic.serverProcessRestarted pageProcessStart poll.buildEpochSec)
    Assert.Equal(None, SyncLogic.getPollOutcome poll (EventIdFixtures.storedId 5))
    Assert.False(
        SyncLogic.serverProcessRestarted restarted poll.buildEpochSec)
    Assert.False(SyncLogic.serverProcessRestarted 0 poll.buildEpochSec)
    Assert.False(SyncLogic.serverProcessRestarted pageProcessStart 0)

[<Fact>]
let ``getPollOutcome returns DataOutdated after server restart when revision is ahead`` () =
    let pageProcessStart = 1_700_000_000
    let poll = mkPoll 6 (pageProcessStart + 1) 1_699_999_000
    Assert.Equal(Some DataOutdated, SyncLogic.getPollOutcome poll (EventIdFixtures.storedId 5))

[<Fact>]
let ``getPollOutcome does not treat page stamp drift as CodeOutdated`` () =
    let poll = mkPoll 5 1 99
    Assert.Equal(None, SyncLogic.getPollOutcome poll (EventIdFixtures.storedId 5))

[<Fact>]
let ``getPollOutcome ignores apiVersion mismatch`` () =
    let poll = { mkPoll 5 1 1 with apiVersion = ApiVersion.current + 1 }
    Assert.Equal(None, SyncLogic.getPollOutcome poll (EventIdFixtures.storedId 5))

[<Fact>]
let ``getPollOutcome returns DataOutdated when event id is ahead even if apiVersion differs`` () =
    let poll = { mkPoll 6 1 1 with apiVersion = ApiVersion.current + 1 }
    Assert.Equal(Some DataOutdated, SyncLogic.getPollOutcome poll (EventIdFixtures.storedId 5))

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
let ``SyncInfo withPending replaces pending list`` () =
    let pending = [ mkChange 0 ]
    let si = SyncInfo.initial
    let si2 = SyncInfo.withPending pending si
    Assert.Equal(1, si2.pending.Length)

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
    let si2 = SyncInfo.withSyncState (WaitingToRetry (1, EventId.zero, [])) si
    Assert.True(si2.syncRiskAcknowledged)

// ---------------------------------------------------------------------------
// applyServerTail
// ---------------------------------------------------------------------------

let private emptyState () : ClientSyncState =
    ClientSyncState.create
        (Graph.create ())
        (EventIdFixtures.storedId 5)
        (ClientHistory.clear ())

let private ofState (st: State) : ClientSyncState =
    ClientSyncState.create
        st.graph
        st.eventId
        (ClientHistory.clear ())

let private applyTail events state =
    SyncLogic.applyServerTail events state

let private withRecorded (event: Ev) (state: ClientSyncState) =
    let history =
        ClientHistory.record { event with commandName = "test" } state.history
    { state with history = history }

let private stateWithNode text : ClientSyncState * NodeId =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode text graph0
    { emptyState() with graph = graph1; eventId = EventIdFixtures.storedId 3 }, nodeId

[<Fact>]
let ``applyServerTail empty list returns Ok with state unchanged`` () =
    let past = mkChange 4
    let st = emptyState () |> withRecorded past
    match applyTail [] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(st.eventId, result.eventId)
        Assert.Equal(st.graph.root, result.graph.root)
        Assert.Equal(st.history, result.history)

[<Fact>]
let ``applyServerTail non-empty tail preserves History`` () =
    let st, nodeId = stateWithNode "before"
    let local = mkChange 2
    let withHistory = st |> withRecorded local
    let upstream =
        SpecialNodeTestHelpers.changeEvent
            ""
            (EventIdFixtures.storedId 3)
            (System.Guid.NewGuid())
            [ Op.SetText(nodeId, "before", "after") ]
    match applyTail [ upstream ] withHistory with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(withHistory.history, result.history)
        Assert.Equal("after", result.graph.nodes.[nodeId].text)
        Assert.Equal(EventIdFixtures.storedId 3, result.eventId)
    let st = emptyState ()
    let changes = [ mkChange 5; mkChange 6; mkChange 7 ]
    match applyTail changes st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result -> Assert.Equal(EventIdFixtures.storedId 7, result.eventId)

[<Fact>]
let ``applyServerTail applies graph mutations`` () =
    let st, nodeId = stateWithNode "before"
    let change =
        { id = EventIdFixtures.storedId 3
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(nodeId, "before", "after") ] }
    match applyTail [ change ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal("after", result.graph.nodes.[nodeId].text)
        Assert.Equal(EventIdFixtures.storedId 3, result.eventId)

let private actorStartEvent eventId focusId : Ev =
    { id = eventId
      submissionId = System.Guid.NewGuid()
      authority = Authority "Browser"
      commandName = "Start"
      body =
        EventBody.ActorStart
            { zoomId = focusId
              focusId = focusId
              commandId = NodeId.New()
              graphIds = [ focusId ]
              eventId = eventId } }

let private actorStopEvent eventId focusId : Ev =
    { id = eventId
      submissionId = System.Guid.NewGuid()
      authority = Authority "Browser"
      commandName = "Stop"
      body = EventBody.ActorStop(focusId, ActorSucceeded) }

[<Fact>]
let ``applyServerTail ActorStart adds and ActorStop removes a live Focus`` () =
    let focusId = NodeId.New()
    let st = emptyState ()
    let started = actorStartEvent (EventIdFixtures.storedId 6) focusId
    match applyTail [ started ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok afterStart ->
        Assert.True(Set.contains focusId afterStart.actorLiveFocusIds)
        let stopped = actorStopEvent (EventIdFixtures.storedId 7) focusId
        match applyTail [ stopped ] afterStart with
        | Error msg -> failwith $"Expected Ok, got Error: {msg}"
        | Ok afterStop ->
            Assert.False(Set.contains focusId afterStop.actorLiveFocusIds)

[<Fact>]
let ``applyServerTail ActorStart keeps pending Graph ops`` () =
    let st, nodeId = stateWithNode "orig"
    let localOps = [ Op.SetText(nodeId, "orig", "local") ]
    let afterLocal =
        match
            ResidentProjection.applyOps
                localOps
                { graph = st.graph; eventId = st.eventId }
        with
        | ApplyResult.Changed next -> next.graph
        | other -> failwith $"local text: {other}"
    let pendingEv =
        { id = EventId.zero
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = "Edit"
          body = EventBody.Change localOps }
    let st =
        { st with
            graph = afterLocal
            pending = [ pendingEv ] }
    let started = actorStartEvent (EventIdFixtures.storedId 6) nodeId
    match applyTail [ started ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal("local", result.graph.nodes.[nodeId].text)
        Assert.Equal(1, result.pending.Length)
        Assert.True(Set.contains nodeId result.actorLiveFocusIds)

[<Fact>]
let ``applyServerTail non-mismatch Change keeps pending Graph ops`` () =
    let st, nodeId = stateWithNode "orig"
    let otherGraph, otherId = Graph.newNode "other" st.graph
    let localOps = [ Op.SetText(nodeId, "orig", "local") ]
    let afterLocal =
        match
            ResidentProjection.applyOps
                localOps
                { graph = otherGraph; eventId = st.eventId }
        with
        | ApplyResult.Changed next -> next.graph
        | other -> failwith $"local text: {other}"
    let pendingEv =
        { id = EventId.zero
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = "Edit"
          body = EventBody.Change localOps }
    let st =
        { st with
            graph = afterLocal
            pending = [ pendingEv ] }
    let incoming =
        { id = EventIdFixtures.storedId 6
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(otherId, "other", "remote") ] }
    match applyTail [ incoming ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal("local", result.graph.nodes.[nodeId].text)
        Assert.Equal("remote", result.graph.nodes.[otherId].text)
        Assert.Equal(1, result.pending.Length)

[<Fact>]
let ``applyServerTail carries SetUpdateTime after SetText as poll stamp path`` () =
    let st, nodeId = stateWithNode "before"
    let stamp = System.DateTime(2026, 7, 22, 18, 0, 0, System.DateTimeKind.Utc)
    let change =
        { id = EventIdFixtures.storedId 3
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change
              [ Op.SetText(nodeId, "before", "after")
                Op.SetUpdateTime(nodeId, NodeUpdateTime.missing, stamp) ] }
    match applyTail [ change ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal("after", result.graph.nodes.[nodeId].text)
        Assert.Equal(
            NodeUpdateTime.toDbPrecision stamp,
            result.graph.nodes.[nodeId].updateTime)

let private changeEvent eventId ops : Ev =
    { id = EventIdFixtures.storedId eventId
      submissionId = System.Guid.NewGuid()
      authority = Authority "Browser"
      commandName = ""
      body = EventBody.Change ops }

[<Fact>]
let ``applyServerTail returns Error on first hard-invalid change`` () =
    let st, _ = stateWithNode "original"
    let badChange =
        changeEvent 5 [ Op.SetText(st.graph.root, "wrong", "new") ]
    let goodChange = mkChange 6
    match applyTail [ badChange; goodChange ] st with
    | Ok _ -> failwith "Expected Error but got Ok"
    | Error _ -> ()

[<Fact>]
let ``applyServerTail short-circuits: state unchanged after hard-invalid change`` () =
    let st, nodeId = stateWithNode "original"
    let badChange =
        changeEvent 3 [ Op.SetText(st.graph.root, "wrong", "y") ]
    let goodChange =
        changeEvent 4 [ Op.SetText(nodeId, "original", "modified") ]
    match applyTail [ badChange; goodChange ] st with
    | Ok _ -> failwith "Expected Error but got Ok"
    | Error _ ->
        Assert.Equal("original", st.graph.nodes.[nodeId].text)

[<Fact>]
let ``applyServerTail soft-skips SetName CAS and continues the event list`` () =
    let st, nodeId = stateWithNode "original"
    let staleName = changeEvent 4 [ Op.SetName(nodeId, "stale", "renamed.md") ]
    let laterText = changeEvent 5 [ Op.SetText(nodeId, "original", "modified") ]
    match applyTail [ staleName; laterText ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(Filename.Empty, result.graph.nodes.[nodeId].name)
        Assert.Equal("modified", result.graph.nodes.[nodeId].text)
        Assert.Equal(EventIdFixtures.storedId 5, result.eventId)
        Assert.Equal(Some "can't change the name", result.applyDetail)

[<Fact>]
let ``applyServerTail soft-skips SetText CAS and continues later ops`` () =
    let st, nodeId = stateWithNode "original"
    let mixed =
        changeEvent
            4
            [ Op.SetText(nodeId, "stale", "skipped")
              Op.SetText(nodeId, "original", "kept") ]
    match applyTail [ mixed ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal("kept", result.graph.nodes.[nodeId].text)
        Assert.Equal(Some "can't change the text", result.applyDetail)

[<Fact>]
let ``applyServerTail soft-skips SetClasses CAS`` () =
    let st, nodeId = stateWithNode "original"
    let stale =
        changeEvent
            4
            [ Op.SetClasses(
                  nodeId,
                  CssClass.ofList [ "stale" ],
                  CssClass.ofList [ "next" ]) ]
    match applyTail [ stale ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(CssClass.empty, result.graph.nodes.[nodeId].cssClasses)
        Assert.Equal(EventIdFixtures.storedId 4, result.eventId)
        Assert.Equal(Some "can't change the classes", result.applyDetail)

[<Fact>]
let ``applyServerTail soft-skips Replace CAS`` () =
    let st, parentId = stateWithNode "original"
    let childId = NodeId.New()
    let ghost = NodeId.New()
    let seeded =
        match
            ChangeValidation.applyOps
                [ Op.NewNode(childId, "c")
                  ChildListWire.replace
                      parentId
                      []
                      [ ChildNode.owner childId ] ]
                { graph = st.graph; eventId = st.eventId }
        with
        | ApplyResult.Changed next -> { st with graph = next.graph }
        | other -> failwith $"seed child: {other}"
    let stale =
        changeEvent
            4
            [ ChildListWire.replace
                  parentId
                  [ ChildNode.owner ghost ]
                  [] ]
    match applyTail [ stale ] seeded with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal<ChildNode list>(
            [ ChildNode.owner childId ],
            Graph.children result.graph parentId)
        Assert.Equal(EventIdFixtures.storedId 4, result.eventId)
        Assert.Equal(Some "can't change the structure", result.applyDetail)

[<Fact>]
let ``applyServerTail SetName CAS undoes conflicting pending rename`` () =
    let st, nodeId = stateWithNode "n"
    match Graph.setName nodeId "" "keep.md" st.graph with
    | Error msg -> failwith msg
    | Ok named ->
        let localOps = [ Op.SetName(nodeId, "keep.md", "local.md") ]
        let afterLocal =
            match
                ResidentProjection.applyOps
                    localOps
                    { graph = named; eventId = st.eventId }
            with
            | ApplyResult.Changed next -> next.graph
            | other -> failwith $"local rename: {other}"
        let localId = System.Guid.NewGuid()
        let pendingEv =
            { id = EventId.zero
              submissionId = localId
              authority = Authority "Browser"
              commandName = "Rename"
              body = EventBody.Change localOps }
        let st =
            { st with
                graph = afterLocal
                pending = [ pendingEv ] }
        let incoming =
            changeEvent 5 [ Op.SetName(nodeId, "keep.md", "server.md") ]
        match applyTail [ incoming ] st with
        | Error msg -> failwith $"Expected Ok, got Error: {msg}"
        | Ok result ->
            Assert.Equal(
                Filename.Ok "server.md",
                result.graph.nodes.[nodeId].name)
            Assert.Equal(Some "can't change the name", result.applyDetail)

[<Fact>]
let ``applyServerTail consumes Change on Absent Header without graph effect`` () =
    let st = emptyState ()
    let absentId = NodeId.New()
    let change =
        { id = EventIdFixtures.storedId 5
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(absentId, "old", "new") ] }
    match applyTail [ change ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(EventIdFixtures.storedId 5, result.eventId)
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
            owner = Graph.workspacesId)
    let child =
        Node.Create(childId, text = "child", owner = wsId)
    let graph =
        graph0
        |> addDetachedMany [ ws; child ]
        |> appendKids Graph.workspacesId [ ChildNode.owner wsId ]
        |> unload wsId
    let st: ClientSyncState =
        { graph = graph
          history = ClientHistory.clear ()
          eventId = EventIdFixtures.storedId 3
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty
          applyDetail = None
          pending = [] }
    let change =
        { id = EventIdFixtures.storedId 4
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change
              [ Op.Replace(wsId, [], [ ChildNode.owner childId ]) ] }
    match applyTail [ change ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(EventIdFixtures.storedId 4, result.eventId)
        Assert.Equal(Unloaded, Graph.childrenStatus result.graph wsId)
        Assert.Equal<ChildNode list>([], Graph.children result.graph wsId)

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
            owner = Graph.workspacesId)
    let graph =
        graph0
        |> Graph.addDetachedNode ws
        |> appendKids Graph.workspacesId [ ChildNode.owner wsId ]
        |> unload wsId
    let st: ClientSyncState =
        { graph = graph
          history = ClientHistory.clear ()
          eventId = EventIdFixtures.storedId 2
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty
          applyDetail = None
          pending = [] }
    let change =
        { id = EventIdFixtures.storedId 3
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(wsId, "before", "after") ] }
    match applyTail [ change ] st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal("after", result.graph.nodes.[wsId].text)
        Assert.Equal(Unloaded, Graph.childrenStatus result.graph wsId)

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
            owner = Graph.workspacesId)
    let marker =
        Node.Create(markerId, text = "marker", owner = Graph.rootId)
    let graph =
        graph0
        |> addDetachedMany [ wsHeader; marker ]
        |> appendKids Graph.workspacesId [ ChildNode.owner wsId ]
        |> appendKids graph0.root [ ChildNode.owner markerId ]
        |> unload wsId
    let st: ClientSyncState =
        { graph = graph
          history =
            ClientHistory.record { mkChange 1 with commandName = "test" } (ClientHistory.clear ())
          eventId = EventIdFixtures.storedId 5
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty
          applyDetail = None
          pending = [] }
    let child =
        Node.Create(childId, text = "leaf", owner = wsId)
    // External resident header whose owner edge lives only in an Unloaded list.
    let external =
        Node.Create(
            ownerWs,
            text = "ext",
            name = Filename.Ok "ext",
            kind = Special Workspace,
            owner = wsId)
    // Change touches a Loaded root child; answer then installs ws at response event id.
    let response =
        { events =
              [ SpecialNodeTestHelpers.changeEvent
                    ""
                    (EventIdFixtures.storedId 6)
                    (System.Guid.NewGuid())
                    [ Op.SetText(markerId, "marker", "marker-tail") ] ]
          nodes = [ wsHeader; child; external ]
          childMap =
            Map.ofList
                [ wsId, [ ChildNode.owner childId ]
                  childId, [] ] }
    match SyncLogic.applySyncResponse response st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(st.history, result.history)
        Assert.Equal(EventIdFixtures.storedId 6, result.eventId)
        Assert.Equal(Loaded, Graph.childrenStatus result.graph wsId)
        Assert.Equal(1, (Graph.children result.graph wsId).Length)
        Assert.Equal("marker-tail", result.graph.nodes.[markerId].text)
        Assert.Equal(wsId, result.graph.nodes.[external.id].owner)
        Assert.Equal(Some wsId, result.graph.ownerParentByChild |> Map.tryFind childId)
        Assert.Equal(None, result.graph.ownerParentByChild |> Map.tryFind ownerWs)

[<Fact>]
let ``applyServerTail multi-change tail advances revision and graph`` () =
    let state0 =
        { ModelBuilder.createState12 () with eventId = EventIdFixtures.storedId 10 }
    let rootKids = Graph.children state0.graph state0.graph.root
    let nodeA = state0.graph.nodes.[rootKids.[0].id]
    let nodeB = state0.graph.nodes.[rootKids.[1].id]
    let change1 =
        { id = EventIdFixtures.storedId 1
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(nodeA.id, nodeA.text, nodeA.text + "1") ] }
    let change2 =
        { id = EventIdFixtures.storedId 2
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(nodeB.id, nodeB.text, nodeB.text + "2") ] }
    match applyTail [ change1; change2 ] (ofState state0) with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(EventIdFixtures.storedId 2, result.eventId)
        Assert.Equal(nodeA.text + "1", result.graph.nodes.[nodeA.id].text)
        Assert.Equal(nodeB.text + "2", result.graph.nodes.[nodeB.id].text)

[<Fact>]
let ``applySyncResponse empty answer and empty changes preserves History`` () =
    let past = mkChange 4
    let st = emptyState () |> withRecorded past
    match
        SyncLogic.applySyncResponse
            { events = []
              nodes = []
              childMap = Map.empty }
            st
    with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(st.history, result.history)
        Assert.Equal(st.eventId, result.eventId)

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
            owner = Graph.workspacesId)
    let graph =
        graph0
        |> Graph.addDetachedNode ws
        |> appendKids Graph.workspacesId [ ChildNode.owner wsId ]
        |> unload wsId
    let past = mkChange 2
    let st: ClientSyncState =
        { graph = graph
          history =
            ClientHistory.record { past with commandName = "test" } (ClientHistory.clear ())
          eventId = EventIdFixtures.storedId 4
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty
          applyDetail = None
          pending = [] }
    match
        SyncLogic.applySyncResponse
            { events = []
              nodes = [ ws ]
              childMap = Map.ofList [ wsId, [] ] }
            st
    with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(st.history, result.history)
        Assert.Equal(EventIdFixtures.storedId 4, result.eventId)
        Assert.Equal(Loaded, Graph.childrenStatus result.graph wsId)
        Assert.Equal<ChildNode list>([], Graph.children result.graph wsId)

[<Fact>]
let ``applySyncResponse installs Want answer after Event tail`` () =
    let graph0 = Graph.create ()
    let parentId = NodeId.New()
    let childId = NodeId.New()
    let markerId = NodeId.New()
    let parent = Node.Create(parentId, text = "parent", owner = graph0.root)
    let marker = Node.Create(markerId, text = "marker", owner = graph0.root)
    let graph =
        graph0
        |> addDetachedMany [ parent; marker ]
        |> appendKids graph0.root [ ChildNode.owner parentId; ChildNode.owner markerId ]
        |> unload parentId
    let st: ClientSyncState =
        { graph = graph
          history =
            ClientHistory.record { mkChange 1 with commandName = "test" } (ClientHistory.clear ())
          eventId = EventIdFixtures.storedId 5
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty
          applyDetail = None
          pending = [] }
    let child = Node.Create(childId, text = "leaf", owner = parentId)
    let response =
        { events =
              [ SpecialNodeTestHelpers.changeEvent
                    ""
                    (EventIdFixtures.storedId 6)
                    (System.Guid.NewGuid())
                    [ Op.SetText(markerId, "marker", "marker-tail") ] ]
          nodes = [ parent; child ]
          childMap =
            Map.ofList
                [ parentId, [ ChildNode.owner childId ]
                  childId, [] ] }
    match SyncLogic.applySyncResponse response st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(st.history, result.history)
        Assert.Equal(EventIdFixtures.storedId 6, result.eventId)
        Assert.Equal("marker-tail", result.graph.nodes.[markerId].text)
        Assert.Equal(Loaded, Graph.childrenStatus result.graph parentId)
        Assert.Equal(1, (Graph.children result.graph parentId).Length)
        Assert.Equal(childId, (Graph.children result.graph parentId).[0].id)
        Assert.True(Map.containsKey childId result.graph.nodes)

[<Fact>]
let ``applySyncResponse Want-answer empty list marks Loaded leaf`` () =
    let graph0 = Graph.create ()
    let parentId = NodeId.New()
    let parent =
        Node.Create(parentId, text = "leaf-parent", owner = graph0.root)
    let graph =
        graph0
        |> Graph.addDetachedNode parent
        |> appendKids graph0.root [ ChildNode.owner parentId ]
        |> unload parentId
    let past = mkChange 2
    let st: ClientSyncState =
        { graph = graph
          history =
            ClientHistory.record { past with commandName = "test" } (ClientHistory.clear ())
          eventId = EventIdFixtures.storedId 4
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty
          applyDetail = None
          pending = [] }
    match
        SyncLogic.applySyncResponse
            { events = []
              nodes = [ parent ]
              childMap = Map.ofList [ parentId, [] ] }
            st
    with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(st.history, result.history)
        Assert.Equal(EventIdFixtures.storedId 4, result.eventId)
        Assert.Equal(Loaded, Graph.childrenStatus result.graph parentId)
        Assert.Equal<ChildNode list>([], Graph.children result.graph parentId)

[<Fact>]
let ``applySyncResponse refuses dangling Want edges`` () =
    let graph0 = Graph.create ()
    let parentId = NodeId.New()
    let missingId = NodeId.New()
    let parent = Node.Create(parentId, text = "parent", owner = graph0.root)
    let graph =
        graph0
        |> Graph.addDetachedNode parent
        |> appendKids graph0.root [ ChildNode.owner parentId ]
        |> unload parentId
    let st: ClientSyncState =
        { graph = graph
          history = ClientHistory.clear ()
          eventId = EventIdFixtures.storedId 3
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty
          applyDetail = None
          pending = [] }
    match
        SyncLogic.applySyncResponse
            { events = []
              nodes = [ parent ]
              childMap =
                Map.ofList [ parentId, [ ChildNode.owner missingId ] ] }
            st
    with
    | Ok _ -> failwith "Expected dangling edge refusal"
    | Error msg -> Assert.Equal("dangling edge", msg)

[<Fact>]
let ``applySyncResponse Load Fetch answer installs through installWantAnswer`` () =
    let graph0 = Graph.create ()
    let wantParentId = NodeId.New()
    let wantChildId = NodeId.New()
    let wsId = NodeId.New()
    let wsChildId = NodeId.New()
    let wantParent =
        Node.Create(wantParentId, text = "want-parent", owner = graph0.root)
    let wsHeader =
        Node.Create(
            wsId,
            text = "ws",
            name = Filename.Ok "ws",
            kind = Special Workspace,
            owner = Graph.workspacesId)
    let graph =
        graph0
        |> addDetachedMany [ wantParent; wsHeader ]
        |> appendKids graph0.root [ ChildNode.owner wantParentId ]
        |> appendKids Graph.workspacesId [ ChildNode.owner wsId ]
        |> unload wantParentId
        |> unload wsId
    let st: ClientSyncState =
        { graph = graph
          history = ClientHistory.clear ()
          eventId = EventIdFixtures.storedId 4
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty
          applyDetail = None
          pending = [] }
    let wantChild =
        Node.Create(wantChildId, text = "want-leaf", owner = wantParentId)
    let wsChild = Node.Create(wsChildId, text = "ws-leaf", owner = wsId)
    let response =
        { events = []
          nodes = [ wantParent; wantChild; wsHeader; wsChild ]
          childMap =
            Map.ofList
                [ wantParentId, [ ChildNode.owner wantChildId ]
                  wantChildId, []
                  wsId, [ ChildNode.owner wsChildId ]
                  wsChildId, [] ] }
    match SyncLogic.applySyncResponse response st with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok result ->
        Assert.Equal(Loaded, Graph.childrenStatus result.graph wantParentId)
        Assert.Equal(wantChildId, (Graph.children result.graph wantParentId).[0].id)
        Assert.Equal(Loaded, Graph.childrenStatus result.graph wsId)
        Assert.Equal(wsChildId, (Graph.children result.graph wsId).[0].id)

[<Fact>]
let ``changeSuccessToSync carries Want answer`` () =
    let parentId = NodeId.New()
    let childId = NodeId.New()
    let parent = Node.Create(parentId, text = "parent")
    let child = Node.Create(childId, text = "child", owner = parentId)
    let poll: ChangeSuccessResponse =
        { mkPoll 7 1 1 with
            events = [ mkChange 8 ]
            nodes = [ parent; child ]
            childMap = Map.ofList [ parentId, ChildNode.owners [ childId ] ] }
    let sync = SyncLogic.changeSuccessToSync poll
    Assert.Equal(1, sync.events.Length)
    Assert.Equal(2, sync.nodes.Length)
    Assert.Equal(parentId, sync.nodes.[0].id)
    Assert.Equal<ChildNode list>(
        ChildNode.owners [ childId ],
        sync.childMap.[parentId])

[<Fact>]
let ``getPollOutcome keys on event id not apiVersion`` () =
    let parentId = NodeId.New()
    let poll =
        { mkPoll 5 1 1 with
            nodes = [ Node.Create(parentId, text = "p") ]
            childMap = Map.ofList [ parentId, [] ]
            apiVersion = ApiVersion.current + 1 }
    Assert.Equal(None, SyncLogic.getPollOutcome poll (EventIdFixtures.storedId 5))
    let ahead = { poll with eventId = EventIdFixtures.storedId 6 }
    Assert.Equal(
        Some DataOutdated,
        SyncLogic.getPollOutcome ahead (EventIdFixtures.storedId 5))

[<Fact>]
let ``applyServerTail trusts server tails without ownership re-check`` () =
    // Server-accepted tails are trusted: poll apply must not reject on ownership.
    let state0 = ModelBuilder.createState12 ()
    let rootId = state0.graph.root
    let rootKids = Graph.children state0.graph rootId
    let childA = rootKids.[0]
    let nodeA = state0.graph.nodes.[childA.id]
    let childB = (Graph.children state0.graph childA.id).[0]
    let originalBChildren = Graph.children state0.graph childB.id
    let nodeC = state0.graph.nodes.[rootKids.[1].id]
    let goodChange =
        { id = EventIdFixtures.storedId 1
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(nodeC.id, nodeC.text, "ok") ] }
    let ownershipBreakingChange =
        { id = EventIdFixtures.storedId 2
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change
            [ Op.Replace(childB.id, originalBChildren, originalBChildren @ [ childA ]) ] }
    match applyTail [ goodChange; ownershipBreakingChange ] (ofState state0) with
    | Error msg -> failwith $"Expected Ok (no ownership re-check), got Error: {msg}"
    | Ok result ->
        Assert.Equal(EventIdFixtures.storedId 2, result.eventId)
        Assert.Equal("ok", result.graph.nodes.[nodeC.id].text)
        match ChangeValidation.validateOwnership result.graph with
        | Ok () -> failwith "Expected ownership to fail on result (proves check was skipped)"
        | Error msg -> Assert.Contains("ownership", msg)

// ---------------------------------------------------------------------------
// External-changes consume — rewind and replay
// ---------------------------------------------------------------------------

let private textChange id nodeId oldText newText : Ev =
    SpecialNodeTestHelpers.changeEvent
        "Edit node"
        id
        (System.Guid.NewGuid())
        [ Op.SetText(nodeId, oldText, newText) ]

let private seededEditState () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange EventId.zero nodeId "before" "after"
    let state0 : ClientSyncState =
        { graph = graph1
          eventId = EventId.zero
          history = ClientHistory.clear ()
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty
          applyDetail = None
          pending = [] }
    match SyncLogic.applyLocalEvent change state0 with
    | Error msg -> failwith msg
    | Ok (state, pending) -> nodeId, state, pending, change

[<Fact>]
let ``consumeCatchUpPoll rewinds to baseline and preserves History`` () =
    let nodeId, optimistic, pending, _ = seededEditState ()
    let baselineGraph =
        match Graph.setText nodeId "after" "before" optimistic.graph with
        | Ok graph -> graph
        | Error msg -> failwith msg
    let baseline : CatchUpBaseline =
        { eventId = EventId.zero
          graph = baselineGraph }
    let serverChange =
        SpecialNodeTestHelpers.changeEvent
            ""
            (EventIdFixtures.storedId 1)
            (System.Guid.NewGuid())
            [ Op.SetText(nodeId, "before", "server") ]
    match
        SyncLogic.consumeCatchUpPoll
            baseline
            [ serverChange ]
            (EventIdFixtures.storedId 1)
            optimistic
    with
    | Error msg -> failwith msg
    | Ok result ->
        Assert.Equal("server", result.graph.nodes.[nodeId].text)
        Assert.Equal(EventIdFixtures.storedId 1, result.eventId)
        Assert.Equal(optimistic.history, result.history)
        Assert.NotEqual(pending.submissionId, serverChange.submissionId)

[<Fact>]
let ``consumeCatchUpPoll stamps History when stream matches submissionId`` () =
    let nodeId, optimistic, pending, change = seededEditState ()
    let baselineGraph =
        match Graph.setText nodeId "after" "before" optimistic.graph with
        | Ok graph -> graph
        | Error msg -> failwith msg
    let baseline : CatchUpBaseline =
        { eventId = EventId.zero
          graph = baselineGraph }
    let stamped =
        { change with id = EventIdFixtures.storedId 4 }
    match
        SyncLogic.consumeCatchUpPoll
            baseline
            [ stamped ]
            (EventIdFixtures.storedId 4)
            optimistic
    with
    | Error msg -> failwith msg
    | Ok result ->
        Assert.Equal("after", result.graph.nodes.[nodeId].text)
        Assert.Equal(EventIdFixtures.storedId 4, result.eventId)
        Assert.Equal(pending.submissionId, stamped.submissionId)
        match ClientHistory.undoEvent result.history with
        | None -> failwith "expected stamped Undo"
        | Some (event, _) ->
            match event.body with
            | EventBody.Undo(target, _) ->
                Assert.Equal(EventIdFixtures.storedId 4, target)
            | _ -> failwith "expected Undo body"

[<Fact>]
let ``consumeCatchUpPoll keeps trailing pending and shows it after Server play`` () =
    let graph0 = Graph.create ()
    let graphA, nodeA = Graph.newNode "a0" graph0
    let baselineGraph, nodeB = Graph.newNode "b0" graphA
    let prefixId = System.Guid.NewGuid()
    let trailingId = System.Guid.NewGuid()
    let prefix =
        { textChange EventId.zero nodeA "a0" "a-posted" with
            submissionId = prefixId }
    let trailing =
        { textChange EventId.zero nodeB "b0" "b-local" with
            submissionId = trailingId }
    let serverPrefix =
        { textChange (EventIdFixtures.storedId 1) nodeA "a0" "a-server" with
            submissionId = prefixId }
    let state : ClientSyncState =
        { graph = baselineGraph
          eventId = EventId.zero
          history = ClientHistory.clear ()
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty
          applyDetail = None
          pending = [ prefix; trailing ] }
    let baseline : CatchUpBaseline =
        { eventId = EventId.zero
          graph = baselineGraph }
    match
        SyncLogic.consumeCatchUpPoll
            baseline
            [ serverPrefix ]
            (EventIdFixtures.storedId 1)
            state
    with
    | Error msg -> failwith msg
    | Ok result ->
        Assert.Equal("a-server", result.graph.nodes.[nodeA].text)
        Assert.Equal("b-local", result.graph.nodes.[nodeB].text)
        Assert.Equal<Ev list>([ prefix; trailing ], result.pending)

[<Fact>]
let ``applySyncResponse re-applies trailing pending after precondition undo`` () =
    let graph0 = Graph.create ()
    let graphA, nodeA = Graph.newNode "orig" graph0
    let graphB, nodeB = Graph.newNode "b0" graphA
    let prefixId = System.Guid.NewGuid()
    let trailingId = System.Guid.NewGuid()
    let prefix =
        { textChange EventId.zero nodeA "orig" "local" with
            submissionId = prefixId }
    let trailing =
        { textChange EventId.zero nodeB "b0" "b-local" with
            submissionId = trailingId }
    let graph =
        match Graph.setText nodeA "orig" "local" graphB with
        | Error msg -> failwith msg
        | Ok graphA' ->
            match Graph.setText nodeB "b0" "b-local" graphA' with
            | Error msg -> failwith msg
            | Ok graphB' -> graphB'
    let state : ClientSyncState =
        { graph = graph
          eventId = EventId.zero
          history = ClientHistory.clear ()
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty
          applyDetail = None
          pending = [ prefix; trailing ] }
    let serverEdit =
        { textChange (EventIdFixtures.storedId 2) nodeA "orig" "server" with
            submissionId = System.Guid.NewGuid() }
    match
        SyncLogic.applySyncResponse
            { events = [ serverEdit ]
              nodes = []
              childMap = Map.empty }
            state
    with
    | Error msg -> failwith msg
    | Ok result ->
        Assert.Equal("server", result.graph.nodes.[nodeA].text)
        Assert.Equal("b-local", result.graph.nodes.[nodeB].text)
        Assert.Equal<Ev list>([ prefix; trailing ], result.pending)

[<Fact>]
let ``applySyncResponse re-applies pending after Want rewind`` () =
    let graph0 = Graph.create ()
    let graph1, markerId = Graph.newNode "orig" graph0
    let parentId = NodeId.New()
    let parent =
        Node.Create(parentId, text = "leaf-parent", owner = graph0.root)
    let graph =
        graph1
        |> Graph.addDetachedNode parent
        |> appendKids graph1.root [ ChildNode.owner parentId ]
        |> unload parentId
    let pendingEdit = textChange EventId.zero markerId "orig" "local"
    let graphLocal =
        match Graph.setText markerId "orig" "local" graph with
        | Error msg -> failwith msg
        | Ok next -> next
    let state : ClientSyncState =
        { graph = graphLocal
          eventId = EventIdFixtures.storedId 4
          history = ClientHistory.clear ()
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty
          applyDetail = None
          pending = [ pendingEdit ] }
    match
        SyncLogic.applySyncResponse
            { events = []
              nodes = [ parent ]
              childMap = Map.ofList [ parentId, [] ] }
            state
    with
    | Error msg -> failwith msg
    | Ok result ->
        Assert.Equal("local", result.graph.nodes.[markerId].text)
        Assert.Equal<Ev list>([ pendingEdit ], result.pending)
        Assert.Equal(Loaded, Graph.childrenStatus result.graph parentId)

[<Fact>]
let ``applyServerTail with changes preserves History`` () =
    let past = mkChange 4
    let st = emptyState () |> withRecorded past
    let state0 = ModelBuilder.createState12 ()
    let rootKids = Graph.children state0.graph state0.graph.root
    let nodeA = state0.graph.nodes.[rootKids.[0].id]
    let change =
        { id = EventIdFixtures.storedId 1
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(nodeA.id, nodeA.text, nodeA.text + "!") ] }
    let client : ClientSyncState =
        { graph = state0.graph
          eventId = state0.eventId
          history = st.history
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty
          applyDetail = None
          pending = [] }
    match applyTail [ change ] client with
    | Error msg -> failwith msg
    | Ok result -> Assert.Equal(st.history, result.history)
