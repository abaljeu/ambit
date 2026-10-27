# 131 — Reconcile parent drops its SetDocumentState

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

This is round 9 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `LazyLoadReconciliationApply.markParentCurrent` moves an Unparsed parent `Unparsed → Current` after its stubs attach. No Notice covers this today (`uploadOrArrival`).

## 4. Change

- `LazyLoadReconciliationApply.markParentCurrent` returns an empty op list.
- `LazyLoadReconciliationReport.parsedParentIds` lists each Unparsed Directory or Workspace that owns an attached stub.
- `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `Notice.NodeParsed` for each of those ids.
- Graph-only apply uses `guardUnparsed` false.

Source: `LazyLoadReconciliationApply.fs`, `LazyLoadReconciliationReport.fs`, `LazyLoadReconciliationServer.fs`.

## 5. Tests

[ ] `LazyLoadReconciliationTests` (the facts where stubs attach under an Unparsed parent)
[ ] `LazyLoadReconciliationServerTests`

## 6. Answer (Alan)

Answered. Tabled.

No. `applyNodeParsed`'s `pushParsedUnpersisted` and `noteParsed` is not right for an upload or reconcile parent that goes `Unparsed → Current` without a parse.

Alan believes the question concerns the WebDAV-channel upload case.

Fixing it is tabled.

The intended direction: such uploads should go through the existing Load channel, which invokes parse at the right moments. It is unknown whether Load supports everything needed.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.

## 8. Round

[ ] `LazyLoadReconciliationApply.markParentCurrent` returns an empty op list. The Unparsed Directory or Workspace check stays.
[ ] `LazyLoadReconciliationApply.parentsOfAttachedStubs` names that container. `LazyLoadReconciliationReport.parsedParentIds` carries those ids.
[ ] `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `Notice.NodeParsed` for each id.
[ ] `upload-built directories become current when members are added` asserts those ids and `Notice.NodeParsed`.
[ ] `DocumentOpImpact.noticesBeside` stays as it is.
[ ] Graph-only apply uses `guardUnparsed` false.
