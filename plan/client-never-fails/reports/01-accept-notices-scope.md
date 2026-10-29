# 01 — Accept Notices scope

## Spec story

[ ] Story 1 Accept Notices in [Client never fails on server data](plan/client-never-fails/spec.md): "Accept Notices — As an outliner, I want the Browser to apply each Notice and take its event id when it has no pending changes, and a post reply to apply its Notices too, so that parse and persist status never stops Sync while I type or expand."

## Signatures

[ ] `src/Shared/NoticeGraph.fs` line 82: `let writeGraph (notice: Notice) (graph: Graph) : Result<Graph, string>`

[ ] `src/Shared/SyncLogic.fs` line 92: `let private applyNoticeGraph (notice: Notice) (st: ClientSyncState)`

[ ] `src/Shared/SyncLogic.fs` line 101–105: `let private applyLoggedNotice (event: Ev) (st: ClientSyncState) (undid: bool)`

[ ] `src/Shared/SyncLogic.fs` line 303–306: `let applyServerTail (events: Ev list) (state: ClientSyncState) : Result<ClientSyncState, string>`

[ ] `src/Shared/SyncLogic.fs` line 545–551: `let reconcileAck (submitted: Ev list) (confirmed: Ev list) (serverEventId: Gambol.Shared.EventId) (state: ClientSyncState) (syncInfo: SyncInfo) : AckReconcile`

[ ] `src/Client/Update.fs` line 170: `let update (msg: Msg) (model: VM) : VM * Effect list`

[ ] `src/Client/Update.fs` line 137–141: `let private applySubmitResponse (submitted: Ev list) (response: ChangeSuccessResponse) (model: VM) : VM * Effect list`

[ ] `src/Client/UpdateHelpers.fs` line 266: `let isAutoSyncBlocked (model: VM) : bool`

[ ] `src/Shared/SyncLogic.fs` line 388: `let private dropNotices (events: Ev list)`

## Match sites

[ ] `NoticeGraph.writeGraph` at `src/Shared/NoticeGraph.fs` lines 82–100 matches each `Notice` case; `Notice.NodePersisted` calls `writePersisted` at line 99–100.

[ ] `SyncLogic.applyNoticeGraph` at `src/Shared/SyncLogic.fs` lines 92–99: absent node returns `Ok st` at lines 94–95; present node matches `NoticeGraph.writeGraph notice st.graph` at lines 97–99 with arms `| Error msg -> Error msg` and `| Ok graph -> Ok { st with graph = graph }`.

[ ] `SyncLogic.applyLoggedNotice` at `src/Shared/SyncLogic.fs` lines 106–112: `EventBody.Notice notice` at line 107 calls `applyNoticeGraph notice st` at line 108 with arms `| Error msg -> Error msg` at line 109 and `| Ok next -> Ok({ next with eventId = event.id }, undid)` at line 111; other bodies return `Ok({ st with eventId = event.id }, undid)` at line 112.

[ ] `SyncLogic.foldProjectedEvents` step at `src/Shared/SyncLogic.fs` lines 126–127 calls `applyLoggedNotice event st undid` when `isNoticeEvent event`.

[ ] `SyncLogic.applySyncResponse` at `src/Shared/SyncLogic.fs` lines 227–243 calls `foldProjectedEvents response.events state` at line 231.

[ ] `SyncLogic.applyServerTail` at `src/Shared/SyncLogic.fs` lines 303–311 calls `applySyncResponse` with `{ events = events; nodes = []; childMap = Map.empty }` at lines 307–311.

[ ] `SyncLogic.dropNotices` at `src/Shared/SyncLogic.fs` lines 388–389 filters with `not (isNoticeEvent event)`.

[ ] `SyncLogic.identityError` at `src/Shared/SyncLogic.fs` line 395 sets `let confirmed = dropNotices confirmed` before length and submission-id checks.

[ ] `SyncLogic.collectSuffixes` at `src/Shared/SyncLogic.fs` line 417 sets `let confirmed = dropNotices confirmed` before the suffix loop.

[ ] `SyncLogic.reconcileAck` at `src/Shared/SyncLogic.fs` lines 552–588: empty submitted returns `AckReconcile.Rejected "missing confirmation"` at lines 552–553; then `identityError`, `collectSuffixes`, `queueOutcome`, and `projectSuffixes`; applied path sets `eventId = serverEventId` and `graph = markUndoRedoVersions confirmed graph` at lines 574–577 without a prior notice-apply fold on `confirmed`.

[ ] `SyncLogic.isNoticeEvent` at `src/Shared/SyncLogic.fs` lines 57–60 matches `EventBody.Notice _`.

[ ] `SyncLogic.persistFailedMessage` at `src/Shared/SyncLogic.fs` lines 63–69 matches `EventBody.Notice (Notice.PersistFailed (_, message))`.

[ ] `Update.reconcileSubmit` at `src/Client/Update.fs` lines 60–77 calls `SyncLogic.reconcileAck submitted response.events response.eventId state model.syncInfo` at lines 74–75 when `needsCatchUp` is false.

[ ] `Update.applySubmitResponse` at `src/Client/Update.fs` lines 229–230 dispatches `SysMsg (SubmitResponse (submitted, response))` to `applySubmitResponse submitted response model` at line 230, which calls `reconcileSubmit` at line 150.

[ ] `Update.update` PollDone at `src/Client/Update.fs` lines 302–456 matches `SysMsg (PollDone (stateOpt, syncResponse, readyOpt, responseEventId))`.

[ ] PollDone default branch at `src/Client/Update.fs` line 355 sets `let si = SyncInfo.withSyncState Idle readyModel.syncInfo`.

[ ] PollDone arm at `src/Client/Update.fs` lines 430–433 matches `Some DataOutdated when events.IsEmpty || isAutoSyncBlocked readyModel` and returns `{ readyModel with syncInfo = SyncInfo.withSyncState DataOutdated si }`.

[ ] PollDone arm at `src/Client/Update.fs` lines 434–453 matches `Some DataOutdated` with a non-blocked apply path via `SyncLogic.applySyncResponse`.

[ ] PollDone Uploading arm at `src/Client/Update.fs` lines 334–336 gates apply on `not (isAutoSyncBlocked readyModel)`.

[ ] `UpdateHelpers.isAutoSyncBlocked` at `src/Client/UpdateHelpers.fs` lines 266–282 returns true when `not model.syncInfo.pending.IsEmpty` at lines 267–268, or when `Editing _` mode and `readEditInputValue () <> graphText` at lines 270–281.

[ ] Tests call `applyTail` at `tests/Shared.Tests/SyncLogicTests.fs` lines 156–157 as `SyncLogic.applyServerTail events state`.

## Existing tests

[ ] `PersistFailed leaves the graph` in `tests/Shared.Tests/SyncLogicTests.fs` lines 1349–1368 calls `applyTail [ failed ] st`, expects `Ok afterFail`, asserts node `updateTime` and `persistState` match the before snapshot, and asserts `SyncLogic.persistFailedMessage [ failed ]` is `Some "disk full"`.

[ ] `NodePersisted on a normal node is rejected` in `tests/Shared.Tests/SyncLogicTests.fs` lines 1371–1390 calls `applyTail [ persisted ] st`, expects `Error msg` with message `"normal nodes do not have parse or persist state"` at lines 1387–1389, and asserts the graph `updateTime` stayed at `before` on the original `st` at line 1390.

[ ] `Notice in the reply keeps the confirmation echo` in `tests/Shared.Tests/AckReconcileTests.fs` lines 400–436 builds `confirmed = [ confirm pending []; notice ]` with `EventBody.Notice (Notice.NodePersisted ...)`, asserts `SyncLogic.isConfirmationEcho [ pending ] confirmed`, calls `SyncLogic.reconcileAck [ pending ] confirmed ...`, and expects `AckReconcile.Applied` via `expectApplied`.

[ ] `Poll answer chains the next Included residency Want` in `tests/Server.Tests/BrowserResidencyClientTests.fs` lines 81–103 calls `Update.update (SysMsg (PollDone (None, response, None, Some EventId.zero)))` on a residency model and asserts child node presence, next `Want`, and a `PollServer _` effect.

## Copy-from

[ ] `SyncLogic.applyLoggedNotice` at `src/Shared/SyncLogic.fs` lines 108–111 on `applyNoticeGraph` success stores the notice event id with `Ok({ next with eventId = event.id }, undid)` at line 111.

[ ] `SyncLogic.applyLoggedNotice` at `src/Shared/SyncLogic.fs` line 109 on `applyNoticeGraph` failure returns `Error msg` and leaves `st.eventId` at its incoming value.

[ ] `NoticeGraph.writeGraph` at `src/Shared/NoticeGraph.fs` lines 90–94 returns `Ok graph` unchanged for `Notice.PersistFailed _`, so `applyTail` already reaches the Ok path for PersistFailed notices.

[ ] `NoticeGraph.writeGraph` for `Notice.NodePersisted` on a normal node returns `Error` with `"normal nodes do not have parse or persist state"` from `GraphMutate.setPersistState` at `src/Shared/GraphMutate.fs` line 217 through `writePersisted` at `NoticeGraph.fs` lines 76–78.

## Seams

[ ] Public test seam `NoticeGraph.writeGraph` in `src/Shared/NoticeGraph.fs` line 82.

[ ] Public test seam `SyncLogic.applyServerTail` in `src/Shared/SyncLogic.fs` line 303 reaches private `applyLoggedNotice` and `applyNoticeGraph` through `applySyncResponse` and `foldProjectedEvents`.

[ ] Public test seam `SyncLogic.reconcileAck` in `src/Shared/SyncLogic.fs` line 545.

[ ] Public test seam `Gambol.Client.Update.update` in `src/Client/Update.fs` line 170 reaches the PollDone arm at line 302 and post path through `SysMsg (SubmitResponse ...)` at line 229.

[ ] Public test seam `Gambol.Client.UpdateHelpers.isAutoSyncBlocked` in `src/Client/UpdateHelpers.fs` line 266.

[ ] Private `SyncLogic.applyLoggedNotice` and `SyncLogic.applyNoticeGraph` are reachable only through `applyServerTail` or `applySyncResponse` in Shared tests.

[ ] Private `Update.applySubmitResponse` is reachable through `Update.update` with `SysMsg (SubmitResponse (submitted, response))`.

## Stop risks

[ ] Ticket [01 — Accept Notices](plan/client-never-fails/issues/01-accept-notices/01-accept-notices-ticket.md) Inputs name `src/Shared/NoticeGraph.fs` read-only; a fix inside `writeGraph` or `GraphMutate.setPersistState` needs a stop at `plan/client-never-fails/reports/01-accept-notices-stop.md`.

[ ] Ticket Outputs name only `src/Shared/SyncLogic.fs`, `src/Client/Update.fs`, and three test files; a PollDone fix that must change `UpdateHelpers.fs` or `SyncAnswer.fs` needs a stop report.

[ ] Post-reply notice apply inside `reconcileAck` stays in `src/Shared/SyncLogic.fs` per Outputs; moving notice replay into `SyncAnswer.applyChangeSuccess` would need a stop report.

[ ] Spec story 1 Accept Notices requires the Browser to take each Notice event id when the pending queue is empty; storing the id on `writeGraph` `Error` matches that story.

[ ] Spec story 1 Accept Notices requires a post reply to apply its Notices; `reconcileAck` notice replay before `dropNotices` in confirmation helpers matches that story.

[ ] Spec story 1 Accept Notices requires parse and persist status to keep Sync usable while typing; PollDone Idle when `isAutoSyncBlocked` is true with an empty pending queue matches that story.

## implement

[ ] Ticket [01 — Accept Notices](plan/client-never-fails/issues/01-accept-notices/01-accept-notices-ticket.md) Process step 2: today `applyNoticeGraph` at `SyncLogic.fs` line 98 returns `Error msg` and does not update `st.graph`; ticket wants `writeGraph` `Error` to leave the graph unchanged and still advance stored state for the notice event id.

[ ] Ticket Process step 2: today `applyLoggedNotice` at `SyncLogic.fs` line 109 returns `Error msg` on `applyNoticeGraph` failure; ticket wants `Ok({ st with eventId = event.id }, undid)` on `writeGraph` `Error` of a string with `st.graph` unchanged.

[ ] Ticket Process step 3: today `reconcileAck` at `SyncLogic.fs` lines 555–588 runs confirmation on `dropNotices confirmed` only; ticket wants each Notice in the post `confirmed` list applied to `state` (same path as poll tail notices) before `identityError` and suffix projection.

[ ] Ticket Process step 4: today PollDone at `Update.fs` lines 430–433 sets `DataOutdated` when `events.IsEmpty || isAutoSyncBlocked readyModel` under `Some DataOutdated`; ticket wants `Idle` in `si` when the pending queue is empty, including when `isAutoSyncBlocked readyModel` is true from a dirty edit box.

[ ] Ticket Process step 5: today `NodePersisted on a normal node is rejected` expects `applyTail` `Error`; ticket wants `Ok` with unchanged graph fields and `afterFail.eventId` equal to the notice event id.

[ ] New fact 1 writeGraph Error in `tests/Shared.Tests/SyncLogicTests.fs`: call `SyncLogic.applyServerTail` (or test helper `applyTail` at line 156) with one `EventBody.Notice` whose `writeGraph` returns `Error` (same `NodePersisted` on a normal node fixture as the existing test); assert `Ok` result, graph node fields match before, and `result.eventId` equals the notice event `id`.

[ ] New fact 2 post-reply Notice in `tests/Shared.Tests/AckReconcileTests.fs`: extend or mirror `Notice in the reply keeps the confirmation echo` at line 400; after `SyncLogic.reconcileAck` with trailing `Notice.NodePersisted`, assert applied `ClientSyncState.eventId` equals `serverEventId` and notice-side graph rules hold (graph unchanged when `writeGraph` errors on a normal node, or updated when the notice writes).

[ ] New fact 3 PollDone Idle in `tests/Server.Tests/BrowserResidencyClientTests.fs`: build a VM with empty `syncInfo.pending`, `Editing _` mode, dirty edit via the same hooks `isAutoSyncBlocked` uses, and `PollDone (Some DataOutdated, empty events response, ...)` or `(None, empty events, ...)`; call `Update.update` and assert `next.syncInfo.syncState` is `Idle`.

[ ] Changed test `NodePersisted on a normal node is rejected` in `SyncLogicTests.fs` lines 1383–1390: replace `Error` expectation with `Ok afterFail`, assert graph `updateTime` unchanged on the node, and assert `afterFail.eventId` equals `EventIdFixtures.storedId 3` from the fixture at line 1376.
