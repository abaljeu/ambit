# 46 — Record the persist version

**Status:** `defined`
**Type:** coding
**Blocked by:** [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md); 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Persist can finish after a later graph change. Then `Notice.NodePersisted` must not mark the node Persisted, because the file lacks that change. This ticket gives the message and the handler one version to compare. The version is that of the graph the persist thread actually wrote: the `eventId` of the state it read on take in [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md).

## What to build

Persist state is Unpersisted, or Persisted at a version. The persist thread reports the version of the graph it wrote. Record the newer version when a graph change sets the node Unpersisted. The Core-loop handler sets the Persisted version only when the written version is that current version. An older written version leaves Unpersisted, including after Undo or Redo.

### 1. Persist version

1. [ ] **Persist thread** — `Notice.NodePersisted` becomes `NodePersisted of nodeId * version`. After a successful write, the persist thread posts the `eventId` of the state whose graph it wrote. The thread still does not edit the graph.
2. [ ] **Graph-change applier** — Change the Core change applier behind `PersistHandlers.applyEvent` to receive the accepted Graph-changing Event id and record it when `GraphMutate` sets the nearest owning Special Node Unpersisted.
3. [ ] **Core-loop version gate** — Change the NodePersisted handler in `CoreMailboxLoad.applyNotice` to receive the node id and written version. It compares that version with the newer version recorded when the node was set Unpersisted. When they match, it sets persist state to that Persisted version. When the written version is older, it leaves Unpersisted.

### Acceptance Criteria

1. [ ] **Version test** — `NodePersisted for an older version leaves the node Unpersisted`. A current version sets the Persisted version. The persist thread does not edit the graph. An Undo or Redo that set Unpersisted stays Unpersisted when an older persist finishes.
2. [ ] **Graph-change test** — `graph change sets Unpersisted with the newer version`. The nearest owning special node is Unpersisted.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change persist version](../arch.md#graph-change-persist-version)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [GLOSSARY](../../GLOSSARY.md)
