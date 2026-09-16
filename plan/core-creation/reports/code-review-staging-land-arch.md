# Code review — staging land vs arch.md

Range: uncommitted index vs `HEAD` (`git diff HEAD`); tree equals `staging` tip (issues 43–45 Event migrate/contract land).
Spec: [[../arch.md]].
Fixed point: current `dev` tip before land (`persist handlers`).

## Standards

### Hard violations

**[[.agents/rules/refer-by-name.md]]** — bare id  
- [44 — …](../issues/44-migrate-browser-poll-history-pending-and-eventid.md):48 — “conflicts with issue 43” (no name).

**[[.agents/rules/fsharp-source.md]] — Don’t use mutable**  
- [EventLogFile.fs](../../../src/Server/EventLogFile.fs):62 `let mutable totalRead = 0`  
- same:76 `let mutable b = stream.ReadByte()`  
(new via ChangeLog absorb)

**[[.agents/rules/fsharp-source.md]] — Don’t use Exceptions**  
- [EventLogFile.fs](../../../src/Server/EventLogFile.fs):81 `Int32.Parse(...)` (throwing) in new `readEntryAt`.

**[[.agents/rules/fsharp-source.md]] — 100 chars or less** (new/changed lines)  
- [SyncBatch.fs](../../../src/Shared/SyncBatch.fs):6 (118), :17 (111)  
- [SyncLogic.fs](../../../src/Shared/SyncLogic.fs):333 (105)  
- [SyncPlanner.fs](../../../src/Shared/SyncPlanner.fs):17 (115), :112 (109)  
- [Serialization.fs](../../../src/Shared/Serialization.fs):432 (103)  
- [FileAgent.fs](../../../src/Server/Core/FileAgent.fs):108 (102)  
- Tests: [FileAgentFailureTests](../../../tests/Server.Tests/FileAgentFailureTests.fs):315; [StateEndpointTests](../../../tests/Server.Tests/StateEndpointTests.fs):307; [DeleteOpsTests](../../../tests/Shared.Tests/DeleteOpsTests.fs):487,:504; [ImportDocumentTests](../../../tests/Shared.Tests/ImportDocumentTests.fs):282; [SerializationTests](../../../tests/Shared.Tests/SerializationTests.fs):290,:298

**[[.agents/rules/fsharp-source.md]] — 400 lines; don’t grow if already over**  
- [SyncLogic.fs](../../../src/Shared/SyncLogic.fs) 402→431  
- [LazyLoadReconciliationServerTests.fs](../../../tests/Server.Tests/LazyLoadReconciliationServerTests.fs) 802→805  
- [StateEndpointTests.fs](../../../tests/Server.Tests/StateEndpointTests.fs) 1231→1232  
- [SerializationTests.fs](../../../tests/Shared.Tests/SerializationTests.fs) 442→464  
- [SyncLogicTests.fs](../../../tests/Shared.Tests/SyncLogicTests.fs) 583→584

**[[.agents/rules/core-agent-behavior.md]] — Surgical / own orphans**  
- [EventLogFile.fs](../../../src/Server/EventLogFile.fs):11–12 — unused `Encode` / `Decode` aliases added.

Bindings measured ≤40 lines — no hit. No core-api Adapter/Core boundary breach spotted in the Event migrate hunks.

### Judgement (smells)

- **Shotgun Surgery** — Event/Change rename across ~71 files (migrate-shaped; expected).  
- **Duplicated Code** — header/`%08d`+json+newline built in both `appendEntry` and `appendEntries`.  
- **Parameter Explosion / Primitive Obsession** — `graphOnly: bool` threaded on `persist` / dispatch in [CoreEventDispatch.fs](../../../src/Server/Core/CoreEventDispatch.fs).

## Spec

**Note:** Many arch checkboxes for this land are still `[ ]` while code exists (stale boxes, not failures). Compared to requirement text.

### (a) Missing / partial

- **§1 Story Caller, persist, and Poll → Migrate 8** — “client holds EventLog of the same type… Do not migrate onto a module named History.” `ClientSyncState.eventLog` is added but never restored/updated from Poll/ack (`SyncLogic` only declares the field; no `EventLog.restore` on consume). Client constructors in `Update.fs` / `Program.fs` are outside this diff and do not wire `eventLog`.
- **Same hop** — “`ClientHistory.undo` locally then name-only submit.” Local undo rebuilds `Undo`/`Redo` with full inverse `ops` and posts them (`SyncLogic.applyInverse`); wire JSON requires `ops` (`EventJson.decodeUndoBody`). Server can fill empty ops (`CoreEventDispatch.completeAction`), but the Browser path does not submit name-only.
- **§1 Migrate 6** — “`Revision` → EventId cursor (`State.revision`, `ClientSyncState.revision`).” Shared `State`/`ClientSyncState` move to `EventId`, but `VM.revision` stays `Revision` and most client sync call sites are unchanged in this tree.
- **§2 Module ClientHistory Interface 1–2** — “`record commandName event`”; “`undo` / `redo` … produce the Undo/Redo Event.” Production path still uses Change-shaped `ClientHistory.record` / `undo` / `redo`; Event APIs are `recordEvent` / `undoEvent` / `redoEvent` only.

### (b) Scope creep

- **§2 HTTP Adapter** does not ask for a second POST route: `/ambit/events` aliases `/changes` (`RouteRegistration`).
- **§1** does not ask to bump the protocol marker: `ApiVersion.current` `1` → `11`.
- **§1 Contract 3** asks to drop the ChangeLog name; `Database` also `DROP TABLE IF EXISTS changes` and creates `events` (schema cutover beyond rename).

### (c) Implemented but wrong vs arch

- **§1 Migrate 1 / §2 HTTP Adapter Interface 4–5** — Change posts call `postEvents`, Poll/Load return `events`, but post ack sets `events = batch.events` (request echo). `CoreChangesAccepted` still carries `changes: Change list`, so ack is not the stored/completed Event (stamped `authority`, filled Undo ops, assigned `id`).
- **§1 Migrate 6 / Chosen Event destination** — “poll cursor is EventId”; `State.revision` is `EventId`, yet `FileAgent` still does `revision = Revision nextRev` / `.Value` (Revision stamp into an EventId field).
- **§4 Chosen Event destination** — “`postEvent` is the Changes door. Payload is Event.” HTTP is on Events, but `CoreChanges` keeps a parallel Change-list `postChange` door used by persist/dispatch and Actor paths.

## Summary

Standards: ~20 hard hits (worst: mutable + throwing parse in `EventLogFile`, SyncLogic grown past 400). Spec: 4 missing/partial, 3 scope-creep, 3 wrong (worst: post ack echoes request Events; FileAgent still stamps `Revision` into `EventId`).
