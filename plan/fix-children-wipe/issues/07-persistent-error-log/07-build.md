# 07 — Build: Persistent server error log

Status: built for the logger and the two call sites in [07 — Persistent server error log](../07-persistent-error-log.md).

The other cold-start entries are [08 — Error log remaining uses](../08-error-log-remaining-uses.md).

## The log

`ErrorLog` in `src/Server/ErrorLog.fs` is the writer. `ErrorLog.logPath` is `<dataDir>/SYSTEM/errors.log`, under `Bookkeeping.systemDir`. `ErrorLog.append` opens the file with `FileMode.Append`. Each entry is one line: an ISO-8601 UTC timestamp, a kind, and a detail. `oneLine` turns a newline in the detail into `\n`. Writes share one `obj` lock. `tryAppendLine` swallows a write failure, so a logging problem stays off the startup path.

`ErrorLog.append` takes `dataDir: string`. The two call sites pass `string option` through these helpers. A `None` data directory writes nothing:

- `ErrorLog.appendProjectionRepair` — kind `PROJECTION-REPAIR`. `DbAgent.liveStartupSweep` in `src/Server/Core/DbAgent.fs` passes the same message its `eprintfn` prints (counts and affected node ids).
- `ErrorLog.appendDbConnectionFallback` — kind `DB-CONNECTION-FALLBACK`. `DatabaseSetup.resolveDbConnection` in `src/Server/DatabaseSetup.fs` passes the same message its `eprintfn` prints.

`DbAgent.createWithLiveSave` already holds `liveSaveDataDir` and passes it into `liveStartupSweep`. `resolveDbConnection` already takes `dataDir`.

## Tests

- `tests/Server.Tests/ErrorLogTests.fs` checks `ErrorLog` on its own: one timestamped line, a later line kept in order, a line written before this process kept, each of the two helpers, a `None` data directory, and `ErrorLog.logPath`.
- `tests/Server.Tests/DbAgentTests.fs`: ``DbAgent startup sweep repair logs a PROJECTION-REPAIR entry``.
- `tests/Server.Tests/DatabaseSetupTests.fs`: ``resolveDbConnection logs a file-store fallback when the DB connection fails``.

## Check

`scripts/agent-test.sh --full`: `tests/Server.Tests` 787 passed, 1 skipped, 788 total; `tests/Shared.Tests` 1948 passed, 4 skipped, 1952 total. The skips are the ones the suite already had (`upload structure persistence preserves existing file mtime`, and four Shared tests).

## Left open

A size bound and a rotation policy for `errors.log` stay an open question, as 07 — Persistent server error log says.
