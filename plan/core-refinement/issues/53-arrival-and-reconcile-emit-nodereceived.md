# 53 — Arrival and reconcile emit NodeReceived

**Status:** `defined`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Graph change apply has an `InMsg.NodeReceived` handler in `CoreMailboxLoad.applyInMsg`. The handler applies `InMsg.NodeReceived` and logs it. Tests call that handler. `WorkspaceUploadStructure.planServerFilePresentOps` stays a shared Op-list planner and does not post. Arrival still emits `Op.SetDocumentState(fileId, NoServerFile, Unparsed)`. `Api.postEvents` runs that Change. Git Load still emits `LazyLoadReconciliationApply.markUnparsed`'s `Op.SetDocumentState` from Current or NoServerFile to Unparsed. `LazyLoadReconciliationServer.reconcileReceivedFiles` runs that write. A second Load repairs a file that never arrived.

## What to build

`Api.postEvents` and `LazyLoadReconciliationServer.reconcileReceivedFiles` post `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` and stop those writes. `WorkspaceUploadStructure.planServerFilePresentOps` does not post.

### 1. Arrival

1. [ ] **Arrival caller** — Change `Api.postEvents` to post `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` when it runs the Change from `WorkspaceUploadStructure.planServerFilePresentOps` for each absent file that function already selects, and stop `Op.SetDocumentState(fileId, NoServerFile, Unparsed)`. That function stays a shared Op-list planner and does not post. `CoreMailboxLoad.applyInMsg` applies `InMsg.NodeReceived` and logs it.
2. [ ] **Reconcile caller** — Change `LazyLoadReconciliationServer.reconcileReceivedFiles` to post `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` for a changed File Node already on the server. It stops `LazyLoadReconciliationApply.markUnparsed`'s `Op.SetDocumentState` from Current or NoServerFile to Unparsed. That write sets Unparsed and clears PullingFile. `CoreMailboxLoad.applyInMsg` applies `InMsg.NodeReceived` and logs it.

### Acceptance Criteria

1. [ ] **Arrival test** — `incoming file emits NodeReceived`. The test calls `Api.postEvents` with the Change from `WorkspaceUploadStructure.planServerFilePresentOps`. `Api.postEvents` posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` and stops `Op.SetDocumentState(fileId, NoServerFile, Unparsed)`. The planner does not post. The `InMsg.NodeReceived` handler sets Unparsed and clears PullingFile.
2. [ ] **Reconcile test** — `reconcile emits NodeReceived`. The test calls `LazyLoadReconciliationServer.reconcileReceivedFiles`. It posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` and stops `LazyLoadReconciliationApply.markUnparsed`'s `Op.SetDocumentState` for a changed File Node. The `InMsg.NodeReceived` handler sets Unparsed and clears PullingFile.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change arrival](../arch.md#graph-change-arrival)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [GLOSSARY](../../GLOSSARY.md)
