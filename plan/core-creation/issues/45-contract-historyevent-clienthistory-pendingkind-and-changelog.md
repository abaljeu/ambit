# 45 — Contract HistoryEvent, ClientHistory, PendingKind, StartActorRequest, and ChangeLog

**Status:** blocked
**Blocked by:** [[41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]], [[42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]], [[43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]], [[44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId cursor]]

## Context

The migrate batches of Story **Caller, persist, and Poll** on [[../arch.md|Core creation architecture]] have moved callers onto Event, EventLog, History, `postEvent`, Event Poll, and Event persist. HistoryEvent, ActorLifecycleEvent, ClientHistory, PendingKind, the StartActorRequest name, and the ChangeLog name remain beside the new form. No caller should remain. This ticket is the contract hop: delete the old form. Field shapes: [[../reports/event-abstraction.md]]. Type inventory this replaces: [[../reports/event-abstraction.md]] section **Type inventory this replaces**.

## What to build

Delete the old form once no caller remains. Delete HistoryEvent, ActorLifecycleEvent, ClientHistory, and PendingKind. Delete the name StartActorRequest once every caller says ActorStart. Drop the ChangeLog name; persist is EventLog. CI stays green because every migrate batch is done.

### 1. Delete old Event and History types

Remove the replaced Shared types. Modules **Event**, **EventLog**, **History**.

- [ ] Delete HistoryEvent — no HistoryEvent (`ChangeEvent` / `ActorEvent`) remains.
- [ ] Delete ActorLifecycleEvent — no ActorLifecycleEvent (`ActorStarted` / `ActorFinished`) remains.
- [ ] Delete ClientHistory — no ClientHistory remains. History is the Emacs Action view.
- [ ] Delete PendingKind — no PendingKind remains. PendingChange / ChangeBatch wrap Event (or EventBody).

### 2. Delete StartActorRequest name

Remove the old start-request name. Modules **CoreMailbox**, **CoreActorPool**, **HTTP Adapter**.

- [ ] Delete StartActorRequest name — every caller says ActorStart. The name StartActorRequest is gone.

### 3. Drop ChangeLog name

Rename persist to EventLog. Module **EventLog**. Persist seam **PersistHandlers**.

- [ ] Drop ChangeLog name — persist is EventLog. Payload is Event. There is no second log. Today’s [[src/Server/ChangeLog.fs]] name is gone.

## Out of scope

1. Shared Event expand — Shared Event / EventLog / History functions stay on [[37-expand-shared-event-eventlog-and-history.md|37 — Expand Shared Event, EventLog, and History]]. Do not reopen that expand.
2. Cherry-pick Undo — invert a chosen Change Event; skip Actor Events. Deferred past hello on [[../arch.md|Core creation architecture]] Unsettled. Not this ticket.

## See also

[[../arch.md|Core creation architecture]], [[../reports/event-abstraction.md|Event abstraction]]

## Comments

- 2026-09-15 — Filed via `/to-tickets` for Story **Caller, persist, and Poll** only. Contract hop. Blocked by every story 5 migrate batch.
