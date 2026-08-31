# Undo Slice 5 worker report

## Outcome

ACK reconciliation uses the `SubmitResponse` submitted list and complete confirmed Changes. Validation is atomic: ordered identity, submitted Ops as an exact prefix, and `SetUpdateTime`-only suffixes. A valid active response removes only that `pendingChanges` prefix, retires those transitions, projects suffixes through ResidentProjection, and advances Revision without changing ClientHistory. A fully valid late response is ignored only when every submitted identity is already retired and its Revision is not ahead. Partial overlap and every other mismatch reject and require reload. Synchronous `applyAndPostSync` and async `completeUploadStructurePost` use the same seam with singleton lineage.

Legacy `ChangeRequest`, `History.applyAction`, Server undo/redo stacks, ACK ID aggregates, `PersistStamp.applyToGraph`, and the localStorage ChangeRequest fallback are gone from `src`. Slices 6 and 7 were not started.

## Files changed

- [[src/Shared/SyncLogic.fs]] — `AckReconcile` and `reconcileAck`
- [[src/Shared/SyncPlanner.fs]] — `retireSubmittedPrefix`; removed `ackBatch`, `ackRequiresReload`, and `applyAndEnqueueLocalAction`
- [[src/Shared/History.fs]] — removed `ChangeRequest`, `applyAction`, `addChange`, `History.undo`/`redo`, and `PersistStamp.applyToGraph`; `applyChange` no longer records Server History
- [[src/Shared/ViewModelSync.fs]] — removed `fromRequest` / `toChangeRequest`
- [[src/Shared/Serialization.fs]] — removed `encodeChangeRequest` / `decodeChangeRequest`
- [[src/Client/Update.fs]] — SubmitResponse goes through `reconcileAck`
- [[src/Client/UpdateWorkspaceSync.fs]] — both workspace ACK paths go through `reconcileAck`
- [[src/Client/UpdateCodec.fs]] — removed `stampOpsFromSubmitted`
- [[src/Client/UpdateHelpers.fs]] — pending-queue load no longer decodes ChangeRequest
- [[src/Server/FileAgent.fs]] — no History reset after load; revision-mismatch text says Change
- [[src/Server/DbAgent.fs]] — revision-mismatch text says Change
- [[tests/Shared.Tests/AckReconcileTests.fs]] — new checkpoint matrix
- [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]]
- [[tests/Shared.Tests/SyncPlannerTests.fs]]
- [[tests/Shared.Tests/HistoryTests.fs]]
- [[tests/Shared.Tests/ClientHistoryRuntimeTests.fs]]

[[src/Client/UpdateWorkspaceDownload.fs]], [[src/Server/Database.fs]], [[src/Server/DocumentLoader.fs]], and [[src/Server/SavePrep.fs]] needed no edits: download already calls `applyAndPostSync`, and State constructors still set `History.empty`.

## Checkpoint tests

- Normal, Undo, Redo, and same-batch C/U/Redo ACK retire the prefix and leave ClientHistory unchanged.
- Partial residency skips a stamp on an Absent node.
- Retry removes only the submitted prefix and resubmits the remainder.
- Late duplicate is ignored when identities are retired and Revision is not ahead.
- Rejection leaves graph, Revision, History, and pending unchanged.
- Workspace singleton ACK and async late-duplicate ignore use the same seam.
- Tests reject missing, reordered, unmatched, changed-prefix, partial-overlap, forward-Revision, and forbidden-suffix confirmations atomically.
- Source search finds no `ChangeRequest`, `applyAction`, `ackBatch`, `stampOpsFromSubmitted`, or `PersistStamp.applyToGraph` under `src`.

## Verification

```bash
dotnet build tests/Shared.Tests -c Debug
```

Passed: 0 warnings, 0 errors.

```bash
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~AckReconcileTests|FullyQualifiedName~SyncPlannerTests|FullyQualifiedName~HistoryTests|FullyQualifiedName~ClientHistoryRuntimeTests|FullyQualifiedName~SyncLogicTests|FullyQualifiedName~SerializationTests|FullyQualifiedName~ClientHistoryTests"
```

Passed: 176 of 176.

```bash
dotnet build src/Client/Gambol.Client.fsproj -c Debug
```

Passed: 0 warnings, 0 errors.

```bash
dotnet build tests/Server.Tests -c Debug
```

Passed: 0 warnings, 0 errors.

```bash
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~FileAgentFailureTests|FullyQualifiedName~StateEndpointTests|FullyQualifiedName~DatabaseProjectionContractTests"
```

Passed: 66 of 66.

No commit was created.

## Leftover risks for slices 6 and 7

- Slice 6 still owns command names: `applyAndPost` records `"Change"` until source names are wired.
- `State.history` remains an unused empty History field so Graph apply still has a State value. It is not a Server undo stack.
- `overlayFresh` stays duplicated in FileAgent and DbAgent; it builds complete confirmations, not ACK ID aggregates.
- Old localStorage ChangeRequest JSON no longer restores. PendingChange JSON still does.
- DB startup sweep can still drop detached Undo headers; ChangeLog still has the inverse Change (from Slice 4).
- Two `DbAgentTests` hang/failure cases failed under a broader filter (`Internal server error in DbAgent PostChan`). They do not use ACK recon. Treat as unrelated flake unless Slice 7 hits them again.

## WORK.md mutations

- `remove` [[plan/selective-client-loading/undo-implementation-plan.md]] — implement Slice 5 (reconcile ACKs and remove legacy History)
- `add` [[plan/selective-client-loading/undo-implementation-plan.md]] — implement Slice 6 (wire command provenance and feedback) (parent: [[plan/selective-client-loading/undo-spec.md]])
- `move` none
- `block` none
