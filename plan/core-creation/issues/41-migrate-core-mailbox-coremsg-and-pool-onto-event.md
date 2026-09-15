# 41 — Migrate Core mailbox, CoreMsg, and Pool onto Event

**Status:** coded
**Blocked by:** None — [40 — Expand postEvent, EventLog store, and Event JSON persist](40-expand-postevent-eventlog-and-event-json.md) is Status `coded`.

## Context

[40 — Expand postEvent, EventLog store, and Event JSON persist](40-expand-postevent-eventlog-and-event-json.md) adds `postEvent`, an EventLog mailbox store, and Event JSON beside ChangeLog. Core Changes callers still use the old door. GetEventHistory is still a two-stack. The mailbox does not yet append ActorStart / ActorStop Events. Start still uses the StartActorRequest name. Story **Caller, persist, and Poll** on [Core creation architecture](../arch.md) migrates this Core layer next. Field shapes: [[../reports/event-abstraction.md]].

## What to build

Move Core mailbox, CoreMsg, and Pool callers onto Event. Changes callers use `postEvent`. Name-only Undo/Redo may carry only `target`; dispatch fills inverse Ops and stores that completed Event (same `submissionId`). GetEventHistory returns the log or `since`, not a two-stack. The mailbox appends ActorStart / ActorStop Events; callers do not `postEvent` those bodies. Every start request is ActorStart. Core stamps `authority` on every stored Event from the admitted Caller. Command-builder still produces Change; Event is built at the `postEvent` door. The old types still compile. CI stays green.

### 1. postEvent callers and name-only Undo/Redo

Migrate Changes callers on **CoreMailbox** and **CoreMsg / CoreMailboxBackend**. Seam **`postEvent` door**.

- [x] Changes callers use postEvent — Core Changes callers use `postEvent`. Event is built at that door.
- [x] Name-only Undo/Redo — name-only Undo/Redo may carry only `target`. Dispatch `tryFind`s the target Event, fills inverse Ops, and stores the completed Event (same `submissionId`).

### 2. GetEventHistory

Migrate the history door on **CoreMailbox**.

- [x] GetEventHistory log or since — `eventHistory` / `GetEventHistory` returns the EventLog (the log or `since`), not a two-stack.

### 3. ActorStart and ActorStop on EventLog

Migrate lifecycle append on **CoreMsg / CoreMailboxBackend** and **CoreActorPool**. Seam **CoreActorPool table and start**.

- [x] Mailbox appends ActorStart — mailbox appends ActorStart Events. Callers do not `postEvent` those bodies. Interface: [Core creation architecture](../arch.md) Module **CoreActorPool**.
- [x] Mailbox appends ActorStop — mailbox stores an ActorStop Event on EventLog for `ActorResult`. Callers do not `postEvent` ActorStop. Interface: [Core creation architecture](../arch.md) Module **CoreMsg / CoreMailboxBackend**.
- [x] start request is ActorStart — every start request is ActorStart. The StartActorRequest name still compiles until contract.

### 4. Authority stamp

Stamp authority on **CoreMsg / CoreMailboxBackend**.

- [x] authority from admitted Caller — stamp `authority` from the admitted Caller on every stored Event. The wire does not supply it.

## Out of scope

1. PersistHandlers migrate — File/Db `EventLog.restore`, `getEventsSince` as Events, and persist of ActorStart / ActorStop stay on [42 — Migrate PersistHandlers restore and getEventsSince](42-migrate-persisthandlers-restore-and-geteventssince.md).
2. HTTP Adapter migrate — Change posts calling `postEvent` from [[src/Server/Api.fs]], Poll/Load Event tail, and command-builder still producing Change stay on [43 — Migrate HTTP Adapter onto postEvent and Event Poll](43-migrate-http-adapter-onto-postevent-and-event-poll.md).
3. Browser migrate — Poll consume, EventId cursor, PendingChange / ChangeBatch, and ClientHistory undo stay on [44 — Migrate Browser Poll, History, pending, and EventId cursor](44-migrate-browser-poll-history-pending-and-eventid.md).
4. Contract deletes — Delete of HistoryEvent, ActorLifecycleEvent, mailbox `type History` / History name, PendingKind, the StartActorRequest name, and the ChangeLog name stays on [45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog](45-contract-historyevent-clienthistory-pendingkind-and-changelog.md). ClientHistory remains.

## See also

[Core creation architecture](../arch.md), [Event abstraction](../reports/event-abstraction.md)

## Comments

- 2026-09-15 — Filed via `/to-tickets` for Story **Caller, persist, and Poll** only. Core-layer migrate batch. Blocked by the story 5 expand.
- 2026-09-15 — Status corrected to `coded` (implementation already landed; ticket state lagged).
