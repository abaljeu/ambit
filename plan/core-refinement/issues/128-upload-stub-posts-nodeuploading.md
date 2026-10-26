# 128 — Upload stub posts NodeUploading

## 1. Type

coding

## 2. Blockers

[ ] [134 — The client door forwards state Notices](134-client-door-forwards-state-notices.md)

## 3. Problem

This is round 5 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). The only source of `NodeUploading` is `WorkspaceUploadStructure.markPullingFile`'s `Op.SetDocumentState(_, _, PullingFile)`, read back by `noticesBeside`. Without the op, a new stub stays Unparsed (#402).

## 4. Change

- `markPullingFile` emits `Notice.NodeUploading fileId` as a **trail** Notice. The stub does not exist before its `NewSpecialNode`. Placement is the rule in [135 — Op readers leave](135-op-readers-leave.md).
- `planStubOps` returns `EventBody list`, unless 129 already made that change, and `foldStateOps` applies each body with `Ev.apply`.
- `UpdateWorkspaceSync.completeUploadInventoryWith` and `WorkspaceCloudUpload.postStubs` send the list.
- Delete both `Op.SetDocumentState` arms.

Source: `WorkspaceUploadStructure.fs`, `UpdateWorkspaceSync.fs`, `WorkspaceCloudUpload.fs`, `AmbitSession.fs`.

## 5. Tests

[ ] `WorkspaceUploadStructureTests` `markPullingFile sets PullingFile`, `new file stubs start PullingFile`, `reuses existing owned path without duplicating`
[ ] `NodeEventExpandTests` `upload posts NodeUploading beside SetDocumentState`, `upload emits NodeUploading`

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
