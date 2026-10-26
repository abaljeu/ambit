# 131 — Reconcile parent drops its SetDocumentState

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

This is round 9 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `LazyLoadReconciliationApply.markParentCurrent` moves an Unparsed parent `Unparsed → Current` after its stubs attach. No Notice covers this today (`uploadOrArrival`).

## 4. Change

- Delete the `Op.SetDocumentState(nodeId, Unparsed, Current)` line.

Backend inference: a `Parse` graph-only Change whose `Replace` attaches owned stubs to an Unparsed Directory or Workspace infers a trail `NodeParsed dirId`. The rule goes in `DocumentOpImpact.inferNotices` (135).

Hard: every reconcile and every file parse posts with command name `Parse` (`GraphOnlyChangePost.postChunks`, `ParseThread.postParseOps`). The same `Replace` in a received-file reconcile ends with the container Unparsed (133). The ops cannot tell the two apart.

Source: `LazyLoadReconciliationApply.fs`, `DocumentOpImpact.fs`.

## 5. Tests

[ ] `LazyLoadReconciliationTests` (the facts where stubs attach under an Unparsed parent)
[ ] `LazyLoadReconciliationServerTests`

## 6. Open question (Alan)

`applyNodeParsed` also calls `pushParsedUnpersisted` and `noteParsed`. Is that right for an upload or reconcile parent that goes `Unparsed → Current` without a parse (rounds 7 and 9)? Today `uploadOrArrival` hides it.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
