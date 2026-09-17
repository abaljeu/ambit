# 14 — EventId serial on Shared + Server

**Status:** defined
**Blocked by:** [13 — Revision always 0 (diagnostic)](13-revision-always-zero.md)

## Context

Alan approved the SES replan ([Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md)). This ticket is redo **11a**: one serial EventId on Shared + Server without changing the client pending model. Mega ticket [11 — One serial event id](11-one-serial-event-id.md) remains a historical `coded` land.

## What to build

One serial id type everywhere wire/API/State already talks revision; EventId private; `EventId.next` only in EventLog; leftover `Change.id` is EventId; JSON `"eventId"`; `getEventId`.

**In:** [History.fs](src/Shared/History.fs) EventId API, [EventLog.fs](src/Shared/EventLog.fs), [EventJson.fs](src/Shared/EventJson.fs), Server Api/Core/Database peels that still say Revision, core-api EventId serial rule.

**Green bar:** Shared + Server tests that do not depend on client pending-by-submissionId.

## Out of scope

1. ClientHistory pending queue shape; SyncPlanner/SyncLogic pending list; Browser `record` / approve — [15 — Client pending = zero + submissionId](15-client-pending-zero-submissionid.md), [16 — Approve / merge stamp + beforeAll](16-approve-merge-stamp-beforeall.md).
2. Contract deletes — [17 — Delete Revision aliases](17-delete-revision-aliases.md) and later.

## See also

[Single event source architecture](../arch.md), [Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md), [13 — Revision always 0 (diagnostic)](13-revision-always-zero.md), [11 — One serial event id](11-one-serial-event-id.md) (historical coded land)

## Comments

- 2026-09-17 — Maps to replan **11a**. Redo path; does not replace the historical land of [11 — One serial event id](11-one-serial-event-id.md).

## Time
