# Code review — 11 repair re-check

Independent re-check after repair. Not approval. Ticket [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md) stays **Status:** `coded`.

Range: `origin/staging...HEAD` (three-dot). Tip `75738008`. Base `c1e2c1b2`. Non-empty. 115 files. Two commits: `Implement one serial EventId and client pending by submissionId.` then `Repair 11: stamp merge History, Undo targets, EventId.beforeAll.` Repair-only delta vs `origin/cursor/one-serial-event-id-2e7c` is that second commit. Prior review: [code-review-11-one-serial-event-id.md](plan/single-event-source/reports/code-review-11-one-serial-event-id.md).

Mechanical scan (`python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`): BARE_ID [code-review-11-one-serial-event-id.md](plan/single-event-source/reports/code-review-11-one-serial-event-id.md) L25 [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md); FILE [History.fs](src/Shared/History.fs) 801→824; FILE [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs) 598→632. LONG: [LazyLoadReconciliationServerTests.fs](tests/Server.Tests/LazyLoadReconciliationServerTests.fs) L379 (107); [StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs) L359 (123), L414 (126), L431 (130), L509 (122); [DeleteOpsTests.fs](tests/Shared.Tests/DeleteOpsTests.fs) L487 (142), L504 (131); [ImportDocumentTests.fs](tests/Shared.Tests/ImportDocumentTests.fs) L282 (124); [ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) L2961 (107). No changed binding over 40 lines.

Alan locks applied: EventId private; only [EventLog.fs](src/Shared/EventLog.fs) uses `EventId.next`; only serialize or named HTTP/SQL codecs use fromJson/toJson; else `EventId.zero` / `EventId.beforeAll`. Client has no serial; pending is `EventId.zero`; match/stamp by `submissionId`; approve stamps server id; server merge / revised stream (ops inserted ahead of the client submission) rewinds and stamps. No Interject API. Change→Ev: rename locals; Ev-taking APIs must not keep Change names unless they filter Change events. Labeled links `[label](path)`. [History.fs](src/Shared/History.fs) EventId shape authorized. No GitHub PR for this report. Deleting `type Revision` is [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md).

Focused Shared tests (not CI): `ClientHistoryTests`, `SyncLogicTests`, `BootCacheTests`, `EventTests` — 87 passed.

## Standards

Range `origin/staging...HEAD` is not empty (115 files). Tip `75738008`. Base `origin/staging` `c1e2c1b2`. Mechanical-scan lines are documented-standard hits.

Prior mint / Change-name hits are gone. Production `EventId.next` is only [EventLog.fs](src/Shared/EventLog.fs). `EventId.fromJson` in [EventJson.fs](src/Shared/EventJson.fs), [Serialization.fs](src/Shared/Serialization.fs), [ApiResponseSerialization.fs](src/Shared/ApiResponseSerialization.fs), `Ev.fromJson`, [Database.fs](src/Server/Database.fs) `decodeProjectionEventId`, and [Api.fs](src/Server/Api.fs) `decodeQueryEventId` is wire/SQL decode, not mint. [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) uses `EventId.beforeAll`. [ResidentProjection.fs](src/Shared/ResidentProjection.fs) / [App.fs](src/Client/App.fs) / [BootCacheStore.fs](src/Client/BootCacheStore.fs) no longer mint. `applyLocalChange`, `postChanges`, and Ev-typed `changes` locals are renamed. `encodePendingEvent` is gone.

**Hard documented violations**

- [refer-by-name.md](.agents/rules/refer-by-name.md): [code-review-11-one-serial-event-id.md](plan/single-event-source/reports/code-review-11-one-serial-event-id.md) L25 names [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md) by number only (scan `BARE_ID`).
- [fsharp-source.md](.agents/rules/fsharp-source.md) 400-line file: [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs) 598→632 (already over; grew further on repair).
- Same rule, 100-char added lines (scan): [LazyLoadReconciliationServerTests.fs](tests/Server.Tests/LazyLoadReconciliationServerTests.fs) L379; [StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs) L359/414/431/509; [DeleteOpsTests.fs](tests/Shared.Tests/DeleteOpsTests.fs) L487/504; [ImportDocumentTests.fs](tests/Shared.Tests/ImportDocumentTests.fs) L282; [ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) L2961.
- [core-agent-behavior.md](.agents/rules/core-agent-behavior.md) orphans: [BootCache.fs](src/Shared/BootCache.fs) `clientRevision` has no callers after `clientEventId` took `foldLog`.

[History.fs](src/Shared/History.fs) 801→824: EventId private / `fromJson` / `toJson` / `beforeAll` / `Ev.fromJson`/`toJson` are authorized. Not scored as a file-growth fail. Repair dropped several prior LONG hits (`tryStartSubmit`, `tryStartPoll`, [Database.fs](src/Server/Database.fs) L374, [AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs) L294, [BootCacheTests.fs](tests/Shared.Tests/BootCacheTests.fs) L19). Surgical under-100-line preference is not a fail ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

**Judgement smells (not hard)**

Primitive Obsession (repo has `EventId`; Alan lock): `SubmitPendingBatch` / `WaitingToRetry` / `SubmitNetworkError` still `baseEventId: int`; `PollServer` / `LoadServer` / `LoadDone` / `applyLoadResponse` / `getPollOutcome` still `eventId: int`; `eventsAfter` still `snapshotRevision: int`.

Middle Man: [SyncBatch.fs](src/Shared/SyncBatch.fs) `let toWireBatch (events: Ev list) : Ev list = events` — identity; production callers are gone (tests still call it).

Duplicated Code: `Ev.asChange { event with id = EventId.zero }` in [SyncPlanner.fs](src/Shared/SyncPlanner.fs) `restorePending` and [SyncLogic.fs](src/Shared/SyncLogic.fs) `undoPendingGraph`. `projectSuffixes` uses `Op.makeChange` (leftover Change; name is fine).

Shotgun 115-file Revision→`eventId` rename: ticket wins; suppress. `type Revision` stays for [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md).

## Spec

Range `origin/staging...HEAD` is non-empty (115 files, tip `75738008`). Spec is [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md). Ticket “On interject, rewind” means the server merge / revised-stream path (ops inserted ahead of the client submission), not an API named Interject.

**CLOSED (prior worst items)**

1. Merge/revised-stream stamp — “On a server merge / revised event stream that inserts other server ops before what the client sent, rewind and stamp zeros from matching `submissionId`.” `consumeCatchUpPoll` sets `history = ClientHistory.approve events state.history` ([SyncLogic.fs](src/Shared/SyncLogic.fs)). Echo ACK still stamps in `reconcileAck`. `reconcileExternalAck` notes catch-up; Poll `catchUp` then approve-stamps by `submissionId`. No Interject API.
2. Nested Undo/Redo targets — “On approve, replace zero with the server-assigned id.” / “Pending events use `EventId.zero` only.” `approve` runs `stampEvent` then `stampBody` / `resolveTarget` ([ClientHistory.fs](src/Shared/ClientHistory.fs)). Zero `Undo`/`Redo` targets are filled from the confirmed stream (inverse-ops match). Tests: [ClientHistoryTests.fs](tests/Shared.Tests/ClientHistoryTests.fs), [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs).
3. `EventId.beforeAll` — “Only serializing should use the fromJson/toJson functions.” `EventId.beforeAll = EventId -1` ([History.fs](src/Shared/History.fs)); `EventLog.all` / `seedEventLog` use it ([EventLog.fs](src/Shared/EventLog.fs), [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs)). No `fromJson -1`. Remaining `fromJson` is Ev/Change/EventJson codecs plus named HTTP/SQL `decodeQueryEventId` / `decodeProjectionEventId`. Production `EventId.next` is only [EventLog.fs](src/Shared/EventLog.fs).
4. BootCache — “`Revision` type and `revision` fields become event id. JSON key is `"eventId"`.” `SnapshotRecord.eventId: EventId`; encode/decode and IndexedDB key `"eventId"` ([BootCache.fs](src/Shared/BootCache.fs), [BootCacheStore.fs](src/Client/BootCacheStore.fs)).

**(a) Missing or partial**

None versus What to build on the four focus items. Leftover `type Revision` / `ofRevision` / `toRevision` / bookkeeping (`getFileRevision`, [DocumentLoader.fs](src/Server/DocumentLoader.fs) `EventId.ofRevision`, [RouteRegistration.fs](src/Server/RouteRegistration.fs) `EventId.toRevision`) stay for [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md). “CI stays green.” Not run here. Focused Shared tests above are green.

**(b) Scope creep**

No extra product behavior. `tryPeekUndoEvent` / `tryPeekRedoEvent` and `EventLog.all` are stamp/cursor helpers. [History.fs](src/Shared/History.fs) EventId shape is authorized.

**(c) Looks implemented, looks wrong**

None on the four items. Envelope stamp is by `submissionId`; nested targets use inverse-ops against the confirmed list, which is what the repair tests cover.

## Summary

Standards: 12 hard findings remain (1 BARE_ID, 1 FILE growth, 9 LONG lines, 1 unused `clientRevision`); 3 judgement smells (Primitive Obsession, Middle Man, Duplicated Code). Worst remaining: [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs) FILE 598→632. Prior worst (`EventId.fromJson` mint outside serialize) is closed.

Spec: 4 of 4 prior worst items closed; 0 still open. Worst: none.
