# ChildNode drop ref

## Destination

`ChildNode` remains (`children: ChildNode list`) but loses `ref` — id-only edges.
Ordered `children` stay the primary structure. `Node.owner` is the sole ownership
source (≤1 owner is structural and travels with a resident node, fitting selective
load). A **Loaded-scope** Graph/Op seam assures membership ↔ `Node.owner` when the
relevant child lists are Loaded; disagreement under an Unloaded owner parent stays
unprovable and accepted. `Op.SetOwner` carries owner changes; `Replace` is id-only.
JSON: first encode/decode `Node.owner` on the node (absent from wire today); migrate
readers off edge `ref=Owner`; only then stop encoding edge `ref` (decode may tolerate
old edge `ref` / missing node `owner` for a bounded window).
DB `node_children.ownership` stays as load bootstrap (no `nodes.owner` column yet);
dual-Owner rows are detected **before** collapse into one `Node.owner`.
Reached when edge `ref` is gone from the type and all live paths classify ownership
from `Node.owner`, via a **progressive** migration (no oneshot field deletion).

## Notes

- Prior context: [[tmp/childnode-refactor.md]] Phases A+B on `w/childnode-ownership`.
  Phase C (delete `ChildNode` / `NodeId list`) abandoned for this effort.
- Shared ctors already exist: `ChildNode.owner` / `reference` / `ofOwnership` / `owners`.
- **Progressive only** — each slice must leave the tree green; dual-read/write windows
  are allowed when a ticket says so. Do not oneshot-delete `ref`.
- Plan by default (decision tickets). Implementation may follow the resolved spine
  slice-by-slice on this branch; do not treat the whole destination as one Change.
- Selective load: not every node or child list is Loaded; a Loaded child list implies
  those child nodes (and their `owner` fields) are resident. Prefer sole `Node.owner`
  over a required dual-Owner seam on edge `ref` alone — edge dual-Owner is unprovable
  across Unloaded parents.
- Skills: [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/grilling/SKILL.md]],
  [[.agents/skills/domain-modeling/SKILL.md]], [[.cursor/skills/implement-fsharp-feature/SKILL.md]].
- Destination locks from grill / integrity review (not yet ticket answers): keep type +
  name; order primary; owner-only truth; Loaded-scope membership seam; ops set
  `Node.owner` via `SetOwner`; Replace id-only; JSON owner-field-first then drop edge
  `ref`; DB ownership column kept as bootstrap with pre-collapse dual-Owner detection.

## Decisions so far

## Not yet specified

- Whether `ChildNode.owner` / `reference` ctors shrink to id aliases then vanish, or
  rename in a late slice.
- Planner/cold-parse paths that still classify proposed edges via `child.ref` —
  exact slice boundaries after the spine lands.
- Later: drop DB `ownership` column or add `nodes.owner` (explicitly beyond near destination).

## Out of scope

- Deleting `ChildNode` or changing `children` to `NodeId list` (old Phase C).
- Renaming `ChildNode`.
- Adding a persisted `nodes.owner` column in this effort’s near destination.
- Split owned-tree vs Ref-multimap storage that demotes outline order to a merge/view.
