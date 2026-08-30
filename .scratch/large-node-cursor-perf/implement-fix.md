# Large-node cursor perf — implement report

Date: 2026-08-17  
Branch: `w/large-node-cursor-perf`  
Stage: active (implemented, not committed)

## What changed

### 1. Selection-only fast path (`planPatchDOM` / `patchDOM`)

- **`src/Shared/ViewModelDomPlan.fs`:** When `siteMap` and `graph` are reference-equal, both modes are `Selecting`, and workspace/sync capability fields used for row classes are unchanged, plan only the union of old/new selection-appearance instance ids (directly selected range + visible descendants). Emit non-empty `PatchRow`s (class / file indicator) only.
- **`src/Client/View.fs`:** If mutations have no `CreateRow`/`RemoveRow`/`RecreateRow`, apply only those `PatchRow`s — skip full visible preorder walk and `atCorrectPos` DOM reordering.

**Effect:** CursorUp/Down among siblings no longer plans or position-checks all N visible rows.

### 2. O(1) child index on `SiteEntry`

- **`src/Shared/ViewModel.fs`:** `SiteEntry.childIndex`; `SiteMap.siteChildIndex` / sibling next-prev use it.
- **`src/Shared/ViewModelSiteMap.fs`:** Set/maintain `childIndex` on build, expand, reconcile.
- **`src/Shared/ViewModelRowState.fs`:** `isInstanceDirectlySelected` / `isInstanceFocused` / `rowOwnership` use `entry.childIndex` (no `List.tryFindIndex` over siblings).
- **`src/Shared/ViewModelSelection.fs`:** `singleSelectionForInstance` uses `childIndex` with a sanity check against `parent.children.[idx]`.

**Effect:** Full `planPatchDOM` (non-fast-path) drops from O(N²) to O(N) for large sibling sets.

### Tests / fixtures

- New facts in `tests/Shared.Tests/ViewModelTests.fs` (childIndex + selection-move fast path).
- Literal `SiteEntry` records in DeleteOps / NodeDesktopPath tests updated with `childIndex = 0`.

## Tests run

```text
dotnet build tests/Shared.Tests -c Debug
dotnet test … --filter planPatchDOM|childIndex|Site.childIndex|ViewModelRowStateTests  → 51 passed
dotnet test … --filter selection move|SiteEntry.childIndex|Site.childIndex|DeleteOps|NodeDesktopPath → 31 passed
dotnet build src/Client -c Debug → succeeded
```

## Hypotheses addressed

| Investigation claim | Result |
| --- | --- |
| RC1: O(N²) `tryFindIndex` in sel/foc during full plan | Fixed via `childIndex`; fast path avoids the plan entirely for cursor moves |
| Full plan on every selection move | Fast path patches ~2 rows |
| `patchDOM` always walks all visible | Skipped for non-structural mutation lists |
| Virtualization | Out of scope (unchanged) |

## Remaining risks

- Fast path requires **reference** equality of `siteMap`/`graph`. Ops that rebuild equivalent maps fall through to full plan (correct, slower).
- Shift-select across a huge expanded subtree still patches all appearance ids under the range (necessary for correctness).
- Edit enter/exit still uses full plan — see [[further-speedups.md]].
- Unrelated dirty tree files on this branch (node-bullet-tooltip WIP, etc.) were not part of this fix; do not commit them with the perf change.

## Follow-up investigation

See [[further-speedups.md]] — residual no-op dispatch costs; edit-mode fast path recommendation.

## Board

Claimed/completed on [[WORK.md]]; item removed after verify.
