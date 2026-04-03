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

let private tryInlineEditWrap (mode: Mode) : (string * (string -> int -> Mode)) option =
    let rec unwrapMode m =
        match m with
        | Editing (orig, _) ->
            Some (orig, fun t c -> Editing (t, EditCaret.utf16ClampedToLength c t.Length))
        | CommandPalette (q, sc, ret) ->
            unwrapMode ret |> Option.map (fun (o, w) -> (o, fun t c -> CommandPalette (q, sc, w t c)))
        | CssClassPrompt (ret, iv) ->
            unwrapMode ret |> Option.map (fun (o, w) -> (o, fun t c -> CssClassPrompt (w t c, iv)))
        | _ -> None
    unwrapMode mode

/// Move the selected nodes to after `too`. May remove from old parent and add to new
/// (two Op.Replace ops), or reorder within the same parent.
/// Inline edit: `tryTextCommitOps` + move in one change; stay in edit mode with clamped caret.
let moveNodeFromTo (too: SiteNodeRange) (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let from = sel.range
        let selectedChildren = rangeChildren model.graph from
        if selectedChildren.IsEmpty then model, []
        else
            let wrap = tryInlineEditWrap model.mode
            let live = readEditInputValue ()
            let caret = readEditInputCursor ()
            let editingId = focusedNodeId model.graph sel
            let textOps =
                match wrap with
                | Some (orig, _) -> tryTextCommitOps editingId orig live model.graph
                | None -> []
            let count = selectedChildren.Length
            let sameParent = from.parent.nodeId = too.parent.nodeId
            let replaceOps =
                if sameParent then
                    // Reordering within same parent: remove then insert.
                    // Insert index after removal: if too.endd > from.endd, shift left by count.
                    let insertIdx =
                        if too.endd <= from.start then too.endd
                        else too.endd - count
                    [ Op.Replace(from.parent.nodeId, from.start, selectedChildren, [])
                      Op.Replace(from.parent.nodeId, insertIdx, [], selectedChildren) ]
                else
                    [ Op.Replace(from.parent.nodeId, from.start, selectedChildren, [])
                      Op.Replace(too.parent.nodeId, too.endd, [], selectedChildren) ]
            let ops = textOps @ replaceOps
            let change =
                { id = model.revision.Value
                  changeId = System.Guid.NewGuid()
                  ops = ops }
            match applyAndPost change model with
            | Some m, effects ->
                let insertIdx =
                    if sameParent then (if too.endd <= from.start then too.endd else too.endd - count)
                    else too.endd
                let newParent = if sameParent then from.parent else too.parent
                let newRange: SiteNodeRange = { parent = newParent; start = insertIdx; endd = insertIdx + count }
                let focusOffset = sel.focus - from.start
                let newFocus = insertIdx + (min (max 0 focusOffset) (count - 1))
                let newSel = { range = newRange; focus = newFocus }
                let m' = { m with selectedNodes = Some newSel }
                match wrap with
                | Some (_, rebuild) ->
                    let t = m.graph.nodes.[focusedNodeId m.graph newSel].text
                    { m' with mode = rebuild t (min (max 0 caret) t.Length) }, effects
                | None -> m', effects
            | None, _ -> model, []

/// Alt+Up/Down: swap the selected range with the adjacent sibling. Delegates to moveNodeFromTo.
let moveNodeDelta (delta: int) (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
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
                            |> List.tryFindIndex (fun child -> child.id = range.parent.nodeId)
                            |> Option.map (fun parentIdx ->
                                let insertIdx = parentIdx + (if delta = -1 then 0 else 1)
                                { parent = gp; start = insertIdx; endd = insertIdx } : SiteNodeRange))
                moveToSib |> Option.orElseWith (fun () -> moveToGrandparent)
            else None
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
        | Some prevSibEntry ->
            let too: SiteNodeRange = { parent = prevSibEntry; start = max 0 (insertIdx - 1); endd = insertIdx }
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
            | Some grandparentEntry ->
                let too: SiteNodeRange = { parent = grandparentEntry; start = parentIdx; endd = parentIdx + 1 }
                let result, effects = moveNodeFromTo too model
                withSiteMap result, effects

