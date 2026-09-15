# 45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog

**Status:** blocked
**Blocked by:** [[41-migrate-core-mailbox-coremsg-and-pool-onto-event.md|41 — Migrate Core mailbox, CoreMsg, and Pool onto Event]], [[42-migrate-persisthandlers-restore-and-geteventssince.md|42 — Migrate PersistHandlers restore and getEventsSince]], [[43-migrate-http-adapter-onto-postevent-and-event-poll.md|43 — Migrate HTTP Adapter onto postEvent and Event Poll]], [[44-migrate-browser-poll-history-pending-and-eventid.md|44 — Migrate Browser Poll, History, pending, and EventId cursor]]

## Context

The migrate batches of Story **Caller, persist, and Poll** on [[../arch.md|Core creation architecture]] have moved callers onto Event, EventLog, ClientHistory, `postEvent`, Event Poll, and Event persist. HistoryEvent, ActorLifecycleEvent, mailbox `type History` / History name, PendingKind, the StartActorRequest name, and the ChangeLog name remain beside the new form. No caller should remain. This ticket is the contract hop: delete the old form. Do not delete ClientHistory. Field shapes: [[../reports/event-abstraction.md]]. Type inventory this replaces: [[../reports/event-abstraction.md]] section **Type inventory this replaces**.

## What to build

Delete the old form once no caller remains. Delete HistoryEvent, ActorLifecycleEvent, mailbox `type History` / History name (replaced by EventLog), and PendingKind. Delete the name StartActorRequest once every caller says ActorStart. Drop the ChangeLog name; persist is EventLog. Do not delete ClientHistory. CI stays green because every migrate batch is done.

### 1. Delete old Event and mailbox History types

Remove the replaced Shared types. Modules **Event** and **EventLog**. ClientHistory remains.

- [ ] Delete HistoryEvent — no HistoryEvent (`ChangeEvent` / `ActorEvent`) remains.
- [ ] Delete ActorLifecycleEvent — no ActorLifecycleEvent (`ActorStarted` / `ActorFinished`) remains.
- [ ] Delete mailbox History name — no `type History` / `module History` remains. EventLog replaced that mailbox-log role. Do not plan a new History at [[src/Shared/History.fs]].
- [ ] Delete PendingKind — no PendingKind remains. PendingChange / ChangeBatch wrap Event (or EventBody).
- [ ] ClientHistory remains — ClientHistory stays ClientHistory (Emacs Action view). Do not delete it.

### 2. Delete StartActorRequest name

Remove the old start-request name. Modules **CoreMailbox**, **CoreActorPool**, **HTTP Adapter**.

- [ ] Delete StartActorRequest name — every caller says ActorStart. The name StartActorRequest is gone.

### 3. Drop ChangeLog name

Rename persist to EventLog. Module **EventLog**. Persist seam **PersistHandlers**.

- [ ] Drop ChangeLog name — persist is EventLog. Payload is Event. There is no second log. Today’s [[src/Server/ChangeLog.fs]] name is gone.

## Out of scope

1. Shared Event expand — Shared Event / EventLog / ClientHistory functions stay on [[37-expand-shared-event-eventlog-and-history.md|37 — Expand Shared Event, EventLog, and History]]. Do not reopen that expand.
2. Cherry-pick Undo — invert a chosen Change Event; skip Actor Events. Deferred past hello on [[../arch.md|Core creation architecture]] Unsettled. Not this ticket.
3. Delete ClientHistory — ClientHistory is the destination Emacs Action view. Contract does not delete it.

## See also

[[../arch.md|Core creation architecture]], [[../reports/event-abstraction.md|Event abstraction]]

## Comments

- 2026-09-15 — Filed via `/to-tickets` for Story **Caller, persist, and Poll** only. Contract hop. Blocked by every story 5 migrate batch.
- 2026-09-15 — Contract deletes HistoryEvent, ActorLifecycleEvent, mailbox History name (replaced by EventLog), PendingKind, StartActorRequest, and the ChangeLog name. ClientHistory remains. Do not delete ClientHistory as if a History module replaced it.
