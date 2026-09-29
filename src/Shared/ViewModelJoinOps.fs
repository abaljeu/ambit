namespace Gambol.Shared

module ViewModelJoinOps =

    open ViewModel

    type JoinEditPlan =
        | Apply of ops: Op list * text: string * caret: EditCaret * focusInstanceId: SiteId
        | RestoreCaret

    type VisibleNode = {
        instanceId: SiteId
        nodeId: NodeId
        node: Node
    }

    type ParentSlot = {
        parentId: NodeId
        indexInParent: int
    }

    /// Previous sibling plus the current row's place. Shared by the
    /// join-into-previous helpers. Focus, current node, and Graph stay on `VM`.
    type PreviousJoin = {
        currentText: string
        previous: VisibleNode
        slot: ParentSlot
    }

    let private tryVisibleNeighbor offset model sel =
        focusedInstanceId sel
        |> Option.bind (fun focusInstId ->
            let rows = getVisibleRowInstanceIds model.siteMap

            rows
            |> List.tryFindIndex ((=) focusInstId)
            |> Option.bind (fun currentIndex ->
                let neighborIndex = currentIndex + offset

                if neighborIndex < 0 || neighborIndex >= rows.Length then
                    None
                else
                    let instanceId = rows.[neighborIndex]
                    let entry = model.siteMap.entries.[instanceId]
                    let nodeId = entry.nodeId
                    Some
                        { instanceId = instanceId
                          nodeId = nodeId
                          node = model.graph.nodes.[nodeId] }))

    let private removeCurrentChildOp (g: Graph) (currentId: NodeId) (parentId: NodeId) (indexInParent: int) =
        let oldChildren = GraphChildren.get g parentId
        ChildListWire.removeRange parentId oldChildren indexInParent 1

    let private isBlankLeaf (node: Node) (children: ChildNode list) =
        children.IsEmpty
        && node.kind = Normal
        && System.String.IsNullOrWhiteSpace node.text

    let private immediatePrevIndex
        (graph: Graph)
        (prevId: NodeId)
        (parentId: NodeId)
        (indexInParent: int)
        : int option =
        match Graph.tryFindParentAndIndex prevId graph with
        | Some (pp, pi) when pp = parentId && pi + 1 = indexInParent ->
            Some pi
        | _ -> None

    let private focusedSelection (model: VM) : Selection option =
        match model.mode, model.selectedNodes with
        | Editing _, sel -> sel
        | _ -> None

    let private tryDiscardBlankPrevious (model: VM) (join: PreviousJoin) : JoinEditPlan option =
        let graph = model.graph
        let prevId = join.previous.nodeId
        let parentId = join.slot.parentId
        let indexInParent = join.slot.indexInParent
        let prevChildren = GraphChildren.get graph prevId
        match immediatePrevIndex graph prevId parentId indexInParent with
        | Some prevIndex when isBlankLeaf join.previous.node prevChildren ->
            let currentInstId =
                focusedSelection model
                |> Option.bind focusedInstanceId
                |> Option.defaultValue join.previous.instanceId
            Some
                (Apply
                    ([ removeCurrentChildOp graph prevId parentId prevIndex ],
                     join.currentText,
                     EditCaret.Utf16Index 0,
                     currentInstId))
        | _ -> None

    let private joinIntoPreviousOps (model: VM) (join: PreviousJoin) : Op list =
        match focusedSelection model with
        | None -> []
        | Some sel ->
            let graph = model.graph
            let prevId = join.previous.nodeId
            let prevNode = join.previous.node
            let currentId = focusedNodeId graph sel
            let joinedText = prevNode.text + join.currentText
            let prevChildren = GraphChildren.get graph prevId
            let currentChildren = GraphChildren.get graph currentId
            let parentId = join.slot.parentId
            let indexInParent = join.slot.indexInParent
            [ if joinedText <> prevNode.text then
                  yield Op.SetText(prevId, prevNode.text, joinedText)
              if not currentChildren.IsEmpty then
                  yield ChildListWire.append prevId prevChildren currentChildren
                  yield ChildListWire.replace currentId currentChildren []
              yield removeCurrentChildOp graph currentId parentId indexInParent ]

    let joinWithNextPlan (currentText: string) (model: VM) : JoinEditPlan option =
        match model.mode, model.selectedNodes with
        | Editing _, Some sel ->
            let currentId = focusedNodeId model.graph sel
            let currentNode = model.graph.nodes.[currentId]

            match tryVisibleNeighbor 1 model sel with
            | None -> None
            | Some _ when
                not (GraphChildren.get model.graph currentId).IsEmpty ->
                Some RestoreCaret
            | Some next ->
                let nextInstId = next.instanceId
                let nextId = next.nodeId
                let nextNode = next.node
                match Graph.tryFindParentAndIndex nextId model.graph,
                      Graph.tryFindParentAndIndex currentId model.graph with
                | Some _, Some (currParentId, currIndexInParent) ->
                    let removeCurrent =
                        removeCurrentChildOp model.graph currentId currParentId currIndexInParent

                    if System.String.IsNullOrWhiteSpace currentText then
                        Some
                            (Apply
                                ([ removeCurrent ],
                                 nextNode.text,
                                 EditCaret.Utf16Index 0,
                                 nextInstId))
                    else
                        let joinedText = currentText + nextNode.text
                        let ops =
                            [ if joinedText <> nextNode.text then
                                  yield Op.SetText(nextId, nextNode.text, joinedText)
                              yield removeCurrent ]

                        Some
                            (Apply
                                (ops,
                                 joinedText,
                                 EditCaret.Utf16Index currentText.Length,
                                 nextInstId))
                | _ -> None
        | _ -> None

    let private previousJoinPlan (model: VM) (join: PreviousJoin) : JoinEditPlan =
        match tryDiscardBlankPrevious model join with
        | Some plan -> plan
        | None ->
            match focusedSelection model with
            | Some sel when
                NodeKind.artifact (model.graph.nodes.[focusedNodeId model.graph sel].kind) ->
                RestoreCaret
            | Some _ ->
                let prevNode = join.previous.node
                Apply(
                    joinIntoPreviousOps model join,
                    prevNode.text + join.currentText,
                    EditCaret.Utf16Index prevNode.text.Length,
                    join.previous.instanceId)
            | None -> RestoreCaret

    let joinWithPreviousPlan (currentText: string) (model: VM) : JoinEditPlan option =
        match model.mode, model.selectedNodes with
        | Editing _, Some sel ->
            let currentId = focusedNodeId model.graph sel
            match tryVisibleNeighbor -1 model sel,
                  Graph.tryFindParentAndIndex currentId model.graph with
            | Some previous, Some (parentId, index)
                when (GraphChildren.get model.graph currentId).IsEmpty
                     || (GraphChildren.get model.graph previous.nodeId).IsEmpty ->
                Some (
                    previousJoinPlan
                        model
                        { currentText = currentText
                          previous = previous
                          slot =
                            { parentId = parentId
                              indexInParent = index } })
            | _ -> None
        | _ -> None
