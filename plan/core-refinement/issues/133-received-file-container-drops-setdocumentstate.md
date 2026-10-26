# 133 — Received file container drops its SetDocumentState

**Status:** `defined`

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

## 6. Decision (Alan)

`Notice.NodeUnparsed` covers `Current → Unparsed`. [121 — MarkUnparsed renamed NodeUnparsed](121-markunparsed-renamed-nodeunparsed.md) renames `Notice.MarkUnparsed` to that case. Rounds 8 and 11 use it.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
