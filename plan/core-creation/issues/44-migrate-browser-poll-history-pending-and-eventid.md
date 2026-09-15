# 44 — Migrate Browser Poll, History, pending, and EventId cursor

**Status:** blocked
**Blocked by:** [[40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]], [[41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]], [[43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]]

## Context

[[43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]] returns an Event tail on Poll/Load and sends Change posts through `postEvent`. [[41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]] fills name-only Undo/Redo on the server. Browser still consumes a Change list on Poll, holds a Revision cursor (`State.revision`, `ClientSyncState.revision`), wraps pending work as Change plus PendingKind, and uses ClientHistory for Emacs undo. Story **Caller, persist, and Poll** on [[../arch.md|Core creation architecture]] migrates the Browser next. Field shapes: [[../reports/event-abstraction.md]]. History module: [[../arch.md|Core creation architecture]] Module **History**.

## What to build

Move Browser Poll consume, pending submit, and Emacs undo onto Event. Poll consume reads an Event tail. The poll cursor is EventId (`State.revision`, `ClientSyncState.revision`). PendingChange / ChangeBatch wrap Event (or EventBody). Browser ClientHistory callers use History. `History.undo` runs locally, then a name-only submit; ack/reconcile stays the pending path. History is not persisted and is not sent on Poll. The old types still compile until contract. CI stays green.

### 1. Poll consume and EventId cursor

Migrate Browser Poll consume. Seam **EventLog** (`since` is the Poll tail).

- [ ] Poll consume Events — Browser Poll consume reads an Event tail (client consume).
- [ ] EventId cursor — poll cursor is EventId. Today’s `State.revision` and `ClientSyncState.revision` become that EventId cursor.

### 2. PendingChange and ChangeBatch wrap Event

Migrate pending submit wrappers.

- [ ] PendingChange wraps Event — PendingChange wraps Event (or EventBody). PendingKind remains until contract.
- [ ] ChangeBatch wraps Event — ChangeBatch wraps Event (or EventBody).

### 3. History undo and name-only submit

Migrate Browser Emacs undo onto **History**.

- [ ] ClientHistory callers use History — Browser ClientHistory callers use History (`record`, `undo` / `redo`, peek).
- [ ] History undo then name-only submit — `History.undo` runs locally, then a name-only submit. Ack/reconcile stays the pending path.

## Out of scope

1. Contract deletes — Delete of HistoryEvent, ActorLifecycleEvent, ClientHistory, PendingKind, the StartActorRequest name, and the ChangeLog name stays on [[45-contract-historyevent-clienthistory-pendingkind-and-changelog.md|45 — Contract HistoryEvent, ClientHistory, PendingKind, StartActorRequest, and ChangeLog]].

## See also

[[../arch.md|Core creation architecture]], [[../reports/event-abstraction.md|Event abstraction]]

## Comments

- 2026-09-15 — Filed via `/to-tickets` for Story **Caller, persist, and Poll** only. Browser migrate batch. Blocked by Core name-only Undo/Redo and HTTP Adapter Event Poll.
