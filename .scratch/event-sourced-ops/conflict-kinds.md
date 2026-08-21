# Conflict kinds (proposed)

Not locked. Names only — no merge algorithm. Merge process and preserve-information invariant: [[merge.md]]. Cite [[.scratch/relaxed-concurrency/map.md]] for existing apply facts; do not copy that spec.

Independence is the load-bearing idea: if two Changes do not conflict, Actors can work those areas and merge is simple.

## User sketch

- Edits that do not touch the same nodes, or do not touch outgoing edges of the same nodes, are safe.
- Same node, same field → conflict.
- Edit to a node's children → potential conflict, depends.
- Two Actors both adding a child under the same parent → accept in either order.

## Logical slip

"Don't touch the same nodes **or** don't touch outgoing edges" fights the two-add case. Adding a child **is** writing the parent's outgoing edges. Two adds also both "touch" the parent id (as parent); the new Node ids differ.

Independence is not a set of Node ids. Split **node fields** from **child-list / outgoing edges**. Child-list is not one conflict class.

## Proposed kinds (not locked)

1. **Disjoint field / disjoint parent-list** — no conflict. Different Nodes' fields, or child-lists of different parents.
2. **Same node, same field** — conflict. (Today: attribute CAS on text, classes, name, documentState. `SetUpdateTime` ignores mismatch — not this class.)
3. **Same parent child-list** — not automatically a conflict.
   - **Insert + insert** — no conflict; either order OK.
   - **Overlapping Replace / delete / reorder** — conflict, or "depends" (pin later).

An Actor's "area" should be node fields and/or child-list spans, not a Subgraph blob. Two Actors under one parent then need not look overlapping.

## Facts (do not copy the spec)

Ops are already per-Node field or per-parent child list. Attribute Ops CAS the field. `Replace` CAS a span of one parent's children. Distinct parents do not interact. Same-parent structural overlap is the remaining class. Global revision gate treats *all* concurrent Changes as conflicting — that is what relaxed-concurrency drops.
