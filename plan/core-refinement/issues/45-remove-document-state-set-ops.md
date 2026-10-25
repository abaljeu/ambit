# 45 — Add Notice.PersistStopped beside NodePersisted and PersistFailed

**Status:** `defined`
**Trackers:** architecture

Alan, 2026-10-24: persist just stops, like an Actor or a search. The server posts one stop Notice, `Notice.PersistStopped`, with an outcome: written at a version, or failed with a reason. It replaces `Notice.NodePersisted` and `Notice.PersistFailed`, and it closes the open `Notice.NodeUnpersisted` decision. This ticket was "SetDocumentState leaves; NodeUnpersisted replaces it". Its remaining SetDocumentState and MarkUnparsed scope moved to [121 — SetDocumentState and MarkUnparsed leave](121-remove-document-state-set-ops-and-markunparsed.md). The sequence is:

- 45 Expand: add the case and its handlers; nothing posts it
- [119 — The persist thread posts Notice.PersistStopped](119-the-persist-thread-posts-notice-persiststopped.md) Change: the persist thread posts it
- [120 — NodePersisted and PersistFailed leave Notice](120-nodepersisted-and-persistfailed-leave-notice.md) Contract: the two old cases leave

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] none

## 3. Look at

### Owner

[ ] [History.fs](src/Shared/History.fs) owns `Notice`

### Source

[ ] [EventJson.fs](src/Shared/EventJson.fs) `encodeNodePersisted`, `encodePersistFailed`, `encodeNotice`, `decodeNodePersisted`, `decodePersistFailed`, `decodeNoticeBody`
[ ] [SyncLogic.fs](src/Shared/SyncLogic.fs) `persistFailedMessage`, `applyNodePersisted`, `applyLoggedNotice`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `writePersisted`, `applyNotice`
[ ] [Database.fs](src/Server/Database.fs) `writeNoticeRow`

### Docs

[ ] [cqrs-messages.md](plan/core-refinement/cqrs-messages.md) §2 item 8 **Notice** and decision 12 **PersistStopped**, read only

### Match sites and constructors

[ ] `Notice` is matched without a wildcard in `EventJson.encodeNotice`, `CoreMailboxLoad.applyNotice` and `Database.writeNoticeRow`
[ ] `CoreEventDispatch.writeAxis` hands its notice to `persist.replaceGraph`; `DbAgent` writes the node row through `Database.writeNoticeRow`
[ ] [Update.fs](src/Client/Update.fs) reads `SyncLogic.persistFailedMessage`, read only; its signature stays

### Tests this ticket names

[ ] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) `Notice JSON round trip`
[ ] [CoreLoopNoticeTests.fs](tests/Server.Tests/CoreLoopNoticeTests.fs) each construction of `Notice.NodePersisted` or `Notice.PersistFailed`
[ ] [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs) `PersistFailed leaves the graph`, `NodePersisted applies the written file time`, `logged NodePersisted for an older version leaves the client node Unpersisted`
[ ] [DbAgentNoticeTests.fs](tests/Server.Tests/DbAgentNoticeTests.fs) `NodePersisted writes persist state to the database`

## 4. Context

### Today

[ ] `Notice.NodePersisted of nodeId: NodeId * persistedVersion: EventId * updateTime: System.DateTime` ([99 — NodePersisted carries the written version](99-nodepersisted-carries-the-written-version.md))
[ ] `Notice.PersistFailed of nodeId: NodeId * message: string` ([75 — A failed write does not fail the edit](75-a-failed-write-does-not-fail-the-edit.md))
[ ] `PersistThread.runRequest` posts `NodePersisted` after a good write and `PersistFailed` after a bad write
[ ] `applyNotice` for `NodePersisted` calls `noteDiffDone`, then `writePersisted` (the written-version rule of [100 — Core loop gates Persisted on the written version](100-core-loop-gates-persisted-on-the-written-version.md)), then `notePersisted`
[ ] `applyNotice` for `PersistFailed` calls `noteWriteFailed` and leaves persist state; the node stays Unpersisted, because the persist thread writes only an Unpersisted node
[ ] `SyncLogic.applyLoggedNotice` applies a logged `NodePersisted` with the rule of [101 — Client sync applies the persist version rule](101-client-sync-applies-the-persist-version-rule.md); any other Notice only moves `eventId`
[ ] `SyncLogic.persistFailedMessage` gives the last `PersistFailed` message; `Update.fs` shows it as the command error
[ ] `Database.writeNoticeRow` writes the node row for `NodePersisted` and writes nothing for `PersistFailed`
[ ] The encoder writes the tags `nodePersisted` and `persistFailed`; the decoder also reads `snapshotDone` as `NodePersisted`

### Decisions

[ ] Alan, 2026-10-24: `Notice.PersistStopped` replaces `NodePersisted`, with a fail case; names follow `Notice.*Stopped`
[ ] A Notice starts no client catch-up ([77 — Rename SnapshotDone to NodePersisted, and Notices start no catch-up](77-rename-snapshotdone-and-notices-start-no-catch-up.md)); `PersistStopped` is no exception

## 5. Changes

### Expand

[ ] One expand: add `PersistResult` and `Notice.PersistStopped`, with handlers that do what the two old cases do; nothing posts the new case yet

### History.fs

[ ] Add, before `Notice`:

```fsharp
[<RequireQualifiedAccess>]
type PersistResult =
    | Written of persistedVersion: EventId * updateTime: System.DateTime
    | Failed of message: string
```

[ ] Add `Notice.PersistStopped of nodeId: NodeId * result: PersistResult`; keep `NodePersisted` and `PersistFailed`

### EventJson.fs

[ ] `encodeNotice` writes `PersistStopped` with tag `persistStopped`, `nodeId`, and `result`
[ ] `result` is `written` with `persistedVersion` (`EventId.toJson`) and `updateTime` (ticks), or `failed` with `message`
[ ] `decodeNoticeBody` reads tag `persistStopped` back to the same value

### CoreMailboxLoad.fs

[ ] `writePersisted` takes the notice it logs, so the `DbAgent` row write gets the same notice
[ ] `applyNotice` for `PersistStopped(nodeId, Written(version, time))` runs the `NodePersisted` arm with that notice
[ ] `applyNotice` for `PersistStopped(_, Failed _)` runs the `PersistFailed` arm: `noteWriteFailed`, persist state unchanged

### SyncLogic.fs

[ ] `applyLoggedNotice` applies `PersistStopped(nodeId, Written(version, time))` with `applyNodePersisted`
[ ] `applyLoggedNotice` for `PersistStopped(_, Failed _)` moves `eventId` only; the client node stays Unpersisted
[ ] `persistFailedMessage` also reads the message of `PersistStopped(_, Failed message)`

### Database.fs

[ ] `writeNoticeRow` writes `writePersistedNode` for `PersistStopped(_, Written _)` and nothing for `PersistStopped(_, Failed _)`

### Size

[ ] 5 source files: `History.fs`, `EventJson.fs`, `SyncLogic.fs`, `CoreMailboxLoad.fs`, `Database.fs`
[ ] 4 test files: `EventJsonTests.fs`, `CoreLoopNoticeTests.fs`, `SyncLogicTests.fs`, `DbAgentNoticeTests.fs`

## 6. Tests

### JSON

[ ] Change `Notice JSON round trip` in `EventJsonTests.fs`: the list also holds `PersistStopped` with `Written` at a non-zero version and `PersistStopped` with `Failed "disk full"`; each round-trips
[ ] Filter: `Notice JSON round trip`

### Core loop

[ ] In `CoreLoopNoticeTests.fs`, each `NodePersisted(id, v, t)` becomes `PersistStopped(id, Written(v, t))`, and each `PersistFailed(id, m)` becomes `PersistStopped(id, Failed m)`; the asserts stay
[ ] Rename `NodePersisted sets Persisted and calls the diff hook` to `PersistStopped Written sets Persisted and calls the diff hook`
[ ] Rename `NodePersisted for an older version leaves the node Unpersisted` to `PersistStopped Written for an older version leaves the node Unpersisted`
[ ] Rename `PersistFailed does not change persist state` to `PersistStopped Failed does not change persist state`
[ ] Rename `mailbox queue applies NodePersisted off CoreMsg` to `mailbox queue applies PersistStopped off CoreMsg`
[ ] Filter: `CoreLoopNoticeTests`

### Client sync

[ ] In `SyncLogicTests.fs`, the three tests in Look at log `PersistStopped` in place of the old case; the asserts stay
[ ] Rename them `PersistStopped Failed leaves the graph`, `PersistStopped Written applies the written file time`, and `logged PersistStopped for an older version leaves the client node Unpersisted`
[ ] Filter: `SyncLogicTests`

### Database row

[ ] In `DbAgentNoticeTests.fs`, rename `NodePersisted writes persist state to the database` to `PersistStopped Written writes persist state to the database`; it applies `PersistStopped(id, Written(written, stamp))`
[ ] Filter: `PersistStopped Written writes persist state to the database`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
