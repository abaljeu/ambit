module Gambol.Client.UpdatePaste

open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.Paste
open Gambol.Shared.ViewModel

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

/// Prefer node IDs from clipboard format, else GUIDs parsed from paste entry lines.
let private tryPasteLinkIds
    (graph: Graph) (preferredNodeIds: string option) (entries: (string * int) list)
    : NodeId list option =
    match preferredNodeIds |> Option.bind (fun s -> tryResolveNodeIdsFormat s graph) with
    | Some ids -> Some ids
    | None ->
        let ids =
            entries
            |> List.choose (fun (text, _) ->
                match System.Guid.TryParse(text.Trim()) with
                | true, guid ->
                    let id = NodeId guid
                    if Map.containsKey id graph.nodes then Some id else None
                | _ -> None)
        if ids.IsEmpty then None else Some ids

let private spliceTextAtCaret (line: string) (caret: int) (insert: string) : string =
    line.[..caret - 1] + insert + line.[caret..]

/// Keep `Editing` after paste: baseline text from graph; caret clamped to that text.
let private editingModeAfterPaste (m: VM) (focusId: NodeId) (caretUtf16: int) : VM =
    let t = m.graph.nodes.[focusId].text
    { m with mode = Editing (t, EditCaret.utf16ClampedToLength caretUtf16 t.Length) }

let private editingUnchangedAtCaret (model: VM) (line: string) (caret: int) : VM =
    { model with mode = Editing (line, EditCaret.utf16ClampedToLength caret line.Length) }

let private newChange (model: VM) (ops: Op list) : Change =
    { id = model.revision.Value; changeId = System.Guid.NewGuid(); ops = ops }

let private normalizePasteLines (pastedText: string) : string list =
    pastedText.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n')
    |> Array.toList

let private planColdPaste (text: string) : (NodeId list * Op list) option =
    match DocumentColdParse.planPasteOps text with
    | Ok result -> Some result
    | Error _ -> None

let private pasteNodesSelecting
    (model: VM) (sel: Selection) (entries: (string * int) list)
    (preferredNodeIds: string option) (pastedText: string)
    : VM * Effect list =
    let topLevelIds, pasteOps =
        match tryPasteLinkIds model.graph preferredNodeIds entries with
        | Some existingIds -> existingIds, []
        | None ->
            match planColdPaste pastedText with
            | Some result -> result
            | None -> [], []

    if topLevelIds.IsEmpty then
        model, []
    else
        let range = sel.range
        let selectedChildren = rangeChildren model.graph range
        let replaceOp =
            Op.Replace(
                range.parent.nodeId,
                range.start,
                selectedChildren,
                childrenForPaste model.graph topLevelIds
            )
        let change = newChange model (pasteOps @ [replaceOp])
        match applyAndPost change model with
        | Ok (m, effects) ->
            let newEnd = range.start + topLevelIds.Length
            let newSel =
                { range = { parent = range.parent; start = range.start; endd = newEnd }
                  focus = range.start }
            { m with selectedNodes = Some newSel }, effects
        | Error _ -> model, []

let private pasteEditingLink
    (model: VM) (originalText: string) (currentText: string) (cursorPos: int) (focusId: NodeId)
    (parentId: NodeId) (focusIdx: int) (refIds: NodeId list)
    : VM * Effect list =
    let setTextOps =
        if currentText <> originalText then [ Op.SetText(focusId, originalText, currentText) ]
        else []
    let insertOp =
        Op.Replace(parentId, focusIdx + 1, [], childrenForPaste model.graph refIds)
    let change = newChange model (setTextOps @ [insertOp])
    match applyAndPost change model with
    | Ok (m, effects) -> editingModeAfterPaste m focusId cursorPos, effects
    | Error _ -> model, []

let private pasteEditingSingleLine
    (model: VM) (originalText: string) (currentText: string) (cursorPos: int) (focusId: NodeId)
    (firstText: string)
    : VM * Effect list =
    let newText = spliceTextAtCaret currentText cursorPos firstText
    if newText = originalText then
        editingUnchangedAtCaret model originalText cursorPos, []
    else
        let ops = [ Op.SetText(focusId, originalText, newText) ]
        let afterCaret = cursorPos + firstText.Length
        match applyAndPost (newChange model ops) model with
        | Ok (m, effects) -> editingModeAfterPaste m focusId afterCaret, effects
        | Error _ -> model, []

let private pasteEditingMultiline
    (model: VM) (originalText: string) (currentText: string) (cursorPos: int) (focusId: NodeId)
    (parentId: NodeId) (focusIdx: int) (firstText: string) (restText: string)
    : VM * Effect list =
    let newText = spliceTextAtCaret currentText cursorPos firstText
    let setTextOps =
        if newText <> originalText then [ Op.SetText(focusId, originalText, newText) ]
        else []
    let remainingTopIds, remainingOps =
        match planColdPaste restText with
        | Some result -> result
        | None -> [], []
    let insertOps =
        if remainingTopIds.IsEmpty then []
        else [ Op.Replace(parentId, focusIdx + 1, [], childrenForPaste model.graph remainingTopIds) ]
    let allOps = setTextOps @ remainingOps @ insertOps
    let afterCaret = cursorPos + firstText.Length
    if allOps.IsEmpty then
        editingUnchangedAtCaret model originalText cursorPos, []
    else
        match applyAndPost (newChange model allOps) model with
        | Ok (m, effects) -> editingModeAfterPaste m focusId afterCaret, effects
        | Error _ -> model, []

let private pasteEditingPlainText
    (model: VM) (originalText: string) (currentText: string) (cursorPos: int) (focusId: NodeId)
    (parentId: NodeId) (focusIdx: int) (pastedText: string)
    : VM * Effect list =
    match normalizePasteLines pastedText with
    | [] -> model, []
    | [ firstLine ] ->
        let depth = firstLine |> Seq.takeWhile ((=) '\t') |> Seq.length
        let firstText = firstLine.Substring(depth)
        pasteEditingSingleLine model originalText currentText cursorPos focusId firstText
    | firstLine :: rest ->
        let depth = firstLine |> Seq.takeWhile ((=) '\t') |> Seq.length
        let firstText = firstLine.Substring(depth)
        let restText = String.concat "\n" rest
        pasteEditingMultiline
            model originalText currentText cursorPos focusId parentId focusIdx firstText restText

let private pasteNodesEditing
    (model: VM) (sel: Selection) (entries: (string * int) list) (preferredNodeIds: string option)
    (originalText: string) (pastedText: string)
    : VM * Effect list =
    let currentText = readEditInputValue ()
    let cursorPos = readEditInputCursor ()
    let focusId = focusedNodeId model.graph sel
    let parentId = sel.range.parent.nodeId
    let focusIdx = sel.focus
    match tryPasteLinkIds model.graph preferredNodeIds entries with
    | Some refIds ->
        pasteEditingLink
            model originalText currentText cursorPos focusId parentId focusIdx refIds
    | None ->
        pasteEditingPlainText
            model originalText currentText cursorPos focusId parentId focusIdx pastedText

/// When preferredNodeIds is Some (from cut/copy-as-links clipboard format), resolve
/// to existing nodes and insert as links (Op.Replace only, no NewNode).
/// Select mode: replaces selection with resolved nodes.
/// Edit mode: commits current text then inserts resolved nodes as siblings below; stays Editing.
///
/// Otherwise (external plain text): DocumentColdParse.planPasteOps (stub Plain).
/// Select mode: replaces selection with pasted subtree.
/// Edit mode: splices first line into node at cursor; remaining lines become siblings; stays Editing.
let pasteNodes (pastedText: string) (preferredNodeIds: string option) (model: VM)
    : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let entries = parsePasteText pastedText
        if entries.IsEmpty then model, []
        else
            match model.mode with
            | CommandPalette _ | SearchDialog _ | FileSearchDialog _ | CssClassPrompt _ | RenamePrompt _ -> model, []
            | Selecting ->
                pasteNodesSelecting model sel entries preferredNodeIds pastedText
            | Editing (originalText, _) ->
                pasteNodesEditing model sel entries preferredNodeIds originalText pastedText

/// CutSelection: store clipboard content, remove selected nodes, update selection.
/// Post-cut priority: sibling after > sibling before > parent.
let cutSelection (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let selectedChildren = rangeChildren model.graph sel.range
        let cb = collectSubtree model.graph model.siteMap selectedChildren
        let removeOp = Op.Replace(sel.range.parent.nodeId, sel.range.start, selectedChildren, [])
        let change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = [removeOp] }
        match applyAndPost change model with
        | Ok (m, effects) ->
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
            { m with clipboard = Some cb; selectedNodes = newSel }, effects
        | Error _ -> model, []
