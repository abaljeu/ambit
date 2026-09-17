# 12 — Contract leftover Change and Revision

**Status:** defined
**Blocked by:** [06 — Compile preamble](06-compile-preamble.md), [07 — Persist apply](07-persist-apply.md), [08 — Core doors](08-core-doors.md), [09 — Command mint](09-command-mint.md), [10 — Boot IndexedDB](10-boot-indexeddb.md), [11 — One serial event id](11-one-serial-event-id.md)

## Context

Every migrate batch of Story **Leftover Change and Revision** has moved callers onto Ev and Op list. Leftover Change, `ofChange` / `asChange`, unused EventId.fs, and `type Revision` remain beside the new form. No caller should remain.

## What to build

Delete the old form once no caller remains. Delete leftover Change, `module Change` apply wrapping, `Ev.ofChange` / `Ev.asChange`, `eventFromChange`, unused [[src/Shared/EventId.fs]], `type Revision`, and `EventId.ofRevision` / `toRevision`. CI stays green because every migrate batch is done. Do not edit [[plan/core-creation/arch.md]] on this ticket.

### 1. Delete leftover Change

- [ ] 1.3.1 Delete leftover Change record, `module Change` apply wrapping, `Ev.ofChange` / `Ev.asChange`, `eventFromChange`

### 2. Delete unused EventId.fs

- [ ] 1.3.2 Delete unused [[src/Shared/EventId.fs]]

### 3. Delete Revision

- [ ] 1.3.3 Delete `type Revision` and `EventId.ofRevision` / `toRevision`

## Out of scope

1. Writing [[plan/core-creation/arch.md]] — [04 — Write core-creation arch.md last](04-write-core-creation-arch-md-last.md).

## See also

[Single event source architecture](../arch.md), [[../map.md]]
