# 37 — Expand Shared Event, EventLog, and History

**Status:** ready-to-implement
**Blocked by:** None — can start immediately.

## Context

Shared still uses HistoryEvent (`ChangeEvent` / `ActorEvent`), ActorLifecycleEvent (`ActorStarted` / `ActorFinished`), ClientHistory (Browser Emacs undo), and Change with a log position named Revision. Production callers still use those types. The Event destination is locked: one Event type; mailbox is intake; EventLog is the store after intake; History is the Emacs Action view; persistence is the persisted EventLog. Story **Event, EventLog, and History** on [[../arch.md|Core creation architecture]] sequences expand-migrate-contract and is the Shared slice only. This ticket is that expand: add the new form beside the old. Field shapes: [[../reports/event-abstraction.md]].

## What to build

Add Shared Event, EventLog, and History beside the old types so nothing breaks. EventId, ActorStart, EventBody, and Event exist in Shared. The Event, EventLog, and History modules expose the locked functions. Authority, ActorStart, and ActorResult live in Shared with Event. Shared.Tests prove the narrowest test seam: Shared Event / EventLog / History functions. ClientHistory and HistoryEvent still compile. This story has no production callers to migrate and does not delete HistoryEvent, ActorLifecycleEvent, or ClientHistory.

### 1. Event

Add Shared **Event** beside HistoryEvent. State, Interface, and Uses: [[../arch.md|Core creation architecture]] Module **Event**. Field shapes: [[../reports/event-abstraction.md]].

- [ ] Additive Shared EventId — add EventId, ActorStart, EventBody, and Event in Shared beside HistoryEvent. EventId is the log position (event id). EventBody is Change / Undo / Redo / ActorStart / ActorStop. Event holds id, submissionId, authority, and body. ActorStart is the start request (zoom, focus, command, graphIds, basis EventId). ActorStop is focusId plus ActorResult.
- [ ] Event functions — `id`, `authority`, `ops` (none for Actor bodies), `target` (none except Undo/Redo), `apply` (Graph apply via those Ops; Actor bodies do not touch the Graph), `inverseOps` (when building Undo/Redo from a target Event). Change is the command-builder product (Ops). Every Event carries Authority; Core stamps it from the admitted Caller; the wire does not supply it.
- [ ] Shared Authority ActorStart ActorResult — Authority, ActorStart, and ActorResult live in Shared with Event.

### 2. EventLog

Add Shared **EventLog** beside the old HistoryEvent sequence. State, Interface, and Uses: [[../arch.md|Core creation architecture]] Module **EventLog**. Field shapes: [[../reports/event-abstraction.md]].

- [ ] EventLog functions — `empty`, `append`, `nextId`, `since`, `tryFind`, `restore`. The log is append-only, oldest-head. `since` is the Poll/Load tail of self-contained Events. `tryFind` serves Core name-only Undo/Redo. `restore` merges persisted Events and dedupes by submissionId. Do not add Event JSON encode/read or persist of ActorStart / ActorStop on this ticket.

### 3. History

Add Shared **History** as the Emacs Action view beside ClientHistory. State, Interface, and Uses: [[../arch.md|Core creation architecture]] Module **History**. Field shapes: [[../reports/event-abstraction.md]].

- [ ] History Emacs Actions — `record commandName event` folds future into past. `undo` / `redo` move the local stack and produce the Undo/Redo Event (target plus inverse Ops). `tryPeekUndoName` / `tryPeekRedoName` peek. State is newest-head past/future of Actions (Change/Undo/Redo) with commandName for peek. History is not persisted and is not sent on Poll.

### 4. Shared.Tests

Prove the narrowest test seam for Story **Event, EventLog, and History**: Shared Event / EventLog / History functions.

- [ ] append since tryFind — EventLog append, since, and tryFind
- [ ] restore dedupe — EventLog restore dedupes by submissionId
- [ ] History record fold — History.record folds future into past
- [ ] Undo inverse Ops — undo produces `Undo(target, inverseOps)` and redo names the Undo Event
- [ ] Actor bodies do not apply — Event.apply of ActorStart / ActorStop does not touch the Graph
- [ ] Undo Redo carried Ops — Event.apply of Undo/Redo uses carried Ops (no lookup)
- [ ] Every Event carries Authority — every Event carries Authority
- [ ] ActorStart body equals start request — ActorStart body equals the start request
- [ ] ActorStop carries ActorResult — ActorStop carries ActorResult

### 5. Compile beside old

Keep the old form. Expand does not replace it.

- [ ] ClientHistory and HistoryEvent still compile — ClientHistory and HistoryEvent still compile. Do not delete HistoryEvent, ActorLifecycleEvent, or ClientHistory.

## Out of scope

1. Migrate production callers — Story **Caller, persist, and Poll** moves callers onto Event. This story has no production callers to migrate.
2. Contract deletes — Story **Caller, persist, and Poll** deletes HistoryEvent, ActorLifecycleEvent, and ClientHistory when no caller remains. This story does not delete those types.
3. postEvent and persist — CoreMailbox `postEvent`, mailbox EventLog store, Event JSON persist, Poll Event tail, GetEventHistory as log-or-since, the StartActorRequest name drop, and the ChangeLog name drop stay on Story **Caller, persist, and Poll**.

## See also

[[../arch.md|Core creation architecture]], [[../reports/event-abstraction.md|Event abstraction]]

## Comments

- 2026-09-15 — Filed via `/to-tickets` for Story **Event, EventLog, and History** only (expand–contract, Shared expand). Sequence expand-migrate-contract on that story is skill `expand-contract`. No migrate batch and no contract delete in this story. Story **Caller, persist, and Poll** is not ticketed.
