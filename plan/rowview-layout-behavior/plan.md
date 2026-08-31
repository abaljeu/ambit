# RowView / FocusView — layout vs behavior cohesion

Prerequisite base: post-refactor modules from the split-view-by-concern effort (`RowView`, `FocusView`, thin `View` orchestrator). As of plan writing, sibling modules already exist beside an still-intact `View`; implementers must wait until that split is compiled and `View` is thin before starting these steps.

## Problem Statement

After the concern split, `RowView` still mixes two jobs in the same functions: building the physical row DOM (structure, classes, text, indicators) and assigning interactive behavior (event listeners, scroll-defer flags/timers, dispatch). `FocusView.manageFocus` is already mostly “behavior after paint,” but it shares mutable scroll-defer state with row click handlers buried inside `makeRowElement`. Zoom-path sync similarly rebuilds breadcrumb DOM and wires clicks in one pass.

That coupling makes it hard to reason about “what does this row look like?” versus “what does this row do when clicked / focused?” Patches already update appearance only (`applyRowPatches`); recreate is what refreshes listeners — but that invariant is implicit in one tangled builder, not expressed as two cohesive groups.

The developer wants more cohesion **within RowView and FocusView only**: one group constructs physical layout; the other assigns behavior. Behavior-identical, surgical steps. CommandDock, Overlays, and App wiring are out of scope except as needed for this cohesion.

## Solution

Keep the public orchestration surface stable (`makeRowElement`, `resolveRow`, `syncZoomPath`, `manageFocus`, scroll helpers) while introducing an explicit **layout vs behavior** seam inside those modules:

1. **Layout group** — build or patch DOM structure, classes, text content, and indicators. No `addEventListener`, no dispatch, no scroll-defer mutation.
2. **Behavior group** — attach listeners, own scroll-defer / fold-toggle timers, focus/caret/scroll after transitions, dispatch wiring.

Recommended shape (Approach A below): nested modules inside the existing `RowView` / `FocusView` homes (no new fsproj siblings unless a later decision prefers files). `makeRowElement` becomes a thin compose: layout skeleton → wire behavior. `applyRowPatches` stays layout-only. `FocusView` remains the post-patch focus/caret/scroll home and consumes scroll-defer APIs from the behavior group.

## Options considered

### Approach A — Two-phase compose inside existing modules (recommended)

Inside `RowView`, add nested groups (names TBD: Layout / Behavior, or Chrome / Interaction). Extract:

- Layout: depth helper, zoom-path element query/rebuild (DOM only), row skeleton builder, CSS class sync, `applyRowPatches`, structure queries used by `View` (`firstRowAnchor`, etc.).
- Behavior: scroll-defer + fold-toggle timers, `wireRow` (listeners + defer flags), zoom-path click wiring, exports Focus needs (`cancelPendingSelectionScroll`, `deferSelectionScroll`, `scrollFocusedRow`).

`makeRowElement` / `syncZoomPath` stay as compose functions so `View` / `resolveRow` call sites do not churn.

**Pros:** Surgical; behavior-identical; matches current file ownership; tiny steps with re-exports.  
**Cons:** Nested modules can feel less “physical” than separate files; names still need a decision.

### Approach B — New sibling modules (extra files)

Split into dedicated siblings compiled between StatusView and View (e.g. row layout module + row behavior module + FocusView). Focus opens behavior; View opens layout + resolve glue.

**Pros:** Clearest filesystem cohesion; mirrors StatusView-style one-concern-per-file.  
**Cons:** Second fsproj/order pass soon after split-view-by-concern; more open/wiring churn; higher merge risk while the first split lands.

### Approach C — Handler injection into layout

Layout builders take callbacks (`onActivate`, `onFold`, …) instead of a separate wire phase. Layout still creates nodes; callers supply behavior closures.

**Pros:** Layout never imports dispatch/UpdateOps; easy to unit-test builders with fake handlers if DOM tests appear later.  
**Cons:** Large signature surface; more invasive first steps; listeners still attached “inside” the layout call stack unless carefully staged — weaker visual separation than an explicit wire phase.

**Decision for this plan:** implement **Approach A**. Revisit Approach B only if nested modules prove awkward after a few steps. Reject Approach C for this pass (too much signature churn for the same cohesion goal).

## Exact scope

### Moves / stays

| Concern | Home after refactor | Notes |
| --- | --- | --- |
| `computeDepth`, indent/text/guid/file-indicator construction, row class attributes | Layout group in RowView | Pure structure + content |
| `syncUserCssClasses`, `applyRowPatches` | Layout group | Already appearance-only; keep that way |
| `zoomPathEl`, `firstRowAnchor`, zoom-path DOM rebuild | Layout group | Remove/create segments, classes, text |
| Zoom-path segment `click` → dispatch | Behavior group | Wired after layout rebuild |
| Fold/leaf element creation (classes, glyphs) | Layout group | |
| Fold toggle timers + mousedown/dblclick | Behavior group | |
| Edit textDiv listeners (keydown, paste/copy/cut, stopPropagation) | Behavior group | |
| Selecting activate / double-click on text or leaf | Behavior group | Sets scroll-defer flags |
| Scroll-defer mutable state + `scrollFocusedRow` | Behavior group in RowView | FocusView continues to read/write via existing internal exports |
| `manageFocus` (focus, caret, scroll-into-view) | FocusView (behavior) | Already the right home |
| `resolveRow` | RowView orchestrator | Still create/recreate/patch; calls compose `makeRowElement` |
| `render` / `patchDOM` | Thin View | Unchanged call pattern; no App opens of RowView/FocusView required |

### Invariants to preserve

- `RecreateRow` (editing transition, children-indicator change, kind change) remains the path that refreshes listeners, because patches never rebind events.
- `PatchRow` remains appearance-only.
- Double-tap scroll defer (400ms) and fold-toggle defer stay timing-identical.
- Dead/commented paths in thin `View` are untouched by this effort.

## Steps

Each step leaves the Client buildable and behavior-identical. Prefer Approach A nesting; do not start until split-view-by-concern has `RowView` / `FocusView` on the compile path and `View` is the thin orchestrator.

1. **Confirm base** — Build Client; smoke that rows, fold/double-tap, edit caret, and selection scroll match pre-split behavior. Stop if the split is incomplete.
2. **Name the groups** — Pick nested module names (recommendation: `Layout` and `Behavior`) and document them in Further Notes if they differ. No code move yet beyond empty nested modules + `open` aliases if useful.
3. **Move pure layout helpers** — Relocate `computeDepth`, `zoomPathEl`, `firstRowAnchor`, and `syncUserCssClasses` into the Layout group. Re-export at RowView top level so FocusView/View call sites stay stable. Build.
4. **Move `applyRowPatches` into Layout** — Keep private; `resolveRow` still calls it. Build.
5. **Extract row skeleton builder** — From `makeRowElement`, pull a Layout function that creates the row element tree (classes, indents, fold/leaf spans, text div content/editability attributes, name, file indicator) **without** any `addEventListener` or scroll-defer writes. `makeRowElement` still attaches listeners afterward (temporarily duplicated structure ownership is OK if compose is clear). Build + smoke row render/patch.
6. **Extract `wireRow`** — Move all row listeners and defer-flag writes from `makeRowElement` into a Behavior function that takes the built row (and needed model/dispatch/entry context). `makeRowElement` = layout then wire. Build + smoke activate, double-click, fold, edit keys/clipboard.
7. **Split zoom-path sync** — Layout rebuilds path DOM; Behavior attaches segment clicks. Public `syncZoomPath` remains the compose entry used by View. Build + smoke zoom ingress clicks.
8. **Move timer/scroll-defer state into Behavior** — Fold-toggle and selection-scroll mutables + helpers live with Behavior; keep `internal` exports FocusView already uses. FocusView opens stay equivalent. Build + smoke selection scroll defer vs immediate, edit cancel of pending scroll.
9. **FocusView pass** — Confirm `manageFocus` only does post-paint behavior; add a short module comment stating it is the focus/caret/scroll behavior surface and depends on RowView Behavior for defer. No logic change unless a stray layout concern is found (none expected).
10. **Optional cleanup** — Collapse any temporary re-export shims that are no longer needed by View/FocusView; keep public compose names if they still clarify the orchestrator. Do not rename exports just for aesthetics in this pass.
11. **Verify** — `dotnet build` Client; manual smoke: create/patch/recreate rows, fold + double-tap zoom, edit caret/scroll, selecting navigation scroll, zoom path, wheel without snap-back on non-navigation dispatches.

## Decision Document

- Scope is limited to the row DOM module and the focus/caret/scroll module produced by the prior concern split; dock, overlays, and app entry wiring are not redesigned.
- Cohesion target is two groups: physical layout construction versus behavior assignment (listeners, defer timers, focus/caret/scroll, dispatch wiring).
- Preferred structure is nested groups inside the existing modules (Approach A), not new sibling files and not callback-injection into layout (Approaches B and C deferred/rejected for this pass).
- Public compose entry points used by the thin view orchestrator stay name-stable so the orchestrator does not need a redesign.
- Appearance patches remain listener-free; recreate remains the listener-refresh path. The refactor must not start rebinding events on patch.
- Scroll-defer and fold-toggle mutable state stay owned by the row behavior group; the focus module continues to collaborate through the same internal surface rather than duplicating timers.
- Zoom-path click wiring is behavior; zoom-path element structure and text are layout.
- Naming of nested groups defaults to Layout and Behavior unless implementers agree on Chrome/Interaction before step 2.
- No intentional runtime behavior changes; verification is build plus manual smoke parity.
- Shared pure planners and focus gates are not relocated; this is a Client DOM cohesion refactor only.

## Testing Decisions

### What makes a good test here

- Prefer testing **external behavior** and **pure planners/gates**, not whether a listener was attached in function A vs B.
- Do not assert nested module structure or private wire helpers.
- Client DOM attach/focus/scroll is awkward under current Shared.Tests; do not invent a Fable DOM harness solely for this refactor unless the user expands testing scope.

### Coverage today

- **Present (Shared):** `ManageFocus.shouldInvoke`, `EditingCaretPreserve.shouldPreserveDomCaret`, row presentation helpers (`rowChildrenIndicator`, ownership/sync classes, etc.), and DOM mutation planning (`planPatchDOM` / recreate-vs-patch rules). These already lock the contracts this UI relies on.
- **Absent:** No automated tests for `makeRowElement`, `applyRowPatches`, `syncZoomPath`, scroll-defer timers, or `manageFocus` DOM side effects.

### Plan for this refactor

- Rely on existing Shared tests (no change required for a behavior-identical move).
- Primary verification: Client build + manual smoke checklist in Steps.
- Optional later (out of scope unless requested): extract pure “which listeners does this row mode need?” decisions into Shared and unit-test those; still keep Browser wiring untested.

## Out of Scope

- CommandDock, Overlays, StatusView, SearchDialogView redesign.
- Changing `planPatchDOM` / `RowPatch` vocabulary or recreate triggers.
- App.fs open list changes beyond what a failed compile forces after the prerequisite split.
- Editing the split-view-by-concern plan file.
- New Client DOM test infrastructure.
- Removing dead/commented blocks in the thin view orchestrator.
- Visual/CSS redesign, new indicators, or behavior changes to double-tap / fold / caret.
- Merging FocusView back into RowView, or moving layout into Shared.

## Further Notes

### Prerequisite risk

Sibling modules may exist on disk while `View` still contains the old private copies and the client project file may not list the new files yet. This plan assumes the concern split reaches “thin View + compiled RowView/FocusView” before step 1.

### Coupling map (for implementers)

- Writers of `deferSelectionScroll`: row activate / double-click (behavior); clear on edit focus path (`manageFocus`) and on double-click.
- Readers: `scrollFocusedRow` used from `manageFocus` when the focused site changes under Selecting.
- Fold-toggle timer is private to row behavior today; keep it there.
- `applyRowPatches` never touches listeners — preserve.

### Open decisions (lightweight)

1. Nested module names: Layout/Behavior vs Chrome/Interaction.
2. Whether Approach B (new files) is worth a follow-up after Approach A lands — default no.
3. Whether to add Shared “listener intent” pure helpers later for testability — default no for this pass.
