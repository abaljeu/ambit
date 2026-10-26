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

- Delete both `Op.SetDocumentState` arms in `markPullingFile`.

Backend inference: an Edit Change with `Op.NewSpecialNode(id, File, _)` infers a trail `NodeUploading id`. The rule goes in `DocumentOpImpact.inferNotices`, and the agents post it after commit (135).

Hard: the stub ops come from `FileNodeOps.planCreateOwnedFile`. That is the same function and the same ops as the user's create-file command in `UpdateFileSearch`. Only the command name (`displayName Load`) differs. The `Current → PullingFile` arm re-marks an existing file with no other op.

Source: `WorkspaceUploadStructure.fs`, `DocumentOpImpact.fs`.

## 5. Tests

[ ] `WorkspaceUploadStructureTests` `markPullingFile sets PullingFile`, `new file stubs start PullingFile; directory parent stays Current`, `reuses existing owned path without duplicating`
[ ] `NodeEventExpandTests` `upload posts NodeUploading beside SetDocumentState`, `upload emits NodeUploading`

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
