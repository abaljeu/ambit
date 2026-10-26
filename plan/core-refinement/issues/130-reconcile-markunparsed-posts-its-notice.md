# 130 — Reconcile markUnparsed posts its Notice

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

This is round 8 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `LazyLoadReconciliationApply.markUnparsed` is the only writer of Unparsed for three callers: `invalidateOrParseModified`, `markAddedDocumentsUnparsed`, and `renamedFileUnparsedOps`. Its Notice is read from the op. Directory reconcile (`beside = ignore`) has no other post. `receivedFileIds` selects a renamed File by reading the op (`planMarksUnparsed`). That is #403.

## 4. Change

- `markUnparsed` emits a **trail** Notice: `MarkUnparsed` from Current and `NodeReceived` from PullingFile. Placement is the rule in [135 — Op readers leave](135-op-readers-leave.md).
- `Report.ops` becomes `EventBody list`, unless a sibling ticket (131–133) already made that change. `foldDocumentStateOps` and `applyOps` apply each body with `Ev.apply`.
- `postReconcileReport` posts in order. It posts the Notices even when there are no ops.
- `receivedFileIds` takes a renamed File from the old path's File.
- Delete both `Op.SetDocumentState` arms.

Source: `LazyLoadReconciliationApply.fs`, `LazyLoadReconciliation.fs`, `LazyLoadReconciliationReport.fs`, `LazyLoadReconciliationServer.fs`.

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
