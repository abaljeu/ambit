# ChildNode drop ref

## Destination

`ChildNode` remains (`children: ChildNode list`) but loses `ref` — id-only edges.
Ordered `children` stay the primary `structure.
`Node.owner` is the sole **in-memor`y** ownership source (≤1 owner; travels with a resident node, fitting selective load).
A **Loaded-scope** Graph/Op seam assures membership ↔ `Node.owner` only when the claimed owner parent is Resident with a Loaded child list; Absent/Unloaded → unprovable, accepted.
`Op.SetOwner` carries owner changes; `Replace` is id-only.
JSON: hard before/after per slice — encode both node `owner` and edge `ref` while both exist; later stop edge `ref`; no omit-`owner` / mixed-style messages.
DB keeps Ownership in `node_children.ownership` (no `nodes.owner` column); dual-Owner rows detected **before** collapse into one `Node.owner`.
Reached when edge `ref` is gone from the type and live paths classify from `Node.owner`, via progressive slices ([[plan/childnode-drop-ref/spine-draft.md]]).

## Notes

- Prior context: [[tmp/childnode-refactor.md]] Phases A+B on `w/childnode-ownership`. Phase C abandoned.
- Shared ctors: `ChildNode.owner` / `reference` / `ofOwnership` / `owners`.
- Progressive slices; wire is hard cutover per deploy; DB column remains bootstrap/store for Ownership.
- Plan by default; implement slice-by-slice on this branch.
- Selective load / Server authority as before.
- Skills: wayfinder, grilling, domain-modeling, implement-fsharp-feature.

## Decisions so far

- [Inventory edge ref sites](plan/childnode-drop-ref/issues/06-inventory-edge-ref-sites.md) — [[tmp/childnode-drop-ref-edge-ref-inventory.md]].
- [Detect dual-Owner before load collapse](plan/childnode-drop-ref/issues/08-detect-dual-owner-before-load-collapse.md) — lowest parent wins; extras → Ref.
- [Choose progressive migration spine](plan/childnode-drop-ref/issues/01-choose-progressive-migration-spine.md) — [[plan/childnode-drop-ref/spine-draft.md]].
- [Switch index build to Node.owner](plan/childnode-drop-ref/issues/04-switch-index-build-to-node-owner.md) — maps from `Node.owner` only.
- [Define Loaded-scope ownership seam](plan/childnode-drop-ref/issues/07-define-loaded-scope-ownership-seam.md) — provable → reject; mandatory steps 3/6 before drop-ref encode.
- [Define SetOwner op contract](plan/childnode-drop-ref/issues/02-define-setowner-op-contract.md) — header-only; old/new; NewNode owner arg; Change-complete Apply→Check→Undo; any Op order.
- [Reshape childOwnership API](plan/childnode-drop-ref/issues/05-reshape-childownership-api.md) — `(parentId, child: Node)` at step 5.
- [Define JSON owner-then-drop-ref wire windows](plan/childnode-drop-ref/issues/09-define-json-owner-then-drop-ref-wire-windows.md) — encode both; hard cutovers; no omit-compat.
- [Define load bootstrap without edge ref](plan/childnode-drop-ref/issues/03-define-load-bootstrap-without-edge-ref.md) — DB keeps Ownership on `node_children`; Load sets `Node.owner` from it.
- **ChildNode ctors** — shrink `owner` / `reference` to id aliases, then delete helpers in a cleanup slice (with step 8).
- **Planner/cold-parse** — after step 5, emit `SetOwner` + id-only `Replace` only (no proposed Owned mark / side-channel).

## Not yet specified

- **Duplicate (link) HITL acceptance** — after the ownership migration changes Browser ownership classification, rebuild Fable and the bundle, hard-reload, then verify that Duplicate (link) on an Owned appearance of a Normal Node leaves a sibling Ref. Retained from [[tmp/duplicate-link-double-fail.md]].
- **Dual-Owned parent acceptance** — retain the Ref-only Duplicate case where the Replace parent has two Owned appearances; ownership validation must not reject the Change because it did not change that parent’s ownership. Retained from [[tmp/validate-ownership-change-scope.md]].
- Later: drop DB `ownership` column or add `nodes.owner` (beyond near destination).

## Out of scope

- Deleting `ChildNode` or changing `children` to `NodeId list` (old Phase C).
- Renaming `ChildNode`.
- Adding a persisted `nodes.owner` column in this effort’s near destination.
- Split owned-tree vs Ref-multimap storage that demotes outline order to a merge/view.
