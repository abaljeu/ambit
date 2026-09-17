# 16 — Approve / merge stamp + beforeAll

**Status:** defined
**Blocked by:** [15 — Client pending = zero + submissionId](15-client-pending-zero-submissionid.md)

## Context

Alan approved the SES replan ([Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md)). This ticket is redo **11c**: approve/stamp and `beforeAll`, matching repair commit `75738008`. Mega ticket [11 — One serial event id](11-one-serial-event-id.md) remains a historical `coded` land.

## What to build

On approve, replace zero with server id. On revised stream (server ops inserted ahead), rewind and stamp zeros from matching `submissionId`. Stamp nested Undo/Redo targets. `EventId.beforeAll` instead of `fromJson -1`. BootCache SnapshotRecord `eventId` if still open after [14 — EventId serial on Shared + Server](14-eventid-serial-shared-server.md).

**In:** ClientHistory `approve` / `stampEvent` / `stampBody`, EventLog `all`/`since` cursor, BootCache store field names if needed.

**Green bar:** merge/revise and Undo/Redo target tests from the repair review.

## Out of scope

1. Contract deletes — [17 — Delete Revision aliases](17-delete-revision-aliases.md), [18 — Delete leftover Change wrapping](18-delete-leftover-change-wrapping.md), [19 — Delete unused EventId.fs](19-delete-unused-eventid-fs.md).
2. Writing [core-creation arch.md](../../core-creation/arch.md) — [04 — Write core-creation arch.md last](04-write-core-creation-arch-md-last.md).

## See also

[Single event source architecture](../arch.md), [Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md), [15 — Client pending = zero + submissionId](15-client-pending-zero-submissionid.md), [11 — One serial event id](11-one-serial-event-id.md) (historical coded land)

## Comments

- 2026-09-17 — Maps to replan **11c** / repair `75738008`. Redo path; does not replace the historical land of [11 — One serial event id](11-one-serial-event-id.md).

## Time
