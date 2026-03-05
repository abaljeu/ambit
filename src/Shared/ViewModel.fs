namespace Gambol.Shared

type Mode =
    | Selecting
    | Editing of originalText: string * cursorPos: int option
    // cursorPos: None = place cursor at end; Some n = place cursor at position n

/// A NodeRange with a focus index marking the "active" end used for Shift-Arrow and editing.
/// focus is always range.start or range.endd - 1.
type Selection =
    { range: NodeRange
      focus: int }

/// A rendered appearance of a node in the outline. Each appearance gets a unique
/// instanceId so that fold state is per-occurrence, not per-NodeId.
/// In a DAG the same NodeId may have multiple SiteNode instances.
type SiteNode =
    { instanceId: int
      nodeId: NodeId
      expanded: bool
      children: SiteNode list }

/// Self-contained snapshot of copied/cut nodes for internal clipboard.
/// Independent of graph.nodes — survives graph mutations and snapshot reload.
type ClipboardContent =
    { topLevelIds: NodeId list
      nodes: Map<NodeId, Node> }

// Server State is in FileAgent, and mainly the graph.
type Model = // the client state
    { graph: Graph // the core data
      revision: Revision
      selectedNodes: Selection option
      mode: Mode
      siteRoot: SiteNode
      nextInstanceId: int
      clipboard: ClipboardContent option
      linkPasteEnabled: bool }

type Msg =
    | StateLoaded of Graph * Revision
    | SelectRow of NodeId
    | MoveSelectionUp
    | MoveSelectionDown
    | ShiftArrowUp
    | ShiftArrowDown
    | StartEdit of prefill: string
    | SplitNode of currentText: string * cursorPos: int
    | IndentSelection
    | OutdentSelection
    | MoveNodeUp
    | MoveNodeDown
    | CancelEdit
    | SubmitResponse of Revision
    | PasteNodes of pastedText: string
    | CopySelection
    | CutSelection
    | ToggleFold of instanceId: int
    | ToggleLinkPaste
