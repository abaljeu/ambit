# 42 — Migrate PersistHandlers restore and getEventsSince

**Status:** done
**Blocked by:** None — [40 — Expand postEvent, EventLog store, and Event JSON persist](40-expand-postevent-eventlog-and-event-json.md) is Status `coded`. [41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](41-migrate-core-mailbox-coremsg-and-pool-onto-event.md) is Status `done`.
Actual: 2h30m

## Context

[40 — Expand postEvent, EventLog store, and Event JSON persist](40-expand-postevent-eventlog-and-event-json.md) writes Event JSON beside ChangeLog. [41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](41-migrate-core-mailbox-coremsg-and-pool-onto-event.md) appends ActorStart / ActorStop on EventLog. File and Db still restore through today’s Change path. `getEventsSince` still returns a Change list. Story **Caller, persist, and Poll** on [Core creation architecture](../arch.md) migrates persist next. Field shapes: [[../reports/event-abstraction.md]].

## What to build

Move PersistHandlers onto EventLog. File and Db call `EventLog.restore` on load. `getEventsSince` returns an Ev list. ActorStart and ActorStop persist. The ChangeLog name remains until contract. HTTP Poll still uses the old Change tail until [43 — Migrate HTTP Adapter onto postEvent and Event Poll](43-migrate-http-adapter-onto-postevent-and-event-poll.md). CI stays green.

### 1. persist ActorStart and ActorStop

Persist lifecycle Events on **EventLog**. Module **PersistHandlers**.

- [x] persist ActorStart ActorStop — persist ActorStart / ActorStop. Persistence is this same EventLog on file/DB.

### 2. restore

Migrate load/restore on **PersistHandlers**.

- [x] EventLog.restore — File/Db call `EventLog.restore`. Restore merges persisted Events and dedupes by `submissionId`.

### 3. getEventsSince

Migrate the Poll/Load persist tail on **PersistHandlers** and **CoreMailbox**.

- [x] getEventsSince returns Events — `getEventsSince` returns an Ev tail. The old Change persist read still compiles until HTTP Adapter migrates Poll.

## Out of scope

1. HTTP Adapter migrate — Change posts calling `postEvent`, Poll/Load Event tail, and command-builder still producing Change stay on [43 — Migrate HTTP Adapter onto postEvent and Event Poll](43-migrate-http-adapter-onto-postevent-and-event-poll.md).
2. Browser migrate — Poll consume, EventId cursor, PendingChange / EventBatch, and ClientHistory undo stay on [44 — Migrate Browser Poll, History, pending, and EventId cursor](44-migrate-browser-poll-history-pending-and-eventid.md).
3. Contract deletes — Drop of the ChangeLog name stays on [45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog](45-contract-historyevent-clienthistory-pendingkind-and-changelog.md). ClientHistory remains.

## See also

[Core creation architecture](../arch.md), [Event abstraction](../reports/event-abstraction.md)

## Comments

- 2026-09-15 — Filed via `/to-tickets` for Story **Caller, persist, and Poll** only. PersistHandlers migrate batch. Blocked by the story 5 expand and the Core migrate batch so ActorStart / ActorStop exist to persist.
- 2026-09-19 — Independent Spec review approve → Status `done`. Report: [spec-review-41-42](../reports/spec-review-41-42.md). [46 — Mailbox History durability](46-mailbox-history-durability.md) three-way drop/apply is not a 42 gap. Compiled restore proof is [PersistHandlersRestoreTests](tests/Server.Tests/PersistHandlersRestoreTests.fs); [Issue42PersistHandlersTests](tests/Server.Tests/Issue42PersistHandlersTests.fs) is a duplicate and is not in the Server.Tests fsproj.

## Time

- 2026-09-15 1h30m — PersistHandlers Event append/getEventsSince, File/Db EventLog restore, CoreMailbox door (from chat)
- 2026-09-15 15m — Db Event persist/restore test + report; filter Pass 4/4. Notes: [[../reports/issue-42-db-restore-test.md]].
- 2026-09-19 45m — Independent Spec review (from chat)
