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
        | Sending _ | Polling | Uploading -> true
        | _ -> false

    let tryStartSubmit (baseRevision: Revision) (syncInfo: SyncInfo) : SyncInfo * Effect list =
        match syncInfo.pendingChanges with
        | [] -> syncInfo, []
        | _ when isBlocked syncInfo.syncState -> syncInfo, []
        | _ when isBusy syncInfo.syncState -> syncInfo, []
        | changes ->
            let nextInfo = syncInfo |> SyncInfo.withSyncState (Sending 1)
            nextInfo, [ SubmitPendingBatch (baseRevision.Value, changes) ]

    let ackBatch
        (ackedChangeIds: System.Guid list)
        (revision: Revision)
        (syncInfo: SyncInfo)
        : SyncInfo * Change list * Effect list =
        let acked = ackedChangeIds |> Set.ofList
        let pending =
            syncInfo.pendingChanges
            |> List.filter (fun change -> not (Set.contains change.changeId acked))
        let baseInfo = syncInfo |> SyncInfo.withPendingChanges pending
        match pending with
        | [] ->
            baseInfo |> SyncInfo.withSyncState Idle, pending, []
        | changes ->
            baseInfo |> SyncInfo.withSyncState (Sending 1),
            pending,
            [ SubmitPendingBatch (revision.Value, changes) ]

    /// Release requests parked behind the change-ops queue, once that queue has drained
    /// and nothing is in flight. Called after every message so any path back to Idle
    /// (ack, poll, retry, upload completion) lets the parked request through.
    let tryReleaseQueued (syncInfo: SyncInfo) : SyncInfo * Effect list =
        match syncInfo.queuedRequests with
        | [] -> syncInfo, []
        | request :: remaining when
            syncInfo.pendingChanges.IsEmpty && syncInfo.syncState = Idle ->
            { syncInfo with queuedRequests = remaining },
            [ RunQueuedRequest request ]
        | _ -> syncInfo, []

    /// Emit a PollServer effect when idle with an empty queue and not already polling.
    let tryStartPoll (revision: Revision) (syncInfo: SyncInfo) : SyncInfo * Effect list =
        match syncInfo.syncState, syncInfo.pendingChanges with
        | Idle, [] ->
            syncInfo |> SyncInfo.withSyncState Polling, [ PollServer revision.Value ]
        | _ -> syncInfo, []
