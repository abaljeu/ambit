# Spec review 37 and 40

Date: 2026-09-19
Tip: `fce22cf7f5ead1db27d0b5635c610c8c54d432f0`
Axis: Spec only. No Standards scan. No feature code.

Tickets: [37 — Expand Shared Event, EventLog, and History](../issues/37-expand-shared-event-eventlog-and-history.md), [40 — Expand postEvent, EventLog store, and Event JSON persist](../issues/40-expand-postevent-eventlog-and-event-json.md).

Spec aids: [Core creation architecture](../arch.md) Story **Event, EventLog, and ClientHistory** expand and Story **Caller, persist, and Poll** expand. Field shapes: [Event abstraction](event-abstraction.md).

Later [41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md) through [45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog](../issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md) and [46 — Mailbox History durability](../issues/46-mailbox-history-durability.md) sit on this expand. Expand-only “keep the old form” lines are historical. Missing old names at this tip are not Spec gaps.

## Spec

### 37 — Expand Shared Event, EventLog, and History

(a) Missing or partial — none.

1. Ev — `EventId`, `ActorStart`, `EventBody`, `Ev`, `Authority`, and `ActorResult` live in `Gambol.Shared` in [History.fs](../../../src/Shared/History.fs). `EventBody` is Change / Undo / Redo / ActorStart / ActorStop. `Ev` holds `id`, `submissionId`, `authority`, `commandName`, and `body`. Module `Ev` exposes `id`, `authority`, `ops`, `target`, `apply`, `inverseOps`. Arch expand also names `isAction`, `fromJson`, `toJson`; those exist.
2. EventLog — [EventLog.fs](../../../src/Shared/EventLog.fs) exposes `empty`, `append`, `nextId`, `since`, `tryFind`, `restore`. The log is append-only newest-head (cons). `since` returns EventLog. `restore` conses oldest-first persist and dedupes by `submissionId`.
3. ClientHistory — [ClientHistory.fs](../../../src/Shared/ClientHistory.fs) has Event-shaped `recordEvent` / `undoEvent` / `redoEvent` plus `tryPeekUndoName` / `tryPeekRedoName`. `recordEvent` folds future into past. Undo/Redo produce `Undo`/`Redo` with target and inverse Ops. Alan lock: undo/redo/peek skip ActorStart/ActorStop and leave them on the stack.
4. Shared.Tests — [EventTests.fs](../../../tests/Shared.Tests/EventTests.fs) covers every named 37 seam: append/since/tryFind; restore dedupe; record fold; Undo inverse Ops; Actor bodies do not apply; Undo/Redo carried Ops; every Ev carries Authority; ActorStart body equals the start request; ActorStop carries ActorResult; `commandName` is on Ev.
5. No destination History module. No `Gambol.Shared.Events` namespace.

(b) Scope creep — none for this ticket. Extra EventLog helpers (`recover`, `tip`, `adoptNewestHead`) belong to later [46 — Mailbox History durability](../issues/46-mailbox-history-durability.md). Extra ClientHistory `record` / `approve` / `mintChange` belong to later Browser/SES work.

(c) Implemented but wrong — none. `Ev.apply` of Actor bodies returns `Unchanged`. `Ev.apply` of Undo/Redo uses carried Ops. CoreMailbox `eventsSince` returns EventLog.

Notes, not gaps:

1. Ticket What to build names `record commandName event`. Comments lock `recordEvent`. Both names exist; `record` now takes the Ev only.
2. Alan lock named `ClientHistory.nextEventId`. That field is gone. EventId remains the log serial.
3. Arch expand item 4.1.7 says `HistoryEvent` deleted. Ticket Out of scope and Arch contract 4.3.1 say this story does not delete it. [45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog](../issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md) deleted it later.

### 40 — Expand postEvent, EventLog store, and Event JSON persist

(a) Missing or partial — none.

1. postEvent door — [CoreMailbox.fs](../../../src/Server/Core/CoreMailbox.fs) `postEvent` takes one Ev. `postEvents` posts each Ev. Payload is Change / Undo / Redo. Name-only Undo/Redo with empty Ops is filled in [CoreEventDispatch.fs](../../../src/Server/Core/CoreEventDispatch.fs) (`completeAction` + `tryFind`).
2. Mailbox EventLog store — [CoreMailboxBackend.fs](../../../src/Server/Core/CoreMailboxBackend.fs) holds `eventLog: EventLog ref`. Backend is the writer of that store (through CoreEventDispatch on the loop). `GetEventHistory` replies with that EventLog. `EventsSince` is `EventLog.since`.
3. Event JSON persist — [EventJson.fs](../../../src/Shared/EventJson.fs) encodes and reads Ev. [EventLogFile.fs](../../../src/Server/EventLogFile.fs) writes `gambol.events`. FileAgent appends that EventLog. DbAgent writes the `events` table with the same JSON. Persist is this EventLog on file/DB.
4. Narrowest test seam — [CoreMailboxDoorTests.fs](../../../tests/Server.Tests/CoreMailboxDoorTests.fs) `CoreMailbox.postEvent appends an Ev that eventsSince returns`. Shared EventLog `since` is in [EventTests.fs](../../../tests/Shared.Tests/EventTests.fs). Event JSON round-trip is in [EventJsonTests.fs](../../../tests/Shared.Tests/EventJsonTests.fs).

(b) Scope creep — none for this ticket. Authority stamp, ActorStart/ActorStop append, Poll Ev tail, and caller migrate belong to later 41–44.

(c) Implemented but wrong — none. `postEvent` appends an Ev; `EventLog.since` / `eventsSince` return that tail.

Notes, not gaps:

1. Ticket comments once moved encode/read onto EventLog and deleted EventJson. Current home is EventJson, which matches Arch Module **EventLog** Interface item 7.
2. Ticket said keep the ChangeLog name. [45 — Contract HistoryEvent, mailbox History, PendingKind, StartActorRequest, and ChangeLog](../issues/45-contract-historyevent-clienthistory-pendingkind-and-changelog.md) dropped it later.

## Verdict

1. [37 — Expand Shared Event, EventLog, and History](../issues/37-expand-shared-event-eventlog-and-history.md) — Spec pass. Status `done`.
2. [40 — Expand postEvent, EventLog store, and Event JSON persist](../issues/40-expand-postevent-eventlog-and-event-json.md) — Spec pass. Status `done`.

## Summary

Spec findings: 0. Worst Spec issue: none. Both tickets `done`.
