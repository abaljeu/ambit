# Run unfolds the Run node

Branch: `w/expr`. Tree left dirty. No commit.

## Root cause

Fold is SiteMap state (`SiteEntry.expanded`), not a graph `Op`. [[src/Shared/ExprRun.fs]] already sets `Plan.unfold = true` when Run writes Children. [[src/Shared/AmbleRun.fs]] `run` returned only `plan.ops` and dropped that flag.

[[src/Client/UpdateAmbleRun.fs]] then called `expandEntry` on `focusedInstanceId` after `withSiteMap`. That is the focused occurrence in the refreshed selection, not unfold of the Run Node by NodeId. If selection refresh does not yield that instance, expand is a no-op and Children stay folded.

## Fix

- `AmbleRun.runPlan` returns `ExprRun.Plan` (`ops` and `unfold`). `AmbleRun.run` still returns `ops` only, so sibling error-string work can keep using `run`.
- `AmbleRun.applyUnfold` calls `ViewModel.applyFoldSession` for that NodeId when `unfold` is true.
- The client applies ops, reconciles the SiteMap, then `applyUnfold plan.unfold focusId …` using the focus NodeId from before apply. Ignore / empty ops do not unfold.

## Files changed

- [[src/Shared/AmbleRun.fs]]
- [[src/Client/UpdateAmbleRun.fs]]
- [[tests/Shared.Tests/AmbleRunTests.fs]]

## Tests

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~AmbleRunTests"
```

Result: **Passed — 19/19**.

## WORK.md mutations

None. This work was not on the board.
