# 07 — Persistent server error log

**Status**: ready-for-human
Blockers: none — built; see [07 — Build](07-persistent-error-log/07-build.md).

## Scope

The server writes diagnostics with `eprintfn` to standard error, and writes `<dataDir>/SYSTEM/http-responses.log`. `HttpResponseLog.register` calls `HttpResponseLog.prepareFresh`, which wipes that file on every process start. The data folder comes from `DataDir.resolve` in `src/Server/DataDir.fs`, through `resolveDataDir` in `src/Server/Server.fs`, from the config key `DataDir`.

This ticket adds an append-only error log at `<dataDir>/SYSTEM/errors.log`, beside `http-responses.log`, under `Bookkeeping.systemDir` in `src/Server/Bookkeeping.fs`. The file stays across process starts. Each entry has a timestamp. Existing `eprintfn` lines stay on stderr. A size bound or rotation policy stays an open question.

The log records these two events:

- The `DbAgent: projection repair …` line from `DbAgent.liveStartupSweep` in `src/Server/Core/DbAgent.fs`, with the same counts and affected ids that line already prints.
- The DB-connection fallback in `DatabaseSetup.resolveDbConnection` in `src/Server/DatabaseSetup.fs`, the line that prints `Gambol: DB connection failed - falling back to file store. …`.

Further cold-start entries are [08 — Error log remaining uses](08-error-log-remaining-uses.md).

This ticket adds logging. Load, recover, and fallback behavior stay as they are. Those behavior questions stay with [06 — Children missing on the first connect after idle](06-children-missing-on-connect.md).

## Done

[x] An append-only file `<dataDir>/SYSTEM/errors.log` receives the two events in Scope, each entry timestamped.
[x] A second open of the log leaves the previous entries in that file. The writer uses `FileMode.Append`.
[x] The existing `eprintfn` lines named in Scope still go to stderr.
[x] A test writes an entry, re-opens the log path, and asserts the earlier entry is still there.
[x] `DbAgent.liveStartupSweep` writes a projection-repair entry that includes the repair counts.
[x] `DatabaseSetup.resolveDbConnection` writes a file-store fallback entry when the DB connection fails.

## Stop

[ ] Stop before changing load, recover, or fallback behavior. Those questions stay with 06 — Children missing on the first connect after idle. Logging for the other cold-start paths is 08 — Error log remaining uses.
[ ] Stop before removing or redirecting the existing `eprintfn` lines.
[ ] Stop before inventing a size bound or rotation policy.
[ ] Stop if the new file sits anywhere other than under the data folder's `SYSTEM/` directory.
