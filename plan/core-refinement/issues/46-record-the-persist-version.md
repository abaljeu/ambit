# 46 — Record the persist version

**Status:** `defined`

## 1. Type

`coding`

## 2. Blockers

- [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)

## 3. Look at

The version is `State.eventId`. `EventId` lives in `src/Shared/History.fs` today, and `Model.fs` is compiled before that file.

- `EventId` and `EventId` in `src/Shared/History.fs`
- `PersistState`, `DocumentState`, `DocumentState.fromParts`, `Node.Create`, and `Node.withPersistState` in `src/Shared/Model.fs`
- `Notice` in `src/Shared/History.fs`
- `State` in `src/Shared/History.fs`
- `PersistHandlers.applyEvent` in `src/Server/Core/CoreMsg.fs`
- `PersistThreadDeps.currentState` in `src/Server/Core/PersistThread.fs`
- `GraphProjection.NodePersistenceRow` in `src/Shared/GraphProjection.fs`
- `Database.NodeDbRow` in `src/Server/Database.fs`

The mark that this ticket versions is `GraphMutate.markOwningSpecialUnpersisted`. The notice that this ticket versions is built in `PersistThread.runRequest` and applied in `CoreMailboxLoad.writePersisted`.

`PersistState` constructions and matches:

- `GraphMutate.markOwningSpecialUnpersisted`
- `GraphProjection.storedPersistWord`
- `GraphProjection.persistFromStoredWord`
- `GraphProjection.nodeEquals`
- `Serialization.encodePersistState`
- `Serialization.decodePersistState`
- `Serialization.nodeFromFields`
- `FileAgent.markUnpersisted`
- `FileAgent.scheduleCatchUp`
- `DbAgent.noteParsed`
- `CoreMailboxLoad.writePullingFileUnpersisted`
- `CoreMailboxLoad.applyNotice` for `Notice.NodePersisting`
- `DocumentPersistPath.stampNode` writes `PersistState.Persisted` and stays on that case

`markUnpersisted: bool` callers, each of which takes the version in its place:

- `GraphMutate.finishPersist`
- `GraphMutate.setText`
- `GraphMutate.setClasses`
- `GraphMutate.setName`
- `GraphMutate.replace`
- `Op.applyAllowed`
- `Op.applyAllowing`
- `Op.applyAllAllowing`
- `Op.apply`
- `Op.applyAll`
- `Ev.apply`
- `ChangeValidation.applyOpsAllowing`
- `ChangeValidation.applyOps`
- `ChangeAmendment.applyMarked`
- `ChangeAmendment.applyOps`
- `ChangeAmendment.applyForCommand`
- `ChangeAmendment.applyForGraphOnly`
- `ResidentProjection.applyOpAllowing`
- `ResidentProjection.applyOp`
- `ResidentProjection.applyOpsAllowing`
- `ResidentProjection.applyOpForSyncAllowing`
- `ResidentProjection.applyOpForSync`
- `ResidentProjection.applyOpsForSyncAllowing`
- `FileAgent.applyOne`
- `DbAgent.applyOneEvent`
- `SyncLogic.foldProjectedEvents`
- `BootCache.foldLog`

`Notice.NodePersisted` constructions and pair matches:

- `PersistThread.runRequest`
- `CoreMailboxLoad.writePersisted`
- `CoreMailboxLoad.applyNotice`
- `EventJson.encodeNodePersisted`
- `EventJson.encodeNotice`
- `EventJson.decodeNodePersisted`
- `SyncLogic.applyLoggedNotice`
- `Database.writeNoticeRow`

Row writes and reads that already carry `persist_state`:

- `Database.initSchema`
- `Database.readNodeRows`
- `Database.tryLoadGraphFromProjection`
- `Database.replaceGraphProjectionWithTx`
- `Database.writePersistState`
- `Database.writePersistedNode`
- `Database.writeReceivedNode`
- `GraphProjection.nodeRowFromNode`
- `GraphProjection.graphFromPersistence`
- `DatabaseProjection.upsertNodesSql`
- `DatabaseProjection.bindings`
- `DatabaseProjection.readNode`
- `DatabaseProjection.executeMaintenance`

`PersistState.Unpersisted` in tests, by file and match count:

- `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`, 23
- `tests/Server.Tests/CoreLoopNoticeTests.fs`, 18
- `tests/Server.Tests/DocumentOpPersistenceTests.fs`, 7
- `tests/Server.Tests/DocumentPersistenceTests.fs`, 5
- `tests/Server.Tests/PersistThreadTests.fs`, 3
- `tests/Shared.Tests/HistoryTests.fs`, 2
- `tests/Shared.Tests/LargeChangeApplyTests.fs`, 2
- `tests/Shared.Tests/GraphProjectionTests.fs`, 2
- `tests/Server.Tests/DbAgentFailureTests.fs`, 1
- `tests/Server.Tests/DbAgentNoticeTests.fs`, 1
- `tests/Shared.Tests/DocumentOpImpactTests.fs`, 1
- `tests/Server.Tests/DatabaseProjectionTests.fs`, 1
- `tests/Server.Tests/FileAgentFailureTests.fs`, 1
- `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, 1

`Notice.NodePersisted` pair patterns and constructions in tests, by file and count:

- `tests/Server.Tests/PersistThreadTests.fs`, 5
- `tests/Server.Tests/CoreLoopNoticeTests.fs`, 3
- `tests/Shared.Tests/EventJsonTests.fs`, 3
- `tests/Server.Tests/FileAgentFailureTests.fs`, 2
- `tests/Server.Tests/DbAgentNoticeTests.fs`, 1
- `tests/Shared.Tests/SyncLogicTests.fs`, 1
- `tests/Shared.Tests/AckReconcileTests.fs`, 1
- `tests/Shared.Tests/SplitOriginTraceTests.fs`, 1

## 4. Context

A persist write can finish after a later graph change. The file then holds the earlier graph. `Notice.NodePersisted` is `nodeId` and `updateTime`. `CoreMailboxLoad.applyNotice` sets that node `PersistState.Persisted` for every such notice.

`PersistState` is `Persisted | Unpersisted`. `DocumentState` holds `parseState`, `persistState`, and `pullingFile`. `GraphMutate.markOwningSpecialUnpersisted` writes `Unpersisted` on the nearest owning special node and records no version. `PersistThread.runRequest` reads one `State` through `currentState` and posts `Notice.NodePersisted` for that write.

[Graph change persist version](../arch.md#graph-change-persist-version) is this step. It follows [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md).

## 5. Changes

Move `EventId` and its module, unchanged, into `src/Shared/EventId.fs` in `namespace Gambol.Shared`. List that file in `src/Shared/Gambol.Shared.fsproj` immediately before `Model.fs`.

Add `persistedVersion: EventId` to `DocumentState`. `DocumentState.fromParts` and `Node.Create` set it to `EventId.zero`.

Make `PersistState` this:

- `Persisted`
- `Unpersisted of changedVersion: EventId`

Replace `markUnpersisted: bool` with `changedVersion: EventId option` on the apply chain in Look at. `None` leaves the persist axis as it is. `Some version` writes `Unpersisted version` from `GraphMutate.markOwningSpecialUnpersisted`.

These callers pass `Some event.id` when `Op.marksOwningPersist` is true, and `None` when it is false:

- `Ev.apply`
- `ChangeAmendment.applyForCommand` and `ChangeAmendment.applyForGraphOnly`, called from `FileAgent.applyOne` and `DbAgent.applyOneEvent`
- `SyncLogic.foldProjectedEvents`
- `BootCache.foldLog`

`Op.apply`, `Op.applyAll`, `ChangeValidation.applyOps`, `ResidentProjection.applyOp`, and `ResidentProjection.applyOpForSync` pass `Some state.eventId`.

`FileAgent.markUnpersisted` writes `Unpersisted` of `loaded.state` `eventId`. `CoreMailboxLoad.writePullingFileUnpersisted` and the `Notice.NodePersisting` arm write `Unpersisted` of the `eventId` from `persist.getState ()`.

JSON on the node, beside the persist word:

- `persistedVersion` via `EventId.toJson` and `EventId.fromJson`
- `changedVersion` via the same pair

A missing field decodes as `EventId.zero`. An `Unpersisted` value decoded at `EventId.zero` is `Unpersisted EventId.zero`.

The node row gains `persisted_version` and `changed_version`, `INTEGER NOT NULL DEFAULT 0`. Every write and read in Look at that already carries `persist_state` carries both columns. `Database.tryLoadGraphFromProjection` rebuilds `persistedVersion` and `Unpersisted changedVersion` from them. `GraphProjection.nodeEquals` compares `persistedVersion` and the `Unpersisted` version.

`Notice.NodePersisted` becomes `nodeId: NodeId * persistedVersion: EventId * updateTime: System.DateTime`.

`PersistThread.runRequest` posts the `eventId` of the `State` returned by that request's `currentState ()` call, with the `updateTime` of the successful write. The thread posts the notice. The Core loop writes the graph.

`EventJson.encodeNodePersisted` and `EventJson.decodeNodePersisted` write and read `persistedVersion` with `EventId.toJson` and `EventId.fromJson`. A stored `nodePersisted` or `snapshotDone` entry with no `persistedVersion` decodes as `EventId.zero`.

`CoreMailboxLoad.applyNotice` for `Notice.NodePersisted`:

- sets that node's `persistedVersion` to the written version
- applies `updateTime`
- sets `Persisted` when the node is `Persisted`, or when it is `Unpersisted changedVersion` and `EventId.value persistedVersion >= EventId.value changedVersion`
- leaves `Unpersisted changedVersion` when the written version is older

`SyncLogic.applyLoggedNotice` applies that same version rule and the `updateTime` on the resident graph.

## 6. Tests

Add these tests. Each existing `PersistState.Unpersisted` construction in Look at passes `EventId.zero` where the test has no graph-change id. Each existing `Notice.NodePersisted` pair in Look at gains `persistedVersion`.

- `document state versions survive a JSON round trip` in `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`. A node encoded with `persistedVersion` and `Unpersisted changedVersion` decodes to those versions. A node JSON with neither field decodes both as `EventId.zero`.
- `document state versions survive a database round trip` in `tests/Server.Tests/DatabaseProjectionContractTests.fs`. `Database.replaceGraphProjectionWithTx` then `Database.tryLoadGraphFromProjection` returns the same `persistedVersion` and `Unpersisted changedVersion`.
- `graph change sets Unpersisted with the newer version` in `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`. The accepted Ev id is `changedVersion` on the nearest owning special node.
- `NodePersisted for an older version leaves the node Unpersisted` in `tests/Server.Tests/CoreLoopNoticeTests.fs`. The handler sets `persistedVersion` to the written version. A written version equal to `changedVersion` sets `Persisted`, including `EventId.zero`. A written version older than `changedVersion` leaves `Unpersisted` at that newer `changedVersion`.

`--filter` names:

- `document state versions survive a JSON round trip`
- `document state versions survive a database round trip`
- `graph change sets Unpersisted with the newer version`
- `NodePersisted for an older version leaves the node Unpersisted`

## 7. Done

Shared and Server compile. The four filters pass. `EventJsonTests` and `PersistThreadTests` pass. A persist that read an earlier `State` leaves the node `Unpersisted` at the later graph change, and sets `persistedVersion` to the version that was written.

## See also

- [Graph change persist version](../arch.md#graph-change-persist-version)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
- [GLOSSARY](../../GLOSSARY.md)
