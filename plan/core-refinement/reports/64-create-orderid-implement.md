# 64 — Create OrderId implement

Implement report for [64 — Place Edit changes as Order.Change](../issues/64-merge-postevent-and-postgraphonly-into-change.md). Both `OrderId` and `TrackingOrder` land. `TrackingOrder` is the placement envelope around `Order`.

## Types

[x] [OrderId.fs](../../../src/Shared/OrderId.fs) defines `[<Struct>] OrderId of Guid`, member `Value`, and `OrderId.New()`.
[x] [Gambol.Shared.fsproj](../../../src/Shared/Gambol.Shared.fsproj) compiles `OrderId.fs` immediately after `EventId.fs`.
[x] [CoreMsg.fs](../../../src/Server/Core/CoreMsg.fs) `TrackingDetails` is `orderId: OrderId`, `caller: Caller`, and `commandName: string option`.
[x] `TrackingOrder` is `tracking: TrackingDetails`, `order: Order`, and `reply: AsyncReplyChannel<Result<EventId, string>>`.
[x] `Mailbox.PlaceOrder` is `PlaceOrder of TrackingOrder`.

## Reply channel

[x] `Mailbox.CoreMsg` and `Mailbox.Ask` are one payload each, and that payload holds the reply channel.
[x] The reply channel is a field of `TrackingOrder`, beside `tracking` and `order`.
[x] `TrackingDetails` holds the placement facts: `OrderId`, the caller, and `commandName`.

## Guid boundary

[x] `Ev.submissionId` in [History.fs](../../../src/Shared/History.fs) stays `System.Guid`.
[x] [MailboxLog.fs](../../../src/Shared/MailboxLog.fs) `placedOrder` sets `orderId` to `OrderId event.submissionId`.
[x] `toEv` sets `submissionId` to `placed.orderId.Value`.
[x] [CoreMailboxLoad.fs](../../../src/Server/Core/CoreMailboxLoad.fs) `replyLoad` passes `tracked.tracking.orderId.Value` to `CoreEventDispatch.appendLifecycle`.
[x] `tryStored` still keys on `submissionId`.

## PlaceOrder path

[x] `postedMailbox` in [CoreMailboxApi.fs](../../../src/Server/Core/CoreMailboxApi.fs) builds `TrackingOrder` from `MailboxLog.ofEv`.
[x] The envelope `orderId` is `placed.orderId`.
[x] The envelope `commandName` is `placed.commandName`.
[x] The body is `placed.order`.
[x] `mbLoadNode` mints `OrderId.New()` on `TrackingDetails` and sets the body to `Order.Load subject`.
[x] `dispatchMailbox`, `placeOrder`, `replyLoad`, `replyEdit`, and `editEvent` take `TrackingOrder`.
[x] `editEvent` writes `EventId.zero`, `submissionId` from `tracked.tracking.orderId`, and `EventBody.Change` for origin `Edit`.
[x] Origin `Parse` replies `Error "unsupported"`.

## Tests

[x] ``ofEv then toEv keeps a Change`` in [MailboxLogTests.fs](../../../tests/Shared.Tests/MailboxLogTests.fs) expects `OrderId event.submissionId` and the same `Ev` after `toEv`.
[x] ``login during a pending logout runs and a dropped generation rejects a late order`` in [SessionTests.fs](../../../tests/Server.Tests/SessionTests.fs) posts `PlaceOrder` of `TrackingOrder`.
[x] ``admitted route order after DropAuthority is rejected`` posts `PlaceOrder` of `TrackingOrder`.
[x] ``Mailbox is CoreMsg, PlaceOrder, and Ask`` in [MailboxExpandTests.fs](../../../tests/Server.Tests/MailboxExpandTests.fs) builds `TrackingOrder` with `OrderId Guid.Empty`.
[x] ``postEvent runs through Mailbox CoreMsg`` passed. The stored `submissionId` stays the draft `Guid`.

## scripts/agent-test.sh

[x] `scripts/agent-test.sh` with no arguments passed.
[x] Server filter `FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~MailboxExpandTests|FullyQualifiedName~SessionTests`: 52 passed.
[x] Shared filter `FullyQualifiedName~MailboxLogTests`: 5 passed.
[x] Focused server filter for the four named facts: 4 passed.
[x] Focused shared filter for ``ofEv then toEv keeps a Change``: 1 passed.
[x] This pass adds no new test name.

## Ticket

[x] Status of 64 — Place Edit changes as Order.Change stays `coded`.

## Found elsewhere

### Tuple shape in the ticket

[x] **Problem.** 64 — Place Edit changes as Order.Change section 4 says `Mailbox.PlaceOrder` is `caller * orderId * commandName * order * reply`.
[x] **Fix.** Name `OrderId`, `TrackingDetails`, and `TrackingOrder` in that section.

### Draft guard already in the working tree

[x] **Problem.** `postedMailbox` already limited `EventBody.Change` to `event.id = EventId.zero` before this pass. That guard is still in the working tree.
[x] **Fix.** Keep the guard with the coded draft path, or review it on its own.
