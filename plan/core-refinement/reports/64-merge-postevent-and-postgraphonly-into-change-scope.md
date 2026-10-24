# 64 — Place Edit changes as Order.Change scope

Scope for [64 — Place Edit changes as Order.Change](../issues/64-merge-postevent-and-postgraphonly-into-change.md). Mode: implement. Branch: `dev`. Source unchanged in this pass.

## Signatures

[ ] `Gambol.Shared.Order` in [MailboxLog.fs](../../src/Shared/MailboxLog.fs) is `Change of ops: Op list | Undo | Redo | StartActor | StartPeerActor | LoadSave | Load | Cancel`.
[ ] `Gambol.Shared.PlacedOrder` in [MailboxLog.fs](../../src/Shared/MailboxLog.fs) is `{ orderId: Guid; authority: Authority; commandName: string option; order: Order }`.
[ ] `Gambol.Shared.MailboxLog.ofEv` in [MailboxLog.fs](../../src/Shared/MailboxLog.fs) is `HistoryEv -> Result<MailboxLog.Ev, string>`.
[ ] `Gambol.Shared.MailboxLog.toEv` in [MailboxLog.fs](../../src/Shared/MailboxLog.fs) is `MailboxLog.Ev -> Result<HistoryEv, string>`.
[ ] `Gambol.Server.Mailbox.PlaceOrder` in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) is `caller * orderId: Guid * commandName: string option * order: Order * reply: AsyncReplyChannel<Result<EventId, string>>`.
[ ] `Gambol.Server.CoreMsg.PostEvent` in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) is `caller * event: Ev * AsyncReplyChannel<Result<EventId, string>>`.
[ ] `Gambol.Server.CoreMsg.PostGraphOnly` in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) is `caller * event: Ev * AsyncReplyChannel<Result<EventId, string>>`.
[ ] `Gambol.Server.MailboxHost.postForReplies` in [MailboxHost.fs](../../src/Server/Core/MailboxHost.fs) is `MailboxHost -> (AsyncReplyChannel<'Reply> -> CoreMsg) list -> Async<'Reply list>`.
[ ] `Gambol.Server.MailboxHost.postMailboxAndAsyncReply` in [MailboxHost.fs](../../src/Server/Core/MailboxHost.fs) is `MailboxHost -> (AsyncReplyChannel<'a> -> Mailbox) -> Async<'a>`.
[ ] `Gambol.Server.MailboxHost.coreMsgItem` in [MailboxHost.fs](../../src/Server/Core/MailboxHost.fs) is `CoreMsg -> CoreQueue`.
[ ] `Gambol.Server.CoreMailboxLoad.placeOrder` in [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) is `MailboxContext -> Caller -> Guid -> Order -> AsyncReplyChannel<Result<EventId, string>> -> unit` (private).
[ ] `Gambol.Server.CoreMailboxLoad.dispatchMailbox` in [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) matches `Mailbox.PlaceOrder (caller, orderId, _, order, reply)` and calls `placeOrder context caller orderId order reply` (private).
[ ] `Gambol.Server.CoreMailboxLoad.replyLoad` in [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) is private; it calls `CoreEventDispatch.appendLifecycle` then `reply.Reply(Ok stored.id)`.
[ ] `Gambol.Server.CoreEventDispatch.postEvent` in [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) is `Context -> Caller -> Ev -> graphOnly: bool -> Result<Ev * CoreChangesAccepted option, string>`.
[ ] `Gambol.Server.CoreEventDispatch.tryStored` in [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) is private; it finds an `Ev` in `context.eventLog` where `submissionId` matches.
[ ] `Gambol.Server.CoreEventDispatch.prepare` in [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) is private; it sets `authority` from the caller via `eventAuthority caller.authority`.
[ ] `Gambol.Server.CoreEventDispatch.persistNew` in [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) is private; it rejects when `event.id <> EventId.zero`.
[ ] `Gambol.Server.CoreMailboxEvents.eventDispatchContext` in [CoreMailboxEvents.fs](../../src/Server/Core/CoreMailboxEvents.fs) is `MailboxContext -> CoreEventDispatch.Context`.
[ ] `Gambol.Server.CoreMailboxEvents.replyStored` in [CoreMailboxEvents.fs](../../src/Server/Core/CoreMailboxEvents.fs) is `PostedReply -> Ev -> unit`; it replies `Ok stored.id`.
[ ] `Gambol.Server.CoreMailboxEvents.dispatchPostGraphOnly` in [CoreMailboxEvents.fs](../../src/Server/Core/CoreMailboxEvents.fs) is `MailboxContext -> Caller -> Ev -> AsyncReplyChannel<Result<EventId, string>> -> unit`.
[ ] `Gambol.Server.CoreMailboxActors.dispatchPostEvent` in [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) is `MailboxContext -> Caller -> Ev -> PostedReply -> unit`.
[ ] `Gambol.Server.CoreMailboxApi.postEvents` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) is `MailboxHost -> Caller -> Ev list -> Async<Result<CoreChangesAccepted, string>>`.
[ ] `Gambol.Server.CoreMailboxApi.postGraphOnly` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) is `MailboxHost -> Caller -> Ev -> Async<Result<CoreChangesAccepted, string>>`.
[ ] `Gambol.Server.CoreMailboxApi.mbLoadNode` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) is `MailboxHost -> Caller -> NodeId -> Async<Result<EventId, string>>`.
[ ] `Gambol.Server.CoreDoor.postEvents` in [CoreDoor.fs](../../src/Server/Core/CoreDoor.fs) is `CoreChangesDoor -> Ev list -> Async<Result<CoreChangesAccepted, string>>` when writable.
[ ] `Gambol.Server.Api.postEvents` in [Api.fs](../../src/Server/Api.fs) is `CoreChangesDoor -> buildEpochSec -> pageBuildEpochSec -> body: string -> Async<IResult>`.

## Match sites

### Order and MailboxLog mapping

[ ] [MailboxLog.fs](../../src/Shared/MailboxLog.fs) `orderOfBody` — `HistoryBody.Change ops` constructs `Order.Change ops`.
[ ] [MailboxLog.fs](../../src/Shared/MailboxLog.fs) `historyBody` — `Order.Change ops` maps to `HistoryBody.Change ops`.
[ ] [MailboxLog.fs](../../src/Shared/MailboxLog.fs) `ofEv` — calls `orderOfBody` on `event.body`.
[ ] [MailboxLog.fs](../../src/Shared/MailboxLog.fs) `toEv` — calls `historyBody` on `placed.order`.

### placeOrder and PlaceOrder dispatch

[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `placeOrder` — `Order.Load subject` calls `replyLoad`; `Order.Change _` calls `replyUnsupported`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `placeOrder` — `Order.Undo`, `Order.Redo`, `Order.StartActor`, `Order.StartPeerActor`, `Order.LoadSave`, `Order.Cancel` call `replyUnsupported`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `dispatchMailbox` — `Mailbox.PlaceOrder` calls `placeOrder` when authority is held.
[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `mbLoadNode` — posts `Mailbox.PlaceOrder(caller, Guid.NewGuid(), None, Order.Load subject, channel)`.
[ ] [SessionTests.fs](../../tests/Server.Tests/SessionTests.fs) ``login during a pending logout runs and a dropped generation rejects a late order`` — posts `Mailbox.PlaceOrder` with `Order.Change [ Op.NewNode ... ]`.
[ ] [SessionTests.fs](../../tests/Server.Tests/SessionTests.fs) ``admitted route order after DropAuthority is rejected`` — posts `Mailbox.PlaceOrder` with `Order.Change [ Op.NewNode ... ]`.
[ ] [MailboxExpandTests.fs](../../tests/Server.Tests/MailboxExpandTests.fs) ``Mailbox is CoreMsg, PlaceOrder, and Ask`` — constructs `Mailbox.PlaceOrder` with `Order.Load Graph.rootId`.

### CoreMsg dispatch

[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `runMsg` — `PostGraphOnly` calls `CoreMailboxEvents.dispatchPostGraphOnly`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `runMsg` — `PostEvent` calls `CoreMailboxActors.dispatchPostEvent`.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `dispatchPostEvent` — `EventBody.ActorStart` calls `postActorStart`.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `dispatchPostEvent` — `EventBody.Cancel` calls `postCancel`.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `dispatchPostEvent` — default arm calls `CoreEventDispatch.postEvent` with `graphOnly` `false` for Change, Undo, Redo, and other non-lifecycle bodies.
[ ] [CoreMailboxEvents.fs](../../src/Server/Core/CoreMailboxEvents.fs) `dispatchPostGraphOnly` — calls `CoreEventDispatch.postEvent` with `graphOnly` `true`.

### postEvents and postGraphOnly doors

[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `postEvents` — maps each `Ev` to `fun channel -> PostEvent(caller, event, channel)` and passes that list to `MailboxHost.postForReplies`.
[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `postGraphOnly` — posts `PostGraphOnly(caller, event, channel)` through `reply` / `postAndAsyncReply`.
[ ] [CoreDoor.fs](../../src/Server/Core/CoreDoor.fs) `postEvents` — delegates to `CoreMailboxApi.postEvents` when the door is writable.
[ ] [Api.fs](../../src/Server/Api.fs) `postEvents` — calls `CoreDoor.postEvents handle request.events`.
[ ] [RunAgentActor.fs](../../src/Server/RunAgentActor.fs) `postChangeOps` (private) — calls `CoreDoor.postEvents` with `EventBody.Change ops`.
[ ] [TestActor.fs](../../src/Server/TestActor.fs) `hello` — calls `CoreDoor.postEvents` with a Change `Ev`.
[ ] [TestActor.fs](../../src/Server/TestActor.fs) `postOwnedChild` — calls `CoreDoor.postEvents` with a Change `Ev`.

### Event dispatch and storage

[ ] [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) `postEvent` — `tryStored context event.submissionId` returns the existing `Ev` on resubmit.
[ ] [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) `persist` — when `graphOnly` is `false` and ops exist, calls `pushChangeSpecials` after `applyEvent`.
[ ] [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) `pushChangeSpecials` — for Change, Undo, Redo logs `Notice.NodePersisting` via `markForPersist` and calls `pushPersist` on owning specials.

## Existing tests

[ ] [MailboxLogTests.fs](../../tests/Shared.Tests/MailboxLogTests.fs) ``ofEv then toEv keeps a Change`` — `MailboxLog.ofEv` on `EventBody.Change ops` yields `Order.Change ops`; `toEv` round-trips the source `Ev`.
[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) ``a resubmitted Change replies the stored id`` — two `CoreMailboxApi.postEvents` calls with the same `postedEvent()` share `eventId` and stored events.
[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) ``An Edit pushes its owning special node`` — `CoreMailboxApi.postEvents` with `editFileText` records `pushPersist` for the file node.
[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) ``An Edit marks its owning special node at the Edit's id`` — after two edits via `postEvents`, the file node is `Unpersisted` and `changedVersion` equals the second ack `eventId`.
[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) ``A graph-only change pushes nothing`` — `CoreMailboxApi.postGraphOnly` with `editFileText` leaves `pushPersist` empty (Parse door behavior today; 102 — Place Parse changes as Order.Change moves Parse).
[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) ``postEvents returns the same CoreChangesAccepted from the event id`` — several `postEvents` scenarios rebuild acks from the log via stored submission ids.
[ ] [SessionTests.fs](../../tests/Server.Tests/SessionTests.fs) ``login during a pending logout runs and a dropped generation rejects a late order`` — late `Mailbox.PlaceOrder` with `Order.Change` returns `Error "authority dropped"`.
[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) private `postedEvent` — builds a Change `Ev` with `commandName` `"Set text"` and a fresh `submissionId`.
[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) private `editFileText` — builds a Change `Ev` with `Op.SetText` on a special file node.
[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) private `hostRecordingPush` — test host with `pushPersist` hooked to a `ResizeArray`.

## Copy-from

[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `mbLoadNode` — posts `Mailbox.PlaceOrder` through `MailboxHost.postMailboxAndAsyncReply`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `replyLoad` — PlaceOrder handler that appends a lifecycle `Ev` and replies `Ok stored.id`.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `dispatchPostEvent` default arm — runs `CoreEventDispatch.postEvent` with `graphOnly` `false` through `eventDispatchContext`.
[ ] [CoreMailboxEvents.fs](../../src/Server/Core/CoreMailboxEvents.fs) `dispatchPostGraphOnly` — same `postEvent` path with `graphOnly` `true` (reference for origin `Parse` in 102 — Place Parse changes as Order.Change, not this ticket).
[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `postEvents` — batch `postForReplies` then `mergeIdReplies` / `acceptedFromIds` pattern to rebuild `CoreChangesAccepted`.
[ ] [MailboxHost.fs](../../src/Server/Core/MailboxHost.fs) `postMailboxAndAsyncReply` — wraps `CoreQueue.Public (build channel)` for a `Mailbox` item (target shape for `postForReplies` after the ticket change).

## implement

### Ticket claims that differ from the code

[ ] Type `ChangeOrigin` with cases `Edit` and `Parse` beside `Order` is not in [MailboxLog.fs](../../src/Shared/MailboxLog.fs); [Message shape](../cqrs-messages.md) decision 18 **ChangeOrigin** documents it only in plan docs.
[ ] `Order.Change` is still `ops: Op list` only; the ticket requires `ops: Op list * origin: ChangeOrigin`.
[ ] `MailboxHost.postForReplies` still takes `(AsyncReplyChannel<'Reply> -> CoreMsg) list`, not `(AsyncReplyChannel<'Reply> -> Mailbox) list`.
[ ] `CoreMailboxApi.postEvents` still posts `CoreMsg.PostEvent` for every `Ev`, including `EventBody.Change`.
[ ] `CoreMailboxLoad.dispatchMailbox` ignores `commandName` on `PlaceOrder` (`_` in the match); the ticket requires passing `commandName` into `placeOrder`.
[ ] `CoreMailboxLoad.placeOrder` replies `Error "unsupported"` for `Order.Change`; the ticket requires an `ChangeOrigin.Edit` arm that builds an `Ev` and calls `CoreEventDispatch.postEvent` with `graphOnly` `false`.
[ ] `orderOfBody` and `ofEv` do not set origin `Edit` on mapped Change orders.
[ ] Ticket section 3 names [SessionTests.fs](../../tests/Server.Tests/SessionTests.fs) ``logout returns while the reconciliation route is open`` as an `Order.Change` site; that test posts via `CoreMailboxApi.postEvent` and `orderEvent "reconcile"`, not `Order.Change`.
[ ] Ticket section 6 claims ``logout returns while the reconciliation route is open`` gets `Order.Change(ops, ChangeOrigin.Edit)`; that test has no `Order.Change` constructor today.
[ ] Ticket section 6 claims the two SessionTests constructors and ``ofEv then toEv keeps a Change`` are every `Order.Change` site; [SessionTests.fs](../../tests/Server.Tests/SessionTests.fs) ``admitted route order after DropAuthority is rejected`` also constructs `Order.Change` and is omitted from section 6.
[ ] Test ``postEvents places Order.Change with origin Edit`` from ticket section 6 is not present in [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs).
[ ] Test ``Undo, Redo, ActorStart, and Cancel still post as PostEvent`` from ticket section 6 is not present in [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs).
[ ] [MailboxLogTests.fs](../../tests/Shared.Tests/MailboxLogTests.fs) ``ofEv then toEv keeps a Change`` still asserts `Order.Change ops` without an origin field.

### How each named new test observes the change at a public seam

[ ] ``postEvents places Order.Change with origin Edit`` — call `CoreMailboxApi.postEvents` with `postedEvent()`; read stored history via `CoreMailboxApi.getEventsSince` or `eventHistory`; assert `MailboxLog.ofEv stored` yields `Order.Change(ops, ChangeOrigin.Edit)` and the async result is `Ok` of a new `EventId`.
[ ] ``Undo, Redo, ActorStart, and Cancel still post as PostEvent`` — for each body, call `CoreMailboxApi.postEvents` with one `Ev`; assert `Ok` reply and `getEventsSince` shows `EventBody.Undo`, `EventBody.Redo`, `EventBody.ActorStart`, or `EventBody.Cancel` respectively (not a placed Change order in the mailbox log for those bodies).
