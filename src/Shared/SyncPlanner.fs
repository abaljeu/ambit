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
        | Sending _ | Polling | Uploading | Parsing | Loading -> true
        | _ -> false

    let tryStartSubmit
        (baseEventId: Gambol.Shared.EventId)
        (syncInfo: SyncInfo)
        : SyncInfo * Effect list =
        match syncInfo.pending with
        | [] -> syncInfo, []
        | _ when isBlocked syncInfo.syncState -> syncInfo, []
        | _ when isBusy syncInfo.syncState -> syncInfo, []
        | events ->
            let nextInfo = syncInfo |> SyncInfo.withSyncState (Sending 1)
            nextInfo, [ SubmitPendingBatch (EventId.value baseEventId, events) ]

    let enqueuePending
        (event: Ev)
        (eventId: Gambol.Shared.EventId)
        (syncInfo: SyncInfo)
        : SyncInfo * Effect list =
        let pending = syncInfo.pending @ [ event ]
        let nextSyncInfo, submitEffects =
            { syncInfo with pending = pending }
            |> tryStartSubmit eventId
        nextSyncInfo, SavePendingQueue pending :: submitEffects

    let retireSubmittedPrefix
        (submittedCount: int)
        (eventId: Gambol.Shared.EventId)
        (syncInfo: SyncInfo)
        : SyncInfo * Ev list * Effect list =
        let pending = List.skip submittedCount syncInfo.pending
        let baseInfo = syncInfo |> SyncInfo.withPending pending
        match pending with
        | [] ->
            baseInfo |> SyncInfo.withSyncState Idle, pending, []
        | events ->
            baseInfo |> SyncInfo.withSyncState (Sending 1),
            pending,
            [ SubmitPendingBatch (EventId.value eventId, events) ]

    let restorePending
        (_serverEventId: Gambol.Shared.EventId)
        (saved: Ev list)
        (state: State)
        : State * Ev list =
        saved
        |> List.fold
            (fun (state, reversed) event ->
                let change = Ev.asChange { event with id = EventId.zero }
                match ChangeValidation.applyChange change state with
                | ApplyResult.Changed next ->
                    next, event :: reversed
                | _ ->
                    state, reversed)
            (state, [])
        |> fun (nextState, reversed) -> nextState, List.rev reversed

    let retryWaiting
        (resetCount: bool)
        (syncInfo: SyncInfo)
        : SyncInfo * Effect list =
        match syncInfo.syncState, syncInfo.pending with
        | ServerRejected, _
        | CodeOutdated, _
        | DataOutdated, _ ->
            syncInfo, []
        | _, [] ->
            syncInfo |> SyncInfo.withSyncState Idle, []
        | WaitingToRetry (n, baseEventId, events), _ ->
            let nextAttempt = if resetCount then 1 else n + 1
            syncInfo |> SyncInfo.withSyncState (Sending nextAttempt),
            [ SubmitPendingBatch (baseEventId, events) ]
        | Sending _, _ -> syncInfo, []
        | _ -> syncInfo, []

    /// Release requests parked behind the change-ops queue, once that queue has drained
    /// and nothing is in flight. Called after every message so any path back to Idle
    /// (ack, poll, retry, upload completion) lets the parked request through.
    let tryReleaseQueued (syncInfo: SyncInfo) : SyncInfo * Effect list =
        match syncInfo.queuedRequests with
        | [] -> syncInfo, []
        | request :: remaining when
            syncInfo.pending.IsEmpty && syncInfo.syncState = Idle ->
            { syncInfo with queuedRequests = remaining },
            [ RunQueuedRequest request ]
        | _ -> syncInfo, []

    /// Emit a PollServer effect when idle with an empty queue and not already polling.
    let tryStartPoll
        (eventId: Gambol.Shared.EventId)
        (syncInfo: SyncInfo)
        : SyncInfo * Effect list =
        match syncInfo.syncState, syncInfo.pending with
        | Idle, [] ->
            let pollEventId =
                match syncInfo.catchUp with
                | Some baseline -> baseline.eventId
                | None -> eventId
            syncInfo |> SyncInfo.withSyncState Polling,
            [ PollServer (EventId.value pollEventId) ]
        | _ -> syncInfo, []

    /// Emit a LoadServer effect (Fetch + Poll) when idle with an empty pending queue.
    let tryStartLoad
        (eventId: Gambol.Shared.EventId)
        (targets: LoadTarget list)
        (syncInfo: SyncInfo)
        : SyncInfo * Effect list =
        match syncInfo.syncState, syncInfo.pending with
        | Idle, [] ->
            syncInfo |> SyncInfo.withSyncState Loading,
            [ LoadServer(EventId.value eventId, targets) ]
        | _ -> syncInfo, []
