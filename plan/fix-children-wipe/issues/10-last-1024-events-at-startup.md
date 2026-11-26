# 10 — Last 1024 events at startup

**Status**: defined
Blockers: none — [PR 646](https://github.com/abaljeu/ambit/pull/646) already summarizes decode and recover drops to one line per load.

## Scope

On every server start, `DbAgent.loadReconciled` in `src/Server/Core/DbAgent.fs` rebuilds the in-memory `EventLog` so it matches shutdown. Today that means `Database.getEvents` in `src/Server/Database.fs` runs `SELECT event_id, payload FROM events ORDER BY event_id ASC` with no bound, then `decodeRows` runs `EventLogFile.decodeEvent` on every row, `EventLog.restorePersisted` fills the log, `advancePastRowIds` raises `nextId` past the max row id, and `EventLog.recoverOnInvalid` (via `recoverLogging`) replays events ahead of the projection checkpoint from `Database.loadPersistedStateFor`. Production has tens of thousands of rows. Many legacy rows have a top-level `kind: "change"` that `EventJson.decodeBody` rejects; those rows are dropped. Alan does not care about old event bodies.

Alan, 2026-11-26: skip the old part of the log at startup and keep only the last N=1024 events. The in-memory log after `loadReconciled` must hold at most those newest events (fewer when the table is smaller). `nextId` must still sit past the true tip, so new ids do not collide. The `events` table is not truncated.

The bound goes in SQL: a newest-N read, `ORDER BY event_id DESC LIMIT 1024`, then restore oldest-first. The named constant for 1024 lives in `DbAgent`, which is the load policy; `Database` takes the count as a parameter. Keep `getEvents` for callers that still want the full table (today `tests/Server.Tests/DatabaseProjectionContractTests.fs`).

The kept window has two uses. Undo of a pre-startup target is already refused (`CoreEventDispatch.completeUndoRedo` / `restartTip`). After start, the window is the in-memory history that later Undo can search, and slightly old clients (last event id inside the kept window) use those events to catch up to the server state through `Api.postPoll` and `Api.postLoad`. A client whose last event id is older than the window is Stale: it must be reported Stale and reload, not given a partial list that skips the gap. `/state` still loads the projection.

There is no full history, so startup cannot rebuild a graph from events alone. If `Database.loadPersistedStateFor` writes `LOAD-FALLBACK` and returns `Graph.create (), EventId.zero`, `loadReconciled` must not run `recoverOnInvalid` on the window (that would apply only the last 1024 events onto an empty graph). Leave the fallback empty graph and `EventId.zero` as they are today, still write `LOAD-FALLBACK`, still install the kept window in the EventLog, and still raise `nextId` past the tip. The same rule applies when the projection checkpoint is older than the window floor: do not apply the in-window suffix onto that older graph; leave the projection as loaded. `recoverOnInvalid` runs only when every event after the checkpoint is in the kept window (the checkpoint is at or after the floor, or the table fit in N and was not truncated). `DbAgent.reloadRecovered` stays on the already-loaded in-memory log.

The window floor is the minimum `event_id` of the rows the SQL read returned when the read hit the limit. When fewer than N rows came back, there is no floor: the in-memory log is the full table, and no client is Stale for being "older than the window." `CoreMailboxLoop.seedEventLog` must still see a successful `getEventsSince EventId.zero` (the kept window), not a Stale error.

### Consumers

- Catch-up: `DbAgent.eventsSince`, `PersistFilling.getEventsSince`, `Query.EventsSince`, and `Api.eventsThroughSnapshot` (used by `Api.postPoll` and `Api.postLoad`) return kept events to a client whose cursor is inside the window, so that client can catch up. A cursor older than the floor is Stale. Reuse the existing reload-required path: `SyncState.DataOutdated` in `src/Shared/ViewModelSync.fs`, `SyncLogic.getPollOutcome`, and `StatusView.renderSyncStatus` (`amb-stale`, "Data changed on server — click to reload"). Do not invent a second overlay.
- Undo: `CoreEventDispatch.completeUndoRedo` already refuses a target at or before `restartTip` (`CoreMailboxLoop.makeMailBox` sets that to `EventLog.tip` at seed) with "target Event is from before the restart". A shorter startup log does not cut that further. A target whose ops are empty and that is missing from the log fails with "target Event not found". Client-side `SyncLogic.applyLocalUndo` / `applyLocalRedo` in the current session are unchanged.
- `DbAgent.tryPersistedEvent` and `CoreEventDispatch.tryStored` dedupe by `submissionId` only within the in-memory log.

### Files

- `src/Server/Database.fs` — newest-N read, count passed in, `ORDER BY event_id DESC LIMIT`. Keep `getEvents` unchanged.
- `src/Server/Core/DbAgent.fs` — the named constant, `loadReconciled` uses that read, records the window floor, skips recover when the starting graph is older than the floor, and still runs `advancePastRowIds` on the loaded rows.
- `tests/Server.Tests/DbAgentTests.fs` — the load, floor, and recover tests below, beside the existing decode-drop and recover-invalid cases.

Stale handling needs a few more files; keep them minimal. No split.

- `src/Server/Api.fs` — `eventsThroughSnapshot` reports Stale instead of a partial event list when `request.eventId` is older than the floor. A small pass-through on `CoreDoor` / the persist filling is allowed if Api cannot see the floor otherwise.
- The poll/load response needs a Stale signal if today's body cannot say it: `src/Shared/ApiResponses.fs` and `src/Shared/ApiResponseSerialization.fs`.
- Wire that signal to `DataOutdated` in `src/Shared/SyncLogic.fs` `getPollOutcome` (and the existing client Poll/Load Done path). Do not add a new `SyncState` if `DataOutdated` already means reload required.
- Tests for the Stale poll and for catch-up inside the window go next to the existing poll coverage in `tests/Server.Tests/StateEndpointTests.fs` and, if the outcome helper changes, `tests/Shared.Tests/SyncLogicTests.fs`.

Stop before `FileAgent` and `EventLogFile.readAllEvents`; that file-store path is not this ticket.

## Done

[ ] `DbAgent.loadReconciled` starts with at most the newest 1024 event rows (all of them when the table is smaller), via SQL `ORDER BY event_id DESC LIMIT`. The bound is a named constant in `DbAgent`, set to 1024.
[ ] After that load, `EventLog.nextId` is past the maximum `event_id` in the table, including rows older than the window and rows `decodeRows` dropped.
[ ] `EventLog.recoverOnInvalid` applies kept events after the projection checkpoint only when that checkpoint is at or after the window floor (or the table was not truncated). A `LOAD-FALLBACK` empty graph, or a projection older than the floor, is left as loaded and is not rebuilt from the window. `LOAD-FALLBACK` is still written.
[ ] The `events` table still holds the older rows.
[ ] `Api.eventsThroughSnapshot` (Poll and Load) reports Stale, not a partial window, when the client's event id is older than the floor. A client whose event id is inside the window still receives the later kept events and can catch up. The client surfaces that Stale as `DataOutdated` (reload required).
[ ] `getEventsSince EventId.zero` still succeeds with the kept window, so mailbox seed does not fail.
[ ] A test inserts more than 1024 events, starts `DbAgent`, and asserts `getEventsSince EventId.zero` returns only the newest 1024, in order, and that the next assignable id is past the true tip.
[ ] A test whose projection checkpoint sits inside that window asserts the events after the checkpoint are still applied.
[ ] A test whose projection or fallback graph is older than the floor asserts recover does not apply the window onto that graph, and that `LOAD-FALLBACK` still logs in the fallback case.
[ ] A test asserts rows older than the window are still in the table after startup (`Database.getEvents` or `Database.getEventsAfter`).
[ ] A test asserts Poll or Load from an event id older than the floor is Stale, and that a cursor inside the window returns the later events.

## Stop

[ ] Stop before truncating or deleting older rows from the `events` table. `Database.clearEvents` stays on the existing empty-after-recover path only.
[ ] Stop before changing `EventJson.decodeBody` or adding a decoder for legacy top-level `kind: "change"` bodies.
[ ] Stop before changing `FileAgent` or `EventLogFile.readAllEvents`.
[ ] Stop before changing client undo/redo or `Want.compose`.
[ ] Stop before changing `EventLog.recoverOnInvalid` itself; `loadReconciled` decides whether to call it.
[ ] Stop before making `getEventsSince EventId.zero` return Stale, and before inventing a second client overlay beside `DataOutdated`.
