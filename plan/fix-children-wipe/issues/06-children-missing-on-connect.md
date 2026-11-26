# 06 — Children missing on the first connect after idle

**Status**: pending
Blockers: none — can start immediately.

Investigation: [06 — Investigation: children missing on the first connect after idle](06-children-missing-on-connect/06-investigation.md)

## Scope

On 2026-11-23, the first connect after the user's idle gap showed LAPS's descendants and the ambit workspace Node with no children (filled-circle bullets, Parsed with an empty list). On 2026-11-24 the same Nodes showed their children again, and no event between those times touched either list: [[../evidence/results/results.txt]] shows every change to LAPS's child list was a 1–2 child user edit, with no orphans and no removal event. The data was intact in Postgres throughout, so the empty lists came from the connect itself, not from the store. This is a display/load bug, confirmed separate from the data loss [02 — Recover the lost children](02-recover-children.md) covers.

Investigate these paths by reading the source, then find which one served the empty lists and fix it.

- `DbAgent.loadReconciled` in `src/Server/Core/DbAgent.fs` rebuilds the in-memory graph at startup by replaying the `events` table through `EventLog.recover`; a row that `EventLogFile.decodeEvent` fails to decode is silently dropped.
- The startup projection rebuild: `DbAgent.liveStartupSweep` runs `DatabaseProjection.startupSweepPatch` as the mailbox's `until` prelude. `CoreMailboxLoop.isStartupRead` (`src/Server/Core/CoreMailboxLoop.fs`) lets `Query.GraphAtEvent`, `Query.LastEventId`, and `Query.EventsSince` run before that prelude finishes, so a connect landing in that window reads whatever `loadReconciled` produced, ahead of the sweep's repair.
- The session's initial snapshot: `Api.getState` in `src/Server/Api.fs` calls `CoreDoor.graphAtEvent` and, for the default `BootstrapScope.RootClosure`, trims the reply through `ResidentProjection.visibleClosureGraph`.
- The client's boot cache: `Program.fs`'s boot sequence (`loadFromState`) always fetches `/state` fresh; `BootCacheStore.readCache` and `BootCache.decideBootRead` / `decideBootReadWait` (`src/Shared/BootCache.fs`, `src/Client/BootCacheStore.fs`) are exercised only by `tests/Shared.Tests/BootCacheTests.fs`, not by that boot sequence. Confirm this still holds for the deployed build; if it does not, a stale cached snapshot read back after an idle gap is the other classic shape of this bug.

## Evidence to capture next time

- The browser console line `[Gambol boot] decodeStateResponse: …ms, …chars, …nodes` (`Program.fs` `loadFromState`), and the Network tab's `/state` response body for the affected Nodes, including the `isReady` field (`StateResponse.isReady`, `CoreDoor.isReady`).
- The server startup log line `DbAgent: projection repair deleted=… ownershipUpdates=… insertNodes=… insertChildren=… ordinalShifts=…` (`DbAgent.liveStartupSweep`), with its timestamp relative to the first connect.
- The `db-status` label ("DB synced", "Files only", "DB mismatch1", or "DB mismatch2"; `StatusView.renderDatabaseStatus`, driven by `DatabaseSetup.DbStatus` in `RouteAppShell.fs`) as it read at that connect.

## Done

[ ] The path that can hand the client a Parsed Node with an empty child list while Postgres holds that Node's children is identified and named, confirmed against a captured occurrence rather than from code alone.
[ ] That path is fixed so a first connect after idle waits for, or re-requests, a complete child list instead of painting an empty one.
[ ] A regression test reproduces the confirmed timing and asserts the served Nodes keep their children.
[ ] [[../project.md]] "Candidate causes, ranked" is updated with the confirmed cause.

## Stop

[ ] Stop before concluding a path is the cause from code alone; confirm against a captured occurrence (browser console, server startup log, and `db-status` tooltip) first.
[ ] Stop if investigation turns up an event that actually emptied a child list; that is data loss, not this display bug, and belongs with [02 — Recover the lost children](02-recover-children.md) or [04 — Guard Save on an Unparsed Node](04-save-unparsed-guard.md) instead.
