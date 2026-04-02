namespace Gambol.Shared

open Gambol.Shared.ViewModel

[<RequireQualifiedAccess>]
module SyncPlanner =
    let private isBlocked (syncState: SyncState) =
        match syncState with
        | ServerRejected | CodeOutdated | DataOutdated | WaitingToRetry _ -> true
        | _ -> false

    let private isBusy (syncState: SyncState) =
        match syncState with
        | Sending _ | Polling -> true
        | _ -> false

    let tryStartSubmit (baseRevision: Revision) (syncInfo: SyncInfo) : SyncInfo * Effect list =
        match syncInfo.pendingChanges with
        | [] -> syncInfo, []
        | head :: _ when isBlocked syncInfo.syncState -> syncInfo, []
        | head :: _ when isBusy syncInfo.syncState -> syncInfo, []
        | head :: _ ->
            let nextInfo = syncInfo |> SyncInfo.withSyncState (Sending 1)
            nextInfo, [ SubmitChange (baseRevision.Value, head) ]

    let ackSubmit
        (ackChangeId: System.Guid)
        (revision: Revision)
        (syncInfo: SyncInfo)
        : SyncInfo * Change list * Effect list =
        let pending =
            match syncInfo.pendingChanges with
            | head :: tail when head.changeId = ackChangeId -> tail
            | _ -> syncInfo.pendingChanges
        let baseInfo = syncInfo |> SyncInfo.withPendingChanges pending
        match pending with
        | [] ->
            baseInfo |> SyncInfo.withSyncState Idle, pending, []
        | head :: _ ->
            baseInfo |> SyncInfo.withSyncState (Sending 1),
            pending,
            [ SubmitChange (revision.Value, head) ]

    /// Emit a PollServer effect when idle with an empty queue and not already polling.
    let tryStartPoll (revision: Revision) (syncInfo: SyncInfo) : SyncInfo * Effect list =
        match syncInfo.syncState, syncInfo.pendingChanges with
        | Idle, [] ->
            syncInfo |> SyncInfo.withSyncState Polling, [ PollServer revision.Value ]
        | _ -> syncInfo, []
