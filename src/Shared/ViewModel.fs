namespace Gambol.Shared

type Mode =
    | Selecting
    | Editing of originalText: string * prefill: string option * cursorPos: int option
    // prefill: the initial text shown in the edit input (if different from originalText)
    // cursorPos: None = place cursor at end; Some n = place cursor at position n

/// A rendered appearance of a node in a flat site map. Each appearance gets a unique
/// instanceId so that fold state is per-occurrence, not per-NodeId.
/// In a DAG (including cyclic graphs) the same NodeId may appear multiple times with
/// independent fold states. Cycle termination relies on lazy expansion: a new entry
/// starts collapsed with children = [], so recursion stops naturally.
type SiteEntry =
    { instanceId: int
      nodeId: NodeId
      parentInstanceId: int option   // None = root
      expanded: bool
      childrenStale: bool            // true when children list may not match graph; re-synced on expand
      children: int list }           // instanceId list, ordered to match graph.children (valid when not stale)

/// Flat map keyed by instanceId. O(log S) per-entry access for all operations.
type SiteMap =
    { rootId: int
      entries: Map<int, SiteEntry> }

/// A contiguous span of children under a specific site-map occurrence of a parent node.
/// parent is a SiteEntry (not just a NodeId) so the selection is unambiguous in a DAG
/// where the same NodeId may appear at multiple positions.
type SiteNodeRange =
    { parent: SiteEntry
      start: int
      endd: int }

/// A SiteNodeRange with a focus index marking the "active" end used for Shift-Arrow and editing.
/// focus is always range.start or range.endd - 1.
type Selection =
    { range: SiteNodeRange
      focus: int }

/// Self-contained snapshot of copied/cut nodes for internal clipboard.
/// Independent of graph.nodes — survives graph mutations and snapshot reload.
type ClipboardContent =
    { topLevelIds: NodeId list
      nodes: Map<NodeId, Node> }

type SyncState =
    | Synced    // all changes confirmed by server
    | Syncing   // a POST is currently in-flight
    | Pending   // last POST failed; changes queued, awaiting user retry

// Server `State` is in `FileAgent`, and mainly the graph.
type VM = // the client state
    { graph: Graph // the core data
      revision: Revision
      history: History
      selectedNodes: Selection option
      mode: Mode
      siteMap: SiteMap
      nextInstanceId: int
      zoomRoot: NodeId option   // None = display from graph.root; Some id = display rooted at that node
      clipboard: ClipboardContent option
      linkPasteEnabled: bool
      pendingChanges: Change list  // FIFO queue of changes awaiting server confirmation
      syncState: SyncState }

/// Messages dispatched by async server callbacks (not directly caused by user input).
type SystemMsg =
    | StateLoaded of Graph * Revision
    | SubmitResponse of Revision
    | SubmitFailed

type Msg =
    | System of SystemMsg
