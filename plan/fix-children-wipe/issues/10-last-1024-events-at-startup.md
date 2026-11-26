# 10 — Last 1024 events at startup

**Status**: defined
Blockers: none — [PR 646](https://github.com/abaljeu/ambit/pull/646) already summarizes decode and recover drops to one line per load.

## Scope

On every server start, `DbAgent.loadReconciled` in `src/Server/Core/DbAgent.fs` rebuilds the in-memory `EventLog` so it matches shutdown. Today that means `Database.getEvents` in `src/Server/Database.fs` runs `SELECT event_id, payload FROM events ORDER BY event_id ASC` with no bound, then `decodeRows` runs `EventLogFile.decodeEvent` on every row, `EventLog.restorePersisted` fills the log, `advancePastRowIds` raises `nextId` past the max row id, and `EventLog.recoverOnInvalid` (via `recoverLogging`) replays events ahead of the projection checkpoint from `Database.loadPersistedStateFor`. Production has tens of thousands of rows. Many legacy rows have a top-level `kind: "change"` that `EventJson.decodeBody` rejects; those rows are dropped. Alan does not care about old event bodies.

Alan, 2026-11-26: skip the old part of the log at startup and keep only the last N=1024 events. The in-memory log after `loadReconciled` must hold at most those newest events (fewer when the table is smaller). `nextId` must still sit past the true tip, so new ids do not collide. The `events` table is not truncated.

The graph itself comes from the projection, not from replaying the whole log. `recoverOnInvalid` only applies events with id greater than `GraphAtEvent.eventId`. `DbAgent.reloadRecovered` replays from the already-loaded in-memory log, not a second full read.

### Consumers that see the shortened log

- `DbAgent.eventsSince` and `PersistFilling.getEventsSince` answer from that log. `CoreMailboxLoop.seedEventLog` seeds the mailbox from `getEventLog` after a `getEventsSince EventId.zero` health check, so `Query.EventsSince` and `Api.eventsThroughSnapshot` / `Api.postPoll` only return events still in memory. A client polling from an id older than the kept window does not see the gap, even though those rows remain in the table. `/state` still loads the projection.
- `CoreEventDispatch.completeUndoRedo` already refuses a target at or before `restartTip` (`CoreMailboxLoop.makeMailBox` sets that to `EventLog.tip` at seed) with "target Event is from before the restart". Undo and redo of events that existed at startup are already refused; a shorter startup log does not cut that further. A target whose ops are empty and that is missing from the log fails with "target Event not found". Client-side `SyncLogic.applyLocalUndo` / `applyLocalRedo` in the current session are unchanged.
- `DbAgent.tryPersistedEvent` and `CoreEventDispatch.tryStored` dedupe by `submissionId` only within the in-memory log.

### Open questions

- Put the bound in SQL (`ORDER BY event_id DESC LIMIT 1024`, then restore oldest-first) so Postgres does not ship the old rows, or read every row with `getEvents` and keep the newest 1024 after the read? SQL is the expected answer, because the point is to stop paying for the full table at start.
- Where does the named constant for 1024 live, `Database` or `DbAgent`?
- If `Database.loadPersistedStateFor` writes `LOAD-FALLBACK` and starts from `Graph.create (), EventId.zero`, `recoverOnInvalid` can no longer rebuild the graph from a long log. Refuse that start, or fall back to a full read only in that case?
- If the projection checkpoint is more than 1024 events behind the tip, the unprojected events below the window are not applied. Is that accepted, or must the load also include every event after the checkpoint?
- A poll from an older event id that only receives the kept window: is a partial list enough, because `/state` has the current graph, or should Poll say the cursor is too old?

### Files

- `src/Server/Database.fs` — add a newest-N read (keep `getEvents` for callers that still want the full table; today that is this load path and `tests/Server.Tests/DatabaseProjectionContractTests.fs`), or give `getEvents` a limit.
- `src/Server/Core/DbAgent.fs` — `loadReconciled` uses that read and the named constant. `advancePastRowIds` still runs on the loaded rows so `nextId` stays past the tip.
- `tests/Server.Tests/DbAgentTests.fs` — the tests below, beside the existing decode-drop and recover-invalid cases.

No split is needed. Stop before `FileAgent` and `EventLogFile.readAllEvents`; that file-store path is not this ticket.

## Done

[ ] `DbAgent.loadReconciled` starts with at most the newest 1024 event rows (all of them when the table is smaller). The bound is a named constant set to 1024.
[ ] After that load, `EventLog.nextId` is past the maximum `event_id` in the table, including rows older than the window and rows `decodeRows` dropped.
[ ] `EventLog.recoverOnInvalid` still applies kept events that are ahead of the projection checkpoint.
[ ] The `events` table still holds the older rows.
[ ] A test inserts more than 1024 events, starts `DbAgent`, and asserts `getEventsSince EventId.zero` returns only the newest 1024, in order, and that the next assignable id is past the true tip.
[ ] A test whose projection checkpoint sits inside that window asserts the events after the checkpoint are still applied.
[ ] A test asserts rows older than the window are still in the table after startup (`Database.getEvents` or `Database.getEventsAfter`).

## Stop

[ ] Stop before truncating or deleting older rows from the `events` table. `Database.clearEvents` stays on the existing empty-after-recover path only.
[ ] Stop before changing `EventJson.decodeBody` or adding a decoder for legacy top-level `kind: "change"` bodies.
[ ] Stop before changing `FileAgent` or `EventLogFile.readAllEvents`.
[ ] Stop before changing client undo/redo or `Want.compose`.
[ ] Stop before changing `EventLog.recoverOnInvalid` itself, unless a decided open question requires a fallback-only full read or a refused start.
