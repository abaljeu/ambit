# Implement 37 — Expand Shared Event, EventLog, and History

Date: 2026-09-15. Ticket: [[../issues/37-expand-shared-event-eventlog-and-history.md|37 — Expand Shared Event, EventLog, and History]]. Status `coded`. Not committed.

## 1. Files added or changed

1. [[src/Shared/Event.fs]] — added. Namespace `Gambol.Shared.Events`.
2. [[src/Shared/EventLog.fs]] — added. Namespace `Gambol.Shared.Events`.
3. [[src/Shared/ClientHistory.fs]] — Event-shaped API beside Change API. Still `Gambol.Shared`.
4. [[src/Shared/Gambol.Shared.fsproj]] — compile Event.fs then EventLog.fs before ClientHistory.fs.
5. [[tests/Shared.Tests/EventTests.fs]] — added.
6. [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]] — register EventTests.fs.
7. [[src/Server/Core/CoreChanges.fs]] — Server `Authority` / `ActorResult` kept (not migrated onto Events).
8. Ticket and [[../project.md]] Stage / time.

## 2. Public types and functions

Namespace `Gambol.Shared.Events` only (not `Gambol.Shared`):

```fsharp
type EventId = EventId of int
type Authority = Authority of string
type ActorResult = ActorSucceeded | ActorFailed
type ActorStart =
    { zoomId: NodeId; focusId: NodeId; commandId: NodeId
      graphIds: NodeId list; revision: EventId }
type EventBody =
    | Change of ops: Op list
    | Undo of target: EventId * ops: Op list
    | Redo of target: EventId * ops: Op list
    | ActorStart of ActorStart
    | ActorStop of focusId: NodeId * result: ActorResult
type Event =
    { id: EventId; submissionId: Guid; authority: Authority
      commandName: string; body: EventBody }
module Event =
    val id: Event -> EventId
    val authority: Event -> Authority
    val ops: Event -> Op list option
    val target: Event -> EventId option
    val inverseOps: Event -> Op list option
    val apply: Event -> State -> ApplyResult
type EventLog = { events: Event list; nextId: EventId }
module EventLog =
    val empty: EventLog
    val nextId: EventLog -> EventId
    val append: Event -> EventLog -> EventLog
    val since: EventId -> EventLog -> Event list
    val tryFind: EventId -> EventLog -> Event option
    val restore: Event list -> EventLog -> EventLog
```

ClientHistory (`Gambol.Shared`) Event-shaped beside Change (F# cannot overload `record`):

```fsharp
val recordEvent: string -> Event -> ClientHistory -> ClientHistory
val undoEvent: ClientHistory -> (Event * ClientHistory) option
val redoEvent: ClientHistory -> (Event * ClientHistory) option
```

`tryPeekUndoName` / `tryPeekRedoName` peek the Event stack when present, else the Change stack.

## 3. Tests

[[tests/Shared.Tests/EventTests.fs]] — 10 passed. Existing ClientHistoryTests — 15 passed. History + ClientHistoryRuntime — 72 passed. Client compile gate passed. Server build passed.

## 4. Old types still compile

`type History` / `module History`, `HistoryEvent`, `ActorLifecycleEvent`, and Change-shaped ClientHistory remain in [[src/Shared/History.fs]] and [[src/Shared/ClientHistory.fs]]. No destination History module. Server `Authority` / `ActorResult` remain on CoreChanges.

## 5. Ticket and Stage

Ticket Status `coded`. Checkboxes `[x]`. Stage `build`. Actual 1h30m on the ticket; project Actual 47h05m.

## 6. Spec notes

1. All new Event types live in `Gambol.Shared.Events` so they sit beside History/Change without a Fable `Event` clash.
2. Event-shaped ClientHistory functions are `recordEvent` / `undoEvent` / `redoEvent` because module `record` / `undo` / `redo` already take Change.
3. `since` is exclusive (`id > eventId`).
4. No Event JSON, no persist of ActorStart/ActorStop, no production caller migration, no commit.
