# 37 — Expand Shared Event, EventLog, and History

**Status:** coded
Actual: 2h30m
**Blocked by:** None — can start immediately.

## Context

Shared still uses HistoryEvent (`ChangeEvent` / `ActorEvent`), ActorLifecycleEvent (`ActorStarted` / `ActorFinished`), ClientHistory (Browser Emacs undo, Change-shaped), and Change with a log position named Revision. Production callers still use those types. The Event destination is locked: one Event type; mailbox is intake; EventLog is the store after intake; ClientHistory is the Emacs Action view; persistence is the persisted EventLog. There is no destination module named History. Today’s `type History` / `module History` in [[src/Shared/History.fs]] is the lagging mailbox-log name and becomes EventLog. Story **Event, EventLog, and ClientHistory** on [[../arch.md|Core creation architecture]] sequences expand-migrate-contract and is the Shared slice only. This ticket is that expand: add the new form beside the old. Field shapes: [[../reports/event-abstraction.md]].

## What to build

Add Shared Event and EventLog beside the old types, and Event-shaped functions on ClientHistory, so nothing breaks. Do not add a new Shared History type. Do not plan a module at [[src/Shared/History.fs]]. EventId, ActorStart, EventBody, and Event exist in Shared. The Event, EventLog, and ClientHistory modules expose the locked functions. Authority, ActorStart, and ActorResult live in Shared with Event. Shared.Tests prove the narrowest test seam: Shared Event / EventLog / ClientHistory functions. ClientHistory and HistoryEvent still compile. This story has no production callers to migrate and does not delete HistoryEvent, ActorLifecycleEvent, or ClientHistory.

### 1. Event

Add Shared **Event** beside HistoryEvent. State, Interface, and Uses: [[../arch.md|Core creation architecture]] Module **Event**. Field shapes: [[../reports/event-abstraction.md]].

- [x] Additive Shared EventId — add EventId, ActorStart, EventBody, and Event in Shared beside HistoryEvent. EventId is the log position (event id). EventBody is Change / Undo / Redo / ActorStart / ActorStop. Event holds id, submissionId, authority, and body. `commandName` lives on Event. ActorStart is the start request (zoom, focus, command, graphIds, basis EventId). ActorStop is focusId plus ActorResult.
- [x] Event functions — `id`, `authority`, `ops` (none for Actor bodies), `target` (none except Undo/Redo), `apply` (Graph apply via those Ops; Actor bodies do not touch the Graph), `inverseOps` (when building Undo/Redo from a target Event). Change is the command-builder product (Ops). Every Event carries Authority; Core stamps it from the admitted Caller; the wire does not supply it.
- [x] Shared Authority ActorStart ActorResult — Authority, ActorStart, and ActorResult live in Shared with Event.

### 2. EventLog

Add Shared **EventLog** beside the old mailbox History sequence. State, Interface, and Uses: [[../arch.md|Core creation architecture]] Module **EventLog**. Field shapes: [[../reports/event-abstraction.md]]. EventLog replaces the mailbox History role. Today’s `type History` / `module History` in [[src/Shared/History.fs]] stays until contract; do not add a second History module.

- [x] EventLog functions — `empty`, `append`, `nextId`, `since`, `tryFind`, `restore`. The log is append-only, newest-head. `since` is the Poll/Load tail of self-contained Events. `tryFind` serves Core name-only Undo/Redo. `restore` merges persisted Events and dedupes by submissionId. Do not add Event JSON encode/read or persist of ActorStart / ActorStop on this ticket.

### 3. ClientHistory

Evolve [[src/Shared/ClientHistory.fs]] beside the old Change-shaped API, or add Event-shaped functions on ClientHistory. State, Interface, and Uses: [[../arch.md|Core creation architecture]] Module **ClientHistory**. Field shapes: [[../reports/event-abstraction.md]]. Do not add a new Shared History type.

- [x] ClientHistory Emacs Actions — Event-shaped `record commandName event` folds future into past. `undo` / `redo` move the local stack and produce the Undo/Redo Event (target plus inverse Ops). `tryPeekUndoName` / `tryPeekRedoName` peek. State is newest-head past/future of Actions (Change/Undo/Redo). `commandName` lives on Event; `record` still takes it for peek. ClientHistory is not persisted and is not sent on Poll. The Change-shaped ClientHistory API may remain beside the Event-shaped functions. Do not add a new Shared History type.

### 4. Shared.Tests

Prove the narrowest test seam for Story **Event, EventLog, and ClientHistory**: Shared Event / EventLog / ClientHistory functions.

- [x] append since tryFind — EventLog append, since, and tryFind
- [x] restore dedupe — EventLog restore dedupes by submissionId
- [x] ClientHistory record fold — ClientHistory.record folds future into past
- [x] Undo inverse Ops — undo produces `Undo(target, inverseOps)` and redo names the Undo Event
- [x] Actor bodies do not apply — Event.apply of ActorStart / ActorStop does not touch the Graph
- [x] Undo Redo carried Ops — Event.apply of Undo/Redo uses carried Ops (no lookup)
- [x] Every Event carries Authority — every Event carries Authority
- [x] ActorStart body equals start request — ActorStart body equals the start request
- [x] ActorStop carries ActorResult — ActorStop carries ActorResult
- [x] commandName is on the Event — commandName is on the Event, not only on ClientHistory

### 5. Compile beside old

Keep the old form. Expand does not replace it.

- [x] ClientHistory and HistoryEvent still compile — ClientHistory and HistoryEvent still compile. Do not delete HistoryEvent, ActorLifecycleEvent, or ClientHistory. Do not add a new Shared History type.

## Out of scope

1. Migrate production callers — Story **Caller, persist, and Poll** moves callers onto Event. This story has no production callers to migrate.
2. Contract deletes — Story **Caller, persist, and Poll** deletes HistoryEvent, ActorLifecycleEvent, mailbox `type History` / History name (replaced by EventLog), and PendingKind when no caller remains. ClientHistory remains. This story does not delete those types.
3. postEvent and persist — CoreMailbox `postEvent`, mailbox EventLog store, Event JSON persist, Poll Event tail, GetEventHistory as log-or-since, the StartActorRequest name drop, and the ChangeLog name drop stay on Story **Caller, persist, and Poll**.

## See also

[[../arch.md|Core creation architecture]], [[../reports/event-abstraction.md|Event abstraction]]

## Comments

- 2026-09-15 — Filed via `/to-tickets` for Story **Event, EventLog, and History** only (expand–contract, Shared expand). Sequence expand-migrate-contract on that story is skill `expand-contract`. No migrate batch and no contract delete in this story. Story **Caller, persist, and Poll** is not ticketed.
- 2026-09-15 — Destination module 6 is ClientHistory, not a new History. Section 3 evolves ClientHistory (Event-shaped beside Change-shaped). EventLog replaces the mailbox History role. Do not add a Shared History type.
- 2026-09-15 — Shared expand coded. Event types live in namespace Gambol.Shared.Events. Event-shaped ClientHistory API is recordEvent / undoEvent / redoEvent beside the Change API. No destination History module.
- 2026-09-15 — Alan review lock: ClientHistory undo/redo/peek skip ActorStart/ActorStop (leave them on the stack; invert the next Action only). ClientHistory.nextEventId is EventId. EventLog is newest-head throughout with no List.rev: append cons, since filters to EventLog (not Event list), restore conses oldest-first persist. CoreMailbox.eventsSince returns EventLog.

## Time

- 2026-09-15 1h30m — Shared Event / EventLog / ClientHistory expand (from chat)
- 2026-09-15 1h — Review lock: skip-non-change undo, newest-head EventLog, since returns EventLog (from chat)
