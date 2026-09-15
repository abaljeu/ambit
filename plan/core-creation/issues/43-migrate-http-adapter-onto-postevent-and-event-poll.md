# 43 — Migrate HTTP Adapter onto postEvent and Event Poll

**Status:** blocked
**Blocked by:** [[40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]], [[41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]], [[42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]]

## Context

[[40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]] adds the `postEvent` door. [[41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]] moves Core Changes callers onto that door and returns EventLog from GetEventHistory. [[42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]] returns Events from `getEventsSince`. HTTP Adapter still decodes Change posts onto the old door and returns a Change list on Poll/Load. Story **Caller, persist, and Poll** on [[../arch.md|Core creation architecture]] migrates [[src/Server/Api.fs]] next. Field shapes: [[../reports/event-abstraction.md]].

## What to build

Move HTTP Adapter decode/encode onto Event. Change posts call `postEvent`. Poll and Load tails are Events, not a Change list. Poll returns an Event tail on the server. Keep the old Change Poll beside that Event tail so Browser consume stays green until [[44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId cursor]]. Command-builder still produces Change (Ops); Event is built at the `postEvent` door. The old types still compile. CI stays green.

### 1. Change posts call postEvent

Migrate HTTP Changes posts on **HTTP Adapter**. Seam **`postEvent` door**.

- [ ] Change posts call postEvent — HTTP Adapter decodes Change posts and calls CoreMailbox `postEvent`. Command-builder still produces Change (Ops). Event is built at the `postEvent` door.

### 2. Poll and Load Event tail

Migrate Poll and Load encode on **HTTP Adapter**. Seam **EventLog** (`since` is the Poll/Load tail).

- [ ] Poll Event tail — Poll returns an Event tail (server return). Old Change Poll remains beside it until Browser migrate.
- [ ] Load Event tail — Load tail is Events. Old Change Load remains beside it until Browser migrate.

## Out of scope

1. Browser consume — Browser Poll consume, `Revision` → EventId cursor, PendingChange / ChangeBatch wrapping Event, and ClientHistory undo stay on [[44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId cursor]].
2. Contract deletes — Delete of HistoryEvent, ActorLifecycleEvent, mailbox `type History` / History name, PendingKind, the StartActorRequest name, and the ChangeLog name stays on [[45-contract-historyevent-clienthistory-pendingkind-and-changelog.md|45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog]]. ClientHistory remains.

## See also

[[../arch.md|Core creation architecture]], [[../reports/event-abstraction.md|Event abstraction]]

## Comments

- 2026-09-15 — Filed via `/to-tickets` for Story **Caller, persist, and Poll** only. HTTP Adapter migrate batch. Blocked by expand, Core migrate, and PersistHandlers migrate. Event Poll sits beside the old Change list until [[44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId cursor]].
