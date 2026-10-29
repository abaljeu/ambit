# 01 — Accept Notices scope

Slice: With an empty pending queue, apply each Notice and store its event id; `NoticeGraph.writeGraph` writes the Notice and an `Error` of a string leaves the Graph unchanged while the event id is still stored; a post reply applies its Notices the same way then matches confirmations; `Update` PollDone leaves Sync `Idle` when the pending queue is empty, including when the edit box holds new text.

## Signatures

[ ] `type Notice` with cases `NodeParsed`, `NodePersisted`, `NodeUnparsed`, `NodeUploading`, `NodeReceived`, `NodeParsing`, `NodePersisting`, `NodeUnpersisted`, `PersistFailed`, and `ParseFailed` in [[src/Shared/History.fs]].

[ ] `let writeGraph (notice: Notice) (graph: Graph) : Result<Graph, string>` in [[src/Shared/NoticeGraph.fs]], function `writeGraph`.

[ ] `let private applyNoticeGraph (notice: Notice) (st: ClientSyncState)` in [[src/Shared/SyncLogic.fs]], function `applyNoticeGraph`.

[ ] `let private applyLoggedNotice (event: Ev) (st: ClientSyncState) (undid: bool) : Result<ClientSyncState * bool, string>` in [[src/Shared/SyncLogic.fs]], function `applyLoggedNotice`.

[ ] `let private foldProjectedEvents (events: Ev list) (state: ClientSyncState) : Result<ClientSyncState * bool, string>` in [[src/Shared/SyncLogic.fs]], function `foldProjectedEvents`.

[ ] `let applySyncResponse (response: SyncResponse) (state: ClientSyncState) : Result<ClientSyncState, string>` in [[src/Shared/SyncLogic.fs]], function `applySyncResponse`.

[ ] `let applyServerTail (events: Ev list) (state: ClientSyncState) : Result<ClientSyncState, string>` in [[src/Shared/SyncLogic.fs]], function `applyServerTail`.

[ ] `let reconcileAck (submitted: Ev list) (confirmed: Ev list) (serverEventId: EventId) (state: ClientSyncState) (syncInfo: SyncInfo) : AckReconcile` in [[src/Shared/SyncLogic.fs]], function `reconcileAck`.

[ ] `let isConfirmationEcho (submitted: Ev list) (confirmed: Ev list) : bool` in [[src/Shared/SyncLogic.fs]], function `isConfirmationEcho`.

[ ] `let persistFailedMessage (events: Ev list) : string option` in [[src/Shared/SyncLogic.fs]], function `persistFailedMessage`.

[ ] `let update (msg: Msg) (model: VM) : VM * Effect list` in [[src/Client/Update.fs]], function `update`.

[ ] `let isAutoSyncBlocked (model: VM) : bool` in [[src/Client/UpdateHelpers.fs]], function `isAutoSyncBlocked`.

## Match sites

[ ] `NoticeGraph.writeGraph` matches every `Notice` case in [[src/Shared/NoticeGraph.fs]], function `writeGraph`.

[ ] `SyncLogic.noticeNodeId` matches every `Notice` case in [[src/Shared/SyncLogic.fs]], function `noticeNodeId`.

[ ] `SyncLogic.applyLoggedNotice` matches `EventBody.Notice notice` in [[src/Shared/SyncLogic.fs]], function `applyLoggedNotice`.

[ ] `SyncLogic.foldProjectedEvents` calls `applyLoggedNotice` when `isNoticeEvent event` is true in [[src/Shared/SyncLogic.fs]], function `foldProjectedEvents`.

[ ] `SyncLogic.applySyncResponse` calls `foldProjectedEvents response.events` in [[src/Shared/SyncLogic.fs]], function `applySyncResponse`.

[ ] `SyncLogic.reconcileAck` calls `dropNotices` in `identityError` and `collectSuffixes` in [[src/Shared/SyncLogic.fs]], function `reconcileAck`.

[ ] `Update.reconcileSubmit` calls `SyncLogic.reconcileAck` or `SyncLogic.reconcileExternalAck` in [[src/Client/Update.fs]], function `reconcileSubmit`.

[ ] `Update.applySubmitResponse` calls `reconcileSubmit` then `finishAppliedSubmit` in [[src/Client/Update.fs]], function `applySubmitResponse`.

[ ] `Update.finishAppliedSubmit` calls `SyncAnswer.applyChangeSuccess` in [[src/Client/Update.fs]], function `finishAppliedSubmit`.

[ ] `Update.update` matches `SysMsg (PollDone (stateOpt, syncResponse, readyOpt, responseEventId))` in [[src/Client/Update.fs]], function `update`.

[ ] `App.runPollServer` dispatches `PollDone` with `SyncLogic.getPollOutcome poll model.eventId` in [[src/Client/App.fs]], function `runPollServer`.

[ ] `UpdateWorkspaceSync.reconcileWorkspaceAck` calls `SyncLogic.reconcileAck` in [[src/Client/UpdateWorkspaceSync.fs]], function `reconcileWorkspaceAck`.

## Existing tests

[ ] ``PersistFailed leaves the graph`` in [[tests/Shared.Tests/SyncLogicTests.fs]] checks `applyServerTail` leaves update time and persist state unchanged for `Notice.PersistFailed` and that `SyncLogic.persistFailedMessage` returns the message.

[ ] ``NodePersisted on a normal node is rejected`` in [[tests/Shared.Tests/SyncLogicTests.fs]] checks `applyServerTail` returns `Error` with message `normal nodes do not have parse or persist state` and leaves the graph unchanged.

[ ] ``logged NodePersisted for an older version leaves the client node Unpersisted`` in [[tests/Shared.Tests/SyncLogicTests.fs]] checks `applyServerTail` applies `Notice.NodePersisted` on a File Node and version-gates persist state.

[ ] ``Notice in the reply keeps the confirmation echo`` in [[tests/Shared.Tests/AckReconcileTests.fs]] checks `SyncLogic.isConfirmationEcho` and `SyncLogic.reconcileAck` with a trailing `EventBody.Notice` `Notice.NodePersisted` in the confirmed list.

[ ] ``Poll answer chains the next Included residency Want`` in [[tests/Server.Tests/BrowserResidencyClientTests.fs]] calls `Update.update` with `SysMsg (PollDone (None, response, None, Some EventId.zero))` and expects a follow-on `PollServer` effect.

## Copy-from

[ ] `applyLoggedNotice` in [[src/Shared/SyncLogic.fs]] already advances `eventId` and calls `applyNoticeGraph` for `EventBody.Notice`; the slice extends that path so a `NoticeGraph.writeGraph` `Error` still stores the event id and leaves the graph unchanged.

## write-ticket

### Source files

[ ] [[src/Shared/SyncLogic.fs]] — functions `applyNoticeGraph`, `applyLoggedNotice`, and `reconcileAck` (apply Notice events from a post reply before confirmation matching).

[ ] [[src/Client/Update.fs]] — function `update`, `SysMsg (PollDone …)` arm (apply notice-only tails when the pending queue is empty and keep Sync `Idle`, including when `isAutoSyncBlocked` is true).

[ ] [[src/Shared/NoticeGraph.fs]] — function `writeGraph` (read-only unless a Notice case must align with accept semantics; today failures come from `GraphMutate` on non-artifact nodes).

### Tests

[ ] [[tests/Shared.Tests/SyncLogicTests.fs]] — change ``NodePersisted on a normal node is rejected`` to expect `Ok` with stored event id and an unchanged graph.

[ ] [[tests/Shared.Tests/SyncLogicTests.fs]] — add a `[<Fact>]` that `applyServerTail` stores the event id when `NoticeGraph.writeGraph` returns `Error` on a present node (same setup as the changed normal-node test).

[ ] [[tests/Shared.Tests/AckReconcileTests.fs]] — add a `[<Fact>]` that `SyncLogic.reconcileAck` applies a File Node `Notice.NodePersisted` from the confirmed list before suffix projection (copy graph setup from ``logged NodePersisted for an older version leaves the client node Unpersisted``).

[ ] [[tests/Server.Tests/BrowserResidencyClientTests.fs]] — add a `[<Fact>]` that `Update.update` PollDone with `Some DataOutdated`, a notice-only tail, empty pending, and Editing mode with dirty draft leaves `syncInfo.syncState` as `Idle` and advances `eventId`.

## Size

[ ] Source file count: 2 (`SyncLogic.fs`, `Update.fs`; `NoticeGraph.fs` is read-only unless a write case changes).

[ ] Test add-or-change count: 4 (one change, three adds).

[ ] Fits 8 source files and 4 tests; no split.
