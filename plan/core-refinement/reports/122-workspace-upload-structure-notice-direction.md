# 122 — Document state moves by Notice — workspace upload structure

Ticket: [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md).

Scope: [122 — Document state moves by Notice — Workspace upload structure](122-workspace-upload-structure-scope.md).

Spec: [Document-state events](../spec.md) §2 item 7 and §3 item 1.

## 1. Direction

`WorkspaceUploadStructure.planStubOps`, `markPullingFile`, and `planServerFilePresentOps` stay shared Op-list planners.

They do not post.

`Api.postEvents` posts the matching Notice through `CoreMailboxApi.postNotice` after the Change is accepted.

## 2. Toward the mailbox

- `src/Shared/WorkspaceUploadStructure.fs` `planStubOps` or `planServerFilePresentOps`
- `src/Client/UpdateWorkspaceSync.fs` `completeUploadInventoryWith` then `applyStructureLocally`, or `markServerFilesPresent` then `applyAndPostSync`
- `src/Shared/dotnet/WorkspaceCloudUpload.fs` `postStubs` or `markBodiesPresent` then `AmbitSession.postOps`
- `src/Server/Api.fs` `postEvents`
- `src/Server/Api.fs` `inferredNodeEvents`
- `src/Shared/DocumentOpImpact.fs` `noticesBeside`
- `src/Server/Core/CoreMailboxApi.fs` `postNotice`

`src/Server/Core/FileAgent.fs` `postGraphOnlyNotices` posts that same list for a graph-only Change.

## 3. Each SetDocumentState

`noticesBeside` is the map. `Ev.documentStateFromNotice` is the enum. `GraphMutate.setDocumentState` ignores the old enum and returns the same graph when the node already holds the new enum.

- Line 143, `markUnparsed`, `Current` to `Unparsed`: `Notice.MarkUnparsed`.
- Line 145, `markUnparsed`, `PullingFile` to `Unparsed`: `Notice.NodeReceived`.
- Line 151, `markPullingFile`, `Current` to `PullingFile`: `Notice.NodeUploading`.
- Line 153, `markPullingFile`, `Unparsed` to `PullingFile`: `Notice.NodeUploading`.
- Line 319, `planServerFilePresentOps`, `PullingFile` to `Unparsed`: `Notice.NodeReceived`.
- Line 160, `markParentCurrent`, `Unparsed` to `Current`: `Notice.NodeParsed` only when the Change has no op whose new state is `PullingFile` or `Unparsed`.

A new Directory stub is created `Unparsed`, so `markUnparsed` returns no op for that create.

`planStubOps` for a file puts `PullingFile` in the same Change, so line 160 does not post `Notice.NodeParsed` on that batch.

Document-state events gives a new Directory or Workspace no new message. The create stays `Op.NewSpecialNode`. `NodeParsed` stays the parse mark.

## 4. Received path

The client applies the logged Notice through `SyncLogic.applyLoggedNotice` and `ResidentProjection.applyDocumentNotice`.

`BootCache.applyLoggedEvent` applies the same enum write.

## 5. Change

Source stays as read.

`Op.SetDocumentState` stays in the planner.

## 6. Tests

Tests stayed unrun. No source file changed.

Section 6 of 122 — Document state moves by Notice requires no new test.

Existing posts:

- `tests/Server.Tests/NodeEventExpandTests.fs` ``upload posts NodeUploading beside SetDocumentState``
- `tests/Server.Tests/NodeEventExpandTests.fs` ``incoming file posts NodeReceived beside SetDocumentState``
