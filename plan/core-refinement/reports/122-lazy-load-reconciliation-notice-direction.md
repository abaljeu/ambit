# 122 — Document state moves by Notice — lazy load reconciliation

Ticket: [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md).

Scope: [122 — Document state moves by Notice — Lazy load reconciliation scope](122-lazy-load-reconciliation-scope.md).

## 1. Direction

[x] `LazyLoadReconciliation.unparsedStateOp` and `LazyLoadReconciliationApply` stay shared Op-list planners.
[x] They do not post.
[x] `LazyLoadReconciliationServer.postReconcileReport` posts the Op list with `CoreDoor.postGraphOnly`.
[x] `FileAgent.postGraphOnlyNotices` posts `DocumentOpImpact.noticesBeside` through the bound `postNotice`.
[x] `LazyLoadReconciliationServer.postParsedFromPulling` posts `Notice.NodeParsed` for a node whose last state op in that list is `PullingFile` to `Current`.

## 2. Toward the mailbox

[x] `src/Shared/dotnet/LazyLoadReconciliation.fs` `containingDirectoryMarkOps`.
[x] `src/Shared/dotnet/LazyLoadReconciliationApply.fs` `markUnparsed`, `markParentCurrent`, and `parseDirInfoIfPresent`.
[x] `src/Shared/dotnet/LazyLoadReconciliationReport.fs` `planChangedPathsWithArtifacts` and `planReceivedFiles`.
[x] `src/Server/LazyLoadReconciliationServer.fs` `postReconcileReport`.
[x] `src/Server/Core/FileAgent.fs` `postGraphOnlyNotices`.
[x] `src/Shared/DocumentOpImpact.fs` `noticesBeside`.
[x] `src/Server/Core/CoreMailboxApi.fs` `postNotice`.

## 3. Each SetDocumentState

`noticesBeside` is the map. `Ev.documentStateFromNotice` is the enum. `GraphMutate.setDocumentState` writes the new enum and returns the same graph when the node already holds it.

[x] `LazyLoadReconciliationApply.markUnparsed` line 23, `Current` to `Unparsed`: `Notice.MarkUnparsed`.
[x] `LazyLoadReconciliationApply.markUnparsed` line 25, `PullingFile` to `Unparsed`: `Notice.NodeReceived`.
[x] `LazyLoadReconciliationApply.markParentCurrent` line 32, `Unparsed` to `Current`: `Notice.NodeParsed` when the Change has no op whose new state is `PullingFile` or `Unparsed`.
[x] `LazyLoadReconciliationApply.parseDirInfoIfPresent` line 145, `Unparsed` to `Current`: `Notice.NodeParsed` on that same gate.
[x] `LazyLoadReconciliationApply.parseDirInfoIfPresent` line 147, `PullingFile` to `Current`: `Notice.NodeParsed` from `postParsedFromPulling` after the post returns Ok.
[x] `LazyLoadReconciliation.unparsedStateOp` line 370, any enum to `Unparsed`: `Notice.MarkUnparsed`, or `Notice.NodeReceived` when the old enum is `PullingFile`.
[x] `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `Notice.NodeReceived` for a git-changed File after that post.

## 4. Received path

[x] `CoreMailboxApi.postNotice` enqueues `CoreQueue.Notice`.
[x] `CoreMailboxLoop.dispatchItem` logs it with `CoreEventDispatch.logNotice`.
[x] `CoreMailboxLoop.applyNotice` matches `Notice.NodeParsed`, `Notice.NodeReceived`, and `Notice.MarkUnparsed`.
[x] `CoreMailboxFilesAxis.applyNodeParsed` writes Parsed when `pullingFile` is clear.
[x] The `PullingFile` to `Current` op clears `pullingFile` before `postParsedFromPulling` runs, so the Notice leaves Parsed in place.
[x] A second `Notice.NodeParsed` on a node that is already Parsed leaves that node in place.

## 5. Change

[x] `Op.SetDocumentState` stays in both planners.
[x] `LazyLoadReconciliationServer.postReconcileReport` posts `Notice.NodeParsed` for the `PullingFile` to `Current` pair.
[x] The two Look at rows in 122 — Document state moves by Notice are checked.

## 6. Tests

[x] Section 6 of 122 — Document state moves by Notice names no new test.
[x] `tests/Server.Tests/NodeEventExpandTests.fs` ``git changed file posts NodeReceived beside SetDocumentState`` waits until the log holds `Notice.NodeReceived` beside `Op.SetDocumentState` to Unparsed.
