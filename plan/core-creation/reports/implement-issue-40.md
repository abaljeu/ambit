# Implement 40 — Expand postEvent, EventLog store, and Event JSON persist

Date: 2026-09-15. Ticket: [[../issues/40-expand-postevent-eventlog-and-event-json.md|40 — Expand postEvent, EventLog store, and Event JSON persist]]. Status `coded`. Not committed. [[../issues/37-expand-shared-event-eventlog-and-history.md|37 — Expand Shared Event, EventLog, and History]] stays `coded` (not `done`).

## 1. What landed

1. CoreMailbox `postEvent` door. Payload is Event. Admits the Caller. Today’s `postChange` still compiles.
2. Mailbox store is `EventLog ref`. CoreMailboxBackend is the only writer. GetEventHistory stays the History two-stack.
3. Event JSON encode/read lives on [[src/Shared/EventLog.fs]] (`Gambol.Shared.Events`). No EventJson sidecar. No ChangeLog Event codec. ChangeLog still appends opaque Change lines.

Did not revert newest-head EventLog, ClientHistory eventPast/eventFuture, or `eventsSince : Async<EventLog>`.

## 2. Files

1. [[src/Shared/EventLog.fs]] — `encode` / `decode` on EventLog. Compile after Serialization.fs.
2. [[src/Shared/EventJson.fs]] — deleted.
3. [[src/Server/ChangeLog.fs]] — Event encode/decode removed.
4. [[src/Server/Core/CoreMsg.fs]] — `PostEvent`, `EventsSince`.
5. [[src/Server/Core/CoreMailbox.fs]] — `postEvent`, `eventsSince`.
6. [[src/Server/Core/CoreMailboxBackend.fs]] — `eventLog` store, dispatch.
7. [[tests/Shared.Tests/EventJsonTests.fs]] — EventLog.encode / EventLog.decode round-trips.
8. [[tests/Server.Tests/CoreMailboxDoorTests.fs]] — postEvent + since membership, refuse.
9. Ticket and [[../project.md]] time / Status.

## 3. Public signatures

```fsharp
// Gambol.Shared.Events.EventLog
val empty: EventLog
val nextId: EventLog -> EventId
val append: Event -> EventLog -> EventLog
val since: EventId -> EventLog -> EventLog
val tryFind: EventId -> EventLog -> Event option
val restore: Event list -> EventLog -> EventLog
val encode: Event -> IEncodable
val decode: Decoder<Event>

// Gambol.Server.CoreMailbox
val postEvent:
    MailboxHost -> Caller -> Event -> Async<Result<Event, string>>
val eventsSince: MailboxHost -> EventId -> Async<EventLog>
```

`postEvent` appends via `EventLog.append` and returns the stored Event (id from `EventLog.nextId`). It does not persist File/Db, apply Graph, stamp authority, fill name-only Undo/Redo, or append ActorStart / ActorStop.

## 4. Tests

1. EventJsonTests — 3 passed (Change, name-only Undo, ActorStart / ActorStop) through EventLog.encode / decode.
2. EventTests — 15 passed (newest-head append / since).
3. CoreMailboxDoorTests — 25 passed (postEvent + since membership, refuse).
4. Client compile gate passed. Full Shared+Server suite started after coding.

## 5. Ticket and Stage

Ticket Status `coded`. Checkboxes `[x]`. Stage stays `build`. Actual 2h15m on the ticket. No commit.
