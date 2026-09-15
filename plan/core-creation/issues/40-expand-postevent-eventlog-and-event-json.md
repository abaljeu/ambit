# 40 — Expand postEvent, EventLog store, and Event JSON persist

**Status:** coded
Actual: 1h30m
**Blocked by:** None — [[37-expand-shared-event-eventlog-and-history.md|37 — Expand Shared Event, EventLog, and History]] is Status `coded`.

## Context

Story **Event, EventLog, and ClientHistory** on [[../arch.md|Core creation architecture]] adds Shared Event and EventLog beside HistoryEvent, and Event-shaped functions on ClientHistory. There is no destination module named History. Production callers still post Changes through today’s Core door. The mailbox store is not EventLog. Persist still writes Change JSON through today’s [[src/Server/ChangeLog.fs]]. Story **Caller, persist, and Poll** follows that Shared expand. This ticket is the first expand hop: add the new form beside the old so nothing breaks. Field shapes: [[../reports/event-abstraction.md]].

## What to build

Add CoreMailbox `postEvent`, hold the mailbox store as EventLog, and persist that EventLog as Event JSON beside today’s ChangeLog. Old doors, old GetEventHistory, HistoryEvent, ClientHistory, and the ChangeLog name still compile. CI stays green. Prove the narrowest test seam for Story **Caller, persist, and Poll**: CoreMailbox `postEvent` and EventLog `since`. Do not migrate production callers. Do not delete old types or names.

### 1. postEvent door

Add the Changes door on **CoreMailbox**. State, Interface, and Uses: [[../arch.md|Core creation architecture]] Module **CoreMailbox**. Seam **`postEvent` door**. Field shapes: [[../reports/event-abstraction.md]].

- [x] postEvent door — `postEvent` is on CoreMailbox. Payload is Event (Change, Undo, Redo). Name-only Undo/Redo may arrive with only `target`. Today’s Changes post door still compiles.

### 2. mailbox EventLog store

Hold the mailbox store as EventLog after intake. Modules **CoreMailbox** and **CoreMsg / CoreMailboxBackend**. Seam **EventLog**.

- [x] EventLog ref — mailbox store is `EventLog ref`. CoreMailboxBackend is the only writer of that store. Today’s History two-stack GetEventHistory still compiles.

### 3. Event JSON persist

Persist EventLog as Event JSON beside today’s ChangeLog. Module **EventLog**. Persist seam **PersistHandlers**.

- [x] Event JSON beside ChangeLog — encode and read Event JSON. Persist is this same EventLog on file/DB. Today’s [[src/Server/ChangeLog.fs]] name remains. Do not drop the ChangeLog name.

### 4. Narrowest test seam

Prove CoreMailbox `postEvent` and EventLog `since`.

- [x] postEvent and since — CoreMailbox `postEvent` appends an Event. EventLog `since` returns that Event tail.

## Out of scope

1. Core caller migrate — ActorStart / ActorStop append, authority stamp, name-only Undo/Redo fill, GetEventHistory as log-or-since, and every start request as ActorStart stay on [[41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]].
2. PersistHandlers migrate — File/Db `EventLog.restore`, `getEventsSince` as Events, and persist of ActorStart / ActorStop stay on [[42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]].
3. HTTP Adapter migrate — Change posts calling `postEvent`, Poll/Load Event tail, and command-builder still producing Change stay on [[43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]].
4. Browser migrate — Poll consume, EventId cursor, PendingChange / ChangeBatch, and ClientHistory undo stay on [[44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId cursor]].
5. Contract deletes — Delete of HistoryEvent, ActorLifecycleEvent, mailbox `type History` / History name, PendingKind, the StartActorRequest name, and the ChangeLog name stays on [[45-contract-historyevent-clienthistory-pendingkind-and-changelog.md|45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog]]. ClientHistory remains.

## See also

[[../arch.md|Core creation architecture]], [[../reports/event-abstraction.md|Event abstraction]]

## Time

- 2026-09-15 1h30m — expand postEvent, EventLog ref via public API, Event JSON in EventJson (from chat)

## Comments

- 2026-09-15 — Filed via `/to-tickets` for Story **Caller, persist, and Poll** only (expand–contract). Sequence expand-migrate-contract on that story is skill `expand-contract`. First expand ticket. Blocked by story 4 expand [[37-expand-shared-event-eventlog-and-history.md|37 — Expand Shared Event, EventLog, and History]].
- 2026-09-15 — Did not edit [[src/Shared/EventLog.fs]], [[src/Shared/Event.fs]], or [[src/Shared/ClientHistory.fs]]. Alan locked newest-head EventLog and skip-non-change ClientHistory undo. Event JSON is [[src/Shared/EventJson.fs]]. Mailbox store calls `EventLog.append` / `EventLog.since` only. Report: [[../reports/implement-issue-40.md]].
