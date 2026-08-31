# LargeChangeApply nested Replace budget

## Failure

`LargeChangeApplyTests.nested parse tail with many Replace ops stays responsive` —
`sw.ElapsedMilliseconds < 300L` failed around ~365ms on apply alone.

## Diagnosis

1. **Append fast-path still used.** Nested shape is empty-parent `Replace(..., index=0, old=[], news)` for the document root and each branch. `Graph.replace` sets `isAppend` and calls `GraphBuild.appendChildren` — not `fromNodes`. No regression of the `8662f84` append path.
2. **Cost is many Replace validations, not rebuilds.** ~2200 `NewNode` + 201 `Replace`. FSI microbench (Debug Shared): NewNodes ~12ms; Replaces ~50–70ms of ~70–80ms apply. Under `dotnet test`, whole nested case was ~340–365ms wall before the fix (setup + apply + index assert).
3. **Hot work per Replace** (still on the append commit path):
   - `Op.apply` → `isMemberOfInaccessibleDocument` walked containing roots **twice** (Unparsed, then NoServerFile).
   - `Graph.replace` called `invalidOwnedFileDirectoryPlacement` **once per introduced child** (re-walking the owner chain each time).
   - Name-conflict checks ran for every Owner introduction even when no `Filename.Ok` names exist (parse-tail Normals).
4. **Budget not the first remedy.** Path was correct but validation was quadratic-ish with introduced-child count; prefer speedup over raising 300ms.

## Change

Surgical opts only (budget assert unchanged):

| File | Change |
| --- | --- |
| [[src/Shared/DocumentPartition.fs]] | `isMemberOfInaccessibleDocument` — single ancestor walk for Unparsed \| NoServerFile |
| [[src/Shared/GraphMutate.fs]] | Batch `invalidOwnedFileDirectoryPlacement` over full `newChildren`; skip sibling/artifact conflict walks unless an introduced Owner has `Filename.Ok` |

## Verification

Focused `LargeChangeApplyTests` (8× `--no-build` after rebuild):

- nested: ~164–181ms wall (was ~340–365ms)
- bulk NewNode perf: ~194–212ms
- consistency test: pass

Also: full `ModelTests` (226) pass — placement / Workspace / name-conflict coverage.

FSI apply-only nested med ~48ms (was ~71ms).

## Non-change

- Did **not** raise the 300ms guard.
- Did **not** commit (not requested).
