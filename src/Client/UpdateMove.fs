module Gambol.Client.UpdateMove

open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.CommandEntry
open Gambol.Shared.ViewModel
open Gambol.Shared.ViewModelMoveOps


let parentSiblingTarget (delta: int) (me: SiteEntry) (model: VM) : (VM * SiteEntry) option =
    ViewModel.parentSiblingTarget
        delta
        me
        model.graph
        model.siteMap
        model.nextSiteId
        model.zoomRoot
    |> Option.map (fun (siteMap, nextSiteId, sibling) ->
        { model with siteMap = siteMap; nextSiteId = nextSiteId }, sibling)

type private InlineEditContext =
    { originalText: string
      rebuildMode: string -> int -> Mode }

let private mapRebuild
    (rewrap: (string -> int -> Mode) -> string -> int -> Mode)
    (ctx: InlineEditContext)
    : InlineEditContext =
    { ctx with rebuildMode = rewrap ctx.rebuildMode }

let private trySaveContext (mode: Mode) : InlineEditContext option =
    let rec unwrapMode m =
        match m with
        | Editing (orig, _) ->
            Some
                { originalText = orig
                  rebuildMode = fun t c -> Editing (t, EditCaret.utf16ClampedToLength c t.Length) }
        | Mode.CommandPalette (q, sc, ret) ->
            unwrapMode ret
            |> Option.map (mapRebuild (fun rebuild t c ->
                Mode.CommandPalette (q, sc, rebuild t c)))
        | SearchDialog s ->
            unwrapMode s.returnTo
            |> Option.map (mapRebuild (fun rebuild t c ->
                SearchDialog { s with returnTo = rebuild t c }))
        | FileSearchDialog s ->
            unwrapMode s.returnTo
            |> Option.map (mapRebuild (fun rebuild t c ->
                FileSearchDialog { s with returnTo = rebuild t c }))
        | CssClassPrompt (ret, iv) ->
            unwrapMode ret
            |> Option.map (mapRebuild (fun rebuild t c -> CssClassPrompt (rebuild t c, iv)))
        | RenamePrompt (ret, iv) ->
            unwrapMode ret
            |> Option.map (mapRebuild (fun rebuild t c -> RenamePrompt (rebuild t c, iv)))
        | Selecting ->
            None
    unwrapMode mode


let private tryApplyOps (commandName: string) (ops: Op list) (model: VM) =
    let change =
        { id = model.revision.Value
          changeId = System.Guid.NewGuid()
          ops = ops }
    match applyAndPost commandName change model with
    | Ok (m, effects) -> Ok (m, effects)
    | Error err -> Error err

let private tryBuildMoveInputs
    (too: NodeRange)
    (model: VM)
    =
    match model.selectedNodes with
    | None -> None
    | Some staleSel ->
        match ViewModel.refreshSelection model.graph model.siteMap staleSel with
        | None -> None
        | Some sel ->
            let from = sel.range
            let selectedChildren = rangeChildren model.graph from
            if selectedChildren.IsEmpty then
                None
            else
                let count = selectedChildren.Length
                let sameParent = from.parent.nodeId = too.pnode
                let insertIdx =
                    if sameParent && too.endd > from.start then too.endd - count else too.endd
                Some (sel, from, selectedChildren, count, sameParent, insertIdx)

let private replaceOpsForMove
    (too: NodeRange)
    (from: SiteNodeRange)
    (selectedChildren: ChildNode list)
    (sameParent: bool)
    (insertIdx: int)
    (graph: Graph)
    : Op list =
    if sameParent then
        // One Replace so ROOT can reorder Workspaces/TRASH without a
        // temporary "removed from root" intermediate state.
        let parentId = from.parent.nodeId
        let oldChildren = graph.nodes.[parentId].children
        let count = selectedChildren.Length
        [ ChildListWire.edit parentId oldChildren from.start count insertIdx selectedChildren ]
    else
        let fromParentId = from.parent.nodeId
        let toParentId = too.pnode
        let fromOld = graph.nodes.[fromParentId].children
        let toOld = graph.nodes.[toParentId].children
        let count = selectedChildren.Length
        [ ChildListWire.removeRange fromParentId fromOld from.start count
          ChildListWire.insertAt toParentId toOld insertIdx selectedChildren ]

let private restoreInlineMode
    (oldMode: InlineEditContext option)
    (caret: int)
    (newSel: Selection)
    (model: VM)
    : VM =
    match oldMode with
    | None -> model
    | Some ctx ->
        let text = model.graph.nodes.[focusedNodeId model.graph newSel].text
        let clampedCaret = min (max 0 caret) text.Length
        { model with mode = ctx.rebuildMode text clampedCaret }

/// Move the selected nodes to after `too`. May remove from old parent and add to new
/// (two Op.Replace ops), or reorder within the same parent.
/// Inline edit: `tryTextCommitOps` + move in one change; stay in edit mode with clamped caret.
/// Error when inputs are missing or History rejects the change (message for `#cmd-last-result`).
let tryMoveNodeFromTo
    (commandName: string)
    (stayAtSource: bool)
    (too: NodeRange)
    (model: VM)
    : Result<VM * Effect list, string> =
    let oldMode = trySaveContext model.mode
    let live = readEditInputValue ()
    let caret = readEditInputCursor ()
    let textOps =
        match model.selectedNodes, oldMode with
        | Some sel, Some ctx ->
            let editingId = focusedNodeId model.graph sel
            tryTextCommitOps editingId ctx.originalText live model.graph
        | _ -> []
    match tryBuildMoveInputs too model with
    | None -> Error invalidMoveTargetMessage
    | Some (sel, from, selectedChildren, count, sameParent, insertIdx) ->
        let replaceOps =
            replaceOpsForMove too from selectedChildren sameParent insertIdx model.graph
        let ops = textOps @ replaceOps
        match tryApplyOps commandName ops model with
        | Error e -> Error e
        | Ok (movedModel, effects) ->
            let destParentId = if sameParent then from.parent.nodeId else too.pnode
            let movedModel =
                selectionModelAfterStructuralMove
                    model.graph
                    from
                    stayAtSource
                    destParentId
                    insertIdx
                    count
                    (sel.focus - from.start)
                    from.parent
                    movedModel
            let finalModel =
                match movedModel.selectedNodes, oldMode with
                | Some newSel, _ -> restoreInlineMode oldMode caret newSel movedModel
                | None, Some _ -> { movedModel with mode = Selecting }
                | None, None -> movedModel
            Ok (finalModel, effects)

let moveNodeFromTo
    (commandName: string)
    (stayAtSource: bool)
    (too: NodeRange)
    (model: VM)
    : VM * Effect list =
    match tryMoveNodeFromTo commandName stayAtSource too model with
    | Ok result -> result
    | Error msg -> withMoveError msg model, []

/// Alt+Up/Down: swap the selected range with the adjacent sibling. Delegates to moveNodeFromTo.
let private moveIntoOpenParentSibling (delta: int) (range: SiteNodeRange) (model: VM) =
    let edgeChild =
        if delta = -1 then SiteNodeRange.firstChild else SiteNodeRange.lastChild

    edgeChild range model.siteMap
    |> Option.bind (fun child -> parentSiblingTarget delta child model)
    |> Option.map (fun (model, sibling) ->
        let siblingId = sibling.nodeId
        let target =
            if delta = -1 then
                let insertIdx = model.graph.nodes.[siblingId].children.Length
                { pnode = siblingId; start = insertIdx; endd = insertIdx }
            else
                { pnode = siblingId; start = 0; endd = 0 }
        model, target)

let private moveBesideParent (delta: int) (range: SiteNodeRange) (model: VM) =
    if range.parent.nodeId = model.zoomRoot then
        None
    else
        range.parent.parentInstanceId
        |> Option.bind (fun parentInstId ->
            SiteMap.siteChildIndex
                model.siteMap (Some parentInstId) (Some range.parent.instanceId)
            |> Option.map (fun parentIdx -> parentInstId, parentIdx))
        |> Option.bind (fun (parentInstId, parentIdx) ->
            Map.tryFind parentInstId model.siteMap.entries
            |> Option.map (fun parent ->
                let insertIdx = parentIdx + (if delta = -1 then 0 else 1)
                { pnode = parent.nodeId; start = insertIdx; endd = insertIdx }))

let moveNodeDelta (commandName: string) (delta: int) (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let range = sel.range
        let parentId = range.parent.nodeId
        let parentLen = model.graph.nodes.[parentId].children.Length
        let moveTarget: (VM * NodeRange) option =
            if delta < 0 && range.start > 0 then
                // Move to before sibling above: insert at range.start - 1 (after the range ending there)
                let s = if range.start = 1 then 0 else range.start - 2
                Some
                    (model, { pnode = parentId; start = s; endd = range.start - 1 })
            elif delta > 0 && range.endd < parentLen then
                // Move to after sibling below
                Some
                    (model, { pnode = parentId; start = range.endd; endd = range.endd + 1 })
            elif delta = -1 || delta = 1 then
                moveIntoOpenParentSibling delta range model
                |> Option.orElseWith (fun () ->
                    moveBesideParent delta range model
                    |> Option.map (fun target -> model, target))
            else
                None
        match moveTarget with
        | None -> model, []
        | Some (targetModel, target) ->
            moveNodeFromTo commandName false target targetModel


// ---------------------------------------------------------------------------
// Indent / Outdent (use moveNodeFromTo for edit-mode semantics)
// ---------------------------------------------------------------------------

/// Tab: make selected nodes children of the sibling immediately before them.
/// No-op if the selection starts at index 0 (no previous sibling).
/// Rejected apply: original selection/focus unchanged + error in lastCmdResult.
let indentSelection (model: VM) : VM * Effect list =
    match planIndentSelection model with
    | None -> model, []
    | Some plan ->
        match tryMoveNodeFromTo (displayName Indent) false plan.target plan.model with
        | Error msg -> completeIndent model plan (Error msg), []
        | Ok (result, effects) ->
            completeIndent model plan (Ok (withSiteMap result)), effects

/// Shift+Tab: make selected nodes siblings of their current parent (under grandparent).
/// When the parent is the siteMap root, the move still succeeds and the siteMap root is
/// shifted up to the grandparent so the moved nodes remain visible.
let outdentSelection (model: VM) : VM * Effect list =
    match planOutdentSelection model with
    | None -> model, []
    | Some plan ->
        match tryMoveNodeFromTo (displayName Outdent) false plan.target plan.model with
        | Error msg -> withMoveError msg model, []
        | Ok (result, effects) ->
            match plan.afterMove with
            | ReconcileCurrentZoom -> withSiteMap result, effects
            | ZoomOutToGrandparent (grandparentId, parentIdx, count, focusOffset) ->
                let siteMap, nextId =
                    ViewModel.buildSiteMapFrom result.graph grandparentId result.nextSiteId

                let grandparentEntry = siteMap.entries.[siteMap.rootId]
                let insertIdx = parentIdx + 1
                let newSel =
                    { range =
                        { parent = grandparentEntry
                          start = insertIdx
                          endd = insertIdx + count }
                      focus = insertIdx + focusOffset }

                { result with
                    zoomRoot = grandparentId
                    siteMap = siteMap
                    nextSiteId = nextId
                    selectedNodes = Some newSel }, effects

