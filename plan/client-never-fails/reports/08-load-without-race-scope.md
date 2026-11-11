# 08 — Load without a race — scope

Project: [Client never fails on server data](plan/client-never-fails/spec.md). Story 11 — Load without a race. Architecture path 9 — Load without a race in [arch.md](plan/client-never-fails/arch.md). Ticket: [08 — Load without a race](plan/client-never-fails/issues/08-load-without-race/08-load-without-race-ticket.md).

### Signatures

[ ] `SyncLogic.applySyncResponse` in `src/Shared/SyncLogic.fs` has signature `(response: SyncResponse) -> (state: ClientSyncState) -> Result<ClientSyncState, string>`.
[ ] `SyncLogic.applyLoadResponse` in `src/Shared/SyncLogic.fs` has signature `(responseEventId: EventId) -> (hasPendingLocal: bool) -> (response: SyncResponse) -> (state: ClientSyncState) -> Result<ClientSyncState, string>`.
[ ] `Update.update` in `src/Client/Update.fs` handles `SysMsg (LoadDone (stateOpt, syncResponse, responseRevision, readyOpt))`.
[ ] `UpdateHelpers.clientSyncState` in `src/Client/UpdateHelpers.fs` has signature `(model: VM) -> ClientSyncState`.
[ ] `UpdateActorLive.withAppliedResult` in `src/Client/UpdateActorLive.fs` has signature `(events: Ev list) -> (state: ClientSyncState) -> (syncInfo: SyncInfo) -> (model: VM) -> VM`.

### Match sites

[ ] `applyLoadResponse` in `src/Shared/SyncLogic.fs` filters substantive events with `isNoticeEvent`, sets `answerOnly` when substantive is empty and `response.nodes` is non-empty, and on `answerOnly && (hasPendingLocal || responseEventId <> state.eventId)` returns `Error "raced Load answer"`; the else arm calls `applySyncResponse response state`.
[ ] `SyncLogic.isNoticeEvent` in `src/Shared/SyncLogic.fs` is private; it marks `EventBody.Notice` events as non-substantive for the guard.
[ ] `Update.update` `LoadDone` arm in `src/Client/Update.fs` builds `hasPendingLocal` from `syncInfo.pending` and `Sending` / `WaitingToRetry`, then on applicable branches calls `SyncLogic.applyLoadResponse responseRevision hasPendingLocal syncResponse (clientSyncState readyModel)`; `Error _` sets `DataOutdated` and leaves the graph; `Ok newState` uses `withAppliedResult`, `withSiteMap`, and `adjustModeAfterServerApply`.
[ ] `Update.update` `PollDone` arms in `src/Client/Update.fs` call `SyncLogic.applySyncResponse` directly (for example `applyPollDataOutdated`, Uploading `DataOutdated`, catch-up empty events, and default `None` stateOpt paths).
[ ] `Program.applyBootNovel` in `src/Client/Program.fs` calls `SyncLogic.applySyncResponse` for boot poll apply.
[ ] No other production caller invokes `applyLoadResponse`; only `LoadDone` and Shared tests use it.

### Existing tests

[ ] ``applyLoadResponse answer-only with pending is raced Load answer`` in `tests/Shared.Tests/SplitOriginTraceTests.fs` builds a split graph with pending split ops, sends nodes-only `SyncResponse`, calls `applyLoadResponse` with `hasPendingLocal = true`, and asserts `Error` message equals `"raced Load answer"`. Public seam: `SyncLogic.applyLoadResponse`.
[ ] ``applyLoadResponse ignores a Notice when guarding a raced Load answer`` in `tests/Shared.Tests/SplitOriginTraceTests.fs` is the same setup with a `NodePersisted` Notice in `events` plus nodes-only answer; asserts `Error "raced Load answer"`. Public seam: `SyncLogic.applyLoadResponse`.
[ ] ``answer-only Load at the same event id preserves ClientHistory`` in `tests/Shared.Tests/ClientHistoryRuntimeTests.fs` calls `applyLoadResponse` with matching event id, `hasPendingLocal = false`, empty events, workspace node in `nodes`; asserts `Ok` preserves `history`, event id, and `Loaded` children status. Public seam: `SyncLogic.applyLoadResponse` (closed path; ticket leaves this test unchanged).
[ ] ``answer-only Load at the same event id refuses pending local work`` in `tests/Shared.Tests/ClientHistoryRuntimeTests.fs` calls `applyLoadResponse` with matching event id and `hasPendingLocal = true`; asserts `Error` and message contains `"raced"`. Public seam: `SyncLogic.applyLoadResponse`.
[ ] ``answer-only Load refuses an event id mismatch`` in `tests/Shared.Tests/ClientHistoryRuntimeTests.fs` calls `applyLoadResponse` with response event id 5 and state event id 4, `hasPendingLocal = false`; asserts `Error` and message contains `"raced"`. Public seam: `SyncLogic.applyLoadResponse`.

### Copy-from

[ ] `SyncLogic.applySyncResponse` in `src/Shared/SyncLogic.fs` is the apply Load should use once the raced Load guard is removed; it folds projected events, rewinds pending before Want when needed, runs `graphAfterWant`, and may `reapplyLeftoverPending`.
[ ] `applyPollDataOutdated` in `src/Client/Update.fs` is the LoadDone success pattern copy: `applySyncResponse` on `clientSyncState`, `Error _` → `DataOutdated`, `Ok newState` → `withAppliedResult` with `withSiteMap` and `adjustModeAfterServerApply`.

### implement

[ ] Input `SyncLogic.applyLoadResponse` returns `Error "raced Load answer"` for nodes-only with pending or event-id mismatch: matches current code in `src/Shared/SyncLogic.fs`.
[ ] Input `Update.update` matches `LoadDone`: matches current code in `src/Client/Update.fs`.
[ ] Input the guard treats pending and event-id mismatch as the same `Error` string: matches the single `if answerOnly && (hasPendingLocal || responseEventId <> state.eventId)` branch.
[ ] Input other replies go through `applySyncResponse`: matches Poll and boot paths; Load goes through `applyLoadResponse` first (which delegates to `applySyncResponse` only when the guard does not fire).
[ ] Input the two raced-answer facts in `SplitOriginTraceTests.fs`: match current tests and assertions.
[ ] Input the two answer-only Load refuse facts in `ClientHistoryRuntimeTests.fs`: match current tests and assertions.
[ ] Input `SyncLogic.applySyncResponse` is the apply the Load reply uses after the guard is gone: does not match current code today; `applyLoadResponse` still owns the guard and is the LoadDone entry point.
[ ] Ticket claim after edit: `applyLoadResponse` applies every reply through `applySyncResponse` with no `raced Load answer` guard; differs from current guarded implementation.
[ ] Ticket claim after edit: `LoadDone` still calls `applyLoadResponse` (per Inputs); worker may simplify the call if `hasPendingLocal` and `responseRevision` become unused parameters—any signature change is extra scope beyond the ticket Outputs list.
[ ] Ticket claim after edit: ``applyLoadResponse answer-only with pending is raced Load answer`` must observe apply success (nodes installed, pending handled per `applySyncResponse`) instead of `Error "raced Load answer"` at public seam `SyncLogic.applyLoadResponse`.
[ ] Ticket claim after edit: ``applyLoadResponse ignores a Notice when guarding a raced Load answer`` must observe Notice plus nodes-only Load applies like `applySyncResponse` (no raced refusal) at public seam `SyncLogic.applyLoadResponse`.
[ ] Ticket claim after edit: ``answer-only Load at the same event id refuses pending local work`` must observe `Ok` with graph or sync state updated per `applySyncResponse` rules, not raced refusal, at public seam `SyncLogic.applyLoadResponse`.
[ ] Ticket claim after edit: ``answer-only Load refuses an event id mismatch`` must observe apply through `applySyncResponse` (Want install and event tail) instead of raced refusal at public seam `SyncLogic.applyLoadResponse`.
[ ] Closed fact ``answer-only Load at the same event id preserves ClientHistory`` already passes through `applyLoadResponse` → `applySyncResponse` on the happy path; behavior should stay consistent after the guard removal.
[ ] Architecture path 9 — Load without a race, step 3, says `Update` LoadDone uses that apply. Today LoadDone uses `applyLoadResponse`, which will become a thin delegate to `applySyncResponse`.
