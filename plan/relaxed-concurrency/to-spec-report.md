# to-spec report — relaxed concurrency

Date: 2026-08-19

## Spec path

[[plan/relaxed-concurrency/spec.md]] — `Status: ready-for-agent`

## Proposed testing seams (for user confirmation)

**Primary seam (recommended, one):** Server Change submission integration tests via the existing HTTP POST `/ambit/changes` harness (`StateEndpointTests` pattern), parameterized across file and database backends.

Covers:

- Unrelated attribute edits with stale base revision → success
- Same-node attribute race → op-level rejection
- Unrelated structural edits under different parents with stale revision → success
- Same-parent structural race → span mismatch rejection
- changeId dedup unchanged
- Rewrite of today's "wrong base revision alone returns 400" test

**Not proposed as a separate seam:** Shared History/GraphMutate unit tests (prior art already covers span and old-value CAS); client sync planner; browser HITL.

## Files changed

- `plan/relaxed-concurrency/spec.md` — created (this spec)
- `plan/relaxed-concurrency/to-spec-report.md` — created (this report)
- `plan/relaxed-concurrency/project.md` — summary refreshed
- `plan/index.md` — regenerated
- `WORK.md` — pending entry now links to spec

## Stage

Remains `spec` — destination locked in spec; `/to-tickets` not run in this step.
