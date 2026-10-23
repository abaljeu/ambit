# 87 — Remove the event-id checkpoint

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [86 — FileAgent stops writing the event-id checkpoint](86-fileagent-stops-writing-the-event-id-checkpoint.md)

## 3. Look at
- Files:
  - `src/Server/Bookkeeping.fs`
  - `src/Server/DocumentLoader.fs`
  - `src/Server/Core/FileAgent.fs`
  - `src/Server/DatabaseSetup.fs`
  - `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
  - `tests/Server.Tests/StateEndpointTests.fs`
  - `tests/Server.Tests/DocumentLoaderTests.fs`
  - `tests/Server.Tests/DbAgentTests.fs`
- Types:
  - `State`, `EventLog`
  - `DatabaseSetup.DbStatus`
- Functions:
  - `Bookkeeping.readEventId`, `Bookkeeping.writeEventId`, `Bookkeeping.metaPath`
  - `DocumentLoader.stateFromGraph`, `DocumentLoader.tryLoadState`, `DocumentLoader.loadState`
  - `FileAgent.reconcileLoaded`
  - `DatabaseSetup.validateAmbNetworkAgainstDb`, `DatabaseSetup.bootstrapFromFileIfEmpty`, `DatabaseSetup.documentStatesMatch`
  - `Database.rebuildFromDocumentFiles`
  - `EventLog.tip`

## 4. Context
After [86 — FileAgent stops writing the event-id checkpoint](86-fileagent-stops-writing-the-event-id-checkpoint.md), nothing in `src/` writes `SYSTEM/gambol.meta`. It is still read:

- `DocumentLoader.stateFromGraph` sets `state.eventId` from `Bookkeeping.readEventId`.
- `FileAgent.reconcileLoaded` truncates the log when that id is greater than the log tip.
- `DatabaseSetup` runs in File mode with a database. `validateAmbNetworkAgainstDb` compares the file state with the database state by outline and by `eventId`, and rebuilds the database when they differ. `bootstrapFromFileIfEmpty` loads the file state into an empty database. `Database.rebuildFromDocumentFiles` writes the file state's `eventId`.
- Callers of `DocumentLoader`: `FileAgent.loadOrFail`, `DatabaseSetup.bootstrapFromFileIfEmpty`, `DatabaseSetup.validateAmbNetworkAgainstDb`, and tests in `DocumentLoaderTests.fs`, `DbAgentTests.fs` and `ChangeEndpointResilienceTests.fs`.

## 5. Changes
- Delete `Bookkeeping.readEventId`, `Bookkeeping.writeEventId` and the private `ensureSystemDir`. Keep `Bookkeeping.systemDir` and `Bookkeeping.metaPath`; tests use `metaPath` as a SYSTEM file that a save must not change.
- `DocumentLoader.stateFromGraph` sets `eventId = EventId.zero`. The graph comes from the files only.
- `FileAgent.reconcileLoaded` sets `state.eventId` to `EventLog.tip` of the log and keeps the log. Delete its truncate branch.
- `DatabaseSetup.validateAmbNetworkAgainstDb` compares by outline only: it gives the file state the database state's `eventId` before `documentStatesMatch` and before `Database.rebuildFromDocumentFiles`.
- `DbAgent` keeps `EventLog.recover`.

## 6. Tests
- Delete `File empty EventLog recover exposes getEventId Graph checkpoint` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`.
- Delete `File Graph greater than Log drops persist EventLog` in the same file.
- Change `File empty Graph checkpoint live append continues past Graph` in the same file and rename it `File live append continues past the log tip`. It appends ids 1 to 5 with `appendOnly`, restarts, starts an Actor, and the new head and `getEventId` are id 6.
- Change `File equal Log and Graph keeps EventLog` in the same file and rename it `File startup keeps the event log and takes its tip`. It does not call `Bookkeeping.writeEventId`. The history has 2 events and `getEventId` is id 2.
- The helper `writeDocumentFiles` in `tests/Server.Tests/StateEndpointTests.fs` stops calling `Bookkeeping.writeEventId`.
- `tryLoadState empty dataDir returns empty graph` and `tryLoadState reads amb network` in `tests/Server.Tests/DocumentLoaderTests.fs` and `rebuildFromDocumentFiles aligns DB with on-disk document` in `tests/Server.Tests/DbAgentTests.fs` pass unchanged.
- Filters:
  - `scripts/agent-test.sh --filter "MailboxHistoryDurabilityTests"`
  - `scripts/agent-test.sh --filter "DocumentLoaderTests"`
  - `scripts/agent-test.sh --filter "DatabaseSetupTests"`
  - `scripts/agent-test.sh --filter "StateEndpointTests"`

## 7. Done
- The named tests pass.
- No file under `src/` or `tests/` names `readEventId` or `writeEventId`.
- `FileAgent.fs` does not name `afterCheckpoint` or `EventLogFile.truncate`.
- Upload, import, parse, Load, undo and persist complete.
