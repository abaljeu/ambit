module Gambol.Client.UpdateMove

open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.ViewModel


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
        | CommandPalette (q, sc, ret) ->
            unwrapMode ret
            |> Option.map (mapRebuild (fun rebuild t c -> CommandPalette (q, sc, rebuild t c)))
        | SearchDialog (q, sr, ret, onPick) ->
            unwrapMode ret
            |> Option.map (mapRebuild (fun rebuild t c -> SearchDialog (q, sr, rebuild t c, onPick)))
        | CssClassPrompt (ret, iv) ->
            unwrapMode ret
            |> Option.map (mapRebuild (fun rebuild t c -> CssClassPrompt (rebuild t c, iv)))
        | Selecting ->
            None
    unwrapMode mode


let private tryApplyOps (ops: Op list) (model: VM) : (VM * Effect list) option =
    let change =
        { id = model.revision.Value
          changeId = System.Guid.NewGuid()
          ops = ops }
    match applyAndPost change model with
    | Some m, effects -> Some (m, effects)
    | None, _ -> None

let private tryBuildMoveInputs
    (too: NodeRange)
    (model: VM)
    =
    match model.selectedNodes with
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
    : Op list =
    if sameParent then
        [ Op.Replace(from.parent.nodeId, from.start, selectedChildren, [])
          Op.Replace(from.parent.nodeId, insertIdx, [], selectedChildren) ]
    else
        [ Op.Replace(from.parent.nodeId, from.start, selectedChildren, [])
          Op.Replace(too.pnode, too.endd, [], selectedChildren) ]

let private resolveNewParent
    (too: NodeRange)
    (from: SiteNodeRange)
    (sameParent: bool)
    (movedModel: VM)
    : SiteEntry =
    if sameParent then
        from.parent
    else
        movedModel.siteMap.entries
        |> Map.tryPick (fun _ e -> if e.nodeId = too.pnode then Some e else None)
        |> Option.defaultValue from.parent

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
let moveNodeFromTo (too: NodeRange) (model: VM) : VM * Effect list =
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
    | None -> model, []
    | Some (sel, from, selectedChildren, count, sameParent, insertIdx) ->
        let replaceOps = replaceOpsForMove too from selectedChildren sameParent insertIdx
        let ops = textOps @ replaceOps
        match tryApplyOps ops model with
        | None -> model, []
        | Some (movedModel, effects) ->
            let newParent = resolveNewParent too from sameParent movedModel
            let focusOffset = sel.focus - from.start
            let newSel =
                ViewModel.selectionAfterStructuralMove
                    model.graph
                    movedModel.graph
                    movedModel.siteMap
                    from
                    newParent
                    insertIdx
                    count
                    focusOffset
            let movedModel = { movedModel with selectedNodes = Some newSel }
            restoreInlineMode oldMode caret newSel movedModel, effects

/// Alt+Up/Down: swap the selected range with the adjacent sibling. Delegates to moveNodeFromTo.
let moveNodeDelta (delta: int) (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let range = sel.range
        let parentId = range.parent.nodeId
        let parentLen = model.graph.nodes.[parentId].children.Length
        let too: NodeRange option =
            if delta < 0 && range.start > 0 then
                // Move to before sibling above: insert at range.start - 1 (after the range ending there)
                let s = if range.start = 1 then 0 else range.start - 2
                Some { pnode = parentId; start = s; endd = range.start - 1 }
            elif delta > 0 && range.endd < parentLen then
                // Move to after sibling below
                Some { pnode = parentId; start = range.endd; endd = range.endd + 1 }
            elif delta = -1 || delta = 1 then
                let effectiveRoot = model.zoomRoot |> Option.defaultValue model.graph.root
                let moveToSib =
                    (if delta = -1 then SiteNodeRange.firstChild else SiteNodeRange.lastChild) range model.siteMap
                    |> Option.bind (fun child -> parentSiblingOpen delta child model)
                    |> Option.map (fun sibEntry ->
                        let sibId = sibEntry.nodeId
                        if delta = -1 then
                            let insertIdx = model.graph.nodes.[sibId].children.Length
                            { pnode = sibId; start = insertIdx; endd = insertIdx }
                        else
                            { pnode = sibId; start = 0; endd = 0 })
                let moveToGrandparent =
                    if range.parent.nodeId = effectiveRoot then
                        None
                    else
                        range.parent.parentInstanceId
                        |> Option.bind (fun gpid -> Map.tryFind gpid model.siteMap.entries)
                        |> Option.bind (fun gp ->
                            model.graph.nodes.[gp.nodeId].children
                            |> List.tryFindIndex (fun child -> child.id = range.parent.nodeId)
                            |> Option.map (fun parentIdx ->
                                let insertIdx = parentIdx + (if delta = -1 then 0 else 1)
                                { pnode = gp.nodeId; start = insertIdx; endd = insertIdx }))
                moveToSib |> Option.orElseWith (fun () -> moveToGrandparent)
            else
                None
        match too with
        | None -> model, []
        | Some t -> moveNodeFromTo t model


// ---------------------------------------------------------------------------
// Indent / Outdent (use moveNodeFromTo for edit-mode semantics)
// ---------------------------------------------------------------------------

/// Tab: make selected nodes children of the sibling immediately before them.
/// No-op if the selection starts at index 0 (no previous sibling).
let indentSelection (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel when sel.range.start = 0 -> model, []  // no previous sibling — no-op
    | Some sel ->
        let prevSibId = model.graph.nodes.[sel.range.parent.nodeId].children.[sel.range.start - 1].id
        let insertIdx = model.graph.nodes.[prevSibId].children.Length
        match model.siteMap.entries
            |> Map.tryPick (fun _ e -> if e.nodeId = prevSibId then Some e else None) with
        | None -> model, []
        | Some _ ->
            let too: NodeRange =
                { pnode = prevSibId; start = max 0 (insertIdx - 1); endd = insertIdx }
            let result, effects = moveNodeFromTo too model
            let result = withSiteMap result
            // Ensure the new parent is expanded so the indented items are visible after reconcile
            match result.siteMap.entries
                |> Map.tryPick (fun _ e -> if e.nodeId = prevSibId then Some e else None) with
            | Some entry when not entry.expanded ->
                let siteMap, nextId =
                    ViewModel.expandEntry entry.instanceId result.graph result.siteMap result.nextSiteId
                { result with siteMap = siteMap; nextSiteId = nextId }, effects
            | _ -> result, effects

/// Shift+Tab: make selected nodes siblings of their current parent (under grandparent).
let outdentSelection (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        match Graph.tryFindParentAndIndex sel.range.parent.nodeId model.graph with
        | None -> model, []  // parent is root — no-op
        | Some (grandparentId, parentIdx) ->
            match model.siteMap.entries
                |> Map.tryPick (fun _ e -> if e.nodeId = grandparentId then Some e else None) with
            | None -> model, []
            | Some _ ->
                let too: NodeRange =
                    { pnode = grandparentId; start = parentIdx; endd = parentIdx + 1 }
                let result, effects = moveNodeFromTo too model
                withSiteMap result, effects

