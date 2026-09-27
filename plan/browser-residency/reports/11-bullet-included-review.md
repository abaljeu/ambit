# 11 — Migrate Bullet and Included readers — review

Range: uncommitted vs `HEAD` (`git diff HEAD`). Mechanical Standards scan: `scan: none` (empty stdout, exit 0). Spec source: [11 — Migrate Bullet and Included readers](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md), plus modules **Bullet** and **Included** in [Browser residency architecture](plan/browser-residency/arch.md) and Solution **Hollow-circle Bullet** in [Browser residency specification](plan/browser-residency/spec.md). This report is not approval. Ticket Status stays `coded`.

## Standards

No findings.

The scan printed no long lines, no TAB, no `mutable` in added source, and no file over the 800-line cap. [ViewModelChildrenIndicator.fs](src/Shared/ViewModelChildrenIndicator.fs) `rowChildrenIndicator` is 8 lines. [IncludedDescendantIds.fs](src/Shared/IncludedDescendantIds.fs) `expand` is 25 lines, including nested `walk` and `addChildren`. Added lines stay under 100 characters. Production hunks stay pure: `match` on `GraphChildren.tryGet`, no Exceptions, no Node-children field. `GraphChildren.tryGet` and `GraphChildren.get` read `graph.childMap` ([Model.fs](src/Shared/Model.fs) module `GraphChildren`). An unchanged test still uses `let mutable seen` as a formatter spy; that line is not in this diff.

## Spec

No findings.

**Bullet.** The ticket says: “The hollow-circle Bullet follows absent `childMap` or Unparsed.” New `rowChildrenIndicator` returns `HollowCircle` for `Unparsed` first, then `None` (absent `childMap`) as Unloaded, `Some []` as a Loaded leaf (`SolidCircle`), and `Some _` as `FoldChevron`. That matches **11.2.2 — Absent key is hollow Unloaded**, **11.2.1 — Unparsed stays hollow**, **26.2 — Children arrive**, **34.1 — Unloaded is not empty**, and **12.2 — No new loading Status**. There is no new per-Node loading Status and no new command that names Nodes (**32.1 — No new named-Node command**). Unparsed with resident Children now stays hollow; HEAD painted `FoldChevron`. That matches **26.2 — Children arrive** (“chevron or solid replaces hollow when Loaded and not Unparsed”), not specification story **11 — Hollow Unparsed Bullet**’s “as today” if today means HEAD. The ticket is the originating spec for this range.

**Included.** The ticket says: “`expand` walks `childMap`, not Node children” and “stop at folded children.” HEAD already called `GraphChildren.get`. This diff only drops `Map.tryFind nodeId graph.nodes` before the walk. Children still come from `childMap`. Fold still comes from `entry.expanded` and the zip with `entry.children`. `GraphChildren.get` treats an absent key as `[]`, so an Unloaded parent adds no descendants. That is the correct walk. No retired Node-children residency read remains: `Node` has no children field.

**Out of product scope.** The production and test hunks stay on Bullet and Included readers. No Server Sync doors, no Browser Poll/post-Event/Boot, no Want compose, and no Load-package contract work from later tickets.

## Test seams

Indicator proof is adequate at the Shared seam in [ViewModelRowStateTests.fs](tests/Shared.Tests/ViewModelRowStateTests.fs):

1. Absent key — Unloaded — `unload` then `HollowCircle`.
2. Empty Loaded — detached Node (Loaded `[]`) then `SolidCircle`.
3. Populated Loaded — `setChildren` then `FoldChevron`.
4. Unparsed leaf — Unparsed plus Loaded `[]` then `HollowCircle`.
5. Unparsed with resident Children — assertion changed from `FoldChevron` to `HollowCircle`.
6. `planPatchDOM` still recreates the row when a leaf becomes hollow and when Unloaded becomes Loaded with Children.

Included proof for **33.1 — Honor Fold** is pre-existing only. [IncludedDescendantIdsTests.fs](tests/Shared.Tests/IncludedDescendantIdsTests.fs) is not in the diff. Those tests still start at Zoom, walk unfolded Children from `Graph.replace` (childMap), and stop at folded Children. They do not pin the removed `graph.nodes` header guard, and they do not `unload` a parent then `expand`. That gap is not a ticket miss: HEAD already walked `childMap`, and Fold-plus-Children remains covered.

## Out of scope (working tree)

Outside the named code files, the same `HEAD` diff also edits [11 — Migrate Bullet and Included readers](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md) (Status `coded`, checkboxes, `Actual` / Time) and [project.md](plan/browser-residency/project.md) (`Actual: 7h 07m`). Those are Status and time bookkeeping, not extra product behavior.

## Summary

Standards: 0 findings. Spec: 0 findings. No blocking issue on either axis.
