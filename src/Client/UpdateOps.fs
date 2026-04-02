module Gambol.Client.UpdateOps

open Browser.Dom
open Browser.Types
open Fable.Core
open Gambol.Client.JsInterop
open Gambol.Client.UpdateEdit
open Gambol.Client.UpdateHelpers
open Gambol.Client.UpdateMove
open Gambol.Client.UpdatePaste
open Gambol.Shared
open Gambol.Shared.Paste
open Gambol.Shared.ViewModel


// ---------------------------------------------------------------------------
// Op type and named operations
// ---------------------------------------------------------------------------

/// A self-contained pure model transformation. Returns the new VM and any effects to run.
type Op = VM -> VM * Effect list

/// Op: Move to selection mode (or deselect if already selecting), reverting any edit.
let handleEsc (model: VM) : VM * Effect list =
    match model.mode with
    | Editing _ -> commitIfEditing model
    | Selecting -> collapseToFocus model, []
    | CommandPalette _ | CssClassPrompt _ -> model, []  // handled by closeCommandPaletteOp / closeCssClassPromptOp

/// Op: Copy the focused subtree to the internal clipboard.
let copySelectionOp (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let selectedChildren =
            model.graph.nodes.[sel.range.parent.nodeId].children
            |> List.skip sel.range.start
            |> List.take (sel.range.endd - sel.range.start)
        { model with clipboard = Some (collectSubtree model.graph model.siteMap selectedChildren) }, []

/// Op: Cut the focused subtree.
let cutSelectionOp (model: VM) : VM * Effect list =
    let m, effs = cutSelection model
    withSiteMap m, effs

/// Op: Enter edit mode for the focused node, prefilled with its current text.
let startEditOp (model: VM) : VM * Effect list =
    let text =
        match model.selectedNodes with
        | None -> model.graph.nodes.[viewRootNodeId model].text
        | Some sel -> model.graph.nodes.[focusedNodeId model.graph sel].text
    { model with mode = Editing (text, EditCaret.EndOfText) }, []

/// Op: Enter edit mode for the focused node, with cursor placed at a specific position.
let startEditAtPos (cursorPos: int) (model: VM) : VM * Effect list =
    let text =
        match model.selectedNodes with
        | None -> model.graph.nodes.[viewRootNodeId model].text
        | Some sel -> model.graph.nodes.[focusedNodeId model.graph sel].text
    { model with mode = Editing (text, EditCaret.Utf16Index cursorPos) }, []

/// Re-export palette ops for use by Controller and View.
let openCommandPaletteOp = Gambol.Client.CommandPalette.openCommandPaletteOp
let closeCommandPaletteOp = Gambol.Client.CommandPalette.closeCommandPaletteOp

/// Op: Select a specific node, committing any in-progress edit first.
let selectRow (nodeId: NodeId) (model: VM) : VM * Effect list =
    let result, effects =
        match model.mode, model.selectedNodes with
        | Editing (originalText, _), Some sel ->
            let editingId = focusedNodeId model.graph sel
            let newText = readEditInputValue ()
            let model', effs = commitTextEdit editingId originalText newText model
            { model' with selectedNodes = singleSelection model'.graph model'.siteMap nodeId }, effs
        | _ ->
            { model with
                selectedNodes = singleSelection model.graph model.siteMap nodeId
                mode = Selecting }, []
    if not (System.Object.ReferenceEquals(result.graph, model.graph)) then
        withSiteMap result, effects else result, effects

/// Op: Select a specific view-line by instanceId, committing any in-progress edit first.
/// Prefer this over selectRow when a nodeId may appear multiple times in the view.
let selectInstance (instanceId: SiteId) (model: VM) : VM * Effect list =
    let result, effects =
        match model.mode, model.selectedNodes with
        | Editing (originalText, _), Some sel ->
            let editingId = focusedNodeId model.graph sel
            let newText = readEditInputValue ()
            let model', effs = commitTextEdit editingId originalText newText model
            { model' with selectedNodes = singleSelectionForInstance model'.siteMap instanceId }, effs
        | _ ->
            { model with
                selectedNodes = singleSelectionForInstance model.siteMap instanceId
                mode = Selecting }, []
    if not (System.Object.ReferenceEquals(result.graph, model.graph)) then
        withSiteMap result, effects else result, effects

/// Op: Move selection up, committing any in-progress edit first.
let moveSelectionUp (model: VM) : VM * Effect list =
    match model.mode with
    | CommandPalette _ -> Gambol.Client.CommandPalette.paletteSelectUpOp model
    | CssClassPrompt _ -> model, []
    | _ ->
        let result, effects =
            match model.mode with
            | Editing _ ->
                let committed, effs = commitIfEditing model
                moveSelectionBy -1 committed, effs
            | _ -> applyMoveSelectionUp model, []
        if not (System.Object.ReferenceEquals(result.graph, model.graph)) then
            withSiteMap result, effects else result, effects

/// Op: Move selection down, committing any in-progress edit first.
let moveSelectionDown (model: VM) : VM * Effect list =
    match model.mode with
    | CommandPalette _ -> Gambol.Client.CommandPalette.paletteSelectDownOp model
    | CssClassPrompt _ -> model, []
    | _ ->
        let result, effects =
            match model.mode with
            | Editing _ ->
                let committed, effs = commitIfEditing model
                moveSelectionBy 1 committed, effs
            | _ -> applyMoveSelectionDown model, []
        if not (System.Object.ReferenceEquals(result.graph, model.graph)) then
            withSiteMap result, effects else result, effects

/// Op: Extend or shrink the selection by one row (Shift+Arrow).
let shiftArrowOp (delta: int) (model: VM) : VM * Effect list = shiftArrow delta model, []

/// Op: Split the node at the given cursor position.
let splitNodeOp (currentText: string) (cursorPos: int) (model: VM) : VM * Effect list =
    let m, effs = splitNode currentText cursorPos model
    withSiteMap m, effs

/// Op: Commit current edit and move into edit mode on the previous visible row.
let moveEditUp (cursorPos: int) (model: VM) : VM * Effect list =
    moveEdit -1 cursorPos model

/// Op: Commit current edit and move into edit mode on the next visible row.
let moveEditDown (cursorPos: int) (model: VM) : VM * Effect list =
    moveEdit 1 cursorPos model

/// Op: Indent selection (Tab). moveNodeFromTo commits in-progress edits and retains edit mode.
let indentOp (model: VM) : VM * Effect list =
    let m, effs = indentSelection model
    withSiteMap m, effs

/// Op: Outdent selection (Shift+Tab). moveNodeFromTo commits in-progress edits and retains edit mode.
let outdentOp (model: VM) : VM * Effect list =
    let m, effs = outdentSelection model
    withSiteMap m, effs

/// Op: Move selected nodes up.
let moveNodeUpOp (model: VM) : VM * Effect list =
    let m, effs = moveNodeDelta -1 model
    withSiteMap m, effs

/// Op: Move selected nodes down.
let moveNodeDownOp (model: VM) : VM * Effect list =
    let m, effs = moveNodeDelta 1 model
    withSiteMap m, effs

/// Op: PageUp — cursor to start of current level (no graph move).
let pageCursorLevelStartOp (model: VM) : VM * Effect list =
    ViewModel.cursorLevelStart model, []

/// Op: PageDown — cursor to end of current level (no graph move).
let pageCursorLevelEndOp (model: VM) : VM * Effect list =
    ViewModel.cursorLevelEnd model, []

/// Op: Shift+PageDown — shift-style focus motion to end of current level.
let shiftPgDownOp (model: VM) : VM * Effect list =
    ViewModel.shiftPgDown model, []

/// Op: Shift+PageUp — shift-style focus motion to start of current level.
let shiftPgUpOp (model: VM) : VM * Effect list =
    ViewModel.shiftPgUp model, []

/// Op: Home — cursor to first direct child of view root.
let homeSelectionOp (model: VM) : VM * Effect list =
    ViewModel.cursorViewRootFirstChild model, []

/// Op: End — cursor to last direct child of view root.
let endSelectionOp (model: VM) : VM * Effect list =
    ViewModel.cursorViewRootLastChild model, []

/// Move selection with `moveNodeFromTo` when `resolveToo` returns Some.
let private tryStructuralMove
    (model: VM)
    (resolveToo: VM -> Selection -> SiteNodeRange option)
    : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        match resolveToo model sel with
        | None -> model, []
        | Some too ->
            let m, effs = moveNodeFromTo too model
            withSiteMap m, effs

/// Op: Ctrl+PageUp — move selected objects to start of current level; selection follows.
let moveSelectionToLevelStartOp (model: VM) : VM * Effect list =
    tryStructuralMove model (fun _ sel ->
        Some { parent = sel.range.parent; start = 0; endd = 0 })

/// Op: Ctrl+PageDown — move selected objects to end of current level; selection follows.
let moveSelectionToLevelEndOp (model: VM) : VM * Effect list =
    tryStructuralMove model (fun m sel ->
        let range = sel.range
        let parentLen = m.graph.nodes.[range.parent.nodeId].children.Length
        if parentLen = 0 || range.endd >= parentLen then None
        else
            Some
                { parent = range.parent
                  start = parentLen - 1
                  endd = parentLen })

/// Op: Ctrl+Home — move selected objects to first slot under view root; selection follows.
let moveSelectionToViewRootStartOp (model: VM) : VM * Effect list =
    tryStructuralMove model (fun m sel ->
        match Map.tryFind m.siteMap.rootId m.siteMap.entries with
        | None -> None
        | Some rootEntry ->
            let n = m.graph.nodes.[rootEntry.nodeId].children.Length
            if n = 0 then None
            elif sel.range.parent.nodeId = rootEntry.nodeId && sel.range.start = 0 then None
            else
                Some { parent = rootEntry; start = 0; endd = 0 })

/// Op: Ctrl+End — move selected objects to last slot under view root; selection follows.
let moveSelectionToViewRootEndOp (model: VM) : VM * Effect list =
    tryStructuralMove model (fun m sel ->
        match Map.tryFind m.siteMap.rootId m.siteMap.entries with
        | None -> None
        | Some rootEntry ->
            let n = m.graph.nodes.[rootEntry.nodeId].children.Length
            if n = 0 then None
            elif sel.range.parent.nodeId = rootEntry.nodeId && sel.range.endd >= n then None
            else
                Some { parent = rootEntry; start = n - 1; endd = n })

/// Op: Paste text into the model. preferredNodeIds from clipboard format, if present.
let pasteNodesOp (pastedText: string) (preferredNodeIds: string option) (model: VM)
    : VM * Effect list =
    let m, effs = pasteNodes pastedText preferredNodeIds model
    withSiteMap m, effs

/// Op: Toggle fold for a specific site-map entry.
let toggleFoldOp (instanceId: SiteId) (model: VM) : VM * Effect list =
    match Map.tryFind instanceId model.siteMap.entries with
    | None -> model, []
    | Some entry ->
        if entry.expanded then
            { model with siteMap = ViewModel.toggleFold instanceId model.siteMap }, []
        else
            let siteMap, nextId =
                ViewModel.expandEntry instanceId model.graph model.siteMap model.nextSiteId
            { model with siteMap = siteMap; nextSiteId = nextId }, []

/// Op: ArrowLeft in selection — fold if expanded, else move to parent.
let arrowLeftSelectionOp (model: VM) : VM * Effect list =
    model.selectedNodes
    |> Option.map (fun sel -> (focusedInstanceId sel, sel))
    |> Option.bind (fun (fid, _) ->
        Map.tryFind fid model.siteMap.entries
        |> Option.map (fun entry -> (entry, fid)))
    |> Option.map (fun (entry, focusInstId) ->
        let node = model.graph.nodes.[entry.nodeId]
        if not node.children.IsEmpty && entry.expanded then
            { model with siteMap = ViewModel.toggleFold focusInstId model.siteMap }
        else
            entry.parentInstanceId
            |> Option.bind (singleSelectionForInstance model.siteMap)
            |> Option.map (fun parentSel -> { model with selectedNodes = Some parentSel })
            |> Option.defaultValue model)
    |> Option.defaultValue model
    |> fun m -> m, []

/// Op: ArrowLeft in selection — move to parent (do not fold).
let arrowLeftSelectionNoFoldOp (model: VM) : VM * Effect list =
    model.selectedNodes
    |> Option.map focusedInstanceId
    |> Option.bind (fun fid -> Map.tryFind fid model.siteMap.entries)
    |> Option.bind (fun e -> e.parentInstanceId)
    |> Option.bind (singleSelectionForInstance model.siteMap)
    |> Option.map (fun ps -> { model with selectedNodes = Some ps })
    |> Option.defaultValue model
    |> fun m -> m, []

/// Op: ArrowRight in selection — expand if folded, else move to first child.
let arrowRightSelectionOp (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let focusInstId = focusedInstanceId sel
        match Map.tryFind focusInstId model.siteMap.entries with
        | None -> model, []
        | Some entry ->
            let node = model.graph.nodes.[entry.nodeId]
            let hasChildren = not node.children.IsEmpty
            if not hasChildren then model, []
            elif not entry.expanded then
                let siteMap, nextId =
                    ViewModel.expandEntry focusInstId model.graph model.siteMap model.nextSiteId
                { model with siteMap = siteMap; nextSiteId = nextId }, []
            else
                match entry.children with
                | [] -> model, []
                | firstChildInstId :: _ ->
                    match singleSelectionForInstance model.siteMap firstChildInstId with
                    | None -> model, []
                    | Some childSel -> { model with selectedNodes = Some childSel }, []

/// Op: Toggle fold for all selected entries.
let toggleFoldSelectionOp (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let selectedInstIds =
            sel.range.parent.children
            |> List.skip sel.range.start
            |> List.take (sel.range.endd - sel.range.start)
        let anyExpanded =
            selectedInstIds |> List.exists (fun instId ->
                match Map.tryFind instId model.siteMap.entries with
                | Some entry -> entry.expanded
                | None -> false)
        if anyExpanded then
            let siteMap =
                selectedInstIds
                |> List.fold (fun sm instId -> ViewModel.toggleFold instId sm) model.siteMap
            { model with siteMap = siteMap }, []
        else
            let siteMap, nextId =
                selectedInstIds |> List.fold
                    (fun (sm, nid) instId -> ViewModel.expandEntry instId model.graph sm nid)
                    (model.siteMap, model.nextSiteId)
            { model with siteMap = siteMap; nextSiteId = nextId }, []

/// Op: Duplicate the selected nodes as references — insert the same NodeIds beside.
/// Inserts at range.endd; selection expands to include the new references.
let duplicateSelectionOp (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let selectedChildren = rangeChildren model.graph sel.range
        if selectedChildren.IsEmpty then model, []
        else
            let duplicatedRefs =
                selectedChildren
                |> List.map (fun child -> { child with ref = Ownership.Ref })
            let insertOp = Op.Replace(sel.range.parent.nodeId, sel.range.endd, [], duplicatedRefs)
            let change =
                { id = model.revision.Value
                  changeId = System.Guid.NewGuid()
                  ops = [ insertOp ] }
            match applyAndPost change model with
            | None, _ -> model, []
            | Some m, effects -> withSiteMap m, effects

/// Op: Delete the selected nodes (Replace with nothing), updating selection to next/prev/parent.
let deleteSelectionOp (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let selectedChildren = rangeChildren model.graph sel.range
        let ownedDeletedIds =
            selectedChildren
            |> List.choose (fun child ->
                match child.ref with
                | Ownership.Owner -> Some child.id
                | Ownership.Ref -> None)
            |> List.distinct

        let excludedParentsForSearch =
            let descendantsAndSelf (startId: NodeId) =
                let rec loop (stack: NodeId list) (visited: Set<NodeId>) =
                    match stack with
                    | [] -> visited
                    | nodeId :: rest ->
                        if Set.contains nodeId visited then
                            loop rest visited
                        else
                            let childIds =
                                model.graph.nodes
                                |> Map.tryFind nodeId
                                |> Option.map (fun node -> node.children |> List.map (fun child -> child.id))
                                |> Option.defaultValue []
                            loop (childIds @ rest) (Set.add nodeId visited)

                loop [ startId ] Set.empty

            selectedChildren
            |> List.fold (fun acc child -> Set.union acc (descendantsAndSelf child.id)) Set.empty

        let replacementOpsForOrphanedOwners =
            let tryPromoteRefToOwner (targetId: NodeId) =
                model.graph.nodes
                |> Map.toSeq
                |> Seq.tryPick (fun (parentId, parent) ->
                    if Set.contains parentId excludedParentsForSearch then
                        None
                    else
                        parent.children
                        |> List.mapi (fun index child -> index, child)
                        |> List.tryPick (fun (index, child) ->
                            let inDeletedSpan =
                                parentId = sel.range.parent.nodeId
                                && index >= sel.range.start
                                && index < sel.range.endd

                            if inDeletedSpan then
                                None
                            elif child.id = targetId && child.ref = Ownership.Ref then
                                Some(Op.Replace(parentId, index, [ child ], [ { child with ref = Ownership.Owner } ]))
                            else
                                None))
                |> Option.toList

            ownedDeletedIds |> List.collect tryPromoteRefToOwner

        let change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops =
                replacementOpsForOrphanedOwners
                @ [ Op.Replace(sel.range.parent.nodeId, sel.range.start, selectedChildren, []) ] }
        match applyAndPost change model with
        | None, _ -> model, []
        | Some m, effects ->
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
            withSiteMap { m with selectedNodes = newSel }, effects

/// Union of user classes (non-amb-) across selected nodes, as space-separated string.
let private initialUserClassesForSelection (model: VM) (sel: Selection) : string =
    let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
    let selectedIds =
        parentNode.children
        |> List.skip sel.range.start
        |> List.take (sel.range.endd - sel.range.start)
        |> List.map (fun child -> child.id)
    selectedIds
    |> List.collect (fun nid ->
        model.graph.nodes.[nid].cssClasses
        |> CssClass.toList
        |> List.filter (fun c -> not (c.StartsWith("amb-"))))
    |> List.distinct
    |> String.concat " "

/// Op: Open the CSS class prompt overlay, pre-filled with current user classes (amb- excluded).
let openCssClassPromptOp (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        { model with mode = CssClassPrompt (model.mode, initialUserClassesForSelection model sel) }, []

/// Op: Close the CSS class prompt without applying.
let closeCssClassPromptOp (model: VM) : VM * Effect list =
    match model.mode with
    | CssClassPrompt (ret, _) -> { model with mode = ret }, []
    | _ -> model, []

let private readCssClassPromptValue () : string =
    let el = document.getElementById "css-class-prompt-input"
    if isNull el then ""
    else (el :?> HTMLInputElement).value

/// Op: Substitute user classes from prompt input (old → new), preserving amb- classes. Close.
let submitCssClassPromptOp (model: VM) : VM * Effect list =
    match model.mode, model.selectedNodes with
    | CssClassPrompt (ret, _), Some sel ->
        let input = readCssClassPromptValue ()
        let newUserClasses = CssClass.parseUserClasses (if isNull input then "" else input)
        let result = { model with mode = ret }
        let parentNode = model.graph.nodes.[sel.range.parent.nodeId]
        let selectedIds =
            parentNode.children
            |> List.skip sel.range.start
            |> List.take (sel.range.endd - sel.range.start)
            |> List.map (fun child -> child.id)
        let ops =
            selectedIds
            |> List.choose (fun nid ->
                let node = model.graph.nodes.[nid]
                let oldClasses = node.cssClasses
                let ambClasses = CssClass.ambOnly oldClasses
                let newClasses = CssClass.toList ambClasses @ CssClass.toList newUserClasses |> CssClass.ofList
                if oldClasses = newClasses then None
                else Some (Op.SetClasses(nid, oldClasses, newClasses)))
        if ops.IsEmpty then result, []
        else
            let change =
                { id = model.revision.Value
                  changeId = System.Guid.NewGuid()
                  ops = ops }
            match applyAndPost change result with
            | Some m, effects -> m, effects
            | None, _ -> result, []
    | _ -> model, []

/// Build a first-child Selection for the root entry of a freshly-built siteMap.
/// Returns None if the root has no children.
let private firstChildSelection (siteMap: SiteMap) (rootNodeId: NodeId) : Selection option =
    siteMap.entries
    |> Map.tryPick (fun _ e -> if e.nodeId = rootNodeId then Some e else None)
    |> Option.bind (fun rootEntry ->
        if rootEntry.children.IsEmpty then None
        else Some { range = { parent = rootEntry; start = 0; endd = 1 }; focus = 0 })

/// Op: Zoom in — set the view root to the first selected node (Ctrl+]).
/// Commits any in-progress edit first. No-op when the view root is focused or the node is a leaf.
let zoomInOp (model: VM) : VM * Effect list =
    let model', effs = commitIfEditing model
    match model'.selectedNodes with
    | None -> model', effs
    | Some sel ->
        let firstId = firstSelectedNodeId model'.graph sel
        let firstNode = model'.graph.nodes.[firstId]
        let zoomId =
            if firstNode.children.IsEmpty
            then
                match Graph.tryFindParentAndIndex firstId model'.graph with
                | Some (parentId, _) -> parentId
                | None -> firstId
            else firstId
        let siteMap, nextId =
            ViewModel.buildSiteMapFrom model'.graph zoomId model'.nextSiteId
        { model' with
            zoomRoot = Some zoomId
            siteMap = siteMap
            nextSiteId = nextId
            selectedNodes = firstChildSelection siteMap zoomId
            mode = Selecting }, effs

/// Op: Zoom out — move the view root one level up toward the graph root (Ctrl+[).
/// Commits any in-progress edit first. No-op when already showing the full tree.
let zoomOutOp (model: VM) : VM * Effect list =
    let model', effs = commitIfEditing model
    match model'.zoomRoot with
    | None -> model', effs
    | Some currentZoomRoot ->
        let newZoomRoot =
            match Graph.tryFindParentAndIndex currentZoomRoot model'.graph with
            | None -> None
            | Some (parentId, _) ->
                if parentId = model'.graph.root then None else Some parentId
        let effectiveRoot = newZoomRoot |> Option.defaultValue model'.graph.root
        let siteMap, nextId =
            ViewModel.buildSiteMapFrom model'.graph effectiveRoot model'.nextSiteId
        { model' with
            zoomRoot = newZoomRoot
            siteMap = siteMap
            nextSiteId = nextId
            selectedNodes = firstChildSelection siteMap effectiveRoot
            mode = Selecting }, effs

/// Op: Retry pending server POST. Only valid from WaitingToRetry state.
/// resetCount=true (manual click) restarts the attempt counter from 1.
let retryPendingOp (resetCount: bool) (model: VM) : VM * Effect list =
    match model.syncInfo.syncState, model.syncInfo.pendingChanges with
    | ServerRejected, _ | CodeOutdated, _ | DataOutdated, _ -> model, []
    | _, [] ->
        { model with syncInfo = model.syncInfo |> SyncInfo.withSyncState Idle }, []
    | WaitingToRetry n, _ ->
        let nextAttempt = if resetCount then 1 else n + 1
        let effects =
            model.syncInfo.pendingChanges
            |> List.tryHead
            |> Option.map (fun head -> [ SubmitChange (model.revision.Value, head) ])
            |> Option.defaultValue []
        { model with
            syncInfo = model.syncInfo |> SyncInfo.withSyncState (Sending nextAttempt) }, effects
    | Sending _, _ -> model, []
    | _ -> model, []

/// Op: Undo the last change, committing any in-progress edit first.
let undoOp (model: VM) : VM * Effect list =
    let model', commitEffects = commitIfEditing model
    let state = { graph = model'.graph; history = model'.history; revision = model'.revision }
    match model'.history.past |> List.tryHead with
    | None -> model', commitEffects
    | Some headChange ->
        match History.undo state with
        | ApplyResult.Changed newState ->
            let invertedChange = { Change.invert headChange with id = model'.history.nextId }
            let pending = model'.syncInfo.pendingChanges @ [invertedChange]
            let nextSyncInfo, submitEffects =
                { model'.syncInfo with pendingChanges = pending }
                |> SyncPlanner.tryStartSubmit model'.revision
            let effects = commitEffects @ (SavePendingQueue pending) :: submitEffects
            { model' with
                graph = newState.graph
                history = newState.history
                mode = Selecting
                syncInfo = nextSyncInfo }
            |> withSiteMap
            |> fun m -> m, effects
        | _ -> model', commitEffects

/// Op: Redo the last undone change, committing any in-progress edit first.
let redoOp (model: VM) : VM * Effect list =
    let model', commitEffects = commitIfEditing model
    let state = { graph = model'.graph; history = model'.history; revision = model'.revision }
    match model'.history.future |> List.tryHead with
    | None -> model', commitEffects
    | Some headChange ->
        match History.redo state with
        | ApplyResult.Changed newState ->
            let reChange =
                { headChange with
                    id = model'.history.nextId
                    changeId = System.Guid.NewGuid() }
            let pending = model'.syncInfo.pendingChanges @ [reChange]
            let nextSyncInfo, submitEffects =
                { model'.syncInfo with pendingChanges = pending }
                |> SyncPlanner.tryStartSubmit model'.revision
            let effects = commitEffects @ (SavePendingQueue pending) :: submitEffects
            { model' with
                graph = newState.graph
                history = newState.history
                mode = Selecting
                syncInfo = nextSyncInfo }
            |> withSiteMap
            |> fun m -> m, effects
        | _ -> model', commitEffects
