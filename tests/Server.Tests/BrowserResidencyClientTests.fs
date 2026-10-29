module Gambol.Server.Tests.BrowserResidencyClientTests

open Gambol.Client
open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error error ->
        Assert.Fail($"{label}: {error}")
        Unchecked.defaultof<_>

let private findInstanceId nodeId (siteMap: SiteMap) =
    siteMap.entries
    |> Map.tryPick (fun siteId entry ->
        if entry.nodeId = nodeId then Some siteId else None)
    |> Option.defaultWith (fun () -> failwith $"missing SiteMap entry for {nodeId}")

let private modelFor graph siteMap nextSiteId zoomRoot =
    { graph = graph
      eventId = EventId.zero
      history = ClientHistory.clear ()
      actorLiveFocusIds = Set.empty
      selectedNodes = None
      mode = Selecting
      siteMap = siteMap
      nextSiteId = nextSiteId
      zoomRoot = zoomRoot
      zoomIngress = []
      clipboard = None
      desktopCapabilities = None
      serverCapabilities = None
      desktopFileIndicator = BlankFileIndicator
      workspaceMappedLabels = Set.empty
      workspaceRoots = Map.empty
      workspaceRemotes = Map.empty
      workspaceSyncFacts = Map.empty
      pendingAutoDownloads = []
      syncInfo = { SyncInfo.initial with syncState = Polling }
      lastCmdResult = None }

type private ResidencyFixture =
    { server: Graph
      browser: Graph
      siteMap: SiteMap
      nextSiteId: SiteId
      parentId: NodeId
      childId: NodeId }

let private residencyFixture () =
    let graph0 = Graph.create ()
    let parentId = NodeId.New()
    let childId = NodeId.New()
    let parent = Node.Create(parentId, text = "parent", owner = graph0.root)
    let child = Node.Create(childId, text = "child", owner = parentId)
    let server =
        graph0
        |> Graph.addDetachedNode parent
        |> Graph.addDetachedNode child
        |> Graph.replace graph0.root 0 [] [ ChildNode.owner parentId ]
        |> requireOk "root"
        |> Graph.replace parentId 0 [] [ ChildNode.owner childId ]
        |> requireOk "parent"
    let browser =
        ResidentProjection.visibleClosureGraph (Some graph0.root) server
    let siteMap0, nextId0 =
        ViewModel.buildSiteMapFrom browser graph0.root (Sid 0)
    let parentInst = findInstanceId parentId siteMap0
    let siteMap, nextId =
        ViewModel.expandEntry parentInst browser siteMap0 nextId0
    { server = server
      browser = browser
      siteMap = siteMap
      nextSiteId = nextId
      parentId = parentId
      childId = childId }

[<Fact>]
let ``Poll answer chains the next Included residency Want`` () =
    let fixture = residencyFixture ()
    let firstWant =
        Want.compose fixture.browser fixture.siteMap fixture.browser.root
    Assert.Equal<NodeId list>([ fixture.parentId ], firstWant)
    let edges, nodes =
        ResidentProjection.wantAnswer fixture.server firstWant
    let response =
        { events = []
          nodes = nodes
          childMap = edges }
    let next, effects =
        Update.update
            (SysMsg (PollDone (None, response, None, Some EventId.zero)))
            (modelFor
                fixture.browser
                fixture.siteMap
                fixture.nextSiteId
                fixture.browser.root)
    let nextWant = Want.compose next.graph next.siteMap next.zoomRoot
    Assert.True(Map.containsKey fixture.childId next.graph.nodes)
    Assert.Equal<NodeId list>([ fixture.childId ], nextWant)
    Assert.Contains(effects, function | PollServer _ -> true | _ -> false)

[<Fact>]
let ``PollDone leaves Sync Idle when the edit box holds new text`` () =
    let fixture = residencyFixture ()
    let model =
        { modelFor
            fixture.browser
            fixture.siteMap
            fixture.nextSiteId
            fixture.parentId
          with mode = Editing ("parent new", EditCaret.EndOfText) }
    Assert.Empty(model.syncInfo.pending)
    Assert.Equal("parent", model.graph.nodes.[fixture.parentId].text)
    match model.mode with
    | Editing (text, _) -> Assert.Equal("parent new", text)
    | _ -> failwith "expected Editing"
    let response =
        { events = []
          nodes = []
          childMap = Map.empty }
    let next, _ =
        Update.update
            (SysMsg (PollDone (
                Some DataOutdated, response, None, Some EventId.zero)))
            model
    Assert.Equal(Idle, next.syncInfo.syncState)

[<Fact>]
let ``Ignored reply posts the rest of the pending changes`` () =
    let fixture = residencyFixture ()
    let parentId = fixture.parentId
    let baseModel =
        modelFor
            fixture.browser
            fixture.siteMap
            fixture.nextSiteId
            fixture.parentId
    let state0 =
        ClientSyncState.create
            baseModel.graph
            baseModel.eventId
            baseModel.history
    let firstEvent =
        { id = EventId.zero
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = "Edit node"
          body =
            EventBody.Change [ Op.SetText(parentId, "parent", "local") ] }
    let state1, first =
        match SyncLogic.applyLocalEvent firstEvent state0 with
        | Ok pair -> pair
        | Error msg -> failwith msg
    let later =
        { first with
            id = EventId.zero
            submissionId = System.Guid.NewGuid()
            body = EventBody.Change [ Op.SetText(NodeId.New(), "x", "y") ] }
    let different =
        { first with
            submissionId = System.Guid.NewGuid()
            body =
                EventBody.Change
                    [ Op.SetText(parentId, "parent", "server") ] }
    let pending = [ first; later ]
    let model =
        { baseModel with
            graph = state1.graph
            history = state1.history
            syncInfo =
                { SyncInfo.initial with
                    pending = pending
                    syncState = Sending 1 } }
    let response =
        { eventId = EventIdFixtures.storedId 1
          buildEpochSec = 0
          pageBuildEpochSec = 0
          apiVersion = ApiVersion.current
          isReady = true
          externalChanges = false
          events = [ different ]
          message = None
          bootstrapHash = None
          nodes = []
          childMap = Map.empty }
    let next, effects =
        Update.update
            (SysMsg (SubmitResponse ([ first ], response)))
            model
    Assert.Equal("local", next.graph.nodes.[parentId].text)
    Assert.Equal<Ev list>([ later ], next.syncInfo.pending)
    Assert.Equal(Sending 1, next.syncInfo.syncState)
    Assert.True(next.syncInfo.catchUp.IsNone)
    match
        effects
        |> List.tryFind (function
            | SubmitPendingBatch _ -> true
            | _ -> false)
    with
    | Some (SubmitPendingBatch (_, events)) ->
        Assert.Equal<Ev list>([ later ], events)
    | _ -> failwith "expected remainder SubmitPendingBatch"
