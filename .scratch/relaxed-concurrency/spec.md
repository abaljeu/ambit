# Relaxed concurrency

Status: ready-for-agent

## Problem Statement

Gambol serializes every client edit through a single global revision counter. When one client commits any Change anywhere in the Graph, the server rejects every other client's in-flight submission that still names the prior revision — even when the two edits target completely unrelated Nodes or parents.

That behaviour shows up as occasional "Revision mismatch" failures during normal multi-client use. Safety holds: nothing corrupts. Liveness does not: unrelated work collides unnecessarily. Users experience avoidable sync rejection instead of successful concurrent editing.

The underlying Ops already carry per-target preconditions (`oldText`, `oldClasses`, `oldName`, `oldState`, and full-value `oldChildren` on structural Replace). Attribute edits and structural edits under different parents could commit independently if the server stopped treating revision equality as the sole conflict boundary.

## Solution

Remove the global revision gate from server Change application. Keep `changeId` deduplication, revision advancement on each successful apply, and every existing per-Op precondition in Shared graph mutation.

After this change:

- Two clients may edit different Nodes' attributes concurrently when each Op's old-value precondition still matches live state at apply time.
- Two clients may perform structural edits under different parents concurrently when each Replace's span precondition matches.
- Genuine collisions still reject: same Node attribute races fail on old-value mismatch; same-parent structural races fail on span mismatch.
- Same-parent structural rejection continues through today's server Reject path and client handling — no retry, rebase, or new collision UX in this effort.

No model change, no wire-format change, no client sync redesign. The `old*` fields already on the wire do the work.

## User Stories

### Core acceptance

1. As a user editing attributes on one Node while a colleague edits another, I want both Changes to commit when each Op's old-value precondition matches, so that unrelated attribute work is not serialized by revision.
2. As a user performing structural edits under one parent while a colleague edits under another, I want both Changes to commit when each Replace span matches, so that distant outline changes do not block each other.
3. As a user whose in-flight Change names a stale base revision but whose Ops still match live state, I want the server to accept the Change, so that revision lag alone is not a rejection reason.

### Preserved rejection and unchanged behaviour

4. As a user racing on the same target, I want the losing Change to reject with today's op-level errors (attribute old-value mismatch or same-parent Replace span mismatch), so that I am not silently overwritten and structural integrity is preserved.
5. As a user who receives a same-parent structural rejection, I want the client to follow today's Reject flow with no new retry, rebase, or collision UX, so that behaviour stays predictable.
6. As a user or developer, I want `changeId` deduplication, batch fail-fast on first error, revision advancement per successful apply, and unchanged-submission rejection to behave exactly as today, so that only the global revision gate is removed.
7. As a developer, I want the same relaxed apply semantics on file and database server paths, so that local, test, and production deployments match.

## Implementation Decisions

- Remove the `change.id <> serverRevision` rejection branch from server Change batch application on **both** persistence backends (file agent and database agent). This is the only behavioural change in this effort.
- Preserve existing branches in the same apply step: `changeId` deduplication against persisted/logged Changes; delegation to Shared `History.applyChange`; rejection of unchanged submissions; revision increment by one per successfully applied Change; batch fold fail-fast on first error.
- Do **not** modify Shared `History.applyChange`, `Graph.replace`, or attribute setters. Per-op preconditions already enforce compare-and-swap for SetText, SetClasses, SetName, SetDocumentState, and Replace (full-value span equality).
- Do **not** change the Change or Op wire shape. Clients continue to send `change.id` (base revision at plan time). After gate removal it is informational for client sync and logging, not a server acceptance predicate.
- Keep global revision as the monotonic sequence counter for poll, load, and catch-up. Successful applies still bump revision; the counter no longer doubles as the conflict boundary for unrelated edits.
- Same-parent structural collision: when Replace span compare-and-swap fails, propagate the existing error string through the apply path; HTTP layer continues to surface it as a failed submission (today's 400 + error body). No new retry, rebase-and-resubmit, or collision-specific client UX.
- Attribute collision: existing old-value mismatch errors (`old text does not match`, `old classes do not match`, `old name does not match`, `old state does not match`) remain the rejection surface; no merge or last-writer-wins tiebreak.
- Op types without compare-and-swap remain as today: `NewNode` and `NewSpecialNode` create nodes (collision if id already present is a separate validation concern); `SetUpdateTime` intentionally ignores `oldTime` mismatch per Op contract.
- Parse and import producers already plan Replace against live child lists or use the zero-width insert idiom (`oldChildren = []`). No producer changes in this spec except where tests document invariants. Known gap in `ViewModelJoinOps.removeCurrentOp` (fabricated Owner edge on Ref rows) is tracked separately and is **not** fixed here.
- Document batch-ordering invariants worth preserving: cold parse emits delete Ops before child Replace on the same parent; import attach Replace must read `existingChildren` at plan time. These are not new runtime checks — they remain planner responsibilities.
- Update the server integration test that currently expects rejection on stale base revision alone with an unrelated valid Op; that test must reflect the new semantics (stale revision with valid preconditions succeeds).

## Testing Decisions

**Proposed primary seam (one): server Change submission integration tests** — the same HTTP POST `/ambit/changes` harness used today for revision, dedup, and persistence tests, parameterized across file and database backends.

What makes a good test here:

- Assert externally visible outcomes: HTTP status, returned revision, acked `changeId`s, and resulting graph state via GET — not private agent internals.
- Model two-client interleaving by committing Change A, then submitting Change B with a stale `change.id` but Ops planned against B's pre-change view; verify accept or reject based on whether targets actually collide.
- Prefer minimal graphs (root + one or two children under distinct parents) to keep scenarios readable.

Scenarios the seam must cover:

1. **Unrelated attribute concurrency** — Client B submits SetText on node X with stale revision after Client A committed SetText on node Y; expect 200, revision +2 total, both texts present.
2. **Attribute collision** — After A changes node X's text, B submits SetText on X with B's stale oldText; expect 400 with op-level old-text mismatch; graph retains A's text.
3. **Unrelated structural concurrency** — After A replaces under parent P1, B replaces under parent P2 with stale revision; expect both succeed.
4. **Same-parent structural collision** — After A replaces under parent P, B submits Replace on P with stale span; expect 400 with span mismatch; P's children match A's result.
5. **Idempotent dedup unchanged** — Resubmit same `changeId` with stale revision after first success; expect 200, revision unchanged, same ack (existing test pattern).
6. **Replace obsolete revision-only test** — Retire or rewrite the test that expects 400 for wrong base revision with an otherwise valid unrelated SetText; it becomes a success case under this spec.

Prior art: existing state-endpoint tests for revision bump, wrong-revision 400, and changeId dedup; Shared History tests for invalid Replace span leaving graph unchanged and duplicate-link Replace acceptance.

**Out of seam for this spec:** new Shared-only concurrency suite (preconditions are already covered in ModelTests and HistoryTests); browser multi-client HITL; client sync planner changes (client continues sending base revision; no planner change required for server-first rollout); join-on-Ref fix (separate WORK item).

Completion criterion: focused server integration tests above pass on both backends; no regression in changeId dedup or revision monotonicity tests.

## Out of Scope

- Event sourcing, genesis replay, or retaining historic parsers.
- Id-anchored Replace, **strong form** only — dropping `index` and locating the span by id run alone. Rejected as ambiguous in [[map.md]]; duplicate ids under one parent are legal.
- Id-anchored Replace, **weak form** — server-side silent relocation rejected (G resolved). Client replan after merge may reuse contiguous-run matching; **deferred to slices 2–3** ([[map.md]]). Out of scope for slice 1 (this spec).
- ChildNode occurrence identity or edge identity model changes.
- Order-CRDTs, tombstones, or convergence without rejection.
- Offline editing.
- Client retry, rebase-and-resubmit, or new collision surfacing UX for same-parent structural rejection.
- Fixing `ViewModelJoinOps.removeCurrentOp` Owner fabrication (separate pending WORK item).
- Hybrid file/log authority (document-derived subgraph vs graph-native overlay).
- Document node identity stability across reparse.
- Undo redesign under a command/event split.
- Wire-format or API version bump.
- Changing which Ops carry compare-and-swap or adding preconditions to NewNode / SetUpdateTime.
- Breaking this spec into numbered implementation issues (`/to-tickets` follows separately).

## Further Notes

- [[map.md]] records rejected alternatives and open questions D–F; this spec implements only slice 1 (drop global gate, keep per-op preconditions, keep Reject path).
- Duplicate ids under one parent remain legal; full-value Replace CAS is load-bearing — do not weaken to id-only matching ([[child-occurrence-uniqueness.md]]).
- G resolved (2026-08-19): client merge-sync for slices 2–3; server weak form rejected. Rationale: [[design.md#Client vs server replan]]. Slice 1 acceptance criteria unchanged.
- `SetUpdateTime` ignoring mismatch is deliberate.
- Design pass: [[design.md]]. After delivery, consider updating [[doc/current/sync-mvp.md]].
