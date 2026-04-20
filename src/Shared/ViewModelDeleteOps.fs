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

        // All-or-nothing: if any selected child is TRASH, cancel the entire delete.
        // Behavioral note: previously TRASH was silently skipped; now it cancels all siblings too.
        let anyTrash = selectedChildren |> List.exists (fun (_, c) -> c.id = Graph.trashId)
        if anyTrash then []
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
                match child.ref, isOwnerHere, ownerUnderTrash, others with
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
                |> List.tryFind (fun (_, _, c) -> c.ref = Ownership.Ref)
                |> Option.map (fun (promoParentId, promoIdx, oldChild) ->
                    let newChild = { oldChild with ref = Ownership.Owner }
                    Op.Replace(promoParentId, promoIdx, [ oldChild ], [ newChild ]))
            | _ -> None)

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
                | MoveToTrash -> Some { ref = Ownership.Owner; id = item.child.id }
                | _ -> None)
        match newOwners with
        | [] -> []
        | owners ->
            let trashLen = graph.nodes.[Graph.trashId].children.Length
            [ Op.Replace(Graph.trashId, trashLen, [], owners) ]

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
                let indicesSet = Set.ofList indices
                let remaining =
                    children
                    |> List.mapi (fun i c -> i, c)
                    |> List.filter (fun (i, _) -> not (Set.contains i indicesSet))
                    |> List.map snd
                Op.Replace(pid, 0, children, remaining)))

    /// Build the complete ordered op list for a classified delete gesture.
    /// Precondition: classified is non-empty (caller checks classifyDeleteForSelection <> []).
    /// Op ordering: promote ops → span remove → TRASH append → hard-delete subtree ops.
    let planDeleteOps
        (graph: Graph)
        (range: SiteNodeRange)
        (classified: ClassifiedDelete list)
        : Op list
        =
        let parentId = range.parent.nodeId
        let parentChildren = graph.nodes.[parentId].children
        let selectedChildren =
            parentChildren |> List.skip range.start |> List.take (range.endd - range.start)
        let promoteOps = buildPromoteOps graph classified
        let spanRemove = Op.Replace(parentId, range.start, selectedChildren, [])
        let trashOps = buildTrashOp graph classified
        let hardDeleteOps = buildHardDeleteOps graph parentId classified
        promoteOps @ [ spanRemove ] @ trashOps @ hardDeleteOps
