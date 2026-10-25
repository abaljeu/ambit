# 123 — Prepare document state Notice context

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] none

## 3. Look at

### Owner

[ ] Still-open producers in section 4. Context of this ticket

### Source

[ ] [History.fs](../../../src/Shared/History.fs) `Op.SetDocumentState`

### Producers

[ ] [WorkspaceUploadStructure.fs](../../../src/Shared/WorkspaceUploadStructure.fs) `markUnparsed`, `markParentCurrent`
[ ] [LazyLoadReconciliationApply.fs](../../../src/Shared/dotnet/LazyLoadReconciliationApply.fs) `markUnparsed`, `markParentCurrent`, `parseDirInfoIfPresent`
[ ] [LazyLoadReconciliation.fs](../../../src/Shared/dotnet/LazyLoadReconciliation.fs) `unparsedStateOp`

## 4. Context

### Deferred

[ ] [54 — Parse and import stop the Parsed Set](54-parse-and-import-stop-the-parsed-set.md) owns `markDocumentCurrentBeforeParse`, `planParseFile`, `markCurrent`, and `Notice.NodeParsed`.
[ ] [50 — Upload emits NodeUploading](50-upload-emits-nodeuploading.md) owns `markPullingFile` and `Notice.NodeUploading`.
[ ] [53 — Arrival and reconcile emit NodeReceived](53-arrival-and-reconcile-emit-nodereceived.md) owns `planServerFilePresentOps` and `Notice.NodeReceived`.
[ ] [57 — Upload Actor posts Node notices](57-upload-actor-posts-document-state.md) removes `inferredNodeEvents`.

### Still open

[ ] `WorkspaceUploadStructure.markUnparsed` and `LazyLoadReconciliationApply.markUnparsed` emit `Current` → `Unparsed` or `PullingFile` → `Unparsed`. An existing `Unparsed` emits no op.
[ ] `markParentCurrent` on both modules emits `Unparsed` → `Current` for a `Directory` or `Workspace` parent.
[ ] `parseDirInfoIfPresent` emits `Unparsed` → `Current` or `PullingFile` → `Current`.
[ ] `unparsedStateOp` emits a move to `Unparsed` from the node's current state, including when that state is already `Unparsed`.

## 5. Changes

[ ] [122 — Document state moves by Notice](122-document-state-moves-by-notice.md) points at the tickets that establish each Notice effect. This ticket keeps the still-open producers.

## 6. Tests

[ ] No new test is required.
[ ] The old tests should still work.

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
