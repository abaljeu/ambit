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
// Encoding / decoding helpers
// ---------------------------------------------------------------------------

/// Encode the body for POST /submit
let encodeSubmitBody (change: Change) (revision: Revision) : string =
    let encoded =
        Encode.object
            [ "clientRevision", Serialization.encodeRevision revision
              "change", Serialization.encodeChange change ]
    Thoth.Json.JavaScript.Encode.toString 0 encoded

/// Decode the response from POST /submit (just need revision)
let decodeSubmitResponse (text: string) : Result<Revision, string> =
    let decoder =
        Decode.object (fun get ->
            get.Required.Field "revision" Serialization.decodeRevision)
    Thoth.Json.JavaScript.Decode.fromString decoder text

/// Decode the initial GET /state response
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

/// Flatten graph into visible row order (preorder, excluding root).
let getVisibleRowIds (graph: Graph) : NodeId list =
    let rec gather (nodeId: NodeId) : NodeId list =
        let node = graph.nodes.[nodeId]
        nodeId :: (node.children |> List.collect gather)

    let root = graph.nodes.[graph.root]
    root.children |> List.collect gather

/// Move current selection by delta (-1 for up, +1 for down) in visible row order.
let moveSelectionBy (delta: int) (model: Model) : Model =
    match model.selectedNode with
    | None -> model
    | Some selectedId ->
        let rows = getVisibleRowIds model.graph
        match rows |> List.tryFindIndex ((=) selectedId) with
        | None -> model
        | Some currentIndex ->
            let nextIndex = currentIndex + delta
            if nextIndex < 0 || nextIndex >= rows.Length then
                model
            else
                { model with selectedNode = Some rows[nextIndex]; mode = Selection }

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
        // No change — just exit edit mode
        { model with mode = Selection }
    else
        let op = Op.SetText(nodeId, originalText, newText)
        let change: Change = { id = 0; ops = [ op ] }
        let state: State = { graph = model.graph; history = History.empty }
        match Change.apply change state with
        | ApplyResult.Changed newState ->
            // POST to server in background (optimistic)
            let body = encodeSubmitBody change model.revision
            postJson "/submit" body (fun responseText ->
                match decodeSubmitResponse responseText with
                | Ok rev -> dispatch (SubmitResponse rev)
                | Error _err -> () // MVP: ignore errors
            )
            { model with graph = newState.graph; mode = Selection }
        | ApplyResult.Invalid (_s, _err) ->
            // Op failed locally — just exit edit mode, don't POST
            { model with mode = Selection }
        | ApplyResult.Unchanged _s ->
            { model with mode = Selection }

/// Insert a new empty sibling below the selected node (selection mode).
let insertSibling (model: Model) (dispatch: Msg -> unit) : Model =
    match model.mode, model.selectedNode with
    | Selection, Some selectedId ->
        match tryFindParentAndIndex model.graph selectedId with
        | None -> model
        | Some (parentId, indexInParent) ->
            let newNodeId = NodeId.New()
            let change: Change =
                { id = 0
                  ops =
                    [ Op.NewNode(newNodeId, "")
                      Op.Replace(parentId, indexInParent + 1, [], [ newNodeId ]) ] }
            let state: State = { graph = model.graph; history = History.empty }
            match Change.apply change state with
            | ApplyResult.Changed newState ->
                let body = encodeSubmitBody change model.revision
                postJson "/submit" body (fun responseText ->
                    match decodeSubmitResponse responseText with
                    | Ok rev -> dispatch (SubmitResponse rev)
                    | Error _err -> ()
                )
                { model with
                    graph = newState.graph
                    selectedNode = Some newNodeId
                    mode = Selection }
            | ApplyResult.Invalid (_s, _err) -> model
            | ApplyResult.Unchanged _s -> model
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
          selectedNode = None
          mode = Selection }

    | SelectRow nodeId ->
        match model.mode, model.selectedNode with
        | Editing originalText, Some editingId ->
            // Commit current edit, then select new row
            let newText = readEditInputValue ()
            let model' = commitTextEdit editingId originalText newText model dispatch
            { model' with selectedNode = Some nodeId }
        | _ ->
            { model with selectedNode = Some nodeId; mode = Selection }

    | MoveSelectionUp ->
        match model.mode, model.selectedNode with
        | Editing originalText, Some editingId ->
            let newText = readEditInputValue ()
            let model' = commitTextEdit editingId originalText newText model dispatch
            moveSelectionBy -1 model'
        | _ ->
            moveSelectionBy -1 model

    | MoveSelectionDown ->
        match model.mode, model.selectedNode with
        | Editing originalText, Some editingId ->
            let newText = readEditInputValue ()
            let model' = commitTextEdit editingId originalText newText model dispatch
            moveSelectionBy 1 model'
        | _ ->
            moveSelectionBy 1 model

    | StartEdit _prefill ->
        match model.selectedNode with
        | None -> model // nothing selected, ignore
        | Some _nodeId ->
            let node = model.graph.nodes.[_nodeId]
            { model with mode = Editing node.text }

    | CommitEdit newText ->
        match model.mode, model.selectedNode with
        | Editing originalText, Some nodeId ->
            commitTextEdit nodeId originalText newText model dispatch
        | _ -> model

    | InsertSibling ->
        insertSibling model dispatch

    | CancelEdit ->
        match model.mode with
        | Editing _ ->
            // In edit mode: revert text, return to selection mode
            { model with mode = Selection }
        | Selection ->
            // In selection mode: deselect
            { model with selectedNode = None }

    | SubmitResponse revision ->
        { model with revision = revision }
