# Does planPatchDOM run when nothing meaningful changed?

Date: 2026-08-17  
Parent audit: [[investigation.md]]

## Call path

```
dispatch msg                          App.fs ~586–631
  prev = model
  update msg prev                     (CursorUp/Down → moveSelection*)
  refreshDesktopFileIndicator         (always runs; may rewrite same fields)
  SyncPlanner.tryReleaseQueued
  model <- newModel
  patchDOM prev newModel …            App.fs ~618–619  (unless StateLoaded / *SearchQuery)
    planPatchDOM old new cachedIds    View.fs ~74 → ViewModelDomPlan.fs ~30
    for every visible instId:         View.fs ~106–130
      resolveRow + atCorrectPos DOM check
```

**No model-equality short-circuit** before `planPatchDOM` or inside it. Search-query msgs skip `patchDOM` entirely; everything else (including no-op selection moves) still patches.

---

## Answers

### 1. CursorUp/Down at edge when selection does not move?

**Yes — `planPatchDOM` still runs.**

At the bottom edge, `moveSelectionBy` returns the same `model` unchanged:

```250:253:src/Shared/ViewModelSelection.fs
                    if nextIndex < 0 then
                        { model with selectedNodes = None; mode = Selecting }
                    elif nextIndex >= rows.Length then
                        model
```

`dispatch` still always calls `patchDOM prev newModel` (App.fs ~612–619). Even if `prev` and `newModel` are observationally identical (same selection, same siteMap/graph), planning still walks **all** visible rows.

Top edge with a selection: `nextIndex < 0` **does** clear selection (`selectedNodes = None`) — that *is* a model change. With `selectedNodes = None` already, CursorUp is a pure no-op model (`applyMoveSelection` → `model`) but still hits `patchDOM`.

### 2. selectedNodes/focus changed, document structure unchanged?

**Yes — full `planPatchDOM` + full visible-row `patchDOM` apply loop.**

Planning does not have a selection-only fast path. It diffs every visible row’s selected/focused classes (and other per-row fields). Typical DOM *writes* are ~2 class changes; the *cost* is still the full walk + O(N²) `tryFindIndex` selection checks (see investigation).

`patchDOM` then walks all visible IDs again for depth + DOM order (`atCorrectPos`), even when mutations are only `PatchRow` with class tweaks — or empty patch lists.

### 3. Equality check that skips planning?

**No.**

- No `oldModel = newModel` / reference-equality guard in `dispatch`, `patchDOM`, or `planPatchDOM`.
- Per-row, only individual patch *emissions* are gated (`if newClass <> oldClass then yield …`). Empty patch lists still produce `PatchRow (instId, [])` for every cached visible row (`ViewModelDomPlan.fs` ~135).
- `resolveRow` still calls `applyRowPatches` on those empty lists (`RowView.fs` ~409–412).

### 4. What “nothing changes” means here

| Layer | Unchanged selection (edge / no-op) | Selection moved, structure same |
|-------|-------------------------------------|----------------------------------|
| `update` | Often identical VM fields | `selectedNodes` (and maybe `mode`) only |
| `planPatchDOM` | Always invoked; full visible walk; usually empty patches | Always invoked; full walk; ~2 `SetClassName` |
| `patchDOM` apply | Always walks all visible rows; DOM order checks; empty applies | Same full walk; applies ~2 class patches |
| Meaningful UI | No class/text DOM writes | Focus/selected classes on ~2 rows |

So “nothing meaningful changed” for the user (no selection move, or only chrome-identical model) still means **full O(visible) planning + apply**, including the expensive selection/focus recompute. Meaningful structural identity is never used to skip that path.
