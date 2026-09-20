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

let withAppliedResult
    (events: Ev list)
    (state: ClientSyncState)
    (syncInfo: SyncInfo)
    (model: VM)
    : VM =
    let next =
        withAppliedSync state model |> withActorCmdResult events
    { next with syncInfo = syncInfo }

let cancelFocusOp (focusId: NodeId) (model: VM) : VM * Effect list =
    match ActorLive.cancelEffect focusId model.actorLiveFocusIds with
    | Some effect -> model, [ effect ]
    | None -> model, []

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
            |> withSiteMap
            |> adjustModeAfterServerApply model.graph
        next, []
