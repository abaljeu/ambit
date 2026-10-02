# 04 — Browser want priority versus directory reconcile

**Type:** grilling
**Status:** defined
Blocked by: None

## 1. Question

When a Browser want and a Directory reconcile both need the one parse thread, which work runs first?

[browser-residency](../../browser-residency/project.md) computes Browser wants. This Project decides how the parse thread uses them. [05 — Selection-scoped Parse after whole-tree git Load](../../core-refinement/issues/05-selection-scoped-parse-after-whole-tree-git-load.md) says a selection push has no special priority. This ticket asks whether a Browser want differs from that selection rule when the other work is Directory reconcile.

Lock:

1. **Same rule or a different rule** — Does a Browser want use the same push-on-stack rule as selection, or does directory reconcile yield to the want?
2. **What yields** — If one yields, which work waits: the want, the Directory reconcile, or neither?
3. **Already on the stack** — When the Directory target is already pushed, does a new want reorder it?

How the Browser computes wants stays [browser-residency](../../browser-residency/project.md). See [[../map.md]] Out of scope.

## Comments

- 2026-10-01 — Filed from the directory/Load chart. Status `defined`. Open for grilling.
- 2026-10-01 — Alan: this is poll. Not addressing. Status stays `defined`.
