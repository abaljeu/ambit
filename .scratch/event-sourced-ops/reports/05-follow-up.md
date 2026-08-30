# Report — Issue 05 follow-up

**Date:** 2026-08-22
**Branch:** `w/event-sourced-ops`
**Issue:** [[../issues/05-child-list-accept-both.md]]
**Build report:** [[05-child-list-accept-both-build.md]]

## Board changes

- **remove** — [[../issues/05-child-list-accept-both.md]] from [[../../../WORK.md]] Pending (verified complete; linked build report).

## Issue status

- Status already **done**; all acceptance criteria checked.
- No issue file edits required.

## Test verification

| Run | Result |
| --- | --- |
| `ClientHistoryRuntimeTests` (8 tests) | Passed 8, Failed 0 |
| Full `Shared.Tests` | Passed 1350, Failed 0, Skipped 1 |

Prior failure `non-empty Poll tail clears ClientHistory before projection` was already fixed in this branch: test renamed to `non-empty Poll tail preserves ClientHistory before projection` to match issue 04 behavior ([[05-client-history-poll-tail-fix.md]]). No additional fixes applied in this follow-up.

## Project stage

- [[../project.md]] remains **active** (issues 06–12 and polish remain; issue 05 was one slice, not project completion).
- `.scratch/index.md` unchanged.

## Commits

None (per instruction).
