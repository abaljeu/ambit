# 133 — Received file container posts MarkUnparsed

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

This is round 11 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `LazyLoadReconciliation.unparsedStateOp` marks the containing Directory or Workspace Unparsed after an added, removed, or renamed file. The op is appended after the file plan, so it wins over `markParentCurrent`. `MarkUnparsed` is read from the op. That is #399.

## 4. Change

- `containingDirectoryMarkOps` emits `Notice.MarkUnparsed dirId` as a **trail** Notice, after every file-plan body. Placement is the rule in [135 — Op readers leave](135-op-readers-leave.md).
- `Report.ops` becomes `EventBody list`, unless a sibling ticket already made that change.
- Delete `unparsedStateOp`.

Source: `LazyLoadReconciliation.fs`, `LazyLoadReconciliationReport.fs`, `LazyLoadReconciliationServer.fs`.

## 5. Tests

[ ] `LazyLoadReconciliationTests`: the 8 `received file …` facts #399 found
[ ] `LazyLoadReconciliationServerTests` `git pull add marks the containing directory Unparsed`

## 6. Open question (Alan)

[121 — MarkUnparsed leaves Notice](121-markunparsed-leaves-notice.md) removes `Notice.MarkUnparsed`, which rounds 8 and 11 use. If 121 lands first, which Notice covers `Current → Unparsed`?

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
