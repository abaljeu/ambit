# 15 — Client pending = zero + submissionId

**Status:** done
**Actual:** 45m
**Blocked by:** [14 — EventId serial on Shared + Server](14-eventid-serial-shared-server.md)

## Context

Alan approved the SES replan ([Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md)). This ticket is redo **11b**: client has no EventId serial; pending uses `EventId.zero` and `submissionId`. Mega ticket [11 — One serial event id](11-one-serial-event-id.md) remains a historical `coded` land.

## What to build

Client has no EventId serial. Pending events are Ev with `EventId.zero`. Match/approve by `submissionId`. Drop `nextEventId`, `PendingTransition`, `PendingChange.transition`, `record` local id. SyncInfo pending is an event list.

**In:** [ClientHistory.fs](src/Shared/ClientHistory.fs), [SyncLogic.fs](src/Shared/SyncLogic.fs), [SyncPlanner.fs](src/Shared/SyncPlanner.fs), [ViewModelSync.fs](src/Shared/ViewModelSync.fs), Client Update/App/Program paths that post pending.

**Green bar:** ClientHistory / SyncLogic tests for zero pending + submissionId match. **Wire assert:** posted new client events have `eventId: 0` (this is the likely live reject). Interactive repro at `:5215` `/ambit?debug=1` remains useful.

## Out of scope

1. Nested Undo/Redo target stamping and merge-stream rewind edge cases if they can wait — [16 — Approve / merge stamp + beforeAll](16-approve-merge-stamp-beforeall.md).
2. Contract deletes — [17 — Delete Revision aliases](17-delete-revision-aliases.md) and later.

## See also

[Single event source architecture](../arch.md), [Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md), [14 — EventId serial on Shared + Server](14-eventid-serial-shared-server.md), [11 — One serial event id](11-one-serial-event-id.md) (historical coded land)

## Comments

- 2026-09-17 — Maps to replan **11b**. Redo path; does not replace the historical land of [11 — One serial event id](11-one-serial-event-id.md).
- 2026-09-18 — Client pending EventId.zero / submissionId was already on Shared + Client from historical [11 — One serial event id](11-one-serial-event-id.md). This land dropped leftover test adapters (`asPending`, `withKind`, `recordId` names) and stamped [AmbitSession.fs](src/Shared/dotnet/AmbitSession.fs) encodeBatch through `toWireBatch`.

## Time

- 2026-09-18 45m — Drop leftover pending adapters; stamp AmbitSession wire batch
