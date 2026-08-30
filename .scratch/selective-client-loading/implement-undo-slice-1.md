# Undo Slice 1 worker report

## Files changed

- [[tests/Shared.Tests/HistoryTests.fs]] adds reachable-structure Undo and Redo characterization for nested paste, split, and NewSpecialNode Changes.
- [[tests/Shared.Tests/LargeChangeApplyTests.fs]] adds the 2,000-Node paste-shaped Undo baseline and structural create-Op count.
- [[.scratch/selective-client-loading/implement-undo-slice-1.md]] records this worker result.

No production source or test project file changed.

## Verification

```bash
dotnet build tests/Shared.Tests -c Debug
```

Passed: build succeeded with 0 warnings and 0 errors.

```bash
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~HistoryTests|FullyQualifiedName~LargeChangeApplyTests" --logger "console;verbosity=detailed"
```

Passed: 57 of 57 focused tests in 3.8065 seconds.

## Baseline

The final focused run measured 2,175.085 ms for current Undo of a 2,000-Node paste-shaped Change. The structural assertion counted K = 2,000 NewNode or NewSpecialNode Ops, which are K rebuild opportunities in the current destructive Undo path.

Caveat: elapsed time is a local Windows Debug baseline, not a pass/fail budget. The pre-existing [[WORK.md]] modification was preserved and was not changed by this worker.
