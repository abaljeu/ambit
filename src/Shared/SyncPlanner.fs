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

    let tryStartSubmit (baseRevision: Gambol.Shared.Events.EventId) (syncInfo: SyncInfo) : SyncInfo * Effect list =
        match syncInfo.pendingChanges with
        | [] -> syncInfo, []
        | _ when isBlocked syncInfo.syncState -> syncInfo, []
        | _ when isBusy syncInfo.syncState -> syncInfo, []
        | changes ->
            let nextInfo = syncInfo |> SyncInfo.withSyncState (Sending 1)
            let (Gambol.Shared.Events.EventId baseRev) = baseRevision
            nextInfo, [ SubmitPendingBatch (baseRev, changes) ]

    let enqueuePending
        (item: PendingChange)
        (revision: Gambol.Shared.Events.EventId)
        (syncInfo: SyncInfo)
        : SyncInfo * Effect list =
        let pending = syncInfo.pendingChanges @ [ item ]
        let nextSyncInfo, submitEffects =
            { syncInfo with pendingChanges = pending }
            |> tryStartSubmit revision
        nextSyncInfo, SavePendingQueue pending :: submitEffects

    let retireSubmittedPrefix
        (submittedCount: int)
        (revision: Gambol.Shared.Events.EventId)
        (syncInfo: SyncInfo)
        : SyncInfo * PendingChange list * Effect list =
        let pending = List.skip submittedCount syncInfo.pendingChanges
        let baseInfo = syncInfo |> SyncInfo.withPendingChanges pending
        let (Gambol.Shared.Events.EventId rev) = revision
        match pending with
        | [] ->
            baseInfo |> SyncInfo.withSyncState Idle, pending, []
        | changes ->
            baseInfo |> SyncInfo.withSyncState (Sending 1),
            pending,
            [ SubmitPendingBatch (rev, changes) ]

    let restorePending
        (serverRevision: Gambol.Shared.Events.EventId)
        (saved: PendingChange list)
        (state: State)
        : State * PendingChange list =
        let (Gambol.Shared.Events.EventId serverRev) = serverRevision
        let prepared =
            saved
            |> List.filter (fun item ->
                let (Gambol.Shared.Events.EventId itemId) = item.event.id
                itemId >= serverRev)
            |> List.map (fun item -> { item with transition = None })
        let extractChange (event: Gambol.Shared.Events.Event) =
            { id = 0
              changeId = event.submissionId
              ops = Gambol.Shared.Events.Event.ops event |> Option.defaultValue [] }
        prepared
        |> List.fold
            (fun (state, reversed) item ->
                match History.applyChange (extractChange item.event) state with
                | ApplyResult.Changed next ->
                    next, item :: reversed
                | _ ->
                    state, reversed)
            (state, [])
        |> fun (nextState, reversed) -> nextState, List.rev reversed

    let retryWaiting
        (resetCount: bool)
        (syncInfo: SyncInfo)
        : SyncInfo * Effect list =
        match syncInfo.syncState, syncInfo.pendingChanges with
        | ServerRejected, _
        | CodeOutdated, _
        | DataOutdated, _ ->
            syncInfo, []
        | _, [] ->
            syncInfo |> SyncInfo.withSyncState Idle, []
        | WaitingToRetry (n, baseRev, changes), _ ->
            let nextAttempt = if resetCount then 1 else n + 1
            syncInfo |> SyncInfo.withSyncState (Sending nextAttempt),
            [ SubmitPendingBatch (baseRev, changes) ]
        | Sending _, _ -> syncInfo, []
        | _ -> syncInfo, []

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
    let tryStartPoll (revision: Gambol.Shared.Events.EventId) (syncInfo: SyncInfo) : SyncInfo * Effect list =
        match syncInfo.syncState, syncInfo.pendingChanges with
        | Idle, [] ->
            let (Gambol.Shared.Events.EventId pollRevision) =
                match syncInfo.catchUp with
                | Some baseline -> baseline.revision
                | None -> revision
            syncInfo |> SyncInfo.withSyncState Polling,
            [ PollServer pollRevision ]
        | _ -> syncInfo, []

    /// Emit a LoadServer effect (Fetch + Poll) when idle with an empty pending queue.
    let tryStartLoad
        (revision: Gambol.Shared.Events.EventId)
        (targets: LoadTarget list)
        (syncInfo: SyncInfo)
        : SyncInfo * Effect list =
        match syncInfo.syncState, syncInfo.pendingChanges with
        | Idle, [] ->
            let (Gambol.Shared.Events.EventId rev) = revision
            syncInfo |> SyncInfo.withSyncState Loading,
            [ LoadServer(rev, targets) ]
        | _ -> syncInfo, []
