# 07 — Persistent server error log

**Status**: pending
Blockers: none — can start immediately.

## Scope

The only server diagnostics today are `eprintfn` to standard error, and `<dataDir>/SYSTEM/http-responses.log`. Neither one keeps a record across restarts. `DbAgent.liveStartupSweep` in `src/Server/Core/DbAgent.fs` prints `DbAgent: projection repair deleted=… ownershipUpdates=… insertNodes=… insertChildren=… ordinalShifts=… affected=[…]` to stderr; that stream reaches an Azure App Service only if application logging is turned on in the portal (the repo has no `web.config` or deploy setting that enables it). `HttpResponseLog.logPath` in `src/Server/HttpResponseLog.fs` writes `http-responses.log` under the data folder; `HttpResponseLog.register` calls `HttpResponseLog.prepareFresh`, which wipes that file on every process start. The data folder itself comes from `DataDir.resolve` in `src/Server/DataDir.fs`, reached through `resolveDataDir` in `src/Server/Server.fs` from the config key `DataDir`.

The [06 investigation](06-children-missing-on-connect/06-investigation.md) names three more startup failures that are not logged at all: `Database.loadPersistedState` in `src/Server/Database.fs` falls back silently to `Graph.create (), EventId.zero` when `tryLoadGraphFromProjection` returns `Error` (including `GraphProjection.graphFromPersistence` in `src/Shared/GraphProjection.fs` rejecting non-dense child ordinals with `graphFromPersistence: child ordinals must be 0..n-1 per parent`); `DbAgent.loadReconciled` drops a row whose `EventLogFile.decodeEvent` fails, via `List.choose` and `Result.toOption`, before `EventLog.recover` sees it; and `EventLog.recover` in `src/Shared/EventLog.fs` treats `ApplyResult.Invalid` by keeping the state unchanged and still advancing `eventId` past that event, discarding the reason. After the 2026-11-23 connect, those gaps left no surviving record that could confirm or rule out the cold-restart theory.

Add an append-only error log at `<dataDir>/SYSTEM/errors.log` (alongside `http-responses.log`, under `Bookkeeping.systemDir` in `src/Server/Bookkeeping.fs`). The file is not wiped on process start. Each entry is timestamped. Existing `eprintfn` lines stay on stderr as they are. A size bound or rotation policy is an open question; this ticket does not invent one.

The log captures at least:

- The `DbAgent: projection repair …` line from `DbAgent.liveStartupSweep`, including the same counts and affected ids it already prints.
- A `Database.loadPersistedState` fallback, with the `Error` reason `tryLoadGraphFromProjection` / `GraphProjection.graphFromPersistence` returned.
- Each event `DbAgent.loadReconciled` drops because `EventLogFile.decodeEvent` failed, with the row's event id and the decode error.
- Each event `EventLog.recover` advances past with `ApplyResult.Invalid`, with that event's id and the invalid reason.
- The DB-connection fallback in `DatabaseSetup.resolveDbConnection` (`src/Server/DatabaseSetup.fs`), the line that today is only `eprintfn "Gambol: DB connection failed - falling back to file store. …"`.

This ticket only adds logging. It does not change the fallback, the decode drop, or how `EventLog.recover` handles `ApplyResult.Invalid`. Those behavior questions stay with [06 — Children missing on the first connect after idle](06-children-missing-on-connect.md).

## Done

[ ] A new append-only file `<dataDir>/SYSTEM/errors.log` is written on the events in Scope, each entry timestamped.
[ ] Starting the server a second time leaves the previous entries in that file; nothing equivalent to `HttpResponseLog.prepareFresh` runs against it.
[ ] The existing `eprintfn` lines named in Scope still go to stderr, unchanged.
[ ] A test writes at least one of those events, restarts (or re-opens) the log path, and asserts the earlier entry is still there.
[ ] A test asserts a `loadPersistedState` fallback, a decode drop, and an `ApplyResult.Invalid` recover skip each produce an entry that includes the event id or the fallback reason.

## Stop

[ ] Stop before changing the fallback in `Database.loadPersistedState`, the `List.choose` drop in `DbAgent.loadReconciled`, or `EventLog.recover`'s handling of `ApplyResult.Invalid`; those are 06, not this ticket.
[ ] Stop before removing or redirecting the existing `eprintfn` lines.
[ ] Stop before inventing a size bound or rotation policy; leave that as an open question.
[ ] Stop if the new file is placed anywhere other than under the data folder's `SYSTEM/` directory.
