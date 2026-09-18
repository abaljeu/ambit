# 19 — Delete unused EventId.fs

**Status:** defined
**Blocked by:** [18 — Delete leftover Change wrapping](18-delete-leftover-change-wrapping.md)

## Context

Alan approved the SES replan ([Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md)). This ticket is redo **12c**: delete unused [EventId.fs](src/Shared/EventId.fs) if still present (EventId lives in History.fs). Mega ticket [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md) remains a historical `coded` land.

## What to build

Delete unused [EventId.fs](src/Shared/EventId.fs) if still present after [17 — Delete Revision aliases](17-delete-revision-aliases.md) and [18 — Delete leftover Change wrapping](18-delete-leftover-change-wrapping.md). EventId lives in [History.fs](src/Shared/History.fs).

**Green bar:** project compiles; unused EventId.fs gone (or already absent).

## Out of scope

1. Writing [core-creation arch.md](../../core-creation/arch.md) — [04 — Write core-creation arch.md last](04-write-core-creation-arch-md-last.md).
2. Further contract cleanup beyond this file delete.

## See also

[Single event source architecture](../arch.md), [Replan — smaller increments for SES 11 and 12](../reports/replan-11-12-smaller-increments.md), [18 — Delete leftover Change wrapping](18-delete-leftover-change-wrapping.md), [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md) (historical coded land)

## Comments

- 2026-09-17 — Maps to replan **12c**. Redo path; does not replace the historical land of [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md). Optional fold into 18 if tiny; still ordered after 18 for the chain.

## Time
