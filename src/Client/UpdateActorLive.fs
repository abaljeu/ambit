module Gambol.Client.UpdateActorLive

open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Client.UpdateHelpers

let withAppliedSync (state: ClientSyncState) (model: VM) : VM =
    { model with
        graph = state.graph
        history = state.history
        eventId = state.eventId
        actorLiveFocusIds = state.actorLiveFocusIds }

let withActorCmdResult (events: Ev list) (model: VM) : VM =
    match ActorLive.lastCmdResult model.graph model.zoomRoot events with
    | None -> model
    | Some result -> { model with lastCmdResult = Some result }

let withApplyDetail (state: ClientSyncState) (model: VM) : VM =
    match state.applyDetail with
    | None -> model
    | Some msg ->
        { model with
            lastCmdResult = Some (CmdLastResult.Detail (None, msg)) }

let withAppliedResult
    (events: Ev list)
    (state: ClientSyncState)
    (syncInfo: SyncInfo)
    (model: VM)
    : VM =
    let next =
        withAppliedSync state model
        |> withActorCmdResult events
        |> withApplyDetail state
    { next with syncInfo = syncInfo }

let withLaunchResults (events: Ev list) (model: VM) : VM =
    let next = withActorCmdResult events model
    { next with
        actorLiveFocusIds =
            ActorLive.applyEvents events next.actorLiveFocusIds }

let cancelFocusOp (focusId: NodeId) (model: VM) : VM * Effect list =
    if not (ActorLive.offersCancel focusId model.actorLiveFocusIds) then
        model, []
    else
        let syncInfo, effects =
            RunLaunch.queueCancel
                focusId
                model.eventId
                model.syncInfo
                []
        { model with syncInfo = syncInfo }, effects

let applyCommandEvents (events: Ev list) (model: VM) : VM * Effect list =
    match SyncLogic.applyServerTail events (clientSyncState model) with
    | Error msg ->
        { model with
            lastCmdResult = Some (CmdLastResult.Error (Some "Run", msg)) },
        []
    | Ok state ->
        let next =
            withAppliedSync state model
            |> withActorCmdResult events
            |> withApplyDetail state
            |> withSiteMap
            |> adjustModeAfterServerApply model.graph
        next, []
