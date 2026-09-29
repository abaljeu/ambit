namespace Gambol.Shared

module ViewModelJoinOps =

    open ViewModel

    type JoinEditPlan =
        | Apply of ops: Op list * text: string * caret: EditCaret * focusInstanceId: SiteId
        | RestoreCaret

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
                    Some (instanceId, nodeId, model.graph.nodes.[nodeId])))

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

    let private tryDiscardBlankPrevious
        (graph: Graph)
        (sel: Selection)
        (currentText: string)
        (prevInstId: SiteId)
        (prevId: NodeId)
        (prevNode: Node)
        (parentId: NodeId)
        (indexInParent: int)
        : JoinEditPlan option =
        let prevChildren = GraphChildren.get graph prevId
        match immediatePrevIndex graph prevId parentId indexInParent with
        | Some prevIndex when isBlankLeaf prevNode prevChildren ->
            let currentInstId =
                focusedInstanceId sel |> Option.defaultValue prevInstId
            Some
                (Apply
                    ([ removeCurrentChildOp graph prevId parentId prevIndex ],
                     currentText,
                     EditCaret.Utf16Index 0,
                     currentInstId))
        | _ -> None

    let private joinIntoPreviousOps
        (graph: Graph)
        (prevId: NodeId)
        (prevNode: Node)
        (currentId: NodeId)
        (currentText: string)
        (parentId: NodeId)
        (indexInParent: int)
        : Op list =
        let joinedText = prevNode.text + currentText
        let prevChildren = GraphChildren.get graph prevId
        let currentChildren = GraphChildren.get graph currentId
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
            | Some (nextInstId, nextId, nextNode) ->
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

    let private previousJoinPlan
        (currentText: string)
        (model: VM)
        (sel: Selection)
        (currentId: NodeId)
        (currentNode: Node)
        (prevInstId: SiteId)
        (prevId: NodeId)
        (prevNode: Node)
        (parentId: NodeId)
        (indexInParent: int)
        : JoinEditPlan =
        match
            tryDiscardBlankPrevious
                model.graph
                sel
                currentText
                prevInstId
                prevId
                prevNode
                parentId
                indexInParent
        with
        | Some plan -> plan
        | None when NodeKind.artifact currentNode.kind -> RestoreCaret
        | None ->
            Apply(
                joinIntoPreviousOps
                    model.graph
                    prevId
                    prevNode
                    currentId
                    currentText
                    parentId
                    indexInParent,
                prevNode.text + currentText,
                EditCaret.Utf16Index prevNode.text.Length,
                prevInstId)

    let joinWithPreviousPlan (currentText: string) (model: VM) : JoinEditPlan option =
        match model.mode, model.selectedNodes with
        | Editing _, Some sel ->
            let currentId = focusedNodeId model.graph sel
            let currentNode = model.graph.nodes.[currentId]
            match tryVisibleNeighbor -1 model sel,
                  Graph.tryFindParentAndIndex currentId model.graph with
            | Some (prev, prevId, prevNode), Some (parentId, index)
                when (GraphChildren.get model.graph currentId).IsEmpty
                     || (GraphChildren.get model.graph prevId).IsEmpty ->
                Some (
                    previousJoinPlan
                        currentText model sel currentId currentNode
                        prev prevId prevNode parentId index)
            | _ -> None
        | _ -> None
