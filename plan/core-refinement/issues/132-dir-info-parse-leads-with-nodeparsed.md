# 132 — Dir info parse leads with NodeParsed

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

This is round 10 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). In `LazyLoadReconciliationApply.parseDirInfoIfPresent`, `markCurrent` (Unparsed or PullingFile to Current) leads the artifact parse ops and unlocks them. Two things cover it today. `noticesBeside` covers Unparsed to Current, but only when the Change has no Unparsed or PullingFile target. `LazyLoadReconciliationServer.postParsedFromPulling` covers PullingFile to Current. Both read the op.

## 4. Change

- `parseDirInfoIfPresent` emits `Notice.NodeParsed nodeId` as a **lead** Notice. Placement is the rule in [135 — Op readers leave](135-op-readers-leave.md).
- `Report.ops` becomes `EventBody list`, unless a sibling ticket already made that change.
- Delete both `markCurrent` arms.

Source: `LazyLoadReconciliationApply.fs`, `LazyLoadReconciliation.fs`, `LazyLoadReconciliationReport.fs`, `LazyLoadReconciliationServer.fs`.

## 5. Tests

[ ] `LazyLoadReconciliationTests` `exact amb add with text parses outline immediately`, `exact amb modify with text reparses instead of leaving unparsed`, `exact amb modify warm keeps node id on text edit`
[ ] `LazyLoadReconciliationServerTests`

## 6. Open question (Alan)

A `parseDirInfoIfPresent` node created earlier in the same reconcile plan cannot take a lead Notice, because it does not exist yet. Should it trail, so the Change keeps the op until a later Change?

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
