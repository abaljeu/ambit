namespace Gambol.Shared

module ViewModelDeleteOps =

    open ViewModel

    /// Classification for how a particular occurrence of a node should be deleted.
    type DeleteAction =
        | MoveToTrash
        | HardDeleteSubtreeInTrash
        | LocalDeleteWithPromotion
        | LocalDeleteRefOnly

    /// A single classified delete decision for an occurrence within a selection.
    type ClassifiedDelete =
        { parentId: NodeId
          index: int
          child: ChildNode
          otherOccurrences: (NodeId * int * ChildNode) list
          action: DeleteAction }

    /// Owned system folders and Workspace Nodes are not deleted. A Ref to them is.
    let private isBlockedOwnedDelete
        (graph: Graph)
        (parentId: NodeId)
        (child: ChildNode)
        =
        let isProtectedTarget =
            Graph.isSystemFolderNode child.id
            || match graph.nodes.[child.id].kind with
               | Special Workspace -> true
               | _ -> false
        isProtectedTarget
        && Node.childOwnership graph parentId child = Ownership.Owner

    /// Classify each child in the selected span under the same parent into a delete action.
    let classifyDeleteForSelection
        (graph: Graph)
        (range: SiteNodeRange)
        : ClassifiedDelete list
        =
        let parentId = range.parent.nodeId
        let parentNode = graph.nodes.[parentId]
        let selectedChildren =
            parentNode.children
            |> List.mapi (fun i child -> i, child)
            |> List.filter (fun (i, _) -> i >= range.start && i < range.endd)

        if selectedChildren |> List.exists (fun (_, c) ->
            isBlockedOwnedDelete graph parentId c) then []
        else

        selectedChildren
        |> List.map (fun (index, child) ->
            let nodeId = child.id
            let ownerOcc = getOwnerOccurrence graph nodeId
            let isOwnerHere =
                let (ownerParentId, ownerIndex, _) = ownerOcc
                ownerParentId = parentId && ownerIndex = index
            let ownerUnderTrash = isOwnerUnderTrash graph nodeId
            let others = occurrencesOutsideSelection graph range nodeId

            let action =
                match
                    Node.childOwnership graph parentId child,
                    isOwnerHere,
                    ownerUnderTrash,
                    others
                with
                | Ownership.Ref, _, _, _ ->
                    DeleteAction.LocalDeleteRefOnly
                | Ownership.Owner, false, _, _ ->
                    // Non-owner occurrence inside span (should not happen); treat as ref-only.
                    DeleteAction.LocalDeleteRefOnly
                | Ownership.Owner, true, true, [] ->
                    // Unique owner is under TRASH, no refs outside → hard delete subtree.
                    DeleteAction.HardDeleteSubtreeInTrash
                | Ownership.Owner, true, true, _::_ ->
                    // Owner under TRASH, refs elsewhere → local ref-only removal.
                    DeleteAction.LocalDeleteRefOnly
                | Ownership.Owner, true, false, [] ->
                    // Last non-TRASH owner → move subtree to TRASH.
                    DeleteAction.MoveToTrash
                | Ownership.Owner, true, false, _::_ ->
                    // Owner with other occurrences outside → promote a ref first.
                    DeleteAction.LocalDeleteWithPromotion

            { parentId = parentId
              index = index
              child = child
              otherOccurrences = others
              action = action })

    /// Plan hard-delete for an entire subtree whose owner is under TRASH and has no refs outside.
    /// Returns, for each parent, the sorted list of child indices to remove.
    let hardDeleteSubtreePlan (graph: Graph) (rootNodeId: NodeId) : Map<NodeId, int list> =
        let rec collectSubtree (pending: NodeId list) (visited: Set<NodeId>) : Set<NodeId> =
            match pending with
            | [] -> visited
            | nid :: rest ->
                if Set.contains nid visited then
                    collectSubtree rest visited
                else
                    let children =
                        graph.nodes
                        |> Map.tryFind nid
                        |> Option.map (fun n -> n.children |> List.map (fun c -> c.id))
                        |> Option.defaultValue []
                    collectSubtree (children @ rest) (Set.add nid visited)

        let subtreeNodes = collectSubtree [ rootNodeId ] Set.empty

        let parentToIndices =
            subtreeNodes
            |> Seq.collect (fun nid ->
                getAllOccurrences graph nid
                |> Seq.map (fun (parentId, index, _) -> parentId, index))
            |> Seq.fold
                (fun acc (parentId, index) ->
                    let existing = acc |> Map.tryFind parentId |> Option.defaultValue []
                    acc |> Map.add parentId (index :: existing))
                Map.empty

        parentToIndices
        |> Map.map (fun _ indices ->
            indices
            |> List.distinct
            |> List.sort)

    // -----------------------------------------------------------------------
    // Op planning
    // -----------------------------------------------------------------------

    /// Build promote ops: one single-item Replace per LocalDeleteWithPromotion item.
    /// Promote ops must run before the span remove (they target outside the selection slice).
    let private buildPromoteOps
        (graph: Graph)
        (classified: ClassifiedDelete list)
        : Op list
        =
        classified
        |> List.choose (fun item ->
            match item.action with
            | LocalDeleteWithPromotion ->
                item.otherOccurrences
                |> List.tryFind (fun (promoParentId, _, c) ->
                    Node.childOwnership graph promoParentId c = Ownership.Ref)
                |> Option.map (fun (promoParentId, promoIdx, oldChild) ->
                    let newChild = { oldChild with ref = Ownership.Owner }
                    let oldChildren = graph.nodes.[promoParentId].children
                    ChildListWire.updateChildAt promoParentId oldChildren promoIdx newChild)
            | _ -> None)

    /// Rename MoveToTrash items that would collide under TRASH (after span remove).
    let private buildTrashRenameOps
        (graph: Graph)
        (classified: ClassifiedDelete list)
        : Op list
        =
        classified
        |> List.fold
            (fun (ops, reserved) item ->
                match item.action with
                | MoveToTrash ->
                    match Map.tryFind item.child.id graph.nodes with
                    | Some node ->
                        match Filename.tryValue node.name with
                        | None -> ops, reserved
                        | Some baseName ->
                            let newName =
                                GraphQuery.unusedOwnedName
                                    graph Graph.trashId baseName reserved
                            let reserved' =
                                Set.add (newName.ToLowerInvariant()) reserved
                            if newName = baseName then
                                ops, reserved'
                            else
                                Op.SetName(item.child.id, baseName, newName) :: ops,
                                reserved'
                    | None -> ops, reserved
                | _ -> ops, reserved)
            ([], Set.empty)
        |> fst
        |> List.rev

    /// Build TRASH append op for all MoveToTrash items (one op, appended at end).
    let private buildTrashOp
        (graph: Graph)
        (classified: ClassifiedDelete list)
        : Op list
        =
        let newOwners =
            classified
            |> List.choose (fun item ->
                match item.action with
                | MoveToTrash -> Some (ChildNode.owner item.child.id)
                | _ -> None)
        match newOwners with
        | [] -> []
        | owners ->
            let oldChildren = graph.nodes.[Graph.trashId].children
            [ ChildListWire.append Graph.trashId oldChildren owners ]

    /// Build hard-delete ops for HardDeleteSubtreeInTrash items.
    /// selectionParentId is excluded from the plan because spanRemove already removes those entries.
    let private buildHardDeleteOps
        (graph: Graph)
        (selectionParentId: NodeId)
        (classified: ClassifiedDelete list)
        : Op list
        =
        classified
        |> List.choose (fun item ->
            match item.action with
            | HardDeleteSubtreeInTrash -> Some item.child.id
            | _ -> None)
        |> List.distinct
        |> List.collect (fun rootId ->
            hardDeleteSubtreePlan graph rootId
            |> Map.toList
            |> List.filter (fun (pid, _) -> pid <> selectionParentId)
            |> List.map (fun (pid, indices) ->
                let children = graph.nodes.[pid].children
                ChildListWire.removeIndices pid children (Set.ofList indices)))

    /// Build the complete ordered op list for a classified delete gesture.
    /// Precondition: classified is non-empty (caller checks classifyDeleteForSelection <> []).
    /// Op ordering: promote → span remove → trash renames → TRASH append → hard-delete.
    let planDeleteOps
        (graph: Graph)
        (range: SiteNodeRange)
        (classified: ClassifiedDelete list)
        : Op list
        =
        let parentId = range.parent.nodeId
        let parentChildren = graph.nodes.[parentId].children
        let promoteOps = buildPromoteOps graph classified
        let spanRemove =
            ChildListWire.removeRange
                parentId
                parentChildren
                range.start
                (range.endd - range.start)
        let trashRenames = buildTrashRenameOps graph classified
        let trashOps = buildTrashOp graph classified
        let hardDeleteOps = buildHardDeleteOps graph parentId classified
        promoteOps @ [ spanRemove ] @ trashRenames @ trashOps @ hardDeleteOps

    let private occurrencesOutsideIndices
        (graph: Graph)
        (parentId: NodeId)
        (indices: Set<int>)
        (nodeId: NodeId)
        =
        getAllOccurrences graph nodeId
        |> List.filter (fun (pid, index, _) ->
            if pid <> parentId then true
            else not (Set.contains index indices))

    /// Classify Delete actions for specific child indices under one parent.
    let private classifyDeleteAtIndices
        (graph: Graph)
        (parentId: NodeId)
        (indices: int list)
        : ClassifiedDelete list
        =
        let parentNode = graph.nodes.[parentId]
        let selected =
            indices
            |> List.choose (fun index ->
                parentNode.children
                |> List.tryItem index
                |> Option.map (fun child -> index, child))

        if selected |> List.exists (fun (_, c) ->
            isBlockedOwnedDelete graph parentId c) then
            []
        else
            let indexSet = Set.ofList indices

            selected
            |> List.map (fun (index, child) ->
                let nodeId = child.id
                let ownerParentId, ownerIndex, _ = getOwnerOccurrence graph nodeId
                let isOwnerHere =
                    ownerParentId = parentId && ownerIndex = index
                let ownerUnderTrash = isOwnerUnderTrash graph nodeId
                let others =
                    occurrencesOutsideIndices graph parentId indexSet nodeId

                let action =
                    match
                        Node.childOwnership graph parentId child,
                        isOwnerHere,
                        ownerUnderTrash,
                        others
                    with
                    | Ownership.Ref, _, _, _ ->
                        DeleteAction.LocalDeleteRefOnly
                    | Ownership.Owner, false, _, _ ->
                        DeleteAction.LocalDeleteRefOnly
                    | Ownership.Owner, true, true, [] ->
                        DeleteAction.HardDeleteSubtreeInTrash
                    | Ownership.Owner, true, true, _ :: _ ->
                        DeleteAction.LocalDeleteRefOnly
                    | Ownership.Owner, true, false, [] ->
                        DeleteAction.MoveToTrash
                    | Ownership.Owner, true, false, _ :: _ ->
                        DeleteAction.LocalDeleteWithPromotion

                { parentId = parentId
                  index = index
                  child = child
                  otherOccurrences = others
                  action = action })

    let private buildHardDeleteOpsExcluding
        (graph: Graph)
        (excludedParents: Set<NodeId>)
        (classified: ClassifiedDelete list)
        : Op list
        =
        classified
        |> List.choose (fun item ->
            match item.action with
            | HardDeleteSubtreeInTrash -> Some item.child.id
            | _ -> None)
        |> List.distinct
        |> List.collect (fun rootId ->
            hardDeleteSubtreePlan graph rootId
            |> Map.toList
            |> List.filter (fun (pid, _) -> not (Set.contains pid excludedParents))
            |> List.map (fun (pid, indices) ->
                let children = graph.nodes.[pid].children
                ChildListWire.removeIndices pid children (Set.ofList indices)))

    /// Delete-command disposal for owned children dropped under each parent
    /// (warm parse unmatched Owner rows). One shared TRASH append.
    let planDeleteDroppedOwnedMany
        (graph: Graph)
        (dropByParent: Map<NodeId, Set<NodeId>>)
        : Op list
        =
        let classified =
            dropByParent
            |> Map.toList
            |> List.collect (fun (parentId, dropIds) ->
                if Set.isEmpty dropIds then
                    []
                else
                    match Map.tryFind parentId graph.nodes with
                    | None -> []
                    | Some parent ->
                        let indices =
                            parent.children
                            |> List.mapi (fun i c -> i, c)
                            |> List.choose (fun (i, c) ->
                                if
                                    Node.childOwnership graph parentId c
                                       = Ownership.Owner
                                    && Set.contains c.id dropIds
                                then
                                    Some i
                                else
                                    None)

                        if indices.IsEmpty then
                            []
                        else
                            classifyDeleteAtIndices graph parentId indices)

        match classified with
        | [] -> []
        | _ ->
            let promoteOps = buildPromoteOps graph classified
            let removeOps =
                classified
                |> List.groupBy (fun item -> item.parentId)
                |> List.map (fun (parentId, items) ->
                    let children = graph.nodes.[parentId].children
                    let indicesToRemove =
                        items |> List.map (fun item -> item.index) |> Set.ofList
                    ChildListWire.removeIndices parentId children indicesToRemove)
            let trashRenames = buildTrashRenameOps graph classified
            let trashOps = buildTrashOp graph classified
            let excluded =
                classified |> List.map (fun c -> c.parentId) |> Set.ofList
            let hardDeleteOps =
                buildHardDeleteOpsExcluding graph excluded classified
            promoteOps @ removeOps @ trashRenames @ trashOps @ hardDeleteOps
