# 133 — Received file container drops its SetDocumentState

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

This is round 11 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `LazyLoadReconciliation.unparsedStateOp` marks the containing Directory or Workspace Unparsed after an added, removed, or renamed file. It is appended after the file plan, so it wins over `markParentCurrent`. That is #399.

## 4. Change

- Delete `unparsedStateOp` and its use in `containingDirectoryMarkOps`.

Backend inference: a `Replace` that adds or removes an owned File under a Directory or Workspace infers a trail `MarkUnparsed dirId`, ordered after 131's `NodeParsed`. The rule goes in `DocumentOpImpact.inferNotices` (135).

Hard: the same `Replace` under the same command name `Parse` ends Current in directory reconcile (131), and an Edit create or delete under a Directory has it too. Only `reconcileReceivedFiles` knows that a change is a git-received one.

Source: `LazyLoadReconciliation.fs`, `DocumentOpImpact.fs`.

## 5. Tests

[ ] `LazyLoadReconciliationTests`: the 8 `received file …` facts #399 found
[ ] `LazyLoadReconciliationServerTests` `git pull add marks the containing directory Unparsed`

## 6. Open question (Alan)

[121 — MarkUnparsed leaves Notice](121-markunparsed-leaves-notice.md) removes `Notice.MarkUnparsed`, which rounds 8 and 11 use. If 121 lands first, which Notice covers `Current → Unparsed`?

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
