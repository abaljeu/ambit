# 86 — FileAgent stops writing the event-id checkpoint

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [85 — File startup does not replay the event log](85-file-startup-does-not-replay-the-event-log.md)

## 3. Look at
- Files:
  - `src/Server/Core/FileAgent.fs`
  - `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
  - `tests/Server.Tests/StateEndpointTests.fs`
  - `tests/Server.Tests/LazyLoadReconciliationServerTests.fs`
- Types:
  - `FileAgent.LoadedFile` (fields `persistClean`, `lastPersisted`, `pendingCheckpoint`)
  - `PersistHandlers`
- Functions:
  - `FileAgent.persistCollected`, `FileAgent.takeCheckpoint`, `FileAgent.noteStamped`, `FileAgent.persistHandlers`, `FileAgent.openLoaded`, `FileAgent.startPersist`
  - `Bookkeeping.writeEventId`, `Bookkeeping.metaPath`

## 4. Context
After [85 — File startup does not replay the event log](85-file-startup-does-not-replay-the-event-log.md), startup applies no logged event, so nothing needs the checkpoint to choose a replay start.

- `FileAgent.persistCollected` appends each Edit's event id to `pendingCheckpoint`.
- `FileAgent.noteStamped` runs on `Notice.NodeStamped`. It takes the oldest id with `takeCheckpoint`. When `persistClean` is true it writes that id with `Bookkeeping.writeEventId` and copies the graph to `lastPersisted`. A failed write sets `persistClean` false.
- Nothing reads `lastPersisted`.
- Three tests wait for `SYSTEM/gambol.meta` to change to learn that a file write finished.

## 5. Changes
- `FileAgent.persistHandlers` sets `noteStamped = ignore`.
- Delete from `FileAgent`:
  - `noteStamped` and `takeCheckpoint`
  - the `pendingCheckpoint` append in `persistCollected`
  - the `LoadedFile` fields `persistClean`, `lastPersisted` and `pendingCheckpoint`, with their values in `openLoaded` and the `lastPersisted` write in `startPersist`
- `src/` has no caller of `Bookkeeping.writeEventId` after this ticket. [87 — Remove the event-id checkpoint](87-remove-the-event-id-checkpoint.md) deletes it.

## 6. Tests
- Change `Undo after File restart fills Change and rejects ActorStart` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`. It waits until the root `.amb` file holds the hello child's text, not for the meta file. Delete the line that writes `"0"` to the meta file.
- Change `Snapshot writes amb artifacts asynchronously after change` in `tests/Server.Tests/StateEndpointTests.fs`. Delete its meta file assertions.
- Change `server reconciler applies planner ops through active agent` in `tests/Server.Tests/LazyLoadReconciliationServerTests.fs`. It waits for the workspace `.amb` file only. Delete its meta file assertions.
- Filters:
  - `scripts/agent-test.sh --filter "Undo after File restart fills Change and rejects ActorStart"`
  - `scripts/agent-test.sh --filter "Snapshot writes amb artifacts asynchronously after change"`
  - `scripts/agent-test.sh --filter "server reconciler applies planner ops through active agent"`
  - `scripts/agent-test.sh --filter "FileAgentFailureTests"`

## 7. Done
- The named tests pass.
- No file under `src/` names `pendingCheckpoint`, `takeCheckpoint`, `persistClean`, `lastPersisted` or `Bookkeeping.writeEventId`.
- Upload, import, parse, Load, undo and persist complete.
