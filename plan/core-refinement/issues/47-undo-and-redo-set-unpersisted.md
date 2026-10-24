# 47 — Undo and redo set Unpersisted

**Status:** `defined`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Graph change apply has the Core-loop Node handlers. Undo and redo change a Graph without reading files. Graph change undo makes their appliers mark the nearest owning special node Unpersisted and skip Node messages.

## What to build

Change the server and Browser Undo and Redo appliers. They apply the inverse Ops and mark the nearest owning special node Unpersisted. Redo gets the same Unpersisted mark as Undo. They set `changedVersion` to their own event id and leave `persistedVersion`. The mailbox handler pushes a persist request for those owning special nodes the same way a Change does. They do not apply NodeUploading, NodeReceived, NodeParsing, NodeParsed, NodePersisting, or NodePersisted.

### 1. Undo

1. [ ] **Server Undo and Redo applier** — `CoreEventDispatch.completeAction` already fills inverse Ops onto an empty Undo body and an empty Redo body. Change the Core change applier behind `PersistHandlers.applyEvent` so that, after those Ops apply, it marks the nearest owning special node Unpersisted with `changedVersion` set to the Undo or Redo event id and leaves `persistedVersion`. Redo gets that same mark. `CoreEventDispatch.pushChangeSpecials` pushes a persist request for those nodes the same way a Change does. It does not apply NodeUploading, NodeReceived, NodeParsing, NodeParsed, NodePersisting, or NodePersisted. [46 — Record the persist version](46-record-the-persist-version.md) leaves that Unpersisted when an in-flight persist finishes with an older version.
2. [ ] **Browser Undo and Redo applier** — Change `SyncLogic.applyLocalUndo` and `SyncLogic.applyLocalRedo`, and the `applyInverse` path, to receive the planned Undo or Redo, apply its inverse Ops through `ResidentProjection`, and mark the nearest owning special node Unpersisted at that event id, leaving `persistedVersion`. A draft id is `EventId.zero`. The ack raises `changedVersion` to the stored id. The logged fold already marks at the stored id. They do not apply a document-state message from `ClientHistory`.

### Acceptance Criteria

1. [ ] **Undo test** — `undo sets Unpersisted and does not apply Node events`. The graph changes. `changedVersion` is the Undo event id and `persistedVersion` stays. NodeUploading, NodeReceived, NodeParsing, NodeParsed, NodePersisting, and NodePersisted from the stream are not applied.
2. [ ] **Redo test** — `redo sets Unpersisted and does not apply Node events`. Redo gets the same Unpersisted mark as Undo. It sets `changedVersion` to the Redo event id and leaves `persistedVersion`.

## See also

- [Graph change undo](../arch.md#graph-change-undo)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [GLOSSARY](../../GLOSSARY.md)
