# 101 — Client sync applies the persist version rule

**Status:** `defined`

## 1. Type
bug-fixing

## 2. Blockers
- [95 — Node JSON stores the persist versions](95-node-json-stores-the-persist-versions.md)
- [100 — Core loop gates Persisted on the written version](100-core-loop-gates-persisted-on-the-written-version.md)

## 3. Look at
- Files:
  - `src/Shared/SyncLogic.fs`
  - `tests/Shared.Tests/SyncLogicTests.fs`
- Types:
  - `ClientSyncState`, `Notice`, `PersistState`
- Functions:
  - `SyncLogic.applyLoggedNotice`, `SyncLogic.withNodeUpdateTime`, `SyncLogic.foldProjectedEvents`

## 4. Context
`SyncLogic.applyLoggedNotice` applies only the `updateTime` of a logged `Notice.NodePersisted` to the resident graph. After [97 — Graph change records changedVersion](97-graph-change-records-changedversion.md), `foldProjectedEvents` marks the client's owning special node `Unpersisted` and sets its `changedVersion` to the event id. After [100 — Core loop gates Persisted on the written version](100-core-loop-gates-persisted-on-the-written-version.md), the server applies the version rule. The client does not, so its persist state can differ from the server's.

## 5. Changes
- `SyncLogic.applyLoggedNotice` applies the `Notice.NodePersisted` rule from [100 — Core loop gates Persisted on the written version](100-core-loop-gates-persisted-on-the-written-version.md) on the resident graph: it sets `persistedVersion`, applies `updateTime`, and sets `Persisted` only when the written version is not older than `DocumentState.changedVersion`.

## 6. Tests
- Add `logged NodePersisted for an older version leaves the client node Unpersisted` in `tests/Shared.Tests/SyncLogicTests.fs`. A logged Edit at N+1 then a logged `NodePersisted` at N leaves the node `Unpersisted` with `changedVersion` N+1. A logged `NodePersisted` at N+1 sets `Persisted`.
- Filters:
  - `scripts/agent-test.sh --filter "logged NodePersisted for an older version leaves the client node Unpersisted"`
  - `scripts/agent-test.sh --filter "SyncLogicTests"`

## 7. Done
- The named tests pass.
- Upload, import, parse, Load, undo and persist complete.
