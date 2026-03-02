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
let applyAndPost (change: Change) (model: Model) (dispatch: Msg -> unit) : Graph option =
    let state: State = { graph = model.graph; history = History.empty }
    match Change.apply change state with
    | ApplyResult.Changed newState ->
        let body = encodeChangeBody change
        postJson $"/{currentFile}/changes" body (fun responseText ->
            match decodeStateResponse responseText with
            | Ok (_graph, rev) -> dispatch (SubmitResponse rev)
            | Error _err -> ()
        )
        Some newState.graph
    | _ -> None

/// Apply a committed text edit to the model and POST to server.
/// Returns the updated model. Dispatches SubmitResponse asynchronously.
let commitTextEdit
    (nodeId: NodeId)
    (originalText: string)
    (newText: string)
    (model: Model)
    (dispatch: Msg -> unit)
    : Model =
    if newText = originalText then
        { model with mode = Selecting }
    else
        let change: Change = { id = model.revision.Value; ops = [ Op.SetText(nodeId, originalText, newText) ] }
        match applyAndPost change model dispatch with
        | Some graph -> { model with graph = graph; mode = Selecting }
        | None       -> { model with mode = Selecting }

/// Split the currently-edited node at the cursor position.
///
/// cursor at 0   → blank sibling inserted above; current node keeps its text; focus at start of current node.
/// cursor > 0    → current node gets text-before; new sibling gets text-after; focus at start of new node.
let splitNode (currentText: string) (cursorPos: int) (model: Model) (dispatch: Msg -> unit) : Model =
    match model.mode, model.selectedNodes with
    | Editing (originalText, _), Some sel ->
        // The node being edited is the focus node.
        let selectedId  = focusedNodeId model.graph sel
        let parentId    = sel.range.parent
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
        | Some graph ->
            { model with
                graph = graph
                selectedNodes = singleSelection graph focusId
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
let pasteNodes (pastedText: string) (model: Model) (dispatch: Msg -> unit) : Model =
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
            let parentNode = model.graph.nodes.[range.parent]
            let selectedIds = parentNode.children |> List.skip range.start |> List.take (range.endd - range.start)
            let replaceOp = Op.Replace(range.parent, range.start, selectedIds, topLevelIds)
            let change = { id = model.revision.Value; ops = pasteOps @ [replaceOp] }
            match applyAndPost change model dispatch with
            | Some graph ->
                let newEnd = range.start + topLevelIds.Length
                let newSel = { range = { parent = range.parent; start = range.start; endd = newEnd }; focus = range.start }
                { model with graph = graph; selectedNodes = Some newSel }
            | None -> model
        | Editing (originalText, _) ->
            let currentText = readEditInputValue ()
            let cursorPos   = readEditInputCursor ()
            let focusId  = focusedNodeId model.graph sel
            let parentId = sel.range.parent
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
                | Some graph -> { model with graph = graph; mode = Selecting }
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
                | Some graph -> { model with graph = graph; mode = Selecting }
                | None -> model

// ---------------------------------------------------------------------------
// Indent / Outdent
// ---------------------------------------------------------------------------

/// Tab: make selected nodes children of the sibling immediately before them.
/// No-op if the selection starts at index 0 (no previous sibling).
/// Move the selected nodes from their current parent to a new parent at insertIdx.
/// Common core of indent and outdent.
let reparentSelection (newParentId: NodeId) (insertIdx: int) (sel: Selection) (model: Model) (dispatch: Msg -> unit) : Model =
    let range = sel.range
    let selectedIds = model.graph.nodes.[range.parent].children |> List.skip range.start |> List.take (range.endd - range.start)
    let ops =
        [ Op.Replace(range.parent, range.start, selectedIds, [])
          Op.Replace(newParentId,  insertIdx,   [],          selectedIds) ]
    let change = { id = model.revision.Value; ops = ops }
    match applyAndPost change model dispatch with
    | Some graph ->
        let newSel = { range = { parent = newParentId; start = insertIdx; endd = insertIdx + selectedIds.Length }; focus = insertIdx }
        { model with graph = graph; selectedNodes = Some newSel }
    | None -> model

let indentSelection (model: Model) (dispatch: Msg -> unit) : Model =
    match model.selectedNodes with
    | None -> model
    | Some sel when sel.range.start = 0 -> model  // no previous sibling — no-op
    | Some sel ->
        let prevSibId = model.graph.nodes.[sel.range.parent].children.[sel.range.start - 1]
        let insertIdx = model.graph.nodes.[prevSibId].children.Length
        reparentSelection prevSibId insertIdx sel model dispatch

let outdentSelection (model: Model) (dispatch: Msg -> unit) : Model =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        match tryFindParentAndIndex model.graph sel.range.parent with
        | None -> model  // parent is root — no-op
        | Some (grandparentId, parentIdx) ->
            reparentSelection grandparentId (parentIdx + 1) sel model dispatch

/// If currently editing, commit the edit and return Selecting model; otherwise return model as-is.
let commitIfEditing (model: Model) (dispatch: Msg -> unit) : Model =
    match model.mode, model.selectedNodes with
    | Editing (originalText, _), Some sel ->
        let editingId = focusedNodeId model.graph sel
        commitTextEdit editingId originalText (readEditInputValue ()) model dispatch
    | _ -> model

/// Alt+Up/Down: swap the selected range with the adjacent sibling using a single Op.Replace.
let moveNode (delta: int) (model: Model) (dispatch: Msg -> unit) : Model =
    match model.selectedNodes with
    | None -> model
    | Some sel ->
        let range       = sel.range
        let parentNode  = model.graph.nodes.[range.parent]
        let selectedIds = parentNode.children |> List.skip range.start |> List.take (range.endd - range.start)
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
            let change = { id = model.revision.Value; ops = [ Op.Replace(range.parent, opStart, oldSpan, newSpan) ] }
            match applyAndPost change model dispatch with
            | Some graph ->
                let newSel = { range = { range with start = newStart; endd = newStart + selectedIds.Length }; focus = sel.focus + delta }
                { model with graph = graph; selectedNodes = Some newSel }
            | None -> model

// ---------------------------------------------------------------------------
// Update
// ---------------------------------------------------------------------------

/// Update function. The dispatch parameter is needed for async effects
/// (server POST callbacks).
let update (msg: Msg) (model: Model) (dispatch: Msg -> unit) : Model =
    match msg with
    | StateLoaded (graph, revision) ->
        { graph = graph
          revision = revision
          selectedNodes = None
          mode = Selecting }

    | SelectRow nodeId ->
        match model.mode, model.selectedNodes with
        | Editing (originalText, _), Some sel ->
            // Commit current edit, then select new row
            let editingId = focusedNodeId model.graph sel
            let newText = readEditInputValue ()
            let model' = commitTextEdit editingId originalText newText model dispatch
            { model' with selectedNodes = singleSelection model'.graph nodeId }
        | _ ->
            { model with selectedNodes = singleSelection model.graph nodeId; mode = Selecting }

    | MoveSelectionUp ->
        match model.mode with
        | Editing _ -> moveSelectionBy -1 (commitIfEditing model dispatch)
        | Selecting -> applyMoveSelectionUp model

    | MoveSelectionDown ->
        match model.mode with
        | Editing _ -> moveSelectionBy 1 (commitIfEditing model dispatch)
        | Selecting -> applyMoveSelectionDown model

    | StartEdit _prefill ->
        match model.selectedNodes with
        | None -> model // nothing selected, ignore
        | Some sel ->
            let nodeId = focusedNodeId model.graph sel
            let node = model.graph.nodes.[nodeId]
            { model with mode = Editing (node.text, None) }  // None = cursor at end

    | ShiftArrowUp   -> shiftArrow -1 model
    | ShiftArrowDown -> shiftArrow  1 model

    | SplitNode (currentText, cursorPos) ->
        splitNode currentText cursorPos model dispatch

    | IndentSelection  -> indentSelection  (commitIfEditing model dispatch) dispatch
    | OutdentSelection -> outdentSelection (commitIfEditing model dispatch) dispatch
    | MoveNodeUp       -> moveNode -1      (commitIfEditing model dispatch) dispatch
    | MoveNodeDown     -> moveNode  1      (commitIfEditing model dispatch) dispatch

    | CancelEdit ->
        match model.mode with
        | Editing _ ->
            // In edit mode: revert text, return to selection mode
            { model with mode = Selecting }
        | Selecting ->
            // In selection mode: deselect
            { model with selectedNodes = None }

    | SubmitResponse revision ->
        { model with revision = revision }

    | PasteNodes pastedText ->
        pasteNodes pastedText model dispatch
