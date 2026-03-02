module Gambol.Client.Update

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Shared
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
        match model.mode, model.selectedNodes with
        | Editing (originalText, _), Some sel ->
            let editingId = focusedNodeId model.graph sel
            let newText = readEditInputValue ()
            let model' = commitTextEdit editingId originalText newText model dispatch
            moveSelectionBy -1 model'
        | _ ->
            applyMoveSelectionUp model

    | MoveSelectionDown ->
        match model.mode, model.selectedNodes with
        | Editing (originalText, _), Some sel ->
            let editingId = focusedNodeId model.graph sel
            let newText = readEditInputValue ()
            let model' = commitTextEdit editingId originalText newText model dispatch
            moveSelectionBy 1 model'
        | _ ->
            applyMoveSelectionDown model

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
