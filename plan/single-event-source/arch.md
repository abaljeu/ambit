# Single event source architecture

Spec: [[map.md]]
Updated: 2026-09-17
Sequence: expand-contract

Feature under design: leftover Change record and Revision serial out of the running code. Ev is transported. Ops are not. Apply, validation, invert, amend, and PersistStamp take an Op list locally. Decisions: [[map.md]]. Inventory: [[reports/inventory-non-event-write-paths.md]]. Do not edit [[plan/core-creation/arch.md]] until [04 — Write core-creation arch.md last](issues/04-write-core-creation-arch-md-last.md). Prefer existing seams. Do not open Wayfinder map tickets for items under Unsettled.

## 1. Story paths

1. **Leftover Change and Revision**
   Sequence: expand-migrate-contract.
   1. **Expand**
      1. [x] Ev, EventLog, `postEvents`, HTTP Ev batch, Poll/Load Ev tail (from [[plan/core-creation/arch.md]] Stories **Event, EventLog, and ClientHistory** and **Caller, persist, and Poll**)
      2. [x] Op-list apply — `Ev.apply` / invert / `ChangeValidation` / amend / PersistStamp take `Op list` (or Ops on EventBody). They do not wrap leftover Change
      3. [x] leftover Change.id is `EventId` (same type as `Ev.id`). No `Revision` stop. EventId has private id
   2. **Migrate**
      Batches after compile may run in any order among persist and command. Serial batch may run beside them. Leftover Change still compiles until Contract.
      1. [x] Compile preamble — [[tests/Server.Tests/TestBackend.fs]] `Ev` type and `Authority` constructor in scope (`module Ev` must not shadow the type)
      2. [x] Persist apply — [[src/Server/Core/FileAgent.fs]] / [[src/Server/Core/DbAgent.fs]] / PersistStamp / [[src/Server/Core/CoreEventDispatch.fs]] admit Ev, apply Ops, `appendEvent` Ev. No Ev→Change copy for apply
         Leftover `postChange` waits for [08 — Core doors](issues/08-core-doors.md). `Ev.asChange` / `Ev.ofChange` wait for [12 — Contract leftover Change and Revision](issues/12-contract-leftover-change-and-revision.md).
      3. [x] Core doors — **CoreChanges** `postEvents` and `postGraphOnly` both take Ev. `postChange` (Change list) and `PostGraphOnlyChange` of leftover Change are gone. Graph-only still skips file persist, not EventLog
      4. [x] Command mint — Browser command builders and Parse [[src/Server/GraphOnlyChangePost.fs]] mint Ev (`EventId.zero`, `commandName`). Run is ActorStart or a Change Event with that Run command in `commandName`
      5. [ ] Boot IndexedDB — [[src/Shared/BootCache.fs]] / [[src/Client/BootCacheStore.fs]] hold Ev list, not leftover Change
      6. [x] One serial — `Revision` type and `revision` fields become event id. JSON key `"eventId"`. `getEventId`. `Change.id` is already `EventId` from Expand. EventId.fromJson/toJson bypasses. EventId.next adds one. Ev.fromJson/toJson. Only serializing uses fromJson/toJson. Only EventLog uses EventId.next. Any event not from these sources has id 0. The client has no EventId serial. Pending events use `EventId.zero`. Match server responses with `submissionId`. On approve, replace zero with the server-assigned id. On interject, rewind
   3. **Contract**
      1. [ ] Delete leftover `{ id; submissionId; ops }` record, `module Change` apply wrapping, `Ev.ofChange` / `Ev.asChange`, `eventFromChange`
      2. [ ] Delete unused [[src/Shared/EventId.fs]]
      3. [ ] Delete `type Revision` and `EventId.ofRevision` / `toRevision`
      4. [ ] [04 — Write core-creation arch.md last](issues/04-write-core-creation-arch-md-last.md) — [[plan/core-creation/arch.md]] matches what this Project created

Shared segments:
1. [x] Admit Ev at CoreMailbox
2. [x] Apply Ops locally
3. [x] Append Ev to EventLog

Narrowest test seam:
1. [x] FileAgent / DbAgent persist apply of one Ev (Ops on EventBody, then `appendEvent`)

## 2. Module map

Deltas only. Hello / Actor-pool modules do not change.

1. **Ev** — [[src/Shared/History.fs]] (`type Ev` and `module Ev`)
   1. State
      1. [x] Envelope: `id` (EventId), `submissionId`, `authority`, `commandName`, `body` (EventBody)
      2. [ ] No leftover Change record in this file
      3. [x] EventId has private id. EventId.fromJson/toJson bypasses. EventId.next adds one
   2. Interface
      1. [x] `ops` / `apply` / `inverseOps` read EventBody. They take or return Op list. They do not build leftover Change
      2. [ ] No `asChange` / `ofChange`
      3. [x] `Ev.fromJson` / `Ev.toJson`. Only serializing uses fromJson/toJson. Events not from JSON or EventLog have id 0
   3. Uses
      1. [ ] Op
      2. [ ] EventBody
2. **ChangeValidation** — [[src/Shared/History.fs]]
   1. State
      1. [ ] None (functions)
   2. Interface
      1. [ ] `applyChange` / trusted apply take Op list (or Ev whose body is Change/Undo/Redo)
   3. Uses
      1. [ ] Op
      2. [ ] Ev
3. **PersistStamp** — [[src/Shared/History.fs]]
   1. State
      1. [ ] None
   2. Interface
      1. [x] Stamp Ops append onto the last Ev in a batch (EventBody Ops), not a leftover Change list
   3. Uses
      1. [ ] Ev
      2. [ ] Op
4. **CoreChanges** — [[src/Server/Core/CoreChanges.fs]]
   1. State
      1. [x] `CoreChangesAccepted.eventId` is EventId (today `revision: Revision`)
   2. Interface
      1. [x] `postEvents: Ev list -> ...`
      2. [x] `postGraphOnly: Ev -> ...` (today `postGraphOnlyChange: Change`)
      3. [x] No `postChange: Change list`
      4. [x] `getEventId` returns EventId (today `getRevision`)
   3. Uses
      1. [ ] Ev
      2. [ ] EventId
5. **CoreMailbox** — [[src/Server/Core/CoreMailbox.fs]] / [[src/Server/Core/CoreMsg.fs]] / [[src/Server/Core/CoreMailboxBackend.fs]]
   1. State
      1. [ ] EventLog ref (unchanged)
   2. Interface
      1. [x] `PostEvent` of Ev
      2. [x] Graph-only is Ev (`graphOnly`), not leftover Change
      3. [x] No `eventFromChange`
   3. Uses
      1. [ ] Ev
      2. [ ] CoreEventDispatch
6. **CoreEventDispatch** — [[src/Server/Core/CoreEventDispatch.fs]]
   1. State
      1. [ ] None
   2. Interface
      1. [x] `postEvent` admits Ev, applies Ops, persist `appendEvent` Ev. No leftover Change copy for apply
   3. Uses
      1. [ ] Ev
      2. [ ] PersistHandlers (`appendEvent`; apply Ops)
7. **FileAgent** — [[src/Server/Core/FileAgent.fs]]
   1. State
      1. [ ] Graph + EventLog (unchanged roles)
   2. Interface
      1. [x] PersistHandlers apply Ev (Ops), then `appendEvent`
   3. Uses
      1. [ ] Ev
      2. [ ] Op
      3. [ ] EventLogFile
8. **DbAgent** — [[src/Server/Core/DbAgent.fs]]
   1. State
      1. [ ] Graph + EventLog + projection (unchanged roles)
   2. Interface
      1. [x] PersistHandlers apply Ev (Ops), then `appendEvent`. Projection still Files/Query persist, not EventLog
   3. Uses
      1. [ ] Ev
      2. [ ] Op
      3. [ ] Database.appendEvent
9. **State** — [[src/Shared/History.fs]]
   1. State
      1. [x] `eventId: EventId` (today `revision: Revision`)
   2. Interface
      1. [x] One serial type EventId. Field and JSON key `"eventId"`
   3. Uses
      1. [ ] EventId
      2. [ ] Graph
10. **BootCache** — [[src/Shared/BootCache.fs]] / [[src/Client/BootCacheStore.fs]]
    1. State
       1. [ ] Ev list log
    2. Interface
       1. [ ] encode/decode Ev, not leftover Change JSON
    3. Uses
       1. [ ] Ev
       2. [ ] EventJson or Event codec
11. **ClientHistory** — [[src/Shared/ClientHistory.fs]]
    1. State
       1. [x] Ev-shaped Emacs Actions. No `nextEventId`. No client EventId serial
    2. Interface
       1. [x] `record` / `undo` / `redo` take or yield Ev / Ops. No leftover Change `asChange`
       2. [x] `record` does not return a local id. Pending events stay `EventId.zero` until approve fills the server id. Match server responses with `submissionId`. Interject rewinds. `PendingTransition` and `PendingChange.transition` are gone. SyncInfo pending is an event list
    3. Uses
       1. [ ] Ev
       2. [ ] Op
12. **GraphOnlyChangePost** — [[src/Server/GraphOnlyChangePost.fs]]
    1. State
       1. [ ] None
    2. Interface
       1. [x] Chunks mint Ev (EventId.zero, commandName) onto `postGraphOnly`
    3. Uses
       1. [ ] Ev
       2. [ ] Op

## 3. Seams

1. [x] **Op apply** — Interface on **Ev** / **ChangeValidation**. Local Graph mutate from an Op list. Tests cross here for invert/amend.
2. [x] **postEvents** — Interface on **CoreChanges**. HTTP and Browser already cross this seam.
3. [x] **postGraphOnly** — Interface on **CoreChanges**. Parse and lazy-load. Ev in; file persist skipped.
4. [x] **Persist apply** — Interface on **FileAgent** / **DbAgent**. Admit Ev, apply Ops, `appendEvent`. Narrowest test seam for this Project.
5. [x] **EventId** — Interface on **State** / **Ev**. One serial. JSON key `"eventId"`. `getEventId`. EventId has private id. EventId.fromJson/toJson bypasses. EventId.next adds one. Ev.fromJson/toJson. Only serializing uses fromJson/toJson. Only EventLog uses EventId.next. Any event not from these sources has id 0. The client has no EventId serial. Pending events use `EventId.zero`. Match server responses with `submissionId`.

## 4. Alternative considered

1. **Chosen** — Ev is the transport envelope. Ops apply locally. Leftover Change is wrapping to delete. One serial is EventId (`Change.id` becomes EventId with no Revision stop). EventId has private id. Only EventLog uses EventId.next. Only serializing uses fromJson/toJson. Persist apply and command mint migrate in any order after compile preamble. Two Core doors stay, both Ev.
2. **Rejected: leftover Change as command-builder product** — [[plan/core-creation/arch.md]] still locks `Ev.ofChange` / `Ev.asChange`. That is the mess. Destination is EventBody.Change of Ops, not a second record.
3. **Rejected: persist apply takes Ev and never exposes Ops** — apply is already `Op.apply`. Hiding Ops behind Ev-only apply at FileAgent would shallow-copy the envelope through amend and PersistStamp. Ops stay the apply interface.
4. **Rejected: Change.id : Revision first** — a second wrapper beside EventId. Skip to EventId.

## 5. Unsettled

<!-- none -->
