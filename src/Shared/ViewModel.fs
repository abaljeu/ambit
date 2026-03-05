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

type Model =
    { graph: Graph
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

// ---------------------------------------------------------------------------
// Pure view-model helpers (no DOM / Fable interop)
// ---------------------------------------------------------------------------

module ViewModel =

    /// Create a placeholder SiteNode for an empty/unloaded graph.
    let emptySiteRoot : SiteNode =
        { instanceId = 0
          nodeId = NodeId(System.Guid.Empty)
          expanded = true
          children = [] }

    /// Build a full SiteNode tree from a graph.  All nodes start collapsed
    /// except the root (which is always expanded).  Returns the root SiteNode
    /// and the next available instanceId.
    let buildSiteTree (graph: Graph) : SiteNode * int =
        let mutable counter = 0
        let nextId () =
            let id = counter
            counter <- counter + 1
            id
        let rec build (nodeId: NodeId) (isRoot: bool) : SiteNode =
            let node = graph.nodes.[nodeId]
            let id = nextId ()
            { instanceId = id
              nodeId = nodeId
              expanded = isRoot
              children = node.children |> List.map (fun cid -> build cid false) }
        let root = build graph.root true
        root, counter

    /// Reconcile the site tree after a graph change.  Preserves existing
    /// instanceIds and fold states for nodes that still exist; assigns new IDs
    /// (starting from nextId) for newly-added nodes.  New nodes default to
    /// collapsed.  The root is always expanded.
    let rebuildSiteTree (graph: Graph) (oldRoot: SiteNode) (nextId: int) : SiteNode * int =
        // Build lookup: NodeId -> old SiteNode (Phase 1 = 1:1)
        let oldMap =
            let rec collect (node: SiteNode) (acc: Map<NodeId, SiteNode>) =
                let acc' = Map.add node.nodeId node acc
                node.children |> List.fold (fun a child -> collect child a) acc'
            collect oldRoot Map.empty
        let mutable counter = nextId
        let freshId () =
            let id = counter
            counter <- counter + 1
            id
        let rec build (nodeId: NodeId) (isRoot: bool) : SiteNode =
            let node = graph.nodes.[nodeId]
            let (instId, expanded) =
                match Map.tryFind nodeId oldMap with
                | Some old -> (old.instanceId, old.expanded)
                | None     -> (freshId (), false)
            { instanceId = instId
              nodeId = nodeId
              expanded = if isRoot then true else expanded
              children = node.children |> List.map (fun cid -> build cid false) }
        let root = build graph.root true
        root, counter

    /// Toggle the expanded flag of the SiteNode with the given instanceId.
    let toggleFold (instanceId: int) (siteRoot: SiteNode) : SiteNode =
        let rec toggle (node: SiteNode) : SiteNode =
            if node.instanceId = instanceId then
                { node with expanded = not node.expanded }
            else
                { node with children = node.children |> List.map toggle }
        toggle siteRoot

    /// Ensure the first SiteNode matching the given NodeId is expanded.
    let expandNodeId (nodeId: NodeId) (siteRoot: SiteNode) : SiteNode =
        let rec go (node: SiteNode) : SiteNode =
            if node.nodeId = nodeId then
                { node with expanded = true }
            else
                { node with children = node.children |> List.map go }
        go siteRoot

    /// Find parent and index of a node in the current graph.
    let tryFindParentAndIndex (graph: Graph) (targetId: NodeId) : (NodeId * int) option =
        graph.nodes
        |> Map.toSeq
        |> Seq.tryPick (fun (parentId, parent) ->
            parent.children
            |> List.tryFindIndex ((=) targetId)
            |> Option.map (fun index -> parentId, index))

    /// Build a single-node Selection for the given nodeId, using the graph to locate its parent.
    /// Returns None if the node has no parent (i.e. it is the root).
    let singleSelection (graph: Graph) (nodeId: NodeId) : Selection option =
        tryFindParentAndIndex graph nodeId
        |> Option.map (fun (parentId, index) ->
            { range = { parent = parentId; start = index; endd = index + 1 }; focus = index })

    /// Extract the first (start) selected NodeId from a Selection.
    let firstSelectedNodeId (graph: Graph) (sel: Selection) : NodeId =
        graph.nodes.[sel.range.parent].children.[sel.range.start]

    /// Extract the focused NodeId from a Selection (the active end, used for editing and Arrow movement).
    let focusedNodeId (graph: Graph) (sel: Selection) : NodeId =
        graph.nodes.[sel.range.parent].children.[sel.focus]

    /// Flatten site tree into visible row order (preorder, excluding root).
    /// Respects fold state: only recurses into expanded nodes.
    let getVisibleRowIds (siteRoot: SiteNode) : NodeId list =
        let rec gather (siteNode: SiteNode) : NodeId list =
            siteNode.nodeId ::
                (if siteNode.expanded then
                     siteNode.children |> List.collect gather
                 else [])
        siteRoot.children |> List.collect gather

    /// Shift-Arrow: move the focused end of the range by delta (-1 = up, +1 = down).
    /// For a single-node selection, always extends. For multi-node, the focused end moves.
    /// Focus follows the moved end. No-op if the move would exceed parent bounds.
    let shiftArrow (delta: int) (model: Model) : Model =
        match model.selectedNodes with
        | None -> model
        | Some sel ->
            let range = sel.range
            let childCount = model.graph.nodes.[range.parent].children.Length
            let rangeSize = range.endd - range.start
            if rangeSize = 1 then
                // Single node: always extend in the arrow direction
                if delta < 0 then
                    let newStart = range.start - 1
                    if newStart < 0 then model
                    else { model with selectedNodes = Some { range = { range with start = newStart }; focus = newStart } }
                else
                    let newEndd = range.endd + 1
                    if newEndd > childCount then model
                    else { model with selectedNodes = Some { range = { range with endd = newEndd }; focus = newEndd - 1 } }
            else
                let focusAtStart = sel.focus = range.start
                if delta < 0 then
                    if focusAtStart then
                        // extend upward
                        let newStart = range.start - 1
                        if newStart < 0 then model
                        else { model with selectedNodes = Some { range = { range with start = newStart }; focus = newStart } }
                    else
                        // shrink from bottom
                        let newEndd = range.endd - 1
                        if newEndd <= range.start then model
                        else { model with selectedNodes = Some { range = { range with endd = newEndd }; focus = newEndd - 1 } }
                else
                    if focusAtStart then
                        // shrink from top
                        let newStart = range.start + 1
                        if newStart >= range.endd then model
                        else { model with selectedNodes = Some { range = { range with start = newStart }; focus = newStart } }
                    else
                        // extend downward
                        let newEndd = range.endd + 1
                        if newEndd > childCount then model
                        else { model with selectedNodes = Some { range = { range with endd = newEndd }; focus = newEndd - 1 } }

    /// Collapse a multi-node selection to a single-node selection at the focus node, without moving.
    let collapseToFocus (model: Model) : Model =
        match model.selectedNodes with
        | None -> model
        | Some sel ->
            let focusId = focusedNodeId model.graph sel
            match singleSelection model.graph focusId with
            | None -> model
            | Some newSel -> { model with selectedNodes = Some newSel }

    /// Move current selection by delta (-1 for up, +1 for down) in visible row order.
    /// Collapses any multi-node selection to the focus node, then moves from there.
    /// The resulting selection is always a single-node Selection.
    let moveSelectionBy (delta: int) (model: Model) : Model =
        match model.selectedNodes with
        | None -> model
        | Some sel ->
            let anchorId = focusedNodeId model.graph sel
            let rows = getVisibleRowIds model.siteRoot
            match rows |> List.tryFindIndex ((=) anchorId) with
            | None -> model
            | Some currentIndex ->
                let nextIndex = currentIndex + delta
                if nextIndex < 0 || nextIndex >= rows.Length then
                    model
                else
                    let nextId = rows[nextIndex]
                    match singleSelection model.graph nextId with
                    | None -> model
                    | Some newSel -> { model with selectedNodes = Some newSel; mode = Selecting }

    /// Pure portion of MoveSelectionUp: handles the non-editing cases.
    /// When focus is not at the range start, moves focus to start (keep range).
    /// Otherwise, moves the whole selection up by one visible row.
    let applyMoveSelectionUp (model: Model) : Model =
        match model.selectedNodes with
        | Some sel when sel.focus > sel.range.start ->
            { model with selectedNodes = Some { sel with focus = sel.range.start } }
        | _ ->
            moveSelectionBy -1 model

    /// Pure portion of MoveSelectionDown: handles the non-editing cases.
    /// When focus is not at the range end, moves focus to end (keep range).
    /// Otherwise, moves the whole selection down by one visible row.
    let applyMoveSelectionDown (model: Model) : Model =
        match model.selectedNodes with
        | Some sel when sel.focus < sel.range.endd - 1 ->
            { model with selectedNodes = Some { sel with focus = sel.range.endd - 1 } }
        | _ ->
            moveSelectionBy 1 model
