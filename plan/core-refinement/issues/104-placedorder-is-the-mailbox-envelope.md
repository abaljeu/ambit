# 104 — PlacedOrder is the mailbox envelope

**Status:** `defined`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] none

## 3. Look at

### Owner

[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) owns `Mailbox`, `TrackingDetails`, and `TrackingOrder`

### Source

[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) `PlacedOrder`, `MailboxLog.MailboxEventBody`, `MailboxLog.Ev`, `placedOrder`, `commandFromText`, `commandToText`, `ofEv`, `toEv`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `replyLoad`, `editEvent`, `replyEdit`, `placeOrder`, `dispatchMailbox`
[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) `postedMailbox`, `mbLoadNode`
[ ] [CoreChanges.fs](src/Server/Core/CoreChanges.fs) `Caller`, read only

### Match sites and constructors

[ ] `TrackingOrder` is built in `CoreMailboxApi.postedMailbox` and `CoreMailboxApi.mbLoadNode`
[ ] `tracked.tracking` is read in `replyLoad`, `editEvent`, `replyEdit`, and `dispatchMailbox`
[ ] `MailboxLog.MailboxEventBody.Order` is built in `MailboxLog.ofEv` and `CoreMailboxLoad.editEvent`
[ ] `MailboxLog.MailboxEventBody.Order` is matched in `MailboxLog.toEv` and `CoreMailboxApi.postedMailbox`

### Tests this ticket names

[ ] [MailboxExpandTests.fs](tests/Server.Tests/MailboxExpandTests.fs) `Mailbox is CoreMsg, PlaceOrder, and Ask` builds a `TrackingOrder`
[ ] [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `login during a pending logout runs and a dropped generation rejects a late order` builds a `TrackingOrder`
[ ] `SessionTests.fs` `admitted route order after DropAuthority is rejected` builds a `TrackingOrder`
[ ] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs) `placed` returns a `PlacedOrder`
[ ] `MailboxLogTests.fs` `ofEv then toEv keeps a Change`, `empty command name maps to None and returns as empty`, `Undo and Redo keep the target and drop the ops`, and `Load keeps its node through EventJson and MailboxLog` read `placed`
[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `postEvents places Order.Change with origin Edit` matches `MailboxEventBody.Order placed`

## 4. Context

### TrackingOrder

[ ] `TrackingDetails` is `{ orderId: OrderId; caller: Caller; commandName: string option }`
[ ] `TrackingOrder` is `{ tracking: TrackingDetails; order: Order; reply: AsyncReplyChannel<Result<EventId, string>> }`
[ ] `Mailbox.PlaceOrder` is `PlaceOrder of TrackingOrder`
[ ] `TrackingOrder` collects the parameters that `placeOrder` passes on

### Shared PlacedOrder

[ ] The shared `PlacedOrder` is `{ orderId: OrderId; authority: Authority; commandName: string option; order: Order }`
[ ] `MailboxLog.MailboxEventBody` is `Order of PlacedOrder | Notice of Notice`
[ ] `MailboxLog.Ev` is `{ id: EventId; body: MailboxEventBody }`
[ ] `ofEv` copies `submissionId`, `authority`, and `commandName` from the History `Ev` into the shared `PlacedOrder`
[ ] `toEv` copies them back into the History `Ev`
[ ] `commandFromText` maps `""` to `None`
[ ] `commandToText` maps `None` to `""`
[ ] `CoreMailboxLoad.editEvent` builds the stored Ev through `toEv`
[ ] `CoreMailboxApi.postedMailbox` reads the order through `ofEv`

### Caller

[ ] `Caller` is a server type in `CoreChanges.fs`
[ ] `dispatchMailbox` checks `authorityHeld` of the caller before `placeOrder`
[ ] The History `Ev` keeps `submissionId`, `authority`, and `commandName`

## 5. Changes

### Change

[ ] One change: the server `PlacedOrder` replaces `TrackingOrder` and the shared `PlacedOrder`

### CoreMsg.fs

[ ] Add `type internal PlacedOrder = { orderId: OrderId; caller: Caller; commandName: string option; order: Order; reply: AsyncReplyChannel<Result<EventId, string>> }`
[ ] `Mailbox.PlaceOrder` is `PlaceOrder of PlacedOrder`
[ ] Remove `TrackingDetails`
[ ] Remove `TrackingOrder`

### MailboxLog.fs

[ ] Remove the shared `PlacedOrder`
[ ] `MailboxEventBody.Order` is `Order of Order`
[ ] `MailboxLog.Ev` is `{ id: EventId; submissionId: System.Guid; authority: Authority; commandName: string; body: MailboxEventBody }`
[ ] `MailboxLog.Ev` has the same fields as the History `Ev`
[ ] `ofEv` copies `id`, `submissionId`, `authority`, and `commandName` onto `MailboxLog.Ev`
[ ] `toEv` copies them back
[ ] `commandFromText` becomes public as `MailboxLog.commandName`
[ ] `commandToText` becomes public as `MailboxLog.commandText`
[ ] Remove `placedOrder`

### CoreMailboxLoad.fs

[ ] `replyLoad`, `editEvent`, `replyEdit`, and `placeOrder` take `PlacedOrder`
[ ] They read `placed.caller`, `placed.orderId`, `placed.commandName`, `placed.order`, and `placed.reply`
[ ] `editEvent` builds `MailboxLog.Ev` with `submissionId = placed.orderId.Value`
[ ] `editEvent` sets `authority = placed.caller.authority`
[ ] `editEvent` sets `commandName = MailboxLog.commandText placed.commandName`
[ ] `editEvent` sets `body = MailboxEventBody.Order placed.order`
[ ] `dispatchMailbox` checks `authorityHeld context placed.caller`

### CoreMailboxApi.fs

[ ] `postedMailbox` builds `PlacedOrder` with `orderId = OrderId event.submissionId`
[ ] `postedMailbox` sets `caller`, `commandName = MailboxLog.commandName event.commandName`, `order` from `MailboxEventBody.Order`, and `reply = channel`
[ ] `mbLoadNode` builds `PlacedOrder` with `orderId = OrderId.New()` and `commandName = None`

### Size

[ ] 4 source files: `CoreMsg.fs`, `MailboxLog.fs`, `CoreMailboxLoad.fs`, `CoreMailboxApi.fs`
[ ] 4 test files: `MailboxExpandTests.fs`, `SessionTests.fs`, `MailboxLogTests.fs`, `CoreMailboxDoorTests.fs`

## 6. Tests

### Mailbox shape

[ ] Change `Mailbox is CoreMsg, PlaceOrder, and Ask` in `MailboxExpandTests.fs`
[ ] It builds `Mailbox.PlaceOrder` of a `PlacedOrder`
[ ] Filter: `Mailbox is CoreMsg, PlaceOrder, and Ask`

### Session constructors

[ ] In `SessionTests.fs`, `login during a pending logout runs and a dropped generation rejects a late order` builds a `PlacedOrder`
[ ] In the same file, `admitted route order after DropAuthority is rejected` builds a `PlacedOrder`
[ ] Filter: `login during a pending logout runs and a dropped generation rejects a late order`
[ ] Filter: `admitted route order after DropAuthority is rejected`

### MailboxLog

[ ] In `MailboxLogTests.fs`, `placed` returns the `Order`
[ ] `ofEv then toEv keeps a Change` reads `orderId`, `authority`, and `commandName` from `MailboxLog.Ev`
[ ] `empty command name maps to None and returns as empty` checks `MailboxLog.commandName ""` is `None`
[ ] `Undo and Redo keep the target and drop the ops` and `Load keeps its node through EventJson and MailboxLog` compare the `Order`
[ ] Filter: `ofEv then toEv keeps a Change`
[ ] Filter: `empty command name maps to None and returns as empty`
[ ] Filter: `Undo and Redo keep the target and drop the ops`
[ ] Filter: `Load keeps its node through EventJson and MailboxLog`

### Edit order

[ ] In `CoreMailboxDoorTests.fs`, `postEvents places Order.Change with origin Edit` matches `MailboxEventBody.Order order`
[ ] Filter: `postEvents places Order.Change with origin Edit`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
