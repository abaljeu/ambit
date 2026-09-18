# 43 — Migrate HTTP Adapter onto postEvent and Event Poll

**Status:** done
**Blocked by:** [40 — Expand postEvent, EventLog store, and Event JSON persist](40-expand-postevent-eventlog-and-event-json.md), [41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](41-migrate-core-mailbox-coremsg-and-pool-onto-event.md), [42 — Migrate PersistHandlers restore and getEventsSince](42-migrate-persisthandlers-restore-and-geteventssince.md)

## Context

[40 — Expand postEvent, EventLog store, and Event JSON persist](40-expand-postevent-eventlog-and-event-json.md) adds the `postEvent` door. [41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](41-migrate-core-mailbox-coremsg-and-pool-onto-event.md) moves Core Changes callers onto that door and returns EventLog from GetEventHistory. [42 — Migrate PersistHandlers restore and getEventsSince](42-migrate-persisthandlers-restore-and-geteventssince.md) returns Events from `getEventsSince`. HTTP Adapter still decodes Change posts onto the old door and returns a Change list on Poll/Load. Story **Caller, persist, and Poll** on [Core creation architecture](../arch.md) migrates [[src/Server/Api.fs]] next. Field shapes: [[../reports/event-abstraction.md]].

## What to build

Move HTTP Adapter decode/encode onto Ev. Change posts call `postEvent`. Poll and Load tails are Ev lists, not a Change list. Poll returns an Ev tail on the server. Keep the old Change Poll beside that Ev tail so Browser consume stays green until [44 — Migrate Browser Poll, History, pending, and EventId basis](44-migrate-browser-poll-history-pending-and-eventid.md). Command-builder still produces Change (Ops); Ev is built at the `postEvent` door. The old types still compile. CI stays green.

### 1. Change posts call postEvent

Migrate HTTP Changes posts on **HTTP Adapter**. Seam **`postEvent` door**.

- [x] Change posts call postEvent — HTTP Adapter decodes Change posts and calls CoreMailbox `postEvent`. Command-builder still produces Change (Ops). Ev is built at the `postEvent` door.

### 2. Poll and Load Event tail

Migrate Poll and Load encode on **HTTP Adapter**. Seam **EventLog** (`since` is the Poll/Load tail).

- [x] Poll Event tail — Poll returns an Ev tail (server return). Old Change Poll remains beside it until Browser migrate.
- [x] Load Event tail — Load tail is an Ev list. Old Change Load remains beside it until Browser migrate.

## Out of scope

1. Browser consume — Browser Poll consume, `Revision` → EventId basis, PendingChange / EventBatch wrapping Ev, and ClientHistory undo stay on [44 — Migrate Browser Poll, History, pending, and EventId basis](44-migrate-browser-poll-history-pending-and-eventid.md).
2. Contract deletes — Delete of HistoryEvent, ActorLifecycleEvent, mailbox `type History` / History name, PendingKind, the StartActorRequest name, and the ChangeLog name stays on [45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog](45-contract-historyevent-clienthistory-pendingkind-and-changelog.md). ClientHistory remains.

## See also

[Core creation architecture](../arch.md), [Event abstraction](../reports/event-abstraction.md)

## Comments

- 2026-09-15 — Filed via `/to-tickets` for Story **Caller, persist, and Poll** only. HTTP Adapter migrate batch. Blocked by expand, Core migrate, and PersistHandlers migrate. Event Poll sits beside the old Change list until [44 — Migrate Browser Poll, History, pending, and EventId basis](44-migrate-browser-poll-history-pending-and-eventid.md).
- 2026-09-15 — Implemented. Added `getEventsSince` to CoreChanges interface. Poll and Load now return Event tail alongside Change list. Change posts already call postEvent through CoreMailbox.postChange. Merged to staging (9035ceb).
