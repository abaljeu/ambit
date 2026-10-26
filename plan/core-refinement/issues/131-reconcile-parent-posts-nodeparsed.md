# 131 — Reconcile parent posts NodeParsed

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

This is round 9 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `LazyLoadReconciliationApply.markParentCurrent` moves an Unparsed parent to Current after its stubs attach. No Notice covers this today, because `noticesBeside` drops `NodeParsed` beside an Unparsed mark (`uploadOrArrival`).

## 4. Change

- `markParentCurrent` emits `Notice.NodeParsed dirId` as a **trail** Notice. Placement is the rule in [135 — Op readers leave](135-op-readers-leave.md).
- `Report.ops` becomes `EventBody list`, unless a sibling ticket already made that change.
- Delete the `Op.SetDocumentState(nodeId, Unparsed, Current)` line.

Source: `LazyLoadReconciliationApply.fs`, `LazyLoadReconciliationReport.fs`, `LazyLoadReconciliationServer.fs`.

## 5. Tests

[ ] `LazyLoadReconciliationTests` (the facts where stubs attach under an Unparsed parent)
[ ] `LazyLoadReconciliationServerTests`

## 6. Open question (Alan)

`applyNodeParsed` also calls `pushParsedUnpersisted` and `noteParsed`. Is that right for an upload or reconcile parent that goes `Unparsed → Current` without a parse (passes 7 and 9)? Today `uploadOrArrival` hides it.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
