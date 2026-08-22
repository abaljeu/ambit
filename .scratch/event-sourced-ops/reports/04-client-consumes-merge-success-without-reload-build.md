# Report — Issue 04 client consumes merge success without reload

**Date:** 2026-08-22
**Branch:** `w/event-sourced-ops`
**Issue:** [[../issues/04-client-consumes-merge-success-without-reload.md]]

## Summary

Recoverable merge success (`externalChanges` or non-confirmation Post ack) no longer routes to `ServerRejected` / forced reload. The Browser notes a catch-up baseline from the Post signal, retires the submitted queue prefix without applying the ack tail, polls from the baseline when the queue drains, then rewinds and replays the Poll Change list. Post and Poll paths preserve History. `#sync-status` shows "Merging remote changes…" while catch-up is pending.

## Files changed

| File | Change |
| --- | --- |
| [[../../../src/Shared/ViewModelSync.fs]] | `CatchUpBaseline`, `SyncInfo.catchUp`, helpers |
| [[../../../src/Shared/SyncLogic.fs]] | `isConfirmationEcho`, `reconcileExternalAck`, `consumeCatchUpPoll`, `undoPendingGraph`; poll apply no longer clears History |
| [[../../../src/Shared/SyncPlanner.fs]] | `tryStartPoll` uses catch-up baseline revision |
| [[../../../src/Shared/ViewModel.fs]] | `SubmitResponse` carries `externalChanges`; `PollDone` carries `responseRevision` |
| [[../../../src/Client/Update.fs]] | Branch Post ack on external vs echo; Poll catch-up consume path |
| [[../../../src/Client/App.fs]] | Wire `externalChanges` and poll revision into SysMsg |
| [[../../../src/Client/StatusView.fs]] | "Merging remote changes…" while `catchUp` pending |
| [[../../../src/Client/UpdateWorkspaceSync.fs]] | Sync workspace POST uses external ack path |
| [[../../../tests/Shared.Tests/AckReconcileTests.fs]] | External ack does not reject |
| [[../../../tests/Shared.Tests/SyncLogicTests.fs]] | Rewind/replay + History preservation |
| [[../../../tests/Shared.Tests/SyncPlannerTests.fs]] | Poll revision from catch-up baseline |

## Red / green evidence

**Red (before):** amended Post acks hit `reconcileAck` → `Rejected` → `ServerRejected`; `applyServerTail` cleared History.

**Green:**

```bash
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~AckReconcileTests|FullyQualifiedName~SyncLogicTests|FullyQualifiedName~SyncPlannerTests"
```

Result: Failed 0, Passed 81, Skipped 0.

```bash
dotnet build src/Client/Gambol.Client.fsproj -c Debug
```

Result: Build succeeded.

## Acceptance criteria

- [x] Recoverable merge success no longer lands the Browser in forced-reload Reject with lost pending work.
- [x] Empty posting queue + Poll rewinds to baseline and replays the Server Change list (no in-place optimistic patch).
- [x] Neither Post nor Poll clears History; leftover pending remains for the next post; History can retain Server-originated Changes.
- [x] After an external-changes Post signal, `#sync-status` shows remote catch-up is pending until the queue-empty Poll replay finishes.

## Out of scope (unchanged)

- Client-side replan of pending Ops before POST.

## Remaining concerns

- `SyncLogic.fs` is over the 400-line file guideline (pre-existing growth + this slice); split in a follow-up if desired.
- HITL browser check for concurrent edit + merge still recommended.
- No commit in this session (per instructions).

## Board mutation (for root)

- **remove** — [[../issues/04-client-consumes-merge-success-without-reload.md]] from Active when verified.
