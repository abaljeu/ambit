# 128 — Upload stub drops its SetDocumentState

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

This is round 5 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `WorkspaceUploadStructure.markPullingFile` writes `Unparsed → PullingFile`, or `Current → PullingFile`, for each new upload stub. `NodeUploading` is read back from that op. That is #402.

After a rollback to `47a85e1d^`: `WorkspaceUploadStructure.markUnparsed` has its round 6 op again (a4f7af01).

## 4. Change

[x] Delete both `Op.SetDocumentState` arms in `markPullingFile`. [128/129: delete upload stub and parent SetDocumentState](https://github.com/abaljeu/ambit/pull/419).

[x] Remove the do-nothing `markPullingFile`, `markUnparsed`, and the state fold in `markNewStubsUnparsed`. `foldStateOps` and `createdStubIds` left with that fold.

[x] Backend inference is dropped per Alan. The desktop should not figure out nodes for uploaded files.

[ ] Correct each disabled test named in §5.

The stub ops come from `FileNodeOps.planCreateOwnedFile`. That is the same function and the same ops as the user's create-file command in `UpdateFileSearch`. Only the command name (`displayName Load`) differs.

`planServerFilePresentOps transitions only matching absent files` stays as on staging. [127 — Server file present drops its SetDocumentState](127-server-file-present-drops-setdocumentstate.md) disables it.

Source: `WorkspaceUploadStructure.fs`, `DocumentOpImpact.fs`.

## 5. Tests

[x] Delete `WorkspaceUploadStructureTests` `markPullingFile sets PullingFile`. It only exercised the removed mark.
[x] Delete `WorkspaceUploadStructureTests` `new file stubs start PullingFile; directory parent stays Current`. It only exercised the removed marks.
[ ] Correct `WorkspaceUploadStructureTests` `reuses existing owned path without duplicating` (disabled, 128).
[ ] Correct `NodeEventExpandTests` `upload posts NodeUploading beside SetDocumentState` (disabled, 128/129).
[ ] Correct `NodeEventExpandTests` `upload emits NodeUploading` (disabled, 128/129).
[ ] Correct `DocumentPersistenceTests` `upload structure persistence preserves existing file mtime` (disabled, 128).

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests in this round are green.
