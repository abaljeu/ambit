namespace Gambol.Shared

// ---------------------------------------------------------------------------
// DOM mutation plan (pure — no Browser interop)
// ---------------------------------------------------------------------------

module ViewModelDomPlan =

    open ViewModelSiteMap
    open ViewModelRowState

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

    /// Compute the minimal set of DOM mutations needed to transition from oldModel to newModel.
    /// cachedInstIds is the set of instanceIds currently held in the element cache.
    /// Returns removals followed by visible-row operations in preorder display order.
    let planPatchDOM (oldModel: VM) (newModel: VM) (cachedInstIds: Set<SiteId>) : RowMutation list =
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
                    let oldHasChildren =
                        oldModel.graph.nodes
                        |> Map.tryFind entry.nodeId
                        |> Option.map (fun n -> not n.children.IsEmpty)
                        |> Option.defaultValue false
                    let newHasChildren = not (newModel.graph.nodes.[entry.nodeId].children.IsEmpty)
                    let oldKind =
                        oldModel.graph.nodes
                        |> Map.tryFind entry.nodeId
                        |> Option.map (fun n -> n.kind)
                    let newKind = newModel.graph.nodes.[entry.nodeId].kind
                    if wasEditing <> nowEditing || oldHasChildren <> newHasChildren
                       || oldKind <> Some newKind then
                        RecreateRow instId
                    else
                        let oldEntry = Map.tryFind instId oldModel.siteMap.entries
                        let patches = [
                            let sel = isEntrySelected newModel entry
                            let foc = isEntryFocused newModel entry
                            let isRoot = entry.instanceId = newModel.siteMap.rootId
                            let newNode = newModel.graph.nodes.[entry.nodeId]
                            let oldNode = oldModel.graph.nodes |> Map.tryFind entry.nodeId
                            let newSyncClass =
                                rowWorkspacePathSyncClass newModel entry newNode
                            let newClass =
                                "amb-row"
                                |> CssClass.add (rowOwnershipClass newModel entry)
                                |> addSpecialKindRowClass newNode.id newNode.kind
                                |> (fun s ->
                                    match newSyncClass with
                                    | Some c -> CssClass.add c s
                                    | None -> s)
                                |> CssClass.addIf
                                    (rowArtifactAbsentClassEligible newModel entry newNode)
                                    "amb-row-artifact-absent"
                                |> CssClass.addIf isRoot "amb-view-root"
                                |> CssClass.addIf sel "amb-selected"
                                |> CssClass.addIf foc "amb-focused"
                            let oldSel = oldEntry |> Option.map (isEntrySelected oldModel) |> Option.defaultValue false
                            let oldFoc = oldEntry |> Option.map (isEntryFocused oldModel) |> Option.defaultValue false
                            let oldSyncClass =
                                match oldEntry, oldNode with
                                | Some e, Some n -> rowWorkspacePathSyncClass oldModel e n
                                | _ -> None
                            let oldClass =
                                "amb-row"
                                |> CssClass.add (
                                    oldEntry
                                    |> Option.map (rowOwnershipClass oldModel)
                                    |> Option.defaultValue "amb-row-owned")
                                |> (fun s ->
                                    match oldNode with
                                    | Some n -> addSpecialKindRowClass n.id n.kind s
                                    | None -> s)
                                |> (fun s ->
                                    match oldSyncClass with
                                    | Some c -> CssClass.add c s
                                    | None -> s)
                                |> CssClass.addIf
                                    (match oldEntry, oldNode with
                                     | Some e, Some n -> rowArtifactAbsentClassEligible oldModel e n
                                     | _ -> false)
                                    "amb-row-artifact-absent"
                                |> CssClass.addIf isRoot "amb-view-root"
                                |> CssClass.addIf oldSel "amb-selected"
                                |> CssClass.addIf oldFoc "amb-focused"
                            if newClass <> oldClass then yield SetClassName newClass
                            let newIndicator, newTitle =
                                rowFileIndicator newModel entry newNode
                            yield SetFileIndicator (newIndicator, newTitle)
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
                            if newHasChildren then
                                let oldExpanded = oldEntry |> Option.map (fun e -> e.expanded) |> Option.defaultValue false
                                if entry.expanded <> oldExpanded then
                                    yield SetFoldArrow (if entry.expanded then "\u25BC" else "\u25B6")
                        ]
                        PatchRow (instId, patches)
                else
                    CreateRow instId)

        removals @ upserts
