# 09 — Error log remaining uses

**Status**: defined
Blockers: [07 — Persistent server error log](07-persistent-error-log.md) — `ErrorLog` and `<dataDir>/SYSTEM/errors.log` exist.

## Scope

07 — Persistent server error log adds the append-only log and two call sites. This ticket adds logging for the remaining cold-start paths. Each entry is timestamped in that same file. Existing `eprintfn` lines stay on stderr.

The log records:

- A `Database.loadPersistedState` fallback in `src/Server/Database.fs`, with the `Error` reason from `Database.tryLoadGraphFromProjection` and `GraphProjection.graphFromPersistence`. `DatabaseSetup.loadPersistedDbState` in `src/Server/DatabaseSetup.fs` is the file-mode caller of that load.
- Each row `DbAgent.loadReconciled` in `src/Server/Core/DbAgent.fs` drops because `EventLogFile.decodeEvent` failed, with the row's event id and the decode error.
- Each event `EventLog.recover` in `src/Shared/EventLog.fs` advances past with `ApplyResult.Invalid`, with that event's id and the invalid reason.
- A startup projection sweep failure from `DbAgent.startupPrelude` in `src/Server/Core/DbAgent.fs`: a sweep exception, a sweep `Error`, or a failed post-sweep reload.
- A file/DB outline mismatch in `DatabaseSetup.validateAmbNetworkAgainstDb` in `src/Server/DatabaseSetup.fs`.
- An unhandled exception while `RouteRegistration.registerPersistenceAndRoutes` in `src/Server/RouteRegistration.fs` composes the persistence layer and routes, including a `DocumentLoader.loadState` failure inside `FileAgent.create`.

This ticket adds logging. The fallback in `Database.loadPersistedState`, the drop in `DbAgent.loadReconciled`, and `EventLog.recover` on `ApplyResult.Invalid` stay as they are. Those behavior questions stay with [06 — Children missing on the first connect after idle](06-children-missing-on-connect.md).

## Done

[ ] A `Database.loadPersistedState` fallback writes an entry that includes the fallback reason.
[ ] An `EventLogFile.decodeEvent` drop writes an entry that includes the event id and the decode error.
[ ] An `ApplyResult.Invalid` skip from `EventLog.recover` writes an entry that includes the event id and the invalid reason.
[ ] A startup sweep failure in `DbAgent.startupPrelude` writes an entry in `errors.log`.
[ ] A file/DB outline mismatch in `DatabaseSetup.validateAmbNetworkAgainstDb` writes an entry in `errors.log`.
[ ] An unhandled exception in `RouteRegistration.registerPersistenceAndRoutes` writes an entry in `errors.log`.
[ ] The existing `eprintfn` lines on these paths still go to stderr.
[ ] One focused test covers each of those entries.

## Stop

[ ] Stop before changing the fallback in `Database.loadPersistedState`, the drop in `DbAgent.loadReconciled`, or `EventLog.recover`'s handling of `ApplyResult.Invalid`.
[ ] Stop before removing or redirecting the existing `eprintfn` lines.
[ ] Stop before inventing a size bound or rotation policy for `errors.log`.
