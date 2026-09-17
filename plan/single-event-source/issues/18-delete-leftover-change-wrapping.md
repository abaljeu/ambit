# 18 — Delete leftover Change wrapping

**Status:** defined
**Blocked by:** [17 — Delete Revision aliases](17-delete-revision-aliases.md)

## Context

Alan approved the SES replan ([Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md)). This ticket is redo **12b** (after swap: Delete leftover Change wrapping second). Mega ticket [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md) remains a historical `coded` land.

## What to build

Delete leftover `{ id; submissionId; ops }` Change record, `module Change` apply wrapping, `Ev.ofChange` / `Ev.asChange`, `eventFromChange`. Callers already on Ev/Ops from 05–16.

**Green bar:** production + tests compile with no `asChange`/`ofChange`.

## Out of scope

1. Unused EventId.fs — [19 — Delete unused EventId.fs](19-delete-unused-eventid-fs.md).
2. Writing [core-creation arch.md](../../core-creation/arch.md) — [04 — Write core-creation arch.md last](04-write-core-creation-arch-md-last.md).

## See also

[Single event source architecture](../arch.md), [Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md), [17 — Delete Revision aliases](17-delete-revision-aliases.md), [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md) (historical coded land)

## Comments

- 2026-09-17 — Maps to replan **12b** (swapped: leftover Change wrapping after Revision aliases). Redo path; does not replace the historical land of [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md).

## Time
