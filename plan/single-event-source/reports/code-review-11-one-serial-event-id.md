# Code review — 11 One serial event id

Independent review. Not approval. Ticket [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md) stays **Status:** `coded`.

Range: `origin/staging...HEAD` (three-dot). Tip `6016a926`. Base `c1e2c1b2`. Non-empty. 113 files. One commit: `Implement one serial EventId and client pending by submissionId.` [History.fs](src/Shared/History.fs) EventId shape is authorized for this ticket.

Mechanical scan (`python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`): FILE [History.fs](src/Shared/History.fs) 801→822; FILE [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs) 598→600. LONG: [SyncPlanner.fs](src/Shared/SyncPlanner.fs) L17 (107), L103 (101); [Database.fs](src/Server/Database.fs) L374 (116); [LazyLoadReconciliationServerTests.fs](tests/Server.Tests/LazyLoadReconciliationServerTests.fs) L379 (107); [StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs) L359 (123), L414 (126), L431 (130), L509 (122); [AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs) L294 (103); [BootCacheTests.fs](tests/Shared.Tests/BootCacheTests.fs) L19 (107); [DeleteOpsTests.fs](tests/Shared.Tests/DeleteOpsTests.fs) L487 (142), L504 (131); [ImportDocumentTests.fs](tests/Shared.Tests/ImportDocumentTests.fs) L282 (124); [ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) L2961 (107). No changed binding over 40 lines.

Alan locks applied: EventId private; only [EventLog.fs](src/Shared/EventLog.fs) uses `EventId.next`; only serialize uses fromJson/toJson; else `EventId.zero`. Client has no serial; pending is `EventId.zero` and match by `submissionId`; approve stamps server id; server merge / revised stream (ops inserted ahead of the client submission) rewinds/reapplies. Ticket phrase “On interject, rewind” means that merge path — not an API named Interject. Change→Ev: rename locals; Ev-taking APIs must not keep Change names unless they filter Change events. Labeled links `[label](path)`. History.fs EventId shape authorized. No GitHub PR for this report. Deleting `type Revision` is [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md).

## Standards

Range `origin/staging...HEAD` is not empty (113 files). Tip `6016a926`. Base `origin/staging` `c1e2c1b2`. Mechanical-scan lines are documented-standard hits.

**Hard documented violations**

File size ([fsharp-source.md](.agents/rules/fsharp-source.md): 400 lines; do not grow a file already over 400). EventId shape in [History.fs](src/Shared/History.fs) is authorized; the growth still hits the rule: FILE 801→822 (`Ev.fromJson`/`toJson` plus EventId helpers). [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs) FILE 598→600.

Long lines (same rule, 100 chars; added hunks only): [SyncPlanner.fs](src/Shared/SyncPlanner.fs) L17 (107) `tryStartSubmit`, L103 (101) `tryStartPoll`; [Database.fs](src/Server/Database.fs) L374 (116); [LazyLoadReconciliationServerTests.fs](tests/Server.Tests/LazyLoadReconciliationServerTests.fs) L379 (107); [StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs) L359 (123), L414 (126), L431 (130), L509 (122); [AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs) L294 (103); [BootCacheTests.fs](tests/Shared.Tests/BootCacheTests.fs) L19 (107); [DeleteOpsTests.fs](tests/Shared.Tests/DeleteOpsTests.fs) L487 (142), L504 (131); [ImportDocumentTests.fs](tests/Shared.Tests/ImportDocumentTests.fs) L282 (124); [ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) L2961 (107). No changed binding over 40 lines. Surgical under-100-line preference is not a fail ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

EventId serial ([core-api.md](.agents/rules/core-api.md) EventId serial; Alan lock: only EventLog uses `EventId.next`; only serialize uses `fromJson`/`toJson`; else `EventId.zero`): production `EventId.next` is only [EventLog.fs](src/Shared/EventLog.fs). JSON codecs are fine. Non-serialize `EventId.fromJson`: [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `let after = EventId.fromJson -1` mints a cursor; [Database.fs](src/Server/Database.fs) `eventId = EventId.fromJson revision`; [ResidentProjection.fs](src/Shared/ResidentProjection.fs) `eventId = EventId.fromJson revision`; [App.fs](src/Client/App.fs) `{ eventId = EventId.fromJson revision }`; [BootCacheStore.fs](src/Client/BootCacheStore.fs) `{ eventId = EventId.fromJson revision }`; [BootCache.fs](src/Shared/BootCache.fs) wraps `clientRevision` int math; [Api.fs](src/Server/Api.fs) `EventId.fromJson clientRev`.

Change→Ev names ([fsharp-source.md](.agents/rules/fsharp-source.md) rename locals to `event`/`events`; Alan lock: APIs that take Ev must not keep Change names unless they filter Change events): [App.fs](src/Client/App.fs) `let postChanges = SyncBatch.toWireBatch events`; [Update.fs](src/Client/Update.fs) `SubmitNetworkError (baseRev, changes, kind)` still names Ev list `changes` after [ViewModel.fs](src/Shared/ViewModel.fs) renamed the field to `events`; [ResidentProjection.fs](src/Shared/ResidentProjection.fs) `captureLoadResponse ... (changes: Ev list)`; [SyncLogic.fs](src/Shared/SyncLogic.fs) `applyLocalChange (event: Ev)` keeps a Change API name.

Markdown / planning: no consecutive-blank or bare-id hits ([markdown-writing.md](.agents/rules/markdown-writing.md), [refer-by-name.md](.agents/rules/refer-by-name.md)). Ticket and [project.md](plan/single-event-source/project.md) use `[label](path)`. [arch.md](plan/single-event-source/arch.md) keeps allowed `[[path]]` bare refs. Plan text still says “interject”; that wording is stale. The code is server merge / revised stream (`undoPendingGraph` / `consumeCatchUpPoll`). Stage stays `build`; ticket Status `coded`. `type Revision` is ticket 12, out of scope.

**Judgement smells (not hard)**

Middle Man: [EventJson.fs](src/Shared/EventJson.fs) `let encodePendingEvent (event: Ev) : IEncodable = encode event` and `let decodePendingEvent: Decoder<Ev> = decode`. [SyncBatch.fs](src/Shared/SyncBatch.fs) `let toWireBatch (events: Ev list) : Ev list = events`.

Duplicated Code: leftover Change construction `{ id = EventId.zero; submissionId = ...; ops = ... }` in [SyncLogic.fs](src/Shared/SyncLogic.fs) (`undoPendingGraph` / `projectSuffixes`) and [SyncPlanner.fs](src/Shared/SyncPlanner.fs) `extractChange` (same shape as `Ev.asChange`).

Primitive Obsession: Effects still carry `baseEventId: int` and `PollServer of eventId: int` after [ViewModelSync.fs](src/Shared/ViewModelSync.fs).

Shotgun Surgery across 113 files is the in-scope Revision→eventId rename; suppress (ticket wins). [EventTests.fs](tests/Shared.Tests/EventTests.fs) calls `EventId.next EventId.zero` to assert +1; production EventLog-only stays clean.

## Spec

Range `origin/staging...HEAD` is non-empty (113 files, tip `6016a926`). Spec is [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md). I did not run CI. Ticket “On interject, rewind” means the server merge / revised-stream path (ops inserted ahead of the client submission), not an API named Interject.

**(a) Missing or partial**

- “`Revision` type and `revision` fields become event id. JSON key is `"eventId"`.” Live doors moved: leftover `Change.id` is `EventId`, [State](src/Shared/History.fs) / Ev / ActorStart / [CoreChangesAccepted](src/Server/Core/CoreChanges.fs) use `eventId`, Core door is `getEventId`, State / Ev / Change JSON use `"eventId"`. Leftover adapters remain: [SavePrep.fs](src/Server/SavePrep.fs) still takes `getFileRevision: unit -> Async<Revision>`; [RouteRegistration.fs](src/Server/RouteRegistration.fs) still calls `EventId.toRevision`; [DocumentLoader.fs](src/Server/DocumentLoader.fs) still uses `EventId.ofRevision`; [BootCache.fs](src/Shared/BootCache.fs) `SnapshotRecord.revision` still encodes JSON `"revision"`. Deleting `type Revision` is out of scope ([12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md)).
- “CI stays green.” Not verified here.

**(b) Scope creep**

No extra product behavior. [History.fs](src/Shared/History.fs) EventId shape is authorized. Leftover `type Revision`, `EventId.ofRevision` / `toRevision`, and unused [EventId.fs](src/Shared/EventId.fs) stay for [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md). Plan/arch still say “interject”; that is old wording, not a Spec miss.

**(c) Looks implemented, looks wrong**

- “On approve, replace zero with the server-assigned id.” / “On interject, rewind.” Echo ACK calls [ClientHistory.approve](src/Shared/ClientHistory.fs). The merge / revised-stream path ([SyncLogic.reconcileExternalAck](src/Shared/SyncLogic.fs) / `consumeCatchUpPoll`) rewinds the graph via poll and sets `history = state.history`, so pending zeros are not stamped from the revised stream by `submissionId`.
- “Pending events use `EventId.zero` only.” `invertAs` writes `EventBody.Undo` / `Redo` `target` from `Ev.id` while that id is still zero. `approve` stamps envelope `id` only, not nested targets, if undo/redo runs before the original ACK.
- “Only serializing should use the fromJson/toJson functions. Any event not from these sources should have id 0.” [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `seedEventLog` uses `EventId.fromJson -1` as a get-all cursor (not a codec, not `EventId.next`, not zero).

## Summary

Standards: 27 hard findings (2 FILE growth, 14 LONG lines, 7 `EventId.fromJson` outside serialize, 4 Change-named Ev bindings), 3 judgement smells (Middle Man, Duplicated Code, Primitive Obsession); worst is `EventId.fromJson` used to mint or wrap ids outside serialize.

Spec: 4 findings (leftover revision adapters / IndexedDB `"revision"` JSON partial; merge / revised-stream path does not stamp zeros; undo/redo nested targets stay zero; `EventId.fromJson -1` cursor) plus CI not run; worst is the merge / revised-stream path leaving `EventId.zero` unstamped by `submissionId`.
