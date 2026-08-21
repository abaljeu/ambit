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

**Classes (accepted):** a Node's classes are a **set of names**. Merge applies a **set delta** (add/remove vs the common prior), not whole-set replace. From a common prior, adds and removes are disjoint, so concurrent class edits are independent — no loss neither Actor intended. Compute the delta from `(common prior, this Actor's new set)`, never from the already-merged set.

Implemented `Op.SetClasses` is still whole-set replace + CAS. That is **not** this spec.

**Children (accepted):** default / happy path is a **positional Replace** — specified position, specified nodes. That is the posted Op.

On conflicting child-list edits, do not reject the Change as the whole story. Compute a best approximation. The **critical** invariant is occurrence-bag **Accept Both** vs the common prior: adds = new slots, removes = prior slots; those sets are disjoint, like class deltas. Order is important but **not** critical. Approximation algorithm is later — no per-Op tables here.

A parent's children are a **bag of occurrences** (edges / child slots), not a bag of Node ids. If the bag were Node ids only, add-another-X vs remove-X can cancel: neither-intended / identity confusion. That reading is rejected.

The Server **amends** the newest Actor's posted Replace so it applies after the **common prior** plus **other Actors' accepted Changes**. The result is a definitive ordered-list Replace for that combined Local Graph — not a rewrite against isolated node fields, and not a substitute for sending those other Changes. Completing Ops ([[server-fill-ops.md]]) fill missing Ops in **that** Change; they are not this order. Client consume is rewind+replay ([[#Client correction]]), not `SetText C→B`. Order: [[#Amendment order]].

`amb-conflict` is a **node indicator** (text and name), not an edge-edit device. The child is a **Normal Node**; conflict-ness is the `amb-conflict` name/role, not a Kind. Do not add conflict children for child-list conflicts. Do not invent a Conflict Kind.

Move still respects owner count: add on one parent and remove on another; a well-formed Change does not 1→2.

Implemented `Op.Replace` (overlapping-span CAS, reject on mismatch) is **not** this spec. It is as-implemented **behavior to beat** — same role as genesis-replay and whole-set `SetClasses`. Do not treat it as a fact that stands in for the spec.

**Same text (accepted):** prior `A`; Server arrival is first (`A→B` stays on the Node). Second Change is rewritten: drop `SetText A→C`; **Add Node** as first child, class `amb-conflict`, text `C` (in that Change). Every Local Graph converges to that. Not HITL-dialog, not reject, not LWW.

Optimistic Client correction is **rewind + replay** ([[#Client correction]]), not a field rematch. The Client is at `C`. Rewind to the common prior (text `A`). Replay other accepted Changes (`SetText A→B`) then the newest amended Change (Add Node `amb-conflict` / `C`). Do **not** rematch with local-as-first (that would put `B` in the child and diverge). `SetText C→B` is **not** the strategy. A later increment may prove an in-place transform identical to rewind+replay; until then it is not equal.

`SetText A→B` is the Server/common-prior Op. After rewind it applies against the common prior.

**Name (accepted):** concurrent `SetName` — Server arrival is first (that name stays on the Node). Second Change is rewritten: drop `SetName`; **Add Node** as first child, class `amb-conflict`, text = the new name. That child is a **Normal Node**. Merge success, not HTTP Reject. Same family as same-text. Report: [[name-clash-amb-conflict.md]].

**DocumentState (accepted):** the field is **deleted**. Not a merge case. **NoServerFile** is inferred from whether a file exists. **Unparsed** is inferred from the relative dates of the file and the File Node. Implemented `Op.SetDocumentState` / `Node.documentState` remain facts until removed.

Do not say **Deleted**. The as-implemented word is **Orphaned**.

## Single owner (document existing)

Every Node has a **single Owned** parent, except **ROOT** and except **Orphaned** Nodes. **Orphaned** = a subgraph not reachable by **Owned** child references. Orphaned Nodes stay in the Graph until GC. This is the Owned/Ref distinction ([[CONTEXT.md]]). **Which** Node is the owner is important but **not** critical.

Not a new delete model. Future Change merge must preserve this reachability.

**Owner count (accepted):** a well-formed Change does not raise any Node from 1 Owned parent to 2. **Move** adds ownership on one parent and removes it on another — one Op may do both. **Delete** removes ownership; **Undo** puts it back. Two concurrent Moves of the same Node still leave owner count 1 (adjust the second). Dual-Own is not a merge case.

Bug net: if a Change would increase owner 1→2, extra Owned → **Ref**. Which owner is not critical.

Facts: CONTEXT has no **Orphaned** entry. TRASH children are Owned by TRASH — not Orphaned. Hard delete removes a subtree already under TRASH with no outside refs. Startup GC removes Nodes unreachable from ROOT by **any** edge; a Ref-reachable Node with no owner is treated as a defect (promote a Ref) — [[.scratch/owner-edge-db-repair/]].

## Amendment order

**Accepted (order).** Per-Op tables still later. The change-amendment strategy is **not** valid if it injects corrections that are true only to the **nodes in question** and omits other Actors' accepted data.

1. Take the **common prior** Local Graph (the base).
2. Apply the **accepted Changes from the other Actors** (every Op in those Changes).
3. Apply the **newest Actor contributor's** Change, **amending** its Ops so they are compatible with that combined state — without destroying critical information.

**Newest** = the Change being merged in now. Server arrival sequences who is first. For more than two concurrent Changes, each next Change is newest relative to those already accepted.

The **Server must** produce this sequence. The **Client must** receive that sequence and **rewind + replay** it ([[#Client correction]]). A node-local patch (old-value rematch, span rematch, "true to these Nodes") is not enough.

Invalid: Server looks at the touched Nodes, rewrites the newest Ops to match those Nodes, and emits only that rewrite. That drops other Actors' Ops on other Nodes and fields.

Worker report: [[amendment-order.md]].

## Client correction

**Accepted.** The optimistic Client must match the Server's Local Graph. The strategy is **rewind + replay**. When a **Change list** arrives (queue-empty Poll, or a POST that is not pipelined): **undo to baseline**, then apply that list. Do not apply the list onto the optimistic unamended Local Graph.

1. **Undo to baseline** — rewind the optimistic Local Graph to the **baseline** (the catch-up point; last-received Server Revision when that is the same thing). This is consume rewind, **not** unrestricted Undo ([[undo.md#Unrestricted Undo desirability]]).
2. Replay the **Change list** (Server-produced: other Actors' accepted Changes, then newest amended; `Op.apply` per Op).

**Pipelined ACK** does not apply a tail. It **informs** that external Changes exist; the Client **notes the baseline**. List apply waits until the **posting queue is empty**, then **Poll** from that baseline. POST and Poll are **not** the same path ("Poll = empty POST" **superseded** — [[pipelined-post.md]]).

**Neither POST nor Poll clears History** (accepted, [[unified-messaging.md]]). Poll = POST `/changes` with an empty posted list. Today's Poll-with-tail clear is software debt.

**Leftover pending (accepted; Q1 B superseded):** leftover pending stays **as planned**; next POST sends those **unamended** Changes; Server amends on apply. Queue-empty means those have been POSTed and ACKed; then Poll catch-up is the Server sequence. POST/Poll carry only the **last Revision received from the Server**. Stale that vs Server current **is** amendment.

This is **consume**. Amendment order is **produce**. Fill-in completes **one** Change ([[server-fill-ops.md]]). Load packages are Graph transfer and stay parked ([[poll-load-conveyance.md]]). Rewind+replay is **not** genesis replay: the base is the common prior this merge already shares, plus a short Server tail — not replay from empty.

Do not treat an in-place transform as an equal alternative unless a later increment proves it is identical to rewind+replay.

A shorter Server list (only the amended newest Change, plus completing Ops) is still this strategy when that list is the whole sequence from the rewind base. It is not a second path.

## Unified POST-ACK / Poll

**Two paths (accepted):** POST ACK is an external-changes **signal**; the Change list is applied on **queue-empty Poll** (undo to baseline + replay). **"Poll = empty POST" superseded.** Neither clears History. Recoverable kick-back is 200 Merge, not slice 2 Reject ([[slice-2-obsoleted.md]]). Home: [[pipelined-post.md]], [[unified-messaging.md]].

## Process

Two Changes, both defined on a **common prior** Local Graph. **Sequence** them. **Adjust the second** so it is compatible with the first, without destroying critical information. That is [[#Amendment order]] for two Changes.

In our words: Merge the second Change into a Local Graph that already includes the first. Transform is per-Op inside the second Change — after the first Change is in the Local Graph, not against isolated node fields.

Who is first **for text** is Server arrival. Browser still merges the remote payload into its Local Graph ([[.scratch/relaxed-concurrency/map.md]] slices 2–3) but does not pick a different winner.

A **soft lock** does not change this process ([[soft-lock.md]]). Meaning accepted: advisory checkout of the Actor's subtree — recommended to work elsewhere; not illegal. Concurrent edits there are other accepted Changes; the job is amended as newest. Merge still runs.

The produced sequence makes unrestricted Undo **possible**. Whether that is **desirable** (see and understand those edits) is open — [[undo.md#Unrestricted Undo desirability]]. Do not answer it here.

## Completing Ops (proposed)

A Local Subgraph Actor may drop an Owned edge and be unable to emit the promote-to-Owned Op. Server (fuller Local Graph) completes **that Change** with the missing Ops (timing accepted). Delete+promote in one Change is Move-shaped: owner count stays 1. Pattern: [[server-fill-ops.md]].

## Facts (do not copy specs)

- `Op.SetText` replaces the whole field and CAS `old*` ([[src/Shared/GraphMutate.fs]]). `Op.SetClasses` is the same **today**; merge spec is set delta, not that Op.
- Child insert and move are `Op.Replace` spans (overlapping CAS, reject on mismatch). That apply path is **behavior to beat**, not the Children spec. User remove-from-outline is usually **MoveToTrash** (still Owned). Hard delete only when already under TRASH with no refs outside ([[src/Shared/ViewModelDeleteOps.fs]]).
