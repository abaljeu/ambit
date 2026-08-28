module Gambol.Client.UpdateAmbleRun

open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.CommandEntry
open Gambol.Shared.ViewModel

let private focusLine (model: VM) (focusId: NodeId) : string =
    match model.mode with
    | Editing _ -> readEditInputValue ()
    | _ -> model.graph.nodes.[focusId].text

let runAmbleOp (model: VM) : VM * Effect list =
    match model.selectedNodes with
    | None -> model, []
    | Some sel ->
        let focusId = focusedNodeId model.graph sel
        let line = focusLine model focusId
        match AmbleRun.runPlan focusId model.graph line with
        | Error _ -> model, []
        | Ok plan ->
            if plan.ops.IsEmpty then
                model, []
            else
                let change =
                    { id = model.revision.Value
                      changeId = System.Guid.NewGuid()
                      ops = plan.ops }
                match applyAndPost (displayName Exec) change model with
                | Error _ -> model, []
                | Ok (m, effects) ->
                    let m = withSiteMap m
                    let sm, nextId =
                        AmbleRun.applyUnfold
                            plan.unfold focusId m.graph m.siteMap m.nextSiteId
                    { m with siteMap = sm; nextSiteId = nextId }, effects
