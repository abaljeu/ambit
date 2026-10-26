# 130 — Reconcile markUnparsed drops its SetDocumentState

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

This is round 8 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `LazyLoadReconciliationApply.markUnparsed` writes `Current → Unparsed` and `PullingFile → Unparsed` for three callers: `invalidateOrParseModified`, `markAddedDocumentsUnparsed`, and `renamedFileUnparsedOps`. `receivedFileIds` reads the op for a rename (`planMarksUnparsed`). That is #403.

## 4. Change

- Delete both arms in `markUnparsed`.
- `receivedFileIds` takes a renamed File from the old path's File.

Backend inference: a rename (`Replace` that moves a File) infers a trail `MarkUnparsed`, through `DocumentOpImpact.inferNotices` (135). For a modified file, `LazyLoadReconciliationServer.reconcileReceivedFiles` already posts `NodeReceived` from the changed-path list.

Hard: a modified file's plan is only this op, so the Change has no other op. Directory reconcile (`reconcileChangedPathsWithDiscovery`, `beside = ignore`) has no changed-path post. The `PullingFile → Unparsed` stub case has no other op either.

Source: `LazyLoadReconciliationApply.fs`, `LazyLoadReconciliationServer.fs`, `DocumentOpImpact.fs`.

## 5. Tests

[ ] `LazyLoadReconciliationTests`: `modified file becomes unparsed without losing identity or placement` and the three other facts #403 found
[ ] `LazyLoadReconciliationServerTests` `git pull marks received files at any depth and does not discover`
[ ] `NodeEventExpandTests`: `git changed file posts NodeReceived beside SetDocumentState`, `reconcile emits NodeReceived`, and the renamed and moved facts

## 6. Decision (Alan)

`Notice.NodeUnparsed` covers `Current → Unparsed`. [121 — MarkUnparsed renamed NodeUnparsed](121-markunparsed-renamed-nodeunparsed.md) renames `Notice.MarkUnparsed` to that case. Rounds 8 and 11 use it.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.

## 8. Round

[ ] `LazyLoadReconciliationServer.reconcileReceivedFiles` builds `Notice.NodeUnparsed` for each plan op `SetDocumentState` from Current to Unparsed and posts that Notice on the core mailbox.
[ ] `ReceivedFilesPosted.notices` is that post. `git load reconcile posts NodeUnparsed for a modified file` asserts one `Notice.NodeUnparsed` for the modified file.
[ ] `DocumentOpImpact.noticesBeside` posts one further `Notice.NodeUnparsed` for the same op when the graph-only Change commits.
[ ] For one modified Current file, the event log holds 2 `Notice.NodeUnparsed` rows: one from `reconcileReceivedFiles`, one from `noticesBeside`.
[ ] `LazyLoadReconciliationApply.markUnparsed` writes the `SetDocumentState` op.
