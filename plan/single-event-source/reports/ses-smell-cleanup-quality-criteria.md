# SES smell-cleanup quality criteria

Lasting accept/reject bars for a later smell-cleanup implement ticket after the SES Spec path is okay. Question: [20 — SES smell-cleanup quality criteria](plan/single-event-source/issues/20-ses-smell-cleanup-quality-criteria.md).

Each item names what to look for and what **fixed** means. A later implement ticket applies these bars. This file does not rewrite [13 — Revision always 0 (diagnostic)](plan/single-event-source/issues/13-revision-always-zero.md)–[19 — Delete unused EventId.fs](plan/single-event-source/issues/19-delete-unused-eventid-fs.md) and does not reopen Spec for [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md) or [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md).

## Sources

Rules (own the policy):

1. [fsharp-source.md](.agents/rules/fsharp-source.md) — Ev locals after leftover Change remap: `event` / `events`; keep `ev` / `evs` only when that file already uses them; do not leave `change` / `changes` bound to Ev.
2. [core-api.md](.agents/rules/core-api.md) EventId serial — only [EventLog.fs](src/Shared/EventLog.fs) calls `EventId.next`; `fromJson` / `toJson` only when serializing; any Event not from those sources has id 0; `EventId.beforeAll` is the get-all cursor and is not `fromJson`; Client and Shared mint drafts with `EventId.zero` or rebuild from the wire (`Ev.fromJson` / `Ev.toJson`).
3. [core-agent-behavior.md](.agents/rules/core-agent-behavior.md) — surgical edits; unused bindings that a cleanup change introduces must be removed. File length and function length in [fsharp-source.md](.agents/rules/fsharp-source.md) are **not** accept bars here (see [Out of scope](#out-of-scope)).
4. [CONTEXT.md](CONTEXT.md) — Event (`Ev`), event id (`EventId`), Change (`EventBody.Change` of an Op list), Authority, EventLog. Revision is a retired name.

Starting reports (entry points; each claim below was checked in current `src/` / `tests/`):

1. [code-review-11-one-serial-event-id.md](code-review-11-one-serial-event-id.md)
2. [code-review-11-repair.md](code-review-11-repair.md)
3. [code-review-12-contract-leftover-change-and-revision.md](code-review-12-contract-leftover-change-and-revision.md)
4. [code-review-34b-outside-core-lifecycle-proof.md](plan/core-creation/reports/code-review-34b-outside-core-lifecycle-proof.md) — in scope for SES remaps, EventId usage, and naming. That file is not on this Project tip; the independent 34b review commit that added it was read, then the hits were checked in current code.

Alan locks on [20 — SES smell-cleanup quality criteria](plan/single-event-source/issues/20-ses-smell-cleanup-quality-criteria.md) §3–4: naming; improper type usage (`EventId.fromJson` / `toJson` outside serialize or named wire/SQL peel; Primitive Obsession `int` vs `EventId`; drafts `EventId.zero` / `EventId.beforeAll`; wrong Authority stamps). Not file or function length as accept bars, except a function a follow-on cleanup ticket actually edits may be brought in line when touched.

## Out of scope

These are **not** accept/reject bars for smell-cleanup, and must not be re-litigated as such:

1. **File length** — do not use ≤400 lines as a pass/fail bar. [fsharp-source.md](.agents/rules/fsharp-source.md) still owns ordinary surgical judgment on files a cleanup ticket edits.
2. **Function length** — do not use ≤40 lines as a pass/fail bar, except a function that ticket actually edits may be brought in line when touched. Same source.
3. **Closed 11/12 Spec** — merge/revised-stream stamp by `submissionId`, nested Undo/Redo target stamp, `EventId.beforeAll` instead of `fromJson -1`, BootCache `"eventId"`, delete leftover Change record / `module Change` / `Ev.ofChange` / `Ev.asChange`, delete `type Revision` / `ofRevision` / `toRevision`, delete unused [EventId.fs](src/Shared/EventId.fs). Those are closed on the historical lands ([code-review-11-repair.md](code-review-11-repair.md), [code-review-12-contract-leftover-change-and-revision.md](code-review-12-contract-leftover-change-and-revision.md)). Do not reopen them here.
4. **Redo What to build** — [13 — Revision always 0 (diagnostic)](plan/single-event-source/issues/13-revision-always-zero.md)–[19 — Delete unused EventId.fs](plan/single-event-source/issues/19-delete-unused-eventid-fs.md) stay as written. Overlap is noted at the end. This checklist does not change those tickets.
5. **34b non-SES Standards** — mutable, Exceptions, unused `actorCaller` in TestActor tests ([code-review-34b-outside-core-lifecycle-proof.md](plan/core-creation/reports/code-review-34b-outside-core-lifecycle-proof.md)). Not naming or EventId type usage. Leave them to core-creation review. Post-SES remaps (Change → Ev, ActorStarted → `EventBody.ActorStart`, mailbox History → EventLog) are fulfillment, not defects.

## 1. Naming

### 1.1 Ev bindings after leftover Change remap

**Look for:** A local, parameter, or field typed `Ev` or `Ev list` still named `change` / `changes` / `rev` / `revision`, or an API that takes Ev and keeps a Change name when it does not mint or filter `EventBody.Change`. [fsharp-source.md](.agents/rules/fsharp-source.md); Alan lock on [code-review-11-one-serial-event-id.md](code-review-11-one-serial-event-id.md) and [code-review-12-contract-leftover-change-and-revision.md](code-review-12-contract-leftover-change-and-revision.md).

**Fixed:** Those bindings are `event` / `events` (or `ev` / `evs` only if that file already uses them). Production hits from the 11 review (`applyLocalChange`, `postChanges`, Ev-typed `changes`) are already renamed at this tip ([code-review-11-repair.md](code-review-11-repair.md)). Remaining illustrative hits: [FileAgent.fs](src/Server/Core/FileAgent.fs) and [DbAgent.fs](src/Server/Core/DbAgent.fs) `ackChanges` (`Ev list`); [ApiResponses.fs](src/Shared/ApiResponses.fs) `ChangeSuccessResponse.changes` / `LoadResponse.changes` / `SyncResponse.changes` (aliases over `events`, not JSON keys).

### 1.2 KEEP names that mean EventBody.Change or HTTP

**Look for:** A rename that would hide `EventBody.Change` of an Op list, or that would rename an HTTP `/changes` door or `postChange` route helper. [CONTEXT.md](CONTEXT.md) Change; KEEP lock on [code-review-12-contract-leftover-change-and-revision.md](code-review-12-contract-leftover-change-and-revision.md).

**Fixed:** Those names stay. Illustrative KEEP: [ClientHistory.fs](src/Shared/ClientHistory.fs) `mintChange` (returns Ev with `EventBody.Change`); [ImportText.fs](src/Shared/ImportText.fs) `buildImportChange` / `buildDirectoryMergeChange`; [SpecialNodeTestHelpers.fs](tests/Shared.Tests/SpecialNodeTestHelpers.fs) `changeEvent` / `changeEventZero` / `applyChange` when they mean that body; HTTP `POST /changes` and `Api.postEvents` alias routes.

### 1.3 Mysterious leftover Revision names on EventId

**Look for:** A function, parameter, or field whose type is `EventId` (or that returns `EventId`) still named `revision` / `rev` / `readRevision` / `writeRevision` / `getFileRevision` / `decodeRevision` after `type Revision` is gone. [CONTEXT.md](CONTEXT.md) event id; Mysterious Name in [code-review-12-contract-leftover-change-and-revision.md](code-review-12-contract-leftover-change-and-revision.md).

**Fixed:** The name says event id (`readEventId`, `writeEventId`, `getFileEventId`, `eventId`). The value stays `EventId`. Illustrative hits: [Bookkeeping.fs](src/Server/Bookkeeping.fs) `readRevision : EventId` and `writeRevision (rev: int)`; [SavePrep.fs](src/Server/SavePrep.fs) `getFileRevision: unit -> Async<EventId>`; [Database.fs](src/Server/Database.fs) `decodeProjectionEventId (revision: int)` (peel name + leftover `revision`).

### 1.4 Comment or log strings that still say Revision for EventId

**Look for:** A public API comment, error, or log that calls an `EventId` value Revision, after the SES remap. Not a SQL column that has not been migrated. [CONTEXT.md](CONTEXT.md) Avoid: Revision.

**Fixed:** Spoken and API text say event id. SQL column `graph.revision` may stay until a storage migrate ticket; the in-process peel name must still follow [1.3 Mysterious leftover Revision names on EventId](#13-mysterious-leftover-revision-names-on-eventid). Illustrative: [App.fs](src/Client/App.fs) log `baseRev=` next to `baseEventId: EventId`; [ViewModelSync.fs](src/Shared/ViewModelSync.fs) comments “settled revision”.

## 2. EventId peel vs mint

### 2.1 fromJson / toJson only at named peel

**Look for:** `EventId.fromJson` or `EventId.toJson` outside a named serialize, HTTP query, SQL, file, or IndexedDB peel. [core-api.md](.agents/rules/core-api.md); Alan lock on [code-review-11-repair.md](code-review-11-repair.md) and [code-review-12-contract-leftover-change-and-revision.md](code-review-12-contract-leftover-change-and-revision.md).

**Allowed peel (current tip, verified):** [History.fs](src/Shared/History.fs) `Ev.fromJson` / `Ev.toJson`; [EventJson.fs](src/Shared/EventJson.fs) `encodeEventId` / `decodeEventId`; [ApiResponseSerialization.fs](src/Shared/ApiResponseSerialization.fs); [Api.fs](src/Server/Api.fs) `decodeQueryEventId`; [Bookkeeping.fs](src/Server/Bookkeeping.fs) parse of `SYSTEM/gambol.meta`; [Database.fs](src/Server/Database.fs) `decodeProjectionEventId`; [BootCacheStore.fs](src/Client/BootCacheStore.fs) IndexedDB `"eventId"`; [DbAgent.fs](src/Server/Core/DbAgent.fs) `EventId.value` into [Database.fs](src/Server/Database.fs) `appendEvent (eventId: int)`.

**Fixed:** Production has no other `fromJson` / `toJson`. A new peel is a named `decode*` / `encode*` at the wire, SQL, file, or IndexedDB edge — not an in-process constructor. Tests that decode JSON or SQL may call the same peels. Tests that mint an unpublished Ev do not (see [2.2 Drafts use EventId.zero](#22-drafts-use-eventidzero)).

### 2.2 Drafts use EventId.zero

**Look for:** A newly constructed unpublished Ev, or `ActorStart.eventId` on a draft start, minted with `EventId.fromJson 0` (or any other `fromJson`). [core-api.md](.agents/rules/core-api.md) “Any event not from these sources should have id 0”; [code-review-34b-outside-core-lifecycle-proof.md](plan/core-creation/reports/code-review-34b-outside-core-lifecycle-proof.md); production already uses `EventId.zero` in [ClientHistory.fs](src/Shared/ClientHistory.fs), [ImportText.fs](src/Shared/ImportText.fs), [GraphOnlyChangePost.fs](src/Server/GraphOnlyChangePost.fs), [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `lifecycleEvent` / `persistNew`.

**Fixed:** Drafts and pending posts use `EventId.zero`. `EventId.fromJson 0` is gone from those sites. Illustrative remaining hits: [TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs) `EventId.fromJson 0` on Ev and `ActorStart.eventId`; many Shared/Server fixtures still mint `{ id = EventId.fromJson 0` ([code-review-12-contract-leftover-change-and-revision.md](code-review-12-contract-leftover-change-and-revision.md)). A fixture that stands for an **assigned** serial may use `EventId.fromJson n` (`n` ≠ 0) only as a peel of that integer (codec test or named helper).

### 2.3 Get-all cursor is EventId.beforeAll

**Look for:** `EventId.fromJson -1`, a raw `-1` cursor, or a comment that treats `-1` as a serialized id. [core-api.md](.agents/rules/core-api.md); [History.fs](src/Shared/History.fs) `beforeAll` (“Not a serialized id”); [code-review-11-repair.md](code-review-11-repair.md) closed `fromJson -1` in production.

**Fixed:** Get-all / seed / restore-from-empty uses `EventId.beforeAll`. Peel to int only at SQL: `EventId.toJson EventId.beforeAll` (or `EventId.value`), never a bare `-1` in Core. Production seed is already `persist.getEventsSince EventId.beforeAll` ([CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs)). Illustrative remaining hit: [DbAgent.fs](src/Server/Core/DbAgent.fs) `Database.getEventsAfter connectionString (-1)`.

### 2.4 EventId.next only in EventLog (production)

**Look for:** `EventId.next` outside [EventLog.fs](src/Shared/EventLog.fs) in `src/`. [core-api.md](.agents/rules/core-api.md); [code-review-11-repair.md](code-review-11-repair.md).

**Fixed:** Production `next` stays only in EventLog (`empty`, `append`, `restorePersisted`). A Shared/Client caller does not advance the serial. [EventTests.fs](tests/Shared.Tests/EventTests.fs) may call `EventId.next EventId.zero` to assert +1; that is not a production mint.

## 3. Primitive Obsession (int vs EventId)

### 3.1 In-process event id is EventId

**Look for:** An in-process field or parameter typed `int` whose meaning is event id (`eventId`, `baseEventId`, `revision`, `clientRev`, `snapshotRevision`, `afterEventId`). [code-review-11-repair.md](code-review-11-repair.md) Primitive Obsession; [CONTEXT.md](CONTEXT.md) event id.

**Fixed:** The in-process type is `EventId`. Compare and pass `EventId` values. Use `EventId.value` / `toJson` only at a peel. 11-repair listed `SubmitPendingBatch` / `WaitingToRetry` / `PollServer` / `LoadServer` as `int`; those effects are `EventId` at this tip ([ViewModelSync.fs](src/Shared/ViewModelSync.fs)). Illustrative remaining hits: [BootCache.fs](src/Shared/BootCache.fs) `shouldTruncate (snapshotRevision: int) (clientRev: int)` while `eventsAfter` / `clientEventId` already take `EventId`; [FileAgent.fs](src/Server/Core/FileAgent.fs) `syncPersistChange (rev: int)`; [Database.fs](src/Server/Database.fs) `tryLoadGraphFromProjection` returning `Graph * int` then `decodeProjectionEventId`; [DatabaseProjection.fs](src/Server/DatabaseProjection.fs) `plan (revision: int)`.

### 3.2 Int stays at the peel only

**Look for:** An `int` that is the JSON number, query string, SQL column, or file text of an event id, then immediately wrapped or unwrapped. That is a peel, not Primitive Obsession. [core-api.md](.agents/rules/core-api.md); [Api.fs](src/Server/Api.fs) `decodeQueryEventId (clientRev: int)`; [Database.fs](src/Server/Database.fs) `appendEvent (eventId: int)`.

**Fixed:** The `int` does not leak past the named peel. Callers hold `EventId` and pass `EventId.value` / `EventId.toJson` in. The peel name should still follow [1.3 Mysterious leftover Revision names on EventId](#13-mysterious-leftover-revision-names-on-eventid) (`clientRev` → `clientEventId` at the query parse, then `fromJson`).

## 4. Authority stamps

### 4.1 Draft Authority matches the posting Caller

**Look for:** A newly constructed Ev that claims one Authority kind, then is posted under a different Caller, relying on Core overwrite. [CONTEXT.md](CONTEXT.md) Authority; [code-review-34b-outside-core-lifecycle-proof.md](plan/core-creation/reports/code-review-34b-outside-core-lifecycle-proof.md) Mysterious Name; [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `prepare` sets `authority = eventAuthority caller.authority`.

**Fixed:** The minted `event.authority` matches the `Caller.authority` that posts it. Core may still stamp from Caller (that is the admit path). Do not leave a known-wrong kind on the draft. Illustrative hit: [TestActor.fs](tests/Server.Tests/TestActor.fs) `hello` sets `Authority "Browser"` then `asCaller` with `Authority "Actor"`. Browser-minted drafts that a Browser Caller posts ([ClientHistory.fs](src/Shared/ClientHistory.fs) `mintChange`, [ImportText.fs](src/Shared/ImportText.fs)) are already correct. Parse-minted drafts that a Parse Caller posts ([GraphOnlyChangePost.fs](src/Server/GraphOnlyChangePost.fs)) are already correct. `invertAs` that copies `action.authority` is fine.

## 5. How a follow-on ticket uses this list

1. Pick a named criterion. Search for the **Look for** pattern. Do not use file or function length to decide the ticket is done.
2. Change only sites that fail that criterion, plus the tests that pin them. Surgical: [core-agent-behavior.md](.agents/rules/core-agent-behavior.md).
3. Accept the change when **Fixed** is true for the sites the ticket named. Reject a change that reopens closed 11/12 Spec, that renames a KEEP [1.2](#12-keep-names-that-mean-eventbodychange-or-http) name, or that treats ≤400 / ≤40 as the bar.
4. A function the ticket already edits may be brought in line with [fsharp-source.md](.agents/rules/fsharp-source.md) length while it is open. That is ordinary surgical judgment, not a project-wide accept bar.

## 6. Overlap with redo tickets 13–19 (do not rewrite)

Note only. What to build on those tickets stays as written.

| Criterion | Overlap | What this research does not do |
| --- | --- | --- |
| 2.1–2.4 peel / zero / beforeAll / next | [14 — EventId serial on Shared + Server](plan/single-event-source/issues/14-eventid-serial-shared-server.md), [15 — Client pending = zero + submissionId](plan/single-event-source/issues/15-client-pending-zero-submissionid.md), [16 — Approve / merge stamp + beforeAll](plan/single-event-source/issues/16-approve-merge-stamp-beforeall.md) redo the Spec path | Does not change those Green bars |
| 1.3 Revision names | [17 — Delete Revision aliases](plan/single-event-source/issues/17-delete-revision-aliases.md) deletes `type Revision` / converters, not leftover *names* on EventId | Leftover names stay a cleanup bar after 17 |
| 1.2 KEEP EventBody.Change | [18 — Delete leftover Change wrapping](plan/single-event-source/issues/18-delete-leftover-change-wrapping.md) deletes the leftover record, not `EventBody.Change` | Does not rename mintChange / HTTP `/changes` |
| unused EventId.fs | [19 — Delete unused EventId.fs](plan/single-event-source/issues/19-delete-unused-eventid-fs.md) | File is already absent at this tip; 19 stays the redo delete if it returns |
