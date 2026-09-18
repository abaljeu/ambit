# 12 — Contract leftover Change and Revision

**Status:** coded
**Actual:** 4h
**Blocked by:** [06 — Compile preamble](06-compile-preamble.md), [07 — Persist apply](07-persist-apply.md), [08 — Core doors](08-core-doors.md), [09 — Command mint](09-command-mint.md), [10 — Boot IndexedDB](10-boot-indexeddb.md), [11 — One serial event id](11-one-serial-event-id.md)

## Context

Every migrate batch of Story **Leftover Change and Revision** has moved callers onto Ev and Op list. Leftover Change, `ofChange` / `asChange`, unused EventId.fs, and `type Revision` remain beside the new form. No caller should remain.

## What to build

Delete the old form once no caller remains. Delete leftover Change, `module Change` apply wrapping, `Ev.ofChange` / `Ev.asChange`, `eventFromChange`, unused [[src/Shared/EventId.fs]], `type Revision`, and `EventId.ofRevision` / `toRevision`. CI stays green because every migrate batch is done. Do not edit [[plan/core-creation/arch.md]] on this ticket.

### 1. Delete leftover Change

- [x] 1.3.1 Delete leftover Change record, `module Change` apply wrapping, `Ev.ofChange` / `Ev.asChange`, `eventFromChange`

### 2. Delete unused EventId.fs

- [x] 1.3.2 Delete unused [[src/Shared/EventId.fs]]

### 3. Delete Revision

- [x] 1.3.3 Delete `type Revision` and `EventId.ofRevision` / `toRevision`

## Out of scope

1. Writing [[plan/core-creation/arch.md]] — [04 — Write core-creation arch.md last](04-write-core-creation-arch-md-last.md).

## See also

[Single event source architecture](../arch.md), [[../map.md]]

## Comments

- 2026-09-17 — Redo sequence charted as tickets [13 — Revision always 0 (diagnostic)](13-revision-always-zero.md)–[19 — Delete unused EventId.fs](19-delete-unused-eventid-fs.md) ([replan](../reports/replan-11-12-smaller-increments.md)). Swap: 12a = Delete Revision aliases, 12b = Delete leftover Change wrapping, 12c = Delete unused EventId.fs. This ticket remains a historical `coded` land (no-retrofit); do not re-implement here.

## Time

- 2026-09-17 2h — Migrate production leftover Change / Revision callers and delete leftover types (from chat)
- 2026-09-17 2h — Migrate test fixtures, drop leftover persist-batch test, Shared/Server/client green (from chat)
