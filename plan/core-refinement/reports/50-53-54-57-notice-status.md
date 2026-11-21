# 50 53 54 57 notice status

| Ticket | Prior status | New status | Evidence |
| --- | --- | --- | --- |
| [50 — Upload emits NodeUploading](../issues/50-upload-emits-nodeuploading.md) | defined | coded | `Api.postEvents` posts `Notice.NodeUploading` from `Api.inferredNodeEvents` |
| [53 — Arrival and reconcile emit NodeReceived](../issues/53-arrival-and-reconcile-emit-nodereceived.md) | defined | coded | `Api.inferredNodeEvents` posts `Notice.NodeReceived`; `LazyLoadReconciliationServer.reconcileReceivedFiles` posts it |
| [54 — Parse and import stop the Parsed Set](../issues/54-parse-and-import-stop-the-parsed-set.md) | defined | defined | `ImportText.markDocumentCurrentBeforeParse`, `ImportDocument.planParseFile`, and `DirectoryReconcile.markCurrent` still emit `Op.SetDocumentState` |
| [57 — Upload Actor posts Node notices](../issues/57-upload-actor-posts-document-state.md) | defined | defined | `Api.inferredNodeEvents` still matches `Op.SetDocumentState`; `Api.postEvents` still collects those notices |

## 50 — Upload emits NodeUploading

Status is `coded`.

- **Upload post** is checked.
- `WorkspaceUploadStructure.planStubOps` reaches `WorkspaceUploadStructure.markPullingFile`, which emits `Op.SetDocumentState` to PullingFile.
- `Api.postEvents` collects `Api.inferredNodeEvents` and posts `Notice.NodeUploading` through `CoreMailboxApi.postNotice`.
- `CoreMailboxLoad.writePullingFileUnpersisted` sets PullingFile and Unpersisted.
- The named test `upload emits NodeUploading` is absent. `NodeEventExpandTests.upload posts NodeUploading beside SetDocumentState` calls `Api.postEvents` and asserts `Notice.NodeUploading`.

## 53 — Arrival and reconcile emit NodeReceived

Status is `coded`.

- **Arrival post** is checked.
- `WorkspaceUploadStructure.planServerFilePresentOps` emits `Op.SetDocumentState` from PullingFile to Unparsed.
- `Api.inferredNodeEvents` maps that pair to `Notice.NodeReceived`.
- `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `Notice.NodeReceived` through `CoreMailboxApi.postNotice`.
- `CoreMailboxLoad.writeReceived` sets Unparsed and Persisted. `GraphMutate.setParseState` clears PullingFile.
- These constructors still emit `Op.SetDocumentState`: `WorkspaceUploadStructure.markUnparsed`, `LazyLoadReconciliationApply.markUnparsed`, `LazyLoadReconciliation.unparsedStateOp`.
- The named tests `reconcile emits NodeReceived` and `incoming file emits NodeReceived` are absent. `NodeEventExpandTests` covers the same posts in `git changed file posts NodeReceived beside SetDocumentState` and `incoming file posts NodeReceived beside SetDocumentState`.

## 54 — Parse and import stop the Parsed Set

Status stays `defined`. The Parsed notice is partial.

`ParseThread.bootCore` gives `finishParse`, which posts `Notice.NodeParsed`. `ParseThread.parseFile` calls `DocumentPersistWrite.planParseFile`. `ParseThread.parseDirectory` calls `DirectoryReconcile.planDirectoryReconcile`.

These still emit `Op.SetDocumentState` to Current:

- `ImportText.markDocumentCurrentBeforeParse`
- `ImportDocument.planParseFile`
- `DirectoryReconcile.markCurrent`, used by `DirectoryReconcile.planDirectoryReconcile`
- `LazyLoadReconciliationApply.parseDirInfoIfPresent`
- `LazyLoadReconciliationApply.markParentCurrent`
- `WorkspaceUploadStructure.markParentCurrent`

`ImportText.buildImportChange` and `ImportText.buildDirectoryMergeChange` still place that mark on a client Change. `Api.applyParseFile` still calls `DocumentPersistWrite.planParseFile`, posts with `CoreDoor.postGraphOnly`, and posts `Notice.NodeParsed` itself.

## 57 — Upload Actor posts Node notices

Status stays `defined`.

- `Api.inferredNodeEvents` still reads `Op.SetDocumentState` and builds `Notice.NodeUploading`, `Notice.NodeReceived`, and `Notice.NodeParsed`.
- `Api.postEvents` still collects that list and posts each notice.
- No upload Actor receives `CoreMailboxApi.postNotice`. `WorkspaceCloudUpload.markBodiesPresent` and `UpdateWorkspaceSync.markServerFilesPresent` still post `WorkspaceUploadStructure.planServerFilePresentOps`.
