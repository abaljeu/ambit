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

        selectedChildren
        |> List.choose (fun (index, child) ->
            if child.id = Graph.trashId then
                None
            else
                let nodeId = child.id
                let allOccs = getAllOccurrences graph nodeId
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
                        // Deleting a non-owner occurrence inside the span (should not happen),
                        // treat as a local ref delete to be safe.
                        DeleteAction.LocalDeleteRefOnly
                    | Ownership.Owner, true, true, [] ->
                        // Unique owner is under TRASH and no refs outside → hard delete subtree.
                        DeleteAction.HardDeleteSubtreeInTrash
                    | Ownership.Owner, true, true, _::_ ->
                        // Owner under TRASH but there are refs elsewhere – selection in TRASH
                        // should only remove TRASH occurrences; external refs remain.
                        DeleteAction.LocalDeleteRefOnly
                    | Ownership.Owner, true, false, [] ->
                        // Last non-TRASH owner occurrence → move whole subtree to TRASH.
                        DeleteAction.MoveToTrash
                    | Ownership.Owner, true, false, _::_ ->
                        // Owner not under TRASH and there are other occurrences → promote a ref.
                        DeleteAction.LocalDeleteWithPromotion

                Some
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
