# 92 — Delete the meta file path

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [87 — Remove the event-id checkpoint](87-remove-the-event-id-checkpoint.md)
- [91 — Delete the database rebuild from files](91-delete-the-database-rebuild-from-files.md)

## 3. Look at
- Files:
  - `src/Server/Bookkeeping.fs`
  - `src/Server/DocumentPersistPath.fs`
  - `tests/Server.Tests/SavePrepTests.fs`
  - `tests/Server.Tests/GitSaveEndpointTests.fs`
  - `tests/Server.Tests/DocumentPersistenceTests.fs`
  - `tests/Shared.Tests/LazyLoadReconciliationTests.fs`
  - `tests/Server.Tests/WorkspaceGitTests.fs`
  - `tests/Server.Tests/LazyLoadReconciliationServerTests.fs`
- Functions:
  - `Bookkeeping.metaPath`, `Bookkeeping.systemDir`
  - `DocumentPersistPath.shouldSkipDiscoveryFile`, `DocumentPersistPath.discoverArtifactRelatives`
  - `Filename.isReservedSystemName` (skips every `gambol.*` name)

## 4. Context
After [87 — Remove the event-id checkpoint](87-remove-the-event-id-checkpoint.md), nothing in `src/` reads or writes `SYSTEM/gambol.meta`. After [91 — Delete the database rebuild from files](91-delete-the-database-rebuild-from-files.md), `DbAgentTests.fs` does not name it.

- `Bookkeeping.metaPath` has no `src/` caller. Four tests use it to put a file in SYSTEM:
  - `Git DB flush returns eventId without rewriting disk` and `Full DB sync returns eventId without rewriting disk` in `SavePrepTests.fs` write a sentinel there and check a sync leaves it alone.
  - `POST save commits file changes in data dir` and `POST save commit message includes X-Gambol-Client` in `GitSaveEndpointTests.fs` write `"0"` there before the save.
- `DocumentPersistPath.shouldSkipDiscoveryFile` skips every name ending in `.meta`, as well as each name `Filename.isReservedSystemName` matches.
- Tests that name a `gambol.meta` file to check the `gambol.*` rule, and stay unchanged: `discoverArtifactRelatives excludes reserved gambol dot files` (`DocumentPersistenceTests.fs`), `reserved gambol dot files are excluded from reconciliation` (`LazyLoadReconciliationTests.fs`), `ensureInit excludes reserved gambol dot files from tracking` (`WorkspaceGitTests.fs`) and `SYSTEM workspace reconcile creates File stubs under systemId` (`LazyLoadReconciliationServerTests.fs`).

## 5. Changes
- Delete `Bookkeeping.metaPath`. Change the module comment to name the SYSTEM folder, not `gambol.meta`.
- `DocumentPersistPath.shouldSkipDiscoveryFile` stops skipping names that end in `.meta`. An old `SYSTEM/gambol.meta` is still skipped by `Filename.isReservedSystemName`.

## 6. Tests
- Change `Git DB flush returns eventId without rewriting disk` in `tests/Server.Tests/SavePrepTests.fs`. Its sentinel is `Path.Combine(Bookkeeping.systemDir dataDir, "sentinel.txt")`.
- Change `Full DB sync returns eventId without rewriting disk` in the same file the same way.
- Change `POST save commits file changes in data dir` in `tests/Server.Tests/GitSaveEndpointTests.fs`. It writes `Path.Combine(Bookkeeping.systemDir tempDir, "user.css")`.
- Change `POST save commit message includes X-Gambol-Client` in the same file the same way.
- Filters:
  - `scripts/agent-test.sh --filter "SavePrepTests"`
  - `scripts/agent-test.sh --filter "GitSaveEndpointTests"`
  - `scripts/agent-test.sh --filter "reserved gambol dot files"`
  - `scripts/agent-test.sh --filter "SYSTEM workspace reconcile creates File stubs under systemId"`

## 7. Done
- The named tests pass.
- No file under `src/` or `tests/` names `metaPath`.
- `DocumentPersistPath.fs` does not name `.meta`.
- Upload, import, parse, Load, undo and persist complete.
