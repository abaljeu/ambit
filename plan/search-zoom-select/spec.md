# Find pick: restore prior zoom; select target on leaf fallback

Stage: done
Updated: 2026-08-17

## Problem

An incorrect change routed Find pick through `ViewModel.focusNode` (always owner-parent framing). That must be reverted. The only intended delta vs the prior Find pick is selection when zoom falls back to the parent because the hit has no children.

## Terms

| Term | Meaning | Code anchor |
| --- | --- | --- |
| **Target** | The Node the user picked from Find results (`NodeSearchResult.nodeId`). | [[src/Shared/ViewModelSearch.fs]] `searchPickSetRoot` hit |
| **Zoom root** | `VM.zoomRoot`: the Node the site map is built from for the current view. | [[src/Shared/ViewModel.fs]] `zoomRoot` |
| **Leaf fallback** | Target has empty `children`; zoom uses structural parent (`Graph.tryFindParentAndIndex`) instead — same as Zoom-in on a leaf. | [[src/Client/UpdateOps.fs]] `zoomInOp` ≈600–608 |
| **Selection** | `VM.selectedNodes`: child span under the zoom-root site-map entry. | [[src/Shared/ViewModelSiteMap.fs]] `childSelectionAt`, `firstChildSelection` |

## Desired sequence

After Find confirms a pick (`findRootOp` → `searchPickSetRoot`):

1. Resolve **target** = picked `nodeId`.
2. Choose **zoom root** as before the focusNode change:
   - Target has children → zoom root = target.
   - Target has no children → zoom root = structural parent (or target if none).
3. Rebuild site map from that zoom root; seed `zoomIngress` with `ownerPathIngress` for the new zoom root.
4. Set **selection**:
   - Leaf fallback (zoomed parent) → select the **target** at its index under that parent (`childSelectionAt`).
   - Otherwise → `firstChildSelection` under the zoom root (prior behavior).
5. Enter `Selecting` mode.

## Current behavior (divergence)

[[src/Shared/ViewModelSearch.fs]] `searchPickSetRoot` currently calls `ViewModel.focusNode` always — owner parent as zoom root, select target. That is wrong.

Prior (correct zoom framing, wrong leaf selection):

```text
zoomId = parent if target.children empty else target
selectedNodes = firstChildSelection siteMap zoomId
```

Leaf fallback therefore often selected the **first** sibling, not the hit.

## Acceptance criteria

1. Non-empty-children target: zoom root = target; selection = first child under target (prior framing restored; not owner-parent / focusNode).
2. Empty-children target with a parent: zoom root = parent; focused selected Node id = target.
3. Empty-children non-first sibling: selection is that sibling, not the first child.
4. Shared non-leaf hit: zoom root = the shared node (prior), with owner-path ingress seeded for zoom-out — not forced to owner parent.
5. Zoom-in / Zoom-out / Zoom-owner commands unchanged; only Find on-pick selection on leaf fallback changes vs prior.
6. Shared.Tests cover (1) and (2)/(3); drop focusNode-only Find expectations.

## Out of scope

- Changing search discovery order.
- File-search dialog.
- Inventing new zoom framing beyond restoring pre-focusNode Find pick.
