# Undo Slice 3a worker report

## Outcome

Queue, submit, retry, error, and save payloads now use ordinary Change plus optional PendingTransition (`PendingChange`). `SyncInfo.pendingChanges` remains the complete unacknowledged queue. `SyncState.Sending` remains the single-submit marker. There is no second submitted-list field. The `SubmitPendingBatch` list is the same value that `runSubmitPendingBatch` posts, stores in `SubmitNetworkError` / `WaitingToRetry`, and passes through `SubmitResponse` for Slice 5.

## Files changed

- [[src/Shared/ViewModelSync.fs]] — `PendingKind`, `PendingTransition`, `PendingChange`
- [[src/Shared/ViewModel.fs]] — `SubmitResponse` carries the submitted list
- [[src/Shared/SyncPlanner.fs]] — enqueue, restore, retry snapshot
- [[src/Shared/SyncBatch.fs]] — `toPendingDeltaChain` / `toWireBatch`
- [[src/Shared/Serialization.fs]] — persist `PendingChange` (HTTP batch remains `ChangeRequest`)
- [[src/Client/App.fs]] — submit, restore, async workspace effect
- [[src/Client/Update.fs]] — retain submitted list on ACK
- [[src/Client/UpdateHelpers.fs]] — save/load pending queue
- [[src/Client/UpdateOps.fs]] — retry uses `SyncPlanner.retryWaiting`
- [[src/Client/UpdateWorkspaceSync.fs]] — singleton lineage before sync and async posts
- [[tests/Shared.Tests/SyncPlannerTests.fs]]
- [[tests/Shared.Tests/SyncLogicTests.fs]]
- [[tests/Shared.Tests/WorkspaceUploadTests.fs]]

[[src/Client/UpdateWorkspaceDownload.fs]] and [[tests/Shared.Tests/VmTestHelpers.fs]] needed no edits: download still calls `applyAndPostSync` with a Change, and that function builds the singleton before the request.

## Checkpoint tests

- Later actions do not alter the already-emitted `SubmitPendingBatch` list, and `retryWaiting` resends the `WaitingToRetry` snapshot rather than the grown queue.
- C, Undo, and Redo with the same `recordId` stay one batch, including the `ackBatch` remainder.
- `restorePending` strips transition, does not write History, drops stale revisions, and keeps the projected Change.
- Workspace singleton identity is the item used for delta-chain, wire encoding, and `ContinuePostUploadStructure`.

## Verification

```bash
dotnet build tests/Shared.Tests -c Debug
```

Passed: 0 warnings, 0 errors.

```bash
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~SyncPlannerTests|FullyQualifiedName~SyncLogicTests|FullyQualifiedName~WorkspaceUploadTests|FullyQualifiedName~HistoryTests"
```

Passed: 137 of 137 (the HistoryTests filter also matches ClientHistoryTests).

```bash
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~SerializationTests"
```

Passed: 36 of 36.

```bash
dotnet build src/Client/Gambol.Client.fsproj -c Debug
```

Passed: 0 warnings, 0 errors.

No commit was created.

## Leftover risks

- HTTP encoding still maps Undo/Redo transitions to `ChangeRequest.Undo` / `Redo` until Slice 4. Restored items have `transition = None`, so they POST as ordinary Changes.
- `ackBatch` still removes by ACK id set. Prefix validation and ignore-late-duplicate rules stay in Slice 5. `SubmitResponse` now has the submitted list.
- `PendingChange.fromRequest` uses `recordId = 0` for live Undo/Redo until Slice 3b records ClientHistory.
- `completeUploadStructurePost` receives the singleton and does not reconcile it yet.
- Old localStorage Undo/Redo intents without ops are dropped on load.

[[clarify-undo-slice-3a.md]] lists prefix ACK validation under Slice 3a new behavior. This work followed [[undo-implementation-plan.md]] Slice 3a and left that validation to Slice 5.

## WORK.md mutations

- `remove` Slice 3a from Active if the parent listed [[undo-implementation-plan.md]] or this report as the in-flight item.
- `add` [[undo-implementation-plan.md]] — implement Slice 3b (wire runtime History and projected local flow) (parent: [[undo-spec.md]])
- `move` none
- `block` none
