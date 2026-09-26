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

    let joinWithPreviousPlan (currentText: string) (model: VM) : JoinEditPlan option =
        match model.mode, model.selectedNodes with
        | Editing _, Some sel ->
            let currentId = focusedNodeId model.graph sel
            let currentNode = model.graph.nodes.[currentId]

            match tryVisibleNeighbor -1 model sel,
                  Graph.tryFindParentAndIndex currentId model.graph with
            | Some (prevInstId, prevId, prevNode), Some (parentId, indexInParent)
                when (GraphChildren.get model.graph currentId).IsEmpty
                     || (GraphChildren.get model.graph prevId).IsEmpty ->
                let joinedText = prevNode.text + currentText
                let prevChildren = GraphChildren.get model.graph prevId
                let currentChildren = GraphChildren.get model.graph currentId
                let ops =
                    [ if joinedText <> prevNode.text then
                          yield Op.SetText(prevId, prevNode.text, joinedText)
                      if not currentChildren.IsEmpty then
                          yield
                              ChildListWire.append prevId prevChildren currentChildren
                      yield removeCurrentChildOp model.graph currentId parentId indexInParent ]

                Some (Apply (ops, joinedText, EditCaret.Utf16Index prevNode.text.Length, prevInstId))
            | _ -> None
        | _ -> None
