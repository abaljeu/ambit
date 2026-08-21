# Amendment order

Worker report. Topic docs hold the rule. Home: [[merge.md#Amendment order]].

## Verdict

The user's concern is **valid**. The docs already said "common prior, sequence, adjust the second," but the **amendment** speech strayed into **node-local corrections**.

Where it strayed:

- [[child-list-accept-both.md]] and [[merge.md]] said the Server amends Replace to match the **current** Server graph, "analogous to `SetText C→B`": wire result applies to current state, not the Actor's stale span CAS.
- That reading lets the Server rewrite Ops so they are true to the **Nodes in question** (old-value rematch, span rematch, this parent's children) and emit **only** that rewrite.
- Those corrections omit **other Actors' accepted Changes** — every Op that is not a rewrite of the overlapping Node or span.
- `SetText C→B` is a one-field apply transform for an optimistic Local Graph already at `C`. The docs used it as the general amendment strategy. That is the confusion.

The Process section was closer (merge the second Change into a Local Graph that already includes the first). It did not say the Server must emit the other Changes, or that the Client must apply the full picture.

## Corrected algorithm

Vocab: **Actor**, **Change**, **Op**, **Local Graph**, **common prior**, **Merge**, **amend**, **Server**, **Client**. Newest = the Change being merged in now (Server arrival sequences first).

1. Take the **common prior** Local Graph (the base).
2. Apply the **accepted Changes from the other Actors** (every Op in those Changes).
3. Apply the **newest Actor contributor's** Change, **amending** its Ops against that combined Local Graph, without destroying critical information (unless an Actor removed it).

For more than two concurrent Changes, each next Change is newest relative to those already accepted.

**Invalid:** inject corrections true only to the touched Nodes and omit the other accepted Changes.

**Accepted:** this order. Per-Op tables still later. Merge as a whole stays proposed.

## Server vs Client

**Server must** produce the sequence. It emits:

- Other Actors' accepted Changes as they applied (full Changes, not a slice of overlapping Nodes).
- The newest Change **as amended**: rewritten Replace (child-list Accept Both vs common prior, then a definitive ordered list for the combined state), same-text rewrite (`A→B` on the Node; second becomes `amb-conflict` child), completing Ops ([[server-fill-ops.md]]) in that Change, owner-count adjust.

If the Client only gets "corrections true to the Nodes," it is missing every other-Actor Op that does not rewrite that same field or span.

**Client must** receive and apply the **full picture**:

- Apply other accepted Changes, then the amended newest Change (`Op.apply` per Op). Not a patch-only rematch of local node CAS.
- A Client that already applied the other accepted Changes needs only the amended newest Change (plus completing Ops).
- A Client that has not applied them must receive them. Optimistic Client at "common prior + own unamended Change" cannot reach Server state from node-local patches alone.
- Poll is already a Change list — [[poll-load-conveyance.md]]. That is the conveyance shape. The list contents must be this sequence. Load packages stay Graph transfer for residency (parked); they do not replace the Change tail.

## Relation to other topic docs

- **Merge:** Process = this order for two Changes. Children: Server still amends Replace to a definitive ordered list — computed after other accepted Changes are in, not from isolated node state. Classes: delta vs common prior, then apply after others. Same-text: Server rewrite stays; Client consume is now rewind+replay ([[merge.md#Client correction]]), not `SetText C→B`.
- **Undo:** invert the **amended** newest Change that applied, not the posted Ops. Other accepted Changes stay separate (or Poll-cleared). Node-local corrections cannot invert the full applied Change. [[undo.md]].
- **Conflict kinds:** independence does not skip conveyance. Disjoint Changes still ride in the sequence. Same-parent Accept Both still holds. [[conflict-kinds.md]].
- **Server fill-in:** fill-in completes **one** Change (delete+promote, same Change). Amendment rewrites the newest Change **after** others. Distinct. Client receives both. [[server-fill-ops.md]].

## Files changed

- [[merge.md]] — Amendment order section; Children and same-text no longer treat `SetText C→B` / "current graph" as the general strategy; Process points at the order
- [[vocab.md]] — Merge row; Children next-increment line
- [[conflict-kinds.md]] — independence does not skip conveyance
- [[server-fill-ops.md]] — fill-in ≠ amendment
- [[undo.md]] — invert amended Ops
- [[collab-vocab.md]] — speak the order
- [[goal.md]] — draft + grill-record line
- [[project.md]] — summary + links (Stage still `charting`)
- [[child-list-accept-both.md]] — later-correction note
- This file

No software. No [[CONTEXT.md]]. No [[WORK.md]]. No Stage change; [[.scratch/index.md]] not regenerated.

## WORK.md mutations

Update the Active [[project.md]] related list: add [[amendment-order.md]] (amendment order accepted). No `add` / `move` / `block` / `remove` of a work item. Grill stays Active.

## Later (this increment)

- Q1 **accepted:** rewind+replay — [[merge.md#Client correction]]. In-place transform is not equal.
- Q2 **proposed:** unified POST-ACK / Poll — [[unified-messaging.md]]. ACK wire today is still `SetUpdateTime` suffixes.
