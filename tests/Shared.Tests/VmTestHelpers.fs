module VmTestHelpers

open Gambol.Shared
open Gambol.Shared.ViewModel

/// Minimal VM — no selection, Selecting mode, siteMap from graph root.
let emptyModel (graph: Graph) : VM =
    let siteMap, nextId = buildSiteMap graph

    { graph = graph
      revision = Revision.Zero
      history = History.empty
      selectedNodes = None
      mode = Selecting
      siteMap = siteMap
      nextSiteId = nextId
      zoomRoot = graph.root
      zoomIngress = []
      clipboard = None
      desktopCapabilities = None
      serverCapabilities = None
      desktopFileIndicator = BlankFileIndicator
      workspaceMappedLabels = Set.empty
      workspaceSyncFacts = Map.empty
      syncInfo = SyncInfo.initial
      lastCmdResult = None }

/// VM scoped to viewRoot as the display root (siteMap built from viewRoot).
let emptyModelAt (graph: Graph) (viewRoot: NodeId) : VM =
    let siteMap, nextId = buildSiteMapFrom graph viewRoot (Sid 0)

    { graph = graph
      revision = Revision.Zero
      history = History.empty
      selectedNodes = None
      mode = Selecting
      siteMap = siteMap
      nextSiteId = nextId
      zoomRoot = viewRoot
      zoomIngress = []
      clipboard = None
      desktopCapabilities = None
      serverCapabilities = None
      desktopFileIndicator = BlankFileIndicator
      workspaceMappedLabels = Set.empty
      workspaceSyncFacts = Map.empty
      syncInfo = SyncInfo.initial
      lastCmdResult = None }
