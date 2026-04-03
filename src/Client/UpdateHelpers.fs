module Gambol.Client.UpdateHelpers

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Client.JsInterop
open Gambol.Shared
open Gambol.Shared.Paste
open Gambol.Shared.ViewModel
open Thoth.Json.Core

// ---------------------------------------------------------------------------
// File identity (derived from URL path)
// ---------------------------------------------------------------------------

let currentFile =
    let path = Browser.Dom.window.location.pathname
    if path.StartsWith("/") then path.Substring(1) else path

// ---------------------------------------------------------------------------
// Pending-queue localStorage persistence
// ---------------------------------------------------------------------------

let private pendingKey = "gambol-pending-v1"

let savePendingQueue (changes: Change list) =
    if changes.IsEmpty then localStorageRemove pendingKey
    else
        let encoded = Encode.list (changes |> List.map Serialization.encodeChange)
        let json = Thoth.Json.JavaScript.Encode.toString 0 encoded
        localStorageSet pendingKey json

let loadPendingQueue () : Change list =
    let json = localStorageGet pendingKey
    if isNull json || json = "" then []
    else
        match Thoth.Json.JavaScript.Decode.fromString
            (Decode.list Serialization.decodeChange) json with
        | Ok cs -> cs
        | Error _ -> []

// ---------------------------------------------------------------------------
// Update helpers
// ---------------------------------------------------------------------------

/// Read the live edit field text from the DOM (`contentEditable` `div#edit-input`).
let readEditInputValue () : string =
    let el = document.getElementById "edit-input"
    if isNull el then ""
    else
        let t = el.textContent
        if isNull t then "" else t

/// Read caret start offset (UTF-16) within `#edit-input`.
let readEditInputCursor () : int =
    let el = document.getElementById "edit-input"
    if isNull el then 0
    else getContentEditableSelectionStart el

/// Read selection end offset within `#edit-input`.
let readEditInputSelectionEnd () : int =
    let el = document.getElementById "edit-input"
    if isNull el then 0
    else getContentEditableSelectionEnd el

/// Apply a change to the local model, enqueue it for posting to the server,
/// and return the updated VM (or None if the change was rejected locally) plus effects.
/// Fires SubmitChange only when the queue was empty and no request is in-flight.
/// Blocked states (ServerRejected / CodeOutdated / DataOutdated / WaitingToRetry) queue
/// changes locally but do not fire a POST.
let applyAndPost (change: Change) (model: VM) : VM option * Effect list =
    let state: State = { graph = model.graph; revision = model.revision; history = model.history }
    match History.applyChange change state with
    | ApplyResult.Changed newState ->
        let pending = model.syncInfo.pendingChanges @ [change]
        let nextSyncInfo, submitEffects =
            { model.syncInfo with pendingChanges = pending }
            |> SyncPlanner.tryStartSubmit model.revision
        if not submitEffects.IsEmpty then
            consoleLog (
                "[Gambol sync] applyAndPost fireFirst modelRev=" + string model.revision.Value
                + " qLen=" + string pending.Length)
        let effects = (SavePendingQueue pending) :: submitEffects
        Some
            { model with
                graph = newState.graph
                history = newState.history
                syncInfo = nextSyncInfo }, effects
    | _ -> None, []

/// Extract the child span covered by a SiteNodeRange.
let rangeChildren (graph: Graph) (range: SiteNodeRange) =
    graph.nodes.[range.parent.nodeId].children
    |> List.skip range.start
    |> List.take (range.endd - range.start)

let ownedChild (id: NodeId) : ChildNode =
    { ref = Ownership.Owner; id = id }

let ownedChildren (ids: NodeId list) : ChildNode list =
    ids |> List.map ownedChild

let childrenForPaste (graph: Graph) (ids: NodeId list) : ChildNode list =
    let existingOwnerIds =
        graph.nodes
        |> Map.values
        |> Seq.collect (fun node -> node.children)
        |> Seq.choose (fun child ->
            match child.ref with
            | Ownership.Owner -> Some child.id
            | Ownership.Ref -> None)
        |> Set.ofSeq

    let folder (seenOwnerIds, children) id =
        let ownership =
            if Set.contains id seenOwnerIds then Ownership.Ref else Ownership.Owner

        let child = { ref = ownership; id = id }
        Set.add id seenOwnerIds, child :: children

    let _, childrenRev = ids |> List.fold folder (existingOwnerIds, [])
    childrenRev |> List.rev

/// The node being edited when selectedNodes = None: the zoom root if zoomed, else the graph root.
let viewRootNodeId (model: VM) : NodeId =
    model.zoomRoot |> Option.defaultValue model.graph.root

/// If `newText` differs from the graph, the same `SetText` op `commitTextEdit` would post (no mode change).
let tryTextCommitOps (nodeId: NodeId) (originalTextForHistory: string) (newText: string) (graph: Graph) : Op list =
    let modelText = graph.nodes.[nodeId].text
    if newText = modelText then [] else [ Op.SetText(nodeId, originalTextForHistory, newText) ]

/// Apply a committed text edit to the model and POST to server.
/// Returns the updated model and any effects.
let commitTextEdit
    (nodeId: NodeId)
    (_originalText: string)
    (newText: string)
    (model: VM)
    : VM * Effect list =
    match tryTextCommitOps nodeId _originalText newText model.graph with
    | [] -> { model with mode = Selecting }, []
    | ops ->
        let change: Change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = ops }
        match applyAndPost change model with
        | Some m, effects -> { m with mode = Selecting }, effects
        | None, _         -> { model with mode = Selecting }, []

/// Split the currently-edited node at the cursor position.
///
/// cursor at 0   → blank sibling inserted above; current node keeps its text; focus moves to the new blank node.
/// cursor > 0    → current node gets text-before; new sibling gets text-after; focus at start of new node.
let splitNode (currentText: string) (cursorPos: int) (model: VM) : VM * Effect list =
    match model.mode, model.selectedNodes with
    | Editing (originalText, _), None ->
        // View root is being edited: commit text, no split
        commitTextEdit (viewRootNodeId model) originalText (readEditInputValue ()) model
    | Editing (originalText, _), Some sel ->
        // The node being edited is the focus node.
        let selectedId  = focusedNodeId model.graph sel
        let modelText = model.graph.nodes.[selectedId].text
        let parentId    = sel.range.parent.nodeId
        let indexInParent = sel.focus
        let clampedPos = max 0 (min cursorPos currentText.Length)
        let textBefore = currentText.[..clampedPos - 1]
        let textAfter  = currentText.[clampedPos..]
        let newChild = ChildNode.New()

        let (insertIndex, newNodeText, focusId, focusText) =
            if clampedPos = 0 then
                // blank node above; focus moves to the new blank node
                (indexInParent, "", newChild.id, "")
            else
                // new node after; focus moves to new node
                (indexInParent + 1, textAfter, newChild.id, textAfter)

        let ops =
            [ yield Op.NewNode(newChild.id, newNodeText)
              yield Op.Replace(parentId, insertIndex, [], [ newChild ])
              // update current node's text only when it actually changes
              let updatedText = if clampedPos = 0 then currentText else textBefore
              if updatedText <> modelText then
                  yield Op.SetText(selectedId, modelText, updatedText) ]

        let change: Change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = ops }
        match applyAndPost change model with
        | Some m, effects ->
            let effRoot =
                m.zoomRoot
                |> Option.filter (fun zr -> Map.containsKey zr m.graph.nodes)
                |> Option.defaultValue m.graph.root
            let siteMap, nextId =
                ViewModel.reconcileSiteMapFrom m.graph effRoot m.siteMap m.nextSiteId
            let m2 = { m with siteMap = siteMap; nextSiteId = nextId }
            let focusInstId =
                if clampedPos = 0 then focusedInstanceId sel
                else
                    match Map.tryFind sel.range.parent.instanceId m2.siteMap.entries with
                    | Some p when insertIndex < p.children.Length ->
                        p.children.[insertIndex]
                    | _ -> Sid -1
            let newSel =
                singleSelectionForInstance m2.siteMap focusInstId
                |> Option.orElseWith
                    (fun () -> singleSelection m2.graph m2.siteMap focusId)
            { m2 with
                selectedNodes = newSel
                mode = Editing (focusText, EditCaret.Utf16Index 0) }, effects
        | None, _ -> model, []
    | _ -> model, []

/// If currently editing, commit the edit and return Selecting model; otherwise return model as-is.
let commitIfEditing (model: VM) : VM * Effect list =
    match model.mode, model.selectedNodes with
    | Editing (originalText, _), None ->
        commitTextEdit (viewRootNodeId model) originalText (readEditInputValue ()) model
    | Editing (originalText, _), Some sel ->
        let editingId = focusedNodeId model.graph sel
        commitTextEdit editingId originalText (readEditInputValue ()) model
    | _ -> model, []

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
        ViewModel.reconcileSiteMapFrom model.graph effectiveRoot model.siteMap model.nextSiteId
    let model' = { model with siteMap = siteMap; nextSiteId = nextId; zoomRoot = zoomRoot }
    match model'.selectedNodes with
    | None -> model'
    | Some sel ->
        match Map.tryFind sel.range.parent.instanceId model'.siteMap.entries with
        | None -> model'
        | Some freshParent ->
            { model' with
                selectedNodes = Some { sel with range = { sel.range with parent = freshParent } } }

// ---------------------------------------------------------------------------
// Move-edit caret (shared with UpdateEdit)
// ---------------------------------------------------------------------------

type MoveEditCaret =
    | MoveEditUtf16 of int
    | MoveEditPrevLastLineX of float
    | MoveEditNextFirstLineX of float

let moveEditImpl (delta: int) (how: MoveEditCaret) (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let currentId = focusedNodeId model.graph sel
        let focusInstId = focusedInstanceId sel
        let committed, effects =
            commitTextEdit currentId
                (match model.mode with Editing (t, _) -> t | _ -> "")
                (readEditInputValue ()) model
        let rows = getVisibleRowInstanceIds committed.siteMap
        match rows |> List.tryFindIndex ((=) focusInstId) with
        | None -> committed, effects
        | Some idx ->
            let targetIdx = idx + delta
            if targetIdx < 0 || targetIdx >= rows.Length then committed, effects
            else
                let targetInstId = rows.[targetIdx]
                let targetEntry = committed.siteMap.entries.[targetInstId]
                let targetText = committed.graph.nodes.[targetEntry.nodeId].text
                let caret: EditCaret =
                    match how with
                    | MoveEditUtf16 pos ->
                        EditCaret.utf16ClampedToLength pos targetText.Length
                    | MoveEditPrevLastLineX x -> EditCaret.LastVisualLineAtClientX x
                    | MoveEditNextFirstLineX x -> EditCaret.FirstVisualLineAtClientX x
                { committed with
                    mode = Editing (targetText, caret)
                    selectedNodes = singleSelectionForInstance committed.siteMap targetInstId }, effects
