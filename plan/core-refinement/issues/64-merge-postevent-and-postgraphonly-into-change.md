# 64 — Place Edit changes as Order.Change

**Status:** `done`
**Trackers:** architecture

## 1. Type

[ ] `coding`

## 2. Blockers

[ ] [103 — Change reply is the event id](103-change-reply-is-the-event-id.md)
[ ] [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
[ ] [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
[ ] [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)

## 3. Look at

### Owner

[ ] [CoreMailboxApi.fs](src/Server/Core/CoreMailboxApi.fs) owns `postEvents`; `mbLoadNode`

### Source

[ ] [OrderId.fs](src/Shared/OrderId.fs) `OrderId`
[ ] [Gambol.Shared.fsproj](src/Shared/Gambol.Shared.fsproj) compiles `OrderId.fs` after `EventId.fs`
[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) `Order`, `orderOfBody` — `Order.Change(ops, ChangeOrigin.Edit)`, `historyBody` — op list, `ofEv`, `toEv`, `placedOrder`
[ ] [MailboxHost.fs](src/Server/Core/MailboxHost.fs) `postForReplies`, `postMailboxAndAsyncReply`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `placeOrder`, `dispatchMailbox` — `PlaceOrder of TrackingOrder`, `replyEdit`, `editEvent`, `replyLoad`
[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `postEvent`, `tryStored`, `prepare`
[ ] [CoreMailboxEvents.fs](src/Server/Core/CoreMailboxEvents.fs) `eventDispatchContext`, `replyStored`
[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) `TrackingDetails`, `TrackingOrder`, `Mailbox.PlaceOrder of TrackingOrder`, `CoreMsg.PostEvent`
[ ] [CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) `dispatchPostEvent` — Change arm `postEvent` `false`
[ ] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs) `ofEv then toEv keeps a Change` — `Order.Change(ops, ChangeOrigin.Edit)`
[ ] [MailboxExpandTests.fs](tests/Server.Tests/MailboxExpandTests.fs) `Mailbox is CoreMsg, PlaceOrder, and Ask` — `PlaceOrder of TrackingOrder`
[ ] [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `login during a pending logout runs and a dropped generation rejects a late order` — `PlaceOrder of TrackingOrder`, `Order.Change(ops, ChangeOrigin.Edit)`
[ ] [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `admitted route order after DropAuthority is rejected` — `PlaceOrder of TrackingOrder`, `Order.Change(ops, ChangeOrigin.Edit)`
[ ] [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `logout returns while the reconciliation route is open` — `CoreMailboxApi.postEvent` of `orderEvent "reconcile"`

### Tests this ticket names

[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `postedEvent`, `editFileText`, `hostRecordingPush`, `a resubmitted Change replies the stored id`, `An Edit pushes its owning special node`, `An Edit marks its owning special node at the Edit's id`

## 4. Context

### Order

[ ] `Order.Change` is `ops: Op list * origin: ChangeOrigin`

### OrderId

[ ] `OrderId` wraps the order Guid
[ ] `OrderId` is a copy of `NodeId`
[ ] `Ev.submissionId` stays `System.Guid`
[ ] `OrderId event.submissionId` converts the Guid to `OrderId`
[ ] `orderId.Value` converts `OrderId` to `System.Guid`

### TrackingOrder

[ ] `TrackingDetails` is the envelope
[ ] `TrackingDetails` holds `orderId`, `caller`, and `commandName`
[ ] `TrackingOrder` holds `tracking`, `order`, and `reply`
[ ] `tracking` is `TrackingDetails`
[ ] `order` is an `Order`
[ ] `reply` is the mailbox reply channel

### PlaceOrder

[ ] `Mailbox.PlaceOrder` is `PlaceOrder of TrackingOrder`
[ ] The reply is `Result<EventId, string>`
[ ] `dispatchMailbox` passes `TrackingOrder` to `placeOrder`
[ ] `placeOrder` on `ChangeOrigin.Edit` calls `replyEdit`
[ ] `placeOrder` replies `Error "unsupported"` on `ChangeOrigin.Parse`
[ ] `replyLoad` replies `Ok` of the stored Ev's `id`
[ ] `replyLoad` passes `tracked.tracking.orderId.Value` to `appendLifecycle`
[ ] `mbLoadNode` posts `Mailbox.PlaceOrder` of `Order.Load`
[ ] `mbLoadNode` sets `orderId` to `OrderId.New()`

### postEvent Ev

[ ] `CoreEventDispatch.postEvent` takes a full `Ev`
[ ] `tryStored` keys on `submissionId`
[ ] A stored hit replies that Ev's id
[ ] `persistNew` requires `id` `EventId.zero`
[ ] `prepare` writes `authority` from the caller
[ ] `eventDispatchContext` builds the `CoreEventDispatch.Context`
[ ] `replyStored` replies `Ok` of the stored Ev's `id`

### postEvents

[ ] A zero-id `EventBody.Change` posts `Mailbox.PlaceOrder` of `TrackingOrder`
[ ] A Change at `EventId.Int` posts `CoreMsg.PostEvent`
[ ] Undo, Redo, ActorStart, and Cancel post `Mailbox.CoreMsg` of `CoreMsg.PostEvent`
[ ] It posts through `MailboxHost.postForReplies`
[ ] It rebuilds `CoreChangesAccepted` from the replied ids
[ ] `postForReplies` takes `(AsyncReplyChannel<'Reply> -> Mailbox) list`
[ ] `postForReplies` posts `CoreQueue.Public` of each item
[ ] `postMailboxAndAsyncReply` posts `CoreQueue.Public` of a `Mailbox`
[ ] `CoreMsg.PostEvent` replies `Result<EventId, string>`

### PostEvent bodies

[ ] A Change at `EventId.Int` is `PostEvent`
[ ] Undo is `PostEvent`
[ ] Redo is `PostEvent`
[ ] ActorStart is `PostEvent`
[ ] Cancel is `PostEvent`

### dispatchPostEvent

[ ] Change goes to `postEvent` with `graphOnly` false
[ ] Undo goes to `postEvent` with `graphOnly` false
[ ] Redo goes to `postEvent` with `graphOnly` false

### persist

[ ] `postEvent` takes `graphOnly: bool`
[ ] `graphOnly` false writes the document file
[ ] `pushChangeSpecials` marks each owning special Unpersisted at the event id
[ ] `pushChangeSpecials` logs `Notice.NodePersisting`
[ ] `pushChangeSpecials` pushes a persist request
[ ] `graphOnly` true is `dispatchPostGraphOnly`
[ ] `origin` exists only on the placed order
[ ] The logged Change holds the op list

### Callers of `postEvents`

[ ] `Api.postEvents`
[ ] `RunAgentActor`
[ ] `TestActor`

### Parse door

[ ] `CoreMsg.PostGraphOnly`
[ ] `postGraphOnly`

## 5. Changes

### OrderId

[x] `OrderId` wraps the order Guid
[x] `OrderId` is a copy of `NodeId`
[x] `Gambol.Shared.fsproj` compiles `OrderId.fs` after `EventId.fs`
[x] `Ev.submissionId` stays `System.Guid`
[x] `placedOrder` sets `orderId` to `OrderId event.submissionId`
[x] `toEv` sets `submissionId` to `placed.orderId.Value`

### TrackingOrder

[x] `TrackingDetails` holds `orderId`, `caller`, and `commandName`
[x] `TrackingOrder` holds `tracking`, `order`, and `reply`
[x] `Mailbox.PlaceOrder` is `PlaceOrder of TrackingOrder`
[x] `postedMailbox` builds `TrackingOrder` from `MailboxLog.ofEv` for `EventBody.Change` at `EventId.zero`
[x] `placeOrder`, `replyEdit`, `replyLoad`, and `editEvent` take `TrackingOrder`
[x] `mbLoadNode` posts `PlaceOrder of TrackingOrder` with `OrderId.New()`

### MailboxLog

[x] `ChangeOrigin` is `Edit | Parse` beside `Order`
[x] `Order.Change` is `ops: Op list * origin: ChangeOrigin`
[x] `orderOfBody` maps a logged Change to origin `Edit`
[x] `ofEv` maps a logged Change to origin `Edit`
[x] `historyBody` writes the op list
[x] `toEv` writes the op list

### MailboxHost

[x] `postForReplies` takes `(AsyncReplyChannel<'Reply> -> Mailbox) list`
[x] It posts `CoreQueue.Public` of each item, the wrap `postMailboxAndAsyncReply` already uses

### postEvents

[x] `EventBody.Change` at `EventId.zero` posts `Mailbox.PlaceOrder` of `Order.Change` with origin `Edit`
[x] The `TrackingOrder` `orderId` is `OrderId` of the Ev `submissionId`
[x] The `TrackingDetails` `commandName` is the same empty-string map `ofEv` already uses
[x] Undo in the list posts `Mailbox.CoreMsg` of `CoreMsg.PostEvent`
[x] Redo in the list posts `Mailbox.CoreMsg` of `CoreMsg.PostEvent`
[x] ActorStart in the list posts `Mailbox.CoreMsg` of `CoreMsg.PostEvent`
[x] Cancel in the list posts `Mailbox.CoreMsg` of `CoreMsg.PostEvent`
[x] `postForReplies` posts those `Mailbox` items back to back
[x] The `PlaceOrder` reply is the `EventId`
[x] `postEvents` rebuilds `CoreChangesAccepted` from that id

### placeOrder

[x] `dispatchMailbox` passes `TrackingOrder` to `placeOrder`
[x] Origin `Edit` matches `Order.Change` with `ChangeOrigin.Edit`
[x] Origin `Edit` builds an `Ev`
[x] Origin `Edit` calls `CoreEventDispatch.postEvent` with that Ev and `graphOnly` false, through `eventDispatchContext`
[x] The Ev `id` is `EventId.zero`
[x] The Ev `submissionId` is `tracked.tracking.orderId.Value`
[x] The Ev `commandName` is the same unwrap `toEv` already uses
[x] The Ev `body` is `EventBody.Change ops`
[x] `prepare` fills the authority
[x] The reply is `Ok` of the stored Ev's `id`, the way `replyLoad` replies
[x] The document file is written
[x] `pushChangeSpecials` runs

## 6. Tests

### Edit order

[x] Add `postEvents places Order.Change with origin Edit` in [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)
[x] Post `postedEvent` through `postEvents`
[x] `MailboxLog.ofEv` on the stored Ev from `getEventsSince` is `Order.Change` with origin `Edit`
[x] The reply is `Ok` of the new `EventId`
[x] Filter: `postEvents places Order.Change with origin Edit`

### Resubmission

[x] Keep `a resubmitted Change replies the stored id` in [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)
[x] A second `postEvents` of the same Change replies `Ok` of the stored `EventId`
[x] Filter: `a resubmitted Change replies the stored id`

### Mapped origin

[x] Change `ofEv then toEv keeps a Change` in [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs)
[x] The mapped order is origin `Edit`
[x] The round trip keeps the ops
[x] Filter: `ofEv then toEv keeps a Change`

### Ripple constructors

[x] In [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `login during a pending logout runs and a dropped generation rejects a late order`, `Order.Change(ops, ChangeOrigin.Edit)`
[x] In the same file, `admitted route order after DropAuthority is rejected`, `Order.Change(ops, ChangeOrigin.Edit)`
[x] The posted-order constructors of `Order.Change` in tests are `login during a pending logout runs and a dropped generation rejects a late order` and `admitted route order after DropAuthority is rejected`
[x] Filter: `login during a pending logout runs and a dropped generation rejects a late order`
[x] Filter: `admitted route order after DropAuthority is rejected`
[x] Filter: `logout returns while the reconciliation route is open`

### Edit persist

[x] Keep `An Edit pushes its owning special node` in [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)
[x] The document file is written
[x] A persist request is pushed
[x] Filter: `An Edit pushes its owning special node`
[x] Keep `An Edit marks its owning special node at the Edit's id` in [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)
[x] The owning special is Unpersisted at the Change's event id
[x] Filter: `An Edit marks its owning special node at the Edit's id`

### Other bodies

[x] Add `Undo, Redo, ActorStart, and Cancel still post as PostEvent` in [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)
[x] Undo through `postEvents` replies `Ok` and stores `EventBody.Undo`
[x] Redo through `postEvents` replies `Ok` and stores `EventBody.Redo`
[x] ActorStart through `postEvents` replies `Ok` and stores `EventBody.ActorStart`
[x] Cancel through `postEvents` stays on `CoreMsg.PostEvent`
[x] An idle Cancel acks `EventId.zero` with no row
[x] Filter: `Undo, Redo, ActorStart, and Cancel still post as PostEvent`

## 7. Done
[x] Run `scripts/agent-test.sh` with no arguments.
[x] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[x] All executed tests pass.
