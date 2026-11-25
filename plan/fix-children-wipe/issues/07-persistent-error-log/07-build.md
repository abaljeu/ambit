# 07 — Build: Persistent server error log

Status: built, all Done items checked
Scope: [07 — Persistent server error log](../07-persistent-error-log.md)

This note describes the present state of the implementation, not a change history. It also covers the scope addition Alan asked for mid-build: logging every other cold-start error/fallback path in server startup, not only the five events the ticket originally named.

## The log

`ErrorLog` (`src/Server/ErrorLog.fs`) is the whole implementation. `ErrorLog.logPath dataDir` is `<dataDir>/SYSTEM/errors.log`, under `Bookkeeping.systemDir`. `ErrorLog.append` opens the file with `FileMode.Append`, so nothing ever truncates it; there is no `prepareFresh` equivalent. Every entry is one line, `<ISO-8601 UTC timestamp> <KIND> <detail>`; `oneLine` collapses any newlines in `detail` so multi-line messages (e.g. an outline diff) stay one log line. Writes serialize through a single `obj` lock and are best-effort: `tryAppendLine` swallows any write failure so a logging problem can never block startup or a request.

`ErrorLog.append` takes a `dataDir: string` directly. Every call site that may not yet have a resolved `dataDir` (only `Server.Main.configureApplication`'s own `resolveDataDir` failure) goes through the `dataDir: string option` typed appenders instead, which no-op on `None`:

- `appendProjectionRepair` — kind `PROJECTION-REPAIR`. Called from `DbAgent.liveStartupSweep` (`src/Server/Core/DbAgent.fs`) with the same `message` string that line's `eprintfn` already prints (counts and affected node guids).
- `appendLoadFallback` — kind `LOAD-FALLBACK`, `reason=<text>`. Called from `DbAgent.loadInitialState` and `DatabaseSetup.loadPersistedDbState`, both of which get the fallback reason from `Database.loadPersistedStateWithFallbackReason`.
- `appendDecodeDrop` — kind `DECODE-DROP`, `eventId=<n> reason=<text>`. Called from `DbAgent.loadReconciled` for each row `EventLogFile.decodeEvent` fails to decode.
- `appendRecoverInvalid` — kind `RECOVER-INVALID`, `eventId=<n> reason=<text>`. Called from `DbAgent.loadReconciled` for each event `EventLog.recoverWithInvalidSkips` reports as an `ApplyResult.Invalid` skip.
- `appendDbConnectionFallback` — kind `DB-CONNECTION-FALLBACK`. Called from `DatabaseSetup.resolveDbConnection`'s exception handler, with the same `message` string its `eprintfn` prints.
- `appendStartupFallback` — kind `STARTUP-FALLBACK`, `source=<text> detail=<text>`. The generic cold-start fallback case, used by:
  - `DbAgent.startupPrelude`'s private `failStartup`, for a sweep exception, a sweep `Error`, or a failed post-sweep reload (source `DbAgent.startupPrelude`).
  - `DatabaseSetup.validateAmbNetworkAgainstDb`'s private `logOutlineMismatch`, for a file/DB outline mismatch before and after the file-wins rebuild (source `DatabaseSetup.validateAmbNetworkAgainstDb`).
- `appendStartupException` — kind `STARTUP-EXCEPTION`, `source=<text> type=<text> message=<text>`. Any unhandled exception while composing the persistence layer and routes. Used by `RouteRegistration.registerPersistenceAndRoutes`, which wraps its own `registerPersistenceAndRoutesOk` call in a `try`/`with` that logs then `reraise()`s, so the original exception still reaches the host unchanged (e.g. a `DocumentLoader.loadState` throw inside `FileAgent.create`'s private `loadOrFail`).

Every `eprintfn` line named in the ticket's Scope, and in Alan's addition, is unchanged: `ErrorLog` calls sit alongside the existing `eprintfn`, not in place of it.

## The one cold-start error that cannot reach errors.log

`Server.Main.configureApplication`'s `resolveDataDir` failure (`src/Server/Server.fs`) has no `dataDir` to log into — that is what `resolveDataDir` was trying to produce. Its `eprintfn "Gambol: resolveDataDir failed: %s" ex.Message` stays the only record of that one failure; the code at that `try`/`with` says so in a comment.

## Diagnostic-only refactors behind the logging

Two existing functions kept their public signature and behavior unchanged; a new sibling function carries the extra diagnostic detail and the original delegates to it, discarding the extra output:

- `EventLog.recover` (`src/Shared/EventLog.fs`) now delegates to `EventLog.recoverWithInvalidSkips`, which returns the same recovered state plus the list of `(eventId, reason)` pairs for every `ApplyResult.Invalid` skip.
- `Database.loadPersistedState` (`src/Server/Database.fs`) now delegates to `Database.loadPersistedStateWithFallbackReason`, which returns the same state plus an `Option<string>` fallback reason.

`DbAgent.loadReconciled` and `DatabaseSetup.loadPersistedDbState` call the new `*WithFallbackReason` / `*WithInvalidSkips` functions directly, so they get the diagnostic detail without changing what `recover` or `loadPersistedState` do for any other caller.

## Tests

- `tests/Server.Tests/ErrorLogTests.fs` — unit tests on `ErrorLog` directly: append writes one timestamped line; multiple appends accumulate in order; a second `append` call in a fresh process (simulating a restart) keeps the earlier line; each typed appender writes its own kind and detail; every typed appender no-ops on `dataDir = None`; `logPath` is `<dataDir>/SYSTEM/errors.log`.
- `tests/Shared.Tests/EventTests.fs` — `recoverWithInvalidSkips reports the event id and reason recover discards` confirms the new function surfaces what `recover` throws away.
- `tests/Server.Tests/DbAgentTests.fs` — integration tests against the real test Postgres database:
  - `DbAgent startup sweep repair logs a PROJECTION-REPAIR entry` — an orphaned node triggers the sweep's own repair; the logged `affected=` list contains the orphan's `NodeId.Value` guid.
  - `DbAgent loadReconciled logs a DECODE-DROP entry for an unparseable row` and `DbAgent loadReconciled logs a RECOVER-INVALID entry for an ApplyResult.Invalid event`.
  - `DbAgent startup logs a LOAD-FALLBACK entry when the projection rejects non-dense ordinals`.
  - `DbAgent injected startup sweep error logs a STARTUP-FALLBACK entry` and `DbAgent missing ROOT startup sweep failure logs a STARTUP-FALLBACK entry` (the latter waits on the same rejected-post round trip the sibling fails-closed test already uses, since the real sweep failure's timing is otherwise non-deterministic).
  - `errors.log keeps earlier entries when DbAgent starts again on the same dataDir` — a second `DbAgent` over the same `dataDir` does not drop the first one's entries.
- `tests/Server.Tests/DatabaseSetupTests.fs` — `resolveDbConnection in File mode logs an outline mismatch when disk and DB diverge`: bootstraps the DB from a written workspace, then rewrites a plain outline node's text on disk and calls `resolveDbConnection` again; asserts `DbStatus.Mismatch1` and a `STARTUP-FALLBACK` entry naming `DatabaseSetup.validateAmbNetworkAgainstDb`. (A plain `File`-kind node's body text is not part of the cold-load outline comparison — `readAllDocuments cold load file-owns-directory without plain body` in `DocumentPersistChangeTests.fs` already documents this — so the mismatch has to live in a plain outline node, not a File's body, to be visible to `documentStatesMatch`.)
- `tests/Server.Tests/RouteRegistrationTests.fs` (new file) — two tests against a real `WebApplicationFactory<Program>`-backed app in file-persistence mode (`DB_CONNECTION_STRING=""`):
  - `an unreadable workspace listing makes FileAgent.create throw` — makes a written workspace's own `.amb` directory listing unreadable (`File.SetUnixFileMode UnixFileMode.None`) and asserts `FileAgent.create` throws. (An oversized `.amb` is not enough: `DocumentPersistChange.readDirectoryFileTexts` already treats `DocumentParseLimits.refuseText` there as a skip, not an error — `eprintfn "Gambol: skipping oversized document ...`. Only a filesystem-level read failure propagates as a genuine `Error` out of `DocumentLoader.tryLoadState`.)
  - `composing routes over that failure logs a STARTUP-EXCEPTION entry` — the same unreadable listing, built through the app's full startup path; asserts the host fails to compose and `errors.log` gets a `STARTUP-EXCEPTION` entry naming `RouteRegistration.registerPersistenceAndRoutes`.

Full suite: `tests/Server.Tests` 800 passed, 1 skipped, 801 total; `tests/Shared.Tests` 1949 passed, 4 skipped, 1953 total (both via `scripts/agent-test.sh --full`). The one Server.Tests skip predates this ticket (`upload structure persistence preserves existing file mtime`, tracked as issue 128); the four Shared.Tests skips are likewise pre-existing and unrelated.

## Left open

Size bound and rotation policy for `errors.log` are not implemented, as the ticket's Scope says not to invent one.
