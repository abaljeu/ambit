# Fix Exec Delete SiteMap and SiteEntry skip

Tree left dirty. No commit. Branch `w/sitemap-parent-index`. Related: [[run-paths-sitemap.md]], [[delete-abort-when-children-remain.md]], [[delete-range-refactor.md]].

## What changed

[[src/Shared/ViewModelDeleteOps.fs]]

- `classifyDeleteForChildSpan` classifies a child span from the graph Node (`parentId`, `start`, `endd`). Missing parent or empty span returns `[]`. No SiteMap or SiteEntry.
- `planDeleteChildSpan` plans ops from that classified list (`planFromClassified`). No SiteEntry.

[[src/Client/UpdateOps.fs]]

- `deleteChildSpan` classifies and plans via those Shared functions, `applyAndPost` as Delete, then `withSiteMap` on successful apply. Does not clamp or swap `selectedNodes`.
- `deleteRange` delegates to `deleteChildSpan` (`range.parent.nodeId`, `start`, `endd`). Command Delete still clamps selection after, then remaps again.

[[src/Client/Commands.fs]] `execRunOp`

- After `shouldExec`, if the query Node exists and has Children, calls `deleteChildSpan` on `{ start = 0; endd = n }`. No `focusedInstanceId` or SiteEntry lookup.
- Skip Delete only when the query Node is missing or has no Children.
- Abort Run if Children remain after Delete. Unchanged.

## How each SiteMap hole is closed

`deleteRange` / `deleteChildSpan` remap on successful apply. Exec holes inherit that.

| Hole | After this change |
| --- | --- |
| Empty plan after Delete applied | Delete remapped. `applyRunPlan` returns that model. |
| `runPlanOnNode` Error after Delete applied | Delete remapped. `runAmbleOp` returns that model. |
| `applyAndPost` of Run ops Error after Delete applied | Delete remapped. Run apply does not overwrite it. |
| Delete applied, Children remain, abort | Delete remapped. Run still skipped. |
| Empty classify / apply Error | Graph unchanged. Remap not required. |
| Happy-path Run | Delete remaps, then `applyRunPlan` remaps again. Fine. |

## Third leftover risk (fixed)

[[delete-range-refactor.md]] said: missing focus instance or SiteEntry while Children exist skips Delete and aborts Run.

That was the wrong gate. Delete is a graph operation. Exec now deletes Children of the query Node whether or not SiteEntries exist. SiteEntry absence does not block Delete. Only a missing query Node, or a query Node with no Children, skips Delete.

## Fourth leftover risk (not a risk)

[[delete-range-refactor.md]] said: `deleteSelectionOp` treats empty effects as no-op; `applyAndPost` success always enqueues `SavePendingQueue`.

Empty effects are a true no-op: empty classify, empty plan, or `applyAndPost` Error (graph unchanged, `lastCmdResult` only). Success always enqueues `SavePendingQueue`, so non-empty effects mean the graph changed and the clamp may run. That match is expected. No code change.

## Tests

```
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter FullyQualifiedName~DeleteOpsTests|FullyQualifiedName~AmbleRunTests
```

Result: **Passed — 42/42**.

New cases in [[tests/Shared.Tests/DeleteOpsTests.fs]]:

- After `planDeleteOps` apply, stale SiteMap still lists deleted children; `reconcileSiteMapFrom` drops them (full span).
- After partial `planDeleteOps` apply, remapped SiteMap keeps leftover children.
- `classifyDeleteForChildSpan` + `planDeleteChildSpan` delete without a SiteEntry.
- Missing parent Node → empty classify.
- Empty span → empty classify.

```
bash ./scripts/client.sh build
```

Result: Fable + esbuild succeeded.

No browser pass. Client `deleteChildSpan` / `execRunOp` are a compile gate only.

## Remaining risk

Blocked classify still returns empty ops (graph unchanged, abort if Children remain). `ResidentProjection.applyChange` can skip an Unloaded `Replace` and still return `Changed`; Exec still aborts when Children remain. SiteMap is remapped in that case.
