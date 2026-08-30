# Inventory edge ref sites

Type: research
Status: resolved

## Question

What are the production (and test-helper) sites that still construct, read, compare, or persist `ChildNode.ref` / edge ownership — grouped so the migration spine can draw compile-safe slice boundaries?

## Notes

Evidence anchors: [[tmp/childnode-refactor.md]], `Node.childOwnership` in [[src/Shared/Model.fs]], `GraphBuild` owner-map fold still on `child.ref`, `node_children.ownership` in [[src/Server/Database.fs]], projection write via `Node.childOwnership` in [[src/Shared/GraphProjection.fs]]. Broader name-mention census (Phase C framing obsolete): [[tmp/childnode-wayfinder-inventory.md]].

## Answer

Grouped inventory and spine-oriented cuts: [[tmp/childnode-drop-ref-edge-ref-inventory.md]] (summary: [[tmp/Inventory-edge-ref-sites.md]]).

Headline facts: no `Op.SetOwner`; JSON omits `Node.owner` (edge `ref` only); load collapses dual-Owner silently (`Map.add` last-wins) with no pre-collapse detect. Hard `child.ref` deps: GraphBuild index/`appendChildren`, ColdParse proposed-edge paths, Serialization + DB `node_children.ownership`, equality helpers, DeleteOps promote, Client `duplicateSelectionOp`. Most other sites go through `Node.childOwnership` (dual-read).
