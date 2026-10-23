# 85 — File startup does not replay the event log

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [82 — The mailbox pushes an Edit's special nodes](82-the-mailbox-pushes-an-edits-special-nodes.md)

## 3. Look at
- Files:
  - `src/Server/Core/FileAgent.fs`
  - `src/Shared/EventLog.fs`
  - `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
  - `tests/Server.Tests/FileAgentFailureTests.fs`
- Types:
  - `FileAgent.LoadedFile`
  - `EventLog`, `State`
- Functions:
  - `FileAgent.reconcileLoaded`, `FileAgent.loadOrFail`, `FileAgent.openLoaded`, `FileAgent.createWithDependencies`
  - `EventLog.recover`, `EventLog.tip`, `EventLog.afterCheckpoint`
  - `EventLogFile.truncate`
  - `DocumentLoader.tryLoadState` (gives `state.eventId` from `Bookkeeping.readEventId`)

## 4. Context
`FileAgent` startup reads the graph from the `.amb` files with `DocumentLoader.tryLoadState`. That state's `eventId` is the checkpoint in `SYSTEM/gambol.meta`. `FileAgent.reconcileLoaded` then calls `EventLog.recover` with that state and the event log read from `SYSTEM/gambol.events`:

- checkpoint greater than the log tip: keep the state, truncate the log file, and use `EventLog.afterCheckpoint`
- checkpoint equal to the log tip: keep both
- checkpoint less than the log tip: apply every logged event after the checkpoint to the graph

`DbAgent.loadReconciled` calls `EventLog.recover` against the database. This ticket does not change it.

## 5. Changes
- `FileAgent.reconcileLoaded` stops calling `EventLog.recover`. It applies no logged event to the graph:
  - checkpoint greater than the log tip: keep the state, truncate the log file, and use `EventLog.afterCheckpoint` (as today)
  - otherwise: keep the graph and the log, and set `state.eventId` to `EventLog.tip` of the log
- An Edit in the log whose file write did not finish before the server stopped is not in the graph after restart. It stays in the log.
- `DbAgent` keeps `EventLog.recover`.

## 6. Tests
- Change `File recover applies Ops Ev ahead of Graph checkpoint` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` and rename it `File startup does not replay the event log`. After restart the graph has no child from the logged Change, and `getEventId` is id 3.
- Change `soft-fail log is replayed into FileAgent state after restart` in `tests/Server.Tests/FileAgentFailureTests.fs` and rename it `soft-fail Edit is not in the graph after restart`. After restart no node has the text `soft-fail-probe`, and `state.eventId` is id 2. Delete its `Bookkeeping.readEventId` assertion.
- Filters:
  - `scripts/agent-test.sh --filter "MailboxHistoryDurabilityTests"`
  - `scripts/agent-test.sh --filter "soft-fail Edit is not in the graph after restart"`

## 7. Done
- The named tests pass.
- `FileAgent.fs` does not name `EventLog.recover`.
- Upload, import, parse, Load, undo and persist complete.
