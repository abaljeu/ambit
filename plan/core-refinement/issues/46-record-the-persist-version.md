# 46 — Record the persist version

**Status:** `defined`
**Type:** coding
**Blocked by:** [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md); 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Persist can finish after a later graph change. Then `Notice.NodePersisted` must not mark the node Persisted, because the file lacks that change. This ticket gives the message and the handler one version to compare. The version is the `eventId` of the `State` the persist thread reads through `currentState` and writes.

A special node's `DocumentState` holds `persistedVersion`, the version its file was last written at. Its persist state is Persisted, or Unpersisted at the version of the graph change that needs writing. The persist thread reports the version of the graph it wrote, and the Core-loop handler always sets `persistedVersion` to it. The node becomes Persisted when `persistedVersion` is not older than its Unpersisted version. Otherwise it stays Unpersisted, including after Undo or Redo.

## What to build

Do these steps in order. Finish each step before starting the next.

1. [ ] **Move EventId** — Move the `EventId` type and its `EventId` module from `src/Shared/History.fs`, unchanged, into a new file `src/Shared/EventId.fs` in `namespace Gambol.Shared`, listed in `src/Shared/Gambol.Shared.fsproj` just before `Model.fs`. Files: `src/Shared/History.fs`, `src/Shared/EventId.fs` (new), `src/Shared/Gambol.Shared.fsproj`. Done when Shared and Server compile with no other change.
2. [ ] **Versions on document state** — Add `persistedVersion: EventId` to `DocumentState`, and make `PersistState` `Persisted | Unpersisted of changedVersion: EventId`. Update every construction and match of them. JSON writes and reads `persistedVersion` and `changedVersion` beside the persist word with `EventId.toJson` and `EventId.fromJson`. The database stores them in new `persisted_version` and `changed_version` columns, `INTEGER NOT NULL DEFAULT 0`, on the node row, writes them with `persist_state`, and Start reads them back. A missing field decodes as `EventId.zero`. Files: `src/Shared/Model.fs` (where `PersistState` is defined), `src/Server/Database.fs`, and each file that `rg -l "PersistState\." src tests` lists: `src/Shared/GraphMutate.fs`, `src/Shared/GraphProjection.fs`, `src/Shared/ChangeAmendment.fs`, `src/Shared/History.fs`, `src/Shared/Serialization.fs`, `src/Server/DocumentPersistPath.fs`, `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/FileAgent.fs`, `src/Server/Core/DbAgent.fs`, and the tests it lists under `tests/Server.Tests/` and `tests/Shared.Tests/`. Add `document state versions survive a database round trip` and `document state versions survive a JSON round trip`. Done when Server and Shared compile and both tests pass.
3. [ ] **Persist thread** — Make `Notice.NodePersisted` `NodePersisted of nodeId: NodeId * persistedVersion: EventId * updateTime: System.DateTime`. After a successful write the persist thread posts the `eventId` of the state whose graph it wrote; it still does not edit the graph. `EventJson` writes and reads `persistedVersion` with `EventId.toJson` and `EventId.fromJson`, and a stored entry without one decodes as `EventId.zero`. Files: `src/Shared/History.fs` (where `Notice` is defined), `src/Shared/EventJson.fs`, `src/Shared/SyncLogic.fs`, `src/Server/Core/PersistThread.fs`, `src/Server/Database.fs`, `src/Server/Core/CoreMailboxLoad.fs`, and each test that `rg -l "NodePersisted" tests` lists. Done when it compiles and `EventJsonTests` and `PersistThreadTests` pass.
4. [ ] **Graph-change applier** — The Core change applier behind `PersistHandlers.applyEvent` passes the accepted Graph-changing `Ev`'s id to `GraphMutate`, and `markOwningSpecialUnpersisted` records it as `Unpersisted changedVersion` on the nearest owning Special Node. Add `graph change sets Unpersisted with the newer version`. Files: `src/Shared/GraphMutate.fs`, `src/Shared/GraphOps.fs`, `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` (their `processPostEvents`), `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`. Done when `graph change sets Unpersisted with the newer version` passes.
5. [ ] **Core-loop version gate** — The `Notice.NodePersisted` arm of `CoreMailboxLoad.applyNotice` takes the node id, the written `persistedVersion` and `updateTime`. It always sets the node's `persistedVersion` to the written version and applies `updateTime`. It sets `Persisted` when the written version is not older than the node's `changedVersion`, and otherwise leaves the node Unpersisted. Add `NodePersisted for an older version leaves the node Unpersisted`. Files: `src/Server/Core/CoreMailboxLoad.fs`, `tests/Server.Tests/CoreLoopNoticeTests.fs`. Done when `NodePersisted for an older version leaves the node Unpersisted` and `graph change sets Unpersisted with the newer version` pass.

### Acceptance Criteria

1. [ ] **Version test** — `NodePersisted for an older version leaves the node Unpersisted`. Every NodePersisted sets `persistedVersion` to the written version. A written version not older than `changedVersion` sets Persisted, including for a node decoded at `EventId.zero`. The persist thread does not edit the graph. An Undo or Redo that set Unpersisted stays Unpersisted when an older persist finishes.
2. [ ] **Graph-change test** — `graph change sets Unpersisted with the newer version`. The nearest owning special node is Unpersisted.

## See also

- [Graph change persist version](../arch.md#graph-change-persist-version)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [GLOSSARY](../../GLOSSARY.md)
