# 11 — One serial event id

**Status:** coded
**Actual:** 4.5h
**Blocked by:** [05 — Expand Op-list apply](05-expand-op-list-apply.md)

## Context

`Change.id` is int. `State.revision` is `Revision`. `Ev.id` is `EventId`. Story **Leftover Change and Revision** skips a Revision stop: leftover Change.id is EventId; Revision fields become event id. This batch may run beside persist and command.

The client has a pending queue and a local EventId serial today (`ClientHistory.nextEventId`, `record` returns a local id, `PendingTransition` / `PendingChange.transition` carry `recordId`). Alan locked the destination: the client has no EventId serial.

## What to build

Leftover `Change.id` is `EventId`. `Revision` type and `revision` fields become event id. JSON key is `"eventId"`. Core door is `getEventId`. `CoreChangesAccepted.eventId` is EventId. Leftover Change still compiles. CI stays green.

EventId has private id. EventId.fromJson/toJson bypasses. EventId.next adds one. Ev.fromJson/toJson. Only serializing should use the fromJson/toJson functions. Only the EventLog should use EventId.next. Any event not from these sources should have id 0. Policy: [core-api.md](.agents/rules/core-api.md) EventId serial.

The client has no EventId serial. Pending events use `EventId.zero` only. The client keeps a pending queue. Match server responses with `submissionId`, not a local recordId or nextEventId. On approve, replace zero with the server-assigned id. On a server merge / revised event stream that inserts other server ops before what the client sent, rewind and stamp zeros from matching `submissionId`. No Interject API. `ClientHistory.nextEventId` and local `EventId.next` go away. `record` must not return a local id. `PendingTransition` dies. `PendingChange.transition` dies; the guid is already on Ev. SyncInfo pending collapses to an event list. Server EventLog remains the only `EventId.next` caller.

### 1. One serial

Modules **Ev**, **State**, **CoreChanges**, **EventLog**, **ClientHistory**. Seam **EventId**. [History.fs](src/Shared/History.fs), [ClientHistory.fs](src/Shared/ClientHistory.fs), and [ViewModelSync.fs](src/Shared/ViewModelSync.fs) change on this ticket.

- [x] 1.1.3 leftover Change.id is EventId
- [x] 1.2.6 One serial — `Revision` type and `revision` fields become event id. JSON key `"eventId"`. `getEventId`. EventId has private id. EventId.fromJson/toJson bypasses. EventId.next adds one. Ev.fromJson/toJson. Only serializing uses fromJson/toJson. Only EventLog uses EventId.next. Any event not from these sources has id 0
- [x] 1.2.6 client pending — Client has no EventId serial. Pending events use `EventId.zero`. Match server responses with `submissionId`. On approve, replace zero with the server-assigned id. On a server merge / revised event stream that inserts other server ops before what the client sent, rewind and stamp zeros from matching `submissionId`. Drop `ClientHistory.nextEventId`, local `EventId.next`, `record` local id, `PendingTransition`, and `PendingChange.transition`. SyncInfo pending is an event list

## Out of scope

1. Contract delete of `type Revision` — [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md).

## See also

[Single event source architecture](../arch.md), [03 — Cleanup seam order](03-cleanup-seam-order.md), [09 — Command mint](09-command-mint.md)

## Comments

- 2026-09-17 — Alan locked the client EventId / pending model. The client has no EventId serial. Pending events use `EventId.zero` only. Match server responses with `submissionId`, not local recordId or nextEventId. On approve, replace zero with the server-assigned id. On a server merge / revised event stream that inserts other server ops before what the client sent, rewind. Match by `submissionId`. No Interject API. Consequences: `ClientHistory.nextEventId` / local `EventId.next` go away; `record` must not return a local id; `PendingTransition` dies; `PendingChange.transition` dies (guid already on Ev); SyncInfo pending collapses to an event list. Server EventLog remains the only `EventId.next` caller (already on this ticket and [core-api.md](.agents/rules/core-api.md)).
- 2026-09-17 — Repair: stamp History from the revised stream by `submissionId`; stamp nested Undo/Redo targets; replace `EventId.fromJson -1` with `EventId.beforeAll`; move BootCache SnapshotRecord to `eventId` / `"eventId"`. Status stays `coded`.

## Time

- 2026-09-17 30m — Fold locked client pending / `submissionId` model into What to build and arch EventId / ClientHistory notes (from chat)
- 2026-09-17 2.5h — Implement private EventId serial, leftover Change.id, client pending EventId.zero / submissionId (from chat)
- 2026-09-17 1.5h — Repair review: merge stamp, Undo/Redo targets, beforeAll cursor, SnapshotRecord eventId (from chat)
