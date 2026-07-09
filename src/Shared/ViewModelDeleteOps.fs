namespace Gambol.Shared

module ViewModelDeleteOps =

    open ViewModel

    /// Classification for how a particular occurrence of a node should be deleted.
    type DeleteAction =
        | MoveToTrash
        | HardDeleteSubtreeInTrash
        | LocalDeleteWithPromotion
        | LocalDeleteRefOnly
        | OwnedSpecialDeleteToTrash
        | OwnedSpecialDeleteHard

    let private isOwnedSpecialFileOrDir (graph: Graph) (nodeId: NodeId) : bool =
        match graph.nodes.[nodeId].kind with
        | Special (File | Directory) -> true
        | _ -> false

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

        // All-or-nothing: system TRASH and workspace roots are not deleted in Stage 6.
        let isBlockedDeleteChild (child: ChildNode) =
            child.id = Graph.trashId
            || (match graph.nodes.[child.id].kind with
                | Special Workspace -> true
                | _ -> false)

        if selectedChildren |> List.exists (fun (_, c) -> isBlockedDeleteChild c) then []
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
                let isOwnedSpecial = isOwnedSpecialFileOrDir graph nodeId

                match child.ref, isOwnerHere, ownerUnderTrash, others with
                | Ownership.Ref, _, _, _ ->
                    DeleteAction.LocalDeleteRefOnly
                | Ownership.Owner, false, _, _ ->
                    DeleteAction.LocalDeleteRefOnly
                | Ownership.Owner, true, true, [] ->
                    DeleteAction.HardDeleteSubtreeInTrash
                | Ownership.Owner, true, true, _::_ when isOwnedSpecial ->
                    DeleteAction.OwnedSpecialDeleteHard
                | Ownership.Owner, true, true, _::_ ->
                    DeleteAction.LocalDeleteRefOnly
                | Ownership.Owner, true, false, [] ->
                    DeleteAction.MoveToTrash
                | Ownership.Owner, true, false, _::_ when isOwnedSpecial ->
                    DeleteAction.OwnedSpecialDeleteToTrash
                | Ownership.Owner, true, false, _::_ ->
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

    let private pathExprText (graph: Graph) (nodeId: NodeId) : string option =
        NodeDesktopPath.pathForNodeId graph nodeId
        |> Option.map (fun path -> "[[" + path + "]]")

    let private buildPathExprReplacementOps
        (graph: Graph)
        (classified: ClassifiedDelete list)
        : Op list
        =
        classified
        |> List.choose (fun item ->
            match item.action with
            | OwnedSpecialDeleteToTrash | OwnedSpecialDeleteHard -> Some item
            | _ -> None)
        |> List.collect (fun item ->
            match pathExprText graph item.child.id with
            | None -> []
            | Some pathText ->
                item.otherOccurrences
                |> List.filter (fun (_, _, c) -> c.ref = Ownership.Ref)
                |> List.collect (fun (parentId, index, oldChild) ->
                    let newId = NodeId.New()
                    [ Op.NewNode(newId, pathText)
                      Op.Replace(parentId, index, [ oldChild ], [ { ref = Ownership.Owner; id = newId } ]) ]))

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
                | MoveToTrash | OwnedSpecialDeleteToTrash ->
                    Some { ref = Ownership.Owner; id = item.child.id }
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
            | HardDeleteSubtreeInTrash | OwnedSpecialDeleteHard -> Some item.child.id
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
        let pathExprOps = buildPathExprReplacementOps graph classified
        let promoteOps = buildPromoteOps graph classified
        let spanRemove = Op.Replace(parentId, range.start, selectedChildren, [])
        let trashOps = buildTrashOp graph classified
        let hardDeleteOps = buildHardDeleteOps graph parentId classified
        pathExprOps @ promoteOps @ [ spanRemove ] @ trashOps @ hardDeleteOps
