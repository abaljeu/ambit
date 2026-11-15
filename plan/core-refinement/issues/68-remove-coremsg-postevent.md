# 68 — Remove CoreMsg.PostEvent

**Status**: defined

One job: The server places a client ActorStart and a client Cancel through `Mailbox.PlaceOrder`. The log stays the same, and every client reply stays the same. The work of `CoreMailboxActors.postActorStart` moves into `placeOrder`'s `ClientRequestActor` arm. There is one cancel, by the focused node. `placeOrder`'s `CancelActor` arm always gives `postCancel`'s reply: the stored event when `storedBySubmission` finds the submission, else `runCancel`'s stored event, else the draft id. That arm replaces `replyCancel`. `cancelByFocus` returns `Result<unit, string>` and drops the `EventId`. Its callers are tests. `CoreMsg.PostEvent` then leaves. `CoreMsg` stays `StartLoadSaveCommand` and `RecordSearchStop`. The reason is Alan, 2026-11-15.

Type: coding

Blockers:

[x] [102 — Place Parse changes as Order.Change](102-place-parse-changes-as-order-change.md). Status `coded`. `CoreMsg` has no `PostGraphOnly`.
[x] [65 — Place Undo and Redo as orders](65-undo-and-redo-resolve-their-ops-from-the-target.md). Status `coded`. `sendsAsOrder` is true for `EventBody.Undo` and `EventBody.Redo`, and `messageForPosted` places those orders.
[x] [18 — Place ActorOrder through PlaceOrder](../../actor-as-client/issues/18-place-actororder-through-placeorder/18-place-actororder-through-placeorder-ticket.md). Status `coded`. `placeOrder` runs `replyActorOrder` for `Order.StartActor`.
[x] [116 — One EventBody type, and MailboxLog.Ev leaves](116-one-eventbody-type-and-mailboxlog-ev-leaves.md). Status `coded`. `MailboxLog.placedOrderFromEvent` reads a History `Ev`. `MailboxLog.Ev` is gone.
[x] [112 — Place Cancel through PlaceOrder](112-place-cancel-through-placeorder.md). Status `coded`. It landed on staging in PR 522. `cancelByFocus` places `Order.CancelActor` through `MailboxHost.postPlacedOrder`.
[x] [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md). Status `coded`. `CoreMsg` has no `ActorStop`.

## Scope

Slice: contract. `CoreMsg.PostEvent` leaves. `placeOrder`'s `ClientRequestActor` arm takes the work of `postActorStart`. `placeOrder`'s `CancelActor` arm takes `postCancel`'s reply and replaces `replyCancel`. `StartLoadSaveCommand` and `RecordSearchStop` stay. The edit adds no `Order` case and no `CoreMsg` case.

### Signatures

[x] `CoreMsg` in `src/Server/Core/CoreMsg.fs` is `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[x] `PostEvent` is `caller: Caller * event: Ev * AsyncReplyChannel<Result<EventId, string>>`.
[x] `EventBody.ActorStart` in `src/Shared/History.fs` builds `EventBody.Order` of `Order.ClientRequestActor`.
[x] `EventBody.Cancel` in `src/Shared/History.fs` builds `EventBody.Order` of `Order.CancelActor`.
[x] `sendsAsOrder` in `src/Server/Core/CoreMailboxApi.fs` is true for `EventBody.Undo`, `EventBody.Redo`, and `EventBody.Change` when `event.id` is `EventId.zero`.
[x] `messageForPosted` places through `Mailbox.placeForCaller` when `sendsAsOrder` is true and `MailboxLog.placedOrderFromEvent` returns `Ok`. Every other event is `Mailbox.CoreMsg` of `PostEvent`.
[x] `acceptOne` places an Undo or a Redo through `MailboxHost.postPlacedOrder` when `placedOrderFromEvent` returns `Ok`. Every other event calls `postEventAccepted`.
[x] `postEventAccepted` builds `PostEvent` through `reply`, which calls `MailboxHost.postAndAsyncReply`.
[x] `postEvents` reaches `messageForPosted` through `editBuild`, then `MailboxHost.postForReplies`. `postEvent` calls `acceptOne`.
[x] `postForReplies` posts each `Mailbox` builder as `CoreQueue.Public`.
[x] `CoreEventDispatch.persistNew` in `src/Server/Core/CoreEventDispatch.fs` replies `Error "posted EventId must be zero"` when `event.id` is not `EventId.zero`.
[x] `parseBuild` replies that same error for a Parse post whose id is not zero.
[x] `MailboxLog.eventFromPlacedOrder` builds the logged `Ev` at `EventId.zero`. `replyEdit` and `replyPlaced` call `CoreEventDispatch.postEvent` with that event.
[x] `runMsg` and `messageCaller` in `src/Server/Core/CoreMailboxLoop.fs` match `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[x] `runMsg` sends `PostEvent` to `CoreMailboxActors.dispatchPostEvent`.
[x] `operationContext` and `replyFailure` in `src/Server/Core/CoreMailboxEvents.fs` match those three cases.
[x] `dispatchPostEvent` replies `Error "ActorStop is not a client event type"` for `EventBody.ActorStop`.
[x] `dispatchPostEvent` calls `postActorStart` for `EventBody.ActorStart` and `postCancel` for `EventBody.Cancel`.
[x] The other `dispatchPostEvent` arm calls `CoreEventDispatch.postEvent`.
[x] `placeOrder` runs `replyCancel` for `Order.CancelActor` and `replyActorOrder` for `Order.StartActor`.
[x] `placeOrder` replies `Error "ClientRequestActor is retired"` for `Order.ClientRequestActor`. The text is `MailboxHost.clientRequestRetired`.
[x] `MailboxHost.postPlacedOrder` returns that error for `Order.ClientRequestActor` and does not queue the order.
[x] `postActorStart` returns a stored event when `storedBySubmission` finds the submission. It queues `?test`, `?ai`, and a query as `Order.StartActor` through `postBuiltOrder`. Any other name replies `Error "unknown actor"`.
[x] `postCancel` returns a stored event when `storedBySubmission` finds the submission. The reply is `stored.id`. It then calls `runCancel`. `Ok (Some stored)` replies `stored.id`. `Ok None` replies `event.id`.
[x] `dispatchPostEvent` sends `EventBody.Cancel` to `postCancel` and `EventBody.ActorStart` to `postActorStart`. `persistNew`'s zero check is on `CoreEventDispatch.postEvent`, which is the other arm.
[x] `replyCancel` calls `runCancel` with `placed.orderId.Value` and replies `Ok EventId.zero`. It has no `storedBySubmission` check.
[x] `cancelByFocus` in `src/Server/Core/CoreMailboxApi.fs` posts `Order.CancelActor` through `postPlacedOrder` with `orderId = OrderId.New()` and `commandName = None`. The return type is `Async<Result<unit, string>>`. The function drops the `EventId`.
[x] Callers of `cancelByFocus` are tests. `cancelFocus` in `tests/Server.Tests/AskCancelHarness.fs` calls it. ``cancelByFocus places Order.CancelActor`` and ``second Cancel for the same run logs no stop`` in `tests/Server.Tests/CancelByFocusTests.fs` call it. `src/` has the definition.
[x] `RunLaunch.queueCancel` in `src/Shared/RunLaunch.fs` posts `commandName` `"Cancel"` and `id` `EventId.zero`.
[x] `MailboxLog.placedOrderFromEvent` in `src/Shared/MailboxLog.fs` sets `orderId` to `OrderId event.submissionId`.
[x] `postCancel`'s `Ok None` reply is `event.id`. `PlacedOrder` is `orderId`, `authority`, `commandName`, and `order`.
[x] `postBuiltOrder`, `queueAdmittedFocus`, `queueAdmittedTest`, `queueAdmittedAi`, `queueAdmittedQuery`, `whenAdmitted`, and `admittedQueue` have no caller besides `postActorStart`.
[x] `recordSearchStop` and `startLoadSaveCommand` call `reply`. `addCoreMsg` has no caller in `src/`.
[x] `dispatchMailbox` and `isStartupRead` match `Mailbox.CoreMsg`. Those arms stay, because `StartLoadSaveCommand` and `RecordSearchStop` stay.

### Where the logic goes

[ ] `messageForPosted` places a client `ActorStart` and a client `Cancel` as `Mailbox.PlaceOrder`.
[ ] `acceptOne` places those same events through `postPlacedOrder`.
[ ] `postPlacedOrder` queues `Order.ClientRequestActor`. The retired error leaves that function, so the client post reaches `placeOrder`.
[ ] `placeOrder`'s `ClientRequestActor` arm in `src/Server/Core/CoreMailboxLoop.fs` does the work of `postActorStart`.
[ ] That arm replies the stored event when `storedBySubmission` finds `placed.orderId.Value`.
[ ] That arm places `?test`, `?ai`, and a query as `Order.StartActor` through `postBuiltOrder`. `queueAdmittedFocus`, `queueAdmittedTest`, `queueAdmittedAi`, `queueAdmittedQuery`, `whenAdmitted`, and `admittedQueue` move or stay as that arm's helpers.
[ ] That arm replies `Error "unknown actor"` for any other name.
[ ] `placeOrder`'s `CancelActor` arm gives `postCancel`'s reply for every `Order.CancelActor`.
[ ] The reply is the stored event when `storedBySubmission` finds the submission.
[ ] Else the reply is `runCancel`'s stored event.
[ ] Else the reply is the draft id.
[ ] `replyCancel` leaves. The arm replaces it.
[ ] `cancelByFocus` keeps `Async<Result<unit, string>>` and still drops the `EventId`.

### Existing tests

[x] ``Mailbox is CoreMsg, PlaceOrder, and Ask`` in `tests/Server.Tests/MailboxExpandTests.fs` builds `PostEvent` of `EventBody.Cancel` as its `CoreMsg` sample.
[x] ``postEvent runs through Mailbox CoreMsg`` in `tests/Server.Tests/MailboxExpandTests.fs` calls `CoreMailboxApi.postEvent` with a zero-id `EventBody.Change`. `acceptOne` sends that Change to `postEventAccepted`.
[x] ``CoreMsg is not a public type`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` asserts that the server assembly exports no type named `CoreMsg`.
[x] ``Undo and Redo post as orders; Cancel posts as PostEvent`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` posts Undo, Redo, and an idle `EventBody.Cancel` through `CoreMailboxApi.postEvents`. The Cancel ack is `EventId.zero`, `events` is empty, and the log has no row for that submission. That pins the idle client Cancel reply.
[x] ``postEvent returns the stored start when that start stops at once`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` posts `EventBody.ActorStart` for `?test` through `CoreMailboxApi.postEvent`. The stored body is `Order.StartActor`. The second post of that submission returns the stored event.
[x] ``postEvent rejects a non-zero EventId on a new client Event`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs` expects `Error "posted EventId must be zero"`.
[x] ``posted gate start replies unknown actor`` in `tests/Server.Tests/QueryActorTests.fs` expects `Error "unknown actor"`.
[x] ``edit then unknown ActorStart replies unknown actor`` in `tests/Server.Tests/OrderedEventStreamTests.fs` expects HTTP 400 and the text `unknown actor`.
[x] ``production events door replies unknown actor`` in `tests/Server.Tests/TestActorCommandErrorTests.fs` expects HTTP 400 and the text `unknown actor` for `?unknown` and `?nope`.
[x] ``posted Cancel cancels a hanging Focus by NodeId`` in `tests/Server.Tests/CancelByFocusTests.fs` posts `EventBody.Cancel` for a live focus. It checks that an `EventBody.ActorStop` is stored. It does not check the ack id.
[x] ``postPlacedOrder refuses ClientRequestActor and appends nothing`` in `tests/Server.Tests/TestActorCommandErrorTests.fs` expects `Error "ClientRequestActor is retired"` and an unchanged log. The request's node is absent from the graph.
[x] ``cancelByFocus places Order.CancelActor`` expects `Ok ()` from `cancelByFocus`.
[x] ``second Cancel on the same focus replies the same`` posts `Order.CancelActor` through `MailboxHost.postPlacedOrder` with `OrderId.New()`. It expects the live reply, the second reply, and an idle reply to be equal. `replyCancel` makes each reply `Ok EventId.zero`.
[x] The test name ``ActorStart and Cancel still post as PostEvent`` is absent. 26 — Retire the posted named start renamed the door test and removed the posted root start from it.
[x] The live client Cancel ack id has no assertion.

## Later

[ ] The `CoreMsg` type leaves after [24 — Remove CoreMsg.RecordSearchStop](../../online-search/issues/24-remove-coremsg-recordsearchstop.md) and [20 — Move Desk Load and Desk Save](../../actor-as-client/issues/20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md). `StartLoadSaveCommand` is the desk message. This ticket leaves that closing ticket unwritten.
[ ] That later ticket owns the renames ``CoreMsg is gone`` and ``Mailbox is PlaceOrder, Ask, and Notice``.

## Inputs

### Working

[x] The signatures and the tests in Scope.
[x] `runCancel` serves `postCancel` and `replyCancel`. The `CancelActor` arm takes that call from `postCancel`.

### Reference

[x] Alan, 2026-11-15, in the One job line.
[x] [Size limit](../../context/size-limit.md).

## Do not load

[x] `recordSearchStop` and `startLoadSaveCommand` call `reply`. `addCoreMsg` has no caller in `src/`.
[ ] The edit leaves `CoreMsg`, `Mailbox.CoreMsg`, `MailboxHost.coreMsgItem`, `MailboxHost.postAndAsyncReply`, `CoreMailboxApi.addCoreMsg`, and `CoreMailboxApi.reply`.
[ ] The `PostEvent` arm of `runMsg` leaves, and the `PostEvent` arm of `messageCaller` leaves. The other arms stay.
[ ] The edit leaves `CoreMsg.RecordSearchStop`, `CoreMsg.StartLoadSaveCommand`, and `CoreEventDispatch.actorStop`.
[ ] The edit leaves `runCancel`. `replyCancel` leaves when the `CancelActor` arm gives `postCancel`'s reply.
[ ] `cancelByFocus` keeps `Async<Result<unit, string>>`.
[ ] `placeOrder`'s `ClientRequestActor` arm is in this ticket. The edit adds no `Order` case.
[ ] The edit adds no `CoreMsg` case.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] A non-zero Change keeps `Error "posted EventId must be zero"`. `sendsAsOrder` is false for that Change, and `parseBuild` keeps its check. ActorStart and Cancel keep the replies from `postActorStart` and `postCancel`. Those two do not call `persistNew`.
3. [ ] `messageForPosted` places a client `ActorStart` and a client `Cancel` as `Mailbox.PlaceOrder`. `acceptOne` places those same events through `postPlacedOrder`.
4. [ ] `postPlacedOrder` queues `Order.ClientRequestActor`. `clientRequestRetired` leaves when nothing calls it. The client post then reaches `placeOrder` and keeps today's `postActorStart` reply.
5. [ ] Move `postActorStart`'s work into `placeOrder`'s `ClientRequestActor` arm. A repeated submission replies the stored event. `?test`, `?ai`, and a query are placed as `Order.StartActor` through `postBuiltOrder` and the `queueAdmitted` helpers. Those helpers move or stay. Any other name replies `Error "unknown actor"`.
6. [ ] Move `postCancel`'s reply into `placeOrder`'s `CancelActor` arm for every `Order.CancelActor`. A repeated submission replies the stored event. Else `runCancel`'s stored event. Else the draft id. `replyCancel` leaves. The arm uses that one reply.
7. [ ] A zero-id Change that `postEventAccepted` accepts is placed as `Mailbox.PlaceOrder`, the path `messageForPosted` already uses when `sendsAsOrder` is true.
8. [ ] Remove `PostEvent` from `CoreMsg`, and remove its arms in `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`.
9. [ ] Remove `dispatchPostEvent`, `postActorStart`, and `postCancel` when nothing else calls them. Remove `postBuiltOrder`, `queueAdmittedFocus`, `queueAdmittedTest`, `queueAdmittedAi`, `queueAdmittedQuery`, `whenAdmitted`, and `admittedQueue` when nothing else calls them.

## Tests

### Renames

[ ] Rename ``Undo and Redo post as orders; Cancel posts as PostEvent`` to ``Undo, Redo, and Cancel are placed as orders``. The idle Cancel ack stays `EventId.zero`, with an empty `events` list and no log row.
[ ] Filter: `Undo, Redo, and Cancel are placed as orders`
[ ] Rename ``postEvent runs through Mailbox CoreMsg`` to ``postEvent runs through Mailbox PlaceOrder``.
[ ] Filter: `postEvent runs through Mailbox PlaceOrder`
[ ] ``Mailbox is CoreMsg, PlaceOrder, and Ask`` keeps its name. Its `CoreMsg` sample becomes `StartLoadSaveCommand` or `RecordSearchStop`.
[ ] Rename ``postPlacedOrder refuses ClientRequestActor and appends nothing`` to ``postPlacedOrder of an absent ClientRequestActor replies unknown actor``. The absent node reaches `placeOrder`. The reply is `Error "unknown actor"`. The log is unchanged.
[ ] Filter: `postPlacedOrder of an absent ClientRequestActor replies unknown actor`

### Added

[ ] Add ``posted Cancel replies the stored stop id`` in `tests/Server.Tests/CancelByFocusTests.fs`.
[ ] The first `CoreMailboxApi.postEvents` of that live `EventBody.Cancel` replies the stored stop's id.
[ ] The same submission posted again replies that same id.
[ ] Filter: `posted Cancel replies the stored stop id`

### Kept

[ ] Keep ``postEvent rejects a non-zero EventId on a new client Event``.
[ ] Filter: `postEvent rejects a non-zero EventId on a new client Event`
[ ] Keep ``postEvent returns the stored start when that start stops at once``.
[ ] Filter: `postEvent returns the stored start when that start stops at once`
[ ] Keep ``CoreMsg is not a public type``.
[ ] Filter: `CoreMsg is not a public type`
[ ] Keep ``posted gate start replies unknown actor``.
[ ] Filter: `posted gate start replies unknown actor`
[ ] Keep ``edit then unknown ActorStart replies unknown actor``.
[ ] Filter: `edit then unknown ActorStart replies unknown actor`
[ ] Keep ``production events door replies unknown actor``.
[ ] Filter: `production events door replies unknown actor`
[ ] ``second Cancel on the same focus replies the same`` reads the `EventId` that `cancelByFocus` drops. The live place replies the stored stop id. Change the test to that reply.
[ ] Filter: `second Cancel on the same focus replies the same`
[ ] Keep the tests that call `cancelByFocus`. They read `Result<unit, string>`.
[ ] Filter: `CancelByFocusTests`

## Outputs

[ ] Source files: `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMsg.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/Core/CoreMailboxEvents.fs`, `src/Server/Core/CoreMailboxActors.fs`, and `src/Server/Core/MailboxHost.fs`. 6 source files.
[ ] Test files: `tests/Server.Tests/MailboxExpandTests.fs`, `tests/Server.Tests/CoreMailboxDoorTests.fs`, `tests/Server.Tests/TestActorCommandErrorTests.fs`, and `tests/Server.Tests/CancelByFocusTests.fs`. 4 test files.
[ ] `tests/Server.Tests/Issue41CoreMailboxTests.fs` stays as it is. The non-zero id test keeps its name and its error text.
[ ] `tests/Server.Tests/QueryActorTests.fs` and `tests/Server.Tests/OrderedEventStreamTests.fs` stay as they are. Their unknown-actor tests already pin `Error "unknown actor"`.

## Stop when

[ ] Rewrite `68-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit removes `CoreMsg`, `Mailbox.CoreMsg`, `MailboxHost.coreMsgItem`, `MailboxHost.postAndAsyncReply`, `CoreMailboxApi.addCoreMsg`, `CoreMailboxApi.reply`, or `runMsg`.
[ ] Stop when the edit removes `RecordSearchStop` or `StartLoadSaveCommand`.
[ ] Stop when the edit changes a log row.
[ ] Stop when a client `postEvent` or `postEvents` reply changes.
[ ] Stop when `cancelByFocus` changes its return type or returns the `EventId`.
[ ] Stop when the edit adds an `Order` case or a `CoreMsg` case.
[ ] Stop when the edit needs a file outside Outputs.
[ ] `postPlacedOrder`'s retired error for `Order.ClientRequestActor` is the change that lets the client post reach `placeOrder`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``Undo, Redo, and Cancel are placed as orders``
  - `tests/Server.Tests`: ``postEvent runs through Mailbox PlaceOrder``
  - `tests/Server.Tests`: ``Mailbox is CoreMsg, PlaceOrder, and Ask``
  - `tests/Server.Tests`: ``postPlacedOrder of an absent ClientRequestActor replies unknown actor``
  - `tests/Server.Tests`: ``posted Cancel replies the stored stop id``
  - `tests/Server.Tests`: ``postEvent rejects a non-zero EventId on a new client Event``
  - `tests/Server.Tests`: ``postEvent returns the stored start when that start stops at once``
  - `tests/Server.Tests`: ``CoreMsg is not a public type``
  - `tests/Server.Tests`: ``posted gate start replies unknown actor``
  - `tests/Server.Tests`: ``edit then unknown ActorStart replies unknown actor``
  - `tests/Server.Tests`: ``production events door replies unknown actor``
  - `tests/Server.Tests`: `CancelByFocusTests`
[ ] All executed tests pass.
[ ] Rewrite `68-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
