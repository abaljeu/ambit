module Gambol.Client.UpdateAmbleRun

open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.CommandEntry
open Gambol.Shared.ViewModel

let private applyRunPlan
    (queryInst: SiteId option)
    (plan: ExprRun.Plan)
    (commitEffects: Effect list)
    (model: VM)
    : VM * Effect list =
    if plan.ops.IsEmpty then
        model, commitEffects
    else
        let change =
            { id = model.revision.Value
              changeId = System.Guid.NewGuid()
              ops = plan.ops }
        match applyAndPost (displayName Exec) change model with
        | Error _ -> model, commitEffects
        | Ok (m, effects) ->
            let m = withSiteMap m
            let sm, nextId =
                match queryInst with
                | None -> m.siteMap, m.nextSiteId
                | Some inst ->
                    AmbleRun.applyUnfold
                        plan.unfold inst m.graph m.siteMap m.nextSiteId
            { m with siteMap = sm; nextSiteId = nextId },
            commitEffects @ effects

let runAmbleOp (model: VM) : VM * Effect list =
    let committed, commitEffects = commitIfEditing model
    match committed.selectedNodes with
    | None -> committed, commitEffects
    | Some sel ->
        let focusId = focusedNodeId committed.graph sel
        match AmbleRun.runPlanOnNode focusId committed.graph with
        | Error _ -> committed, commitEffects
        | Ok plan ->
            applyRunPlan (focusedInstanceId sel) plan commitEffects committed
