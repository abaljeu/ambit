# 21 — SES smell-cleanup

**Status:** done
**Actual:** 1.5h
**Blocked by:** [19 — Delete unused EventId.fs](19-delete-unused-eventid-fs.md), [20 — SES smell-cleanup quality criteria](20-ses-smell-cleanup-quality-criteria.md)

## Context

Alan accepted [20 — SES smell-cleanup quality criteria](20-ses-smell-cleanup-quality-criteria.md). After the SES Spec redo [13 — Revision always 0 (diagnostic)](13-revision-always-zero.md)–[19 — Delete unused EventId.fs](19-delete-unused-eventid-fs.md) is okay, leftover Change / Revision names and EventId type-usage smells remain. An implementer accepts or rejects a change by the locked checklist, not by file or function length.

## What to build

Apply the named bars in [SES smell-cleanup quality criteria](../reports/ses-smell-cleanup-quality-criteria.md). For each criterion: search **Look for**, change only failing sites plus the tests that pin them, accept when **Fixed** is true. Surgical: [core-agent-behavior.md](.agents/rules/core-agent-behavior.md).

**Green bar:** each named criterion below is **Fixed** for the sites this ticket edits. Production compiles. Do not reopen Spec for [11 — One serial event id](11-one-serial-event-id.md) or [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md).

### 1. Naming

1. [x] 1.1 Ev bindings after leftover Change remap — Ev / Ev list locals, parameters, and fields are `event` / `events` (or `ev` / `evs` only if that file already uses them); no `change` / `changes` bound to Ev unless KEEP [1.2](../reports/ses-smell-cleanup-quality-criteria.md#12-keep-names-that-mean-eventbodychange-or-http)
2. [x] 1.2 KEEP EventBody.Change and HTTP — `mintChange`, HTTP `/changes`, and names that mean `EventBody.Change` of an Op list stay
3. [x] 1.3 Mysterious leftover Revision names on EventId — EventId APIs say event id (`readEventId`, `writeEventId`, `getFileEventId`); the value stays `EventId`
4. [x] 1.4 Comment or log strings — spoken and API text say event id, not Revision, for an EventId value (SQL column `graph.revision` may stay)

### 2. EventId peel vs mint

1. [x] 2.1 fromJson / toJson only at named peel — no production `EventId.fromJson` / `toJson` outside a named serialize, HTTP, SQL, file, or IndexedDB peel
2. [x] 2.2 Drafts use EventId.zero — unpublished Ev and draft `ActorStart.eventId` use `EventId.zero`, not `fromJson 0`
3. [x] 2.3 Get-all cursor is EventId.beforeAll — no `fromJson -1` or bare `-1` cursor in Core; peel to int only at SQL
4. [x] 2.4 EventId.next only in EventLog — production `next` stays only in [EventLog.fs](src/Shared/EventLog.fs)

### 3. Primitive Obsession and Authority

1. [x] 3.1 In-process event id is EventId — in-process fields and parameters whose meaning is event id are `EventId`, not `int`
2. [x] 3.2 Int stays at the peel only — JSON / query / SQL / file `int` does not leak past the named peel
3. [x] 4.1 Draft Authority matches the posting Caller — minted `event.authority` matches the `Caller.authority` that posts it

## Out of scope

1. File length ≤400 and function length ≤40 as pass/fail bars. A function this ticket already edits may be brought in line with [fsharp-source.md](.agents/rules/fsharp-source.md) while it is open.
2. Redo What to build on [13 — Revision always 0 (diagnostic)](13-revision-always-zero.md)–[19 — Delete unused EventId.fs](19-delete-unused-eventid-fs.md).
3. Closed 11/12 Spec (merge stamp, leftover Change record, `type Revision`, unused EventId.fs).
4. 34b non-SES Standards (mutable, Exceptions, unused `actorCaller`).
5. Writing [core-creation arch.md](../../core-creation/arch.md) — [04 — Write core-creation arch.md last](04-write-core-creation-arch-md-last.md).

## See also

[SES smell-cleanup quality criteria](../reports/ses-smell-cleanup-quality-criteria.md), [20 — SES smell-cleanup quality criteria](20-ses-smell-cleanup-quality-criteria.md), [Single event source architecture](../arch.md)

## Comments

- 2026-09-18 — Charted after Alan accepted [20 — SES smell-cleanup quality criteria](20-ses-smell-cleanup-quality-criteria.md). Hold until [19 — Delete unused EventId.fs](19-delete-unused-eventid-fs.md); frontier stays [13 — Revision always 0 (diagnostic)](13-revision-always-zero.md).
- 2026-09-18 — Applied named bars: `ackEvents`; drop `.changes` aliases; `shouldTruncate` / `plan` / projection load take `EventId`; get-all uses `EventId.beforeAll`; drafts `EventId.zero`; TestActor hello `Authority "Actor"`. SQL column `graph.revision` and HTTP `/changes` stay.

## Time

- 2026-09-18 1.5h — Apply SES smell-cleanup bars on named leftover Change/Revision and EventId peel sites
