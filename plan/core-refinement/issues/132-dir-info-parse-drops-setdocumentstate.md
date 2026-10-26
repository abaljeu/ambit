# 132 — Dir info parse drops its SetDocumentState

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

This is round 10 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). In `LazyLoadReconciliationApply.parseDirInfoIfPresent`, `markCurrent` (`Unparsed → Current` or `PullingFile → Current`) leads the artifact parse ops and unlocks them. `noticesBeside` covers `Unparsed → Current` only when there is no other Unparsed or PullingFile target. `LazyLoadReconciliationServer.parsedFromPulling` and `postParsedFromPulling` read the op for `PullingFile → Current`.

After a rollback to `47a85e1d^`: `parsedFromPulling` and `postParsedFromPulling` are gone (122a6b96), and `PullingFile → Current` has no Notice.

## 4. Change

- Delete both `markCurrent` arms.
- On staging, delete `parsedFromPulling` and `postParsedFromPulling`.

Backend inference: a `Parse` Change whose ops land inside an Unparsed or PullingFile Directory File root infers a lead `NodeParsed nodeId`. The rule goes in `DocumentOpImpact.inferNotices` and is applied ahead of the ops by the 135 hook.

Hard: the inference reads the graph before the Change. A node created earlier in the same Change is not in it yet.

Source: `LazyLoadReconciliationApply.fs`, `LazyLoadReconciliationServer.fs`, `DocumentOpImpact.fs`.

## 5. Tests

[ ] `LazyLoadReconciliationTests` `exact amb add with text parses outline immediately`, `exact amb modify with text reparses instead of leaving unparsed`, `exact amb modify warm keeps node id on text edit`
[ ] `LazyLoadReconciliationServerTests`

## 6. Open question (Alan)

A `parseDirInfoIfPresent` node created earlier in the same reconcile Change is not in the graph the backend infers from. Should its `NodeParsed` apply after its `NewSpecialNode` instead of ahead of the Change?

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
