# 99 — NodePersisted carries the written version

**Status:** `defined`

> **Size: ask Alan before building.** This step changes 6 source files and 10 test files (16 files, under 20). The test edits are mechanical: each `Notice.NodePersisted (id, time)` pair gains `persistedVersion`. That is more than 4 changed tests. A `Notice.NodePersisted _` pattern still compiles and needs no edit.

## 1. Type
coding

## 2. Blockers
- [93 — Compile EventId before Model](93-compile-eventid-before-model.md)

## 3. Look at
- Files:
  - `src/Shared/History.fs` (where `Notice` is defined)
  - `src/Shared/EventJson.fs`
  - `src/Shared/SyncLogic.fs`
  - `src/Server/Core/PersistThread.fs`
  - `src/Server/Core/CoreMailboxLoad.fs`
  - `src/Server/Database.fs`
- Types:
  - `Notice`, `PersistThreadDeps`, `EventId`
- Each construction or pair match of `Notice.NodePersisted`:
  - `PersistThread.runRequest`
  - `CoreMailboxLoad.writePersisted`, `CoreMailboxLoad.applyNotice`
  - `EventJson.encodeNodePersisted`, `EventJson.encodeNotice`, `EventJson.decodeNodePersisted`, `EventJson.decodeNoticeBody` (the `"nodePersisted"` and `"snapshotDone"` tags)
  - `SyncLogic.applyLoggedNotice`
  - `Database.writeNoticeRow`
- Tests with a `Notice.NodePersisted` pair, by file and count:
  - `tests/Server.Tests/PersistThreadTests.fs`, 5
  - `tests/Server.Tests/CoreLoopNoticeTests.fs`, 3
  - `tests/Shared.Tests/EventJsonTests.fs`, 3
  - `tests/Server.Tests/FileAgentFailureTests.fs`, 2
  - `tests/Server.Tests/DbAgentNoticeTests.fs`, 1
  - `tests/Server.Tests/DbAgentTests.fs`, 1
  - `tests/Server.Tests/DatabaseProjectionContractTests.fs`, 1
  - `tests/Shared.Tests/SyncLogicTests.fs`, 1
  - `tests/Shared.Tests/AckReconcileTests.fs`, 1
  - `tests/Shared.Tests/SplitOriginTraceTests.fs`, 1

## 4. Context
`Notice.NodePersisted` is `nodeId: NodeId * updateTime: System.DateTime`. `PersistThread.runRequest` reads one `State` through `deps.currentState ()`. It skips the write when `CoreEventDispatch.persistedNode` says the node is `Persisted`. Otherwise it writes that state's graph and posts `Notice.NodePersisted(request.nodeId, updateTime)`. The thread does not edit the graph. The notice does not say which graph version was written.

## 5. Changes
- `Notice.NodePersisted` becomes `NodePersisted of nodeId: NodeId * persistedVersion: EventId * updateTime: System.DateTime`.
- `PersistThread.runRequest` posts the `eventId` of the `State` that that request's `currentState ()` call returned, with the `updateTime` of the successful write. The thread still does not edit the graph.
- `EventJson.encodeNodePersisted` writes `"persistedVersion"` with `EventId.toJson`. `EventJson.decodeNodePersisted` reads it with `EventId.fromJson`. A stored `nodePersisted` or `snapshotDone` entry with no `persistedVersion` decodes as `EventId.zero`.
- `CoreMailboxLoad.writePersisted`, `CoreMailboxLoad.applyNotice`, `SyncLogic.applyLoggedNotice` and `Database.writeNoticeRow` take the new shape. Their behavior is unchanged in this step: the handler still sets `Persisted`.
- Each test pair in Look at gains `persistedVersion`.

## 6. Tests
- Change `Notice JSON round trip` in `tests/Shared.Tests/EventJsonTests.fs`. Its `NodePersisted` carries a non-zero `persistedVersion` and round-trips it.
- Add `An old nodePersisted row decodes at version zero` in `tests/Shared.Tests/EventJsonTests.fs`. A `nodePersisted` JSON with no `persistedVersion` decodes to `EventId.zero`.
- Add `persist thread posts the version it read` in `tests/Server.Tests/PersistThreadTests.fs`. The posted `persistedVersion` is the `eventId` of the State that `currentState` returned.
- The other pairs in Look at change mechanically.
- Filters:
  - `scripts/agent-test.sh --filter "EventJsonTests"`
  - `scripts/agent-test.sh --filter "PersistThreadTests"`
  - `scripts/agent-test.sh --filter "CoreLoopNoticeTests"`

## 7. Done
- The named tests pass.
- The thread still does not edit the graph.
- Upload, import, parse, Load, undo and persist complete.
