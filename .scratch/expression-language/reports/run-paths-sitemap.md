# Run paths vs SiteMap

Investigation only. Branch `w/sitemap-parent-index`. Related: [[delete-range-refactor.md]], [[delete-abort-when-children-remain.md]].

## Verdict

**Not all Run paths address SiteMap.** The Exec happy path remaps. Any path that applies Delete ops and then skips Run apply leaves SiteMap stale.

Holes (graph mutated, no `withSiteMap`):

- [[src/Client/UpdateAmbleRun.fs]] `applyRunPlan` empty-plan return after [[src/Client/UpdateOps.fs]] `deleteRange` applied.
- `runAmbleOp` when `AmbleRun.runPlanOnNode` is `Error` after Delete applied (legacy `>` rename reject).
- `applyRunPlan` when `applyAndPost` of Run ops is `Error` after Delete applied.
- `execRunOp` abort when Delete applied but Children remain (no Run, no remap). See [[delete-abort-when-children-remain.md]].

Shared planners never remap. Client `applyRunPlan` is the only remap on a successful Run apply.

## Client entry (only user Run)

There is one user command: [[src/Client/Commands.fs]] `cmd Exec` → `execRunOp`. [[src/Client/UpdateAmbleRun.fs]] `runAmbleOp` is not registered; only `execRunOp` calls it.

### `execRunOp` — [[src/Client/Commands.fs]]

`commitIfEditing`, then `AmbleRun.shouldExec`. If false: stop (no Delete, no Run). If the focus Node has Children, call `deleteRange` on `{ start = 0; endd = n }` only when `focusedInstanceId` and a SiteEntry exist. Then abort Run when `kidsLeft` (Children still present). Else `runAmbleOp`.

SiteMap: this function never remaps. It relies on `applyRunPlan` `withSiteMap` after Run ops. Delete success without a later remap is a hole.

### `deleteRange` — [[src/Client/UpdateOps.fs]]

`classifyDeleteForSelection` → `planDeleteOps` → `applyAndPost` Delete. **Does not remap.** Empty classify or empty plan: graph unchanged. `applyAndPost` `Error`: graph unchanged (`lastCmdResult` only).

### `deleteSelectionOp` — [[src/Client/UpdateOps.fs]]

Command Delete, not Exec. After non-empty effects: clamp selection and `withSiteMap`. Empty effects: no-op. Exec does not call this.

### `runAmbleOp` — [[src/Client/UpdateAmbleRun.fs]]

Second `commitIfEditing`, then `AmbleRun.runPlanOnNode`. `Error`: return committed model, **no remap**. `Ok plan`: `applyRunPlan`.

### `applyRunPlan` — [[src/Client/UpdateAmbleRun.fs]]

Empty `plan.ops`: return model, **no remap**. Else `applyAndPost` Exec. `Error`: return model, **no remap**. `Ok`: `withSiteMap`, then `AmbleRun.applyUnfold` when a query instance exists.

### `withSiteMap` — [[src/Client/UpdateHelpers.fs]]

`ViewModel.reconcileSiteMapFrom` (or rebuild on Zoom fallback). This is the remap Exec relies on.

### `applyAndPost` — [[src/Client/UpdateHelpers.fs]]

Applies the Change to the Graph only. No SiteMap.

### `commitIfEditing` — [[src/Client/UpdateHelpers.fs]]

`SetText` only. Not a structural Run apply. Remap not required.

## Shared (plan only)

### `AmbleRun.runPlan` / `run` / `runPlanOnNode` / `shouldExec` — [[src/Shared/AmbleRun.fs]]

Produce `ExprRun.Plan` or ops. No SiteMap.

`runPlan` empty plan: Special focus; or `ExprRun.Ignore` and the line does not start with `>`. `shouldExec` is false for Special and for non-`=` / non-`>` lines, so Exec does not call Run on those empty plans.

`ExprRun.Apply` (after `shouldExec`) always carries ops today (answers, `No matches found`, or error Nodes). Legacy `>` can still yield empty ops (`planErrorTextNodes` all-blank) or `Error` (`NodeRenameOps.planRenameNode`).

### `AmbleRun.applyUnfold` — [[src/Shared/AmbleRun.fs]]

`ViewModel.expandEntry` when `unfold`. Not a remap. Called only after `withSiteMap` in `applyRunPlan`.

### `ExprRun.run` / `classify` / `isRunStatement` — [[src/Shared/ExprRun.fs]]

Plan only. `Ignore` or `Apply`. No SiteMap.

### `Amble.run` — [[src/Shared/Amble.fs]]

Alias of `AmbleRun.run`. Plan only.

## Cases

| Case | Graph | SiteMap |
| --- | --- | --- |
| Happy: Run ops apply | Changed | `withSiteMap` then unfold |
| Empty plan, no prior Delete | Unchanged | Remap not required |
| Empty / Error Run after Delete applied | Children cleared (or other Delete ops) | **Stale** |
| `shouldExec` false / no selection | Unchanged | Remap not required |
| Blocked classify / missing instance or SiteEntry | Unchanged | Remap not required |
| Delete applied, Children remain, abort | May be changed | **Stale**; Run skipped |
| `deleteSelectionOp` success | Changed | Remapped (not on Exec) |

Tests in [[tests/Shared.Tests/AmbleRunTests.fs]] apply ops then `ViewModel.reconcileSiteMap` themselves. They do not cover Client `execRunOp` / `applyRunPlan`.
