# 50 — Upload emits NodeUploading

**Status:** `defined`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Graph change apply has an `InMsg.NodeUploading` handler in `CoreMailboxLoad.applyInMsg`. The handler applies `InMsg.NodeUploading` and logs it. Tests call that handler. `WorkspaceUploadStructure.planStubOps` and `WorkspaceUploadStructure.markNoServerFile` [o] obsolescent stay shared Op-list planners and do not post. They still emit `Op.SetDocumentState` to NoServerFile. `Api.postEvents` runs that Change.

## What to build

`Api.postEvents` posts `InMsg.NodeUploading` through `CoreMailboxApi.addInMsg` and stops the PullingFile write. The planners do not post. `Op.NewSpecialNode` still inserts the node.

### 1. Upload

1. [ ] **Upload caller** — Change `Api.postEvents` to post `InMsg.NodeUploading` through `CoreMailboxApi.addInMsg` when it runs the Change from `WorkspaceUploadStructure.planStubOps` and when it runs the Change that contains `WorkspaceUploadStructure.markNoServerFile` [o] obsolescent `Op.SetDocumentState(nodeId, Current, NoServerFile)` and `Op.SetDocumentState(nodeId, Unparsed, NoServerFile)`. `Api.postEvents` stops those Set ops. Those functions stay shared Op-list planners and do not post. `CoreMailboxLoad.applyInMsg` applies `InMsg.NodeUploading` and logs it. `Op.NewSpecialNode` still inserts the node.

### Acceptance Criteria

1. [ ] **Upload test** — `upload emits NodeUploading`. The test calls `Api.postEvents` with the Change from `WorkspaceUploadStructure.planStubOps` and with the Change that contains `WorkspaceUploadStructure.markNoServerFile` [o] obsolescent `Op.SetDocumentState(nodeId, Current, NoServerFile)` and `Op.SetDocumentState(nodeId, Unparsed, NoServerFile)`. `Api.postEvents` posts `InMsg.NodeUploading` through `CoreMailboxApi.addInMsg` and stops the PullingFile write. The planners do not post. The `InMsg.NodeUploading` handler sets PullingFile and Unpersisted.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change upload](../arch.md#graph-change-upload)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [GLOSSARY](../../GLOSSARY.md)
