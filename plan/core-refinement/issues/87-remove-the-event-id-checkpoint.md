# 87 — Remove the event-id checkpoint

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [85 — File startup does not replay the event log](85-file-startup-does-not-replay-the-event-log.md)
- [89 — Delete the File-mode database check](89-delete-the-file-mode-database-check.md)

## 3. Look at
- Files:
  - `src/Server/Bookkeeping.fs`
  - `src/Server/DocumentLoader.fs`
  - `src/Server/Core/FileAgent.fs`
  - `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
  - `tests/Server.Tests/StateEndpointTests.fs`
  - `tests/Server.Tests/DocumentLoaderTests.fs`
- Types:
  - `State`, `EventLog`
- Functions:
  - `Bookkeeping.readEventId`, `Bookkeeping.writeEventId`
  - `DocumentLoader.stateFromGraph`, `DocumentLoader.tryLoadState`, `DocumentLoader.loadState`
  - `FileAgent.reconcileLoaded`, `FileAgent.loadOrFail`
  - `EventLog.tip`, `EventLog.afterCheckpoint`
  - `EventLogFile.truncate`

## 4. Context
After [85 — File startup does not replay the event log](85-file-startup-does-not-replay-the-event-log.md), nothing in `src/` writes `SYSTEM/gambol.meta`. After [89 — Delete the File-mode database check](89-delete-the-file-mode-database-check.md), `DatabaseSetup` does not call `DocumentLoader`. The meta file is still read:

- `DocumentLoader.stateFromGraph` sets `state.eventId` from `Bookkeeping.readEventId`.
- `FileAgent.reconcileLoaded` truncates the log when that id is greater than the log tip.
- Callers of `DocumentLoader`: `FileAgent.loadOrFail`, and tests in `DocumentLoaderTests.fs` and `ChangeEndpointResilienceTests.fs`.
- Tests call `Bookkeeping.writeEventId` to set a checkpoint: four in `MailboxHistoryDurabilityTests.fs` and the helper `writeDocumentFiles` in `StateEndpointTests.fs`.

## 5. Changes
- Delete `Bookkeeping.readEventId`, `Bookkeeping.writeEventId` and the private `ensureSystemDir`.
- `DocumentLoader.stateFromGraph` sets `eventId = EventId.zero`. The graph comes from the files only. Its `dataDir` parameter goes.
- `FileAgent.reconcileLoaded` sets `state.eventId` to `EventLog.tip` of the log and keeps the log. Delete its truncate branch. Ids are not reused, because the log's next id is past its tip.
- `DbAgent` keeps `EventLog.recover`.

## 6. Tests
- Delete `File empty EventLog recover exposes getEventId Graph checkpoint` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`.
- Delete `File Graph greater than Log drops persist EventLog` in the same file.
- Change `File empty Graph checkpoint live append continues past Graph` in the same file and rename it `File live append continues past the log tip`. It appends ids 1 to 5 with `appendOnly`, restarts, starts an Actor, and the new head and `getEventId` are id 6.
- Change `File equal Log and Graph keeps EventLog` in the same file and rename it `File startup keeps the event log and takes its tip`. It does not call `Bookkeeping.writeEventId`. The history has 2 events and `getEventId` is id 2.
- The helper `writeDocumentFiles` in `tests/Server.Tests/StateEndpointTests.fs` stops calling `Bookkeeping.writeEventId`.
- `tryLoadState empty dataDir returns empty graph` and `tryLoadState reads amb network` in `tests/Server.Tests/DocumentLoaderTests.fs` pass unchanged.
- Filters:
  - `scripts/agent-test.sh --filter "MailboxHistoryDurabilityTests"`
  - `scripts/agent-test.sh --filter "DocumentLoaderTests"`
  - `scripts/agent-test.sh --filter "StateEndpointTests"`

## 7. Done
- The named tests pass.
- No file under `src/` or `tests/` names `readEventId` or `writeEventId`.
- `FileAgent.fs` does not name `afterCheckpoint` or `EventLogFile.truncate`.
- Upload, import, parse, Load, undo and persist complete.
