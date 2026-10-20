# 46 — Record the persist version

**Status:** `defined`
**Type:** coding
**Blocked by:** 51 — Stand the persist stack; 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

[51 — Stand the persist stack](51-stand-the-persist-stack.md) already has the persist thread post `Notice.NodePersisted` and skip the persist-state write. Graph change apply already gives the Core loop that handler. Persist can finish after a later graph change. Graph change persist version gives that message and the handler one version to compare. [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md) carries the stamp effect and the persist message on Notices. This ticket keeps the `nodeId * version` pair and the version gate.

## What to build

Persist state is Unpersisted, or Persisted at a version. Carry the launch version from the persist collector through `PersistThread` to the Core loop. Record the newer version when a graph change sets the node Unpersisted. The Core-loop handler sets the Persisted version only when the launch version is that current version. An older launch version leaves Unpersisted, including after Undo or Redo.

### 1. Persist version

1. [ ] **Persist collectors** — Add a launch-version field to `PersistSubmit`. `DbAgent.enqueueSnapshot`, `DbAgent.submitLiveOps`, and `FileAgent.collectPersist` copy `loaded.state.Value.eventId` onto that field when they build the job.
2. [ ] **Persist thread** — Change the `Notice.NodePersisted` post from [51 — Stand the persist stack](51-stand-the-persist-stack.md). `PersistThread.runOne` and its success callback receive the launch version from the persist job and post `Notice.NodePersisted(nodeId, version)`. The thread still does not edit the graph.
3. [ ] **Graph-change applier** — Change the Core change applier behind `PersistHandlers.applyEvent` to receive the accepted Graph-changing Event id and record it when `GraphMutate` sets the nearest owning Special Node Unpersisted.
4. [ ] **Core-loop version gate** — Change the NodePersisted handler in `CoreMailboxLoad.applyNotice` to receive the node id and launch version. It compares that version with the newer version recorded when the node was set Unpersisted. When they match, it sets persist state to that Persisted version. When the launch version is older, it leaves Unpersisted.

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
- [69 — Persist an Edit without waiting](69-persist-an-edit-without-waiting.md)
- [GLOSSARY](../../GLOSSARY.md)
