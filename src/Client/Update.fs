module Gambol.Client.Update

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Shared
open Gambol.Shared.Paste
open Gambol.Shared.ViewModel
open Thoth.Json.Core

// ---------------------------------------------------------------------------
// JS interop
// ---------------------------------------------------------------------------

[<Emit("fetch($0,{method:'POST',headers:{'Content-Type':'application/json'},body:$1}).then(r=>r.text()).then($2)")>]
let postJson (url: string) (body: string) (callback: string -> unit) : unit = jsNative

// ---------------------------------------------------------------------------
// File identity (derived from URL path)
// ---------------------------------------------------------------------------

let currentFile =
    let path = Browser.Dom.window.location.pathname
    if path.StartsWith("/") then path.Substring(1) else path

// ---------------------------------------------------------------------------
// Encoding / decoding helpers
// ---------------------------------------------------------------------------

/// Encode a Change as compact JSON for POST /{file}/changes
let encodeChangeBody (change: Change) : string =
    Thoth.Json.JavaScript.Encode.toString 0 (Serialization.encodeChange change)

/// Decode the response from GET /{file}/state or POST /{file}/changes
let decodeStateResponse (text: string) : Result<Graph * Revision, string> =
    let decoder =
        Decode.object (fun get ->
            let g = get.Required.Field "graph" Serialization.decodeGraph
            let r = get.Required.Field "revision" Serialization.decodeRevision
            g, r)
    Thoth.Json.JavaScript.Decode.fromString decoder text

// ---------------------------------------------------------------------------
// Update helpers
// ---------------------------------------------------------------------------

/// Read the edit input value from the DOM (impure — pragmatic for MVP)
let readEditInputValue () : string =
    let el = document.getElementById "edit-input"
    if isNull el then ""
    else (el :?> HTMLInputElement).value

/// Read the edit input cursor position from the DOM.
let readEditInputCursor () : int =
    let el = document.getElementById "edit-input"
    if isNull el then 0
    else int (el :?> HTMLInputElement).selectionStart

/// Apply a change to the local graph, POST it to the server in the background,
/// and return the updated graph (or None if the change was rejected locally).
let applyAndPost (change: Change) (model: VM) (dispatch: Msg -> unit) : (Graph * History) option =
    let state: State = { graph = model.graph; revision = model.revision; history = model.history }
    match History.applyChange change state with
    | ApplyResult.Changed newState ->
        let body = encodeChangeBody change
        postJson $"/{currentFile}/changes" body (fun responseText ->
            match decodeStateResponse responseText with
            | Ok (_graph, rev) -> dispatch (SubmitResponse rev)
            | Error _err -> ()
        )
        Some (newState.graph, newState.history)
    | _ -> None

/// Extract the list of node IDs covered by a SiteNodeRange.
let rangeChildren (graph: Graph) (range: SiteNodeRange) =
    graph.nodes.[range.parent.nodeId].children |> List.skip range.start |> List.take (range.endd - range.start)

/// Apply a committed text edit to the model and POST to server.
/// Returns the updated model. Dispatches SubmitResponse asynchronously.
let commitTextEdit
    (nodeId: NodeId)
    (originalText: string)
    (newText: string)
    (model: VM)
    (dispatch: Msg -> unit)
    : VM =
    if newText = originalText then
        { model with mode = Selecting }
    else
        let change: Change = { id = model.revision.Value; ops = [ Op.SetText(nodeId, originalText, newText) ] }
        match applyAndPost change model dispatch with
        | Some (graph, history) -> { model with graph = graph; history = history; mode = Selecting }
        | None                  -> { model with mode = Selecting }

/// Split the currently-edited node at the cursor position.
///
/// cursor at 0   → blank sibling inserted above; current node keeps its text; focus at start of current node.
/// cursor > 0    → current node gets text-before; new sibling gets text-after; focus at start of new node.
let splitNode (currentText: string) (cursorPos: int) (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode, model.selectedNodes with
    | Editing (originalText, _), None ->
        // Root is being edited: commit text, no split
        commitTextEdit model.graph.root originalText (readEditInputValue ()) model dispatch
    | Editing (originalText, _), Some sel ->
        // The node being edited is the focus node.
        let selectedId  = focusedNodeId model.graph sel
        let parentId    = sel.range.parent.nodeId
        let indexInParent = sel.focus
        let clampedPos = max 0 (min cursorPos currentText.Length)
        let textBefore = currentText.[..clampedPos - 1]
        let textAfter  = currentText.[clampedPos..]
        let newNodeId  = NodeId.New()

        let (insertIndex, newNodeText, focusId, focusText) =
            if clampedPos = 0 then
                // blank node above; focus stays on current node
                (indexInParent, "", selectedId, currentText)
            else
                // new node after; focus moves to new node
                (indexInParent + 1, textAfter, newNodeId, textAfter)

        let ops =
            [ yield Op.NewNode(newNodeId, newNodeText)
              yield Op.Replace(parentId, insertIndex, [], [newNodeId])
              // update current node's text only when it actually changes
              let updatedText = if clampedPos = 0 then currentText else textBefore
              if updatedText <> originalText then
                  yield Op.SetText(selectedId, originalText, updatedText) ]

        let change: Change = { id = model.revision.Value; ops = ops }
        match applyAndPost change model dispatch with
        | Some (graph, history) ->
            { model with
                graph = graph
                history = history
                selectedNodes = singleSelection graph model.siteMap focusId
                mode = Editing (focusText, Some 0) }
        | None -> model
    | _ -> model


// ---------------------------------------------------------------------------
// Paste
// ---------------------------------------------------------------------------

/// Handle a PasteNodes message.
///
/// Select mode: replaces the current selection with the pasted subtree.
/// Edit mode:   splices the first pasted line into the node at the cursor;
///              remaining top-level lines become siblings below the current node.
let pasteNodes (pastedText: string) (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let entries = parsePasteText pastedText
        if entries.IsEmpty then model
        else
        match model.mode with
        | Selecting ->
            let (topLevelIds, pasteOps) = buildPasteOps entries
            if topLevelIds.IsEmpty then model
            else
            let range = sel.range
            let selectedIds = rangeChildren model.graph range
            let replaceOp = Op.Replace(range.parent.nodeId, range.start, selectedIds, topLevelIds)
            let change = { id = model.revision.Value; ops = pasteOps @ [replaceOp] }
            match applyAndPost change model dispatch with
            | Some (graph, history) ->
                let newEnd = range.start + topLevelIds.Length
                let newSel = { range = { parent = range.parent; start = range.start; endd = newEnd }; focus = range.start }
                { model with graph = graph; history = history; selectedNodes = Some newSel }
            | None -> model
        | Editing (originalText, _) ->
            let currentText = readEditInputValue ()
            let cursorPos   = readEditInputCursor ()
            let focusId  = focusedNodeId model.graph sel
            let parentId = sel.range.parent.nodeId
            let focusIdx = sel.focus
            match entries with
            | [] -> model
            | [(firstText, _)] ->
                // Single pasted line: splice into current node at cursor, no new nodes
                let newText = currentText.[..cursorPos - 1] + firstText + currentText.[cursorPos..]
                if newText = originalText then { model with mode = Selecting }
                else
                let change = { id = model.revision.Value; ops = [ Op.SetText(focusId, originalText, newText) ] }
                match applyAndPost change model dispatch with
                | Some (graph, history) -> { model with graph = graph; history = history; mode = Selecting }
                | None -> model
            | (firstText, _) :: rest ->
                // Multi-line: splice first line at cursor; remaining become siblings below
                let newText = currentText.[..cursorPos - 1] + firstText + currentText.[cursorPos..]
                let setTextOps =
                    if newText <> originalText then [ Op.SetText(focusId, originalText, newText) ] else []
                let (remainingTopIds, remainingOps) = buildPasteOps rest
                let insertOps =
                    if remainingTopIds.IsEmpty then []
                    else [ Op.Replace(parentId, focusIdx + 1, [], remainingTopIds) ]
                let allOps = setTextOps @ remainingOps @ insertOps
                if allOps.IsEmpty then { model with mode = Selecting }
                else
                let change = { id = model.revision.Value; ops = allOps }
                match applyAndPost change model dispatch with
                | Some (graph, history) -> { model with graph = graph; history = history; mode = Selecting }
                | None -> model

// ---------------------------------------------------------------------------
// Indent / Outdent
// ---------------------------------------------------------------------------

/// Tab: make selected nodes children of the sibling immediately before them.
/// No-op if the selection starts at index 0 (no previous sibling).
/// Move the selected nodes from their current parent to a new parent at insertIdx.
/// Common core of indent and outdent.
let reparentSelection (newParentEntry: SiteEntry) (insertIdx: int) (sel: Selection) (model: VM) (dispatch: Msg -> unit) : VM =
    let range = sel.range
    let selectedIds = rangeChildren model.graph range
    let ops =
        [ Op.Replace(range.parent.nodeId, range.start, selectedIds, [])
          Op.Replace(newParentEntry.nodeId, insertIdx, [], selectedIds) ]
    let change = { id = model.revision.Value; ops = ops }
    match applyAndPost change model dispatch with
    | Some (graph, history) ->
        let newSel = { range = { parent = newParentEntry; start = insertIdx; endd = insertIdx + selectedIds.Length }; focus = insertIdx }
        { model with graph = graph; history = history; selectedNodes = Some newSel }
    | None -> model

let indentSelection (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel when sel.range.start = 0 -> model  // no previous sibling — no-op
    | Some sel ->
        let prevSibId = model.graph.nodes.[sel.range.parent.nodeId].children.[sel.range.start - 1]
        let insertIdx = model.graph.nodes.[prevSibId].children.Length
        match model.siteMap.entries |> Map.tryPick (fun _ e -> if e.nodeId = prevSibId then Some e else None) with
        | None -> model
        | Some prevSibEntry ->
        let result = reparentSelection prevSibEntry insertIdx sel model dispatch
        // Ensure the new parent is expanded so the indented items are visible after reconcile
        if prevSibEntry.expanded then result
        else
            let siteMap, nextId = ViewModel.expandEntry prevSibEntry.instanceId result.graph result.siteMap result.nextInstanceId
            { result with siteMap = siteMap; nextInstanceId = nextId }

let outdentSelection (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        match Graph.tryFindParentAndIndex sel.range.parent.nodeId model.graph with
        | None -> model  // parent is root — no-op
        | Some (grandparentId, parentIdx) ->
            match model.siteMap.entries |> Map.tryPick (fun _ e -> if e.nodeId = grandparentId then Some e else None) with
            | None -> model
            | Some grandparentEntry ->
            reparentSelection grandparentEntry (parentIdx + 1) sel model dispatch

/// If currently editing, commit the edit and return Selecting model; otherwise return model as-is.
let commitIfEditing (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode, model.selectedNodes with
    | Editing (originalText, _), None ->
        commitTextEdit model.graph.root originalText (readEditInputValue ()) model dispatch
    | Editing (originalText, _), Some sel ->
        let editingId = focusedNodeId model.graph sel
        commitTextEdit editingId originalText (readEditInputValue ()) model dispatch
    | _ -> model

/// CutSelection: store clipboard content, remove selected nodes, update selection.
/// Post-cut priority: sibling after > sibling before > parent.
let cutSelection (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let selectedIds = rangeChildren model.graph sel.range
        let cb = collectSubtree model.graph model.siteMap selectedIds
        let removeOp = Op.Replace(sel.range.parent.nodeId, sel.range.start, selectedIds, [])
        let change = { id = model.revision.Value; ops = [removeOp] }
        match applyAndPost change model dispatch with
        | Some (graph, history) ->
            let newChildren = graph.nodes.[sel.range.parent.nodeId].children
            let newSel =
                if sel.range.start < newChildren.Length then
                    let i = sel.range.start
                    Some { range = { parent = sel.range.parent; start = i; endd = i + 1 }; focus = i }
                elif sel.range.start > 0 then
                    let i = sel.range.start - 1
                    Some { range = { parent = sel.range.parent; start = i; endd = i + 1 }; focus = i }
                else
                    singleSelection graph model.siteMap sel.range.parent.nodeId
            { model with graph = graph; history = history; clipboard = Some cb; selectedNodes = newSel }
        | None -> model

/// Alt+Up/Down: swap the selected range with the adjacent sibling using a single Op.Replace.
let moveNode (delta: int) (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let range       = sel.range
        let parentNode  = model.graph.nodes.[range.parent.nodeId]
        let selectedIds = rangeChildren model.graph range
        let swapOpt =
            if delta < 0 && range.start > 0 then
                let sib = parentNode.children.[range.start - 1]
                Some (range.start - 1, sib :: selectedIds, selectedIds @ [sib], range.start - 1)
            elif delta > 0 && range.endd < parentNode.children.Length then
                let sib = parentNode.children.[range.endd]
                Some (range.start, selectedIds @ [sib], sib :: selectedIds, range.start + 1)
            else None
        match swapOpt with
        | None -> model
        | Some (opStart, oldSpan, newSpan, newStart) ->
            let change = { id = model.revision.Value; ops = [ Op.Replace(range.parent.nodeId, opStart, oldSpan, newSpan) ] }
            match applyAndPost change model dispatch with
            | Some (graph, history) ->
                let newSel = { range = { range with start = newStart; endd = newStart + selectedIds.Length }; focus = sel.focus + delta }
                { model with graph = graph; history = history; selectedNodes = Some newSel }
            | None -> model

// ---------------------------------------------------------------------------
// Update
// ---------------------------------------------------------------------------

/// Rebuild the site map after a graph mutation, preserving fold states.
let withSiteMap (model: VM) : VM =
    let siteMap, nextId = ViewModel.reconcileSiteMap model.graph model.siteMap model.nextInstanceId
    { model with siteMap = siteMap; nextInstanceId = nextId }

/// Join the currently-edited node with the previous visible (inorder) node.
/// 1. If current has no children: append current's text to prev, delete current.
/// 2. If current and prev both have children: abort.
/// 3. If current has children but prev does not: move current's children to prev, then do 1.
/// Cursor lands at the join point (end of prevText) in prev.
let joinWithPrevious (currentText: string) (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode, model.selectedNodes with
    | Editing _, Some sel ->
        let currentId = focusedNodeId model.graph sel
        let rows = getVisibleRowIds model.siteMap
        match rows |> List.tryFindIndex ((=) currentId) with
        | None | Some 0 -> model
        | Some currentIndex ->
            let prevId = rows.[currentIndex - 1]
            let prevNode = model.graph.nodes.[prevId]
            let currentNode = model.graph.nodes.[currentId]
            if not currentNode.children.IsEmpty && not prevNode.children.IsEmpty then model
            else
                match Graph.tryFindParentAndIndex currentId model.graph with
                | None -> model
                | Some (parentId, indexInParent) ->
                    let joinedText = prevNode.text + currentText
                    let cursorPos = prevNode.text.Length
                    let ops =
                        [ if joinedText <> prevNode.text then
                              yield Op.SetText(prevId, prevNode.text, joinedText)
                          if not currentNode.children.IsEmpty then
                              yield Op.Replace(prevId, prevNode.children.Length, [], currentNode.children)
                          yield Op.Replace(parentId, indexInParent, [currentId], []) ]
                    let change = { id = model.revision.Value; ops = ops }
                    match applyAndPost change model dispatch with
                    | None -> model
                    | Some (graph, history) ->
                        let result = withSiteMap { model with graph = graph; history = history }
                        { result with
                            mode = Editing (joinedText, Some cursorPos)
                            selectedNodes = singleSelection result.graph result.siteMap prevId }
    | _ -> model

/// Update function. The dispatch parameter is needed for async effects
/// (server POST callbacks).
let update (msg: Msg) (model: VM) (dispatch: Msg -> unit) : VM =
    match msg with
    | StateLoaded (graph, revision) ->
        let siteMap, nextId = ViewModel.buildSiteMap graph 0
        { graph = graph
          revision = revision
          history = History.empty
          selectedNodes = None
          mode = Selecting
          siteMap = siteMap
          nextInstanceId = nextId
          clipboard = None
          linkPasteEnabled = false }

    | SelectRow nodeId ->
        let result =
            match model.mode, model.selectedNodes with
            | Editing (originalText, _), Some sel ->
                // Commit current edit, then select new row
                let editingId = focusedNodeId model.graph sel
                let newText = readEditInputValue ()
                let model' = commitTextEdit editingId originalText newText model dispatch
                { model' with selectedNodes = singleSelection model'.graph model'.siteMap nodeId }
            | _ ->
                { model with selectedNodes = singleSelection model.graph model.siteMap nodeId; mode = Selecting }
        if not (System.Object.ReferenceEquals(result.graph, model.graph)) then withSiteMap result else result

    | MoveSelectionUp ->
        let result =
            match model.mode with
            | Editing _ -> moveSelectionBy -1 (commitIfEditing model dispatch)
            | Selecting -> applyMoveSelectionUp model
        if not (System.Object.ReferenceEquals(result.graph, model.graph)) then withSiteMap result else result

    | MoveSelectionDown ->
        let result =
            match model.mode with
            | Editing _ -> moveSelectionBy 1 (commitIfEditing model dispatch)
            | Selecting -> applyMoveSelectionDown model
        if not (System.Object.ReferenceEquals(result.graph, model.graph)) then withSiteMap result else result

    | StartEdit _prefill ->
        match model.selectedNodes with
        | None ->
            let root = model.graph.nodes.[model.graph.root]
            { model with mode = Editing (root.text, None) }
        | Some sel ->
            let nodeId = focusedNodeId model.graph sel
            let node = model.graph.nodes.[nodeId]
            { model with mode = Editing (node.text, None) }  // None = cursor at end

    | ShiftArrowUp   -> shiftArrow -1 model
    | ShiftArrowDown -> shiftArrow  1 model

    | SplitNode (currentText, cursorPos) ->
        splitNode currentText cursorPos model dispatch |> withSiteMap

    | JoinWithPrevious currentText ->
        joinWithPrevious currentText model dispatch

    | IndentSelection  -> indentSelection  (commitIfEditing model dispatch) dispatch |> withSiteMap
    | OutdentSelection -> outdentSelection (commitIfEditing model dispatch) dispatch |> withSiteMap
    | MoveNodeUp       -> moveNode -1      (commitIfEditing model dispatch) dispatch |> withSiteMap
    | MoveNodeDown     -> moveNode  1      (commitIfEditing model dispatch) dispatch |> withSiteMap

    | CancelEdit ->
        match model.mode with
        | Editing _ ->
            // In edit mode: revert text, return to selection mode
            { model with mode = Selecting }
        | Selecting ->
            // In selection mode: deselect
            { model with selectedNodes = None }

    | SubmitResponse revision ->
        { model with
            revision = revision
            history = { model.history with nextId = max model.history.nextId revision.Value } }

    | PasteNodes pastedText ->
        pasteNodes pastedText model dispatch |> withSiteMap

    | CopySelection ->
        match model.selectedNodes with
        | None -> model
        | Some sel ->
            let selectedIds = rangeChildren model.graph sel.range
            { model with clipboard = Some (collectSubtree model.graph model.siteMap selectedIds) }

    | CutSelection ->
        cutSelection model dispatch |> withSiteMap

    | ToggleFold instanceId ->
        match Map.tryFind instanceId model.siteMap.entries with
        | None -> model
        | Some entry ->
            if entry.expanded then
                { model with siteMap = ViewModel.toggleFold instanceId model.siteMap }
            else
                let siteMap, nextId = ViewModel.expandEntry instanceId model.graph model.siteMap model.nextInstanceId
                { model with siteMap = siteMap; nextInstanceId = nextId }

    | ToggleFoldSelection ->
        match model.selectedNodes with
        | None -> model
        | Some sel ->
            let selectedInstIds =
                sel.range.parent.children
                |> List.skip sel.range.start
                |> List.take (sel.range.endd - sel.range.start)
            let anyExpanded =
                selectedInstIds |> List.exists (fun instId ->
                    match Map.tryFind instId model.siteMap.entries with
                    | Some entry -> entry.expanded
                    | None -> false)
            if anyExpanded then
                let siteMap =
                    selectedInstIds |> List.fold (fun sm instId -> ViewModel.toggleFold instId sm) model.siteMap
                { model with siteMap = siteMap }
            else
                let siteMap, nextId =
                    selectedInstIds |> List.fold
                        (fun (sm, nid) instId -> ViewModel.expandEntry instId model.graph sm nid)
                        (model.siteMap, model.nextInstanceId)
                { model with siteMap = siteMap; nextInstanceId = nextId }

    | ToggleLinkPaste ->
        { model with linkPasteEnabled = not model.linkPasteEnabled }

    | Undo ->
        let model' = commitIfEditing model dispatch
        let state = { graph = model'.graph; history = model'.history; revision = model'.revision }
        match model'.history.past |> List.tryHead with
        | None -> model'
        | Some headChange ->
            match History.undo state with
            | ApplyResult.Changed newState ->
                let invertedChange = { Change.invert headChange with id = model'.history.nextId }
                let body = encodeChangeBody invertedChange
                postJson $"/{currentFile}/changes" body (fun responseText ->
                    match decodeStateResponse responseText with
                    | Ok (_, rev) -> dispatch (SubmitResponse rev)
                    | Error _ -> ()
                )
                { model' with graph = newState.graph; history = newState.history; mode = Selecting }
                |> withSiteMap
            | _ -> model'

    | Redo ->
        let model' = commitIfEditing model dispatch
        let state = { graph = model'.graph; history = model'.history; revision = model'.revision }
        match model'.history.future |> List.tryHead with
        | None -> model'
        | Some headChange ->
            match History.redo state with
            | ApplyResult.Changed newState ->
                let reChange = { headChange with id = model'.history.nextId }
                let body = encodeChangeBody reChange
                postJson $"/{currentFile}/changes" body (fun responseText ->
                    match decodeStateResponse responseText with
                    | Ok (_, rev) -> dispatch (SubmitResponse rev)
                    | Error _ -> ()
                )
                { model' with graph = newState.graph; history = newState.history; mode = Selecting }
                |> withSiteMap
            | _ -> model'
