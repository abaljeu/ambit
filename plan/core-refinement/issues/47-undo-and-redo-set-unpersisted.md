# 47 — Undo and redo set Unpersisted

**Status:** `defined`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Graph change apply has the Core-loop Node handlers. Undo and redo change a Graph without reading files. Graph change undo makes their appliers mark the nearest owning special node Unpersisted and skip Node messages.

## What to build

Change the server and Browser Undo and Redo appliers. They apply the inverse Ops and mark the nearest owning special node Unpersisted. Redo gets the same Unpersisted mark as Undo. They do not write a version. They do not apply NodeUploading, NodeReceived, NodeParsing, NodeParsed, NodePersisting, or NodePersisted.

### 1. Undo

1. [ ] **Server Undo and Redo applier** — `CoreEventDispatch.completeAction` already fills inverse Ops onto an empty Undo body and an empty Redo body. Change the Core change applier behind `PersistHandlers.applyEvent` so that, after those Ops apply, it marks the nearest owning special node Unpersisted and does not write a version. Redo gets that same mark. It does not apply NodeUploading, NodeReceived, NodeParsing, NodeParsed, NodePersisting, or NodePersisted. [46 — Record the persist version](46-record-the-persist-version.md) leaves that Unpersisted when an in-flight persist finishes with an older version.
2. [ ] **Browser Undo and Redo applier** — Change `SyncLogic.applyLocalUndo` and `SyncLogic.applyLocalRedo`, and the `applyInverse` path, to receive the planned Undo or Redo, apply its inverse Ops through `ResidentProjection`, and mark the nearest owning special node Unpersisted. They do not apply a document-state message from `ClientHistory`.

### Acceptance Criteria

1. [ ] **Undo test** — `undo sets Unpersisted and does not apply Node events`. The graph changes. NodeUploading, NodeReceived, NodeParsing, NodeParsed, NodePersisting, and NodePersisted from the stream are not applied.
2. [ ] **Redo test** — `redo sets Unpersisted and does not apply Node events`. Redo gets the same Unpersisted mark as Undo. It does not write a version.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change undo](../arch.md#graph-change-undo)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [GLOSSARY](../../GLOSSARY.md)
