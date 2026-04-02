namespace Gambol.Shared

open Gambol.Shared.ViewModel

type SubmitIntent =
    | NoSubmit
    | SubmitHead of baseRevision: int * headChange: Change

[<RequireQualifiedAccess>]
module SyncPlanner =
    let private isBlocked (syncState: SyncState) =
        match syncState with
        | ServerRejected | CodeOutdated | DataOutdated | WaitingToRetry _ -> true
        | _ -> false

    let tryStartSubmit (baseRevision: Revision) (syncInfo: SyncInfo) : SyncInfo * SubmitIntent =
        match syncInfo.pendingChanges with
        | [] -> syncInfo, NoSubmit
        | head :: _ when isBlocked syncInfo.syncState -> syncInfo, NoSubmit
        | head :: _ when syncInfo.submitInFlight || syncInfo.pollInFlight -> syncInfo, NoSubmit
        | head :: _ ->
            let nextInfo =
                syncInfo
                |> SyncInfo.withSyncState (Sending 1)
                |> SyncInfo.withSubmitInFlight true
            nextInfo, SubmitHead (baseRevision.Value, head)

    let ackSubmit
        (ackChangeId: System.Guid)
        (revision: Revision)
        (syncInfo: SyncInfo)
        : SyncInfo * Change list * SubmitIntent =
        let pending =
            match syncInfo.pendingChanges with
            | head :: tail when head.changeId = ackChangeId -> tail
            | _ -> syncInfo.pendingChanges
        let baseInfo = syncInfo |> SyncInfo.withPendingChanges pending
        match pending with
        | [] ->
            baseInfo
            |> SyncInfo.withSyncState Idle
            |> SyncInfo.withSubmitInFlight false, pending, NoSubmit
        | head :: _ ->
            baseInfo
            |> SyncInfo.withSyncState (Sending 1)
            |> SyncInfo.withSubmitInFlight true, pending, SubmitHead (revision.Value, head)
