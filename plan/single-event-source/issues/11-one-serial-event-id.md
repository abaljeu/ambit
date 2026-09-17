# 11 — One serial event id

**Status:** defined
**Blocked by:** [05 — Expand Op-list apply](05-expand-op-list-apply.md)

## Context

`Change.id` is int. `State.revision` is `Revision`. `Ev.id` is `EventId`. Story **Leftover Change and Revision** skips a Revision stop: leftover Change.id is EventId; Revision fields become event id. This batch may run beside persist and command.

## What to build

Leftover `Change.id` is `EventId`. `Revision` type and `revision` fields become event id. JSON key is `"eventId"`. Core door is `getEventId`. `CoreChangesAccepted.eventId` is EventId. Leftover Change still compiles. CI stays green.

### 1. One serial

Modules **Ev**, **State**, **CoreChanges**. Seam **EventId**.

- [ ] 1.1.3 leftover Change.id is EventId
- [ ] 1.2.6 One serial — `Revision` type and `revision` fields become event id. JSON key `"eventId"`. `getEventId`

## Out of scope

1. Contract delete of `type Revision` — [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md).

## See also

[Single event source architecture](../arch.md), [03 — Cleanup seam order](../issues/03-cleanup-seam-order.md)
