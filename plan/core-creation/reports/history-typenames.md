# History / ChangeLog / ClientHistory — key typenames and collections

Date: 2026-09-15. Source inventory for the History / Change / today’s [[src/Server/ChangeLog.fs]] / ClientHistory / undo-redo / mailbox `eventHistory` cluster. Glossary matches from [[CONTEXT.md]]: **History**, **EventLog**, **Change**, **Op**, **Undo**, **Redo**, **Event**, **event id** (code still uses `Revision`). Today’s ChangeLog module is the lagging persist name of EventLog.

## 1. Key types

### 1.1 Op — [[src/Shared/History.fs]]

DU (`RequireQualifiedAccess`). Cases:

1. `NewNode` — `nodeId`, `text`
2. `SetText` — `nodeId`, `oldText`, `newText`
3. `SetClasses` — `nodeId`, `oldClasses`, `newClasses`
4. `Replace` — `parentId`, `oldChildren`, `newChildren`
5. `NewSpecialNode` — `nodeId`, `kind`, `name`
6. `SetName` — `nodeId`, `oldName`, `newName`
7. `SetDocumentState` — `nodeId`, `oldState`, `newState`
8. `SetUpdateTime` — `nodeId`, `oldTime`, `newTime`

Module `Op`: `apply`, `undo`, `involvedNodeIds`.

### 1.2 Change — [[src/Shared/History.fs]]

Record: `id: int`, `changeId: Guid`, `ops: Op list`.

Module `Change`: `addOp`, `inverse`, `invert`, `apply`, `undo`.

### 1.3 ActorLifecycleEvent — [[src/Shared/History.fs]]

DU:

1. `ActorStarted` — `focusId`, `authority`
2. `ActorFinished` — `focusId`

### 1.4 HistoryEvent — [[src/Shared/History.fs]]

DU:

1. `ChangeEvent` of `Change`
2. `ActorEvent` of `id: int * ActorLifecycleEvent`

### 1.5 History — [[src/Shared/History.fs]]

Record (glossary **History**): `past: HistoryEvent list`, `future: HistoryEvent list`, `nextId: int`.

Module `History`: `empty`, `restoreChanges`, `fromChanges`, `newChange`, ownership validators, `applyChange` / `applyChangeTrusted`.

### 1.6 State — [[src/Shared/History.fs]]

Record: `graph: Graph`, `revision: Revision`. No history field.

### 1.7 ApplyResult — [[src/Shared/History.fs]]

DU: `Changed` / `Unchanged` / `Invalid` of `State` (plus message on Invalid).

### 1.8 HistoryRecord — [[src/Shared/ClientHistory.fs]]

Record: `recordId: int`, `commandName: string`, `applied: Change`.

### 1.9 ClientHistory — [[src/Shared/ClientHistory.fs]]

Private record: `past: HistoryRecord list`, `future: HistoryRecord list`, `nextRecordId: int`.

Module `ClientHistory`: `clear`, `record`, `undo`, `redo`, `tryPeekUndoName`, `tryPeekRedoName`. Browser undo/redo stack (Emacs-style fold of future into past on new record).

### 1.10 Revision — [[src/Shared/Model.fs]]

Struct DU: `Revision of int`. On `State` and poll/submit; glossary calls this retired name for **event id**.

### 1.11 PendingChange path — [[src/Shared/ViewModelSync.fs]]

No type named `ChangeRequest` in source. Glossary **ChangeRequest** maps to this path.

1. `PendingKind` — `Normal` | `Undo` | `Redo`
2. `PendingTransition` — `recordId`, `submittedChangeId`, `kind`
3. `PendingChange` — `change: Change`, `transition: PendingTransition option`

### 1.12 ChangeBatch — [[src/Shared/Serialization.fs]]

Record: `changes: Change list`. HTTP POST body for change submit.

### 1.13 ChangeLog — [[src/Server/ChangeLog.fs]]

Module only (today’s persist of **EventLog**; lagging ChangeLog name). Append-only line file helpers: `appendEntry` / `appendEntries`, `buildIndex`, `readEntryAt`, `encodeChange` / `decodeChange`, `tryFindByChangeId`. Line = 8-digit decimal id + compact JSON `Change` + newline.

### 1.14 PersistHandlers / CoreMsg — [[src/Server/Core/CoreMsg.fs]]

1. `PersistHandlers.getChangesSince: Revision -> Result<Change list, string>`
2. `PersistHandlers.postChange: Change list -> Result<CoreChangesAccepted, string>`
3. `CoreMsg.GetChangesSince` / `PostChange` / `GetEventHistory` carry the same shapes through the mailbox.

### 1.15 MailboxContext — [[src/Server/Core/CoreMailboxBackend.fs]]

Record field `eventHistory: History ref`. In-memory mailbox History; restored from ChangeLog Changes plus ActorEvents.

### 1.16 ClientSyncState / VM — holders

1. `ClientSyncState` ([[src/Shared/SyncLogic.fs]]) — `graph`, `revision`, `history: ClientHistory`
2. `VM` ([[src/Shared/ViewModel.fs]]) — same three plus UI fields; `syncInfo.pendingChanges` is separate

## 2. Key collections

### 2.1 Change.ops — `Op list`

Order: apply forward; undo reverses. Writers: client command builders, `Change.addOp`, `PersistStamp.appendToChange`.

### 2.2 ChangeLog file + offset index

1. Durable store: `SYSTEM/gambol.log` via [[src/Server/Bookkeeping.fs]].
2. Index: `int64 ResizeArray` — `index.[i]` = byte offset of entry `i`; `i = 0` oldest.
3. Writers: [[src/Server/Core/FileAgent.fs]] `persistLogEntries` → `ChangeLog.appendEntries` then `offsetIndex.Add`; [[src/Server/Core/DbAgent.fs]] appends encoded Changes to DB (same encode helper, no file index).
4. Readers: `getChangesSince after` returns Changes for indices/rows after `after` (oldest-first among returned).

### 2.3 History.past / History.future — `HistoryEvent list`

1. Order on `past`: oldest-head, newest-tail (`past @ [event]`; `restoreChanges` appends fresh ChangeEvents).
2. `future`: cleared by `restoreChanges`; mailbox Actor recording does not push undo/redo onto it.
3. Writers: mailbox `recordActorStarted` / `recordActorFinished`; `syncEventHistory` via `History.restoreChanges` after successful `postChange` / on `GetEventHistory`.
4. Held at `MailboxContext.eventHistory`.

### 2.4 ClientHistory.past / future — `HistoryRecord list`

1. Order: newest-head on both. Head of `past` = next undo; head of `future` = next redo.
2. Writers: Browser via `ClientHistory.record` / `undo` / `redo` ([[src/Shared/SyncLogic.fs]], [[src/Client/UpdateOps.fs]]).
3. Held on `VM.history` / `ClientSyncState.history`.

### 2.5 SyncInfo.pendingChanges — `PendingChange list`

Client submit queue. Writers: SyncPlanner / SyncLogic / client update. Element: wrap of `Change` plus optional undo/redo transition metadata.

### 2.6 ChangeBatch.changes — `Change list`

Wire payload list for one POST. Writers: client encode ([[src/Client/UpdateCodec.fs]]); server decode ([[src/Server/Api.fs]]).

### 2.7 PersistHandlers.getChangesSince result — `Change list`

Oldest-first Changes after the given Revision. FileAgent walks `offsetIndex`; DbAgent reads DB rows. Fed into mailbox `History.restoreChanges`.

### 2.8 State.revision — not a collection

Monotonic int on apply (`revision + 1` per fresh Change in FileAgent/DbAgent). Checkpointed in meta (`Bookkeeping.writeRevision`) when clean. No Event/History list on State.

## 3. Same facts, different shapes

1. **ChangeLog** (durable Change stream) and **History.past** `ChangeEvent`s — same Change payloads; History also holds ActorEvents and is in-memory; restore dedupes by `changeId`.
2. **ClientHistory.past** `HistoryRecord.applied` — also `Change`, but Browser undo stack (newest-head, command names, local only); not the durable log and not mailbox History.
3. **PendingChange** / **ChangeBatch** — transport/queue wrappers around `Change`, not a third history.
4. **Revision** / `Change.id` / History `nextId` — related counters in code; glossary prefers one **event id** story over a separate Revision counter.
