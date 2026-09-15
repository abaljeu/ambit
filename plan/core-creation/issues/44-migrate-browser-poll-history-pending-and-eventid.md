# 44 — Migrate Browser Poll, History, pending, and EventId cursor

**Status:** done
**Blocked by:** ~~[40 — Expand postEvent, EventLog store, and Event JSON persist](40-expand-postevent-eventlog-and-event-json.md), [41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](41-migrate-core-mailbox-coremsg-and-pool-onto-event.md), [43 — Migrate HTTP Adapter onto postEvent and Event Poll](43-migrate-http-adapter-onto-postevent-and-event-poll.md)~~ (completed)

## Context

[43 — Migrate HTTP Adapter onto postEvent and Event Poll](43-migrate-http-adapter-onto-postevent-and-event-poll.md) returns an Event tail on Poll/Load and sends Change posts through `postEvent`. [41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](41-migrate-core-mailbox-coremsg-and-pool-onto-event.md) fills name-only Undo/Redo on the server. Browser still consumes a Change list on Poll, holds a Revision cursor (`State.revision`, `ClientSyncState.revision`), wraps pending work as Change plus PendingKind, and uses Change-shaped ClientHistory for Emacs undo. Story **Caller, persist, and Poll** on [Core creation architecture](../arch.md) migrates the Browser next. Field shapes: [[../reports/event-abstraction.md]]. ClientHistory module: [Core creation architecture](../arch.md) Module **ClientHistory**. There is no destination module named History.

## What to build

Move Browser Poll consume, pending submit, and Emacs undo onto Event. Poll consume reads an Event tail. The poll cursor is EventId (`State.revision`, `ClientSyncState.revision`). PendingChange / ChangeBatch wrap Event (or EventBody). Browser callers keep using ClientHistory (Event-shaped). The client holds EventLog of the same type as the server. `ClientHistory.undo` runs locally, then a name-only submit; ack/reconcile stays the pending path. ClientHistory is not persisted and is not sent on Poll. Do not migrate onto a module named History. The old types still compile until contract. CI stays green.

### 1. Poll consume and EventId cursor

Migrate Browser Poll consume. Seam **EventLog** (`since` is the Poll tail). The client holds EventLog of the same type.

- [ ] Poll consume Events — Browser Poll consume reads an Event tail (client consume).
- [ ] EventId cursor — poll cursor is EventId. Today’s `State.revision` and `ClientSyncState.revision` become that EventId cursor.
- [ ] Client EventLog — client holds EventLog of the same type as the server; restores Poll/ack tails (undo optimistic Graph edits, apply server list, dedupe by submissionId).

### 2. PendingChange and ChangeBatch wrap Event

Migrate pending submit wrappers.

- [ ] PendingChange wraps Event — PendingChange wraps Event (or EventBody). PendingKind remains until contract.
- [ ] ChangeBatch wraps Event — ChangeBatch wraps Event (or EventBody).

### 3. ClientHistory undo and name-only submit

Keep Browser Emacs undo on **ClientHistory** (Event-shaped). Do not migrate onto a module named History.

- [ ] Event-shaped ClientHistory — Browser ClientHistory callers keep using ClientHistory (`record`, `undo` / `redo`, peek) with Event (Action bodies).
- [ ] ClientHistory undo then name-only submit — `ClientHistory.undo` runs locally, then a name-only submit. Ack/reconcile stays the pending path.

## Out of scope

1. Contract deletes — Delete of HistoryEvent, ActorLifecycleEvent, mailbox `type History` / History name (replaced by EventLog), PendingKind, the StartActorRequest name, and the ChangeLog name stays on [45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog](45-contract-historyevent-clienthistory-pendingkind-and-changelog.md). ClientHistory remains.

## See also

[Core creation architecture](../arch.md), [Event abstraction](../reports/event-abstraction.md)

## Comments

- 2026-09-15 — Filed via `/to-tickets` for Story **Caller, persist, and Poll** only. Browser migrate batch. Blocked by Core name-only Undo/Redo and HTTP Adapter Event Poll.
- 2026-09-15 — ClientHistory callers stay on Event-shaped ClientHistory; client holds EventLog of the same type. Do not migrate onto a module named History.
