# 65 — Place Undo and Redo as orders

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [108 — Log Change as EventBody.Order](108-log-change-as-eventbody-order.md)

## 3. Look at

### Owner

[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) owns `placeOrder`, `editEvent`, `replyChange`

### Source

[ ] [History.fs](src/Shared/History.fs) `Order.Undo`, `Order.Redo`, `Ev.ops`, `Ev.target`, `Ev.withOps`
[ ] [EventJson.fs](src/Shared/EventJson.fs) `encodeOrder`, `decodeOrder`
[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) `orderOfBody`, `historyBody`
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `postedMailbox`
[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `completeAction`, read only
[ ] [CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) `dispatchPostEvent`, read only

### Match sites and constructors

[ ] `Order.Undo` and `Order.Redo` are matched in `CoreMailboxLoad.placeOrder`, `MailboxLog.historyBody`, `EventJson.encodeOrder`, and the `EventBody.Order` arms of `Ev.ops`, `Ev.target`, and `Ev.withOps`
[ ] `Order.Undo` and `Order.Redo` are built in `MailboxLog.orderOfBody` and `EventJson.decodeOrder`
[ ] Tests build `Order.Undo` and `Order.Redo` in `MailboxLogTests.fs` `Undo and Redo keep the target and drop the ops` only

### Tests this ticket names

[ ] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs) `Undo and Redo keep the target and drop the ops`
[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `Undo, Redo, ActorStart, and Cancel still post as PostEvent`
[ ] [EventJsonTests.fs](tests/Shared.Tests/EventJsonTests.fs) `Ev JSON round-trips each EventBody.Order case`
[ ] [Issue41CoreMailboxTests.fs](tests/Server.Tests/Issue41CoreMailboxTests.fs) `name-only Undo and Redo store completed Events with submission ids`

## 4. Context

### Today

[ ] `EventBody.Undo` and `EventBody.Redo` hold `target * ops`
[ ] `Order.Undo` and `Order.Redo` hold only `target`
[ ] `MailboxLog.orderOfBody` drops the ops
[ ] `MailboxLog.historyBody` writes an empty Op list
[ ] `postedMailbox` places only a zero-id Change; an Undo or Redo posts `CoreMsg.PostEvent`
[ ] `dispatchPostEvent` sends that Undo or Redo to `CoreEventDispatch.postEvent` with `graphOnly` false
[ ] `completeAction` fills an empty Op list from the inverse ops of the target
[ ] `completeAction` refuses a target from before the restart
[ ] `replyChange` builds the Ev through `editEvent` and replies `Ok` of the stored id

### Decision

[ ] Alan, 2026-10-24: `Order.Undo` and `Order.Redo` carry `target * ops`, the same shape as the logged Undo and Redo

## 5. Changes

### Change

[ ] One change: Undo and Redo are placed as `Order.Undo` and `Order.Redo` of `target * ops`

### History.fs

[ ] `Order.Undo` is `Undo of target: EventId * ops: Op list`
[ ] `Order.Redo` is `Redo of target: EventId * ops: Op list`
[ ] `Ev.ops` of `EventBody.Order (Order.Undo(_, ops))` or `Order.Redo(_, ops)` is `Some ops`
[ ] `Ev.target` of those bodies is `Some target`
[ ] `Ev.withOps` of those bodies keeps the target and sets the ops

### EventJson.fs

[ ] Order kinds `undo` and `redo` write and read `target` and `ops`

### MailboxLog.fs

[ ] `orderOfBody` maps `EventBody.Undo(target, ops)` to `Order.Undo(target, ops)`
[ ] `orderOfBody` maps `EventBody.Redo(target, ops)` to `Order.Redo(target, ops)`
[ ] `historyBody` maps `Order.Undo(target, ops)` to `EventBody.Undo(target, ops)`
[ ] `historyBody` maps `Order.Redo(target, ops)` to `EventBody.Redo(target, ops)`

### CoreMailboxApi.fs

[ ] `postedMailbox` places a zero-id `EventBody.Undo` or `EventBody.Redo` as `Mailbox.PlaceOrder`, the same way as a zero-id Change

### CoreMailboxLoad.fs

[ ] `editEvent` becomes `orderEvent placed`; it builds the Ev from `placed.order`
[ ] `replyChange` takes `placed` and `graphOnly`
[ ] `placeOrder` sends `Order.Undo` and `Order.Redo` to `replyChange` with `graphOnly` false
[ ] `completeAction` fills an empty Op list, as today
[ ] The stored Undo or Redo holds the target and the filled Op list
[ ] The reply is `Ok` of the stored id

### Size

[ ] 5 source files: `History.fs`, `EventJson.fs`, `MailboxLog.fs`, `CoreMailboxApi.fs`, `CoreMailboxLoad.fs`
[ ] 3 test files: `MailboxLogTests.fs`, `CoreMailboxDoorTests.fs`, `EventJsonTests.fs`

## 6. Tests

### Undo order

[ ] Add `undo places Order.Undo` in `CoreMailboxDoorTests.fs`
[ ] After a Change, `postEvents` of an empty-op Undo of it replies `Ok` of a new `EventId`
[ ] The stored Undo holds the target and the inverse ops
[ ] The graph matches the state before the Change
[ ] Filter: `undo places Order.Undo`

### Redo order

[ ] Add `redo places Order.Redo` in `CoreMailboxDoorTests.fs`
[ ] The stored Redo holds the undone `EventId` and its ops
[ ] The graph matches the state after the Change
[ ] Filter: `redo places Order.Redo`

### PostEvent remainder

[ ] Rename `Undo, Redo, ActorStart, and Cancel still post as PostEvent` to `ActorStart and Cancel still post as PostEvent`
[ ] Remove its Undo and Redo checks
[ ] Filter: `ActorStart and Cancel still post as PostEvent`

### Round trip

[ ] Rename `Undo and Redo keep the target and drop the ops` to `Undo and Redo keep the target and the ops`
[ ] `ofEv` gives `Order.Undo(target, ops)` and `Order.Redo(target, ops)`
[ ] `toEv` of `ofEv` gives the same body
[ ] Filter: `Undo and Redo keep the target and the ops`

### JSON

[ ] In `Ev JSON round-trips each EventBody.Order case`, the Undo and Redo cases carry ops
[ ] Filter: `Ev JSON round-trips each EventBody.Order case`

### Kept

[ ] Keep `name-only Undo and Redo store completed Events with submission ids`
[ ] Filter: `name-only Undo and Redo store completed Events with submission ids`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
