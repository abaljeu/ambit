# 129 — Upload parent drops its SetDocumentState

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

This is round 7 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `WorkspaceUploadStructure.markParentCurrent` and `promoteUnparsedDirsWithMembers` move an Unparsed Directory or Workspace `Unparsed → Current` after its stubs attach. No Notice covers this today. `noticesBeside` drops `NodeParsed` beside a PullingFile or Unparsed mark (`uploadOrArrival`).

After a rollback to `47a85e1d^`: `WorkspaceUploadStructure.markUnparsed` has its round 6 op again (a4f7af01).

## 4. Change

- Delete the `Op.SetDocumentState(nodeId, Unparsed, Current)` line in `markParentCurrent`.

Backend inference: an Edit Change whose `Replace` attaches owned stubs to an Unparsed Directory or Workspace infers a trail `NodeParsed dirId`. The rule goes in `DocumentOpImpact.inferNotices` (135).

Hard: `promoteUnparsedDirsWithMembers` scans the whole graph. A directory it promotes may have no op in the Change. The same `Replace` in a received-file reconcile ends with the container Unparsed (133).

Source: `WorkspaceUploadStructure.fs`, `DocumentOpImpact.fs`.

## 5. Tests

[ ] `WorkspaceUploadStructureTests` `reused Unparsed directory with owned child becomes Current`
[ ] `NodeEventExpandTests` (the upload facts)

## 6. Open question (Alan)

`applyNodeParsed` also calls `pushParsedUnpersisted` and `noteParsed`. Is that right for an upload or reconcile parent that goes `Unparsed → Current` without a parse (rounds 7 and 9)? Today `uploadOrArrival` hides it.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
