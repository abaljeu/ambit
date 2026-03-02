module Gambol.Client.Update

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Shared
open Gambol.Client
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

/// Find parent and index of a node in the current graph.
let tryFindParentAndIndex (graph: Graph) (targetId: NodeId) : (NodeId * int) option =
    graph.nodes
    |> Map.toSeq
    |> Seq.tryPick (fun (parentId, parent) ->
        parent.children
        |> List.tryFindIndex ((=) targetId)
        |> Option.map (fun index -> parentId, index))

/// Build a single-node NodeRange for the given nodeId, using the graph to locate its parent.
/// Returns None if the node has no parent (i.e. it is the root).
let singleNodeRange (graph: Graph) (nodeId: NodeId) : NodeRange option =
    tryFindParentAndIndex graph nodeId
    |> Option.map (fun (parentId, index) -> { parent = parentId; start = index; endd = index + 1 })

/// Extract the primary (first) selected NodeId from a NodeRange.
let firstSelectedNodeId (graph: Graph) (range: NodeRange) : NodeId =
    graph.nodes.[range.parent].children.[range.start]

/// Flatten graph into visible row order (preorder, excluding root).
let getVisibleRowIds (graph: Graph) : NodeId list =
    let rec gather (nodeId: NodeId) : NodeId list =
        let node = graph.nodes.[nodeId]
        nodeId :: (node.children |> List.collect gather)

    let root = graph.nodes.[graph.root]
    root.children |> List.collect gather

/// Extend the selection range by one step within its parent.
/// delta = -1 grows start upward; +1 grows endd downward.
/// Has no effect when nothing is selected or when the boundary is already reached.
let extendSelection (delta: int) (model: Model) : Model =
    match model.selectedNodes with
    | None -> model
    | Some range ->
        let childCount = model.graph.nodes.[range.parent].children.Length
        if delta < 0 then
            let newStart = max 0 (range.start + delta)
            { model with selectedNodes = Some { range with start = newStart } }
        else
            let newEndd = min childCount (range.endd + delta)
            { model with selectedNodes = Some { range with endd = newEndd } }

/// Move current selection by delta (-1 for up, +1 for down) in visible row order.
/// When the selection spans multiple nodes, movement is relative to the first node;
/// the resulting selection is always a single-node range.
let moveSelectionBy (delta: int) (model: Model) : Model =
    match model.selectedNodes with
    | None -> model
    | Some range ->
        let selectedId = firstSelectedNodeId model.graph range
        let rows = getVisibleRowIds model.graph
        match rows |> List.tryFindIndex ((=) selectedId) with
        | None -> model
        | Some currentIndex ->
            let nextIndex = currentIndex + delta
            if nextIndex < 0 || nextIndex >= rows.Length then
                model
            else
                let nextId = rows[nextIndex]
                match singleNodeRange model.graph nextId with
                | None -> model
                | Some newRange -> { model with selectedNodes = Some newRange; mode = Selecting }

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
    | Editing (originalText, _), Some range ->
        // Use range's parent + start directly (avoids re-scanning the graph).
        let selectedId  = firstSelectedNodeId model.graph range
        let parentId    = range.parent
        let indexInParent = range.start
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
                selectedNodes = singleNodeRange graph focusId
                mode = Editing (focusText, Some 0) }
        | None -> model
    | _ -> model


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
        | Editing (originalText, _), Some range ->
            // Commit current edit, then select new row
            let editingId = firstSelectedNodeId model.graph range
            let newText = readEditInputValue ()
            let model' = commitTextEdit editingId originalText newText model dispatch
            { model' with selectedNodes = singleNodeRange model'.graph nodeId }
        | _ ->
            { model with selectedNodes = singleNodeRange model.graph nodeId; mode = Selecting }

    | MoveSelectionUp ->
        match model.mode, model.selectedNodes with
        | Editing (originalText, _), Some range ->
            let editingId = firstSelectedNodeId model.graph range
            let newText = readEditInputValue ()
            let model' = commitTextEdit editingId originalText newText model dispatch
            moveSelectionBy -1 model'
        | _ ->
            moveSelectionBy -1 model

    | MoveSelectionDown ->
        match model.mode, model.selectedNodes with
        | Editing (originalText, _), Some range ->
            let editingId = firstSelectedNodeId model.graph range
            let newText = readEditInputValue ()
            let model' = commitTextEdit editingId originalText newText model dispatch
            moveSelectionBy 1 model'
        | _ ->
            moveSelectionBy 1 model

    | StartEdit _prefill ->
        match model.selectedNodes with
        | None -> model // nothing selected, ignore
        | Some range ->
            let nodeId = firstSelectedNodeId model.graph range
            let node = model.graph.nodes.[nodeId]
            { model with mode = Editing (node.text, None) }  // None = cursor at end

    | ChangeSelectionUp   -> extendSelection -1 model
    | ExtendSelectionDown  -> extendSelection  1 model

    | SplitNode (currentText, cursorPos) ->
        splitNode currentText cursorPos model dispatch

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
