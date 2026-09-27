module Gambol.Client.SyncAnswer

open Gambol.Shared

let withoutEvents (response: SyncResponse) : SyncResponse =
    { response with events = [] }

let fromChangeSuccess (response: ChangeSuccessResponse) : SyncResponse =
    response
    |> SyncLogic.changeSuccessToSync
    |> withoutEvents

let apply
    (response: SyncResponse)
    (state: ClientSyncState)
    : Result<ClientSyncState, string> =
    SyncLogic.applySyncResponse (withoutEvents response) state

let applyChangeSuccess
    (response: ChangeSuccessResponse)
    (state: ClientSyncState)
    : Result<ClientSyncState, string> =
    SyncLogic.applySyncResponse (fromChangeSuccess response) state
