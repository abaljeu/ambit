# Undo Slice 2 worker report

## Public interface

- [[src/Shared/History.fs]] adds Change.inverse with the shape Revision → Guid → Change → Change. It reverses Ops, swaps old and new values, and omits NewNode and NewSpecialNode.
- [[src/Shared/ClientHistory.fs]] adds clear, record, undo, redo, and confirm. ClientHistory keeps its stacks and pending lineage private. HistoryRecord keeps recordId, commandName, and applied. PendingTransition keeps recordId, submittedChangeId, and Normal, Undo, or Redo.
- Undo and Redo return an ordinary inverse Change, the exact command name, the moved ClientHistory, and a PendingTransition. Empty History returns None.
- confirm returns the amended ClientHistory and an optional re-derived direct dependent Change. The dependent keeps its own id and changeId.

## Files changed

- [[src/Shared/History.fs]]
- [[src/Shared/ClientHistory.fs]]
- [[src/Shared/Gambol.Shared.fsproj]]
- [[tests/Shared.Tests/ClientHistoryTests.fs]]
- [[tests/Shared.Tests/LargeChangeApplyTests.fs]]
- [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]]
- [[undo-implementation-plan.md]]
- [[implement-undo-slice-2.md]]

## Verification

```bash
dotnet build tests/Shared.Tests -c Debug
```

Passed: build succeeded with 0 warnings and 0 errors.

```bash
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~ClientHistoryTests|FullyQualifiedName~HistoryTests|FullyQualifiedName~LargeChangeApplyTests" --logger "console;verbosity=detailed"
```

Passed: 71 of 71 focused tests in 4.0382 seconds. The existing destructive 2,000-Node Undo baseline printed 2,216.252 ms. The ordinary inverse test proves that all 2,000 created Nodes remain in Graph.nodes after edge detachment. IDE lint reported no errors in the changed source and project files.

## Slice 3 caveat

confirm returns the revised direct dependent Change, but Slice 3 must replace the matching queued Change and keep it blocked behind its predecessor before release. This slice does not migrate queues, project graph corrections, or change any runtime caller. No commit was created.
