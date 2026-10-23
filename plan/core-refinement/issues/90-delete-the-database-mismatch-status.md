# 90 — Delete the database mismatch status

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [89 — Delete the File-mode database check](89-delete-the-file-mode-database-check.md)

## 3. Look at
- Files:
  - `src/Server/DatabaseSetup.fs`
  - `src/Server/RouteAppShell.fs`
  - `src/Client/StatusView.fs`
  - `tests/Server.Tests/DatabaseSetupTests.fs`
- Types:
  - `DatabaseSetup.DbStatus`
- Functions:
  - `DatabaseSetup.statusFromMatches`, `DatabaseSetup.documentStatesMatch`
  - `RouteAppShell.dbStatusText`
  - the `"mismatch1"` and `"mismatch2"` arms in `src/Client/StatusView.fs`

## 4. Context
After [89 — Delete the File-mode database check](89-delete-the-file-mode-database-check.md), nothing returns `DbStatus.Mismatch1` or `DbStatus.Mismatch2`, and nothing in `src/` calls `statusFromMatches` or `documentStatesMatch`.

Each match on `DbStatus`:
- `RouteAppShell.dbStatusText` maps it to `"ok"`, `"mismatch1"`, `"mismatch2"` or `"absent"` for the page.
- `RouteRegistration.isWritable`, `SavePrep.syncDataDir` and `CoreRuntime.startHost` test only for `DbStatus.Ok`. They do not change.

## 5. Changes
- Delete `DbStatus.Mismatch1` and `DbStatus.Mismatch2`.
- Delete `DatabaseSetup.statusFromMatches` and `DatabaseSetup.documentStatesMatch`.
- `RouteAppShell.dbStatusText` drops its two mismatch arms.
- `src/Client/StatusView.fs` drops its `"mismatch1"` and `"mismatch2"` arms.

## 6. Tests
- Delete `statusFromMatches returns Ok when initial state matches` in `tests/Server.Tests/DatabaseSetupTests.fs`.
- Delete `statusFromMatches returns Mismatch1 when rebuild fixes mismatch` in the same file.
- Delete `statusFromMatches returns Mismatch2 when rebuild does not fix mismatch` in the same file.
- Delete `documentStatesMatch treats two outline reads as same when revision matches` in the same file.
- Filters:
  - `scripts/agent-test.sh --filter "DatabaseSetupTests"`
  - `scripts/agent-test.sh --filter "SessionTests"`

## 7. Done
- The named filters pass.
- No file under `src/` or `tests/` names `Mismatch1`, `Mismatch2`, `mismatch1`, `mismatch2`, `statusFromMatches` or `documentStatesMatch`.
- Upload, import, parse, Load, undo and persist complete.
