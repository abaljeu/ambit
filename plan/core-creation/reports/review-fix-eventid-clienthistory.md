# Review-fix: EventId helpers and ClientHistory Event stacks

Date: 2026-09-15. Ticket: [[../issues/37-expand-shared-event-eventlog-and-history.md|37 — Expand Shared Event, EventLog, and History]]. Status stays `coded`. Not committed.

Alan’s lock: `EventLog.restore` keeps source `nextId`; EventId named zero plus next/max; ClientHistory is Event lists only; no `HistoryRecord`, no `EventAction`, no Change two-stack.

## Files changed

1. [[src/Shared/Event.fs]] — `EventId` module (`zero`, `next`, `max`); `Event.isAction`.
2. [[src/Shared/EventLog.fs]] — restore keeps `log.nextId`; `EventId.zero` / `EventId.next`; unused `open`s removed.
3. [[src/Shared/ClientHistory.fs]] — Event stacks only; Change `record` / `undo` / `redo` wrap Event stacks.
4. [[tests/Shared.Tests/EventTests.fs]] — restore `nextId` assertions; `EventId.zero`; peek no longer uses a Change fallback stack.
5. [[tests/Shared.Tests/ClientHistoryTests.fs]] — drop HistoryRecord-stable `recordId` equalities on redo / folded undo.

Not touched: ticket 40, Event JSON, production callers (SyncLogic signatures unchanged).

## Signatures

Namespace `Gambol.Shared.Events`:

```fsharp
type EventId = EventId of int

module EventId =
    val zero: EventId
    val next: EventId -> EventId
    val max: EventId -> EventId -> EventId

module Event =
    val isAction: Event -> bool
    // existing: id, authority, ops, target, inverseOps, apply

module EventLog =
    val empty: EventLog
    val nextId: EventLog -> EventId
    val append: Event -> EventLog -> EventLog
    val since: EventId -> EventLog -> EventLog
    val tryFind: EventId -> EventLog -> Event option
    val restore: Event list -> EventLog -> EventLog
```

`restore` conses oldest-first persist (newest-head, dedupe `submissionId`). `nextId` stays `log.nextId`.

`Gambol.Shared` ClientHistory:

```fsharp
type ClientHistory =
    private
        { eventPast: Event list
          eventFuture: Event list
          nextEventId: EventId }

module ClientHistory =
    val clear: unit -> ClientHistory
    val recordEvent: string -> Event -> ClientHistory -> ClientHistory
    val undoEvent: ClientHistory -> (Event * ClientHistory) option
    val redoEvent: ClientHistory -> (Event * ClientHistory) option
    val tryPeekUndoName: ClientHistory -> string option
    val tryPeekRedoName: ClientHistory -> string option
    val record: string -> Change -> ClientHistory -> ClientHistory * int
    val undo:
        Revision -> Guid -> ClientHistory
        -> (Change * string * ClientHistory * int) option
    val redo:
        Revision -> Guid -> ClientHistory
        -> (Change * string * ClientHistory * int) option
```

Deleted: `HistoryRecord`, `EventAction`, `past` / `future` / `nextRecordId`.

Change-shaped `record` writes `EventBody.Change` with `commandName` on Event and `EventId` from `nextEventId`. `undo` / `redo` call `undoEvent` / `redoEvent`. Peek reads `event.commandName`. Undo/redo/peek skip ActorStart/ActorStop via `Event.isAction`.

## Tests

Foreground: EventTests, ClientHistoryTests, ClientHistoryRuntimeTests — 38 passed.

Callers: SyncLogicTests, LargeChangeApplyTests, AckReconcileTests — 55 passed.

Client compile gate: `./scripts/client.sh build` succeeded.

New / changed cases:

- `restore dedupe` — `EventLog.nextId` stays `EventId.zero` after restore onto empty.
- `restore keeps source nextId` — persist id 9 onto a log with `nextId` 3 leaves `nextId` 3.
- Peek test no longer assumes a separate Change stack.

## Ticket

[[../issues/37-expand-shared-event-eventlog-and-history.md|37]] Status remains `coded`.
