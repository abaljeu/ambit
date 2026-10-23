# 96 — Node row stores the persist versions

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [94 — Document state carries persist versions](94-document-state-carries-persist-versions.md)

## 3. Look at
- Files:
  - `src/Server/Database.fs`
  - `src/Server/DatabaseProjection.fs`
  - `src/Shared/GraphProjection.fs`
  - `tests/Server.Tests/DatabaseProjectionContractTests.fs`
  - `tests/Shared.Tests/GraphProjectionTests.fs`
  - `tests/Server.Tests/DatabaseProjectionTests.fs`
- Types:
  - `GraphProjection.NodePersistenceRow`, `Database.NodeDbRow`
- Functions that already carry `persist_state`:
  - `Database.initSchema`, `Database.readNodeRows`, `Database.tryLoadGraphFromProjection`, `Database.replaceGraphProjectionWithTx`
  - `Database.writePersistState`, `Database.writePersistedNode`, `Database.writeReceivedNode`, and their SQL (`updatePersistStateSql`, `updatePersistedSql`, `updateReceivedNodeSql`)
  - `GraphProjection.nodeRowFromNode`, `GraphProjection.graphFromPersistence`
  - `DatabaseProjection.upsertNodesSql`, `DatabaseProjection.bindings`, `DatabaseProjection.readNode`, `DatabaseProjection.executeMaintenance`
- Tests that build a `NodePersistenceRow` record:
  - `tests/Shared.Tests/GraphProjectionTests.fs`, 5
  - `tests/Server.Tests/DatabaseProjectionTests.fs`, 1

## 4. Context
After [94 — Document state carries persist versions](94-document-state-carries-persist-versions.md), the node row stores only `persist_state`. Start rebuilds every node at `persistedVersion = EventId.zero` and `Unpersisted EventId.zero`. `Database.initSchema` adds columns with `ADD COLUMN IF NOT EXISTS`.

## 5. Changes
- `Database.initSchema` adds `persisted_version` and `changed_version`, `INTEGER NOT NULL DEFAULT 0`, to the node table.
- `NodePersistenceRow` and `NodeDbRow` gain both values.
- Every write and read in Look at that carries `persist_state` carries both columns. `changed_version` is the `Unpersisted` version, or 0 when `Persisted`.
- `Database.tryLoadGraphFromProjection`, through `GraphProjection.graphFromPersistence`, rebuilds `persistedVersion` and `Unpersisted changedVersion` with `EventId.fromJson`.
- The test records in Look at set both values to 0.

## 6. Tests
- Add `document state versions survive a database round trip` in `tests/Server.Tests/DatabaseProjectionContractTests.fs`. `Database.replaceGraphProjectionWithTx` then `Database.tryLoadGraphFromProjection` returns the same `persistedVersion` and `Unpersisted changedVersion`.
- Change the `NodePersistenceRow` records in `GraphProjectionTests.fs` and `DatabaseProjectionTests.fs` to set both values.
- Filters:
  - `scripts/agent-test.sh --filter "document state versions survive a database round trip"`
  - `scripts/agent-test.sh --filter "GraphProjectionTests"`
  - `scripts/agent-test.sh --filter "DatabaseProjectionTests"`

## 7. Done
- The named tests pass.
- An existing database starts and gets both columns at 0.
- Upload, import, parse, Load, undo and persist complete.
