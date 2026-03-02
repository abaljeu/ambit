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

type Model =
    { graph: Graph
      revision: Revision
      selectedNodes: Selection option
      mode: Mode }

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

// ---------------------------------------------------------------------------
// Pure view-model helpers (no DOM / Fable interop)
// ---------------------------------------------------------------------------

module ViewModel =

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

    /// Flatten graph into visible row order (preorder, excluding root).
    let getVisibleRowIds (graph: Graph) : NodeId list =
        let rec gather (nodeId: NodeId) : NodeId list =
            let node = graph.nodes.[nodeId]
            nodeId :: (node.children |> List.collect gather)

        let root = graph.nodes.[graph.root]
        root.children |> List.collect gather

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
            let rows = getVisibleRowIds model.graph
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
