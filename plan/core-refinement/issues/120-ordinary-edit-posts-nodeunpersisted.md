# 120 — Ordinary edit posts NodeUnpersisted

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [119 — Add Notice.NodeUnpersisted](119-add-notice-nodeunpersisted.md)

## 3. Look at

### Owner

[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) owns `Context`, `postEvent`, `persistNew`, `persist`, `pushChangeSpecials`, `markForPersist`, `writeAxis`, and `logNotice`

### Source

[ ] [CoreMailboxEvents.fs](src/Server/Core/CoreMailboxEvents.fs) `eventDispatchContext`, `dispatchPostGraphOnly`
[ ] [CoreMailbox.fs](src/Server/Core/CoreMailbox.fs) `hostPublishing`, `MailboxHost.create`, `bindPersistFinished`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `placeOrder`, `replyEdit`, `dispatchItem`, `applyNotice`

### Helper

[ ] [History.fs](src/Shared/History.fs) `Notice`, `Ev`, `EventBody`
[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) `ChangeOrigin`, `Order.Change`, `toEv`
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `postNotice`, `postEvents`, `getEventsSince`, `eventHistory`
[ ] [DocumentOpImpact.fs](src/Shared/DocumentOpImpact.fs) `owningSpecials`
[ ] [GraphMutate.fs](src/Shared/GraphMutate.fs) `setUnpersistedAt`
[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) `PersistHandlers.replaceGraph`

### Copy-from

[ ] [Api.fs](src/Server/Api.fs) `postChange`, `postParseFile`

### Match sites

[ ] `placeOrder` sends `Order.Change (ops, ChangeOrigin.Edit)` to `replyEdit`, then `postEvent` with `graphOnly = false`
[ ] `persist` calls `pushChangeSpecials`, which folds `markForPersist` over `owningSpecials` and then `pushPersist`
[x] `markForPersist` logs `Notice.NodeUnpersisted` through `logNotice`, then `writeAxis` with `setUnpersistedAt` at `completed.id`
[ ] `postNotice` enqueues `CoreQueue.Notice`; `dispatchItem` runs `logNotice`, then `applyNotice`
[ ] `dispatchPostEvent` posts Undo, Redo, and a legacy Change with `graphOnly = false`
[ ] `dispatchPostGraphOnly` posts with `graphOnly = true`

### Tests this ticket names

[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `editFileText`, `ordinary edit posts NodeUnpersisted`, `An Edit pushes its owning special node`, `An Edit marks its owning special node at the Edit's id`, `A graph-only change pushes nothing`

## 4. Context

### Ordinary Edit

[ ] An ordinary Edit is `Order.Change(_, ChangeOrigin.Edit)` placed through `postEvents`
[ ] `editFileText` sets `commandName` to `"Edit"`
[ ] `postedEvent` sets `commandName` to `"Set text"`
[ ] Both command names are ordinary Edits when `replyEdit` posts them

### Today

[ ] `Context` is `{ persist: PersistHandlers; eventLog: EventLog ref; restartTip: EventId }`
[x] `hostPublishing` passes `CoreMailboxApi.postNotice created` to `bindPersistFinished`
[x] `eventDispatchContext` copies `persist`, `eventLog`, and `restartTip` into `Context`
[x] `PersistThread.runRequest` posts `Notice.NodePersisting` through its `postNotice` when the taken node is not still Persisted, then `Notice.NodePersisted` or `Notice.PersistFailed`
[x] `CoreMailboxLoop.applyNotice` leaves the graph unchanged for `Notice.NodePersisting`
[x] `Database.writeNoticeRow` writes no row for `Notice.NodePersisting`
[x] `Notice.NodeUnpersisted` is what sets Unpersisted

## 5. Changes

### Persist thread

[x] `PersistThread.runRequest` posts `Notice.NodePersisting nodeId` before the write when the node is not still Persisted
[x] That post uses the persist thread's `postNotice`, the same function that posts `Notice.NodePersisted`
[x] No new caller of `CoreMailboxApi.postNotice`

### Apply

[x] `Notice.NodePersisting` changes no node state
[x] `Database.writeNoticeRow` writes no row for `Notice.NodePersisting`
[x] `Notice.NodeUnpersisted` still sets Unpersisted

## 6. Tests

[x] `NodePersisting changes no node state` in [CoreLoopNoticeTests.fs](../../../tests/Server.Tests/CoreLoopNoticeTests.fs). Filter: `NodePersisting changes no node state`
[x] `NodePersisting writes no persist row` in [DbAgentNoticeTests.fs](../../../tests/Server.Tests/DbAgentNoticeTests.fs). Filter: `NodePersisting writes no persist row`
[x] `persist thread posts NodePersisted on success only` expects `Notice.NodePersisting` then `Notice.NodePersisted`, and `Notice.NodePersisting` then `Notice.PersistFailed` when the write fails
[x] `persist thread posts the version it read` expects `Notice.NodePersisting` then `Notice.NodePersisted`
[x] `CoreMailboxApi.postEvent appends an Ev that eventsSince returns` expects Change, then `Notice.NodePersisting`, then `Notice.NodePersisted`
[x] `new special node's first file write posts NodePersisting` expects `Notice.NodePersisting` for that workspace before `Notice.NodePersisted`
[x] `soft-fail Edit is not in the graph after restart` checks that no node text is `soft-fail-probe`. It does not assert `eventId`

## 7. Done
[x] Run `scripts/agent-test.sh` with no arguments.
[x] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[x] All executed tests pass.
