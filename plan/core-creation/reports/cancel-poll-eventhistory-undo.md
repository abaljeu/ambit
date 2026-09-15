# Cancelled: poll eventHistory / undo door

Date: 2026-09-15

Ticket: leftover after [[../issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]]. Status stays `coded`. No commit. This increment will not continue.

User: "this is way more complex than i expected. let's cancel this request."

## 1. Reverted (this increment only)

`git restore` of 31 paths to `35b65d8` (`clean up duplicate histories`). That commit already had the earlier completed work and did not have this increment.

Dropped:

1. Poll / Load / GET `/state` / POST Change carrying mailbox `eventHistory`.
2. `eventHistory` field on `StateResponse`, `ChangeSuccessResponse`, `LoadResponse`, `SyncResponse`.
3. `CoreChanges.eventHistory` and the extra `coreChanges.eventHistory` wire.
4. `History.recordChange` / `tryUndoChange` / `tryRedoChange`.
5. Replacing `ClientHistory` with mailbox `History` in SyncLogic, VM, Client poll/undo.
6. Half-edited Poll / API / SyncLogic / CoreMailbox door tests for that path.

Restored files:

- Client: `App.fs`, `Program.fs`, `Update.fs`, `UpdateOps.fs`, `UpdateWorkspaceSync.fs`
- Server: `Api.fs`, `Core/CoreChanges.fs`, `Core/CoreMailbox.fs`
- Shared: `ApiResponseSerialization.fs`, `ApiResponses.fs`, `BootCache.fs`, `History.fs`, `ResidentProjection.fs`, `Serialization.fs`, `SyncLogic.fs`, `ViewModel.fs`
- Server tests: `ApiGetStateTests.fs`, `ApiPostLoadTests.fs`, `CoreChangesTests.fs`, `CoreCredentialsTests.fs`, `CoreMailboxDoorTests.fs`
- Shared tests: `AckReconcileTests.fs`, `BootCachePollTests.fs`, `BootCacheTests.fs`, `ClientHistoryRuntimeTests.fs`, `HistoryTests.fs`, `LargeChangeApplyTests.fs`, `LoadCaptureTests.fs`, `SerializationTests.fs`, `SyncLogicTests.fs`, `VmTestHelpers.fs`

## 2. Left in place (earlier increments)

1. `State` is Graph + Revision only. No `State.history`.
2. Mailbox field named `eventHistory`: one `History`, restore from ChangeLog, no post-time copy. Door `CoreMailbox.eventHistory`. Backend is the only writer.
3. ActorStop single-admit (`dispatchActorStop` admits once; `pool.finish` drops).
4. Actors use `CoreMailbox.coreChanges`.

Browser undo is still `ClientHistory` via SyncLogic. Poll still returns Change tail only.

## 3. Truncation

IDE showed `CoreMailbox.fs`, `SyncLogicTests.fs`, and `CoreMailboxDoorTests.fs` as 1 line. On disk they were not truncated (198 / 601 / 678 during the increment). After restore: 197 / 583 / 646. Restored from HEAD, not from a salvage copy.

## 4. Working tree

`scripts/gitstatus.sh`: `## dev`. Tracked tree matches `35b65d8`. No commit. No simpler alternative started.
