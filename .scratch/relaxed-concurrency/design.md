# Relaxed concurrency — design

## Goal

[[spec.md]] removes the global revision gate from server Change application while keeping per-op compare-and-swap in Shared. Unrelated concurrent edits succeed when each Op's `old*` precondition matches live state at apply time; genuine races still reject through existing op-level errors. This document places that subtractive change at the right **seam**, names the **modules** involved, and confirms what stays **deep** (unchanged).

## Modules in play

### FileAgent (`applyBatch`)

**Interface:** Accept a batch of `Change` values; return updated in-memory state, confirmations, and fresh changes, or an error string. Called from `handlePostChange` after JSON decode. Invariants: `changeId` dedup against the log; fail-fast fold; revision increments by one per successfully applied Change; unchanged submissions reject.

**Changes:** Delete the `None when change.id <> s.revision.Value` branch (lines 150–152). The remaining `None` arm delegates directly to `History.applyChange`.

**Unchanged:** Log dedup via `ChangeLog.tryFindByChangeId`; `History.applyChange` delegation; unchanged rejection; revision bump; batch fold; persistence and validation in `handlePostChange`.

### DbAgent (`applyBatch`)

**Interface:** Same contract as FileAgent — batch apply with dedup, fail-fast, revision advance — backed by database persistence instead of file log.

**Changes:** Delete the equivalent revision gate (lines 108–110). Parity with FileAgent is a spec requirement (user story 7).

**Unchanged:** `tryPersistedChange` dedup; `History.applyChange`; unchanged rejection; `persistBatch` projection path.

### History (`applyChange`)

**Interface:** `Change × State → ApplyResult`. Applies ops left-to-right via `Change.apply`, then validates ownership semantics. Returns `Changed`, `Unchanged`, or `Invalid(state, msg)`.

**Changes:** None. Per-op preconditions live in `Op.apply` → `GraphMutate`.

**Unchanged:** Full apply fold, ownership validation, undo/invert, `PersistStamp`.

### GraphMutate (attribute setters, `Graph.replace`)

**Interface:** Pure graph transforms with old-value compare-and-swap: `setText`, `setClasses`, `setName`, `setDocumentState` compare `old*` against live node fields; `replace` compares the live child span against `oldChildren` (`"old span does not match"`).

**Changes:** None.

**Unchanged:** All CAS logic remains the collision boundary for attribute and same-parent structural races.

### HTTP changes endpoint (`Api.postChange` → agent `postChange`)

**Interface:** POST `/ambit/changes` with a change batch JSON body; returns revision, acked `changeId`s, and optional graph on success; 400 + error body on failure. Parameterized across file and database backends via `StateEndpointTests.backends`.

**Changes:** Observable behaviour only — stale base revision with valid preconditions becomes 200 instead of 400. Wire shape, status codes for real collisions, and dedup semantics stay the same.

**Unchanged:** Decode, agent dispatch, poll/load revision monotonicity.

## The seam

**Primary test seam:** POST `/ambit/changes` integration tests in `StateEndpointTests.fs`, parameterized across file and database backends — exactly as spec proposes.

**Why this depth:** The HTTP endpoint is the external **interface** callers and tests share. Two-client interleaving is modeled by committing Change A, then submitting Change B with stale `change.id` but Ops planned against B's pre-change view. Assertions use HTTP status, returned revision, acked `changeId`s, and GET state — not agent mailbox internals.

**Deletion test:** Removing the revision gate from FileAgent/DbAgent does not push complexity to callers. The gate was a pass-through check duplicating what per-op CAS already enforces for real collisions. Deleting it removes one branch; collision detection stays in Shared where it already lives.

**Interface as test surface:** Tests that describe accept/reject outcomes through POST + GET survive internal refactors. No need to test past the agent interface into `History` for concurrency scenarios — Shared tests already cover op-level CAS in isolation.

## What stays deep (do not touch)

History and GraphMutate already carry per-op CAS behind small interfaces. Removing the server revision gate does not require deepening Shared:

- Attribute edits: old-value mismatch errors are localized in GraphMutate.
- Structural edits: full-value span CAS in `Graph.replace` handles same-parent collisions.
- Batch ordering invariants (cold parse delete-before-replace, import live children) remain planner responsibilities, not new runtime checks.

**Locality:** Collision logic stays in one place (Shared mutation). Server agents keep their existing depth: dedup, apply delegation, revision sequencing, persistence.

## The change

Surgical and subtractive:

1. FileAgent: remove revision mismatch branch; fall through to `History.applyChange` on non-deduped changes.
2. DbAgent: same one-branch removal.
3. Tests: rewrite `POST with wrong base revision returns 400` (currently expects 400 for `id = 5` with valid SetText on revision-0 server); add concurrency scenarios from spec.

No new abstractions. No extracted `ConflictPolicy` module. No Shared edits. No wire-format change. `change.id` remains on the wire — informational for client sync after gate removal, not a server acceptance predicate.

## Error modes

After gate removal, callers and tests must know:

| Situation | Outcome | Error surface |
|-----------|---------|---------------|
| Stale `change.id`, Ops match live state | **Accept** (new) | 200; revision advances |
| Stale `change.id`, attribute race on same node | Reject | 400; `"old text does not match"` (or classes/name/state) |
| Stale `change.id`, same-parent Replace race | Reject | 400; `"old span does not match"` |
| Duplicate `changeId` | Accept (idempotent) | 200; revision unchanged |
| Unchanged submission | Reject | 400; `"Unchanged submission is rejected."` |
| Batch with error on Nth change | Fail-fast | 400; state unchanged for that batch |

Revision remains the monotonic sequence counter for poll, load, and catch-up. It is no longer the conflict boundary for unrelated edits.

## Rejected deepenings

- **ConflictPolicy port:** One adapter = hypothetical seam. Revision check and op-level CAS would duplicate responsibility; subtractive deletion is sufficient.
- **Move CAS to server:** Would shallow Shared (logic moves to callers/agents) and split **locality** of collision detection.
- **Unify FileAgent/DbAgent apply into a new module:** Two persistence adapters justify their existing seams; sharing one branch removal does not warrant extraction for a one-line change per agent.
- **Id-anchored Replace (strong form), order-CRDTs, server weak form:** Rejected — [[map.md#Id-anchored `Replace`]]. Client replan after merge (slices 2–3): [[#Client vs server replan]].

## Client vs server replan

**Client replan is preferred over server replan** for recoverable same-parent structural collisions (G / merge-sync). User confirmed after discussion (2026-08-19).

1. **Optimistic apply** — the client applies locally before POST; server silent relocate would leave UI and graph diverged until catch-up.
2. **Semantic ownership** — relocation is a planner choice, not server compare-and-swap.
3. **Server stays pure** — match or reject; no hidden relocate in `Graph.replace`.
4. **Coherence with merge-sync** — one path (reject + remote payload + merge + replan), not two collision paths (server relocate vs client merge-sync).
5. **Cost tradeoff** — server replan would be a smaller diff but externalizes the hard problem; once slice 2 (reject payload) is paid for, server replan adds little.

Slice 3 may reuse the same contiguous-run matching algorithm on the client after merge ([[map.md]]). Server-side weak form remains rejected.

## Ticket hints

Three independently actionable slices aligned with spec user stories:

1. **File agent + tests** — Remove revision gate in `FileAgent.applyBatch`; rewrite obsolete revision-only test; add unrelated-attribute and attribute-collision scenarios on file backend. Verify: focused `StateEndpointTests` green on file backend.

2. **Db agent parity** — Same branch removal in `DbAgent.applyBatch`; confirm parameterized tests pass on database backend. Verify: same test harness, both backends green for revised + new scenarios.

3. **Structural concurrency scenarios** — Add unrelated-parent Replace success, same-parent Replace collision, and idempotent dedup-with-stale-revision cases. Verify: spec testing scenarios 3–5 covered; no regression in revision monotonicity or changeId dedup tests.

Related but **not** in these slices: `ViewModelJoinOps.removeCurrentOp` join-on-Ref gap ([[WORK.md]] pending entry) — separate from gate removal.
