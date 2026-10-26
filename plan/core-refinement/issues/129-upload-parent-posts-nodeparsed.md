# 129 — Upload parent posts NodeParsed

## 1. Type

coding

## 2. Blockers

[ ] [134 — The client door forwards state Notices](134-client-door-forwards-state-notices.md)

## 3. Problem

This is round 7 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). `WorkspaceUploadStructure.markParentCurrent` and `promoteUnparsedDirsWithMembers` move an Unparsed Directory or Workspace to Current after its stubs attach. No Notice covers this today. `noticesBeside` drops `NodeParsed` when the Change also targets PullingFile or Unparsed (`uploadOrArrival`).

## 4. Change

- `markParentCurrent` emits `Notice.NodeParsed dirId` as a **trail** Notice. Placement is the rule in [135 — Op readers leave](135-op-readers-leave.md).
- `planStubOps` returns `EventBody list`, unless 128 already made that change.
- Delete the `Op.SetDocumentState(nodeId, Unparsed, Current)` line.

Source: `WorkspaceUploadStructure.fs`, `UpdateWorkspaceSync.fs`, `WorkspaceCloudUpload.fs`, `AmbitSession.fs`.

## 5. Tests

[ ] `WorkspaceUploadStructureTests` `new file stubs start PullingFile; directory parent stays Current`, `reused Unparsed directory with owned child becomes Current`
[ ] `NodeEventExpandTests` (the upload facts)

## 6. Open question (Alan)

`applyNodeParsed` also calls `pushParsedUnpersisted` and `noteParsed`. Is that right for an upload or reconcile parent that goes `Unparsed → Current` without a parse (passes 7 and 9)? Today `uploadOrArrival` hides it.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
