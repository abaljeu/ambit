# 124 — Remove SetDocumentState one site at a time

JUST REMOVE THE LINE.  THEN RUN THE DEFAULT TESTS.  
THEN IF IT FAILS, ANALYZE, AND CORRECT THE PROGRAM OR TESTS.
IF IT DOESN'T FAIL, CLAIM SUCCESS FOR THE TURN.

## 1. Type

coding

## 2. Blockers

[ ] 122 — Document state moves by Notice

## 3. Look at

### Turn

[ ] One turn looks at one file in Producers.
[ ] That turn removes `SetDocumentState` from that file.
[ ] That turn tests that the file still works.

### Owner

[ ] [History.fs](../../../src/Shared/History.fs) `Op.SetDocumentState`, plus `Op.apply`, `Op.invert`, and undo

### Producers

[x] [ImportText.fs](../../../src/Shared/ImportText.fs) `ImportText.markDocumentCurrentBeforeParse`
[ ] [ImportDocument.fs](../../../src/Shared/dotnet/ImportDocument.fs) `ImportDocument.planParseFile`
[ ] [DirectoryReconcile.fs](../../../src/Shared/dotnet/DirectoryReconcile.fs) `DirectoryReconcile.markCurrent`
[ ] [WorkspaceUploadStructure.fs](../../../src/Shared/WorkspaceUploadStructure.fs) `markUnparsed`, `markPullingFile`, `markParentCurrent`, `planServerFilePresentOps`
[ ] [LazyLoadReconciliationApply.fs](../../../src/Shared/dotnet/LazyLoadReconciliationApply.fs) `markUnparsed`, `markParentCurrent`, `parseDirInfoIfPresent`
[ ] [LazyLoadReconciliation.fs](../../../src/Shared/dotnet/LazyLoadReconciliation.fs) `LazyLoadReconciliation.unparsedStateOp`

### Notice posts

[ ] [DocumentOpImpact.fs](../../../src/Shared/DocumentOpImpact.fs) `DocumentOpImpact.noticesBeside`
[ ] [ParseThread.fs](../../../src/Server/ParseThread.fs) `ParseThread.finishParse`
[ ] [LazyLoadReconciliationServer.fs](../../../src/Server/LazyLoadReconciliationServer.fs) `postParsedFromPulling`, `reconcileReceivedFiles`

### Tests

[ ] [ImportTextTests.fs](../../../tests/Shared.Tests/ImportTextTests.fs)
[ ] [ImportDocumentTests.fs](../../../tests/Shared.Tests/ImportDocumentTests.fs)
[ ] [WorkspaceUploadStructureTests.fs](../../../tests/Shared.Tests/WorkspaceUploadStructureTests.fs)
[ ] [LazyLoadReconciliationTests.fs](../../../tests/Shared.Tests/LazyLoadReconciliationTests.fs)
[ ] [NodeEventExpandTests.fs](../../../tests/Server.Tests/NodeEventExpandTests.fs)
[ ] [LazyLoadReconciliationServerTests.fs](../../../tests/Server.Tests/LazyLoadReconciliationServerTests.fs)
[ ] [SpecialNodeStateAxesTests.fs](../../../tests/Shared.Tests/SpecialNodeStateAxesTests.fs) `SetDocumentState writes parseState and pullingFile together`
[ ] [SerializationTests.fs](../../../tests/Shared.Tests/SerializationTests.fs) `Op.SetDocumentState round-trip`

### Match sites that stay

[ ] Encode and decode in [Serialization.fs](../../../src/Shared/Serialization.fs)
[ ] `ResidentProjection.applyOpAllowing` in [ResidentProjection.fs](../../../src/Shared/ResidentProjection.fs)
[ ] The `SetDocumentState` match in [DatabaseProjection.fs](../../../src/Server/DatabaseProjection.fs)
[ ] `DocumentColdParse.targetsDocumentRoot` in [DocumentColdParse.fs](../../../src/Shared/documents/DocumentColdParse.fs)

## 4. Context

[ ] Context is [122 — Document state moves by Notice](122-document-state-moves-by-notice.md).
[ ] 122 — Document state moves by Notice made every `SetDocumentState` case theoretically safe to clean up.

## 5. Changes

[ ] One round removes one producer function's `Op.SetDocumentState` constructions.
[ ] The round runs when that function's Notice already reaches `CoreMailboxApi.postNotice`.
[ ] The round updates the tests that assert that function's op, in the same round.
[ ] The graph enum after the round matches the enum `Ev.documentStateFromNotice` writes for that Notice.
[ ] A second post of that Notice leaves the node on that enum.
[ ] The `Op.SetDocumentState` case stays until 45 — SetDocumentState leaves; NodeUnpersisted replaces it.
[ ] Match arms that classify, apply, invert, or serialize the op stay.

### Rounds

[x] Round 1. `ImportText.markDocumentCurrentBeforeParse`. `Unparsed` to `Current` leaves. `Notice.NodeParsed` stays. Filter `build import marks unparsed file current before tree operations` and `import logs NodeParsed beside SetDocumentState`.
[ ] Round 2. `ImportDocument.planParseFile`. `Unparsed` to `Current` leaves. `ParseThread.finishParse` still posts `Notice.NodeParsed`. Filter `planParseFile unparsed marks Current` and `planParseFile logs NodeParsed beside SetDocumentState`.
[x] Round 3. `DirectoryReconcile.markCurrent`. `Unparsed` to `Current` leaves. `ParseThread.finishParse` still posts `Notice.NodeParsed`. Filter `planDirectoryReconcile logs NodeParsed beside SetDocumentState`.
[ ] Round 4. `WorkspaceUploadStructure.planServerFilePresentOps`. `PullingFile` to `Unparsed` leaves. `Notice.NodeReceived` stays. Filter `incoming file posts NodeReceived beside SetDocumentState`.
[ ] Round 5. `WorkspaceUploadStructure.markPullingFile`. The move to `PullingFile` leaves. `Notice.NodeUploading` stays. Filter `markPullingFile sets PullingFile` and `upload posts NodeUploading beside SetDocumentState`.
[x] Round 6. `WorkspaceUploadStructure.markUnparsed`. The move to `Unparsed` leaves. `Notice.MarkUnparsed` or `Notice.NodeReceived` stays, matching the old enum.
[ ] Round 7. `WorkspaceUploadStructure.markParentCurrent`. Wait until the parent `Unparsed` to `Current` posts `Notice.NodeParsed` in the same Change as `markPullingFile`. Then the op leaves.
[ ] Round 8. `LazyLoadReconciliationApply.markUnparsed`. The move to `Unparsed` leaves. `Notice.MarkUnparsed` or `Notice.NodeReceived` stays. `reconcileReceivedFiles` still posts `Notice.NodeReceived` for a git-changed File after this op is gone.
[ ] Round 9. `LazyLoadReconciliationApply.markParentCurrent`. Wait until the parent `Unparsed` to `Current` posts `Notice.NodeParsed` in the same batch as `markUnparsed`. Then the op leaves.
[ ] Round 10. `LazyLoadReconciliationApply.parseDirInfoIfPresent`. `Unparsed` to `Current` and `PullingFile` to `Current` leave. `Notice.NodeParsed` still posts after `postParsedFromPulling` can no longer read that op.
[ ] Round 11. `LazyLoadReconciliation.unparsedStateOp`. The move to `Unparsed` leaves. `Notice.MarkUnparsed` stays for the containing Directory or Workspace. Filter `received file add marks the containing directory Unparsed`.

## 6. Tests

[ ] No round adds a new test file.
[ ] The round changes the tests in Look at that assert that producer.
[ ] `SetDocumentState writes parseState and pullingFile together` stays. It applies the op case.
[ ] `Op.SetDocumentState round-trip` stays. It covers the codec.
[ ] The filter for a round is the test name written on that round.

## 7. Done

[ ] Each round above is checked.
[ ] A repo search for `Op.SetDocumentState(` under `src` finds no producer in the eleven functions.
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
