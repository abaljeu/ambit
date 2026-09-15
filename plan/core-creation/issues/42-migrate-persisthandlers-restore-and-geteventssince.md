# 42 — Migrate PersistHandlers restore and getEventsSince

**Status:** blocked
**Blocked by:** [[40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]], [[41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]]

## Context

[[40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]] writes Event JSON beside ChangeLog. [[41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]] appends ActorStart / ActorStop on EventLog. File and Db still restore through today’s Change path. `getEventsSince` still returns a Change list. Story **Caller, persist, and Poll** on [[../arch.md|Core creation architecture]] migrates persist next. Field shapes: [[../reports/event-abstraction.md]].

## What to build

Move PersistHandlers onto EventLog. File and Db call `EventLog.restore` on load. `getEventsSince` returns Events. ActorStart and ActorStop persist. The ChangeLog name remains until contract. HTTP Poll still uses the old Change tail until [[43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]]. CI stays green.

### 1. persist ActorStart and ActorStop

Persist lifecycle Events on **EventLog**. Module **PersistHandlers**.

- [ ] persist ActorStart ActorStop — persist ActorStart / ActorStop. Persistence is this same EventLog on file/DB.

### 2. restore

Migrate load/restore on **PersistHandlers**.

- [ ] EventLog.restore — File/Db call `EventLog.restore`. Restore merges persisted Events and dedupes by `submissionId`.

### 3. getEventsSince

Migrate the Poll/Load persist tail on **PersistHandlers** and **CoreMailbox**.

- [ ] getEventsSince returns Events — `getEventsSince` returns an Event tail. The old Change persist read still compiles until HTTP Adapter migrates Poll.

## Out of scope

1. HTTP Adapter migrate — Change posts calling `postEvent`, Poll/Load Event tail, and command-builder still producing Change stay on [[43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]].
2. Browser migrate — Poll consume, EventId cursor, PendingChange / ChangeBatch, and History undo stay on [[44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId cursor]].
3. Contract deletes — Drop of the ChangeLog name stays on [[45-contract-historyevent-clienthistory-pendingkind-and-changelog.md|45 — Contract HistoryEvent, ClientHistory, PendingKind, StartActorRequest, and ChangeLog]].

## See also

[[../arch.md|Core creation architecture]], [[../reports/event-abstraction.md|Event abstraction]]

## Comments

- 2026-09-15 — Filed via `/to-tickets` for Story **Caller, persist, and Poll** only. PersistHandlers migrate batch. Blocked by the story 5 expand and the Core migrate batch so ActorStart / ActorStop exist to persist.
