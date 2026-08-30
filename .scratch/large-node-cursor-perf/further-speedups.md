# Further speedups after selection fast path

Date: 2026-08-17  
Parent: [[investigation.md]], [[implement-fix.md]]

## 1. Model/view structure unchanged; only selection/focus

**Status after this fix:** Primary path implemented.

When `siteMap`/`graph` are the same references, both modes are `Selecting`, and workspace/sync chrome fields that feed row classes are unchanged, `planPatchDOM` patches only selection-appearance candidates (~2 rows for sibling CursorUp/Down). `patchDOM` skips the full visible walk and DOM order checks when mutations are non-structural.

### Residual opportunities (not implemented)

| Opportunity | Cost today | Notes |
| --- | --- | --- |
| No-op key at selection edge (`moveSelectionBy` returns same model) | Fast path returns `[]`; apply loop is empty. Still pays `planPatchDOM` eligibility checks + `renderSyncChrome` + dispatch overhead. | Tiny; optional early `obj.ReferenceEquals(old, new)` in `dispatch` would skip more. |
| `moveSelectionBy` visible-row list | O(N) `getVisibleRowInstanceIds` per key | Independent of DOM plan; cache visible ids on SiteMap if profiling still shows lag. |
| `desktopFileIndicator` refresh on every dispatch | May rewrite indicator fields even when selection unchanged | Does not force full plan (allowed to differ on selection path only when other gates pass — currently equality not required for indicator). Watch for accidental full-plan fallbacks if more chrome fields are gated. |

**Verdict:** Further gains here are incremental. Prefer visible-id cache only if HITL still feels lag after this ship.

## 2. Entering / exiting edit mode (no structural graph change)

**Status:** Not implemented; medium effort; worthwhile as a follow-up.

### What happens today

- `planPatchDOM` treats `wasEditing <> nowEditing` as `RecreateRow` for that instance (`ViewModelDomPlan.fs`).
- Selection fast path requires **both** modes `Selecting`, so Enter/Esc edit **falls through to the full visible walk**.
- With O(1) `childIndex`, that walk is O(N) selection checks + O(N) mutation list (mostly empty `PatchRow`s), then `patchDOM` does a full preorder DOM order pass because `RecreateRow` is structural.

So edit toggle is already **one-row DOM rebuild**, but planning/apply still scale with visible N.

### Possible further speedup

1. **Edit-toggle fast path** (mirror selection path): when `siteMap`/`graph` refs equal and the only meaningful deltas are `mode` Selecting↔Editing (and maybe `selectedNodes`), emit:
   - `RecreateRow` (or a cheaper `SetEditing` patch) for the single edit target
   - `PatchRow` class updates for selection-appearance candidates only
2. **Avoid full recreate** (optional, harder): replace contentEditable wiring in place (`SetEditing true/false`) instead of destroying the row. Touches `RowView.buildRowElement` / listeners / `manageFocus` / caret restore — higher risk.

### What is *not* needed for edit toggle

- Virtualization
- Changing fold/expand behavior

**Verdict:** Implement an edit-toggle planning fast path next if large-list Enter/Esc feels slow after selection fix. Prefer keep `RecreateRow` initially; skip the N-row plan/apply. In-place edit patch is optional polish.

## Recommended order

1. Ship selection fast path + `childIndex` (done).
2. HITL CursorUp/Down on 300–500 siblings.
3. If Enter/Esc on same list lags → edit-toggle fast path (plan-only first).
4. Visible-id cache / dispatch identity short-circuit only if still hot in profiles.
