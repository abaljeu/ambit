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
        && oldModel.desktopCapabilities = newModel.desktopCapabilities
        && oldModel.serverCapabilities = newModel.serverCapabilities

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

    /// Compute the minimal set of DOM mutations needed to transition from oldModel to newModel.
    /// cachedInstIds is the set of instanceIds currently held in the element cache.
    /// Returns removals followed by visible-row operations in preorder display order.
    let planPatchDOM (oldModel: VM) (newModel: VM) (cachedInstIds: Set<SiteId>) : RowMutation list =
        match tryPlanSelectionOnly oldModel newModel with
        | Some mutations -> mutations
        | None ->
            let newVisible = getVisibleInstanceIds newModel.siteMap
            let newVisibleSet = Set.ofList newVisible

            let removals =
                cachedInstIds
                |> Set.filter (fun id -> not (Set.contains id newVisibleSet))
                |> Set.toList
                |> List.map RemoveRow

            let upserts =
                newVisible |> List.map (fun instId ->
                    let entry = newModel.siteMap.entries.[instId]
                    if Set.contains instId cachedInstIds then
                        let wasEditing = isEditingEntry oldModel entry
                        let nowEditing = isEditingEntry newModel entry
                        let newNode = newModel.graph.nodes.[entry.nodeId]
                        let oldNode = oldModel.graph.nodes |> Map.tryFind entry.nodeId
                        let oldChildrenIndicator =
                            oldNode
                            |> Option.map rowChildrenIndicator
                            |> Option.defaultValue RowChildrenIndicator.SolidCircle
                        let newChildrenIndicator = rowChildrenIndicator newNode
                        let oldKind = oldNode |> Option.map (fun n -> n.kind)
                        let newKind = newNode.kind
                        if wasEditing <> nowEditing
                           || oldChildrenIndicator <> newChildrenIndicator
                           || oldKind <> Some newKind then
                            RecreateRow instId
                        else
                            let oldEntry = Map.tryFind instId oldModel.siteMap.entries
                            let patches = [
                                let sel = isEntrySelected newModel entry
                                let foc = isEntryFocused newModel entry
                                let newClass = rowClassName newModel entry newNode sel foc
                                let oldSel = oldEntry |> Option.map (isEntrySelected oldModel) |> Option.defaultValue false
                                let oldFoc = oldEntry |> Option.map (isEntryFocused oldModel) |> Option.defaultValue false
                                let oldClass =
                                    match oldEntry, oldNode with
                                    | Some e, Some n -> rowClassName oldModel e n oldSel oldFoc
                                    | _ ->
                                        "amb-row"
                                        |> CssClass.add "amb-row-owned"
                                        |> CssClass.addIf oldSel "amb-selected"
                                        |> CssClass.addIf oldFoc "amb-focused"
                                if newClass <> oldClass then yield SetClassName newClass
                                let newFileIndicator, newTitle =
                                    rowFileIndicator newModel entry newNode
                                let oldFileIndicator, oldTitle =
                                    match oldEntry, oldNode with
                                    | Some e, Some n -> rowFileIndicator oldModel e n
                                    | _ -> "", None
                                if newFileIndicator <> oldFileIndicator || newTitle <> oldTitle then
                                    yield SetFileIndicator (newFileIndicator, newTitle)
                                // Sync row text on graph or filename changes (editing row included — e.g. paste).
                                let newText = outlineDisplayText newNode
                                let oldText =
                                    oldNode |> Option.map outlineDisplayText |> Option.defaultValue ""
                                if newText <> oldText then yield SetText newText
                                let newName = rowNameDisplayText newNode.name
                                let oldName =
                                    oldNode |> Option.map (fun n -> rowNameDisplayText n.name) |> Option.defaultValue ""
                                if newName <> oldName then yield SetNodeName newName
                                let newClasses = newNode.cssClasses
                                let oldClasses = oldNode |> Option.map (fun n -> n.cssClasses) |> Option.defaultValue CssClass.empty
                                if newClasses <> oldClasses then yield SetTextClasses newClasses
                                if newChildrenIndicator = RowChildrenIndicator.FoldChevron then
                                    let oldExpanded = oldEntry |> Option.map (fun e -> e.expanded) |> Option.defaultValue false
                                    if entry.expanded <> oldExpanded then
                                        yield SetFoldArrow (if entry.expanded then "\u25BC" else "\u25B6")
                            ]
                            PatchRow (instId, patches)
                    else
                        CreateRow instId)

            removals @ upserts
