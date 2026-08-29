# Reconcile architecture sequence — delegated report

Parent-facing summary of the to-tickets quiz reconcile. Durable sequence: [[to-tickets-draft.md]]. Branch: `w/event-sourced-ops` ([[git.md]]).

## A. Soft-lock vs job vs Parse

**Recommendation:** neither soft-lock-first nor job-first as separate products.

1. **Ticket 6** — generalized Server Actor produce path.
2. **Ticket 7** — Parse realignment as **tracer** (proves produce path; no multi-job identity / soft-lock chrome required).
3. **Ticket 8** — **one vertical:** job identity + advisory soft-lock (lock owned by job; completion clears; indicator opens job).

Rationale: user said lock lifecycle is job-owned and the indicator is a job access point. Two tickets would invent two surfaces that must immediately couple. Parse should not wait on that footprint.

## B. Envelope

**Pinned:** shared success **type** for Post and Poll; channels stay distinct. Folded into **Ticket 0**. Optional late unify ticket **removed**. Caveat: Post must not apply the list.

## C. Late decisions

| Item | Action | Why |
| --- | --- | --- |
| Delete-against-edit + orphan | **Ticket 5** decision early; implement later | May need Change baseline + history scan / retention |
| Completing-ops beyond timing | Ticket 10 after Actor path | Timing already accepted; same-Change fill-in constrains 2–3 |
| Unrestricted Undo | Ticket 11 after Ticket 3 | Only needs History extensibility constraint on Ticket 3 |

Tickets 0–4 carry **extension constraints** (optional baseline field room; adjustable short-tail retention; History not own-posts-only). Product schedule ≠ false technical blocks on 0–4.

## D. Load / state transfer

**Verified accepted** (Round 4 + overview + quiz). Load packages = Graph/state transfer for unloaded Nodes/children; not Ops/genesis replay. Stale “parked” wording corrected in architecture/open-questions/messaging/as-implemented-facts. Still parked: state-endpoint producer duty / partial-view belief; Server-partial Local Graph mode; job residency packaging detail.

## E. Global revision

**Verified accepted:** one global Server arrival/revision sequence. Last-received revision on the wire. Not per-Workspace. Exact token encoding may still be refined.

## Publish-oriented ticket list

`0∥1 → 2 → 3 → 4`; then `5` (recovery decisions); `6 → 7 → 8` (Actor → Parse → job+lock); `9` polish; `10` completing-ops; `11` Undo decision. Publish map `01`–`12`.

## Remaining user choices

1. Approve merged Ticket 8 and Ticket 0 shared-envelope pin for publish.
2. Confirm Ticket 5 may decide delete-against-edit without implementing recovery in the same project.
3. Optional further merges (e.g. Ticket 10 into 6)?

## WORK.md mutation (parent applies)

- **refine** Active [[.scratch/event-sourced-ops/to-tickets-draft.md]] — quiz reconciled; approve Tickets 0–11 then publish `issues/01`–`12`.
- **add** Pending (optional): [[.scratch/event-sourced-ops/reconcile-architecture-sequence-report.md]].
- **note** [[.scratch/relaxed-concurrency/]] is a build-upon layer (Stage done); gate removal delivered in issue 02.

No Stage change. No `issues/`. No software. No commit.
