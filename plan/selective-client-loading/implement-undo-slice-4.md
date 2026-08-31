# Undo Slice 4 worker report

## Outcome

HTTP batches are ordinary `Change` lists. Explicit Undo/Redo JSON has no decoder. Successful `/changes` ACKs return durable complete Changes in request order for both new and duplicate items. Persistence stamps still append to the last newly logged Change, including when later items are duplicates. Unchanged first submissions reject the batch. FileAgent and DbAgent apply `History.applyChange` only; Server `applyAction` is off this path.

Slices 5, 6, and 7 were not started. Client ACK still extracts IDs and stamp suffixes, then uses `ackBatch` and `PersistStamp.applyToGraph`. Prefix validation, suffix projection, and History-neutral ACK remain Slice 5.

## Files changed

- [[src/Shared/Serialization.fs]] — `ChangeBatch` / `ChangeBatchAck` are Change lists; no Undo/Redo HTTP codec
- [[src/Shared/SyncBatch.fs]] — `toWireBatch` returns `Change list`
- [[src/Shared/ViewModel.fs]] — `SubmitResponse` carries confirmed Changes
- [[src/Client/UpdateCodec.fs]] — encode/decode complete Changes; `stampOpsFromSubmitted` bridge
- [[src/Client/Update.fs]] — ACK still retires by ID and applies stamp suffixes
- [[src/Client/App.fs]] — dispatch confirmed Changes
- [[src/Client/UpdateWorkspaceSync.fs]] — workspace ACK reads stamp suffixes from confirmed Changes
- [[src/Server/ChangeLog.fs]] — `tryFindByChangeId`
- [[src/Server/Database.fs]] — `tryGetPersistedPayload` (replaced `hasPersistedChangeId`)
- [[src/Server/FileAgent.fs]] — Change-only apply; complete ACK; reject Unchanged
- [[src/Server/DbAgent.fs]] — same; duplicate lookup before apply
- [[src/Server/Api.fs]] — graph-only posts encode a Change
- [[src/Server/LazyLoadReconciliationServer.fs]] — same encode
- [[tests/Shared.Tests/SerializationTests.fs]]
- [[tests/Shared.Tests/SyncPlannerTests.fs]]
- [[tests/Server.Tests/StateEndpointTests.fs]]
- [[tests/Server.Tests/FileAgentFailureTests.fs]]
- [[tests/Server.Tests/DatabaseProjectionContractTests.fs]]
- compile-only encode updates: DbAgentTests, DbAgentFailureTests, ChangeEndpointResilienceTests, IgnoredDestinationValidationTests, LazyLoadReconciliationServerTests

## Checkpoint tests

- Request order and exact submitted prefixes on ACK and Poll.
- `SetUpdateTime`-only stamp enrichment; ACK Change equals ChangeLog.
- Trailing duplicate keeps new stamps on the last new Change.
- Duplicate retry after a lost ACK returns the stored complete Change.
- Restart-safe inverse Changes remain in ChangeLog (file and DB).
- Unchanged submission is rejected.
- Bad second Change rejects the batch atomically (state and Poll unchanged).
- Explicit Undo/Redo JSON fails decoding; no compatibility decoder.

## Verification

```bash
dotnet build tests/Shared.Tests -c Debug
```

Passed: 0 warnings, 0 errors.

```bash
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~SerializationTests"
```

Passed: 37 of 37.

```bash
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~SerializationTests|FullyQualifiedName~SyncPlannerTests"
```

Passed: 70 of 70.

```bash
dotnet build tests/Server.Tests -c Debug
```

Passed: 0 warnings, 0 errors.

```bash
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~StateEndpointTests|FullyQualifiedName~FileAgentFailureTests|FullyQualifiedName~DatabaseProjectionContractTests"
```

Passed: 66 of 66.

```bash
dotnet build src/Client/Gambol.Client.fsproj -c Debug
```

Passed: 0 warnings, 0 errors.

No commit was created.

## Leftover risks for Slice 5

- `SubmitResponse` has complete Changes, but Update still maps them to IDs + stamp suffixes and applies stamps with `PersistStamp.applyToGraph`. Prefix validation, `SetUpdateTime` suffix projection through ResidentProjection, and History-neutral ACK are Slice 5.
- `ChangeRequest` DU, `History.applyAction`, `encodeChangeRequest` / `decodeChangeRequest` (localStorage fallback), and `applyAndEnqueueLocalAction` remain until Slice 5 deletes legacy History.
- Workspace `completeUploadStructurePost` still `ignore submitted` for reconciliation; it now reads stamp suffixes from confirmed Changes.
- Duplicate `overlayFresh` helpers in FileAgent and DbAgent.
- DB startup sweep can drop detached Undo headers; ChangeLog still has the inverse Change.

## WORK.md mutations

- `remove` [[.scratch/selective-client-loading/undo-implementation-plan.md]] — implement Slice 4 (Change-only wire and Server confirmations)
- `add` [[.scratch/selective-client-loading/undo-implementation-plan.md]] — implement Slice 5 (reconcile ACKs and remove legacy History) (parent: [[.scratch/selective-client-loading/undo-spec.md]])
- `move` none
- `block` none
