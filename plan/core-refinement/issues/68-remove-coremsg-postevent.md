# 68 — Remove CoreMsg.PostEvent

**Status**: defined

One job: `messageForPosted` and `postEventAccepted` place a client `ActorStart` and a client `Cancel` through `Mailbox.PlaceOrder`. A non-zero id still replies `Error "posted EventId must be zero"` before a place. Then `CoreMsg.PostEvent` leaves, with the `PostEvent` arms of `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`. `dispatchPostEvent`, `postActorStart`, and `postCancel` leave when nothing else calls them. This is a strict refactor. The log and the replies stay the same. `CoreMsg` stays `StartLoadSaveCommand` and `RecordSearchStop`. The reason is Alan, 2026-11-15: 68 removes only `CoreMsg.PostEvent`, the same way 114 — Remove CoreMsg.ActorStop removed `CoreMsg.ActorStop`.

Type: coding

Blockers:

[x] [102 — Place Parse changes as Order.Change](102-place-parse-changes-as-order-change.md). Status `coded`. `CoreMsg` has no `PostGraphOnly`.
[x] [65 — Place Undo and Redo as orders](65-undo-and-redo-resolve-their-ops-from-the-target.md). Status `coded`. `sendsAsOrder` is true for `EventBody.Undo` and `EventBody.Redo`, and `messageForPosted` places those orders.
[x] [18 — Place ActorOrder through PlaceOrder](../../actor-as-client/issues/18-place-actororder-through-placeorder/18-place-actororder-through-placeorder-ticket.md). Status `coded`. `placeOrder` runs `replyActorOrder` for `Order.StartActor`.
[x] [116 — One EventBody type, and MailboxLog.Ev leaves](116-one-eventbody-type-and-mailboxlog-ev-leaves.md). Status `coded`. `MailboxLog.placedOrderFromEvent` reads a History `Ev`. `MailboxLog.Ev` is gone.
[x] [112 — Place Cancel through PlaceOrder](112-place-cancel-through-placeorder.md). Status `coded`. It landed on staging in PR 522. `cancelByFocus` places `Order.CancelActor` through `MailboxHost.postPlacedOrder`.
[x] [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md). Status `coded`. `CoreMsg` has no `ActorStop`.

## Scope

Slice: contract. `CoreMsg.PostEvent` leaves. `StartLoadSaveCommand` and `RecordSearchStop` stay.

[x] The build keeps the log and the replies the same.

### Signatures

[x] `CoreMsg` in `src/Server/Core/CoreMsg.fs` is `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[x] `PostEvent` is `caller: Caller * event: Ev * AsyncReplyChannel<Result<EventId, string>>`.
[x] `EventBody.ActorStart` in `src/Shared/History.fs` builds `EventBody.Order` of `Order.ClientRequestActor`.
[x] `EventBody.Cancel` in `src/Shared/History.fs` builds `EventBody.Order` of `Order.CancelActor`.
[x] `sendsAsOrder` in `src/Server/Core/CoreMailboxApi.fs` is true for `EventBody.Undo`, `EventBody.Redo`, and `EventBody.Change` when `event.id` is `EventId.zero`.
[x] `messageForPosted` places through `Mailbox.placeForCaller` when `sendsAsOrder` is true and `MailboxLog.placedOrderFromEvent` returns `Ok`. Every other event is `Mailbox.CoreMsg` of `PostEvent`.
[x] `acceptOne` places an Undo or a Redo through `MailboxHost.postPlacedOrder` when `placedOrderFromEvent` returns `Ok`. Every other event calls `postEventAccepted`.
[x] `postEventAccepted` builds `PostEvent` through `reply`, which calls `MailboxHost.postAndAsyncReply`.
[x] `postEvents` reaches `messageForPosted` through `editBuild`. `postEvent` calls `acceptOne`.
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
[x] `placeOrder` replies `Error "ClientRequestActor is retired"` for `Order.ClientRequestActor`. `MailboxHost.postPlacedOrder` returns that error and does not queue the order.
[x] `postActorStart` returns a stored event when `storedBySubmission` finds the submission. It queues `?test`, `?ai`, and a query as `Order.StartActor` through `postBuiltOrder`. Any other name replies `Error "unknown actor"`.
[x] `postCancel` returns a stored event when `storedBySubmission` finds the submission. It then calls `runCancel`. `Ok (Some stored)` replies `stored.id`. `Ok None` replies the draft event id.
[x] `replyCancel` calls `runCancel` and replies `Ok EventId.zero`. It has no `storedBySubmission` check.
[x] `postBuiltOrder`, `queueAdmittedFocus`, `queueAdmittedTest`, `queueAdmittedAi`, `queueAdmittedQuery`, `whenAdmitted`, and `admittedQueue` have no caller besides `postActorStart`.
[x] `recordSearchStop` and `startLoadSaveCommand` call `reply`. `addCoreMsg` has no caller in `src/`.
[x] `dispatchMailbox` and `isStartupRead` match `Mailbox.CoreMsg`. Those arms stay, because `StartLoadSaveCommand` and `RecordSearchStop` stay.

### Existing tests

[x] ``Mailbox is CoreMsg, PlaceOrder, and Ask`` in `tests/Server.Tests/MailboxExpandTests.fs` builds `PostEvent` of `EventBody.Cancel` as its `CoreMsg` sample.
[x] ``postEvent runs through Mailbox CoreMsg`` in `tests/Server.Tests/MailboxExpandTests.fs` calls `CoreMailboxApi.postEvent` with a zero-id `EventBody.Change`. `acceptOne` sends that Change to `postEventAccepted`.
[x] ``CoreMsg is not a public type`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` asserts that the server assembly exports no type named `CoreMsg`.
[x] ``Undo and Redo post as orders; Cancel posts as PostEvent`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` posts Undo, Redo, and `EventBody.Cancel` through `CoreMailboxApi.postEvents`. The Cancel ack is `EventId.zero`, `events` is empty, and the log has no row for that submission.
[x] ``postEvent returns the stored start when that start stops at once`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` posts `EventBody.ActorStart` for `?test` through `CoreMailboxApi.postEvent`. The stored body is `Order.StartActor`. The second post of that submission returns the stored event.
[x] ``postEvent rejects a non-zero EventId on a new client Event`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs` expects `Error "posted EventId must be zero"`.
[x] The test name ``ActorStart and Cancel still post as PostEvent`` is absent. 26 — Retire the posted named start renamed the door test and removed the posted root start from it.

## Later

[ ] The `CoreMsg` type leaves after [24 — Remove CoreMsg.RecordSearchStop](../../online-search/issues/24-remove-coremsg-recordsearchstop.md) and [20 — Move Desk Load and Desk Save](../../actor-as-client/issues/20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md). `StartLoadSaveCommand` is the desk message. This ticket leaves that closing ticket unwritten.
[ ] That later ticket owns the renames ``CoreMsg is gone`` and ``Mailbox is PlaceOrder, Ask, and Notice``.

## Inputs

### Working

[x] The signatures and the tests in Scope.
[x] `runCancel` serves both `postCancel` and `replyCancel`.

### Reference

[x] Alan, 2026-11-15, in the One job line.
[x] [Size limit](../../context/size-limit.md).

## Do not load

[x] `CoreMsg`, `Mailbox.CoreMsg`, `MailboxHost.coreMsgItem`, `MailboxHost.postAndAsyncReply`, `CoreMailboxApi.addCoreMsg`, and `CoreMailboxApi.reply`. `recordSearchStop` and `startLoadSaveCommand` call `reply`.
[x] `runMsg` and `messageCaller`. The `PostEvent` arm of each leaves. The other arms stay.
[x] `CoreMsg.RecordSearchStop`, `CoreMsg.StartLoadSaveCommand`, and `CoreEventDispatch.actorStop`.
[x] `runCancel` and `replyCancel`. `cancelByFocus` stays on `replyCancel`.
[x] A new `Order` case.
[x] A new `CoreMsg` case.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] A non-zero id replies `Error "posted EventId must be zero"` before a place. `postEvent` and `postEvents` both keep that reply.
3. [ ] `messageForPosted` and `postEventAccepted` place a client `ActorStart` and a client `Cancel` as `Mailbox.PlaceOrder`.
4. [ ] A posted `?test`, `?ai`, or query still becomes `Order.StartActor` and keeps today's log. Any other posted name still replies `Error "unknown actor"`. A repeated submission still returns the stored event.
5. [ ] A posted Cancel keeps `postCancel`'s reply. An idle Cancel still acks `EventId.zero` with an empty `events` list and no log row. `replyCancel` stays the `cancelByFocus` reply.
6. [ ] A zero-id Change that `postEventAccepted` accepts is placed as `Mailbox.PlaceOrder`, the path `messageForPosted` already uses when `sendsAsOrder` is true.
7. [ ] Remove `PostEvent` from `CoreMsg`, and remove its arms in `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`.
8. [ ] Remove `dispatchPostEvent`, `postActorStart`, and `postCancel` when nothing else calls them. Remove `postBuiltOrder`, `queueAdmittedFocus`, `queueAdmittedTest`, `queueAdmittedAi`, `queueAdmittedQuery`, `whenAdmitted`, and `admittedQueue` when nothing else calls them.

## Tests

### Renames

[ ] Rename ``Undo and Redo post as orders; Cancel posts as PostEvent`` to ``Undo, Redo, and Cancel are placed as orders``.
[ ] Filter: `Undo, Redo, and Cancel are placed as orders`
[ ] Rename ``postEvent runs through Mailbox CoreMsg`` to ``postEvent runs through Mailbox PlaceOrder``.
[ ] Filter: `postEvent runs through Mailbox PlaceOrder`
[ ] ``Mailbox is CoreMsg, PlaceOrder, and Ask`` keeps its name. Its `CoreMsg` sample becomes `StartLoadSaveCommand` or `RecordSearchStop`.

### Kept

[ ] Keep ``postEvent rejects a non-zero EventId on a new client Event``.
[ ] Filter: `postEvent rejects a non-zero EventId on a new client Event`
[ ] Keep ``postEvent returns the stored start when that start stops at once``.
[ ] Filter: `postEvent returns the stored start when that start stops at once`
[ ] Keep ``CoreMsg is not a public type``.
[ ] Filter: `CoreMsg is not a public type`
[ ] Keep every test in `tests/Server.Tests/CancelByFocusTests.fs`.
[ ] Filter: `CancelByFocusTests`

## Outputs

[ ] Source files: `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMsg.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/Core/CoreMailboxEvents.fs`, and `src/Server/Core/CoreMailboxActors.fs`. 5 source files.
[ ] Test files: `tests/Server.Tests/MailboxExpandTests.fs` and `tests/Server.Tests/CoreMailboxDoorTests.fs`. 2 test files.
[ ] `tests/Server.Tests/Issue41CoreMailboxTests.fs` stays as it is. The non-zero id test keeps its name and its error text.

## Stop when

[ ] Rewrite `68-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit removes `CoreMsg`, `Mailbox.CoreMsg`, `MailboxHost.coreMsgItem`, `MailboxHost.postAndAsyncReply`, `CoreMailboxApi.addCoreMsg`, `CoreMailboxApi.reply`, or `runMsg`.
[ ] Stop when the edit removes `RecordSearchStop` or `StartLoadSaveCommand`.
[ ] Stop when the edit changes a log row or a reply.
[ ] Stop when the edit adds an `Order` case or a `CoreMsg` case.
[ ] Stop when the edit needs a file outside Outputs.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``Undo, Redo, and Cancel are placed as orders``
  - `tests/Server.Tests`: ``postEvent runs through Mailbox PlaceOrder``
  - `tests/Server.Tests`: ``Mailbox is CoreMsg, PlaceOrder, and Ask``
  - `tests/Server.Tests`: ``postEvent rejects a non-zero EventId on a new client Event``
  - `tests/Server.Tests`: ``postEvent returns the stored start when that start stops at once``
  - `tests/Server.Tests`: ``CoreMsg is not a public type``
  - `tests/Server.Tests`: `CancelByFocusTests`
[ ] All executed tests pass.
[ ] Rewrite `68-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
