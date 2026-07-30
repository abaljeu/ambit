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

    /// Shown in `#cmd-last-result` when a move/indent target is illegal.
    let invalidMoveTargetMessage = "target is not a valid location"

    let withMoveError (message: string) (model: VM) : VM =
        { model with
            lastCmdResult = Some(CmdLastResult.Error (None, message)) }

    let withInvalidMoveTarget (model: VM) : VM =
        withMoveError invalidMoveTargetMessage model

    let private findSiteEntryByNodeId (siteMap: SiteMap) (nodeId: NodeId) =
        siteMap.entries
        |> Map.tryPick (fun _ e -> if e.nodeId = nodeId then Some e else None)

    let private closestIndexedEntry
        (indexed: (int * SiteEntry) list)
        (preferIdx: int)
        : SiteEntry option =
        match indexed with
        | [] -> None
        | _ ->
            indexed
            |> List.minBy (fun (i, _) -> abs (i - preferIdx))
            |> snd
            |> Some

    let private matchingChildEntries
        (siteMap: SiteMap)
        (parent: SiteEntry)
        (nodeId: NodeId)
        : (int * SiteEntry) list =
        parent.children
        |> List.indexed
        |> List.choose (fun (i, cid) ->
            match Map.tryFind cid siteMap.entries with
            | Some e when e.nodeId = nodeId -> Some (i, e)
            | _ -> None)

    let private findAncestorEntry
        (siteMap: SiteMap)
        (startParentInstId: SiteId option)
        (nodeId: NodeId)
        : SiteEntry option =
        let rec go instIdOpt =
            match instIdOpt with
            | None -> None
            | Some iid ->
                match Map.tryFind iid siteMap.entries with
                | Some e when e.nodeId = nodeId -> Some e
                | Some e -> go e.parentInstanceId
                | None -> None

        go startParentInstId

    /// Prefer the destination SiteEntry near `fromRange` when NodeId appears
    /// more than once (indent under prev sibling, outdent to ancestor, etc.).
    let private findDestParentEntry
        (siteMap: SiteMap)
        (destParentNodeId: NodeId)
        (fromRange: SiteNodeRange)
        : SiteEntry option =
        let fromParent = fromRange.parent

        if fromParent.nodeId = destParentNodeId then
            Map.tryFind fromParent.instanceId siteMap.entries
            |> Option.orElse (Some fromParent)
        else
            matchingChildEntries siteMap fromParent destParentNodeId
            |> fun ms -> closestIndexedEntry ms (fromRange.start - 1)
            |> Option.orElseWith (fun () ->
                findAncestorEntry
                    siteMap
                    fromParent.parentInstanceId
                    destParentNodeId)
            |> Option.orElseWith (fun () ->
                match fromParent.parentInstanceId with
                | None -> None
                | Some gpId ->
                    match Map.tryFind gpId siteMap.entries with
                    | None -> None
                    | Some gp ->
                        let fromIdx =
                            gp.children
                            |> List.tryFindIndex ((=) fromParent.instanceId)
                            |> Option.defaultValue 0

                        matchingChildEntries siteMap gp destParentNodeId
                        |> fun ms -> closestIndexedEntry ms fromIdx)
            |> Option.orElseWith (fun () ->
                findSiteEntryByNodeId siteMap destParentNodeId)

    let private expandIfCollapsed (model: VM) (parent: SiteEntry) : VM * SiteEntry =
        if parent.expanded then
            model, parent
        else
            let sm, nid =
                expandEntry
                    parent.instanceId
                    model.graph
                    model.siteMap
                    model.nextSiteId
            let model' = { model with siteMap = sm; nextSiteId = nid }
            model', model'.siteMap.entries.[parent.instanceId]

    /// After a structural move that follows the selection: if the destination
    /// parent is visible (in siteMap) and collapsed, expand it so the moved
    /// nodes can remain selected. `stayAtSource` skips expansion.
    let selectionModelAfterStructuralMove
        (preGraph: Graph)
        (fromRange: SiteNodeRange)
        (stayAtSource: bool)
        (destParentNodeId: NodeId)
        (insertIdx: int)
        (count: int)
        (focusOffset: int)
        (fallbackParent: SiteEntry)
        (model: VM)
        : VM =
        let destOpt =
            findDestParentEntry model.siteMap destParentNodeId fromRange
        let model, newParent =
            match stayAtSource, destOpt with
            | _, None -> model, fallbackParent
            | true, Some parent -> model, parent
            | false, Some parent -> expandIfCollapsed model parent

        let newSel =
            ViewModelSelection.selectionAfterStructuralMove
                preGraph
                model.graph
                model.siteMap
                fromRange
                stayAtSource
                newParent
                insertIdx
                count
                focusOffset

        { model with selectedNodes = newSel }

    /// After a successful indent move (siteMap already reconciled), expand the
    /// previous-sibling parent if needed and select the moved nodes under it.
    let selectionModelAfterIndent (plan: IndentPlan) (result: VM) : VM =
        let result =
            match Map.tryFind plan.parentInstanceId result.siteMap.entries with
            | Some entry when not entry.expanded ->
                let sm, nid =
                    expandEntry
                        entry.instanceId
                        result.graph
                        result.siteMap
                        result.nextSiteId

                { result with siteMap = sm; nextSiteId = nid }
            | _ -> result

        match selectionAfterIndent plan result.siteMap with
        | Some sel -> { result with selectedNodes = Some sel }
        | None -> result

    /// Tab-indent completion. `Error` keeps the caller's original selection/focus
    /// and reports the apply/reject message in `lastCmdResult`.
    let completeIndent
        (original: VM)
        (plan: IndentPlan)
        (moved: Result<VM, string>)
        : VM =
        match moved with
        | Error msg -> withMoveError msg original
        | Ok result -> selectionModelAfterIndent plan result

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
