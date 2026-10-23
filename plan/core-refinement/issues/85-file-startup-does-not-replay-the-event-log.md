# 85 — File startup does not replay the event log

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [88 — Undo does not reach past a restart](88-undo-does-not-reach-past-a-restart.md). It moves the Undo-after-restart test off the meta file.

## 3. Look at
- Files:
  - `src/Server/Core/FileAgent.fs`
  - `src/Shared/EventLog.fs`
  - `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
  - `tests/Server.Tests/FileAgentFailureTests.fs`
  - `tests/Server.Tests/StateEndpointTests.fs`
  - `tests/Server.Tests/LazyLoadReconciliationServerTests.fs`
- Types:
  - `FileAgent.LoadedFile` (fields `persistClean`, `lastPersisted`, `pendingCheckpoint`)
  - `EventLog`, `State`
  - `PersistHandlers`
- Functions:
  - `FileAgent.reconcileLoaded`, `FileAgent.loadOrFail`, `FileAgent.openLoaded`, `FileAgent.startPersist`, `FileAgent.createWithDependencies`
  - `FileAgent.persistCollected`, `FileAgent.takeCheckpoint`, `FileAgent.noteStamped`, `FileAgent.persistHandlers`
  - `EventLog.recover`, `EventLog.tip`, `EventLog.afterCheckpoint`
  - `EventLogFile.truncate`
  - `Bookkeeping.writeEventId`, `DocumentLoader.tryLoadState` (gives `state.eventId` from `Bookkeeping.readEventId`)

## 4. Context
A File-mode server takes no Edit in production: `RouteRegistration.isWritable` makes its browser door read-only, and the parse thread posts graph-only Changes. Tests still post Edits to a `FileAgent` host.

- Startup: `DocumentLoader.tryLoadState` reads the graph from the `.amb` files and the checkpoint id from `SYSTEM/gambol.meta`. `FileAgent.reconcileLoaded` calls `EventLog.recover` with that state and the event log from `SYSTEM/gambol.events`:
  - checkpoint greater than the log tip: keep the state, truncate the log file, and use `EventLog.afterCheckpoint`
  - checkpoint equal to the log tip: keep both
  - checkpoint less than the log tip: apply every logged event after the checkpoint to the graph
- Checkpoint write: `FileAgent.persistCollected` appends each Edit's id to `pendingCheckpoint`. `FileAgent.noteStamped` takes the oldest with `takeCheckpoint` and, when `persistClean` is true, writes it with `Bookkeeping.writeEventId` and copies the graph to `lastPersisted`. Nothing reads `lastPersisted`.
- Three tests wait for the meta file to change to learn that a file write finished.
- `DbAgent.loadReconciled` calls `EventLog.recover` against the database. This ticket does not change it.

## 5. Changes
- `FileAgent.reconcileLoaded` stops calling `EventLog.recover`. It applies no logged event to the graph:
  - checkpoint greater than the log tip: keep the state, truncate the log file, and use `EventLog.afterCheckpoint` (as today)
  - otherwise: keep the graph and the log, and set `state.eventId` to `EventLog.tip` of the log
- `FileAgent.persistHandlers` sets `noteStamped = ignore`.
- Delete from `FileAgent`:
  - `noteStamped` and `takeCheckpoint`
  - the `pendingCheckpoint` append in `persistCollected`
  - the `LoadedFile` fields `persistClean`, `lastPersisted` and `pendingCheckpoint`, with their values in `openLoaded` and the `lastPersisted` write in `startPersist`
- `Bookkeeping.readEventId`, `Bookkeeping.writeEventId` and `DocumentLoader` do not change. Startup still reads the meta file for the truncate rule above.
- `DbAgent` keeps `EventLog.recover`.

## 6. Tests
- Change `File recover applies Ops Ev ahead of Graph checkpoint` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` and rename it `File startup does not replay the event log`. After restart the graph has no child from the logged Change, and `getEventId` is id 3.
- Change `soft-fail log is replayed into FileAgent state after restart` in `tests/Server.Tests/FileAgentFailureTests.fs` and rename it `soft-fail Edit is not in the graph after restart`. After restart no node has the text `soft-fail-probe`, and `state.eventId` is id 2. Delete its `Bookkeeping.readEventId` assertion.
- Change `Snapshot writes amb artifacts asynchronously after change` in `tests/Server.Tests/StateEndpointTests.fs`. Delete its meta file assertions.
- Change `server reconciler applies planner ops through active agent` in `tests/Server.Tests/LazyLoadReconciliationServerTests.fs`. It waits for the workspace `.amb` file only. Delete its meta file assertions.
- Filters:
  - `scripts/agent-test.sh --filter "MailboxHistoryDurabilityTests"`
  - `scripts/agent-test.sh --filter "FileAgentFailureTests"`
  - `scripts/agent-test.sh --filter "Snapshot writes amb artifacts asynchronously after change"`
  - `scripts/agent-test.sh --filter "server reconciler applies planner ops through active agent"`

## 7. Done
- The named tests pass.
- `FileAgent.fs` does not name `EventLog.recover`, `pendingCheckpoint`, `takeCheckpoint`, `persistClean`, `lastPersisted` or `Bookkeeping.writeEventId`.
- The checkpoint tests in `MailboxHistoryDurabilityTests.fs` that set the meta file with `Bookkeeping.writeEventId` pass unchanged.
- Upload, import, parse, Load, undo and persist complete.
