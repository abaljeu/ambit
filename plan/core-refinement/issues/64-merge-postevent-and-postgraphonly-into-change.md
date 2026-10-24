# 64 — Place Edit changes as Order.Change

**Status:** `coded`
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

[ ] [MailboxLog.fs](src/Shared/MailboxLog.fs) `Order`, `orderOfBody` — `Order.Change ops`, `historyBody` — `Order.Change ops`, `ofEv`, `toEv`
[ ] [MailboxHost.fs](src/Server/Core/MailboxHost.fs) `postForReplies`, `postMailboxAndAsyncReply`
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `placeOrder` — `Order.Change _`, `dispatchMailbox` — `PlaceOrder (caller, orderId, _, order, reply)`, `replyLoad`
[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `postEvent`, `tryStored`, `prepare`
[ ] [CoreMailboxEvents.fs](src/Server/Core/CoreMailboxEvents.fs) `eventDispatchContext`, `replyStored`
[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) `Mailbox.PlaceOrder`, `CoreMsg.PostEvent`
[ ] [CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) `dispatchPostEvent` — Change arm `postEvent` `false`
[ ] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs) `ofEv then toEv keeps a Change` — `Order.Change ops`
[ ] [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `login during a pending logout runs and a dropped generation rejects a late order` — `Order.Change [ ops ]`
[ ] [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `logout returns while the reconciliation route is open` — `Order.Change [ ops ]`

### Tests this ticket names

[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `postedEvent`, `editFileText`, `hostRecordingPush`, `a resubmitted Change replies the stored id`, `An Edit pushes its owning special node`, `An Edit marks its owning special node at the Edit's id`

## 4. Context

### Order

[ ] `Order.Change` is `ops: Op list`

### PlaceOrder

[ ] `Mailbox.PlaceOrder` is `caller * orderId * commandName: string option * order * reply`
[ ] The reply is `Result<EventId, string>`
[ ] `dispatchMailbox` passes `caller`, `orderId`, `order`, and `reply` to `placeOrder`
[ ] `placeOrder` replies `Error "unsupported"` on `Order.Change`
[ ] `replyLoad` replies `Ok` of the stored Ev's `id`
[ ] `mbLoadNode` posts `Mailbox.PlaceOrder` of `Order.Load`

### postEvent Ev

[ ] `CoreEventDispatch.postEvent` takes a full `Ev`
[ ] `tryStored` keys on `submissionId`
[ ] A stored hit replies that Ev's id
[ ] `persistNew` requires `id` `EventId.zero`
[ ] `prepare` writes `authority` from the caller
[ ] `eventDispatchContext` builds the `CoreEventDispatch.Context`
[ ] `replyStored` replies `Ok` of the stored Ev's `id`

### postEvents

[ ] `postEvents` posts `CoreMsg.PostEvent` for every `Ev`
[ ] It posts through `MailboxHost.postForReplies`
[ ] It rebuilds `CoreChangesAccepted` from the replied ids
[ ] `postForReplies` takes `(AsyncReplyChannel<'Reply> -> CoreMsg) list`
[ ] It wraps each with `coreMsgItem`
[ ] `postMailboxAndAsyncReply` posts `CoreQueue.Public` of a `Mailbox`
[ ] `CoreMsg.PostEvent` replies `Result<EventId, string>`

### PostEvent bodies

[ ] Change is `PostEvent`
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

[x] `EventBody.Change` posts `Mailbox.PlaceOrder` of `Order.Change` with origin `Edit`
[x] The PlaceOrder `orderId` is the Ev `submissionId`
[x] The PlaceOrder `commandName` is the same empty-string map `ofEv` already uses
[x] Undo in the list posts `Mailbox.CoreMsg` of `CoreMsg.PostEvent`
[x] Redo in the list posts `Mailbox.CoreMsg` of `CoreMsg.PostEvent`
[x] ActorStart in the list posts `Mailbox.CoreMsg` of `CoreMsg.PostEvent`
[x] Cancel in the list posts `Mailbox.CoreMsg` of `CoreMsg.PostEvent`
[x] `postForReplies` posts those `Mailbox` items back to back
[x] The `PlaceOrder` reply is the `EventId`
[x] `postEvents` rebuilds `CoreChangesAccepted` from that id

### placeOrder

[x] `dispatchMailbox` passes `commandName` to `placeOrder`
[x] Origin `Edit` matches `Order.Change` with `ChangeOrigin.Edit`
[x] Origin `Edit` builds an `Ev`
[x] Origin `Edit` calls `CoreEventDispatch.postEvent` with that Ev and `graphOnly` false, through `eventDispatchContext`
[x] The Ev `id` is `EventId.zero`
[x] The Ev `submissionId` is the `orderId`
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

[x] In [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `login during a pending logout runs and a dropped generation rejects a late order`, `Order.Change [ ops ]` becomes `Order.Change(ops, ChangeOrigin.Edit)`
[ ] In [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `logout returns while the reconciliation route is open`, `Order.Change [ ops ]` becomes `Order.Change(ops, ChangeOrigin.Edit)`
[ ] Those two constructors and `ofEv then toEv keeps a Change` are every `Order.Change` site
[x] Filter: `login during a pending logout runs and a dropped generation rejects a late order`
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
[ ] Cancel through `postEvents` replies `Ok` and stores `EventBody.Cancel`
[x] Filter: `Undo, Redo, ActorStart, and Cancel still post as PostEvent`

## 7. Done
[x] Run `scripts/agent-test.sh` with no arguments.
[x] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[x] All executed tests pass.
