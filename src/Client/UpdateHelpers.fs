module Gambol.Client.UpdateHelpers

open Browser.Dom
open Fable.Core.JsInterop
open Gambol.Client.JsInterop
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Shared.ViewModelMoveOps
open Thoth.Json.Core

// ---------------------------------------------------------------------------
// File identity (derived from URL path)
// ---------------------------------------------------------------------------

let currentFile =
    let path = Browser.Dom.window.location.pathname
    if path.StartsWith("/") then path.Substring(1) else path

// ---------------------------------------------------------------------------
// Mutating POST headers (X-Gambol-Client from getClientHint)
// ---------------------------------------------------------------------------

let private withClientIdentity (extra: (string * obj) list) : obj =
    let hint = ClientIdentity.normalize (getClientHint ())
    createObj ((ClientIdentity.HeaderName ==> hint) :: extra)

let emptyMutatingPostHeaders () : obj =
    withClientIdentity []

let jsonMutatingPostHeaders () : obj =
    withClientIdentity [ "Content-Type" ==> "application/json" ]

// ---------------------------------------------------------------------------
// Pending-queue localStorage persistence
// ---------------------------------------------------------------------------

let private pendingKey = "gambol-pending-v1"

let savePendingQueue (actions: HistoryAction list) =
    if actions.IsEmpty then localStorageRemove pendingKey
    else
        let encoded =
            Encode.list (actions |> List.map Serialization.encodeHistoryAction)
        let json = Thoth.Json.JavaScript.Encode.toString 0 encoded
        localStorageSet pendingKey json

let loadPendingQueue () : HistoryAction list =
    let json = localStorageGet pendingKey
    if isNull json || json = "" then []
    else
        match Thoth.Json.JavaScript.Decode.fromString
            (Decode.list Serialization.decodeHistoryAction) json with
        | Ok actions -> actions
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
/// and return the updated VM plus effects, or an error if rejected locally.
/// Fires SubmitPendingBatch only when the queue was empty and no request is in-flight.
/// Blocked states (ServerRejected / CodeOutdated / DataOutdated / WaitingToRetry) queue
/// changes locally but do not fire a POST.
let applyAndPost (change: Change) (model: VM) : Result<VM * Effect list, string> =
    let state: State = { graph = model.graph; revision = model.revision; history = model.history }
    let action = HistoryAction.Change change
    match
        SyncPlanner.applyAndEnqueueLocalAction
            action
            state
            model.syncInfo
    with
    | Ok (newState, nextSyncInfo, effects) ->
        if
            effects
            |> List.exists (function
                | SubmitPendingBatch _ -> true
                | _ -> false)
        then
            consoleLog (
                "[Gambol sync] applyAndPost fireFirst modelRev=" + string model.revision.Value
                + " qLen=" + string nextSyncInfo.pendingChanges.Length)
        Ok
            ({ model with
                graph = newState.graph
                history = newState.history
                syncInfo = nextSyncInfo }, effects)
    | Error error -> Error error

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
    model.zoomRoot

/// True when autosync from a poll response must not proceed:
///   - pending queue is non-empty (defensive; tryStartPoll already blocks this)
///   - mode is Editing and the live edit field differs from the graph (dirty edit)
let isAutoSyncBlocked (model: VM) : bool =
    if not model.syncInfo.pendingChanges.IsEmpty then
        true
    else
        match model.mode with
        | Editing _ ->
            let editingId =
                match model.selectedNodes with
                | Some sel -> focusedNodeId model.graph sel
                | None -> viewRootNodeId model
            let graphText =
                model.graph.nodes
                |> Map.tryFind editingId
                |> Option.map (fun n -> n.text)
                |> Option.defaultValue ""
            readEditInputValue () <> graphText
        | _ -> false

/// After applying server changes, keep Editing mode only if the server didn't
/// touch the node being edited. Otherwise switch to Selecting.
let adjustModeAfterServerApply (prevGraph: Graph) (model: VM) : VM =
    match model.mode with
    | Editing _ ->
        let editingId =
            match model.selectedNodes with
            | Some sel -> focusedNodeId model.graph sel
            | None -> viewRootNodeId model
        let prevText =
            prevGraph.nodes |> Map.tryFind editingId |> Option.map (fun n -> n.text)
        let newText =
            model.graph.nodes |> Map.tryFind editingId |> Option.map (fun n -> n.text)
        if prevText = newText then model
        else { model with mode = Selecting }
    | _ -> model

/// If `newText` differs from the graph, the same `SetText` op `commitTextEdit` would post (no mode change).
let tryTextCommitOps (nodeId: NodeId) (originalTextForHistory: string) (newText: string) (graph: Graph) : Op list =
    if nodeId = graph.root then
        []
    else
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
        | Ok (m, effects) -> { m with mode = Selecting }, effects
        | Error msg -> withMoveError msg { model with mode = Selecting }, []

/// Split the currently-edited node at the cursor position.
///
/// cursor at 0   → blank sibling inserted above; current node keeps its text; focus moves to the new blank node.
/// cursor > 0, expanded with children → text-after becomes first child; focus at start of new child.
/// cursor > 0    → current node gets text-before; new sibling gets text-after; focus at start of new node.
let splitNode (currentText: string) (cursorPos: int) (model: VM) : VM * Effect list =
    match model.mode, model.selectedNodes with
    | Editing (originalText, _), None ->
        // View root is being edited: commit text, no split
        commitTextEdit (viewRootNodeId model) originalText (readEditInputValue ()) model
    | Editing (originalText, _), Some sel ->
        // The node being edited is the focus node.
        let focusedId  = focusedNodeId model.graph sel
        let modelText = model.graph.nodes.[focusedId].text
        let parentId    = sel.range.parent.nodeId
        let indexInParent = sel.focus
        let clampedPos = max 0 (min cursorPos currentText.Length)
        let textBefore = currentText.[..clampedPos - 1]
        let textAfter  = currentText.[clampedPos..]
        let newChild = ChildNode.New()
        let newId = newChild.id

        let focusedHasExpandedChildren =
            SiteMap.nodeIsExpanded model.siteMap (focusedInstanceId sel)

        let (newNodeOwner, insertIndex, newNodeText) =
            if clampedPos = 0 then
                // blank node above; focus moves to the new blank node
                (parentId, indexInParent, textBefore)
            elif focusedHasExpandedChildren then
                // text-after becomes first child of expanded focused node
                (focusedId, 0, textAfter)
            else
                // new node after; focus moves to new node
                (parentId, indexInParent + 1, textAfter)

        let ops =
            [ yield Op.NewNode(newChild.id, newNodeText)
              yield Op.Replace(newNodeOwner, insertIndex, [], [ newChild ])
              // update current node's text only when it actually changes
              let updatedText = if clampedPos = 0 then currentText else textBefore
              if updatedText <> modelText then
                  yield Op.SetText(focusedId, modelText, updatedText) ]

        let change: Change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = ops }
        match applyAndPost change model with
        | Ok (m, effects) ->
            let effRoot = m.zoomRoot
            let siteMap, nextId =
                ViewModel.reconcileSiteMapFrom m.graph effRoot m.siteMap m.nextSiteId
            let m2 = { m with siteMap = siteMap; nextSiteId = nextId }
            let focusInstId =
                if clampedPos = 0 then
                    focusedInstanceId sel
                else
                    let ownerInstId =
                        if newNodeOwner = focusedId then
                            focusedInstanceId sel
                        else
                            Some sel.range.parent.instanceId
                    ownerInstId
                    |> Option.bind (fun id -> Map.tryFind id m2.siteMap.entries)
                    |> Option.bind (fun p ->
                        if insertIndex < p.children.Length then Some p.children.[insertIndex]
                        else None)
            let newSel =
                focusInstId
                |> Option.bind (singleSelectionForInstance m2.siteMap)
                |> Option.orElseWith (fun () -> singleSelection m2.graph m2.siteMap newId)
            { m2 with
                selectedNodes = newSel
                mode = Editing (newNodeText, EditCaret.Utf16Index 0) }, effects
        | Error msg -> withMoveError msg model, []
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
/// focusedInstanceId can resolve against an up-to-date parent.children list.
let withSiteMap (model: VM) : VM =
    let zoomRoot = model.zoomRoot
    let siteMap, nextId =
        ViewModel.reconcileSiteMapFrom model.graph zoomRoot model.siteMap model.nextSiteId
    let model' = { model with siteMap = siteMap; nextSiteId = nextId; zoomRoot = zoomRoot }
    match model'.selectedNodes with
    | None -> model'
    | Some sel ->
        let adapted =
            ViewModel.refreshSelection model'.graph model'.siteMap sel
            |> Option.orElse (ViewModel.firstChildSelection model'.siteMap model'.zoomRoot)
        match adapted with
        | Some refreshed -> { model' with selectedNodes = Some refreshed }
        | None -> model'

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
        let committed, effects =
            commitTextEdit currentId
                (match model.mode with Editing (t, _) -> t | _ -> "")
                (readEditInputValue ()) model
        let rows = getVisibleRowInstanceIds committed.siteMap
        match focusedInstanceId sel with
        | None -> committed, effects
        | Some focusInstId ->
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
                        selectedNodes =
                            singleSelectionForInstance committed.siteMap targetInstId },
                    effects
