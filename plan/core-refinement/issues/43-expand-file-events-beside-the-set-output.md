# 43 — Expand Node events beside the Set output

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec

## Context

Upload, an incoming file, a git-changed file, and import still write document state with today's code. `WorkspaceUploadStructure.planStubOps`, `WorkspaceUploadStructure.markNoServerFile` [o] obsolescent, and `WorkspaceUploadStructure.planServerFilePresentOps` stay shared Op-list planners and do not post. `Api.postEvents` runs the upload Change and the incoming-file Change. `LazyLoadReconciliationServer.reconcileReceivedFiles` runs git Load. Those callers post `InMsg.NodeUploading` or `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` beside that write. Import logs NodeParsed beside that write. The post does not set state. [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md) applies `InMsg.NodeUploading` and `InMsg.NodeReceived` in `CoreMailboxLoad.applyInMsg` and logs them.

## What to build

`Api.postEvents` and `LazyLoadReconciliationServer.reconcileReceivedFiles` post `InMsg.NodeUploading` and `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState`. The shared planners do not post. The post does not set state. Today's write still sets `NoServerFile` [o] obsolescent, which is PullingFile. Log NodeParsed beside `Op.SetDocumentState`. Rename `InMsg.ParseFinished` to `InMsg.NodeParsed`.

### 1. Expand

1. [ ] **Upload** — `WorkspaceUploadStructure.planStubOps` and `WorkspaceUploadStructure.markNoServerFile` [o] obsolescent stay shared Op-list planners and do not post. `Api.postEvents` runs that Change and posts `InMsg.NodeUploading` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState`. The post does not set state. Today's write still sets NoServerFile.
2. [ ] **Received** — `WorkspaceUploadStructure.planServerFilePresentOps` stays a shared Op-list planner and does not post. `Api.postEvents` runs that Change and posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState` for each absent file that function already selects. `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState` for a changed File Node already on the server. The post does not set state. Today's write still sets the state.
3. [ ] **Parsed** — `ImportText.markDocumentCurrentBeforeParse`, `ImportDocument.planParseFile`, and `DirectoryReconcile.planDirectoryReconcile` log NodeParsed beside their `Op.SetDocumentState` when they mark a node Parsed. Rename `InMsg.ParseFinished` to `InMsg.NodeParsed`. It is the same case. The same handler sets Parsed. `ParseThread` `finishParse` posts `InMsg.NodeParsed`.

### Acceptance Criteria

1. [ ] **Upload test** — `upload posts NodeUploading beside SetDocumentState`. The test calls `Api.postEvents` with the Change from `WorkspaceUploadStructure.planStubOps` and with the Change that contains `WorkspaceUploadStructure.markNoServerFile` [o] obsolescent `Op.SetDocumentState(nodeId, Current, NoServerFile)` and `Op.SetDocumentState(nodeId, Unparsed, NoServerFile)`. `Api.postEvents` posts `InMsg.NodeUploading` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState`. The planners do not post. The post does not set state. Today's write still sets NoServerFile.
2. [ ] **Git test** — `git changed file posts NodeReceived beside SetDocumentState`. The test calls `LazyLoadReconciliationServer.reconcileReceivedFiles`. It posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState`.
3. [ ] **Incoming-file test** — `incoming file posts NodeReceived beside SetDocumentState` extends `planServerFilePresentOps transitions only matching absent files`. The test calls `Api.postEvents` with the Change from `WorkspaceUploadStructure.planServerFilePresentOps`. `Api.postEvents` posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState` for those absent files. The planner does not post.
4. [ ] **Import test** — `import logs NodeParsed beside SetDocumentState` extends `build import marks unparsed file current before tree operations`. `ImportText.markDocumentCurrentBeforeParse` puts NodeParsed in the log beside `Op.SetDocumentState`.
5. [ ] **Parse-file test** — `planParseFile logs NodeParsed beside SetDocumentState`. `ImportDocument.planParseFile` puts NodeParsed in the log beside `Op.SetDocumentState` when it marks an Unparsed File Node Parsed.
6. [ ] **Directory-reconcile test** — `planDirectoryReconcile logs NodeParsed beside SetDocumentState`. `DirectoryReconcile.planDirectoryReconcile` puts NodeParsed in the log beside `Op.SetDocumentState` when it marks an Unparsed Directory Node Parsed.
7. [ ] **Rename test** — `ParseFinished is NodeParsed`. `InMsg` has `NodeParsed` and no `ParseFinished`. `ParseThread` `finishParse` posts `InMsg.NodeParsed`. The same handler sets Parsed.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change expand](../arch.md#graph-change-expand)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [GLOSSARY](../../GLOSSARY.md)
