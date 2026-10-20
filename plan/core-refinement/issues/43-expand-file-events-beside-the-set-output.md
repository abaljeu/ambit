# 43 — Expand Node events beside the Set output

**Status:** `done`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec
**Actual:** 3h

## Context

Upload, an incoming file, a git-changed file, and import still set document state with `Op.SetDocumentState`.

Those writes sit in these places:

- **Upload and incoming file** — `WorkspaceUploadStructure` plans the Ops. `Api.postEvents` runs them.
- **Git-changed file** — `LazyLoadReconciliationServer.reconcileReceivedFiles` runs the Load.
- **Import and parse** — `ImportText`, `ImportDocument`, and `DirectoryReconcile` plan the Ops. `planParseFile` and `planDirectoryReconcile` run inside the parse thread.

`Notice.ParseFinished` sets Parsed. 44 — Apply document state from Node events applies `Notice.NodeUploading` and `Notice.NodeReceived`.

## What to build

1. [x] **Log** — The caller posts through `CoreMailboxApi.postNotice` beside the existing `Op.SetDocumentState`. The post does not add a state write. `Notice.NodeParsed` still sets Parsed. `Notice.NodeUploading` and `Notice.NodeReceived` do not set state. The post is stored on `MailboxHost.loggedNotices`. It is not appended to the client Event log.
2. [x] **Upload** — `Api.postEvents` posts `Notice.NodeUploading` when the Change sets PullingFile. `WorkspaceUploadStructure.planStubOps` and `WorkspaceUploadStructure.markPullingFile` do not post.
3. [x] **Incoming file** — `Api.postEvents` posts `Notice.NodeReceived` for each absent file `WorkspaceUploadStructure.planServerFilePresentOps` already selects. That planner does not post.
4. [x] **Git-changed file** — `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `Notice.NodeReceived` for a changed File Node already on the server.
5. [x] **Import** — `Api.postEvents` posts `Notice.NodeParsed` when it runs the import Change. `ImportText.markDocumentCurrentBeforeParse` does not post.
6. [x] **Parse thread** — `ParseThread` `finishParse` posts `Notice.NodeParsed`. `ImportDocument.planParseFile` and `DirectoryReconcile.planDirectoryReconcile` do not post.
7. [x] **Parse file** — `Api.postParseFile` posts `Notice.NodeParsed`.
8. [x] **Rename** — `Notice.ParseFinished` is `Notice.NodeParsed`.

### Acceptance Criteria

1. [x] **Upload test** — `upload posts NodeUploading beside SetDocumentState`. The mailbox log has `Notice.NodeUploading`.
2. [x] **Git test** — `git changed file posts NodeReceived beside SetDocumentState`. The mailbox log has `Notice.NodeReceived`.
3. [x] **Incoming-file test** — `incoming file posts NodeReceived beside SetDocumentState`. The mailbox log has `Notice.NodeReceived` for the absent file and not for the other file.
4. [x] **Import test** — `import logs NodeParsed beside SetDocumentState`. The mailbox log has `Notice.NodeParsed`.
5. [x] **Parse-file test** — `planParseFile logs NodeParsed beside SetDocumentState`. The mailbox log has `Notice.NodeParsed`.
6. [x] **Directory-reconcile test** — `planDirectoryReconcile logs NodeParsed beside SetDocumentState`. The mailbox log has `Notice.NodeParsed`.
7. [x] **Rename test** — `ParseFinished is NodeParsed`. `Notice` has `NodeParsed` and no `ParseFinished`.

## Amendments

- Alan, 2026-10-17: the choices made while coding are the What to build items.

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
