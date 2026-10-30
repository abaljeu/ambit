namespace Gambol.Shared

// ---------------------------------------------------------------------------
// DOM mutation plan (pure — no Browser interop)
// ---------------------------------------------------------------------------

module ViewModelDomPlan =

    open ViewModelSiteMap
    open ViewModelRowState
    open ViewModelChildrenIndicator

    type RowPatch =
        | SetClassName of newClass: string
        | SetText of newText: string
        | SetTextClasses of classes: CssClasses
        | SetFoldArrow of arrow: string   // "▼" or "▶" (has children); "●" (no children, no behavior)
        | SetNodeName of name: string
        | SetFileIndicator of text: string * title: string option

    type RowMutation =
        | RemoveRow of instId: SiteId
        | CreateRow of instId: SiteId
        | RecreateRow of instId: SiteId
        | PatchRow of instId: SiteId * patches: RowPatch list

    let private visibleDescendantIds (siteMap: SiteMap) (instId: SiteId) : SiteId list =
        let rec go id =
            match Map.tryFind id siteMap.entries with
            | Some e when e.expanded ->
                e.children |> List.collect (fun c -> c :: go c)
            | _ -> []
        go instId

    /// Instance ids whose selected/focused presentation can change for this selection.
    let private selectionAppearanceIds (model: VM) : Set<SiteId> =
        match model.selectedNodes with
        | None -> Set.singleton model.siteMap.rootId
        | Some sel ->
            let parentEntry =
                Map.tryFind sel.range.parent.instanceId model.siteMap.entries
                |> Option.defaultValue sel.range.parent
            let direct =
                [ sel.range.start .. sel.range.endd - 1 ]
                |> List.choose (fun i -> List.tryItem i parentEntry.children)
            direct
            |> List.collect (fun id -> id :: visibleDescendantIds model.siteMap id)
            |> Set.ofList

    let private placeholderRowClass (model: VM) (entry: SiteEntry) (sel: bool) (foc: bool) =
        let isRoot = entry.instanceId = model.siteMap.rootId
        "amb-row"
        |> CssClass.add (rowOwnershipClass model entry)
        |> CssClass.add "amb-row-absent"
        |> CssClass.addIf isRoot "amb-view-root"
        |> CssClass.addIf sel "amb-selected"
        |> CssClass.addIf foc "amb-focused"
        |> CssClass.addIf
            (Set.contains entry.nodeId model.actorLiveFocusIds)
            "amb-actor-live"

    let private placeholderCachedMutation
        (oldModel: VM) (newModel: VM) (entry: SiteEntry) (instId: SiteId) : RowMutation =
        match Map.tryFind entry.nodeId oldModel.graph.nodes with
        | Some _ -> RecreateRow instId
        | None ->
            let sel = isEntrySelected newModel entry
            let foc = isEntryFocused newModel entry
            let newClass = placeholderRowClass newModel entry sel foc
            let oldEntry = Map.tryFind instId oldModel.siteMap.entries
            let oldSel =
                oldEntry
                |> Option.map (isEntrySelected oldModel)
                |> Option.defaultValue false
            let oldFoc =
                oldEntry
                |> Option.map (isEntryFocused oldModel)
                |> Option.defaultValue false
            let oldClass =
                match oldEntry with
                | Some e -> placeholderRowClass oldModel e oldSel oldFoc
                | None -> ""
            let patches = [
                if newClass <> oldClass then yield SetClassName newClass
            ]
            PatchRow (instId, patches)

    let private rowClassName (model: VM) (entry: SiteEntry) (node: Node) (sel: bool) (foc: bool) =
        let isRoot = entry.instanceId = model.siteMap.rootId
        let syncClass = rowWorkspacePathSyncClass model entry node
        "amb-row"
        |> CssClass.add (rowOwnershipClass model entry)
        |> addSpecialKindRowClass node.id node.kind
        |> (fun s ->
            match syncClass with
            | Some c -> CssClass.add c s
            | None -> s)
        |> CssClass.addIf (rowArtifactAbsentClassEligible model entry node) "amb-row-artifact-absent"
        |> CssClass.addIf isRoot "amb-view-root"
        |> CssClass.addIf sel "amb-selected"
        |> CssClass.addIf foc "amb-focused"
        |> CssClass.addIf
            (Set.contains entry.nodeId model.actorLiveFocusIds)
            "amb-actor-live"

    let private selectionClassPatches (oldModel: VM) (newModel: VM) (instId: SiteId) : RowPatch list =
        match Map.tryFind instId newModel.siteMap.entries with
        | None -> []
        | Some entry ->
            match Map.tryFind entry.nodeId newModel.graph.nodes with
            | None -> []
            | Some newNode ->
                let oldEntry = Map.tryFind instId oldModel.siteMap.entries
                let oldNode = oldModel.graph.nodes |> Map.tryFind entry.nodeId
                let sel = isEntrySelected newModel entry
                let foc = isEntryFocused newModel entry
                let oldSel = oldEntry |> Option.map (isEntrySelected oldModel) |> Option.defaultValue false
                let oldFoc = oldEntry |> Option.map (isEntryFocused oldModel) |> Option.defaultValue false
                let newClass = rowClassName newModel entry newNode sel foc
                let oldClass =
                    match oldEntry, oldNode with
                    | Some e, Some n -> rowClassName oldModel e n oldSel oldFoc
                    | _ -> ""
                [
                    if newClass <> oldClass then yield SetClassName newClass
                    let newFileIndicator, newTitle = rowFileIndicator newModel entry newNode
                    let oldFileIndicator, oldTitle =
                        match oldEntry, oldNode with
                        | Some e, Some n -> rowFileIndicator oldModel e n
                        | _ -> "", None
                    if newFileIndicator <> oldFileIndicator || newTitle <> oldTitle then
                        yield SetFileIndicator (newFileIndicator, newTitle)
                ]

    /// Structure (siteMap/graph) and row chrome that affect classes are unchanged; only
    /// selection/focus (and possibly desktopFileIndicator) may differ. Both modes Selecting.
    let private canUseSelectionFastPath (oldModel: VM) (newModel: VM) : bool =
        obj.ReferenceEquals(oldModel.siteMap, newModel.siteMap)
        && obj.ReferenceEquals(oldModel.graph, newModel.graph)
        && (match oldModel.mode, newModel.mode with
            | Selecting, Selecting -> true
            | _ -> false)
        && oldModel.zoomRoot = newModel.zoomRoot
        && oldModel.workspaceSyncFacts = newModel.workspaceSyncFacts
        && oldModel.workspaceMappedLabels = newModel.workspaceMappedLabels
        && oldModel.workspaceRoots = newModel.workspaceRoots
        && oldModel.workspaceRemotes = newModel.workspaceRemotes
        && oldModel.desktopCapabilities = newModel.desktopCapabilities
        && oldModel.serverCapabilities = newModel.serverCapabilities
        && oldModel.actorLiveFocusIds = newModel.actorLiveFocusIds

    let private tryPlanSelectionOnly (oldModel: VM) (newModel: VM) : RowMutation list option =
        if not (canUseSelectionFastPath oldModel newModel) then None
        else
            let candidates =
                Set.union (selectionAppearanceIds oldModel) (selectionAppearanceIds newModel)
            let mutations =
                candidates
                |> Set.toList
                |> List.choose (fun instId ->
                    match selectionClassPatches oldModel newModel instId with
                    | [] -> None
                    | patches -> Some (PatchRow (instId, patches)))
            Some mutations

    let private hasStructuralRowMutation (mutations: RowMutation list) =
        mutations
        |> List.exists (function
            | CreateRow _
            | RemoveRow _
            | RecreateRow _ -> true
            | PatchRow _ -> false)

    /// True when patchDOM must walk visible rows and correct DOM order.
    /// Create/Remove/Recreate always need it; sibling reorder (MoveUp/Down) often emits
    /// only PatchRow (or nothing structural) while visible preorder still changes.
    let needsDomOrderWalk (oldModel: VM) (newModel: VM) (mutations: RowMutation list) : bool =
        hasStructuralRowMutation mutations
        || getVisibleInstanceIds oldModel.siteMap <> getVisibleInstanceIds newModel.siteMap

    type private ResidentRowCompare = {
        oldModel: VM
        newModel: VM
        entry: SiteEntry
        instId: SiteId
        newNode: Node
        oldNode: Node option
        oldEntry: SiteEntry option
    }

    let private removalMutations (cachedInstIds: Set<SiteId>) (visibleIds: Set<SiteId>) =
        cachedInstIds
        |> Set.filter (fun id -> not (Set.contains id visibleIds))
        |> Set.toList
        |> List.map RemoveRow

    let private workspaceRemoteArrived (oldModel: VM) (newModel: VM) (node: Node) =
        oldModel.workspaceRemotes <> newModel.workspaceRemotes
        && match node.kind with
           | Special Workspace when node.id <> Graph.rootId -> true
           | _ -> false

    let private classNamePatch (row: ResidentRowCompare) : RowPatch option =
        let sel = isEntrySelected row.newModel row.entry
        let foc = isEntryFocused row.newModel row.entry
        let newClass = rowClassName row.newModel row.entry row.newNode sel foc
        let oldSel =
            row.oldEntry
            |> Option.map (isEntrySelected row.oldModel)
            |> Option.defaultValue false
        let oldFoc =
            row.oldEntry
            |> Option.map (isEntryFocused row.oldModel)
            |> Option.defaultValue false
        let oldClass =
            match row.oldEntry, row.oldNode with
            | Some e, Some n -> rowClassName row.oldModel e n oldSel oldFoc
            | _ ->
                "amb-row"
                |> CssClass.add "amb-row-owned"
                |> CssClass.addIf oldSel "amb-selected"
                |> CssClass.addIf oldFoc "amb-focused"
        if newClass = oldClass then None else Some (SetClassName newClass)

    let private fileIndicatorPatch (row: ResidentRowCompare) : RowPatch option =
        let newFileIndicator, newTitle =
            rowFileIndicator row.newModel row.entry row.newNode
        let oldFileIndicator, oldTitle =
            match row.oldEntry, row.oldNode with
            | Some e, Some n -> rowFileIndicator row.oldModel e n
            | _ -> "", None
        if newFileIndicator <> oldFileIndicator || newTitle <> oldTitle then
            Some (SetFileIndicator (newFileIndicator, newTitle))
        else
            None

    /// Sync row text when the graph or the filename changes.
    let private outlineTextPatch (row: ResidentRowCompare) : RowPatch option =
        let newText = outlineDisplayText row.newNode
        let oldText =
            row.oldNode
            |> Option.map outlineDisplayText
            |> Option.defaultValue ""
        if newText = oldText then None else Some (SetText newText)

    let private nodeNamePatch (row: ResidentRowCompare) : RowPatch option =
        let newName = rowNameDisplayText row.newNode.name
        let oldName =
            row.oldNode
            |> Option.map (fun n -> rowNameDisplayText n.name)
            |> Option.defaultValue ""
        if newName = oldName then None else Some (SetNodeName newName)

    let private textClassesPatch (row: ResidentRowCompare) : RowPatch option =
        let newClasses = row.newNode.cssClasses
        let oldClasses =
            row.oldNode
            |> Option.map (fun n -> n.cssClasses)
            |> Option.defaultValue CssClass.empty
        if newClasses = oldClasses then None else Some (SetTextClasses newClasses)

    let private foldArrowPatch (row: ResidentRowCompare) : RowPatch option =
        let indicator = rowChildrenIndicator row.newModel.graph row.newNode
        if indicator <> RowChildrenIndicator.FoldChevron then None
        else
            let oldExpanded =
                row.oldEntry
                |> Option.map (fun e -> e.expanded)
                |> Option.defaultValue false
            if row.entry.expanded = oldExpanded then None
            else
                let arrow = if row.entry.expanded then "\u25BC" else "\u25B6"
                Some (SetFoldArrow arrow)

    let private residentRowPatches (row: ResidentRowCompare) : RowPatch list =
        [
            classNamePatch row
            fileIndicatorPatch row
            outlineTextPatch row
            nodeNamePatch row
            textClassesPatch row
            foldArrowPatch row
        ]
        |> List.choose id

    let private residentNeedsRecreate (row: ResidentRowCompare) =
        let wasEditing =
            row.oldEntry
            |> Option.map (isEditingEntry row.oldModel)
            |> Option.defaultValue false
        let nowEditing = isEditingEntry row.newModel row.entry
        let oldIndicator = rowChildrenIndicatorForId row.oldModel.graph row.entry.nodeId
        let newIndicator = rowChildrenIndicator row.newModel.graph row.newNode
        let oldKind = row.oldNode |> Option.map (fun n -> n.kind)
        wasEditing <> nowEditing
        || oldIndicator <> newIndicator
        || oldKind <> Some row.newNode.kind
        || workspaceRemoteArrived row.oldModel row.newModel row.newNode

    let private residentCachedMutation (row: ResidentRowCompare) : RowMutation =
        if residentNeedsRecreate row then
            RecreateRow row.instId
        else
            PatchRow (row.instId, residentRowPatches row)

    let private visibleRowMutation
        (oldModel: VM) (newModel: VM) (cachedInstIds: Set<SiteId>) (instId: SiteId)
        : RowMutation =
        let entry = newModel.siteMap.entries.[instId]
        if not (Set.contains instId cachedInstIds) then
            CreateRow instId
        else
            match Map.tryFind entry.nodeId newModel.graph.nodes with
            | None -> placeholderCachedMutation oldModel newModel entry instId
            | Some newNode ->
                residentCachedMutation
                    {
                        oldModel = oldModel
                        newModel = newModel
                        entry = entry
                        instId = instId
                        newNode = newNode
                        oldNode = Map.tryFind entry.nodeId oldModel.graph.nodes
                        oldEntry = Map.tryFind instId oldModel.siteMap.entries
                    }

    /// Compute the minimal set of DOM mutations needed to transition from oldModel to newModel.
    /// cachedInstIds is the set of instanceIds currently held in the element cache.
    /// Returns removals followed by visible-row operations in preorder display order.
    let planPatchDOM (oldModel: VM) (newModel: VM) (cachedInstIds: Set<SiteId>) : RowMutation list =
        match tryPlanSelectionOnly oldModel newModel with
        | Some mutations -> mutations
        | None ->
            let newVisible = getVisibleInstanceIds newModel.siteMap
            let visibleIds = Set.ofList newVisible
            let removals = removalMutations cachedInstIds visibleIds
            let upserts =
                newVisible
                |> List.map (visibleRowMutation oldModel newModel cachedInstIds)
            removals @ upserts
