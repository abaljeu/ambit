# 64 — Place Edit changes as Order.Change

**Status:** `defined`
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
[ ] [CoreMailboxLoad.fs](src/Server/Core/CoreMailboxLoad.fs) `placeOrder` — `Order.Change _`, `dispatchMailbox`
[ ] [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `postEvent`, `persist`, `persistNew`, `pushChangeSpecials`
[ ] [CoreMsg.fs](src/Server/Core/CoreMsg.fs) `Mailbox.PlaceOrder`, `CoreMsg.PostEvent`
[ ] [CoreMailboxActors.fs](src/Server/Core/CoreMailboxActors.fs) `dispatchPostEvent`
[ ] [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs) `ofEv then toEv keeps a Change` — `Order.Change ops`
[ ] [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `login during a pending logout runs and a dropped generation rejects a late order` — `Order.Change [ ops ]`
[ ] [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `logout returns while the reconciliation route is open` — `Order.Change [ ops ]`

### Tests this ticket names

[ ] [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)

## 4. Context

### Order

[ ] `Order.Change` is `ops: Op list`

### PlaceOrder

[ ] `Mailbox.PlaceOrder` is `caller * orderId * commandName * order * reply`
[ ] The reply is `Result<EventId, string>`
[ ] `placeOrder` replies `Error "unsupported"` on `Order.Change`
[ ] `mbLoadNode` posts `Mailbox.PlaceOrder` of `Order.Load`

### postEvents

[ ] `postEvents` posts `CoreMsg.PostEvent` for every `Ev`
[ ] It posts through `MailboxHost.postForReplies`
[ ] It rebuilds `CoreChangesAccepted` from the replied ids
[ ] `postForReplies` builds `Mailbox.CoreMsg`

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

[ ] `ChangeOrigin` is `Edit | Parse` beside `Order`
[ ] `Order.Change` is `ops: Op list * origin: ChangeOrigin`
[ ] `orderOfBody` maps a logged Change to origin `Edit`
[ ] `ofEv` maps a logged Change to origin `Edit`
[ ] `historyBody` writes the op list
[ ] `toEv` writes the op list

### postEvents

[ ] `EventBody.Change` posts `Mailbox.PlaceOrder` of `Order.Change` with origin `Edit`
[ ] Undo in the list posts `CoreMsg.PostEvent`
[ ] Redo in the list posts `CoreMsg.PostEvent`
[ ] ActorStart in the list posts `CoreMsg.PostEvent`
[ ] Cancel in the list posts `CoreMsg.PostEvent`
[ ] `postForReplies` posts those `Mailbox` items back to back
[ ] The `PlaceOrder` reply is the `EventId`
[ ] `postEvents` rebuilds `CoreChangesAccepted` from that id

### placeOrder

[ ] Origin `Edit` calls `CoreEventDispatch`
[ ] The document file is written
[ ] `pushChangeSpecials` runs

### CoreEventDispatch

[ ] Reads `origin`
[ ] Origin `Edit` uses today's `graphOnly` false persist path

## 6. Tests

### Edit order

[ ] Add `postEvents places Order.Change with origin Edit` in [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)
[ ] A Change through `postEvents` is `Order.Change` with origin `Edit`
[ ] The reply is `Ok` of the new `EventId`
[ ] Filter: `postEvents places Order.Change with origin Edit`

### Mapped origin

[ ] Change `ofEv then toEv keeps a Change` in [MailboxLogTests.fs](tests/Shared.Tests/MailboxLogTests.fs)
[ ] The mapped order is origin `Edit`
[ ] The round trip keeps the ops
[ ] Filter: `ofEv then toEv keeps a Change`

### Ripple constructors

[ ] In [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `login during a pending logout runs and a dropped generation rejects a late order`, `Order.Change [ ops ]` becomes `Order.Change(ops, ChangeOrigin.Edit)`
[ ] In [SessionTests.fs](tests/Server.Tests/SessionTests.fs) `logout returns while the reconciliation route is open`, `Order.Change [ ops ]` becomes `Order.Change(ops, ChangeOrigin.Edit)`
[ ] Filter: `login during a pending logout runs and a dropped generation rejects a late order`
[ ] Filter: `logout returns while the reconciliation route is open`

### Edit persist

[ ] `An Edit pushes its owning special node` in [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)
[ ] The document file is written
[ ] A persist request is pushed
[ ] Filter: `An Edit pushes its owning special node`
[ ] `An Edit marks its owning special node at the Edit's id` in [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)
[ ] The owning special is Unpersisted at the Change's event id
[ ] Filter: `An Edit marks its owning special node at the Edit's id`

### Other bodies

[ ] Add `Undo, Redo, ActorStart, and Cancel still post as PostEvent` in [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs)
[ ] Undo through `postEvents` is `CoreMsg.PostEvent`
[ ] Redo through `postEvents` is `CoreMsg.PostEvent`
[ ] ActorStart through `postEvents` is `CoreMsg.PostEvent`
[ ] Cancel through `postEvents` is `CoreMsg.PostEvent`
[ ] Filter: `Undo, Redo, ActorStart, and Cancel still post as PostEvent`

## 7. Done
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] If any added test was missed, run `scripts/agent-test.sh --filter` for each added test.
[ ] All executed tests pass.
