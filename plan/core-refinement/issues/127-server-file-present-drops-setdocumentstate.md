# 127 — Server file present drops its SetDocumentState

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

This is round 4 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `WorkspaceUploadStructure.planServerFilePresentOps` returns only `Op.SetDocumentState(fileId, PullingFile, Unparsed)`, after a PUT or an mtime skip. That is #400.

## 4. Change

- [x] Delete the op line in `WorkspaceUploadStructure.planServerFilePresentOps`. Done in [127 — Server file present drops its SetDocumentState](https://github.com/abaljeu/ambit/pull/420). The function returns no op. `UpdateWorkspaceSync.markServerFilesPresent` and `WorkspaceCloudUpload.markBodiesPresent` then post nothing.

Backend inference: there are no other ops to infer from. The backend point where the body lands is `WorkspaceWebDav.handlePut`.

Hard: `handlePut` has no core handle and posts no Notice today. An mtime-skipped file reaches the backend through no request at all.

Source: `WorkspaceUploadStructure.fs`, `WorkspaceWebDav.fs`, `RouteRegistration.fs`.

## 5. Tests

[ ] `WorkspaceUploadStructureTests` `planServerFilePresentOps transitions only matching absent files`
[ ] `NodeEventExpandTests` `incoming file posts NodeReceived beside SetDocumentState`, `incoming file emits NodeReceived`

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests succeed.

See [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md): §7 row P5 (`planServerFilePresentOps`, `PullingFile → Unparsed`).

## 7. Pending

[ ] Backend inference for this site. See §4.
[ ] `WorkspaceWebDav.handlePut` and the mtime-skip path. See §4.
[ ] Correct `WorkspaceUploadStructureTests` `planServerFilePresentOps transitions only matching absent files`. That fact is disabled.
[ ] Correct `NodeEventExpandTests` `incoming file posts NodeReceived beside SetDocumentState`. That fact is disabled.
[ ] Correct `NodeEventExpandTests` `incoming file emits NodeReceived`. That fact is disabled.
