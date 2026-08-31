# Undo Slice 3b worker report

## Outcome

`VM.history` is now ClientHistory. Normal local Changes record submitted-only payloads with a placeholder command name. Undo and Redo are optimistic: ClientHistory plans the inverse, ResidentProjection applies it, and the queue stores that Change with a real PendingTransition. Queued Undo and Redo items encode as `ChangeRequest.Change` with the Ops already on the queue. Poll and Load clear ClientHistory only for a non-empty semantic Change tail. Package-only Load preserves History only at the same settled Revision with no pending local transition.

Slices 4, 5, 6, and 7 were not started. ACK still uses IDs and `PersistStamp.applyToGraph`. `History.applyAction` remains as the compile bridge.

## Look-ahead easing

`PendingChange.toChangeRequest` always returns `ChangeRequest.Change item.change`. Mixed C/Undo/Redo batches keep their Ops on the wire. Local inversion and Server apply no longer diverge on create or paste.

## Files changed

- [[src/Shared/ViewModelSync.fs]] — always encode queued items as `ChangeRequest.Change`
- [[src/Shared/ViewModel.fs]] — `VM.history` is ClientHistory; `LoadDone` carries response Revision
- [[src/Shared/SyncLogic.fs]] — `ClientSyncState`, local apply, Poll/Load, package-only race
- [[src/Shared/SyncPlanner.fs]] — `enqueuePending`; `applyAndEnqueueLocalAction` kept as compile bridge
- [[src/Shared/WorkspaceUploadStructure.fs]] — annotate local `State` (record clash with `ClientSyncState`)
- [[src/Client/Program.fs]] — initial History is `ClientHistory.clear ()`
- [[src/Client/UpdateHelpers.fs]] — `applyAndPost` records and enqueues through ResidentProjection
- [[src/Client/UpdateOps.fs]] — Undo and Redo use `applyLocalUndo` / `applyLocalRedo`
- [[src/Client/Update.fs]] — Poll/Load use ClientHistory; ACK does not amend History
- [[src/Client/App.fs]] — restore does not copy Server History; `LoadDone` passes Revision
- [[src/Client/UpdateWorkspaceSync.fs]] — workspace apply records ClientHistory; failed structure POST undoes locally
- [[tests/Shared.Tests/VmTestHelpers.fs]]
- [[tests/Shared.Tests/SyncLogicTests.fs]]
- [[tests/Shared.Tests/SyncPlannerTests.fs]]
- [[tests/Shared.Tests/ClientHistoryRuntimeTests.fs]] (new)
- [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]]

## Checkpoint tests

- `applyLocalChange` stores the submitted Change and a Normal transition with a real `recordId`.
- Optimistic Undo and Redo apply through ResidentProjection and encode as `ChangeRequest.Change` with inverse Ops.
- Empty Poll and Load Change tails preserve ClientHistory.
- A non-empty semantic tail clears ClientHistory before projected application.
- Package-only Load at the same settled Revision with no pending local transition preserves History.
- Package-only Load with a pending local transition or a Revision mismatch returns `raced package payload`.

## Verification

```bash
dotnet build tests/Shared.Tests -c Debug
```

Passed: 0 warnings, 0 errors.

```bash
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ClientHistoryRuntimeTests|FullyQualifiedName~SyncLogicTests|FullyQualifiedName~SyncPlannerTests|FullyQualifiedName~ClientHistoryTests"
```

Passed: 76 of 76.

```bash
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~WorkspaceUploadTests|FullyQualifiedName~WorkspaceUploadStructureTests"
```

Passed: 31 of 31.

```bash
dotnet build src/Client/Gambol.Client.fsproj -c Debug
```

Passed: 0 warnings, 0 errors.

No commit was created.

## Leftover risks for slices 4 and 5

- HTTP still has `ChangeRequest.Undo` / `Redo` DU cases, codecs, and Server `applyAction`. The Browser no longer sends those cases for queued items.
- `ackBatch` still removes by ACK id set. Prefix validation, `SetUpdateTime` suffixes, and History-neutral ACK remain Slice 5. SubmitResponse still applies stamps with `PersistStamp.applyToGraph`.
- `applyAndEnqueueLocalAction` and `PendingChange.fromRequest` remain for compile and HistoryTests. Runtime enqueue uses `enqueuePending`.
- `applyAndPost` records the command name `"Change"` until Slice 6 wires source names.
- Restore still applies with `History.applyChange` onto a dummy Server History and does not recreate ClientHistory.
- `ClientSyncState` and `State` share field names; some record literals need an explicit type.

## WORK.md mutations

- `remove` [[plan/selective-client-loading/look-ahead-remaining-slices.md]] — encoding easing is done in this slice.
- `remove` [[plan/selective-client-loading/undo-implementation-plan.md]] — implement Slice 3b (wire runtime History and projected local flow)
- `add` [[plan/selective-client-loading/undo-implementation-plan.md]] — implement Slice 4 (cut the wire and Server to Change-only confirmations) (parent: [[plan/selective-client-loading/undo-spec.md]])
- `move` none
- `block` none
