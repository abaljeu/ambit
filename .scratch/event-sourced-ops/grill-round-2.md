# Grill round 2

Worker report. Parent speaks the user-facing round unchanged. [[WORK.md]] stays Active; no board mutations.

## Settled from Q1

Recorded in [[goal.md]]. Semantic foundation + expand applicability. Snapshot `/state` may stay. Rejection revisited for incomplete use cases. Genesis replay / log-as-truth not asserted.

## Facts (not for the user unless one line is needed)

Transactional core today:

- `Op.apply` → `GraphMutate` (attribute CAS; `Replace` span CAS). `SetUpdateTime` ignores mismatch. `History.applyChange` then checks ownership.
- Server `applyBatch`: global revision gate, then `History.applyChange`, then Change log + snapshot. Parse and lazy-load reconcile plan Ops and `postGraphOnlyChange` (still `applyBatch`; `graphOnly` skips disk path checks).
- Browser local edits, undo, and Poll tails: `ResidentProjection.applyChange` → `Op.apply`. **No** `validateOwnershipForChange`. Absent headers and Unloaded `Replace` become `Unchanged` (silent skip).
- Load/Sync: Change tail through `Op.apply`, then `ResidentProjection.installPackages` — `Map.add` of Nodes. GET `/state` is a Graph snapshot; `bootstrapStateResponse` slices it. No `Op.apply`.
- DbAgent startup can replace or trim the in-memory Graph without Ops. Snapshot load is Graph-from-rows, not replay.

Holes (name later, after this round): package install; `/state` bootstrap slice; snapshot reload; startup graph replace; silent Unloaded skip; Browser apply without ownership validate; Parse already Ops but still in-request, not async Poll.

## User-facing round (speak unchanged)

❓ **Q2** - **Revisit ≠ genesis replay**: You said the last rejection missed use cases, so a fuller concept is back. That must not silently reopen log-as-truth.

A) Expand the *semantic* model: Graph mutations are Ops with today's transactional guarantees. Snapshot + tail stay. Genesis replay stays rejected.

B) Fuller *event-sourced implementation*: the log is the record; snapshots are disposable; replay matters.

C) Undecided — then say what "fuller" adds that the map's knowns 5 and 7 did not already cover.

➡️ **A.** Incomplete use cases justify a wider *goal*. They do not reopen genesis replay. If you want B, say so now.

❓ **Q3** - **What is illegal if `/state` stays a blob?**: GET `/state` and Load packages install Nodes by map-merge, not `Op.apply`. Poll and local edits do go through `Op.apply`. A slogan that the snapshot "is" Ops, with no new reject, is documentation.

What must stay illegal even when `/state` remains a snapshot?

A) Lineage: a blob that could not have been produced by Ops is illegal to install. (You cannot check this on the wire.)

B) Invariants only: installing a Graph that `Op.apply` would reject is illegal. Snapshot is a projection, not a second writer.

C) Thinking tool only. No new reject. `/state` may install Nodes no Change produced.

D) Name a concrete reject I can test.

➡️ **B.** If you cannot name a reject, this is a style guide. Do not call that a semantic foundation.

## Next-next frontier (do not ask yet)

1. "Within a process" — Server, Browser, both, or also cross-process (`POST /changes`)?
2. Which holes are in scope this project (list in Facts above).
3. Incremental Load: must-see vs may-lag vs lie. Then HITL vs agentic vs long-running — one apply/reject model?

## WORK.md mutations

None. Keep Active on [[.scratch/event-sourced-ops/project.md]].
