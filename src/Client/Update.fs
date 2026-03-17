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

[<Emit("fetch($0,{method:'POST',headers:{'Content-Type':'application/json'},body:$1})" +
       ".then(r=>r.text()).then($2).catch(function(){$3()})")>]
let postJson (url: string) (body: string) (onSuccess: string -> unit) (onFail: unit -> unit)
    : unit = jsNative

[<Emit("window.setTimeout($0, $1)")>]
let setTimeout (f: unit -> unit) (ms: int) : unit = jsNative

[<Emit("localStorage.setItem($0,$1)")>]
let private lsSet (k: string) (v: string) : unit = jsNative

[<Emit("localStorage.getItem($0)")>]
let private lsGet (k: string) : string = jsNative

[<Emit("localStorage.removeItem($0)")>]
let private lsDel (k: string) : unit = jsNative

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

/// Decode the response from GET /{file}/poll — lightweight { r, b, p }
/// (int avoids int64/BigInt mismatch with window vars)
let decodePollResponse (text: string) : Result<int * int * int, string> =
    let decoder =
        Decode.object (fun get ->
            let r = get.Required.Field "r" Decode.int
            let b = get.Required.Field "b" Decode.int
            let p = get.Required.Field "p" Decode.int
            r, b, p)
    Thoth.Json.JavaScript.Decode.fromString decoder text

/// Decode the response from GET /{file}/state or POST /{file}/changes
let decodeStateResponse (text: string) : Result<Graph * Revision, string> =
    let decoder =
        Decode.object (fun get ->
            let g = get.Required.Field "graph" Serialization.decodeGraph
            let r = get.Required.Field "revision" Serialization.decodeRevision
            g, r)
    Thoth.Json.JavaScript.Decode.fromString decoder text

// ---------------------------------------------------------------------------
// Pending-queue localStorage persistence
// ---------------------------------------------------------------------------

let private pendingKey = "gambol-pending-v1"

let savePendingQueue (changes: Change list) =
    if changes.IsEmpty then lsDel pendingKey
    else
        let encoded = Encode.list (changes |> List.map Serialization.encodeChange)
        let json = Thoth.Json.JavaScript.Encode.toString 0 encoded
        lsSet pendingKey json

let loadPendingQueue () : Change list =
    let json = lsGet pendingKey
    if isNull json || json = "" then []
    else
        match Thoth.Json.JavaScript.Decode.fromString
            (Decode.list Serialization.decodeChange) json with
        | Ok cs -> cs
        | Error _ -> []

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

/// Fire the next POST in the pending queue (head of the list).
let fireNextPending (pending: Change list) (dispatch: Msg -> unit) : unit =
    match pending with
    | [] -> ()
    | change :: _ ->
        let body = encodeChangeBody change
        postJson $"/{currentFile}/changes" body
            (fun responseText ->
                match decodeStateResponse responseText with
                | Ok (_graph, rev) -> dispatch (System (SubmitResponse rev))
                | Error _ -> dispatch (System SubmitFailed))
            (fun () -> dispatch (System SubmitFailed))

/// Apply a change to the local model, enqueue it for posting to the server,
/// and return the updated VM (or None if the change was rejected locally).
let applyAndPost (change: Change) (model: VM) (dispatch: Msg -> unit) : VM option =
    let state: State = { graph = model.graph; revision = model.revision; history = model.history }
    match History.applyChange change state with
    | ApplyResult.Changed newState ->
        let wasEmpty = model.pendingChanges.IsEmpty
        let pending = model.pendingChanges @ [change]
        savePendingQueue pending
        if wasEmpty then fireNextPending pending dispatch
        Some { model with
                 graph = newState.graph
                 history = newState.history
                 pendingChanges = pending
                 syncState = Syncing }
    | _ -> None

/// Extract the list of node IDs covered by a SiteNodeRange.
let rangeChildren (graph: Graph) (range: SiteNodeRange) =
    graph.nodes.[range.parent.nodeId].children
    |> List.skip range.start
    |> List.take (range.endd - range.start)

/// The node being edited when selectedNodes = None: the zoom root if zoomed, else the graph root.
let private viewRootNodeId (model: VM) : NodeId =
    model.zoomRoot |> Option.defaultValue model.graph.root

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
        let change: Change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = [ Op.SetText(nodeId, originalText, newText) ] }
        match applyAndPost change model dispatch with
        | Some m -> { m with mode = Selecting }
        | None   -> { model with mode = Selecting }

/// Split the currently-edited node at the cursor position.
///
/// cursor at 0: blank above, focus stays. cursor > 0: split, focus on new node.
let splitNode (currentText: string) (cursorPos: int) (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode, model.selectedNodes with
    | Editing (originalText, _, _), None ->
        // View root is being edited: commit text, no split
        commitTextEdit (viewRootNodeId model) originalText (readEditInputValue ()) model dispatch
    | Editing (originalText, _, _), Some sel ->
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

        let change: Change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = ops }
        match applyAndPost change model dispatch with
        | Some m ->
            let effRoot =
                m.zoomRoot
                |> Option.filter (fun zr -> Map.containsKey zr m.graph.nodes)
                |> Option.defaultValue m.graph.root
            let siteMap, nextId =
                ViewModel.reconcileSiteMapFrom m.graph effRoot m.siteMap m.nextInstanceId
            let m2 = { m with siteMap = siteMap; nextInstanceId = nextId }
            let focusInstId =
                if clampedPos = 0 then focusedInstanceId sel
                else
                    match Map.tryFind sel.range.parent.instanceId m2.siteMap.entries with
                    | Some p when insertIndex < p.children.Length ->
                        p.children.[insertIndex]
                    | _ -> -1
            let newSel =
                singleSelectionForInstance m2.siteMap focusInstId
                |> Option.orElseWith
                    (fun () -> singleSelection m2.graph m2.siteMap focusId)
            { m2 with selectedNodes = newSel; mode = Editing (focusText, None, Some 0) }
        | None -> model
    | _ -> model


// ---------------------------------------------------------------------------
// Paste
// ---------------------------------------------------------------------------

/// Parse node IDs format (newline-separated GUIDs) and resolve to existing nodes.
let private tryResolveNodeIdsFormat (nodeIdsText: string) (graph: Graph) : NodeId list option =
    if System.String.IsNullOrWhiteSpace nodeIdsText then None
    else
        let ids =
            nodeIdsText.Split('\n')
            |> Array.toList
            |> List.choose (fun line ->
                match System.Guid.TryParse(line.Trim()) with
                | true, guid ->
                    let id = NodeId guid
                    if Map.containsKey id graph.nodes then Some id else None
                | _ -> None)
        if ids.IsEmpty then None else Some ids

/// Handle a PasteNodes message.
///
/// When preferredNodeIds is Some (from cut/copy-as-links clipboard format), resolve
/// to existing nodes and insert as links (Op.Replace only, no NewNode).
/// Select mode: replaces selection with resolved nodes.
/// Edit mode: commits current text then inserts resolved nodes as siblings below.
///
/// Otherwise (normal deep-copy paste):
/// Select mode: replaces selection with pasted subtree.
/// Edit mode: splices first line into node at cursor; remaining lines become siblings.
let pasteNodes (pastedText: string) (preferredNodeIds: string option) (model: VM)
    (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let entries = parsePasteText pastedText
        if entries.IsEmpty then model
        else

        /// Prefer node IDs format if present and resolvable; else try GUIDs in text/plain.
        let tryLinkIds () =
            match preferredNodeIds
                |> Option.bind (fun s -> tryResolveNodeIdsFormat s model.graph) with
            | Some ids -> Some ids
            | None ->
                let ids =
                    entries
                    |> List.choose (fun (text, _) ->
                        match System.Guid.TryParse(text.Trim()) with
                        | true, guid ->
                            let id = NodeId guid
                            if Map.containsKey id model.graph.nodes then Some id else None
                        | _ -> None)
                if ids.IsEmpty then None else Some ids

        match model.mode with
        | CommandPalette _ -> model
        | Selecting ->
            let topLevelIds, pasteOps =
                match tryLinkIds () with
                | Some existingIds -> existingIds, []
                | None -> buildPasteOps entries
            if topLevelIds.IsEmpty then model
            else
            let range = sel.range
            let selectedIds = rangeChildren model.graph range
            let replaceOp = Op.Replace(range.parent.nodeId, range.start, selectedIds, topLevelIds)
            let change =
                { id = model.revision.Value
                  changeId = System.Guid.NewGuid()
                  ops = pasteOps @ [replaceOp] }
            match applyAndPost change model dispatch with
            | Some m ->
                let newEnd = range.start + topLevelIds.Length
                let newSel =
                    { range = { parent = range.parent; start = range.start; endd = newEnd }
                      focus = range.start }
                { m with selectedNodes = Some newSel }
            | None -> model
        | Editing (originalText, _, _) ->
            let currentText = readEditInputValue ()
            let cursorPos   = readEditInputCursor ()
            let focusId  = focusedNodeId model.graph sel
            let parentId = sel.range.parent.nodeId
            let focusIdx = sel.focus
            match tryLinkIds () with
            | Some refIds ->
                // Link-paste in editing mode: commit current text,
                // insert referenced nodes as siblings below
                let setTextOps =
                    if currentText <> originalText then
                        [ Op.SetText(focusId, originalText, currentText) ]
                    else []
                let insertOp = Op.Replace(parentId, focusIdx + 1, [], refIds)
                let change =
                    { id = model.revision.Value
                      changeId = System.Guid.NewGuid()
                      ops = setTextOps @ [insertOp] }
                match applyAndPost change model dispatch with
                | Some m -> { m with mode = Selecting }
                | None -> model
            | None ->
            match entries with
            | [] -> model
            | [(firstText, _)] ->
                // Single pasted line: splice into current node at cursor, no new nodes
                let newText = currentText.[..cursorPos - 1] + firstText + currentText.[cursorPos..]
                if newText = originalText then { model with mode = Selecting }
                else
                let change =
                    { id = model.revision.Value
                      changeId = System.Guid.NewGuid()
                      ops = [ Op.SetText(focusId, originalText, newText) ] }
                match applyAndPost change model dispatch with
                | Some m -> { m with mode = Selecting }
                | None -> model
            | (firstText, _) :: rest ->
                // Multi-line: splice first line at cursor; remaining become siblings below
                let newText = currentText.[..cursorPos - 1] + firstText + currentText.[cursorPos..]
                let setTextOps =
                    if newText <> originalText then
                        [ Op.SetText(focusId, originalText, newText) ]
                    else []
                let (remainingTopIds, remainingOps) = buildPasteOps rest
                let insertOps =
                    if remainingTopIds.IsEmpty then []
                    else [ Op.Replace(parentId, focusIdx + 1, [], remainingTopIds) ]
                let allOps = setTextOps @ remainingOps @ insertOps
                if allOps.IsEmpty then { model with mode = Selecting }
                else
                let change =
                    { id = model.revision.Value
                      changeId = System.Guid.NewGuid()
                      ops = allOps }
                match applyAndPost change model dispatch with
                | Some m -> { m with mode = Selecting }
                | None -> model

// ---------------------------------------------------------------------------
// Indent / Outdent
// ---------------------------------------------------------------------------

/// Tab: make selected nodes children of the sibling immediately before them.
/// No-op if the selection starts at index 0 (no previous sibling).
/// Move the selected nodes from their current parent to a new parent at insertIdx.
/// Common core of indent and outdent.
let reparentSelection (newParentEntry: SiteEntry) (insertIdx: int) (sel: Selection) (model: VM)
    (dispatch: Msg -> unit) : VM =
    let range = sel.range
    let selectedIds = rangeChildren model.graph range
    let ops =
        [ Op.Replace(range.parent.nodeId, range.start, selectedIds, [])
          Op.Replace(newParentEntry.nodeId, insertIdx, [], selectedIds) ]
    let change =
        { id = model.revision.Value
          changeId = System.Guid.NewGuid()
          ops = ops }
    match applyAndPost change model dispatch with
    | Some m ->
        let newSel =
            { range = { parent = newParentEntry; start = insertIdx
                        endd = insertIdx + selectedIds.Length }
              focus = insertIdx }
        { m with selectedNodes = Some newSel }
    | None -> model

let indentSelection (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel when sel.range.start = 0 -> model  // no previous sibling — no-op
    | Some sel ->
        let prevSibId = model.graph.nodes.[sel.range.parent.nodeId].children.[sel.range.start - 1]
        let insertIdx = model.graph.nodes.[prevSibId].children.Length
        match model.siteMap.entries
            |> Map.tryPick (fun _ e -> if e.nodeId = prevSibId then Some e else None) with
        | None -> model
        | Some prevSibEntry ->
        let result = reparentSelection prevSibEntry insertIdx sel model dispatch
        // Ensure the new parent is expanded so the indented items are visible after reconcile
        if prevSibEntry.expanded then result
        else
            let siteMap, nextId =
                ViewModel.expandEntry prevSibEntry.instanceId result.graph result.siteMap
                    result.nextInstanceId
            { result with siteMap = siteMap; nextInstanceId = nextId }

let outdentSelection (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        match Graph.tryFindParentAndIndex sel.range.parent.nodeId model.graph with
        | None -> model  // parent is root — no-op
        | Some (grandparentId, parentIdx) ->
            match model.siteMap.entries
                |> Map.tryPick (fun _ e -> if e.nodeId = grandparentId then Some e else None) with
            | None -> model
            | Some grandparentEntry ->
            reparentSelection grandparentEntry (parentIdx + 1) sel model dispatch

/// If currently editing, commit the edit and return Selecting model; otherwise return model as-is.
let commitIfEditing (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode, model.selectedNodes with
    | Editing (originalText, _, _), None ->
        commitTextEdit (viewRootNodeId model) originalText (readEditInputValue ()) model dispatch
    | Editing (originalText, _, _), Some sel ->
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
        let change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = [removeOp] }
        match applyAndPost change model dispatch with
        | Some m ->
            let newChildren = m.graph.nodes.[sel.range.parent.nodeId].children
            let newSel =
                if sel.range.start < newChildren.Length then
                    let i = sel.range.start
                    Some { range = { parent = sel.range.parent; start = i; endd = i + 1 }
                           focus = i }
                elif sel.range.start > 0 then
                    let i = sel.range.start - 1
                    Some { range = { parent = sel.range.parent; start = i; endd = i + 1 }
                           focus = i }
                else
                    singleSelection m.graph m.siteMap sel.range.parent.nodeId
            { m with clipboard = Some cb; selectedNodes = newSel }
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
            let change =
                { id = model.revision.Value
                  changeId = System.Guid.NewGuid()
                  ops = [ Op.Replace(range.parent.nodeId, opStart, oldSpan, newSpan) ] }
            match applyAndPost change model dispatch with
            | Some m ->
                let newRange = { range with start = newStart; endd = newStart + selectedIds.Length }
                let newSel = { range = newRange; focus = sel.focus + delta }
                { m with selectedNodes = Some newSel }
            | None -> model

// ---------------------------------------------------------------------------
// Update
// ---------------------------------------------------------------------------

/// Rebuild the site map after a graph mutation, preserving fold states.
/// Uses the effective zoom root if set (and still present in the graph); falls back to graph.root.
/// Also refreshes selectedNodes.range.parent from the new siteMap so that
/// focusedInstanceId reads current children (not the pre-mutation snapshot).
let withSiteMap (model: VM) : VM =
    let effectiveRoot =
        model.zoomRoot
        |> Option.filter (fun zr -> Map.containsKey zr model.graph.nodes)
        |> Option.defaultValue model.graph.root
    let zoomRoot =
        match model.zoomRoot with
        | Some zr when not (Map.containsKey zr model.graph.nodes) -> None
        | z -> z
    let siteMap, nextId =
        ViewModel.reconcileSiteMapFrom model.graph effectiveRoot model.siteMap model.nextInstanceId
    let model' = { model with siteMap = siteMap; nextInstanceId = nextId; zoomRoot = zoomRoot }
    match model'.selectedNodes with
    | None -> model'
    | Some sel ->
        match Map.tryFind sel.range.parent.instanceId model'.siteMap.entries with
        | None -> model'
        | Some freshParent ->
            { model' with
                selectedNodes = Some { sel with range = { sel.range with parent = freshParent } } }

/// Join the currently-edited node with the previous visible (inorder) node.
/// 1. If current has no children: append current's text to prev, delete current.
/// 2. If current and prev both have children: abort.
/// 3. If current has children but prev does not: move current's children to prev, then do 1.
/// Cursor lands at the join point (end of prevText) in prev.
let moveEdit (delta: int) (cursorPos: int) (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let currentId = focusedNodeId model.graph sel
        let focusInstId = focusedInstanceId sel
        let committed =
            commitTextEdit currentId
                (match model.mode with Editing (t, _, _) -> t | _ -> "")
                (readEditInputValue ()) model dispatch
        let rows = getVisibleRowInstanceIds committed.siteMap
        match rows |> List.tryFindIndex ((=) focusInstId) with
        | None -> committed
        | Some idx ->
            let targetIdx = idx + delta
            if targetIdx < 0 || targetIdx >= rows.Length then committed
            else
                let targetInstId = rows.[targetIdx]
                let targetEntry = committed.siteMap.entries.[targetInstId]
                let targetText = committed.graph.nodes.[targetEntry.nodeId].text
                let clampedPos = min cursorPos targetText.Length
                { committed with
                    mode = Editing (targetText, None, Some clampedPos)
                    selectedNodes = singleSelectionForInstance committed.siteMap targetInstId }

let joinWithNext (currentText: string) (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode, model.selectedNodes with
    | Editing _, Some sel ->
        let currentId = focusedNodeId model.graph sel
        let focusInstId = focusedInstanceId sel
        let rows = getVisibleRowInstanceIds model.siteMap
        match rows |> List.tryFindIndex ((=) focusInstId) with
        | None -> model
        | Some currentIndex ->
            if currentIndex >= rows.Length - 1 then model
            else
                let nextInstId = rows.[currentIndex + 1]
                let nextEntry = model.siteMap.entries.[nextInstId]
                let nextId = nextEntry.nodeId
                let nextNode = model.graph.nodes.[nextId]
                let currentNode = model.graph.nodes.[currentId]
                if not currentNode.children.IsEmpty then
                    let pos = readEditInputCursor ()
                    match model.mode with
                    | Editing (t, pf, _) -> { model with mode = Editing (t, pf, Some pos) }
                    | _ -> model
                else
                    match Graph.tryFindParentAndIndex nextId model.graph with
                    | None -> model
                    | Some (parentId, indexInParent) ->
                        if System.String.IsNullOrWhiteSpace currentText then
                            // Empty current row: delete current, keep next
                            match Graph.tryFindParentAndIndex currentId model.graph with
                            | None -> model
                            | Some (currParentId, currIndexInParent) ->
                                let ops =
                                    [ Op.Replace(currParentId, currIndexInParent, [currentId], []) ]
                                let change =
                                    { id = model.revision.Value
                                      changeId = System.Guid.NewGuid()
                                      ops = ops }
                                match applyAndPost change model dispatch with
                                | None -> model
                                | Some m ->
                                    let result = withSiteMap m
                                    { result with
                                        mode = Editing (nextNode.text, None, Some 0)
                                        selectedNodes =
                                            singleSelection result.graph result.siteMap nextId }
                        else
                            // Standard join: merge current into next, delete current, keep next
                            let joinedText = currentText + nextNode.text
                            let cursorPos = currentText.Length
                            match Graph.tryFindParentAndIndex currentId model.graph with
                            | None -> model
                            | Some (currParentId, currIndexInParent) ->
                                let ops =
                                    [ if joinedText <> nextNode.text then
                                          yield Op.SetText(nextId, nextNode.text, joinedText)
                                      if not currentNode.children.IsEmpty then
                                          yield Op.Replace
                                              (nextId, 0, [], currentNode.children)
                                      yield Op.Replace
                                          (currParentId, currIndexInParent, [currentId], [])
                                      ]
                                let change =
                                    { id = model.revision.Value
                                      changeId = System.Guid.NewGuid()
                                      ops = ops }
                                match applyAndPost change model dispatch with
                                | None -> model
                                | Some m ->
                                    let result = withSiteMap m
                                    { result with
                                        mode = Editing (joinedText, None, Some cursorPos)
                                        selectedNodes =
                                            singleSelection result.graph result.siteMap nextId }
    | _ -> model

let joinWithPrevious (currentText: string) (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode, model.selectedNodes with
    | Editing _, Some sel ->
        let currentId = focusedNodeId model.graph sel
        let focusInstId = focusedInstanceId sel
        let rows = getVisibleRowInstanceIds model.siteMap
        match rows |> List.tryFindIndex ((=) focusInstId) with
        | None | Some 0 -> model
        | Some currentIndex ->
            let prevInstId = rows.[currentIndex - 1]
            let prevEntry = model.siteMap.entries.[prevInstId]
            let prevId = prevEntry.nodeId
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
                              yield Op.Replace
                                  (prevId, prevNode.children.Length, [], currentNode.children)
                          yield Op.Replace(parentId, indexInParent, [currentId], []) ]
                    let change =
                        { id = model.revision.Value
                          changeId = System.Guid.NewGuid()
                          ops = ops }
                    match applyAndPost change model dispatch with
                    | None -> model
                    | Some m ->
                        let result = withSiteMap m
                        { result with
                            mode = Editing (joinedText, None, Some cursorPos)
                            selectedNodes =
                                singleSelection result.graph result.siteMap prevId }
    | _ -> model

// ---------------------------------------------------------------------------
// Op type and named operations
// User interactions are represented as Op values and applied directly,
// bypassing the Msg union entirely.
// ---------------------------------------------------------------------------

/// A self-contained model transformation. dispatch is provided for operations
/// that fire async server POSTs; pure transforms ignore it with _.
type VmMsgUnitVm = VM -> (Msg -> unit) -> VM

/// Op: Move to selection mode (or deselect if already selecting), reverting any edit.
let cancelEdit (model: VM) _dispatch : VM =
    match model.mode with
    | Editing _ -> { model with mode = Selecting }
    | Selecting -> { model with selectedNodes = None }
    | CommandPalette _ -> model  // handled by closeCommandPaletteOp

/// Op: Copy the focused subtree to the internal clipboard.
let copySelectionOp (model: VM) _dispatch : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let selectedIds = rangeChildren model.graph sel.range
        { model with clipboard = Some (collectSubtree model.graph model.siteMap selectedIds) }

/// Op: Cut the focused subtree.
let cutSelectionOp (model: VM) (dispatch: Msg -> unit) : VM =
    cutSelection model dispatch |> withSiteMap

/// Op: Enter edit mode for the focused node, prefilled with its current text.
let startEditOp (model: VM) _dispatch : VM =
    let text =
        match model.selectedNodes with
        | None -> model.graph.nodes.[viewRootNodeId model].text
        | Some sel -> model.graph.nodes.[focusedNodeId model.graph sel].text
    { model with mode = Editing (text, Some text, None) }

/// Op: Enter edit mode for the focused node, showing prefill text in the input.
let startEdit (prefill: string) (model: VM) _dispatch : VM =
    match model.selectedNodes with
    | None ->
        let node = model.graph.nodes.[viewRootNodeId model]
        { model with mode = Editing (node.text, Some prefill, None) }
    | Some sel ->
        let nodeId = focusedNodeId model.graph sel
        let node = model.graph.nodes.[nodeId]
        { model with mode = Editing (node.text, Some prefill, None) }

/// Op: Enter edit mode for the focused node, with cursor placed at a specific position.
let startEditAtPos (prefill: string) (cursorPos: int) (model: VM) _dispatch : VM =
    match model.selectedNodes with
    | None ->
        let node = model.graph.nodes.[viewRootNodeId model]
        { model with mode = Editing (node.text, Some prefill, Some cursorPos) }
    | Some sel ->
        let nodeId = focusedNodeId model.graph sel
        let node = model.graph.nodes.[nodeId]
        { model with mode = Editing (node.text, Some prefill, Some cursorPos) }

/// Re-export palette ops for use by Controller and View.
let openCommandPaletteOp = Gambol.Client.CommandPalette.openCommandPaletteOp
let closeCommandPaletteOp = Gambol.Client.CommandPalette.closeCommandPaletteOp

/// Op: Select a specific node, committing any in-progress edit first.
let selectRow (nodeId: NodeId) (model: VM) (dispatch: Msg -> unit) : VM =
    let result =
        match model.mode, model.selectedNodes with
        | Editing (originalText, _, _), Some sel ->
            let editingId = focusedNodeId model.graph sel
            let newText = readEditInputValue ()
            let model' = commitTextEdit editingId originalText newText model dispatch
            { model' with selectedNodes = singleSelection model'.graph model'.siteMap nodeId }
        | _ ->
            { model with
                selectedNodes = singleSelection model.graph model.siteMap nodeId
                mode = Selecting }
    if not (System.Object.ReferenceEquals(result.graph, model.graph)) then
        withSiteMap result else result

/// Op: Select a specific view-line by instanceId, committing any in-progress edit first.
/// Prefer this over selectRow when a nodeId may appear multiple times in the view.
let selectInstance (instanceId: int) (model: VM) (dispatch: Msg -> unit) : VM =
    let result =
        match model.mode, model.selectedNodes with
        | Editing (originalText, _, _), Some sel ->
            let editingId = focusedNodeId model.graph sel
            let newText = readEditInputValue ()
            let model' = commitTextEdit editingId originalText newText model dispatch
            { model' with selectedNodes = singleSelectionForInstance model'.siteMap instanceId }
        | _ ->
            { model with
                selectedNodes = singleSelectionForInstance model.siteMap instanceId
                mode = Selecting }
    if not (System.Object.ReferenceEquals(result.graph, model.graph)) then
        withSiteMap result else result

/// Op: Move selection up, committing any in-progress edit first.
let moveSelectionUp (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode with
    | CommandPalette _ -> Gambol.Client.CommandPalette.paletteSelectUpOp model dispatch
    | _ ->
        let result =
            match model.mode with
            | Editing _ -> moveSelectionBy -1 (commitIfEditing model dispatch)
            | _         -> applyMoveSelectionUp model
        if not (System.Object.ReferenceEquals(result.graph, model.graph)) then
            withSiteMap result else result

/// Op: Move selection down, committing any in-progress edit first.
let moveSelectionDown (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode with
    | CommandPalette _ -> Gambol.Client.CommandPalette.paletteSelectDownOp model dispatch
    | _ ->
        let result =
            match model.mode with
            | Editing _ -> moveSelectionBy 1 (commitIfEditing model dispatch)
            | _         -> applyMoveSelectionDown model
        if not (System.Object.ReferenceEquals(result.graph, model.graph)) then
            withSiteMap result else result

/// Op: Extend or shrink the selection by one row (Shift+Arrow).
let shiftArrowOp (delta: int) (model: VM) _dispatch : VM = shiftArrow delta model

/// Op: Split the node at the given cursor position.
let splitNodeOp (currentText: string) (cursorPos: int) (model: VM) (dispatch: Msg -> unit) : VM =
    splitNode currentText cursorPos model dispatch |> withSiteMap

/// Op: Commit current edit and move into edit mode on the previous visible row.
let moveEditUp (cursorPos: int) (model: VM) (dispatch: Msg -> unit) : VM =
    moveEdit -1 cursorPos model dispatch

/// Op: Commit current edit and move into edit mode on the next visible row.
let moveEditDown (cursorPos: int) (model: VM) (dispatch: Msg -> unit) : VM =
    moveEdit 1 cursorPos model dispatch

/// Op: Indent selection (Tab), committing any in-progress edit first.
let indentOp (model: VM) (dispatch: Msg -> unit) : VM =
    indentSelection (commitIfEditing model dispatch) dispatch |> withSiteMap

/// Op: Outdent selection (Shift+Tab), committing any in-progress edit first.
let outdentOp (model: VM) (dispatch: Msg -> unit) : VM =
    outdentSelection (commitIfEditing model dispatch) dispatch |> withSiteMap

/// Op: Move selected nodes up, committing any in-progress edit first.
let moveNodeUpOp (model: VM) (dispatch: Msg -> unit) : VM =
    moveNode -1 (commitIfEditing model dispatch) dispatch |> withSiteMap

/// Op: Move selected nodes down, committing any in-progress edit first.
let moveNodeDownOp (model: VM) (dispatch: Msg -> unit) : VM =
    moveNode 1 (commitIfEditing model dispatch) dispatch |> withSiteMap

/// Op: Paste text into the model. preferredNodeIds from clipboard format, if present.
let pasteNodesOp (pastedText: string) (preferredNodeIds: string option) (model: VM)
    (dispatch: Msg -> unit) : VM =
    pasteNodes pastedText preferredNodeIds model dispatch |> withSiteMap

/// Op: Toggle fold for a specific site-map entry.
let toggleFoldOp (instanceId: int) (model: VM) _dispatch : VM =
    match Map.tryFind instanceId model.siteMap.entries with
    | None -> model
    | Some entry ->
        if entry.expanded then
            { model with siteMap = ViewModel.toggleFold instanceId model.siteMap }
        else
            let siteMap, nextId =
                ViewModel.expandEntry instanceId model.graph model.siteMap model.nextInstanceId
            { model with siteMap = siteMap; nextInstanceId = nextId }

/// Op: ArrowLeft in selection — fold if expanded, else move to parent.
let arrowLeftSelectionOp (model: VM) _dispatch : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let focusInstId = focusedInstanceId sel
        match Map.tryFind focusInstId model.siteMap.entries with
        | None -> model
        | Some entry ->
            let node = model.graph.nodes.[entry.nodeId]
            let hasChildren = not node.children.IsEmpty
            if hasChildren && entry.expanded then
                { model with siteMap = ViewModel.toggleFold focusInstId model.siteMap }
            else
                match entry.parentInstanceId with
                | None -> model
                | Some parentInstId ->
                    match singleSelectionForInstance model.siteMap parentInstId with
                    | None -> model
                    | Some parentSel -> { model with selectedNodes = Some parentSel }

/// Op: ArrowRight in selection — expand if folded, else move to first child.
let arrowRightSelectionOp (model: VM) _dispatch : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let focusInstId = focusedInstanceId sel
        match Map.tryFind focusInstId model.siteMap.entries with
        | None -> model
        | Some entry ->
            let node = model.graph.nodes.[entry.nodeId]
            let hasChildren = not node.children.IsEmpty
            if not hasChildren then model
            elif not entry.expanded then
                let siteMap, nextId =
                    ViewModel.expandEntry focusInstId model.graph model.siteMap model.nextInstanceId
                { model with siteMap = siteMap; nextInstanceId = nextId }
            else
                match entry.children with
                | [] -> model
                | firstChildInstId :: _ ->
                    match singleSelectionForInstance model.siteMap firstChildInstId with
                    | None -> model
                    | Some childSel -> { model with selectedNodes = Some childSel }

/// Op: Toggle fold for all selected entries.
let toggleFoldSelectionOp (model: VM) _dispatch : VM =
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
                selectedInstIds
                |> List.fold (fun sm instId -> ViewModel.toggleFold instId sm) model.siteMap
            { model with siteMap = siteMap }
        else
            let siteMap, nextId =
                selectedInstIds |> List.fold
                    (fun (sm, nid) instId -> ViewModel.expandEntry instId model.graph sm nid)
                    (model.siteMap, model.nextInstanceId)
            { model with siteMap = siteMap; nextInstanceId = nextId }

/// Op: Duplicate the selected nodes as references — insert the same NodeIds beside.
/// Inserts at range.endd; selection expands to include the new references.
let duplicateSelectionOp (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let selectedIds = rangeChildren model.graph sel.range
        if selectedIds.IsEmpty then model
        else
            let insertOp = Op.Replace(sel.range.parent.nodeId, sel.range.endd, [], selectedIds)
            let change =
                { id = model.revision.Value
                  changeId = System.Guid.NewGuid()
                  ops = [ insertOp ] }
            match applyAndPost change model dispatch with
            | None -> model
            | Some m ->
                let newEnd = sel.range.endd + selectedIds.Length
                let newSel = { range = { sel.range with endd = newEnd }; focus = sel.focus }
                { m with selectedNodes = Some newSel }
            |> withSiteMap

/// Op: Delete the selected nodes (Replace with nothing), updating selection to next/prev/parent.
let deleteSelectionOp (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let selectedIds = rangeChildren model.graph sel.range
        let change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = [ Op.Replace(sel.range.parent.nodeId, sel.range.start, selectedIds, []) ] }
        match applyAndPost change model dispatch with
        | None -> model
        | Some m ->
            let newChildren = m.graph.nodes.[sel.range.parent.nodeId].children
            let newSel =
                if sel.range.start < newChildren.Length then
                    let i = sel.range.start
                    Some { range = { parent = sel.range.parent; start = i; endd = i + 1 }
                           focus = i }
                elif sel.range.start > 0 then
                    let i = sel.range.start - 1
                    Some { range = { parent = sel.range.parent; start = i; endd = i + 1 }
                           focus = i }
                else
                    singleSelection m.graph m.siteMap sel.range.parent.nodeId
            { m with selectedNodes = newSel }
        |> withSiteMap

[<Emit("window.prompt($0, $1)")>]
let private promptDialog (msg: string) (def: string) : string = jsNative

/// Op: Prompt for a CSS class name and toggle its presence on all selected nodes.
/// If all selected nodes have the class, removes it; else adds it to any that lack it.
/// Accepts any legal CSS identifier that does not start with "amb-".
let toggleClassOp (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let input = promptDialog "CSS class name:" ""
        if isNull input || input.Trim() = "" then model
        else
            let name = input.Trim()
            if not (CssClass.isValidUserClass name) then model
            else
                let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
                let selectedIds =
                    parentNode.children
                    |> List.skip sel.range.start
                    |> List.take (sel.range.endd - sel.range.start)
                let allHave =
                    selectedIds
                    |> List.forall (fun nid ->
                        CssClass.contains name model.graph.nodes.[nid].cssClasses)
                let ops =
                    selectedIds
                    |> List.choose (fun nid ->
                        let node = model.graph.nodes.[nid]
                        let oldClasses = node.cssClasses
                        let hasIt = CssClass.contains name oldClasses
                        // remove from all when all have it; add only to those that lack it
                        let shouldChange = if allHave then hasIt else not hasIt
                        if not shouldChange then None
                        else Some (Op.SetClasses(nid, oldClasses, CssClass.toggle name oldClasses)))
                if ops.IsEmpty then model
                else
                    let change =
                        { id = model.revision.Value
                          changeId = System.Guid.NewGuid()
                          ops = ops }
                    match applyAndPost change model dispatch with
                    | Some m -> m
                    | None -> model

/// Build a first-child Selection for the root entry of a freshly-built siteMap.
/// Returns None if the root has no children.
let private firstChildSelection (siteMap: SiteMap) (rootNodeId: NodeId) : Selection option =
    siteMap.entries
    |> Map.tryPick (fun _ e -> if e.nodeId = rootNodeId then Some e else None)
    |> Option.bind (fun rootEntry ->
        if rootEntry.children.IsEmpty then None
        else Some { range = { parent = rootEntry; start = 0; endd = 1 }; focus = 0 })

/// Op: Zoom in — set the view root to the first selected node (Ctrl+]).
/// Commits any in-progress edit first. No-op when the view root is focused or the node is a leaf.
let zoomInOp (model: VM) (dispatch: Msg -> unit) : VM =
    let model' = commitIfEditing model dispatch
    match model'.selectedNodes with
    | None -> model'
    | Some sel ->
        let zoomNodeId = firstSelectedNodeId model'.graph sel
        let zoomNode = model'.graph.nodes.[zoomNodeId]
        if zoomNode.children.IsEmpty then model'
        else
            let siteMap, nextId =
                ViewModel.buildSiteMapFrom model'.graph zoomNodeId model'.nextInstanceId
            { model' with
                zoomRoot = Some zoomNodeId
                siteMap = siteMap
                nextInstanceId = nextId
                selectedNodes = firstChildSelection siteMap zoomNodeId
                mode = Selecting }

/// Op: Zoom out — move the view root one level up toward the graph root (Ctrl+[).
/// Commits any in-progress edit first. No-op when already showing the full tree.
let zoomOutOp (model: VM) (dispatch: Msg -> unit) : VM =
    let model' = commitIfEditing model dispatch
    match model'.zoomRoot with
    | None -> model'
    | Some currentZoomRoot ->
        let newZoomRoot =
            match Graph.tryFindParentAndIndex currentZoomRoot model'.graph with
            | None -> None
            | Some (parentId, _) ->
                if parentId = model'.graph.root then None else Some parentId
        let effectiveRoot = newZoomRoot |> Option.defaultValue model'.graph.root
        let siteMap, nextId =
            ViewModel.buildSiteMapFrom model'.graph effectiveRoot model'.nextInstanceId
        { model' with
            zoomRoot = newZoomRoot
            siteMap = siteMap
            nextInstanceId = nextId
            selectedNodes = firstChildSelection siteMap effectiveRoot
            mode = Selecting }

/// Op: Retry pending server POST.
let retryPendingOp (model: VM) (dispatch: Msg -> unit) : VM =
    match model.syncState, model.pendingChanges with
    | _, [] -> { model with syncState = Synced }
    | Syncing, _ -> model
    | _, _ ->
        fireNextPending model.pendingChanges dispatch
        { model with syncState = Syncing }

/// Op: Undo the last change, committing any in-progress edit first.
let undoOp (model: VM) (dispatch: Msg -> unit) : VM =
    let model' = commitIfEditing model dispatch
    let state = { graph = model'.graph; history = model'.history; revision = model'.revision }
    match model'.history.past |> List.tryHead with
    | None -> model'
    | Some headChange ->
        match History.undo state with
        | ApplyResult.Changed newState ->
            let invertedChange = { Change.invert headChange with id = model'.history.nextId }
            let wasEmpty = model'.pendingChanges.IsEmpty
            let pending = model'.pendingChanges @ [invertedChange]
            savePendingQueue pending
            if wasEmpty then fireNextPending pending dispatch
            { model' with
                graph = newState.graph
                history = newState.history
                mode = Selecting
                pendingChanges = pending
                syncState = Syncing }
            |> withSiteMap
        | _ -> model'

/// Op: Redo the last undone change, committing any in-progress edit first.
let redoOp (model: VM) (dispatch: Msg -> unit) : VM =
    let model' = commitIfEditing model dispatch
    let state = { graph = model'.graph; history = model'.history; revision = model'.revision }
    match model'.history.future |> List.tryHead with
    | None -> model'
    | Some headChange ->
        match History.redo state with
        | ApplyResult.Changed newState ->
            let reChange =
                { headChange with
                    id = model'.history.nextId
                    changeId = System.Guid.NewGuid() }
            let wasEmpty = model'.pendingChanges.IsEmpty
            let pending = model'.pendingChanges @ [reChange]
            savePendingQueue pending
            if wasEmpty then fireNextPending pending dispatch
            { model' with
                graph = newState.graph
                history = newState.history
                mode = Selecting
                pendingChanges = pending
                syncState = Syncing }
            |> withSiteMap
        | _ -> model'

// ---------------------------------------------------------------------------
// System message handler
// User actions bypass this and call the named Op functions directly.
// ---------------------------------------------------------------------------

/// Process an async server message. Op-based user actions do not go through here.
let update (msg: Msg) (model: VM) (dispatch: Msg -> unit) : VM =
    match msg with
    | System (StateLoaded (graph, revision)) ->
        let siteMap, nextId = ViewModel.buildSiteMap graph 0
        { graph = graph
          revision = revision
          history = History.empty
          selectedNodes = None
          mode = Selecting
          siteMap = siteMap
          nextInstanceId = nextId
          zoomRoot = None
          clipboard = None
          pendingChanges = []
          syncState = Synced }

    | System (SubmitResponse revision) ->
        let pending = match model.pendingChanges with _ :: t -> t | [] -> []
        savePendingQueue pending
        if not pending.IsEmpty then fireNextPending pending dispatch
        { model with
            revision = revision
            history = { model.history with nextId = max model.history.nextId revision.Value }
            pendingChanges = pending
            syncState = if pending.IsEmpty then Synced else Syncing }

    | System SubmitFailed ->
        { model with syncState = Pending }

    | System (ServerAhead _) ->
        { model with syncState = Stale }

