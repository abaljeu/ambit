# 17 — Delete Revision aliases

**Status:** done
**Actual:** 20m
**Blocked by:** [16 — Approve / merge stamp + beforeAll](16-approve-merge-stamp-beforeall.md)

## Context

Alan approved the SES replan ([Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md)). This ticket is redo **12a** (after swap: Delete Revision aliases first). Mega ticket [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md) remains a historical `coded` land.

## What to build

Delete `type Revision` and `EventId.ofRevision` / `toRevision`.

**Green bar:** no Revision type left.

## Out of scope

1. Leftover Change wrapping deletes — [18 — Delete leftover Change wrapping](18-delete-leftover-change-wrapping.md).
2. Unused EventId.fs — [19 — Delete unused EventId.fs](19-delete-unused-eventid-fs.md).
3. Writing [core-creation arch.md](../../core-creation/arch.md) — [04 — Write core-creation arch.md last](04-write-core-creation-arch-md-last.md).

## See also

[Single event source architecture](../arch.md), [Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md), [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md) (historical coded land; superseded for redo by 13–19)

## Comments

- 2026-09-17 — Maps to replan **12a** (swapped: Revision aliases before leftover Change wrapping). Redo path; does not replace the historical land of [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md).
- 2026-09-18 — `type Revision` and `EventId.ofRevision` / `toRevision` were already gone from the historical [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md) land. Green bar: no Revision type in `src/` or `tests/`. Leftover Revision *names* on EventId stay for [21 — SES smell-cleanup](21-ses-smell-cleanup.md).
- 2026-09-18 — Alan said next on Status `coded`. Review approved.

## Time

- 2026-09-18 20m — Confirm Revision aliases already deleted; no F# type or alias to delete
