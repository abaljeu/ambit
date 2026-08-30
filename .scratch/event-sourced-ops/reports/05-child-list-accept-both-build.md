# Report — Issue 05 child-list Accept Both build

**Date:** 2026-08-22
**Branch:** `w/event-sourced-ops`
**Issue:** [[../issues/05-child-list-accept-both.md]]

## Summary

Same-parent concurrent child-list Replaces now amend-and-succeed instead of HTTP 400 span CAS Reject. Shared `ChildListMerge` implements occurrence-bag `diff`, deterministic `acceptBoth` (replace-amendment §4), and three-way `resolve`. `ChangeAmendment` amends index-0 full-list Replace span mismatches to `Replace(parentId, current, target)`. Server agents already route through `ChangeAmendment.applyChange`; amended POSTs set `externalChanges = true` for ticket 04 rewind/replay. No Client changes required.

## Files changed

| File | Change |
| --- | --- |
| [[../../../src/Shared/ChildListMerge.fs]] | New: `diff`, `acceptBoth`, `resolve` |
| [[../../../src/Shared/ChangeAmendment.fs]] | Recoverable span CAS; `tryAmendReplace` |
| [[../../../src/Shared/Gambol.Shared.fsproj]] | Register `ChildListMerge.fs` |
| [[../../../tests/Shared.Tests/ChildListMergeTests.fs]] | Unit tests for §4 examples + concurrent remove/append |
| [[../../../tests/Shared.Tests/ChangeAmendmentTests.fs]] | Replace amendment integration test |
| [[../../../tests/Shared.Tests/Gambol.Shared.Tests.fsproj]] | Register `ChildListMergeTests.fs` |
| [[../../../tests/Server.Tests/StateEndpointTests.fs]] | Same-parent collision amends (was 400) |

## Test results

```bash
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~ChildListMergeTests|FullyQualifiedName~ChangeAmendmentTests"
```

Result: Failed 0, Passed 9, Skipped 0.

```bash
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~StateEndpointTests&FullyQualifiedName~same-parent"
```

Result: Failed 0, Passed 2, Skipped 0 (File + Db backends).

## Acceptance criteria

- [x] Same-parent concurrent inserts/removes succeed without Reject (same-slot collision + concurrent remove/append covered).
- [x] Occurrence-bag Accept Both preserves critical add/remove slots and §4 order invariants (prefix insert, same-slot, disjoint append, remove+append unit tests).
- [x] Amended child-list success consumed via existing rewind/replay path (`externalChanges` on amend; no Client diff).

## Remaining gaps

- **Producer migration (issue 13):** Wire contract is full-list Replace only ([[../details/replace-amendment.md]] §1). Client and Shared still emit span/partial Replaces (paste, cross-parent move, delete, import, etc.) — invalid wire usage, migration debt, not a gap in amendment coverage for `index > 0`. Until migration, span posts still hard Reject on CAS mismatch; only index-0 full-list posts amend.
- Issue 10 order polish not started.
- §10 wire field rename (`oldList` / `newList`, drop `index`) still open.
- No commit in this session.

## Board mutation (for root)

- **remove** — [[../issues/05-child-list-accept-both.md]] from Pending (verified green on focused tests).
- **move** — issue to done or archive per project stage policy.
