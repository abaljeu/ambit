namespace Gambol.Shared

// ---------------------------------------------------------------------------
// Row selection, file indicator, and label helpers
// ---------------------------------------------------------------------------

module ViewModelRowState =

    open ViewModelSelection

    /// True when entry is directly within the selected index range AND is a child
    /// of the exact same parent instance that the selection was made on.
    /// Prevents sibling occurrences of the same NodeId (DIGRAPH links) from lighting up.
    let private isInstanceDirectlySelected (sel: Selection) (siteMap: SiteMap) (entry: SiteEntry) : bool =
        match entry.parentInstanceId with
        | Some parentInstId when parentInstId = sel.range.parent.instanceId ->
            match Map.tryFind parentInstId siteMap.entries with
            | None -> false
            | Some parentEntry ->
                match parentEntry.children |> List.tryFindIndex ((=) entry.instanceId) with
                | Some idx -> idx >= sel.range.start && idx < sel.range.endd
                | None -> false
        | _ -> false

    /// True when entry is at the focused index AND is a child of the exact same
    /// parent instance that the selection was made on.
    let private isInstanceFocused (sel: Selection) (siteMap: SiteMap) (entry: SiteEntry) : bool =
        match entry.parentInstanceId with
        | Some parentInstId when parentInstId = sel.range.parent.instanceId ->
            match Map.tryFind parentInstId siteMap.entries with
            | None -> false
            | Some parentEntry ->
                match parentEntry.children |> List.tryFindIndex ((=) entry.instanceId) with
                | Some idx -> idx = sel.focus
                | None -> false
        | _ -> false

    /// Walk up the parentInstanceId chain: true if entry or any ancestor satisfies pred.
    let private ancestorMatch (siteMap: SiteMap) (entry: SiteEntry) (pred: SiteEntry -> bool) : bool =
        let rec go parentInstId =
            match parentInstId with
            | None -> false
            | Some pid ->
                match Map.tryFind pid siteMap.entries with
                | None -> false
                | Some pe -> pred pe || go pe.parentInstanceId
        pred entry || go entry.parentInstanceId

    let isEntrySelected (model: VM) (entry: SiteEntry) =
        if model.selectedNodes = None && entry.parentInstanceId = None then true
        else
            match model.selectedNodes with
            | None -> false
            | Some sel -> ancestorMatch model.siteMap entry (isInstanceDirectlySelected sel model.siteMap)

    let isEntryFocused (model: VM) (entry: SiteEntry) =
        if model.selectedNodes = None && entry.parentInstanceId = None then true
        else
            match model.selectedNodes with
            | None -> false
            | Some sel -> ancestorMatch model.siteMap entry (isInstanceFocused sel model.siteMap)

    let isEditingEntry (model: VM) (entry: SiteEntry) : bool =
        let effectiveMode =
            match model.mode with
            | CommandPalette (_, _, ret) -> ret
            | SearchDialog s -> s.returnTo
            | FileSearchDialog s -> s.returnTo
            | CssClassPrompt (ret, _) -> ret
            | RenamePrompt (ret, _) -> ret
            | m -> m
        match effectiveMode, model.selectedNodes with
        | Editing _, None    -> entry.parentInstanceId = None
        | Editing _, Some sel -> isInstanceFocused sel model.siteMap entry
        | _ -> false

    /// Enter edit mode for a view-line instance in one model step (selection + Editing).
    /// Returns None for unknown instances or the graph root node.
    let startEditInstanceAtPos (instanceId: SiteId) (cursorPos: int) (model: VM) : VM option =
        match Map.tryFind instanceId model.siteMap.entries with
        | None -> None
        | Some entry ->
            if entry.nodeId = model.graph.root then
                None
            else
                let text = model.graph.nodes.[entry.nodeId].text
                let selectedNodes =
                    if instanceId = model.siteMap.rootId then
                        None
                    else
                        singleSelectionForInstance model.siteMap instanceId
                Some
                    { model with
                        selectedNodes = selectedNodes
                        mode = Editing (text, EditCaret.Utf16Index cursorPos) }

    let isActiveEntry (model: VM) (entry: SiteEntry) : bool =
        match model.selectedNodes with
        | None -> entry.instanceId = model.siteMap.rootId
        | Some sel -> focusedInstanceId sel = Some entry.instanceId

    let activeNodeId (model: VM) : NodeId option =
        match model.selectedNodes with
        | None ->
            model.siteMap.entries
            |> Map.tryFind model.siteMap.rootId
            |> Option.map (fun entry -> entry.nodeId)
        | Some sel ->
            tryFocusedNodeId model.graph sel
            |> Option.orElse (
                focusedInstanceId sel
                |> Option.bind (fun instId -> Map.tryFind instId model.siteMap.entries)
                |> Option.map (fun entry -> entry.nodeId))

    let tryFindFocusedPath (graph: Graph) (sel: Selection) : (NodeId * string) option =
        let focusId = focusedNodeId graph sel

        NodeDesktopPath.pathForNodeId graph focusId
        |> Option.map (fun path -> focusId, path)

    let activeFileReference (model: VM) : (NodeId * FileReference) option =
        activeNodeId model
        |> Option.bind (fun nodeId ->
            NodeDesktopPath.fileReferenceForNodeId model.graph nodeId
            |> Option.map (fun fileRef -> nodeId, fileRef))

    let private indicatorMatches nodeId path =
        function
        | CheckingFileStatus (indicatorNodeId, indicatorPath)
        | FileStatusIndicator (indicatorNodeId, indicatorPath, _, _) ->
            indicatorNodeId = nodeId && indicatorPath = path
        | _ -> false

    let private refreshDesktopMappedFileIndicator nodeId path model =
        match model.desktopCapabilities with
        | None -> { model with desktopFileIndicator = BlankFileIndicator }, []
        | Some { file = { canStatus = false } } ->
            { model with desktopFileIndicator = BlankFileIndicator }, []
        | Some _ when indicatorMatches nodeId path model.desktopFileIndicator -> model, []
        | Some { file = { canStatus = true } } ->
            { model with desktopFileIndicator = CheckingFileStatus (nodeId, path) },
            [ RequestDesktopFileStatus (nodeId, path) ]

    let private refreshWorkspaceFileIndicator nodeId path model =
        match model.serverCapabilities with
        | Some { canFileStatus = true } ->
            if indicatorMatches nodeId path model.desktopFileIndicator then
                model, []
            else
                { model with desktopFileIndicator = CheckingFileStatus (nodeId, path) },
                [ RequestServerFileStatus (nodeId, path) ]
        | Some { canFileStatus = false } ->
            refreshDesktopMappedFileIndicator nodeId path model
        | None ->
            { model with desktopFileIndicator = BlankFileIndicator }, []

    let refreshDesktopFileIndicator (model: VM) : VM * Effect list =
        match activeFileReference model with
        | None
        | Some (_, NoFileReference) ->
            { model with desktopFileIndicator = BlankFileIndicator }, []
        | Some (_, InvalidFileReference) ->
            { model with desktopFileIndicator = InvalidFileReferenceIndicator }, []
        | Some (nodeId, FileReference path) ->
            if path.StartsWith(NodeDesktopPath.rootPrefix, System.StringComparison.Ordinal) then
                refreshWorkspaceFileIndicator nodeId path model
            else
                refreshDesktopMappedFileIndicator nodeId path model

    let applyDesktopFileStatus
        (nodeId: NodeId)
        (path: string)
        (status: DesktopFileStatus)
        (sourceModifiedUtc: System.DateTime option)
        (model: VM)
        : VM =
        match activeFileReference model with
        | Some (activeNodeId, FileReference activePath)
            when activeNodeId = nodeId && activePath = path ->
            { model with
                desktopFileIndicator =
                    FileStatusIndicator (nodeId, path, status, sourceModifiedUtc) }
        | _ -> model

    let desktopFileIndicatorText (model: VM) (entry: SiteEntry) (node: Node) : string =
        if not (isActiveEntry model entry) then ""
        else
            match model.desktopFileIndicator with
            | FileStatusIndicator (_, _, status, sourceModifiedUtc) ->
                FileSyncIndicator.indicatorTextForStatus node.updateTime status sourceModifiedUtc
            | other -> DesktopFileIndicator.toText other

    let private isSpecialArtifactNode (node: Node) : bool =
        match node.kind with
        | Special (Workspace | Directory | File) when node.id <> Graph.trashId -> true
        | _ -> false

    let private graphContainsArtifactPath (graph: Graph) (path: string) : bool =
        match NodeDesktopPath.canonicalDesktopPath path with
        | None -> false
        | Some canonicalPath ->
            graph.nodes
            |> Map.toSeq
            |> Seq.map snd
            |> Seq.filter (fun n ->
                match n.kind with
                | Special (Workspace | Directory | File) -> true
                | _ -> false)
            |> Seq.choose (fun n -> NodeDesktopPath.pathForNodeId graph n.id)
            |> Seq.choose NodeDesktopPath.canonicalDesktopPath
            |> Seq.exists ((=) canonicalPath)

    /// Graph-derived indicator when the row's artifact cannot resolve (same DU as desktop).
    let rowArtifactIndicatorState
        (model: VM)
        (_entry: SiteEntry)
        (node: Node)
        : DesktopFileIndicator option =
        if isSpecialArtifactNode node
           && Option.isNone (NodeDesktopPath.pathForNodeId model.graph node.id) then
            Some AbsentArtifactIndicator
        else
            match FileReference.parseFirst node.text with
            | InvalidFileReference -> Some InvalidFileReferenceIndicator
            | FileReference path
                when path.StartsWith(NodeDesktopPath.rootPrefix)
                     && not (graphContainsArtifactPath model.graph path) ->
                Some AbsentArtifactIndicator
            | _ -> None

    /// Outline row label: Special nodes prefer `text`, then `name`; canonical nodes keep `text`.
    let outlineDisplayText (node: Node) : string =
        if node.id = Graph.rootId || node.id = Graph.trashId || node.id = Graph.workspacesId then
            node.text
        else
            match node.kind with
            | Special (File | Directory | Workspace) ->
                if node.text <> "" then
                    node.text
                else
                    match node.name with
                    | Filename.Ok n -> n
                    | _ -> ""
            | _ -> node.text

    /// Right-hand row label from `Node.name` (Empty → blank).
    let rowNameDisplayText (name: Filename) : string =
        match name with
        | Filename.Ok s | Filename.Invalid s -> s
        | Filename.Empty -> ""

    let specialKindRowClass (nodeId: NodeId) (kind: NodeKind) : string option =
        if nodeId = Graph.trashId then
            Some "amb-row-special-trash"
        else
            match kind with
            | Normal -> None
            | Special Workspaces -> Some "amb-row-special-workspaces"
            | Special Workspace -> Some "amb-row-special-workspace"
            | Special Directory -> Some "amb-row-special-directory"
            | Special File -> Some "amb-row-special-file"

    let specialKindSymbol (nodeId: NodeId) (kind: NodeKind) : string option =
        if nodeId = Graph.trashId then
            Some "\u00D7"
        else
            match kind with
            | Normal -> None
            | Special Workspaces -> Some "\u229E"
            | Special Workspace -> Some "@"
            | Special Directory -> Some "\u25A4"
            | Special File -> Some "\u2261"

    let rowArtifactAbsentClassEligible (model: VM) (entry: SiteEntry) (node: Node) : bool =
        (isSpecialArtifactNode node
         && rowArtifactIndicatorState model entry node = Some AbsentArtifactIndicator)
        || (isActiveEntry model entry
            && match model.desktopFileIndicator with
               | FileStatusIndicator (_, _, MissingArtifact, _) -> true
               | _ -> false)

    let rowFileIndicatorText (model: VM) (entry: SiteEntry) (node: Node) : string =
        match rowArtifactIndicatorState model entry node with
        | Some state -> DesktopFileIndicator.toText state
        | None ->
            let desktop = desktopFileIndicatorText model entry node
            if desktop <> "" then desktop
            else specialKindSymbol node.id node.kind |> Option.defaultValue ""

    let private rowOwnership (model: VM) (entry: SiteEntry) : Ownership =
        entry.parentInstanceId
        |> Option.bind (fun parentId -> Map.tryFind parentId model.siteMap.entries)
        |> Option.bind (fun parent ->
            parent.children
            |> List.tryFindIndex ((=) entry.instanceId)
            |> Option.bind (fun index ->
                model.graph.nodes
                |> Map.tryFind parent.nodeId
                |> Option.bind (fun node -> List.tryItem index node.children)))
        |> Option.map (fun child -> child.ref)
        |> Option.defaultValue Ownership.Owner

    let rowOwnershipClass (model: VM) (entry: SiteEntry) : string =
        match rowOwnership model entry with
        | Ownership.Owner -> "amb-row-owned"
        | Ownership.Ref -> "amb-row-ref"

    let rowFileUnparsedClassEligible (model: VM) (entry: SiteEntry) : bool =
        rowOwnership model entry = Ownership.Owner
        && DocumentPartition.isMemberOfUnparsedFile model.graph entry.nodeId

    let addSpecialKindRowClass (nodeId: NodeId) (kind: NodeKind) (className: string) : string =
        match specialKindRowClass nodeId kind with
        | Some sk -> CssClass.add sk className
        | None -> className
