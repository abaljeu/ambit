namespace Gambol.Shared

module ViewModelMoveOps =

    open ViewModel

    type IndentPlan =
        { model: VM
          target: NodeRange
          parentInstanceId: SiteId
          insertIdx: int
          count: int
          focusOffset: int }

    type OutdentAfterMove =
        | ReconcileCurrentZoom
        | ZoomOutToGrandparent of grandparentId: NodeId * parentIdx: int * count: int * focusOffset: int

    type OutdentPlan =
        { model: VM
          target: NodeRange
          afterMove: OutdentAfterMove }

    let private refreshedSelection model =
        model.selectedNodes
        |> Option.bind (refreshSelection model.graph model.siteMap)
        |> Option.map (fun sel -> { model with selectedNodes = Some sel }, sel)

    let planIndentSelection (model: VM) : IndentPlan option =
        refreshedSelection model
        |> Option.bind (fun (model, sel) ->
            if sel.range.start = 0 then
                None
            else
                let prevInstId = sel.range.parent.children.[sel.range.start - 1]

                Map.tryFind prevInstId model.siteMap.entries
                |> Option.map (fun prevEntry ->
                    let siteMap, nextId =
                        if prevEntry.expanded then
                            model.siteMap, model.nextSiteId
                        else
                            expandEntry prevInstId model.graph model.siteMap model.nextSiteId

                    let prevSibId = prevEntry.nodeId
                    let insertIdx = model.graph.nodes.[prevSibId].children.Length
                    let target =
                        { pnode = prevSibId
                          start = max 0 (insertIdx - 1)
                          endd = insertIdx }

                    { model = { model with siteMap = siteMap; nextSiteId = nextId }
                      target = target
                      parentInstanceId = prevInstId
                      insertIdx = insertIdx
                      count = sel.range.endd - sel.range.start
                      focusOffset = sel.focus - sel.range.start }))

    let selectionAfterIndent (plan: IndentPlan) (siteMap: SiteMap) : Selection option =
        Map.tryFind plan.parentInstanceId siteMap.entries
        |> Option.map (fun parent ->
            let focusOffset = min (max 0 plan.focusOffset) (max 0 (plan.count - 1))

            { range =
                { parent = parent
                  start = plan.insertIdx
                  endd = plan.insertIdx + plan.count }
              focus = plan.insertIdx + focusOffset })

    let private planOutdentWithinSiteMap model sel parentInstId =
        Map.tryFind parentInstId model.siteMap.entries
        |> Option.bind (fun grandparent ->
            grandparent.children
            |> List.tryFindIndex ((=) sel.range.parent.instanceId)
            |> Option.map (fun parentIdx ->
                let target =
                    { pnode = grandparent.nodeId
                      start = parentIdx
                      endd = parentIdx + 1 }

                { model = model
                  target = target
                  afterMove = ReconcileCurrentZoom }))

    let private planOutdentFromRoot model sel =
        Graph.tryFindParentAndIndex sel.range.parent.nodeId model.graph
        |> Option.map (fun (grandparentId, parentIdx) ->
            let count = sel.range.endd - sel.range.start
            let focusOffset = sel.focus - sel.range.start
            let target =
                { pnode = grandparentId
                  start = parentIdx
                  endd = parentIdx + 1 }

            { model = model
              target = target
              afterMove =
                ZoomOutToGrandparent(grandparentId, parentIdx, count, focusOffset) })

    let planOutdentSelection (model: VM) : OutdentPlan option =
        refreshedSelection model
        |> Option.bind (fun (model, sel) ->
            match sel.range.parent.parentInstanceId with
            | Some parentInstId -> planOutdentWithinSiteMap model sel parentInstId
            | None -> planOutdentFromRoot model sel)
