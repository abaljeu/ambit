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
/// anchor and focus don't exist in input, but selectionDirection can be used to compute these.
let readEditInputCursor () : int =
    let el = document.getElementById "edit-input"
    if isNull el then 0
    else int (el :?> HTMLInputElement).selectionStart

/// Read the edit input selection end from the DOM (same as caret when no range).
let readEditInputSelectionEnd () : int =
    let el = document.getElementById "edit-input"
    if isNull el then 0
    else int (el :?> HTMLInputElement).selectionEnd

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

/// If `newText` differs from the graph, the same `SetText` op `commitTextEdit` would post (no mode change).
let private tryTextCommitOps (nodeId: NodeId) (originalTextForHistory: string) (newText: string) (graph: Graph) : Op list =
    let modelText = graph.nodes.[nodeId].text
    if newText = modelText then [] else [ Op.SetText(nodeId, originalTextForHistory, newText) ]

/// Apply a committed text edit to the model and POST to server.
/// Returns the updated model. Dispatches SubmitResponse asynchronously.
let commitTextEdit
    (nodeId: NodeId)
    (_originalText: string)
    (newText: string)
    (model: VM)
    (dispatch: Msg -> unit)
    : VM =
    match tryTextCommitOps nodeId _originalText newText model.graph with
    | [] -> { model with mode = Selecting }
    | ops ->
        let change: Change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = ops }
        match applyAndPost change model dispatch with
        | Some m -> { m with mode = Selecting }
        | None   -> { model with mode = Selecting }

/// Split the currently-edited node at the cursor position.
///
/// cursor at 0   → blank sibling inserted above; current node keeps its text; focus moves to the new blank node.
/// cursor > 0    → current node gets text-before; new sibling gets text-after; focus at start of new node.
let splitNode (currentText: string) (cursorPos: int) (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode, model.selectedNodes with
    | Editing (originalText, _), None ->
        // View root is being edited: commit text, no split
        commitTextEdit (viewRootNodeId model) originalText (readEditInputValue ()) model dispatch
    | Editing (originalText, _), Some sel ->
        // The node being edited is the focus node.
        let selectedId  = focusedNodeId model.graph sel
        let modelText = model.graph.nodes.[selectedId].text
        let parentId    = sel.range.parent.nodeId
        let indexInParent = sel.focus
        let clampedPos = max 0 (min cursorPos currentText.Length)
        let textBefore = currentText.[..clampedPos - 1]
        let textAfter  = currentText.[clampedPos..]
        let newNodeId  = NodeId.New()

        let (insertIndex, newNodeText, focusId, focusText) =
            if clampedPos = 0 then
                // blank node above; focus moves to the new blank node
                (indexInParent, "", newNodeId, "")
            else
                // new node after; focus moves to new node
                (indexInParent + 1, textAfter, newNodeId, textAfter)

        let ops =
            [ yield Op.NewNode(newNodeId, newNodeText)
              yield Op.Replace(parentId, insertIndex, [], [newNodeId])
              // update current node's text only when it actually changes
              let updatedText = if clampedPos = 0 then currentText else textBefore
              if updatedText <> modelText then
                  yield Op.SetText(selectedId, modelText, updatedText) ]

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
            { m2 with selectedNodes = newSel; mode = Editing (focusText, Some 0) }
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
        | CommandPalette _ | CssClassPrompt _ -> model
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
        | Editing (originalText, _) ->
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

/// If currently editing, commit the edit and return Selecting model; otherwise return model as-is.
let commitIfEditing (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode, model.selectedNodes with
    | Editing (originalText, _), None ->
        commitTextEdit (viewRootNodeId model) originalText (readEditInputValue ()) model dispatch
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

/// Returns the adjacent sibling of me's parent (delta=-1 for previous, +1 for next) if it exists,
/// is visible (neither me nor its parent is the VM root), and is expanded; otherwise None.
let parentSiblingOpen (delta: int) (me: SiteEntry) (model: VM) : SiteEntry option =
    let parentEntryOpt =
        match me.parentInstanceId with
        | None -> None
        | Some parentId -> Map.tryFind parentId model.siteMap.entries

    match parentEntryOpt with
    | None -> None
    | Some parentEntry ->
        let effectiveRoot = model.zoomRoot |> Option.defaultValue model.graph.root
        let isMeRoot = me.nodeId = effectiveRoot
        let isParentRoot = parentEntry.nodeId = effectiveRoot

        if isMeRoot || isParentRoot then
            None
        else
            let grandparentOpt =
                parentEntry.parentInstanceId
                |> Option.bind (fun id -> Map.tryFind id model.siteMap.entries)
            match grandparentOpt with
            | None -> None
            | Some grandparent ->
                match grandparent.children |> List.tryFindIndex ((=) parentEntry.instanceId) with
                | Some idx ->
                    let sibIdx = idx + delta
                    if sibIdx >= 0 && sibIdx < grandparent.children.Length then
                        let sibInstId = grandparent.children.[sibIdx]
                        match Map.tryFind sibInstId model.siteMap.entries with
                        | Some sibEntry when sibEntry.expanded -> Some sibEntry
                        | _ -> None
                    else
                        None
                | None -> None

/// Inline `Editing` leaf under optional palette/CSS wrappers: undo baseline + rebuild mode with text and caret.
let private tryInlineEditWrap (mode: Mode) : (string * (string -> int -> Mode)) option =
    let rec go m =
        match m with
        | Editing (orig, _) -> Some (orig, fun t c -> Editing (t, Some c))
        | CommandPalette (q, sc, ret) ->
            go ret |> Option.map (fun (o, w) -> (o, fun t c -> CommandPalette (q, sc, w t c)))
        | CssClassPrompt (ret, iv) ->
            go ret |> Option.map (fun (o, w) -> (o, fun t c -> CssClassPrompt (w t c, iv)))
        | _ -> None
    go mode

/// Move the selected nodes to after `too`. May remove from old parent and add to new
/// (two Op.Replace ops), or reorder within the same parent.
/// Inline edit: `tryTextCommitOps` + move in one change; stay in edit mode with clamped caret.
let moveNodeFromTo (too: SiteNodeRange) (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let from = sel.range
        let selectedIds = rangeChildren model.graph from
        if selectedIds.IsEmpty then model
        else
            let wrap = tryInlineEditWrap model.mode
            let live = readEditInputValue ()
            let caret = readEditInputCursor ()
            let editingId = focusedNodeId model.graph sel
            let textOps =
                match wrap with
                | Some (orig, _) -> tryTextCommitOps editingId orig live model.graph
                | None -> []
            let count = selectedIds.Length
            let sameParent = from.parent.nodeId = too.parent.nodeId
            let replaceOps =
                if sameParent then
                    // Reordering within same parent: remove then insert.
                    // Insert index after removal: if too.endd > from.endd, shift left by count.
                    let insertIdx =
                        if too.endd <= from.start then too.endd
                        else too.endd - count
                    [ Op.Replace(from.parent.nodeId, from.start, selectedIds, [])
                      Op.Replace(from.parent.nodeId, insertIdx, [], selectedIds) ]
                else
                    [ Op.Replace(from.parent.nodeId, from.start, selectedIds, [])
                      Op.Replace(too.parent.nodeId, too.endd, [], selectedIds) ]
            let ops = textOps @ replaceOps
            let change =
                { id = model.revision.Value
                  changeId = System.Guid.NewGuid()
                  ops = ops }
            match applyAndPost change model dispatch with
            | Some m ->
                let insertIdx = if sameParent then (if too.endd <= from.start then too.endd else too.endd - count) else too.endd
                let newParent = if sameParent then from.parent else too.parent
                let newRange: SiteNodeRange = { parent = newParent; start = insertIdx; endd = insertIdx + count }
                let focusOffset = sel.focus - from.start
                let newFocus = insertIdx + (min (max 0 focusOffset) (count - 1))
                let newSel = { range = newRange; focus = newFocus }
                let m' = { m with selectedNodes = Some newSel }
                match wrap with
                | Some (_, rebuild) ->
                    let t = m.graph.nodes.[focusedNodeId m.graph newSel].text
                    { m' with mode = rebuild t (min (max 0 caret) t.Length) }
                | None -> m'
            | None -> model

/// Alt+Up/Down: swap the selected range with the adjacent sibling. Delegates to moveNodeFromTo.
let moveNodeDelta (delta: int) (model: VM) (dispatch: Msg -> unit) : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let range = sel.range
        let parentLen = model.graph.nodes.[range.parent.nodeId].children.Length
        let too =
            if delta < 0 && range.start > 0 then
                // Move to before sibling above: insert at range.start - 1 (after the range ending there)
                let s = if range.start = 1 then 0 else range.start - 2
                Some ({ parent = range.parent; start = s; endd = range.start - 1 } : SiteNodeRange)
            elif delta > 0 && range.endd < parentLen then
                // Move to after sibling below
                Some ({ parent = range.parent; start = range.endd; endd = range.endd + 1 } : SiteNodeRange)
            elif delta = -1 || delta = 1 then
                let effectiveRoot = model.zoomRoot |> Option.defaultValue model.graph.root
                let moveToSib =
                    (if delta = -1 then SiteNodeRange.firstChild else SiteNodeRange.lastChild) range model.siteMap
                    |> Option.bind (fun child -> parentSiblingOpen delta child model)
                    |> Option.map (fun sib ->
                        if delta = -1 then
                            let insertIdx = model.graph.nodes.[sib.nodeId].children.Length
                            { parent = sib; start = insertIdx; endd = insertIdx } : SiteNodeRange
                        else
                            { parent = sib; start = 0; endd = 0 } : SiteNodeRange)
                let moveToGrandparent =
                    if range.parent.nodeId = effectiveRoot then None
                    else
                        range.parent.parentInstanceId
                        |> Option.bind (fun gpid -> Map.tryFind gpid model.siteMap.entries)
                        |> Option.bind (fun gp ->
                            model.graph.nodes.[gp.nodeId].children
                            |> List.tryFindIndex ((=) range.parent.nodeId)
                            |> Option.map (fun parentIdx ->
                                let insertIdx = parentIdx + (if delta = -1 then 0 else 1)
                                { parent = gp; start = insertIdx; endd = insertIdx } : SiteNodeRange))
                moveToSib |> Option.orElseWith (fun () -> moveToGrandparent)
            else None
        match too with
        | None -> model
        | Some t -> moveNodeFromTo t model dispatch

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

// ---------------------------------------------------------------------------
// Indent / Outdent (use moveNodeFromTo for edit-mode semantics)
// ---------------------------------------------------------------------------

/// Tab: make selected nodes children of the sibling immediately before them.
/// No-op if the selection starts at index 0 (no previous sibling).
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
            let too: SiteNodeRange = { parent = prevSibEntry; start = max 0 (insertIdx - 1); endd = insertIdx }
            let result = moveNodeFromTo too model dispatch |> withSiteMap
            // Ensure the new parent is expanded so the indented items are visible after reconcile
            match result.siteMap.entries
                |> Map.tryPick (fun _ e -> if e.nodeId = prevSibId then Some e else None) with
            | Some entry when not entry.expanded ->
                let siteMap, nextId =
                    ViewModel.expandEntry entry.instanceId result.graph result.siteMap result.nextInstanceId
                { result with siteMap = siteMap; nextInstanceId = nextId }
            | _ -> result

/// Shift+Tab: make selected nodes siblings of their current parent (under grandparent).
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
                let too: SiteNodeRange = { parent = grandparentEntry; start = parentIdx; endd = parentIdx + 1 }
                moveNodeFromTo too model dispatch |> withSiteMap

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
                (match model.mode with Editing (t, _) -> t | _ -> "")
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
                    mode = Editing (targetText, Some clampedPos)
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
                    | Editing (t, _) -> { model with mode = Editing (t, Some pos) }
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
                                        mode = Editing (nextNode.text, Some 0)
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
                                        mode = Editing (joinedText, Some cursorPos)
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
                            mode = Editing (joinedText, Some cursorPos)
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
type Op = VM -> (Msg -> unit) -> VM

/// Op: Move to selection mode (or deselect if already selecting), reverting any edit.
let handleEsc (model: VM) dispatch : VM =
    match model.mode with
    | Editing _ -> commitIfEditing model dispatch
    | Selecting -> collapseToFocus model
    | CommandPalette _ | CssClassPrompt _ -> model  // handled by closeCommandPaletteOp / closeCssClassPromptOp

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
    { model with mode = Editing (text, None) }

/// Op: Enter edit mode for the focused node, with cursor placed at a specific position.
let startEditAtPos (cursorPos: int) (model: VM) _dispatch : VM =
    let text =
        match model.selectedNodes with
        | None -> model.graph.nodes.[viewRootNodeId model].text
        | Some sel -> model.graph.nodes.[focusedNodeId model.graph sel].text
    { model with mode = Editing (text, Some cursorPos) }

/// Re-export palette ops for use by Controller and View.
let openCommandPaletteOp = Gambol.Client.CommandPalette.openCommandPaletteOp
let closeCommandPaletteOp = Gambol.Client.CommandPalette.closeCommandPaletteOp

/// Op: Select a specific node, committing any in-progress edit first.
let selectRow (nodeId: NodeId) (model: VM) (dispatch: Msg -> unit) : VM =
    let result =
        match model.mode, model.selectedNodes with
        | Editing (originalText, _), Some sel ->
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
        | Editing (originalText, _), Some sel ->
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
    | CssClassPrompt _ -> model
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
    | CssClassPrompt _ -> model
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

/// Op: Indent selection (Tab). moveNodeFromTo commits in-progress edits and retains edit mode.
let indentOp (model: VM) (dispatch: Msg -> unit) : VM =
    indentSelection model dispatch |> withSiteMap

/// Op: Outdent selection (Shift+Tab). moveNodeFromTo commits in-progress edits and retains edit mode.
let outdentOp (model: VM) (dispatch: Msg -> unit) : VM =
    outdentSelection model dispatch |> withSiteMap

/// Op: Move selected nodes up.
let moveNodeUpOp (model: VM) (dispatch: Msg -> unit) : VM =
    moveNodeDelta -1 model dispatch |> withSiteMap

/// Op: Move selected nodes down.
let moveNodeDownOp (model: VM) (dispatch: Msg -> unit) : VM =
    moveNodeDelta 1 model dispatch |> withSiteMap

/// Op: PageUp — cursor to start of current level (no graph move).
let pageCursorLevelStartOp (model: VM) (_dispatch: Msg -> unit) : VM =
    ViewModel.cursorLevelStart model

/// Op: PageDown — cursor to end of current level (no graph move).
let pageCursorLevelEndOp (model: VM) (_dispatch: Msg -> unit) : VM =
    ViewModel.cursorLevelEnd model

/// Op: Shift+PageDown — shift-style focus motion to end of current level.
let shiftPgDownOp (model: VM) (_dispatch: Msg -> unit) : VM =
    ViewModel.shiftPgDown model

/// Op: Shift+PageUp — shift-style focus motion to start of current level.
let shiftPgUpOp (model: VM) (_dispatch: Msg -> unit) : VM =
    ViewModel.shiftPgUp model

/// Op: Home — cursor to first direct child of view root.
let homeSelectionOp (model: VM) (_dispatch: Msg -> unit) : VM =
    ViewModel.cursorViewRootFirstChild model

/// Op: End — cursor to last direct child of view root.
let endSelectionOp (model: VM) (_dispatch: Msg -> unit) : VM =
    ViewModel.cursorViewRootLastChild model

/// Move selection with `moveNodeFromTo` when `resolveToo` returns Some.
let private tryStructuralMove
    (model: VM)
    (dispatch: Msg -> unit)
    (resolveToo: VM -> Selection -> SiteNodeRange option)
    : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        match resolveToo model sel with
        | None -> model
        | Some too -> moveNodeFromTo too model dispatch |> withSiteMap

/// Op: Ctrl+PageUp — move selected objects to start of current level; selection follows.
let moveSelectionToLevelStartOp (model: VM) (dispatch: Msg -> unit) : VM =
    tryStructuralMove model dispatch (fun _ sel ->
        Some { parent = sel.range.parent; start = 0; endd = 0 })

/// Op: Ctrl+PageDown — move selected objects to end of current level; selection follows.
let moveSelectionToLevelEndOp (model: VM) (dispatch: Msg -> unit) : VM =
    tryStructuralMove model dispatch (fun m sel ->
        let range = sel.range
        let parentLen = m.graph.nodes.[range.parent.nodeId].children.Length
        if parentLen = 0 || range.endd >= parentLen then None
        else
            Some
                { parent = range.parent
                  start = parentLen - 1
                  endd = parentLen })

/// Op: Ctrl+Home — move selected objects to first slot under view root; selection follows.
let moveSelectionToViewRootStartOp (model: VM) (dispatch: Msg -> unit) : VM =
    tryStructuralMove model dispatch (fun m sel ->
        match Map.tryFind m.siteMap.rootId m.siteMap.entries with
        | None -> None
        | Some rootEntry ->
            let n = m.graph.nodes.[rootEntry.nodeId].children.Length
            if n = 0 then None
            elif sel.range.parent.nodeId = rootEntry.nodeId && sel.range.start = 0 then None
            else
                Some { parent = rootEntry; start = 0; endd = 0 })

/// Op: Ctrl+End — move selected objects to last slot under view root; selection follows.
let moveSelectionToViewRootEndOp (model: VM) (dispatch: Msg -> unit) : VM =
    tryStructuralMove model dispatch (fun m sel ->
        match Map.tryFind m.siteMap.rootId m.siteMap.entries with
        | None -> None
        | Some rootEntry ->
            let n = m.graph.nodes.[rootEntry.nodeId].children.Length
            if n = 0 then None
            elif sel.range.parent.nodeId = rootEntry.nodeId && sel.range.endd >= n then None
            else
                Some { parent = rootEntry; start = n - 1; endd = n })

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
    model.selectedNodes
    |> Option.map (fun sel -> (focusedInstanceId sel, sel))
    |> Option.bind (fun (fid, _) ->
        Map.tryFind fid model.siteMap.entries
        |> Option.map (fun entry -> (entry, fid)))
    |> Option.map (fun (entry, focusInstId) ->
        let node = model.graph.nodes.[entry.nodeId]
        if not node.children.IsEmpty && entry.expanded then
            { model with siteMap = ViewModel.toggleFold focusInstId model.siteMap }
        else
            entry.parentInstanceId
            |> Option.bind (singleSelectionForInstance model.siteMap)
            |> Option.map (fun parentSel -> { model with selectedNodes = Some parentSel })
            |> Option.defaultValue model)
    |> Option.defaultValue model

/// Op: ArrowLeft in selection — move to parent (do not fold).
let arrowLeftSelectionNoFoldOp (model: VM) _dispatch : VM =
    model.selectedNodes
    |> Option.map focusedInstanceId
    |> Option.bind (fun fid -> Map.tryFind fid model.siteMap.entries)
    |> Option.bind (fun e -> e.parentInstanceId)
    |> Option.bind (singleSelectionForInstance model.siteMap)
    |> Option.map (fun ps -> { model with selectedNodes = Some ps })
    |> Option.defaultValue model

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
            | Some m -> m
                // let newEnd = sel.range.endd + selectedIds.Length
                // let newSel = { range = { sel.range with endd = newEnd }; focus = sel.focus }
                // { m with selectedNodes = Some newSel }
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

/// Union of user classes (non-amb-) across selected nodes, as space-separated string.
let private initialUserClassesForSelection (model: VM) (sel: Selection) : string =
    let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
    let selectedIds =
        parentNode.children
        |> List.skip sel.range.start
        |> List.take (sel.range.endd - sel.range.start)
    selectedIds
    |> List.collect (fun nid ->
        model.graph.nodes.[nid].cssClasses
        |> CssClass.toList
        |> List.filter (fun c -> not (c.StartsWith("amb-"))))
    |> List.distinct
    |> String.concat " "

/// Op: Open the CSS class prompt overlay, pre-filled with current user classes (amb- excluded).
let openCssClassPromptOp (model: VM) _dispatch : VM =
    match model.selectedNodes with
    | None -> model
    | Some sel -> { model with mode = CssClassPrompt (model.mode, initialUserClassesForSelection model sel) }

/// Op: Close the CSS class prompt without applying.
let closeCssClassPromptOp (model: VM) _dispatch : VM =
    match model.mode with
    | CssClassPrompt (ret, _) -> { model with mode = ret }
    | _ -> model

[<Emit("document.getElementById('css-class-prompt-input')?.value ?? ''")>]
let private readCssClassPromptValue () : string = jsNative

/// Op: Substitute user classes from prompt input (old → new), preserving amb- classes. Close.
let submitCssClassPromptOp (model: VM) (dispatch: Msg -> unit) : VM =
    match model.mode, model.selectedNodes with
    | CssClassPrompt (ret, _), Some sel ->
        let input = readCssClassPromptValue ()
        let newUserClasses = CssClass.parseUserClasses (if isNull input then "" else input)
        let result = { model with mode = ret }
        let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
        let selectedIds =
            parentNode.children
            |> List.skip sel.range.start
            |> List.take (sel.range.endd - sel.range.start)
        let ops =
            selectedIds
            |> List.choose (fun nid ->
                let node = model.graph.nodes.[nid]
                let oldClasses = node.cssClasses
                let ambClasses = CssClass.ambOnly oldClasses
                let newClasses = CssClass.toList ambClasses @ CssClass.toList newUserClasses |> CssClass.ofList
                if oldClasses = newClasses then None
                else Some (Op.SetClasses(nid, oldClasses, newClasses)))
        if ops.IsEmpty then result
        else
            let change =
                { id = model.revision.Value
                  changeId = System.Guid.NewGuid()
                  ops = ops }
            match applyAndPost change result dispatch with
            | Some m -> m
            | None -> result
    | _ -> model

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
        let firstId = firstSelectedNodeId model'.graph sel
        let firstNode = model'.graph.nodes.[firstId]
        let zoomId = 
            if firstNode.children.IsEmpty
            then
                match Graph.tryFindParentAndIndex firstId model'.graph with
                | Some (parentId, _) -> parentId
                | None -> firstId
            else firstId
        let siteMap, nextId =
            ViewModel.buildSiteMapFrom model'.graph zoomId model'.nextInstanceId
        { model' with
            zoomRoot = Some zoomId
            siteMap = siteMap
            nextInstanceId = nextId
            selectedNodes = firstChildSelection siteMap zoomId
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

    | System PollingInactive ->
        if model.syncState = Synced && model.pendingChanges.IsEmpty then
            { model with syncState = Inactive }
        else
            model

    | System PollingActive ->
        if model.syncState = Inactive then
            { model with syncState = Synced }
        else
            model

