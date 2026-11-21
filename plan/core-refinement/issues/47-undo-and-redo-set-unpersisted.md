# 47 — Undo and redo set Unpersisted

**Status:** `done`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Graph change apply has the Core-loop Node handlers. Undo and redo change a Graph without reading files. Graph change undo makes them mark the nearest owning special node Unpersisted, the same way a Change does.

## What was built

Undo and Redo apply their inverse Ops and mark the nearest owning special node Unpersisted. Redo gets the same mark as Undo. They set `changedVersion` to their own event id and leave `persistedVersion` as it was. The mailbox handler pushes a persist request for those owning special nodes through the same code path as a Change. Built in PR #383 (staging `8f038a6f`).

### 1. Undo and Redo

1. [x] **Server Undo and Redo applier** — `CoreEventDispatch.completeAction` fills inverse Ops onto an empty Undo body and an empty Redo body. `ChangeAmendment.applyPosted` applies those Ops and marks the nearest owning special node Unpersisted at the posted Undo or Redo event id, the same way it marks a Change. `persistedVersion` stays. `CoreEventDispatch.pushChangeSpecials` matches Change, Undo and Redo in one arm, so all three push a persist request the same way. The persist gate from [100 — Core loop gates Persisted on the written version](100-core-loop-gates-persisted-on-the-written-version.md) leaves the node Unpersisted when an in-flight persist finishes with an older version.
2. [x] **Browser Undo and Redo applier** — `SyncLogic.applyLocalUndo` and `SyncLogic.applyLocalRedo` go through `applyInverse`, which applies the inverse Ops through `ResidentProjection` and marks the nearest owning special node Unpersisted at the event id, leaving `persistedVersion`. The local draft id is `EventId.zero`. The ack raises `changedVersion` to the stored id. The logged fold marks at the stored id.

### Acceptance Criteria

1. [x] **Undo test** — `Undo sets changedVersion to the Undo event and leaves persistedVersion` (`PersistApplyTests`). The graph changes, the node is Unpersisted, `changedVersion` is the Undo event id and `persistedVersion` stays.
2. [x] **Redo test** — `Redo sets changedVersion to the Redo event and leaves persistedVersion` (`PersistApplyTests`). Redo gets the same mark as Undo at the Redo event id.
3. [x] **Undo persist test** — `Undo pushes the document root and the persist thread writes the undone graph` (`PersistApplyTests`). The persist thread writes the file after Undo.
4. [x] **Browser ack test** — `Undo ACK sets changedVersion to the Undo event id` (`AckReconcileTests`).

## See also

- [Graph change undo](../arch.md#graph-change-undo)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [100 — Core loop gates Persisted on the written version](100-core-loop-gates-persisted-on-the-written-version.md)
- [GLOSSARY](../../GLOSSARY.md)
