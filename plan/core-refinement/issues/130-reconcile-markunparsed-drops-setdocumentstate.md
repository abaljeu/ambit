# 130 — Reconcile markUnparsed drops its SetDocumentState

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

## 6. Open question (Alan)

[121 — MarkUnparsed leaves Notice](121-markunparsed-leaves-notice.md) removes `Notice.MarkUnparsed`, which rounds 8 and 11 use. If 121 lands first, which Notice covers `Current → Unparsed`?

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
