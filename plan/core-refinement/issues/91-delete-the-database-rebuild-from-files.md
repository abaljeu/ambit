# 91 — Delete the database rebuild from files

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [90 — Delete the database mismatch status](90-delete-the-database-mismatch-status.md)

## 3. Look at
- Files:
  - `src/Server/Database.fs`
  - `src/Shared/Snapshot.fs`
  - `tests/Server.Tests/TestBackend.fs`
  - `tests/Server.Tests/ChangeEndpointResilienceTests.fs`
  - `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
  - `tests/Server.Tests/DbAgentTests.fs`
  - `tests/Shared.Tests/SnapshotTests.fs`
- Types:
  - `State`
- Functions:
  - `Database.rebuildFromDocumentFiles`, `Database.isEmpty`, `Database.replaceGraphProjectionWithTx`, `Database.getConnection`
  - `Snapshot.describeOutlineMismatch`, `Snapshot.normalizeOutlineForCompare`

## 4. Context
After [89 — Delete the File-mode database check](89-delete-the-file-mode-database-check.md) and [90 — Delete the database mismatch status](90-delete-the-database-mismatch-status.md), no file under `src/` calls `Database.rebuildFromDocumentFiles`, `Database.isEmpty` or `Snapshot.describeOutlineMismatch`.

Test callers of `Database.rebuildFromDocumentFiles`, which use it to seed a database:
- the setup in `tests/Server.Tests/ChangeEndpointResilienceTests.fs`
- `Db Graph greater than Log drops persist EventLog` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
- `rebuildFromDocumentFiles aligns DB with on-disk document` in `tests/Server.Tests/DbAgentTests.fs`, which tests the function itself

`Snapshot.normalizeOutlineForCompare` is also used by `DocumentPersistenceTests.fs` and stays.

## 5. Changes
- Delete `Database.rebuildFromDocumentFiles` and `Database.isEmpty`.
- Delete `Snapshot.describeOutlineMismatch`.
- Add `TestBackend.seedDatabase connStr state` in `tests/Server.Tests/TestBackend.fs`. It opens a connection with `Database.getConnection`, clears the `graph` table, calls `Database.replaceGraphProjectionWithTx` with the state's graph and event id, and commits. These are the steps `rebuildFromDocumentFiles` takes today.
- The setup in `ChangeEndpointResilienceTests.fs` and `Db Graph greater than Log drops persist EventLog` call `TestBackend.seedDatabase`.

## 6. Tests
- Delete `rebuildFromDocumentFiles aligns DB with on-disk document` in `tests/Server.Tests/DbAgentTests.fs`.
- Delete `describeOutlineMismatch reports first differing code point` in `tests/Shared.Tests/SnapshotTests.fs`.
- Change `Db Graph greater than Log drops persist EventLog` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` to seed with `TestBackend.seedDatabase`.
- The setup helper in `tests/Server.Tests/ChangeEndpointResilienceTests.fs` seeds with `TestBackend.seedDatabase`.
- Filters:
  - `scripts/agent-test.sh --filter "Db Graph greater than Log drops persist EventLog"`
  - `scripts/agent-test.sh --filter "ChangeEndpointResilienceTests"`
  - `scripts/agent-test.sh --filter "DbAgentTests"`
  - `scripts/agent-test.sh --filter "SnapshotTests"`

## 7. Done
- The named tests pass.
- No file under `src/` or `tests/` names `rebuildFromDocumentFiles`, `Database.isEmpty` or `describeOutlineMismatch`.
- Upload, import, parse, Load, undo and persist complete.
