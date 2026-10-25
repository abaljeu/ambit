# 102 — Place Parse changes as Order.Change

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [104 — PlacedOrder is the mailbox envelope](104-placedorder-is-the-mailbox-envelope.md)

## 3. Look at

### Owner

[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) owns `postGraphOnly`

### Source

[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) `CoreMsg.PostGraphOnly`, `Mailbox.PlaceOrder`
[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) `PlacedOrder`, `ofEv`, read only
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `placeOrder`, `editEvent`, `replyEdit`, `runMsg`, `messageCaller`
[ ] [CoreMailboxEvents.fs](src/Server/Core/CoreMailboxEvents.fs) `dispatchPostGraphOnly`, `operationContext`, `replyFailure`
[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `postEvent`, read only

### Match sites and callers

[ ] `CoreMsg.PostGraphOnly` is built in `CoreMailboxApi.postGraphOnly`
[ ] `CoreMsg.PostGraphOnly` is matched in `CoreMailboxLoad.runMsg`, `CoreMailboxLoad.messageCaller`, `CoreMailboxEvents.operationContext`, and `CoreMailboxEvents.replyFailure`
[ ] `placeOrder` replies `Error "unsupported"` on `Order.Change` with `ChangeOrigin.Parse`
[ ] `CoreDoor.postGraphOnly` calls `CoreMailboxApi.postGraphOnly`
[ ] `ParseThread`, `Api.applyParseFile`, and `LazyLoadReconciliationServer` through `GraphOnlyChangePost.postChunks` call `CoreDoor.postGraphOnly`
[ ] 17 test files call `CoreMailboxApi.postGraphOnly`; its signature stays, so they stay

### Tests this ticket names

[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `A graph-only change pushes nothing`, `Graph-only post reads authority without repeated admission`, `Graph-only post with admitted Caller reaches persist`
[ ] [Issue41CoreMailboxTests.fs](tests/Server.Tests/Issue41CoreMailboxTests.fs) `postGraphOnly updates Graph and EventLog without file write`

## 4. Context

### postGraphOnly

[ ] `postGraphOnly` takes `host`, `caller`, and a History `Ev`
[ ] It posts `CoreMsg.PostGraphOnly` through `reply`
[ ] The reply is `Result<EventId, string>`
[ ] It rebuilds `CoreChangesAccepted` through `acceptedFromIds`
[ ] `dispatchPostGraphOnly` calls `CoreEventDispatch.postEvent` with `graphOnly` true
[ ] `graphOnly` true skips the document file and the persist push

### placeOrder

[ ] `placeOrder` takes the shared `PlacedOrder`, the `Caller`, and the reply, as `Mailbox.PlaceOrder` carries them
[ ] `Order.Change` with `ChangeOrigin.Edit` calls `replyEdit`
[ ] `replyEdit` builds the Ev through `editEvent`, calls `postEvent` with `graphOnly` false, and replies `Ok` of the stored id
[ ] `postedMailbox` reads the shared `PlacedOrder` from an Ev through `MailboxLog.ofEv` and sets `authority = caller.authority`

## 5. Changes

### Change

[ ] One change: the parse door places `Order.Change` with origin `Parse`, and `CoreMsg.PostGraphOnly` leaves

### CoreMailboxApi.fs

[ ] `postGraphOnly` keeps its signature
[ ] It reads the ops with `Ev.ops event`
[ ] An Ev with no ops returns `Error "graph-only post needs a Change"`
[ ] It reads `placed` through `MailboxLog.ofEv event`, the way `postedMailbox` does
[ ] It posts `Mailbox.PlaceOrder({ placed with authority = caller.authority; order = Order.Change(ops, ChangeOrigin.Parse) }, caller, channel)` through `MailboxHost.postMailboxAndAsyncReply`
[ ] `orderId` and `commandName` come from `ofEv`: `OrderId event.submissionId` and the event's command name
[ ] It rebuilds `CoreChangesAccepted` through `acceptedFromIds`, as today

### CoreMailboxLoad.fs

[ ] `replyEdit` becomes `replyChange context placed caller reply ops graphOnly`
[ ] `Order.Change` with `ChangeOrigin.Edit` calls `replyChange` with `graphOnly` false
[ ] `Order.Change` with `ChangeOrigin.Parse` calls `replyChange` with `graphOnly` true
[ ] Remove the `PostGraphOnly` arm from `runMsg`
[ ] Remove the `PostGraphOnly` arm from `messageCaller`

### CoreMailboxEvents.fs

[ ] Remove `dispatchPostGraphOnly`
[ ] Remove the `PostGraphOnly` arms from `operationContext` and `replyFailure`

### CoreMsg.fs

[ ] Remove `CoreMsg.PostGraphOnly`

### Size

[ ] 4 source files: `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`, `CoreMailboxEvents.fs`, `CoreMsg.fs`
[ ] 1 test file: `CoreMailboxDoorTests.fs`

## 6. Tests

### Parse order

[ ] Add `postGraphOnly places Order.Change with origin Parse` in `CoreMailboxDoorTests.fs`
[ ] `postGraphOnly` of a Change replies `Ok` with an accepted `eventId`
[ ] `getEventsSince` holds the stored Change at that id
[ ] No document file is written
[ ] No persist request is pushed
[ ] Filter: `postGraphOnly places Order.Change with origin Parse`

### Kept

[ ] Keep `A graph-only change pushes nothing`
[ ] Keep `Graph-only post reads authority without repeated admission`
[ ] Keep `Graph-only post with admitted Caller reaches persist`
[ ] Keep `postGraphOnly updates Graph and EventLog without file write` in `Issue41CoreMailboxTests.fs`
[ ] Filter: `A graph-only change pushes nothing`
[ ] Filter: `Graph-only post`
[ ] Filter: `postGraphOnly updates Graph and EventLog without file write`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
