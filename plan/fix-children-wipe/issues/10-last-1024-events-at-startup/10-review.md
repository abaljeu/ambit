Check: fail: a cursor older than the window floor still receives the kept window from Poll and Load, and a table of exactly 1024 rows takes a window floor.

# 10 — Last 1024 events at startup

Ticket: [[../10-last-1024-events-at-startup.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

Commit `3852b35e` on `staging` is the squash of [Load the newest 1024 events at startup](https://github.com/abaljeu/ambit/pull/648), against parent `8940fc66`.

`DbAgent.loadReconciled` reads the newest 1024 event rows. Poll and Load return that window for a cursor older than the floor.

## 1. Standards

- **Scan.** `python3 scripts/standards-scan.py --diff 3852b35e^` exited 0. HEAD is `3852b35e`, so that three-dot range is the squash. The measure lines are `startupEventWindow` at 2 lines, `floorOfRows` at 6, `olderThanWindow` at 5, `loadReconciled` at 34, and `getNewestEvents` at 21. Each is under the 40-line limit. No `LONG`, `TAB`, `MUTABLE`, `PHRASE`, `BARE_ID`, `BLANK_BLANK`, or `FILE` line. `src/Server/Core/DbAgent.fs` is 800 lines, the file cap.
- **Hard.** [Core API](../../../../.agents/rules/core-api.md) says since of `EventId.zero` is every stored Int. `DbAgent.eventsSince` returns the kept window. The ticket requires that window for `CoreMailboxLoop.seedEventLog`. The rule text still says every stored Int.
- **Judgement.** Duplicated Code. `loadReconciled` and `reloadRecovered` each branch on `olderThanWindow` before `recoverLogging`. Source: [Smell baseline](../../../context/SMELLS.md).

```fsharp
let recovered =
    if olderThanWindow floor state then
        state, log
    else
        recoverLogging dataDir state log
```

```fsharp
let recovered =
    if olderThanWindow loaded.windowFloor.Value baseState then
        baseState, loaded.eventLog.Value
    else
        recoverLogging
            loaded.liveSaveDataDir
            baseState
            loaded.eventLog.Value
```

- **Rules.** The source diff matches [.agents/rules/fsharp-source.md](../../../../.agents/rules/fsharp-source.md) on line length, function length, and the short SQL in `getNewestEvents` (same shape as `getEvents`). `LoadedPersist.windowFloor` is a `ref`, the same cell as `startupError`. The diff matches [.agents/rules/writing-markdown.md](../../../../.agents/rules/writing-markdown.md) and [.agents/rules/refer-by-name.md](../../../../.agents/rules/refer-by-name.md).

## 2. Spec

- **Window read.** `Database.getNewestEvents` runs `ORDER BY event_id DESC LIMIT @limit` and sorts the rows ascending. `DbAgent.startupEventWindow` is the literal 1024. `loadReconciled` passes that count and restores the rows with `EventLog.restorePersisted`. `Database.getEvents` is unchanged.
- **Next id.** `advancePastRowIds` runs on the returned rows, including rows `decodeRows` drops. The maximum id in a newest-1024 read is the table tip, so `EventLog.nextId` sits past older rows.
- **In-window recover.** A checkpoint at or after the floor still calls `recoverLogging`. ``startup recover applies a kept event after a checkpoint inside the window`` uses checkpoint 1500, inside 1000..2023, and expects the tip `NewNode` text and event id 2023.
- **Older checkpoint.** When the floor is set and the checkpoint is below it, `loadReconciled` and `reloadRecovered` leave the loaded graph and the kept log. ``startup leaves a projection older than the window as loaded`` uses checkpoint 5. ``startup leaves a LOAD-FALLBACK graph and still logs the fallback`` expects `EventId.zero`, an empty graph, the `LOAD-FALLBACK` line, and 1024 events. Both fixtures insert more than 1024 rows.
- **Partial.** `floorOfRows` sets a floor whenever the read returns 1024 rows. A table of exactly 1024 rows is a full log: every row is in the result. A checkpoint below the oldest id, including `EventId.zero` from `LOAD-FALLBACK`, then skips `recoverOnInvalid`. The recover rule runs that replay when the table fit in 1024.
- **Table.** The `clearEvents` guard still requires the recovered log to be empty. A floor skip returns the kept log, so the older rows stay. ``startup load leaves event rows older than the window in the table`` reads them back with `Database.getEvents`.
- **Zero seed.** `getEventsSince EventId.zero` returns `Ok` and the kept window. ``startup load keeps the newest 1024 events newest-first`` expects 1024 ids from tip 2023 down to floor 1000, and `nextId` 2024. That order is the newest-head list `EventLog.restore` stores.
- **Wrong.** A cursor older than the floor receives the kept events. `Api.eventsThroughSnapshot` returns `DbAgent.eventsSince` for that cursor. `SyncLogic.foldProjectedEvents` applies every event whose id is above the client id, and the client event id jumps across the gap. `SyncLogic.getPollOutcome` yields `DataOutdated` when the server event id is ahead, and `Update.applyPollDataOutdated` applies a non-empty event list and sets Idle. An empty event list on that path also sets Idle. The ticket requires Stale for that cursor, surfaced as `DataOutdated` (`amb-stale`, "Data changed on server — click to reload"). The PR body calls this a later build. The ticket says no split.
- **In-window catch-up.** `EventLog.since` returns kept events whose id is above a cursor inside the window. That cursor can catch up through the same Poll and Load path.
- **Undo.** The diff leaves `CoreEventDispatch.completeUndoRedo` as it stands. `CoreMailboxLoop.makeMailBox` still sets `restartTip` from `EventLog.tip`. The tip of the newest 1024 rows is the table tip.
- **Stops.** The diff leaves `FileAgent`, `EventLogFile.readAllEvents`, `EventJson.decodeBody`, and `EventLog.recoverOnInvalid` as they stand.

## 3. Arch

- No findings. This project has no `arch.md`. The ticket Files list is scored in Spec.

## 4. Tests

- **Suite command.** `scripts/check-test.sh 2957` printed `Permission denied`. `bash scripts/check-test.sh 2957` printed `DONE` and `/tmp/ambit-full-suite-20261126-211738.log`.
- **Server.** `scripts/agent-test.sh --full` passed 797, failed 0, skipped 1. Total 798. The skip is ``Change.upload structure persistence preserves existing file mtime``.
- **Shared.** The same run passed 1948, failed 0, skipped 4. Total 1952. The skips are ``listForPush without git still returns walk``, ``recover silently drops a stale Replace and freezes the parent's children empty``, ``reuses existing owned path without duplicating``, and ``planServerFilePresentOps transitions only matching absent files``.
- **Newest 1024.** ``startup load keeps the newest 1024 events newest-first`` inserts ids 1, 2, and 3 plus 1000..2023. `getEventsSince EventId.zero` returns the 1024 kept ids, newest-head. `eventHistory.nextId` is 2024. The graph event id stays `EventId.zero`.
- **Older rows.** ``startup load leaves event rows older than the window in the table`` expects `Database.getEvents` to return ids 1, 2, 3 and 1000..2023.
- **Checkpoint inside the window.** ``startup recover applies a kept event after a checkpoint inside the window`` saves projection revision 1500 and a tip `NewNode`. The graph contains that text. The event id is 2023.
- **Checkpoint below the floor.** ``startup leaves a projection older than the window as loaded`` saves revision 5. The tip node is absent. The event id stays 5. The kept window and `nextId` 2024 are still there.
- **LOAD-FALLBACK.** ``startup leaves a LOAD-FALLBACK graph and still logs the fallback`` writes an unloadable graph row, then the same window. The state is an empty graph at `EventId.zero`. The error log contains `LOAD-FALLBACK`. `getEventsSince EventId.zero` returns 1024 events.
- **Missing.** The Done line for Poll and Load has no test. `tests/Server.Tests/StateEndpointTests.fs` and `tests/Shared.Tests/SyncLogicTests.fs` are unchanged. The required cases are a cursor older than the floor, and a cursor inside the window.
- **Boundary.** No fact uses a table of exactly 1024 rows. That is the Partial floor case in Spec.

## 5. Miss

- **Wrong.** Poll and Load return the kept window for a cursor older than the floor, and the client applies that list across the gap.
- **Missing.** A Stale signal on that response, wired to `DataOutdated` in `SyncLogic.getPollOutcome` and the Poll and Load Done path, plus the Poll or Load tests for a cursor older than the floor and a cursor inside the window.
- **Partial.** A table of exactly 1024 rows sets a window floor, so a checkpoint below the oldest id skips `recoverOnInvalid` on a full log.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- No findings.
