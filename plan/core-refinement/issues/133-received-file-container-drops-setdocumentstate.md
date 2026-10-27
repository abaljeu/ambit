# 133 — Received file container drops its SetDocumentState

**Status:** `coded`

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

This is round 11 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md), in the same reconcile code as [130](130-reconcile-markunparsed-drops-setdocumentstate.md), [131](131-reconcile-parent-drops-setdocumentstate.md) and [132](132-dir-info-parse-drops-setdocumentstate.md). `LazyLoadReconciliation.containingDirectoryMarkOps` uses `unparsedStateOp` to mark the containing Directory or Workspace Unparsed after an added, removed, or renamed file. It is appended after the file plan, so it wins over `markParentCurrent`.

## 4. Change

- The plan's report carries the containing Directory or Workspace ids that `containingDirectoryMarkOps` chooses, as it carries `parsedParentIds` and `unparsedIds`.
- `reconcileReceivedFiles` posts `Notice.NodeUnparsed dirId` for those ids on the core mailbox, after 131's `NodeParsed`.
- No inference, no `noticesBeside`, no reading of SetDocumentState ops.
- Delete `unparsedStateOp` and its use in `containingDirectoryMarkOps` once the tests pass without it.

Source: `LazyLoadReconciliation.fs`, `LazyLoadReconciliationReport.fs`, `LazyLoadReconciliationServer.fs`.

## 5. Tests

[ ] `LazyLoadReconciliationTests`: the 8 `received file …` facts
[ ] `LazyLoadReconciliationServerTests` `git pull add marks the containing directory Unparsed`
[ ] New: git Load reconcile posts `NodeUnparsed` for the containing Directory after `NodeParsed`, passing with `unparsedStateOp` removed.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.

## 8. Round

[ ] `LazyLoadReconciliationReport.containingDirIds` lists each Directory or Workspace `LazyLoadReconciliation.containingDirectoryIds` chooses for an added, removed, or renamed file.
[ ] A Modified path adds no id to that list.
[ ] `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `Notice.NodeUnparsed` for each of those ids on the core mailbox, after `Notice.NodeParsed` for `parsedParentIds`.
[ ] A PullingFile container stays PullingFile. That id is on the report. The actor posts no `Notice.NodeUnparsed` for it.
[ ] `unparsedStateOp` and `containingDirectoryMarkOps` are gone. This site emits no `Op.SetDocumentState`.
[ ] `LazyLoadReconciliationApply.markUnparsed` returns no op. `Report.renamedReceivedIds` and `receivedFileIds` still name renamed files without reading ops.
[ ] Path failures stay on `Report.failures`. The actor still posts the Notices and the Change.
[ ] `git Load reconcile posts NodeUnparsed for the containing Directory after NodeParsed` checks that order and legacy Unparsed.
[ ] `git Load reconcile posts no NodeUnparsed for a PullingFile container` checks that the container stays PullingFile.
[ ] The received-file facts assert `containingDirIds` and `Notice.NodeUnparsed` for the container. A Modified file asserts an empty container list and its `unparsedIds` entry.
