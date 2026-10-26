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

[x] Delete the `Op.SetDocumentState(nodeId, Unparsed, Current)` line in `markParentCurrent`. [128/129: delete upload stub and parent SetDocumentState](https://github.com/abaljeu/ambit/pull/419).

[x] Remove the do-nothing `markParentCurrent` and `promoteUnparsedDirsWithMembers`. `hasOwnedMember` left with that scan.

[x] Backend inference is dropped per Alan. The desktop should not figure out nodes for uploaded files.

[ ] Correct each disabled test named in §5.

The same `Replace` in a received-file reconcile ends with the container Unparsed ([133 — Received file container drops its SetDocumentState](133-received-file-container-drops-setdocumentstate.md)).

Source: `WorkspaceUploadStructure.fs`, `DocumentOpImpact.fs`.

## 5. Tests

[x] Delete `WorkspaceUploadStructureTests` `reused Unparsed directory with owned child becomes Current`. It only exercised the removed parent mark.
[x] Delete `WorkspaceUploadStructureTests` `plan promotes ancestor directories when nested file stub added`. It only exercised the removed parent mark.
[x] Delete `WorkspaceUploadStructureTests` `markPullingFile sets PullingFile`. It only exercised the removed marks.
[x] Delete `WorkspaceUploadStructureTests` `new file stubs start PullingFile; directory parent stays Current`. It only exercised the removed marks.
[ ] Correct `NodeEventExpandTests` `upload posts NodeUploading beside SetDocumentState` (disabled, 128/129).
[ ] Correct `NodeEventExpandTests` `upload emits NodeUploading` (disabled, 128/129).

## 6. Open question (Alan)

`applyNodeParsed` also calls `pushParsedUnpersisted` and `noteParsed`. Is that right for an upload or reconcile parent that goes `Unparsed → Current` without a parse (rounds 7 and 9)? Today `uploadOrArrival` hides it.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests in this round are green.
