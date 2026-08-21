# Merge Changes (proposed)

Not locked. Invariant and process shape only. No per-Op transform tables.

## Invariant

Never lose **critical information**, except when an Actor removed it — then merge may propagate that removal.

Critical:

- Changing a Node's text
- Adding a cssClass
- Adding a child edge
- Order of edges is important but **not** critical
- **Orphaning** a Node (no Owned path from ROOT) conflicts with changing its critical details
- Moving to TRASH does **not** conflict

Do not say **Deleted**. The as-implemented word is **Orphaned**.

## Single owner (document existing)

Every Node has a **single Owned** parent, except **ROOT** and except **Orphaned** Nodes. **Orphaned** = a subgraph not reachable by **Owned** child references. Orphaned Nodes stay in the Graph until GC. This is the Owned/Ref distinction ([[CONTEXT.md]]). **Which** Node is the owner is important but **not** critical.

Not a new delete model. Future Change merge must preserve this reachability.

Relate: adding a child edge is critical (do not drop the edge). Extra Owned → **Ref** (proposed). Dual-owner is illegal.

Facts: CONTEXT has no **Orphaned** entry. TRASH children are Owned by TRASH — not Orphaned. Hard delete removes a subtree already under TRASH with no outside refs. Startup GC removes Nodes unreachable from ROOT by **any** edge; a Ref-reachable Node with no owner is treated as a defect (promote a Ref) — [[.scratch/owner-edge-db-repair/]].

## Process

Two Changes, both defined on a **common prior** Local Graph. **Sequence** them. **Adjust the second** so it is compatible with the first, without destroying critical information.

In our words: Merge the second Change into a Local Graph that already includes the first. Transform is per-Op inside the second Change.

Who is first is not pinned: Server arrival vs local-already-applied. [[.scratch/relaxed-concurrency/map.md]] slices 2–3: Server apply; Browser merges the remote payload and replans the pending tail.

A **soft lock** does not change this process. It is a UX hint only ([[soft-lock.md]]). Merge still runs if Actors ignore it.

## Completing Ops (proposed)

A Local Subgraph Actor may drop an Owned edge and be unable to emit the promote-to-Owned Op. Server (fuller Local Graph) generates additional Changes and sends them via Poll. Pattern: [[server-fill-ops.md]].

## Facts (do not copy specs)

- `Op.SetText` / `Op.SetClasses` replace the whole field and CAS `old*` ([[src/Shared/GraphMutate.fs]]). There is no add-class Op.
- Child insert and move are `Op.Replace` spans. User remove-from-outline is usually **MoveToTrash** (still Owned). Hard delete only when already under TRASH with no refs outside ([[src/Shared/ViewModelDeleteOps.fs]]).
