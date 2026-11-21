# 104 — PlacedOrder is the mailbox envelope

**Status:** `coded`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] none

## 3. Look at

### Owner

[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) owns `Mailbox`, `TrackingDetails`, and `TrackingOrder`

### Source

[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) `PlacedOrder`, `MailboxLog.MailboxEventBody`, `ofEv`, `toEv`, read only
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `replyLoad`, `editEvent`, `replyEdit`, `placeOrder`, `dispatchMailbox`, `isStartupRead`
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `postedMailbox`, `mbLoadNode`
[ ] [CoreChanges.fs](src/Server/Core/CoreChanges.fs) `Caller`, read only

### Match sites and constructors

[ ] `Mailbox.PlaceOrder` is built in `CoreMailboxApi.postedMailbox` and `CoreMailboxApi.mbLoadNode`
[ ] It is built in tests in `MailboxExpandTests.fs` (1) and `SessionTests.fs` (2)
[ ] It is matched in `CoreMailboxLoad.dispatchMailbox` and in the `Mailbox.PlaceOrder _ -> false` arm of `CoreMailboxLoad.isStartupRead`, and in `MailboxExpandTests.mailboxKind`
[ ] `tracked.tracking` is read in `replyLoad`, `editEvent`, `replyEdit`, and `dispatchMailbox`

### Tests this ticket names

[ ] [MailboxExpandTests.fs](tests/Server.Tests/MailboxExpandTests.fs) `Mailbox is CoreMsg, PlaceOrder, and Ask` builds a `TrackingOrder`
[ ] [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `login during a pending logout runs and a dropped generation rejects a late order` builds a `TrackingOrder`
[ ] `SessionTests.fs` `admitted route order after DropAuthority is rejected` builds a `TrackingOrder`

## 4. Context

### TrackingOrder

[ ] `TrackingDetails` is `{ orderId: OrderId; caller: Caller; commandName: string option }`
[ ] `TrackingOrder` is `{ tracking: TrackingDetails; order: Order; reply: AsyncReplyChannel<Result<EventId, string>> }`
[ ] `Mailbox.PlaceOrder` is `PlaceOrder of TrackingOrder`

### Shared PlacedOrder

[ ] The shared `PlacedOrder` in `MailboxLog.fs` is `{ orderId: OrderId; authority: Authority; commandName: string option; order: Order }`
[ ] `MailboxLog.MailboxEventBody` is `Order of PlacedOrder | Notice of Notice`
[ ] `MailboxLog.Ev` is `{ id: EventId; body: MailboxEventBody }`
[ ] `ofEv` copies `submissionId`, `authority`, and `commandName` from the History `Ev` into the shared `PlacedOrder`; `toEv` copies them back
[ ] `CoreMailboxApi.postedMailbox` reads the shared `PlacedOrder` through `ofEv`
[ ] `CoreMailboxLoad.editEvent` builds `MailboxEventBody.Order` of a shared `PlacedOrder`, with `authority = caller.authority`, and stores it through `toEv`

### Caller

[ ] `Caller` is a server type in `CoreChanges.fs`, with `authority`, `name`, and `secret`
[ ] `dispatchMailbox` checks `authorityHeld` of the caller before `placeOrder`

### Decisions

[ ] Alan, 2026-10-24: the shared `PlacedOrder` stays in `MailboxLog.fs` as written; it is not moved or reshaped
[ ] Alan, 2026-10-24: `Mailbox.PlaceOrder` is `PlaceOrder of PlacedOrder * Caller * AsyncReplyChannel<Result<EventId, string>>`, a tuple with no wrapper record
[ ] `MailboxEventBody`, `MailboxLog.Ev`, `ofEv`, `toEv`, `commandFromText`, and `commandToText` do not change here

## 5. Changes

### Change

[ ] One change: `Mailbox.PlaceOrder` carries the shared `PlacedOrder`, the `Caller`, and the reply; `TrackingOrder` and `TrackingDetails` leave

### CoreMsg.fs

[ ] `Mailbox.PlaceOrder` is `PlaceOrder of PlacedOrder * Caller * AsyncReplyChannel<Result<EventId, string>>`
[ ] Remove `TrackingDetails`
[ ] Remove `TrackingOrder`

### CoreMailboxLoad.fs

[ ] `replyLoad`, `editEvent`, `replyEdit`, and `placeOrder` take `placed`, `caller`, and `reply` in place of `tracked`
[ ] They read `caller` for `tracked.tracking.caller`, `placed.orderId` for `tracked.tracking.orderId`, `placed.order` for `tracked.order`, and `reply` for `tracked.reply`
[ ] `editEvent` builds `MailboxEventBody.Order { placed with order = Order.Change(ops, ChangeOrigin.Edit) }` and stores it through `toEv`, as today
[ ] `dispatchMailbox` matches `Mailbox.PlaceOrder(placed, caller, reply)` and checks `authorityHeld context caller`
[ ] `isStartupRead` keeps `Mailbox.PlaceOrder _ -> false`

### CoreMailboxApi.fs

[ ] `postedMailbox` reads `placed` through `ofEv`, as today, and posts `Mailbox.PlaceOrder({ placed with authority = caller.authority }, caller, channel)`
[ ] The `authority` copy keeps the stored authority the caller's, as `editEvent` sets it today
[ ] `mbLoadNode` posts `Mailbox.PlaceOrder({ orderId = OrderId.New(); authority = caller.authority; commandName = None; order = Order.Load subject }, caller, channel)`

### Size

[ ] 3 source files: `CoreMsg.fs`, `CoreMailboxLoad.fs`, `CoreMailboxApi.fs`
[ ] 2 test files: `MailboxExpandTests.fs`, `SessionTests.fs`

## 6. Tests

### Mailbox shape

[ ] Change `Mailbox is CoreMsg, PlaceOrder, and Ask` in `MailboxExpandTests.fs`
[ ] It builds `Mailbox.PlaceOrder` of a shared `PlacedOrder`, `testCaller`, and the reply
[ ] Filter: `Mailbox is CoreMsg, PlaceOrder, and Ask`

### Session constructors

[ ] In `SessionTests.fs`, `login during a pending logout runs and a dropped generation rejects a late order` builds `Mailbox.PlaceOrder` of a shared `PlacedOrder`, `testCaller`, and the channel
[ ] In the same file, `admitted route order after DropAuthority is rejected` builds it the same way
[ ] Filter: `login during a pending logout runs and a dropped generation rejects a late order`
[ ] Filter: `admitted route order after DropAuthority is rejected`

### Kept

[ ] Keep `postEvents places Order.Change with origin Edit` in `CoreMailboxDoorTests.fs` and every `MailboxLogTests.fs` test
[ ] Filter: `postEvents places Order.Change with origin Edit`
[ ] Filter: `MailboxLogTests`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
