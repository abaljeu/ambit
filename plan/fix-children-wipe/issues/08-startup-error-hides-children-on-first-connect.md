# 08 — Startup error hides children on first connect

**Status**: waiting on [07 — Persistent server error log](07-persistent-error-log.md) and a repeated, logged occurrence
Blockers: that log must be in place, and a later cold start must write the path that fired into `<dataDir>/SYSTEM/errors.log`, before any fix is built.

## Scope

This is a startup error, not a database error, and no recovery is needed. The findings in [06 — Children missing on the first connect after idle](06-children-missing-on-connect.md) and the investigation in [PR 642](https://github.com/abaljeu/ambit/pull/642) stand; the root cause is now understood as a startup error. The walk of the start path is [06 — Investigation: children missing on the first connect after idle](06-children-missing-on-connect/06-investigation.md).

Confirmed facts from Alan for the 2026-11-23 first connect after idle:

- The `db-status` label (`StatusView.renderDatabaseStatus`, driven by `DatabaseSetup.DbStatus` in `RouteAppShell.fs`) said "DB synced", not "Files only".
- The page did not say it was still loading on the first view: `StateResponse.isReady` / `CoreDoor.isReady` was already true.
- The display showed solid circles on ordinary named rows, not dark placeholder rows (`ViewModelChildrenIndicator.rowChildrenIndicator`, drawn by `RowView.nodeBullet`).
- The saved zoom varied and pointed outside both LAPS and the ambit Workspace.

### Re-rank against those facts

- "DB synced" is the `DbStatus.Ok` label. Candidate 1 — a `Database.initSchema` exception in `DatabaseSetup.resolveDbConnection` that returns `DbStatus.Absent` and lets `CoreRuntime.startHost` boot `FileAgent` for the life of the process — shows "Files only". That path is not the Nov 23 connect.
- Ordinary named rows are present Headers with an empty `childMap` (Parsed empty). Candidate 4 — `Api.getState`'s `ResidentProjection.visibleClosureGraph` trim sending a nested Workspace as an Absent Header — uses the dark placeholder bullet. Zoom pointing outside LAPS and ambit would have made that trim more likely to omit those Headers, but the rows that were shown were ordinary named Headers, so the placeholder path is not what the display showed.
- `isReady` already true means the first `/state` was served after `DbAgent.liveStartupSweep` had finished and readiness was set. Candidate 3 — `CoreMailboxLoop.isStartupRead` answering `Query.GraphAtEvent` during the prelude — is not, on its own, the window that was painted.

The lead is therefore the investigation's candidate 2: a cold `DbAgent` replay, or a `Database.loadPersistedState` projection fallback (`GraphProjection.graphFromPersistence` returning `Error`, then a full replay from `Graph.create (), EventId.zero`), left a parent Parsed with an empty child list, and that graph was then published as ready. `EventLogFile.decodeEvent` dropping a row in `DbAgent.loadReconciled`, or `EventLog.recover` advancing past `ApplyResult.Invalid`, can freeze that empty list in memory without writing it back to `node_children`. `Want.compose` (`src/Shared/Want.fs`) requests only Absent Headers, so `Program.fs` `loadFromState` never re-asks for a Header already present with an empty list. The first paint stays empty for the session. A later start whose projection load succeeds shows the children again, with no event in between.

The exact drop or fallback is still unconfirmed against a log line. These facts only re-rank the candidates.

### Next step

07 — Persistent server error log is being built now. Wait for the error to repeat, then read `<dataDir>/SYSTEM/errors.log` to see which silent path fired: the `loadPersistedState` fallback, a decode drop, or an `ApplyResult.Invalid` recover skip. Build no fix before that logged occurrence.

### Provisional fix

Until the log names the path, this fix is provisional. A later coding agent would:

- Make `EventLog.recover` surface a dropped event instead of swallowing it, and have `loadReconciled` refuse readiness rather than publish a Graph with a shortened history.
- Make the `loadPersistedState` `Error` branch a loud startup incident, not a silent fallback to an empty Graph.
- Independently, have the client re-fetch `/state` or re-want a present Parsed-empty Header, so a first paint with empty lists does not stay sticky for the session. Cover `Want.compose` so that filter does not regress.

### Provisional tests

- Unskip ``recover silently drops a stale Replace and freezes the parent's children empty`` in `tests/Shared.Tests/EventTests.fs` once `EventLog.recover` surfaces the drop, and assert the failure is visible to `loadReconciled`.
- A `loadPersistedState` test: `graphFromPersistence` returns `Error`, and startup does not publish `Graph.create (), EventId.zero` as a ready Graph.
- A `Want.compose` test that a present Parsed-empty id is re-requested, or that a client re-fetch replaces that Header's empty list, so the sticky-paint fix does not regress.

## Done

[ ] 07 — Persistent server error log is in place, and a later occurrence has written the path that fired into `<dataDir>/SYSTEM/errors.log`.
[ ] Against that log line, the leading path (projection fallback, decode drop, or recover skip) is confirmed or revised.
[ ] Only after that confirmation, the provisional fix for the confirmed path is built, with the tests above that match it.
[ ] [[../project.md]] "Candidate causes, ranked" is updated with the confirmed cause.

## Stop

[ ] Stop before building any fix until 07 — Persistent server error log is in place and a repeated occurrence has been read from `<dataDir>/SYSTEM/errors.log`.
[ ] Stop before concluding a path is the confirmed cause from the re-rank alone; the log line is the confirmation.
[ ] Stop before changing FileAgent, or treating the `resolveDbConnection` fallback as the Nov 23 path; "DB synced" already leaves that path out of this occurrence.
