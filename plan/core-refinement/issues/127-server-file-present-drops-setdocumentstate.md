# 127 — Server file present drops its SetDocumentState

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

This is round 4 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `WorkspaceUploadStructure.planServerFilePresentOps` returns only `Op.SetDocumentState(fileId, PullingFile, Unparsed)`, after a PUT or an mtime skip. That is #400.

## 4. Change

- Delete the op line. `UpdateWorkspaceSync.markServerFilesPresent` and `WorkspaceCloudUpload.markBodiesPresent` then post nothing.

Backend inference: there are no other ops to infer from. The backend point where the body lands is `WorkspaceWebDav.handlePut`.

Hard: `handlePut` has no core handle and posts no Notice today. An mtime-skipped file reaches the backend through no request at all.

Source: `WorkspaceUploadStructure.fs`, `WorkspaceWebDav.fs`, `RouteRegistration.fs`.

## 5. Tests

[ ] `WorkspaceUploadStructureTests` `planServerFilePresentOps transitions only matching absent files`
[ ] `NodeEventExpandTests` `incoming file posts NodeReceived beside SetDocumentState`, `incoming file emits NodeReceived`

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
