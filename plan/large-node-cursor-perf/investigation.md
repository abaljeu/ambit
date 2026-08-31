# Large-node cursor performance investigation

Date: 2026-08-17  
Branch context: `w/node-bullet-tooltip` (unrelated WIP; this audit is read-only)

## User report (repro hypothesis)

**Symptom:** With one expanded node showing hundreds of children in view, moving the selection cursor (arrow keys / CursorUp-CursorDown in Selecting mode) feels extremely slow. Slowness correlates with **visible child count**, not with editing text inside a single row.

**Minimal repro (expected):**

1. Open a workspace where one parent node has 300–500+ expanded children visible in the zoom root.
2. Select any row under that parent.
3. Hold ArrowDown or `o` (CursorDown) — each step should lag noticeably; lag grows with sibling count.
4. Collapse the parent (or zoom so children are hidden) — navigation returns to normal.

**Secondary hypothesis (mouse pointer):** There are **no** `mousemove` / `pointermove` handlers in the client. If the **mouse pointer** (not keyboard selection) is sluggish while the large list is on screen, that is likely **browser hit-testing / layout over a large flat DOM** (~6–8 elements per row × N rows), not an MVU dispatch loop.

---

## Architecture summary (rendering)

Gambol renders the command tree as **one DOM row per visible `SiteEntry`** — no virtualization.

| Step | File | What happens |
|------|------|----------------|
| Every `dispatch` (except search-query msgs) | `src/Client/App.fs` ~618–619 | `patchDOM prev newModel dispatch elementCache` |
| Plan mutations | `src/Shared/ViewModelDomPlan.fs` `planPatchDOM` | Walk **all** visible instance IDs; diff old/new per row |
| Apply mutations | `src/Client/View.fs` `patchDOM` | Walk **all** visible IDs again; `computeDepth`, DOM order check, patch/create |
| Full rebuild | `src/Client/View.fs` `render` | On `StateLoaded` only |

Visible rows come from preorder walk:

```412:417:src/Shared/ViewModelSiteMap.fs
    let getVisibleInstanceIds (siteMap: SiteMap) : SiteId list =
        match Map.tryFind siteMap.rootId siteMap.entries with
        | None -> []
        | Some root ->
            visiblePreorder siteMap root.instanceId
            |> List.map (fun entry -> entry.instanceId)
```

Each row includes `depth` indent `<div class="amb-indent">` elements, bullet, text, guid, file indicator (`src/Client/RowView.fs` `buildRowElement`). **500 children ≈ 500 `.amb-row` nodes and ~3000+ DOM nodes**, each with flex layout and bordered indents (`src/Server/wwwroot/style.css`).

---

## Root cause 1 (primary — keyboard cursor): O(n²) selection checks in `planPatchDOM`

Every selection move dispatches → `patchDOM` → `planPatchDOM`, which iterates **every visible row** even when only focus/selection classes change on ~2 rows.

For each cached visible row, `planPatchDOM` calls `isEntrySelected` and `isEntryFocused` (twice: new model + old model comparison):

```60:82:src/Shared/ViewModelDomPlan.fs
                        let oldEntry = Map.tryFind instId oldModel.siteMap.entries
                        let patches = [
                            let sel = isEntrySelected newModel entry
                            let foc = isEntryFocused newModel entry
                            ...
                            let oldSel = oldEntry |> Option.map (isEntrySelected oldModel) |> Option.defaultValue false
                            let oldFoc = oldEntry |> Option.map (isEntryFocused oldModel) |> Option.defaultValue false
```

Both helpers use `ancestorMatch`, which may invoke `isInstanceDirectlySelected` / `isInstanceFocused` on entries sharing the selection's parent:

```14:23:src/Shared/ViewModelRowState.fs
    let private isInstanceDirectlySelected (sel: Selection) (siteMap: SiteMap) (entry: SiteEntry) : bool =
        match entry.parentInstanceId with
        | Some parentInstId when parentInstId = sel.range.parent.instanceId ->
            ...
                match parentEntry.children |> List.tryFindIndex ((=) entry.instanceId) with
                | Some idx -> idx >= sel.range.start && idx < sel.range.endd
```

**When navigating among siblings under one parent with N children:**

- All N visible sibling rows share `sel.range.parent.instanceId`.
- Each row's check does `List.tryFindIndex` over the parent's **N-length** `children` list.
- `planPatchDOM` runs ~4 selection/focus comparisons per row (new/old × sel/foc).

**Complexity per CursorUp/CursorDown:** **O(N²)** in F# list scans, plus O(N) for `getVisibleRowInstanceIds` in `moveSelectionBy`:

```238:256:src/Shared/ViewModelSelection.fs
    let moveSelectionBy (delta: int) (model: VM) : VM =
        ...
                let rows = getVisibleRowInstanceIds model.siteMap
                match rows |> List.tryFindIndex ((=) instId) with
                ...
                        let nextInstId = rows[nextIndex]
```

For N=500: ~500 × 4 × 500 ≈ **1M** child-index scans per keystroke before any DOM write. This matches "extremely slow" cursor movement scaling with child count.

**DOM writes are not the bottleneck:** only rows whose `amb-selected` / `amb-focused` classes change get `SetClassName` patches (typically 2). The expensive work is the full-row scan in `planPatchDOM`.

---

## Root cause 2 (secondary): full visible-row pass in `patchDOM` every dispatch

Even when `planPatchDOM` returns empty patch lists for most rows, `patchDOM` still:

1. Calls `getVisibleInstanceIds` again (second preorder walk).
2. Calls `computeDepth` per row (parent-chain walk) — `src/Client/RowView.fs` ~17–25.
3. Queries DOM sibling order for **every** row (`atCorrectPos` / `insertBefore`) — `src/Client/View.fs` ~106–129.

This is **O(N × depth)** + **O(N) DOM queries** on every dispatch, including PollTick / sync chrome updates, even when zero rows moved.

---

## Root cause 3 (mouse pointer / paint): large flat DOM, no virtualization

- No client `mousemove` handlers (grep across `src/Client` and `wwwroot`).
- Rows have no `:hover` rules; bullets use static `title` tooltips (native browser), set only on row create/recreate (`src/Client/RowView.fs` ~184–185).
- **500+ flex rows** with multiple bordered children force the browser to do expensive hit-testing and layout on pointer movement and scroll regardless of app logic.

This explains slowness **without** keyboard input when the large list is merely visible.

---

## What is NOT the hot path

| Area | Finding |
|------|---------|
| Bullet tooltips | `bulletTip` runs at row create/recreate only; not on cursor move |
| Command dock | Snapshot-gated rebuild (`src/Client/CommandDock.fs` ~142–144) |
| Polling alone | PollTick changes `syncInfo` but still triggers full `planPatchDOM` scan (amplifies RC1, not primary user trigger) |
| `refreshDesktopFileIndicator` | O(1) on active file; not per-row |
| Virtualization | **None** — confirmed by design in `View.fs` |

---

## Recommended fixes (ranked)

### 1. Selection-only fast path in `planPatchDOM` / `patchDOM` (high impact, medium effort)

When the only model delta is `selectedNodes` / focus site (mode stays `Selecting`, graph/siteMap structure unchanged):

- Compute `{ oldFocused, newFocused, oldSelectedAncestors, newSelectedAncestors }` once.
- Emit `PatchRow` **only** for instance IDs whose selection/focus classes changed (typically 2–depth rows).
- **Skip** the full visible preorder loop and DOM position checks.

**Prediction:** CursorUp/Down latency becomes O(1) DOM patches regardless of N.

### 2. O(1) child index for selection checks (high impact, low–medium effort)

Replace `List.tryFindIndex ((=) entry.instanceId) parent.children` in `isInstanceDirectlySelected` / `isInstanceFocused` with:

- A cached `Map<SiteId, int>` child index rebuilt when parent's `children` list changes, **or**
- Store `childIndex: int` on `SiteEntry` at site-map build/expand time.

Also use this in `SiteMap.siteChildIndex` / `siteNext` / `sitePrev` (`src/Shared/ViewModel.fs` ~65–78) for navigation generally.

**Prediction:** Full `planPatchDOM` drops from O(N²) to O(N) when fast path is not used.

### 3. Skip DOM order verification when no structural mutations (medium impact, low effort)

If `mutations` contains only `PatchRow` with non-empty patches and no `CreateRow`/`RemoveRow`/`RecreateRow`, skip the per-row `atCorrectPos` / `insertBefore` loop in `View.patchDOM`.

### 4. Cache visible instance ID list on `VM` / `SiteMap` (medium impact, low effort)

Invalidate on fold/expand/zoom/graph/site-map rebuild. Avoids 2–3 preorder walks per dispatch (`moveSelectionBy`, `planPatchDOM`, `patchDOM`).

### 5. Row virtualization (highest impact for mouse + scroll, high effort)

Render only rows in viewport (+ buffer); reuse row DOM pool. Requires scroll container metrics and site-map-index ↔ scroll offset mapping. Addresses RC3 directly.

### 6. CSS indent instead of N indent divs (medium impact for mouse/paint, medium effort)

Replace per-depth `.amb-indent` divs with `padding-left: calc(var(--depth) * 1.5rem)` on `.amb-row`. Cuts ~depth DOM nodes per row.

---

## Quick experiments to confirm

1. **Console timing (browser devtools, `/ambit?debug=1`):** Wrap `ViewModel.planPatchDOM` call in `patchDOM` with `console.time` / `timeEnd`. Expand 500-child node; hold ArrowDown. Expect timer ∝ N².

2. **Temporary fast path:** In `planPatchDOM`, early-return `[]` when `oldModel.selectedNodes <> newModel.selectedNodes` and nothing else changed — patch focus/selected rows manually in a 5-line experiment. If cursor becomes instant, RC1 confirmed.

3. **DOM node count:** `document.querySelectorAll('.amb-row').length` with large node expanded vs collapsed. Correlates with mouse lag if RC3.

4. **Chrome Performance tab:** Record during ArrowDown — expect long Fable/JS frames in `planPatchDOM` / list `tryFindIndex`, not layout/paint (unless mouse-only case).

---

## Code reference index

| Mechanism | Location |
|-----------|----------|
| Dispatch → patch | `src/Client/App.fs` 586–631 |
| DOM patch loop | `src/Client/View.fs` 58–143 |
| O(n²) selection diff | `src/Shared/ViewModelDomPlan.fs` 30–139 |
| Selection index scan | `src/Shared/ViewModelRowState.fs` 14–61 |
| Move selection | `src/Shared/ViewModelSelection.fs` 238–259 |
| Visible row enumeration | `src/Shared/ViewModelSiteMap.fs` 376–417 |
| Row DOM build | `src/Client/RowView.fs` 131–228 |
| Focus scroll on nav | `src/Client/FocusView.fs` 50–63 |

---

## Top recommendation

Implement **#1 (selection-only fast path)** first — smallest behavioral surface, fixes the reported keyboard cursor slowness immediately. Follow with **#2 (O(1) child index)** so non-selection dispatches and shift-selection extensions don't regress at large N. Plan **#5 (virtualization)** if mouse-pointer lag remains after F# hot paths are fixed.
