# 03 — Cleanup seam order

**Type:** grilling
**Status:** done
Blocked by: ~~01~~ (completed)

## 1. Question

Given [[01-inventory-non-event-write-paths.md|01 — Inventory non-Event write paths]], what is the cleanup order that deletes the Change record and the Revision type, puts every listed path onto Event, and does not add implementation tickets to [[plan/core-creation/project.md]]?

## Answer

Ev is transported. Ops are not. Apply, validation, invert, amend, and PersistStamp take an Op list locally. The leftover Change record is unused wrapping; delete it when no site still builds it.

1. Compile preamble — Server.Tests `Ev` / `Authority` in [[tests/Server.Tests/TestBackend.fs]] so persist work can see tests.
2. Persist apply and command mint, in any order — Ev in, Ops out for apply. Two doors stay Event: `postEvents` and `postGraphOnly`.
3. One serial now — leftover `Change.id` is `EventId` (same as `Ev.id`). No `Revision` stop. Collapse `Revision` type and `revision` fields to event id on the same track.
4. Delete leftover Change, `Ev.ofChange` / `Ev.asChange`, unused [[src/Shared/EventId.fs]], when nothing constructs the record.

Implementation tickets live on this Project, not on [[plan/core-creation/project.md]].
