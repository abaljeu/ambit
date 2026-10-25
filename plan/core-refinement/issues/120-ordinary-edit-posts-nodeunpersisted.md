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
[ ] `markForPersist` logs `Notice.NodePersisting` through `writeAxis` with `setUnpersistedAt` at `completed.id`
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
[ ] `hostPublishing` passes `CoreMailboxApi.postNotice created` to `bindPersistFinished`
[ ] `eventDispatchContext` copies `persist`, `eventLog`, and `restartTip` into `Context`

## 5. Changes

### Change

[ ] One change: after a successful ordinary Edit, `postEvent` posts `Notice.NodeUnpersisted` through `postNotice`

### CoreEventDispatch.fs

[ ] `Context` gains a notice poster of type `Notice -> unit`
[ ] The success exit of `persistNew` posts `Notice.NodeUnpersisted nodeId` for each owning special of that Edit
[ ] The ids come from `owningSpecials` on the confirmed ops
[ ] The version is `completed.id`
[ ] The log gains `EventBody.Notice (Notice.NodeUnpersisted nodeId)` after the Change
[ ] For that Edit, `markForPersist` still calls `setUnpersistedAt` at `completed.id`
[ ] That Edit logs one notice, `Notice.NodeUnpersisted`
[ ] `pushChangeSpecials` still calls `pushPersist` for those owning specials
[ ] `EventBody.Undo` and `EventBody.Redo` still log `Notice.NodePersisting` through `markForPersist`
[ ] `graphOnly` true posts no `Notice.NodeUnpersisted`
[ ] `Op.SetDocumentState` stays in `Op`

### CoreMailboxLoad.fs

[ ] `replyEdit` passes the Edit gate into `postEvent`
[ ] The gate covers every `replyEdit`, including `commandName` `"Edit"` and `commandName` `"Set text"`

### CoreMailboxEvents.fs

[ ] `eventDispatchContext` copies the notice poster into `Context`

### CoreMailbox.fs

[ ] After `MailboxHost.create`, `hostPublishing` sets that poster to `CoreMailboxApi.postNotice created`, beside `bindPersistFinished`

## 6. Tests

### Add

[ ] Add `ordinary edit posts NodeUnpersisted` in `CoreMailboxDoorTests.fs`
[ ] The test calls `postEvents` with `editFileText`
[ ] The log from `getEventsSince` or `eventHistory` contains `EventBody.Notice (Notice.NodeUnpersisted fileId)` after the Change
[ ] The live graph shows Unpersisted at the Edit event id
[ ] Filter: `ordinary edit posts NodeUnpersisted`

### Mark

[ ] `An Edit marks its owning special node at the Edit's id` still expects `PersistState.Unpersisted`
[ ] That test still expects `changedVersion` equal to the second ack `eventId`
[ ] The Edit's notice in that log is the one `Notice.NodeUnpersisted` row
[ ] Filter: `An Edit marks its owning special node at the Edit's id`

### Push

[ ] `An Edit pushes its owning special node` still expects `pushPersist` to receive the file `NodeId`
[ ] Filter: `An Edit pushes its owning special node`
[ ] Filter: `A graph-only change pushes nothing`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
