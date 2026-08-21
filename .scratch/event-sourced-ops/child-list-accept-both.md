# Child-list Accept Both (occurrence bag)

Worker report. Not a grill-round archive. Topic docs hold the accepted rule.

**Later correction:** "amends Replace to match the current Server graph" / `SetText C→B` as the general analogy is **too node-local**. Amendment order is common prior, then other accepted Changes, then newest amended — [[merge.md#Amendment order]], [[amendment-order.md]]. Child-list Accept Both still holds.

## Analysis

The prior round flattened Q7/Q8 into “occurrence-delta instead of Replace” and offered “Keep implemented Replace as a fact, not the merge spec.” That line is **wrong**. Implemented span-CAS Replace is **not** a match for the requirement. It is as-implemented **behavior to beat** (same role as genesis-replay / whole-set `SetClasses`), not a fact that stands in for the spec.

**Precise spec.** Actor posts a `Replace` op. Server amends it to a **definitive ordered-list replace** that matches the **current** Server graph after merge. Analogous to completing Ops / `SetText C→B`: the wire result applies to current state, not the Actor’s stale span CAS.

**Default / happy path.** Positional Replace — specified position, specified nodes. This **is** the posted Op. Do not “ignore implemented Replace” as the merge shape.

**Conflict.** Do not reject-as-the-whole-story. Compute a best approximation. The **critical** invariant is occurrence-bag Accept Both vs the common prior (adds = new slots, removes = prior slots; disjoint like class deltas). Order is important but **not** critical (already in [[merge.md]]). Approximation algorithm is **later** — no per-Op tables invented here.

**Bag vs Node ids (still).** If bag elements were Node ids only, add-another-X vs remove-X can cancel (neither-intended). Rejected. `amb-conflict` remains a **node indicator** (text), not an edge-edit device.

Owner-count / Move still hold. Fill-in timing still same Change (delete+promote). Parked items left parked.

**Lock status.** Child-list is **accepted** (algorithm later).

**Next frontier.** Same-Change vs later-Poll for the Server-amended Replace is still load-bearing. Completing Ops already chose same Change. Poll History-clear **resolved** (neither POST nor Poll clears — [[unified-messaging.md]]). Soft-lock **meaning** accepted ([[soft-lock.md]]); who/expiry still proposed.

## Edited

- [[merge.md]] — Children (accepted); implemented Replace = behavior to beat
- [[conflict-kinds.md]] — kind 3 accepted (positional Replace + bag approximation)
- [[vocab.md]] — Next increment: Children accepted
- [[collab-vocab.md]] — speak line
- [[goal.md]] — grill-record line
- [[project.md]] — summary (Stage still charting)

This file overwritten. No software. No branch switch. No [[CONTEXT.md]]. No [[WORK.md]]. No [[.scratch/index.md]] / Stage change. No grill-round file.

## WORK.md mutations

None. Grill already Active on [[project.md]].

## Accept Both

**Accepted** (positional Replace default; bag Accept Both on conflict; Server amends to current graph). Algorithm later. Not locked as a per-Op table.

## User-facing reply (exact)

Accepted. Default child-list is positional Replace — specified position, specified nodes. That is the posted Op / happy path.

On conflicting edits: do not reject the Change as the whole story. Compute a best approximation. The **critical** invariant is occurrence-bag Accept Both vs the common prior (adds = new slots, removes = prior slots; disjoint like class deltas). Order is important but not critical. Algorithm later.

Server amends that Replace to a definitive ordered list matching the **current** Server graph — not the Actor’s stale span CAS. Implemented span-CAS reject is **behavior to beat**, not the spec. `amb-conflict` stays text-only.

❓ **Same-Change amend?** — Does the Server-amended Replace ride in the **same Change** the Actor posted (fill-in style), or a later Poll Change?

➡️ Same Change. Completing Ops already uses that path; a later Poll tail would clear History.
