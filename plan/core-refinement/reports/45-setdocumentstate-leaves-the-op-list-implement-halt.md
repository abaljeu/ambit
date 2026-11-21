# 45 — SetDocumentState leaves the Op list implement halt

Implement of [45 — SetDocumentState leaves the Op list](../issues/45-remove-document-state-set-ops.md) stopped before source edits. Branch is `dev`. Ticket status stays `defined`.

## Size

[ ] Changes removes the `Op.SetDocumentState` union case.
[ ] Look at names 10 source files.
[ ] [Slicing work](../../context/slicing-work.md) Size is 8 source files or fewer.
[ ] The work returns to slicing.

Look at source files:

- [History.fs](../../../src/Shared/History.fs)
- [Serialization.fs](../../../src/Shared/Serialization.fs)
- [DocumentOpImpact.fs](../../../src/Shared/DocumentOpImpact.fs)
- [ResidentProjection.fs](../../../src/Shared/ResidentProjection.fs)
- [DatabaseProjection.fs](../../../src/Server/DatabaseProjection.fs)
- [DocumentColdParse.fs](../../../src/Shared/documents/DocumentColdParse.fs)
- [LazyLoadReconciliationServer.fs](../../../src/Server/LazyLoadReconciliationServer.fs)
- [EventJson.fs](../../../src/Shared/EventJson.fs)
- [Api.fs](../../../src/Server/Api.fs)
- [GraphOps.fs](../../../src/Shared/GraphOps.fs)

## Blockers

[ ] [50 — Upload emits NodeUploading](../issues/50-upload-emits-nodeuploading.md) status is `defined`.
[ ] [53 — Arrival and reconcile emit NodeReceived](../issues/53-arrival-and-reconcile-emit-nodereceived.md) status is `defined`.
[ ] [54 — Parse and import stop the Parsed Set](../issues/54-parse-and-import-stop-the-parsed-set.md) status is `defined`.
[ ] [57 — Upload Actor posts Node notices](../issues/57-upload-actor-posts-document-state.md) status is `defined`.
[ ] Production constructors of `Op.SetDocumentState` remain, so dropping the union case fails to compile.
[ ] `Api.inferredNodeEvents` remains and matches `Op.SetDocumentState`.

### Constructors that remain

[ ] [WorkspaceUploadStructure.fs](../../../src/Shared/WorkspaceUploadStructure.fs) `markUnparsed` builds `Op.SetDocumentState`.
[ ] [WorkspaceUploadStructure.fs](../../../src/Shared/WorkspaceUploadStructure.fs) `markPullingFile` builds `Op.SetDocumentState`.
[ ] [WorkspaceUploadStructure.fs](../../../src/Shared/WorkspaceUploadStructure.fs) `markParentCurrent` builds `Op.SetDocumentState`.
[ ] [WorkspaceUploadStructure.fs](../../../src/Shared/WorkspaceUploadStructure.fs) `planServerFilePresentOps` builds `Op.SetDocumentState`.
[ ] [LazyLoadReconciliationApply.fs](../../../src/Shared/dotnet/LazyLoadReconciliationApply.fs) `markUnparsed` builds `Op.SetDocumentState`.
[ ] [LazyLoadReconciliationApply.fs](../../../src/Shared/dotnet/LazyLoadReconciliationApply.fs) `parseDirInfoIfPresent` local `markCurrent` builds `Op.SetDocumentState`.
[ ] [LazyLoadReconciliation.fs](../../../src/Shared/dotnet/LazyLoadReconciliation.fs) `unparsedStateOp` builds `Op.SetDocumentState`.
[ ] [ImportText.fs](../../../src/Shared/ImportText.fs) `markDocumentCurrentBeforeParse` builds `Op.SetDocumentState`.
[ ] [ImportDocument.fs](../../../src/Shared/dotnet/ImportDocument.fs) `planParseFile` builds `Op.SetDocumentState`.
[ ] [DirectoryReconcile.fs](../../../src/Shared/dotnet/DirectoryReconcile.fs) `markCurrent` builds `Op.SetDocumentState`.

### Inference that remains

[ ] [Api.fs](../../../src/Server/Api.fs) `inferredNodeEvents` reads `Op.SetDocumentState` on `EventBody.Change`.
[ ] [Api.fs](../../../src/Server/Api.fs) `postEvents` collects notices from `inferredNodeEvents`.

## Tests outside this ticket

[ ] Tests the ticket names are three: add `SetDocumentState leaves the Op list`, remove `Op.SetDocumentState round-trip`, remove `SetDocumentState includes the containing parent and filters unwritable root`.
[ ] Other tests still construct or match `Op.SetDocumentState`, so the named test edits leave the build red.

Files that still name `Op.SetDocumentState`:

- [SpecialNodeStateAxesTests.fs](../../../tests/Shared.Tests/SpecialNodeStateAxesTests.fs)
- [HistoryTests.fs](../../../tests/Shared.Tests/HistoryTests.fs)
- [SerializationTests.fs](../../../tests/Shared.Tests/SerializationTests.fs) `PullingFile round-trip` and `PullingFile node and state op round-trip`
- [NodeEventExpandTests.fs](../../../tests/Server.Tests/NodeEventExpandTests.fs)
- [LazyLoadReconciliationTests.fs](../../../tests/Shared.Tests/LazyLoadReconciliationTests.fs)
- [DirectoryReconcileTests.fs](../../../tests/Shared.Tests/DirectoryReconcileTests.fs)
- [WorkspaceUploadStructureTests.fs](../../../tests/Shared.Tests/WorkspaceUploadStructureTests.fs)
- [ImportDocumentTests.fs](../../../tests/Shared.Tests/ImportDocumentTests.fs)
- [ImportTextTests.fs](../../../tests/Shared.Tests/ImportTextTests.fs)
- [ViewModelJoinOpsTests.fs](../../../tests/Shared.Tests/ViewModelJoinOpsTests.fs)
- [ClientHistoryTests.fs](../../../tests/Shared.Tests/ClientHistoryTests.fs)
- [DatabaseProjectionContractTests.fs](../../../tests/Server.Tests/DatabaseProjectionContractTests.fs)
- [DatabaseProjectionTests.fs](../../../tests/Server.Tests/DatabaseProjectionTests.fs)
- [StateEndpointTests.fs](../../../tests/Server.Tests/StateEndpointTests.fs)
- [ParseThreadLoadTests.fs](../../../tests/Server.Tests/ParseThreadLoadTests.fs)
- [LazyLoadReconciliationServerTests.fs](../../../tests/Server.Tests/LazyLoadReconciliationServerTests.fs)
- [LoadSaveCommandTests.fs](../../../tests/Server.Tests/LoadSaveCommandTests.fs)
- [GithubTransportLoadPullTests.fs](../../../tests/Server.Tests/GithubTransportLoadPullTests.fs)
- [GithubTransportActorTests.fs](../../../tests/Server.Tests/GithubTransportActorTests.fs)

## Result

[ ] Source is unchanged.
[ ] Tests are unchanged.
[ ] `scripts/agent-test.sh` did not run.
[ ] Ticket status stays `defined`.
