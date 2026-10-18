# 43 — Expand Node events beside the Set output

**Status:** `coded`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec
**Actual:** 3h

## Context

Upload, an incoming file, a git-changed file, and import still write document state with today's code. `WorkspaceUploadStructure.planStubOps`, `WorkspaceUploadStructure.markNoServerFile` [o] obsolescent, and `WorkspaceUploadStructure.planServerFilePresentOps` stay shared Op-list planners and do not post. `Api.postEvents` runs the upload Change and the incoming-file Change. `LazyLoadReconciliationServer.reconcileReceivedFiles` runs git Load. Those callers post `InMsg.NodeUploading` or `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` beside that write. `ImportText.markDocumentCurrentBeforeParse`, `ImportDocument.planParseFile`, and `DirectoryReconcile.planDirectoryReconcile` stay unchanged and do not post. `planParseFile` and `planDirectoryReconcile` run inside the parse thread, so `ParseThread` `finishParse` posts `InMsg.NodeParsed`. `Api.postEvents` posts `InMsg.NodeParsed` through `CoreMailboxApi.addInMsg` when it runs the import Change. `Api.postParseFile` posts `InMsg.NodeParsed` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState`. The post does not set state. Tests read the mailbox log where `InMsg` cases are logged. [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md) applies `InMsg.NodeUploading` and `InMsg.NodeReceived` in `CoreMailboxLoad.applyInMsg`.

## What to build

`Api.postEvents` and `LazyLoadReconciliationServer.reconcileReceivedFiles` post `InMsg.NodeUploading` and `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState`. The shared planners do not post. The post does not set state. Today's write still sets `NoServerFile` [o] obsolescent, which is PullingFile. `ParseThread` `finishParse`, `Api.postEvents` for the import Change, and `Api.postParseFile` post `InMsg.NodeParsed` beside `Op.SetDocumentState`. Rename `InMsg.ParseFinished` to `InMsg.NodeParsed`.

### 1. Expand

1. [x] **Upload** — `WorkspaceUploadStructure.planStubOps` and `WorkspaceUploadStructure.markNoServerFile` [o] obsolescent stay shared Op-list planners and do not post. `Api.postEvents` runs that Change and posts `InMsg.NodeUploading` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState`. The post does not set state. Today's write still sets NoServerFile.
2. [x] **Received** — `WorkspaceUploadStructure.planServerFilePresentOps` stays a shared Op-list planner and does not post. `Api.postEvents` runs that Change and posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState` for each absent file that function already selects. `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState` for a changed File Node already on the server. The post does not set state. Today's write still sets the state.
3. [x] **Parsed** — `ImportText.markDocumentCurrentBeforeParse`, `ImportDocument.planParseFile`, and `DirectoryReconcile.planDirectoryReconcile` stay unchanged and do not post. `planParseFile` and `planDirectoryReconcile` run inside the parse thread, so `ParseThread` `finishParse` posts `InMsg.NodeParsed`. `Api.postEvents` posts `InMsg.NodeParsed` through `CoreMailboxApi.addInMsg` when it runs the import Change. `Api.postParseFile` posts `InMsg.NodeParsed` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState`. Rename `InMsg.ParseFinished` to `InMsg.NodeParsed`. It is the same case. The same handler sets Parsed.

### Acceptance Criteria

1. [x] **Upload test** — `upload posts NodeUploading beside SetDocumentState`. The test calls `Api.postEvents` with the Change from `WorkspaceUploadStructure.planStubOps` and with the Change that contains `WorkspaceUploadStructure.markNoServerFile` [o] obsolescent `Op.SetDocumentState(nodeId, Current, NoServerFile)` and `Op.SetDocumentState(nodeId, Unparsed, NoServerFile)`. `Api.postEvents` posts `InMsg.NodeUploading` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState`. The planners do not post. The post does not set state. Today's write still sets NoServerFile.
2. [x] **Git test** — `git changed file posts NodeReceived beside SetDocumentState`. The test calls `LazyLoadReconciliationServer.reconcileReceivedFiles`. It posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState`.
3. [x] **Incoming-file test** — `incoming file posts NodeReceived beside SetDocumentState` extends `planServerFilePresentOps transitions only matching absent files`. The test calls `Api.postEvents` with the Change from `WorkspaceUploadStructure.planServerFilePresentOps`. `Api.postEvents` posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState` for those absent files. The planner does not post.
4. [x] **Import test** — `import logs NodeParsed beside SetDocumentState` extends `build import marks unparsed file current before tree operations`. `Api.postEvents` runs the import Change and posts `InMsg.NodeParsed` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState`. The test reads the mailbox log. `ImportText.markDocumentCurrentBeforeParse` stays unchanged and does not post.
5. [x] **Parse-file test** — `planParseFile logs NodeParsed beside SetDocumentState`. `planParseFile` runs inside the parse thread, so `ParseThread` `finishParse` posts `InMsg.NodeParsed`. `Api.postParseFile` posts `InMsg.NodeParsed` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState`. The test reads the mailbox log.
6. [x] **Directory-reconcile test** — `planDirectoryReconcile logs NodeParsed beside SetDocumentState`. `planDirectoryReconcile` runs inside the parse thread, so `ParseThread` `finishParse` posts `InMsg.NodeParsed`. The test reads the mailbox log.
7. [x] **Rename test** — `ParseFinished is NodeParsed`. `InMsg` has `NodeParsed` and no `ParseFinished`. `ParseThread` `finishParse` posts `InMsg.NodeParsed`. The same handler sets Parsed.

## Amendments

- Alan, 2026-10-17: the three Shared planners stay unchanged and do not post. `planParseFile` and `planDirectoryReconcile` run inside the parse thread, so the rename makes `ParseThread` `finishParse` post `InMsg.NodeParsed`. For import and `Api.postParseFile`, the server route that runs them posts `InMsg.NodeParsed` through `CoreMailboxApi.addInMsg` beside today's `Op.SetDocumentState`. Tests read `NodeParsed` from the mailbox log where `InMsg` cases are logged.
- Alan, 2026-10-17: this ticket logs only. `InMsg.NodeUploading` and `InMsg.NodeReceived` handlers are owned by [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md). The renamed `InMsg.NodeParsed` handler keeps setting Parsed.
- The mailbox log is the in-memory `InMsg` list on the mailbox (`MailboxHost.loggedInMsgs`). It is the list the tests read. These posts are not appended to the client Event log. An unknown Event kind fails the client decoder.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## Time

- 2026-10-17 3h — expand Node events beside SetDocumentState (from chat)

## See also

- [Graph change expand](../arch.md#graph-change-expand)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [GLOSSARY](../../GLOSSARY.md)
