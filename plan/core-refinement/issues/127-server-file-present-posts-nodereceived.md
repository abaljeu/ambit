# 127 — Server file present posts NodeReceived

## 1. Type

coding

## 2. Blockers

[ ] [134 — The client door forwards state Notices](134-client-door-forwards-state-notices.md)

## 3. Problem

This is round 4 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `WorkspaceUploadStructure.planServerFilePresentOps` returns only `Op.SetDocumentState(fileId, PullingFile, Unparsed)`. `NodeReceived` is inferred from that op. Without the op, the list is empty, and the callers skip the post (#400).

## 4. Change

- `planServerFilePresentOps` returns `EventBody list`, with one **trail** `Notice.NodeReceived fileId` per confirmed PullingFile. Placement is the rule in [135 — Op readers leave](135-op-readers-leave.md).
- `UpdateWorkspaceSync.markServerFilesPresent` and `WorkspaceCloudUpload.markBodiesPresent` send the list. They send no Change when there are no ops.
- Delete the `Op.SetDocumentState` line.

Source: `WorkspaceUploadStructure.fs`, `UpdateWorkspaceSync.fs`, `WorkspaceCloudUpload.fs`, `AmbitSession.fs` (`postOps`).

## 5. Tests

[ ] `WorkspaceUploadStructureTests` `planServerFilePresentOps transitions only matching absent files`
[ ] `NodeEventExpandTests` `incoming file posts NodeReceived beside SetDocumentState`, `incoming file emits NodeReceived`

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
