# Report — Issue 02 independent concurrent Changes build

**Date:** 2026-08-22
**Branch:** `w/event-sourced-ops`
**Issue:** [[../issues/02-independent-concurrent-changes-succeed.md]]
**Plan:** [[02-implement-plan.md]]

## Summary

Removed the global revision-gate arms from `FileAgent.applyBatch` and `DbAgent.applyBatch`. Unrelated concurrent Changes with stale `change.id` now succeed when per-Op CAS matches; same-target attribute and same-parent Replace collisions still 400 with op-level errors. Issue 01 shared success envelope encoding was left untouched.

## Files changed

| File | Change |
| --- | --- |
| [[../../../tests/Server.Tests/StateEndpointTests.fs]] | Rewrote obsolete wrong-revision 400 into stale-revision success; added unrelated attribute/structural, collision, and stale-revision changeId dedup scenarios |
| [[../../../src/Server/FileAgent.fs]] | Deleted `change.id <> s.revision` reject branch in `applyBatch` |
| [[../../../src/Server/DbAgent.fs]] | Same gate removal in `applyBatch` |
| [[../issues/02-independent-concurrent-changes-succeed.md]] | Acceptance boxes checked; Status → done |

Not touched: History/GraphMutate, ChangeSuccessResponse / envelope encoding, issue 01 docs.

## Red / green evidence

**Red (gate still present):** focused concurrency filter — Failed 10, Passed 2 (collision + dedup paths already 400/200 under the gate).

**Green (after gate removal):**

```bash
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~StateEndpointTests"
```

Result: Failed 0, Passed 62, Skipped 0 (File + Db backends).

## Commands

```bash
./status.sh
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~StateEndpointTests&FullyQualifiedName~stale|FullyQualifiedName~StateEndpointTests&FullyQualifiedName~unrelated|FullyQualifiedName~StateEndpointTests&FullyQualifiedName~collision|FullyQualifiedName~StateEndpointTests&FullyQualifiedName~duplicate changeId with stale"
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~StateEndpointTests"
```

## Relaxed-concurrency slice 1 verify/handoff

Sibling [[../../relaxed-concurrency/spec.md]] / [[../../relaxed-concurrency/design.md]] slice 1 (drop global revision gate) is delivered by this build. Treat that project as verify/handoff complete for slice 1 — do **not** start a second build there. Root may advance/archive that project separately; this report does not edit its Stage.

## Remaining concerns

- Same-target CAS Reject remains until issues 03/05 (intentional).
- Zero-width same-parent inserts (`oldChildren = []`) still both succeed; collision coverage uses non-empty stale span replace.
- No commit in this session (per instructions).

## Board mutation (for root)

- **remove** — [[../issues/02-independent-concurrent-changes-succeed.md]] from Active (verified green on StateEndpointTests, both backends).
