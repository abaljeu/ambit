# 68 — Remove CoreMsg.PostEvent scope

Scope for [68 — Remove CoreMsg.PostEvent](../issues/68-remove-coremsg-postevent.md). Mode: implement. The tree is unchanged in this pass.

## Signatures

### CoreMsg and PostEvent

[ ] **Match** — `CoreMsg` in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) is `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[ ] **Match** — `PostEvent` is `caller: Caller * event: Ev * AsyncReplyChannel<Result<EventId, string>>`.
[ ] **Match** — `Mailbox` in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) is `CoreMsg of CoreMsg`, `PlaceOrder of placed: PlacedOrder * caller: Caller * reply: AsyncReplyChannel<Result<EventId, string>>`, and `Ask of Query`.
[ ] **Match** — `Mailbox.placeForCaller` in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) is `PlacedOrder -> Caller -> AsyncReplyChannel<Result<EventId, string>> -> Mailbox`.

### History EventBody builders

[ ] **Match** — `EventBody.ActorStart` in [History.fs](../../src/Shared/History.fs) builds `EventBody.Order` of `Order.ClientRequestActor`.
[ ] **Match** — `EventBody.Cancel` in [History.fs](../../src/Shared/History.fs) builds `EventBody.Order` of `Order.CancelActor`.

### CoreMailboxApi post routing

[ ] **Match** — `sendsAsOrder` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) is private; it is `true` for `EventBody.Undo`, `EventBody.Redo`, and `EventBody.Change` when `event.id` is `EventId.zero`.
[ ] **Match** — `messageForPosted` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) is private; it places through `Mailbox.placeForCaller` when `sendsAsOrder` is `true` and `MailboxLog.placedOrderFromEvent` returns `Ok`; every other event is `Mailbox.CoreMsg` of `PostEvent`.
[ ] **Match** — `acceptOne` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) is private; it places an Undo or a Redo through `MailboxHost.postPlacedOrder` when `placedOrderFromEvent` returns `Ok`; every other event calls `postEventAccepted`.
[ ] **Match** — `postEventAccepted` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) is private; it builds `PostEvent` through `reply`, which calls `MailboxHost.postAndAsyncReply`.
[ ] **Match** — `postEvent` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) calls `acceptOne`, then rebuilds the stored `Ev` from the log.
[ ] **Match** — `postEvents` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) reaches `messageForPosted` through `editBuild`, then `MailboxHost.postForReplies`.
[ ] **Match** — `cancelByFocus` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) is `MailboxHost -> Caller -> NodeId -> Async<Result<unit, string>>`; it posts `Order.CancelActor` through `postPlacedOrder` with `orderId = OrderId.New()` and `commandName = None`, and drops the `EventId` with `Result.map (fun _ -> ())`.

### MailboxHost

[ ] **Match** — `postForReplies` in [MailboxHost.fs](../../src/Server/Core/MailboxHost.fs) posts each builder as `CoreQueue.Public` of `build channel`.
[ ] **Match** — `postAndAsyncReply` in [MailboxHost.fs](../../src/Server/Core/MailboxHost.fs) wraps `CoreMsg` in `coreMsgItem` and posts `CoreQueue.Public`.
[ ] **Match** — `postPlacedOrder` in [MailboxHost.fs](../../src/Server/Core/MailboxHost.fs) returns `Error clientRequestRetired` for `Order.ClientRequestActor` and does not queue the order; `clientRequestRetired` is the literal `"ClientRequestActor is retired"`.
[ ] **Match** — `addCoreMsg` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) has no caller in `src/` besides its definition.

### CoreEventDispatch

[ ] **Match** — `persistNew` in [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) is private; it replies `Error "posted EventId must be zero"` when `event.id` is not `EventId.zero`.
[ ] **Match** — `postEvent` in [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) calls `persistNew` on a new submission.
[ ] **Match** — `parseBuild` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) replies `Error "posted EventId must be zero"` when the Parse post's `event.id` is not zero.

### MailboxLog

[ ] **Match** — `placedOrderFromEvent` in [MailboxLog.fs](../../src/Shared/MailboxLog.fs) sets `orderId` to `OrderId event.submissionId`.
[ ] **Match** — `eventFromPlacedOrder` in [MailboxLog.fs](../../src/Shared/MailboxLog.fs) builds the logged `Ev` at the supplied `id`.
[ ] **Match** — `replyEdit` and `replyPlaced` in [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) call `CoreEventDispatch.postEvent` with an `Ev` from `eventFromPlacedOrder` at `EventId.zero`.

### CoreMailboxLoop

[ ] **Match** — `runMsg` and `messageCaller` in [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) match `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[ ] **Match** — `runMsg` sends `PostEvent` to `CoreMailboxActors.dispatchPostEvent`.
[ ] **Match** — `placeOrder` in [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) runs `replyCancel` for `Order.CancelActor`, `replyActorOrder` for `Order.StartActor`, and `Error MailboxHost.clientRequestRetired` for `Order.ClientRequestActor`.
[ ] **Match** — `dispatchMailbox` in [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) matches `Mailbox.CoreMsg` and `Mailbox.PlaceOrder`.
[ ] **Match** — `isStartupRead` in [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) matches `Mailbox.CoreMsg _` and returns `false`.

### CoreMailboxEvents

[ ] **Match** — `operationContext` and `replyFailure` in [CoreMailboxEvents.fs](../../src/Server/Core/CoreMailboxEvents.fs) match `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[ ] **Match** — `storedBySubmission` finds an `Ev` in `context.eventLog` by `submissionId`.
[ ] **Match** — `replyStored` replies `Ok stored.id`.

### CoreMailboxActors

[ ] **Match** — `dispatchPostEvent` in [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) replies `Error "ActorStop is not a client event type"` for `EventBody.ActorStop`.
[ ] **Match** — `dispatchPostEvent` calls `postActorStart` for `EventBody.ActorStart` and `postCancel` for `EventBody.Cancel`; the default arm calls `CoreEventDispatch.postEvent`.
[ ] **Match** — `postActorStart` returns a stored event when `storedBySubmission` finds the submission; it queues `?test`, `?ai`, and a query as `Order.StartActor` through `postBuiltOrder`; any other name replies `Error "unknown actor"`.
[ ] **Match** — `postCancel` returns a stored event when `storedBySubmission` finds the submission; on `Ok (Some stored)` it replies `stored.id`; on `Ok None` it replies `event.id` through `replyStored`.
[ ] **Match** — `replyCancel` calls `runCancel` with `placed.orderId.Value` and replies `Ok EventId.zero`; it has no `storedBySubmission` check.
[ ] **Match** — `runCancel` stores `EventBody.ActorStop` of `ActorCancelled` when the pool has a receipt for the focus.
[ ] **Match** — `postBuiltOrder`, `queueAdmittedFocus`, `queueAdmittedTest`, `queueAdmittedAi`, `queueAdmittedQuery`, `whenAdmitted`, and `admittedQueue` have no caller in `src/` besides `postActorStart`.

### RunLaunch

[ ] **Match** — `RunLaunch.queueCancel` in [RunLaunch.fs](../../src/Shared/RunLaunch.fs) posts `commandName` `"Cancel"` and `id` `EventId.zero` through `posted`.

### CoreMsg helpers still on reply

[ ] **Match** — `recordSearchStop` and `startLoadSaveCommand` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) call `reply`, which uses `MailboxHost.postAndAsyncReply`.

## Match sites

### PostEvent constructor and CoreMsg routing

[ ] [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) `CoreMsg` union — defines case `PostEvent`.
[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `postEventAccepted` — constructs `PostEvent(caller, event, channel)` inside `reply`.
[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `messageForPosted` — constructs `Mailbox.CoreMsg(PostEvent(caller, event, channel))` on the non-order path.
[ ] [MailboxExpandTests.fs](../../tests/Server.Tests/MailboxExpandTests.fs) ``Mailbox is CoreMsg, PlaceOrder, and Ask`` — constructs `PostEvent` of `EventBody.Cancel` as a sample `Mailbox.CoreMsg`.

### CoreMsg dispatch in the loop

[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `runMsg` — `PostEvent (caller, event, reply)` calls `CoreMailboxActors.dispatchPostEvent context caller event reply`.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `messageCaller` — `PostEvent (caller, _, _)` returns `caller`.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `dispatch` — calls `runMsg` when authority is held.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `dispatchMailbox` — `Mailbox.CoreMsg msg` calls `dispatch context msg`.
[ ] [CoreMailboxEvents.fs](../../src/Server/Core/CoreMailboxEvents.fs) `operationContext` — `PostEvent _` returns `("PostEvent", "")`.
[ ] [CoreMailboxEvents.fs](../../src/Server/Core/CoreMailboxEvents.fs) `replyFailure` — `PostEvent (_, _, reply)` calls `reply.Reply(Error error)`.

### dispatchPostEvent and lifecycle posts

[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `dispatchPostEvent` — `EventBody.ActorStop _` replies the client-stop error.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `dispatchPostEvent` — `EventBody.ActorStart request` calls `postActorStart`.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `dispatchPostEvent` — `EventBody.Cancel focusId` calls `postCancel`.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `dispatchPostEvent` — default arm calls `CoreEventDispatch.postEvent` then `replyStored`.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `postActorStart` — calls `storedBySubmission`, then `admittedQueue` and `whenAdmitted`, or replies `unknown actor`.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `postCancel` — calls `storedBySubmission`, then `runCancel`, then `replyStored`.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `postBuiltOrder` — calls `MailboxHost.postPlacedOrder` with `Order.StartActor`.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `queueAdmittedTest`, `queueAdmittedAi`, `queueAdmittedQuery` — call `postBuiltOrder` or `queueAdmittedFocus`.

### placeOrder and cancel paths

[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `placeOrder` — `Order.CancelActor focusId` calls `CoreMailboxActors.replyCancel`.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `placeOrder` — `Order.ClientRequestActor _` replies `Error MailboxHost.clientRequestRetired`.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `placeOrder` — `Order.StartActor order` calls `replyActorOrder`.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `replyCancel` — calls `runCancel` and replies `Ok EventId.zero`.

### cancelByFocus and test harness

[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `cancelByFocus` — calls `MailboxHost.postPlacedOrder` with `Order.CancelActor focusId`.
[ ] [AskCancelHarness.fs](../../tests/Server.Tests/AskCancelHarness.fs) `cancelFocus` — calls `CoreMailboxApi.cancelByFocus`.
[ ] [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) ``cancelByFocus places Order.CancelActor`` — calls `CoreMailboxApi.cancelByFocus`.
[ ] [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) ``second Cancel for the same run logs no stop`` — second call is `CoreMailboxApi.cancelByFocus`.

### Client post doors (public seams)

[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `postEvents` — uses `postMailboxChange` and `editBuild` / `messageForPosted`.
[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `postEvent` — calls `acceptOne`.
[ ] [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) ``posted Cancel cancels a hanging Focus by NodeId`` — calls `CoreMailboxApi.postEvents` with `EventBody.Cancel`.
[ ] [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) ``second Cancel on the same focus replies the same`` — calls `MailboxHost.postPlacedOrder` with `Order.CancelActor`.

### Search and desk CoreMsg cases that stay

[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `recordSearchStop` — constructs `RecordSearchStop(caller, rootId, channel)`.
[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `startLoadSaveCommand` — constructs `StartLoadSaveCommand(...)`.
[ ] [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `runMsg` — `RecordSearchStop` calls `CoreEventDispatch.actorStop`; `StartLoadSaveCommand` calls `CoreMailboxLoad.dispatchStartLoadSaveCommand`.

### Browser cancel op (not a server caller)

[ ] [UpdateActorLive.fs](../../src/Client/UpdateActorLive.fs) `cancelFocusOp` — calls `RunLaunch.queueCancel`; it does not call `CoreMailboxApi.cancelByFocus`.

## Existing tests

### MailboxExpandTests

[ ] [MailboxExpandTests.fs](../../tests/Server.Tests/MailboxExpandTests.fs) ``Mailbox is CoreMsg, PlaceOrder, and Ask`` — builds `PostEvent` of `EventBody.Cancel` as its `CoreMsg` sample; asserts the three `Mailbox` kinds and that `CoreQueue.Public` carries `Mailbox.CoreMsg`.
[ ] [MailboxExpandTests.fs](../../tests/Server.Tests/MailboxExpandTests.fs) ``postEvent runs through Mailbox CoreMsg`` — calls `CoreMailboxApi.postEvent` with a zero-id `EventBody.Change`; asserts the stored `Ev` appears in `eventsSince`. Today `acceptOne` sends that Change to `postEventAccepted` and the `PostEvent` path.

### CoreMailboxDoorTests

[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) ``CoreMsg is not a public type`` — asserts the server assembly exports no type named `CoreMsg`.
[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) ``Undo and Redo post as orders; Cancel posts as PostEvent`` — posts Undo, Redo, and an idle `EventBody.Cancel` through `CoreMailboxApi.postEvents`; the Cancel ack is `EventId.zero`, `events` is empty, and the log has no row for that submission.
[ ] [CoreMailboxDoorTests.fs](../../tests/Server.Tests/CoreMailboxDoorTests.fs) ``postEvent returns the stored start when that start stops at once`` — posts `EventBody.ActorStart` for `?test` through `CoreMailboxApi.postEvent`; the stored body is `Order.StartActor`; the second post of that submission returns the stored event.

### Issue41CoreMailboxTests

[ ] [Issue41CoreMailboxTests.fs](../../tests/Server.Tests/Issue41CoreMailboxTests.fs) ``postEvent rejects a non-zero EventId on a new client Event`` — expects `Error "posted EventId must be zero"` from `CoreMailboxApi.postEvent` on a non-zero Change.

### QueryActorTests

[ ] [QueryActorTests.fs](../../tests/Server.Tests/QueryActorTests.fs) ``posted gate start replies unknown actor`` — posts a list through `CoreMailboxApi.postEvents` ending in an ActorStart; expects `Error "unknown actor"`.

### OrderedEventStreamTests

[ ] [OrderedEventStreamTests.fs](../../tests/Server.Tests/OrderedEventStreamTests.fs) ``edit then unknown ActorStart replies unknown actor`` — posts through the HTTP events door; expects HTTP 400 and the text `unknown actor`.

### TestActorCommandErrorTests

[ ] [TestActorCommandErrorTests.fs](../../tests/Server.Tests/TestActorCommandErrorTests.fs) ``postPlacedOrder refuses ClientRequestActor and appends nothing`` — expects `Error "ClientRequestActor is retired"` and an unchanged log; the request node is absent from the graph.
[ ] [TestActorCommandErrorTests.fs](../../tests/Server.Tests/TestActorCommandErrorTests.fs) ``production events door replies unknown actor`` — expects HTTP 400 and `unknown actor` for `?unknown` and `?nope`.

### CancelByFocusTests

[ ] [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) ``cancel by Focus accepts Cancelled and drops the live row`` — calls `cancelFocus`; checks live row drop, owned texts, and fake cancel observation.
[ ] [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) ``Cancel before Change rejects later Agent replace`` — calls `cancelFocus`; checks cancelled focus and change count.
[ ] [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) ``Change before Cancel keeps the accepted children`` — calls `cancelFocus`; checks owned child text remains.
[ ] [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) ``late duplicate completion after cancel is ignored`` — calls `cancelFocus` twice; expects one stop.
[ ] [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) ``posted Cancel cancels a hanging Focus by NodeId`` — posts `EventBody.Cancel` through `CoreMailboxApi.postEvents` for a live focus; checks `ActorStop` of `ActorCancelled` in the accepted tail; does not check the ack id.
[ ] [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) ``actorStop after cancel does not emit a root Focus ActorStop`` — calls `cancelFocus`, then posts `Notice.ActorStopped` of `ActorSucceeded`; expects no root stop.
[ ] [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) ``cancelByFocus places Order.CancelActor`` — calls `CoreMailboxApi.cancelByFocus` and expects `Ok ()`.
[ ] [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) ``second Cancel for the same run logs no stop`` — first call is `cancelFocus`; second is `cancelByFocus` with `Ok ()`; cancelled-stop list unchanged.
[ ] [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) ``second Cancel on the same focus replies the same`` — posts `Order.CancelActor` through `MailboxHost.postPlacedOrder` with `OrderId.New()`; expects equal replies because `replyCancel` returns `Ok EventId.zero`.

### AgentActorStreamTests (cancelFocus only)

[ ] [AgentActorStreamTests.fs](../../tests/Server.Tests/AgentActorStreamTests.fs) ``next open angle commits Focus child before next tag closes`` — calls `cancelFocus` after a partial stream; checks owned text and no SetText ops.
[ ] [AgentActorStreamTests.fs](../../tests/Server.Tests/AgentActorStreamTests.fs) ``cancel mid-stream drops live and keeps streamed children`` — calls `cancelFocus`; checks live drop, owned text, no SetText ops, and fake cancel.

### AgentGrokBotStreamTests (cancelFocus only)

[ ] [AgentGrokBotStreamTests.fs](../../tests/Server.Tests/AgentGrokBotStreamTests.fs) ``inbound deliver stays live for a second chunk`` — calls `cancelFocus` during a two-chunk Grok stream.
[ ] [AgentGrokBotStreamTests.fs](../../tests/Server.Tests/AgentGrokBotStreamTests.fs) ``cancel mid Grok stream drops live and keeps children`` — calls `cancelFocus`; checks live drop and owned children.

### Absent name the ticket checks

[ ] The test name ``ActorStart and Cancel still post as PostEvent`` is absent from `tests/Server.Tests`.

## Copy-from

[ ] [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) `Mailbox.placeForCaller` — sets `placed.authority` from the caller and builds `Mailbox.PlaceOrder`; extend `messageForPosted` and `acceptOne` to use this path for client `ActorStart` and `Cancel`.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `postActorStart` — holds the `ClientRequestActor` reply logic to move into `placeOrder`'s `Order.ClientRequestActor` arm: `storedBySubmission`, `admittedQueue`, `whenAdmitted`, and `unknown actor`.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `postCancel` — holds the `Cancel` reply logic to move into `placeOrder`'s `Order.CancelActor` arm: `storedBySubmission`, then `runCancel`, then stored id or draft id.
[ ] [CoreMailboxActors.fs](../../src/Server/Core/CoreMailboxActors.fs) `runCancel` — already serves `postCancel` and `replyCancel`; the `CancelActor` arm keeps this call.
[ ] [CoreMailboxEvents.fs](../../src/Server/Core/CoreMailboxEvents.fs) `storedBySubmission` and `replyStored` — idempotent reply helpers for stored and draft ids at the `placeOrder` arms.
[ ] [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `messageForPosted` — already routes Undo, Redo, and zero-id Change to `PlaceOrder`; copy this shape for `EventBody.ActorStart` and `EventBody.Cancel` when `placedOrderFromEvent` returns `Ok`.
[ ] [RunLaunch.fs](../../src/Shared/RunLaunch.fs) `queueCancel` — builds the client `Ev` shape that `cancelFocus` in [AskCancelHarness.fs](../../tests/Server.Tests/AskCancelHarness.fs) should post through `CoreMailboxApi.postEvents` after `cancelByFocus` leaves.

## implement

### Ticket claims that differ from the tree

[ ] [68 — Remove CoreMsg.PostEvent](../issues/68-remove-coremsg-postevent.md) claims `messageForPosted` places client `ActorStart` and client `Cancel` as `Mailbox.PlaceOrder`; the tree still sends them as `Mailbox.CoreMsg` of `PostEvent`.
[ ] The ticket claims `acceptOne` places those same events through `MailboxHost.postPlacedOrder`; the tree calls `postEventAccepted` for `ActorStart`, `Cancel`, and singular `postEvent` Change.
[ ] The ticket claims `MailboxHost.postPlacedOrder` queues `Order.ClientRequestActor` and drops `clientRequestRetired`; the tree returns that error and never reaches `placeOrder`.
[ ] The ticket claims `placeOrder`'s `ClientRequestActor` arm does the work of `postActorStart`; the tree replies `Error MailboxHost.clientRequestRetired`.
[ ] The ticket claims `placeOrder`'s `CancelActor` arm gives `postCancel`'s reply for every `Order.CancelActor`; the tree calls `replyCancel`, which always replies `Ok EventId.zero`.
[ ] The ticket claims `replyCancel` leaves; the tree still defines and calls it from `placeOrder`.
[ ] The ticket claims `CoreMailboxApi.cancelByFocus` leaves; the tree still defines it and tests call it.
[ ] The ticket claims `cancelFocus` in [AskCancelHarness.fs](../../tests/Server.Tests/AskCancelHarness.fs) posts `EventBody.Cancel` through `CoreMailboxApi.postEvents` with the `RunLaunch.queueCancel` shape; the tree calls `cancelByFocus`.
[ ] The ticket claims `CoreMsg` keeps only `StartLoadSaveCommand` and `RecordSearchStop`; the tree still has `PostEvent`.
[ ] The ticket claims `dispatchPostEvent`, `postActorStart`, and `postCancel` leave when nothing else calls them; the tree still routes client lifecycle events through them.
[ ] The ticket claims `postPlacedOrder` allows `ClientRequestActor` to reach `placeOrder`; the tree blocks that order at the host gate.

### Renamed tests — observation at public seams

[ ] ``Undo, Redo, and Cancel are placed as orders`` (rename of ``Undo and Redo post as orders; Cancel posts as PostEvent``) — observes through `CoreMailboxApi.postEvents` on a live-focus cancel: idle Cancel still acks `EventId.zero` with an empty `events` list and no log row, via the same door as today.
[ ] ``postEvent runs through Mailbox PlaceOrder`` (rename of ``postEvent runs through Mailbox CoreMsg``) — observes through `CoreMailboxApi.postEvent` on a zero-id `EventBody.Change`: after the edit, that Change queues as `Mailbox.PlaceOrder` instead of `PostEvent`, and the stored `Ev` still appears in `eventsSince`.
[ ] ``Mailbox is CoreMsg, PlaceOrder, and Ask`` — keeps its name; the `CoreMsg` sample becomes `StartLoadSaveCommand` or `RecordSearchStop` instead of `PostEvent`, still asserting the three mailbox kinds.
[ ] ``postPlacedOrder of an absent ClientRequestActor replies unknown actor`` (rename of ``postPlacedOrder refuses ClientRequestActor and appends nothing``) — observes through `MailboxHost.postPlacedOrder` with `Order.ClientRequestActor`: reply becomes `Error "unknown actor"` and the log stays unchanged when `placeOrder` owns the arm.

### New test — observation at public seam

[ ] ``posted Cancel replies the stored stop id`` in [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) — observes through `CoreMailboxApi.postEvents` on a live `EventBody.Cancel`: the first reply is the stored `ActorStop` id; a second post of the same submission replies that same id.

### Kept and migrated tests — observation at public seams

[ ] ``postEvent rejects a non-zero EventId on a new client Event`` — keeps observing `CoreMailboxApi.postEvent` for `Error "posted EventId must be zero"` on a non-zero Change through the `PostEvent` arm until Change placement removes that path for new client Change; non-zero Change on the remaining path still hits `persistNew`.
[ ] ``postEvent returns the stored start when that start stops at once`` — keeps observing `CoreMailboxApi.postEvent` for stored `Order.StartActor` and idempotent second post after routing moves to `PlaceOrder`.
[ ] ``second Cancel on the same focus replies the same`` — changes observation from equal `Ok EventId.zero` replies to equal stored stop `EventId` values on `MailboxHost.postPlacedOrder` of `Order.CancelActor`.
[ ] Six [CancelByFocusTests.fs](../../tests/Server.Tests/CancelByFocusTests.fs) members that call `cancelFocus` migrate to the client Cancel post; behavior at `CoreMailboxApi.postEvents` and the log stays the same.
[ ] ``cancelByFocus places Order.CancelActor`` deletes; live-row and stop behavior stays pinned by ``cancel by Focus accepts Cancelled and drops the live row`` and ``posted Cancel cancels a hanging Focus by NodeId``.
[ ] Four stream tests in [AgentActorStreamTests.fs](../../tests/Server.Tests/AgentActorStreamTests.fs) and [AgentGrokBotStreamTests.fs](../../tests/Server.Tests/AgentGrokBotStreamTests.fs) migrate `cancelFocus` only; file names stay.

## Summary

Every checked signature claim in [68 — Remove CoreMsg.PostEvent](../issues/68-remove-coremsg-postevent.md) matches the tree. The implement section lists the behavioral claims that still differ from the tree and how the named new and renamed tests observe the change at `CoreMailboxApi.postEvents`, `CoreMailboxApi.postEvent`, and `MailboxHost.postPlacedOrder`.