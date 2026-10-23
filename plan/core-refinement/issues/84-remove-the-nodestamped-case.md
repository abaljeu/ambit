# 84 — Remove the NodeStamped case

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [83 — NodePersisted takes over NodeStamped's work](83-nodepersisted-takes-over-nodestamped-work.md)

## 3. Look at
- Files:
  - `src/Shared/History.fs` (where `Notice` is defined)
  - `src/Shared/EventJson.fs`
  - `src/Server/Database.fs`
  - `src/Server/Core/CoreMsg.fs` (where `PersistHandlers` is defined)
  - `src/Server/Core/CoreMailboxLoad.fs`
  - `src/Server/Core/FileAgent.fs`
  - `src/Server/Core/DbAgent.fs`
  - `src/Server/EventLogFile.fs`
  - `tests/Server.Tests/CoreLoopNoticeTests.fs`
  - `tests/Shared.Tests/EventJsonTests.fs`
  - `tests/Server.Tests/StateEndpointTests.fs`
  - `tests/Server.Tests/TestBackend.fs`
  - `tests/Server.Tests/PersistThreadTests.fs`
- Types:
  - `Notice`
  - `PersistHandlers`
- Functions:
  - `EventJson.encodeNodeStamped`, `EventJson.encodeNotice`, `EventJson.decodeNodeStamped`, `EventJson.decodeNoticeBody`
  - `Database.writeNoticeRow`
  - `CoreMailboxLoad.applyNotice`, `CoreMailboxLoad.failedPersist`
  - `FileAgent.persistHandlers`, `FileAgent.noteStamped`
  - `DbAgent.persistHandlers`, `DbAgent.noteStamped`
  - `EventLogFile.readAllEvents`, `DbAgent.loadReconciled`

## 4. Context
After [83 — NodePersisted takes over NodeStamped's work](83-nodepersisted-takes-over-nodestamped-work.md), nothing in `src/` posts `Notice.NodeStamped`. Its `applyNotice` arm logs it and keeps the graph. `PersistHandlers.noteStamped` runs from the `NodePersisted` arm. An event log written earlier can hold `nodeStamped` rows.

## 5. Changes
- Delete `Notice.NodeStamped` from `src/Shared/History.fs`. Each match on `Notice` that names it changes:
  - `EventJson.encodeNotice` and `encodeNodeStamped`
  - `EventJson.decodeNoticeBody` (the `"nodeStamped"` tag) and `decodeNodeStamped`
  - `Database.writeNoticeRow`
  - `CoreMailboxLoad.applyNotice`
- An old `nodeStamped` row fails to decode. `EventLogFile.readAllEvents` and `DbAgent.loadReconciled` skip a row that fails to decode.
- Rename `PersistHandlers.noteStamped` to `notePersisted`. Each record that sets it changes:
  - `CoreMailboxLoad.failedPersist` and the `NodePersisted` arm of `applyNotice`
  - `FileAgent.persistHandlers` (`FileAgent.noteStamped` becomes `notePersisted`)
  - `DbAgent.persistHandlers` (`DbAgent.noteStamped` becomes `notePersisted`)
  - `tests/Server.Tests/TestBackend.fs` `cannedHandlers` and its other handler records
  - `tests/Server.Tests/CoreLoopNoticeTests.fs` `handlers`
  - `tests/Server.Tests/PersistThreadTests.fs` handler record
- Update the comment above `applyNotice` that names NodeStamped.

## 6. Tests
- Delete `Posted NodeStamped is on the log` in `tests/Server.Tests/CoreLoopNoticeTests.fs`.
- Change `Notice JSON round trip` in `tests/Shared.Tests/EventJsonTests.fs`. Its list drops `Notice.NodeStamped`.
- Add `An old nodeStamped row is skipped on read` in `tests/Shared.Tests/EventJsonTests.fs`. Decoding `{"kind":"notice","notice":"nodeStamped"}` returns `Error`.
- The helper `countPersistProgress` in `tests/Server.Tests/StateEndpointTests.fs` stops counting `Notice.NodeStamped`.
- Filters:
  - `scripts/agent-test.sh --filter "Notice JSON round trip"`
  - `scripts/agent-test.sh --filter "An old nodeStamped row is skipped on read"`
  - `scripts/agent-test.sh --filter "CoreLoopNoticeTests"`
  - `scripts/agent-test.sh --filter "StateEndpointTests"`

## 7. Done
- The named tests pass.
- No file under `src/` or `tests/` names `NodeStamped` or `noteStamped`, apart from the `nodeStamped` JSON in the skip test.
- Upload, import, parse, Load, undo and persist complete.
