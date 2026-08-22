namespace Gambol.Shared

[<RequireQualifiedAccess>]
type PendingKind =
    | Normal
    | Undo
    | Redo

type PendingTransition =
    { recordId: int
      submittedChangeId: System.Guid
      kind: PendingKind }

type PendingChange =
    { change: Change
      transition: PendingTransition option }

[<RequireQualifiedAccess>]
module PendingChange =
    let ofChange (change: Change) : PendingChange =
        { change = change; transition = None }

    let workspaceSingleton (recordId: int) (change: Change) : PendingChange =
        { change = change
          transition =
            Some
                { recordId = recordId
                  submittedChangeId = change.changeId
                  kind = PendingKind.Normal } }

type SyncState =
    | Idle                       // all confirmed, nothing pending
    | Sending of attempt: int    // POST in-flight; attempt = 1-based send count
    | Polling                    // GET poll in-flight
    | Uploading                  // workspace file push in progress (blocks poll)
    | Parsing                    // server disk parse/reconcile in progress (blocks poll)
    | Loading                    // Load Fetch+Poll in-flight (blocks poll/submit)
    | WaitingToRetry of attempt: int * baseRevision: int * changes: PendingChange list
    | ServerRejected  // server returned 400 — change cannot be applied; reload required
    | CodeOutdated    // server has newer code (build stamp changed) — reload required
    | DataOutdated    // server has newer data with no local pending — reload required

/// A multi-phase request that must start from a settled revision, so it rides the
/// change-ops queue instead of running while a submit or poll is in flight.
type QueuedRequest =
    | QueuedLoad
    /// Preserve a desktop target while another workspace push is in flight.
    | QueuedWorkspacePush of WorkspaceSyncScope * parseFileId: NodeId option

/// Optimistic graph at the last server revision before catch-up replay.
type CatchUpBaseline =
    { revision: Revision
      graph: Graph }

type SyncInfo =
    { syncState: SyncState
      pendingChanges: PendingChange list
      /// Requests parked behind `pendingChanges` (see `SyncPlanner.tryReleaseQueued`).
      queuedRequests: QueuedRequest list
      /// Baseline noted from a Post external-changes signal until Poll replay completes.
      catchUp: CatchUpBaseline option
      isPollingActive: bool
      isServerReady: bool
      syncRiskAcknowledged: bool }

[<RequireQualifiedAccess>]
module SyncInfo =
    let initial: SyncInfo =
        { syncState = Idle
          pendingChanges = []
          queuedRequests = []
          catchUp = None
          isPollingActive = false
          isServerReady = false
          syncRiskAcknowledged = false }

    let withPendingChanges (pending: PendingChange list) (si: SyncInfo) : SyncInfo =
        { si with pendingChanges = pending }

    /// Park a request behind the change-ops queue. Pressing the command again while
    /// it waits is not a second request.
    let queueRequest (request: QueuedRequest) (si: SyncInfo) : SyncInfo =
        if List.contains request si.queuedRequests then si
        else { si with queuedRequests = si.queuedRequests @ [ request ] }

    let withServerReady ready (si: SyncInfo) : SyncInfo =
        { si with isServerReady = ready }

    let withCatchUp catchUp (si: SyncInfo) : SyncInfo =
        { si with catchUp = catchUp }

    let clearCatchUp (si: SyncInfo) : SyncInfo =
        { si with catchUp = None }

    /// Updates sync state. Clears risk acknowledgment when crossing the risk boundary.
    let withSyncState (newState: SyncState) (si: SyncInfo) : SyncInfo =
        let inRisk =
            function
            | ServerRejected | CodeOutdated | DataOutdated -> true
            | _ -> false
        let wasR = inRisk si.syncState
        let nowR = inRisk newState
        if wasR = nowR then { si with syncState = newState }
        else { si with syncState = newState; syncRiskAcknowledged = false }

type Effect =
    | SubmitPendingBatch of baseRevision: int * changes: PendingChange list
    | PollServer of revision: int
    | LoadServer of revision: int * targets: LoadTarget list
    | ScheduleRetry of delayMs: int
    /// The change-ops queue settled: run a request that was parked behind it.
    | RunQueuedRequest of QueuedRequest
    | SavePendingQueue of PendingChange list
    | RequestDesktopFileStatus of nodeId: NodeId * path: string
    | RequestServerFileStatus of nodeId: NodeId * path: string
    /// Desktop: refresh mapped labels + sync-ledger facts for path-status UI.
    | RequestWorkspacePathSyncSnapshot
    /// After create or Upload: async inventory, then local stubs + structure POST.
    | ContinueWorkspaceStubsThenPush of WorkspaceSyncScope * parseFileId: NodeId option
    /// After local stubs painted: async structure Change POST, then workspace-push.
    | ContinuePostUploadStructure of
        PendingChange * WorkspaceSyncScope * parseFileId: NodeId option
    /// Deferred async workspace-push (`postJson`); Some fileId → parse after push.
    | ContinueWorkspacePush of WorkspaceSyncScope * parseFileId: NodeId option
    /// Poll `GET /_desktop/workspace-download?id=` until job completes or fails.
    | ContinueWorkspaceDownload of jobId: string
    /// Deferred async web reconcile (`postJson /reconciliation/directory`).
    | ContinueDirectoryReconcile of WorkspaceSyncScope
    /// Deferred async file parse (`fetchGet` desktop text when needed, then `postJson`).
    | ContinueParseFile of
        fileId: NodeId *
        desktopReadPath: string option *
        detailPrefix: string *
        detailPath: string
    /// Same Upload may reparse several skipped files; run these requests in order.
    | ContinueUploadParses of parseRequests: Effect list
    /// Arm (or re-arm) the debounced auto-download tick after a delay.
    | ScheduleAutoDownloadTick of delayMs: int
