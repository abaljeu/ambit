# 46 — Record the persist version

**Status:** `defined`
**Type:** coding
**Blocked by:** [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md); 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Persist can finish after a later graph change. Then `Notice.NodePersisted` must not mark the node Persisted, because the file lacks that change. This ticket gives the message and the handler one version to compare. The version is that of the graph the persist thread actually wrote: the `eventId` of the state it read on take in [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md).

Persist state is Unpersisted, or Persisted at a version. The persist thread reports the version of the graph it wrote. Record the newer version when a graph change sets the node Unpersisted. The Core-loop handler sets the Persisted version only when the written version is that current version. An older written version leaves Unpersisted, including after Undo or Redo.

## What to build

Do these steps in order. Finish each step before starting the next.

1. [ ] **Move EventId** — Move the `EventId` type and its `EventId` module from `src/Shared/History.fs`, unchanged, into a new file `src/Shared/EventId.fs` in `namespace Gambol.Shared`, listed in `src/Shared/Gambol.Shared.fsproj` just before `Model.fs`. Files: `src/Shared/History.fs`, `src/Shared/EventId.fs` (new), `src/Shared/Gambol.Shared.fsproj`. Done when Shared and Server compile with no other change.
2. [ ] **Version on persist state** — Make `PersistState` `Persisted of version: EventId | Unpersisted of version: EventId`, and update every construction and match of it. JSON and database encoding keep writing and reading the persist word only; a decoded node gets version `EventId.zero`. Files: `src/Shared/Model.fs` (where `PersistState` is defined), and each file that `rg -l "PersistState\." src tests` lists: `src/Shared/GraphMutate.fs`, `src/Shared/GraphProjection.fs`, `src/Shared/ChangeAmendment.fs`, `src/Shared/History.fs`, `src/Shared/Serialization.fs`, `src/Server/DocumentPersistPath.fs`, `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/FileAgent.fs`, `src/Server/Core/DbAgent.fs`, and the tests it lists under `tests/Server.Tests/` and `tests/Shared.Tests/`. Done when Server and Shared compile and `scripts/agent-test.sh` passes for the edited files.
3. [ ] **Persist thread** — Make `Notice.NodePersisted` `NodePersisted of nodeId: NodeId * version: EventId`. After a successful write the persist thread posts the `eventId` of the state whose graph it wrote; it still does not edit the graph. `EventJson` writes and reads the version with `EventId.toJson` and `EventId.fromJson`, and a stored entry without one decodes as `EventId.zero`. Files: `src/Shared/History.fs` (where `Notice` is defined), `src/Shared/EventJson.fs`, `src/Server/Core/PersistThread.fs`, `src/Server/Database.fs`, `src/Server/Core/CoreMailboxLoad.fs`, and each test that `rg -l "NodePersisted" tests` lists. Done when it compiles and `EventJsonTests` and `PersistThreadTests` pass.
4. [ ] **Graph-change applier** — The Core change applier behind `PersistHandlers.applyEvent` passes the accepted Graph-changing `Ev`'s id to `GraphMutate`, and `markOwningSpecialUnpersisted` records it as `Unpersisted version` on the nearest owning Special Node. Add `graph change sets Unpersisted with the newer version`. Files: `src/Shared/GraphMutate.fs`, `src/Shared/GraphOps.fs`, `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` (their `processPostEvents`), `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`. Done when `graph change sets Unpersisted with the newer version` passes.
5. [ ] **Core-loop version gate** — The `Notice.NodePersisted` arm of `CoreMailboxLoad.applyNotice` takes the node id and written version, sets `Persisted version` when the node is `Unpersisted` at that same version, and otherwise leaves it Unpersisted. Add `NodePersisted for an older version leaves the node Unpersisted`. Files: `src/Server/Core/CoreMailboxLoad.fs`, `tests/Server.Tests/CoreLoopNoticeTests.fs`. Done when `NodePersisted for an older version leaves the node Unpersisted` and `graph change sets Unpersisted with the newer version` pass.

### Acceptance Criteria

1. [ ] **Version test** — `NodePersisted for an older version leaves the node Unpersisted`. A current version sets the Persisted version. The persist thread does not edit the graph. An Undo or Redo that set Unpersisted stays Unpersisted when an older persist finishes.
2. [ ] **Graph-change test** — `graph change sets Unpersisted with the newer version`. The nearest owning special node is Unpersisted.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.

## See also

- [Graph change persist version](../arch.md#graph-change-persist-version)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [GLOSSARY](../../GLOSSARY.md)
