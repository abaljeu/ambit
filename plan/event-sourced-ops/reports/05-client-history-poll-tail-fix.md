# Report — ClientHistory Poll tail test fix

**Date:** 2026-08-22
**Branch:** `w/event-sourced-ops`
**Context:** Issue 05 investigation; failing `ClientHistoryRuntimeTests`

## Root cause

The test `non-empty Poll tail clears ClientHistory before projection` expected pre–issue-04 behavior: `SyncLogic.applyServerTail` clearing `ClientHistory` on a non-empty Change tail.

Issue 04 ([[04-client-consumes-merge-success-without-reload-build.md]]) intentionally changed Poll apply to **preserve** History so catch-up rewind/replay after merge success does not discard undo/redo stacks. `SyncLogic.applySyncResponse` / `applyServerTail` no longer clear History; `consumeCatchUpPoll` also preserves it explicitly.

`SyncLogicTests` was updated for issue 04 (`applyServerTail non-empty tail preserves History`, `applyServerTail with changes preserves History`). `ClientHistoryRuntimeTests` was not.

## What changed

| File | Change |
| --- | --- |
| [[../../../tests/Shared.Tests/ClientHistoryRuntimeTests.fs]] | Renamed test to `non-empty Poll tail preserves ClientHistory before projection`; assert `state.history = result.history` instead of cleared History |

No implementation change — current `SyncLogic.fs` behavior is correct per issue 04.

## Test results

```bash
dotnet test tests/Shared.Tests --filter "FullyQualifiedName~ClientHistoryRuntimeTests"
```

Passed: 8, Failed: 0
