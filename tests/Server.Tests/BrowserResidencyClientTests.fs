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
