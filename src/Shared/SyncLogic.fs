namespace Gambol.Shared

/// Client-side values needed to decide if poll response implies stale.
type ClientPollContext =
    { revision: int
      pendingCount: int
      buildEpochSec: int
      pageBuildEpochSec: int }

[<RequireQualifiedAccess>]
module SyncLogic =

    /// True when the client should dispatch ServerAhead (go Stale). Avoids false positives:
    /// - When client has pending changes, serverRev > clientRev may be our own in-flight change.
    /// - When client build/page stamps are 0 (before injection), skip serverNewer check.
    let shouldReportStale (poll: PollResponse) (client: ClientPollContext) : bool =
        let dataStale =
            poll.revision > client.revision && client.pendingCount = 0
        let serverNewer =
            (client.buildEpochSec <> 0 && client.pageBuildEpochSec <> 0)
            && (poll.buildEpochSec <> client.buildEpochSec
                || poll.pageBuildEpochSec <> client.pageBuildEpochSec)
        dataStale || serverNewer


    /// Handle SubmitFailed: ignore when Stale or when pending is empty (late timeout).
    /// Otherwise transition to Stale.
    let applySubmitRejected(model: VM) : VM =
        if model.syncInfo.syncState = Stale then model
        elif model.syncInfo.pendingChanges.IsEmpty then model
        else { model with syncInfo = SyncInfo.withSyncState Stale model.syncInfo }

    /// Handle SubmitNoResponse: keep increasing Syncing attempts until cap, then Pending.
    let applySubmitNoResponse (model: VM) : VM =
        if model.syncInfo.pendingChanges.IsEmpty then model
        else
            let nextState =
                match model.syncInfo.syncState with
                | Syncing n when n < 10 -> Syncing (n + 1)
                | Syncing n -> Pending n
                | Pending n -> Pending n
                | _ -> Pending 1
            { model with syncInfo = SyncInfo.withSyncState nextState model.syncInfo }

    /// Handle ServerAhead: transition to Stale.
    let applyServerAhead (_rev: Revision) (model: VM) : VM =
        { model with syncInfo = SyncInfo.withSyncState Stale model.syncInfo }
