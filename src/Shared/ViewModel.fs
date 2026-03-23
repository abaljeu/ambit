namespace Gambol.Shared

type Mode =
    | Selecting
    | Editing of originalText: string * cursorPos: int option
    // cursorPos: None = place cursor at end; Some n = place cursor at position n
    | CommandPalette of query: string * selectedCommand: int * returnTo: Mode
    | CssClassPrompt of returnTo: Mode * initialValue: string

type SiteId = Sid of int

/// A rendered appearance of a node in a flat site map. Each appearance gets a unique
/// instanceId so that fold state is per-occurrence, not per-NodeId.
/// In a DAG (including cyclic graphs) the same NodeId may appear multiple times with
/// independent fold states. Cycle termination relies on lazy expansion: a new entry
/// starts collapsed with children = [], so recursion stops naturally.
type SiteEntry =
    { instanceId: SiteId
      nodeId: NodeId
      parentInstanceId: SiteId option   // None = root
      expanded: bool
      childrenStale: bool            // true when children list may not match graph; re-synced on expand
      children: SiteId list }        // instanceId list, ordered to match graph.children (valid when not stale)

/// Flat map keyed by instanceId. O(log S) per-entry access for all operations.
type SiteMap =
    { rootId: SiteId
      entries: Map<SiteId, SiteEntry> }

/// A contiguous span of children under a specific site-map occurrence of a parent node.
/// parent is a SiteEntry (not just a NodeId) so the selection is unambiguous in a DAG
/// where the same NodeId may appear at multiple positions.
type SiteNodeRange =
    { parent: SiteEntry
      start: int
      endd: int }

[<RequireQualifiedAccess>]
module SiteNodeRange =
    /// The SiteEntry for the first node in the range, if in bounds.
    let firstChild (range: SiteNodeRange) (siteMap: SiteMap) : SiteEntry option =
        range.parent.children
        |> List.tryItem range.start
        |> Option.bind (fun id -> Map.tryFind id siteMap.entries)

    /// The SiteEntry for the last node in the range, if in bounds.
    let lastChild (range: SiteNodeRange) (siteMap: SiteMap) : SiteEntry option =
        if range.endd > 0 then
            range.parent.children
            |> List.tryItem (range.endd - 1)
            |> Option.bind (fun id -> Map.tryFind id siteMap.entries)
        else
            None

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
    | Synced      // all changes confirmed by server
    | Inactive    // client paused remote polling due to idle/hidden
    | Syncing of int   // POST in-flight; int = attempt number (1-based)
    | Pending of int  // last POST failed; int = failure count (1..10; stop auto-retry at 10)
    | Conflicted  // received 409; rebase in progress
    | Stale       // server has changes we don't have — refresh to see them

// Server `State` is in `FileAgent`, and mainly the graph.
type VM = // the client state
    { graph: Graph // the core data
      revision: Revision
      history: History
      selectedNodes: Selection option
      mode: Mode
      siteMap: SiteMap
      nextSiteId: SiteId
      zoomRoot: NodeId option   // None = display from graph.root; Some id = display rooted at that node
      clipboard: ClipboardContent option
      pendingChanges: Change list  // FIFO queue of changes awaiting server confirmation
      syncState: SyncState }

/// Messages dispatched by async server callbacks (not directly caused by user input).
type SystemMsg =
    | StateLoaded of Graph * Revision
    | SubmitResponse of Revision
    | SubmitFailed
    | ServerAhead of Revision  // poll found server revision > client; view is stale
    | PollingInactive
    | PollingActive

type Msg =
    | SysMsg of SystemMsg
