# Find pick: zoom to owner, select target

Stage: done
Updated: 2026-08-17

## Problem

An earlier reading of Find (/) behavior was wrong. After the user picks a search hit, Gambol must **reframe zoom at the hit's owner parent**, then **select the hit**. It must not treat Find like Zoom-in (zoom into the hit when it has Children).

## Terms

| Term | Meaning | Code anchor |
| --- | --- | --- |
| **Target** | The Node the user picked from Find results (`NodeSearchResult.nodeId`). | [[src/Shared/ViewModelSearch.fs]] `searchPickSetRoot` hit |
| **Owner parent** | The unique parent that holds the target via an Owned child edge (`graph.ownerParentByChild`). Not a Ref parent. | [[src/Shared/ViewModelOccurrence.fs]] `getOwnerOccurrence`, `tryReframeZoomAtOwnerParent` |
| **Zoom root** | `VM.zoomRoot`: the Node the site map is built from for the current view. | [[src/Shared/ViewModel.fs]] `zoomRoot` |
| **Selection** | `VM.selectedNodes`: child span under the zoom-root site-map entry. | [[src/Shared/ViewModelSiteMap.fs]] `childSelectionAt` |

Glossary: prefer **Owned** over spoken “Owner” for the child role ([[CONTEXT.md]]); “owner parent” here means the parent Node of that Owned placement.

## Desired sequence

After Find confirms a pick (`findRootOp` → `searchPickSetRoot`):

1. Resolve **target** = picked `nodeId`.
2. Set **zoom root** = owner parent of target (same framing as `tryReframeZoomAtOwnerParent` / `focusNode`).
3. Rebuild site map from that zoom root; seed `zoomIngress` with `ownerPathIngress` for the new zoom root.
4. Set **selection** to the Owned child slot of the target under that owner parent (`childSelectionAt` at the owner index).
5. Enter `Selecting` mode.

When target is ROOT, or has no owner parent: no-op / leave model unchanged (same None path as `tryFocusNodeOccurrence` for ROOT).

When the target has only Ref parents (no Owned edge): fall back like `tryFocusNodeOccurrence` — any parent occurrence, zoom to that parent, select the target there.

Reference implementation already exists for steps 2–4: [[src/Shared/ViewModelOccurrence.fs]] `focusNode` / `tryFocusNodeOccurrence` (lines 30–53, 125–136). Find should use that framing, not Zoom-in’s leaf/non-leaf split.

## Current behavior (divergence)

Wire-up: [[src/Client/UpdateOps.fs]] `findRootOp` opens Find with on-pick `ViewModelSearch.searchPickSetRoot` (≈688–690).

[[src/Shared/ViewModelSearch.fs]] `searchPickSetRoot` (≈188–207) today:

1. If target has **no Children** (leaf): `zoomRoot` = structural parent from `Graph.tryFindParentAndIndex`; selection = `firstChildSelection` under that parent.
2. If target **has Children**: `zoomRoot` = **target itself**; selection = `firstChildSelection` under the target (first child, not the target).

That matches Zoom-in framing ([[src/Client/UpdateOps.fs]] zoom-in ≈600–623), not owner-focus framing (`zoomOwnerOp` / `focusNode`).

| Case | Current | Desired |
| --- | --- | --- |
| Leaf, first child under parent | Zoom parent; selection often equals target by accident | Zoom owner parent; select target at owner index |
| Leaf, not first child | Zoom parent; selects **first** child (wrong target) | Zoom owner parent; select **target** |
| Non-leaf (incl. under TRASH) | Zoom **into** target; select its first child | Zoom to **owner parent**; select **target** |
| Shared Node (Owned + Ref) | Non-leaf: zoom into shared Node | Zoom to **owner** parent; select shared (see `focusNode` tests) |

Tests that encode current non-leaf / leaf framing: [[tests/Shared.Tests/ViewModelTests.fs]] `searchPickSetRoot …` (≈2194–2227, 2431–2448). Owner-focus tests already state the desired shape: `tryReframeZoomAtOwnerParent` / `focusNode` (≈2247–2270).

## Acceptance criteria

1. Picking a Find hit always leaves `zoomRoot` equal to the target’s owner parent when an Owned parent exists (else the fallback parent from `tryFocusNodeOccurrence`).
2. After pick, focused selected Node id equals the picked target (not its first child, not an unrelated sibling).
3. Non-leaf targets are **not** used as zoom root solely because they have Children.
4. Shared (Owned + Ref) targets zoom to the **Owned** parent and select the target there (parity with `focusNode`).
5. ~~dropped~~
6. Existing Zoom-in / Zoom-out / Zoom-owner commands keep their current contracts; only Find’s on-pick path changes.
7. Shared.Tests updated: replace or rewrite `searchPickSetRoot` expectations to match owner-then-select; keep / align with owner-focus tests.

## Out of scope

- Changing search discovery order (zoom-subtree then ROOT BFS in [[src/Shared/ViewModelSearch.fs]]).
- File-search dialog (`FileSearchDialog` / `ViewModelFileSearch`).
- Implementing the change in this project stage (spec only until tickets / active work).

## Suggested implementation note

Prefer routing Find on-pick through `focusNode` or `tryFocusNodeOccurrence` (plus any dialog-close / mode cleanup the dialog already does) instead of duplicating leaf/non-leaf zoom-in logic in `searchPickSetRoot`.
