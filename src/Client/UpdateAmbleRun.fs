module Gambol.Client.UpdateAmbleRun

open Gambol.Client.UpdateHelpers
open Gambol.Client.RunLaunch
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
        match applyAndPost (displayName Exec) plan.ops model with
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

/// Search and materialise. Caller Deletes existing Children first when needed.
let runAmbleOp (model: VM) : VM * Effect list =
    afterEditCommit model (fun committed commitEffects ->
        match committed.selectedNodes with
        | None -> committed, commitEffects
        | Some sel ->
            let focusId = focusedNodeId committed.graph sel
            match AmbleRun.runPlanOnNode focusId committed.graph with
            | Error _ -> committed, commitEffects
            | Ok plan ->
                applyRunPlan
                    (focusedInstanceId sel) plan commitEffects committed)
