# 16 — Rename the client start request to ClientRequestActor
**Status**: defined
One job: The client sends `EventBody.RequestActor` of `Order.ClientRequestActor`, and `postPlacedOrder` refuses that order so it never enters the mailbox log. The server converts the request into `Order.StartActor` of `ActorOrder`, and the client never builds an `ActorOrder`. Place actor order in [Arch](../../arch.md) is this order.

Type:

[ ] coding

Blockers:

[ ] none

## Inputs

### Working

[x] `Order` in `src/Shared/History.fs` is `Change`, `Undo`, `Redo`, `StartActorDeprecated` of `ActorStart`, `StartActor` of `ActorOrder`, `Load`, and `CancelActor`.
[x] `EventBody.ActorStart` and `(|ActorStart|_|)` in `src/Shared/History.fs` build and match `EventBody.Order` of `Order.StartActorDeprecated`.
[x] `queueStart` in `src/Shared/RunLaunch.fs` queues `posted "Exec" (EventBody.ActorStart request)`.
[x] `finishReadyStart` in `src/Server/Core/CoreMailboxActors.fs` appends `EventBody.ActorStart`.
[x] `dispatchPostEvent` in `src/Server/Core/CoreMailboxActors.fs` matches `EventBody.ActorStart` and calls `postActorStart`. `postActorStart` places `Order.StartActor` for `test`, for `ai`, and for a query.
[x] `applyEvent` and `resultOf` in `src/Shared/ActorLive.fs` match `EventBody.ActorStart`.
[x] `completeOther` in `src/Server/Core/CoreEventDispatch.fs` matches `EventBody.ActorStart`.
[x] `expandStored` and `affirmedStart` in `src/Server/Core/CoreMailboxApi.fs` match `EventBody.ActorStart`.
[x] `postPlacedOrder` in `src/Server/Core/MailboxHost.fs` posts `Mailbox.PlaceOrder` for every `PlacedOrder`.
[x] `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` replies `Error "StartActorDeprecated is retired"` for `Order.StartActorDeprecated`. `replyPlaced` appends only after `MailboxLog.eventFromPlacedOrder` returns `Ok`.
[x] `encodeOrder` and `decodeOrder` in `src/Shared/EventJson.fs` use wire kind `startActor` for `Order.StartActorDeprecated` and wire kind `actorOrder` for `Order.StartActor`.
[x] `historyBody` in `src/Shared/MailboxLog.fs` returns `Ok (EventBody.Order order)` for `Order.StartActorDeprecated`, in the same arm group as `Order.StartActor`. `orderOfBody` reads any `EventBody.Order`.
[x] ``Ev JSON round-trips each EventBody.Order case`` in `tests/Shared.Tests/EventJsonTests.fs` builds `Order.StartActorDeprecated`.
[x] ``?test Run admits the caller and queues ActorOrder`` in `tests/Server.Tests/TestActorCommandErrorTests.fs` expects a stored `Order.StartActor` and expects no stored `EventBody.ActorStart`.
[x] `withHost` and `eventPast` in `tests/Server.Tests/TestActorCommandErrorTests.fs` open a mailbox and read its log.

### Reference

[x] Place actor order in [Arch](../../arch.md).
[x] [Size limit](../../../context/size-limit.md).

## Do not load

[ ] `Order.StartActor` of `ActorOrder`
[ ] `postActorStart`, `queueAdmittedTest`, `queueAdmittedAi`, and `queueAdmittedQuery`
[ ] The `ActorStart` record
[ ] Wire kind `startActor`
[ ] A new `Order` case
[ ] A new ticket

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: rename `Order.StartActorDeprecated` to `Order.ClientRequestActor` on `Order`.
3. [ ] Change: rename `EventBody.ActorStart` to `EventBody.RequestActor` and `(|ActorStart|_|)` to `(|RequestActor|_|)`. Both build and match `Order.ClientRequestActor`.
4. [ ] Change: `encodeOrder` and `decodeOrder` keep wire kind `startActor` for `Order.ClientRequestActor`. Client and server share this codec, so the kind stays `startActor`.
5. [ ] Change: `historyBody` keeps the `Order.ClientRequestActor` arm and still returns `Ok (EventBody.Order order)`. A stored log can hold that order, and `orderOfBody` reads it back.
6. [ ] Change: `postPlacedOrder` returns `Error "ClientRequestActor is retired"` for `Order.ClientRequestActor` and does not post that order.
7. [ ] Change: `placeOrder` uses that same refusal, so a `PlaceOrder` of `Order.ClientRequestActor` appends nothing.
8. [ ] Change: each `EventBody.ActorStart` build and each `(|ActorStart|_|)` match in Inputs uses `RequestActor`.
9. [ ] Add ``postPlacedOrder refuses ClientRequestActor and appends nothing`` in `tests/Server.Tests/TestActorCommandErrorTests.fs`. The test calls `MailboxHost.postPlacedOrder` with `Order.ClientRequestActor`. The reply is `Error "ClientRequestActor is retired"`. `eventPast` is the same log as before the call.
10. [ ] Change: ``Ev JSON round-trips each EventBody.Order case`` builds `Order.ClientRequestActor` and still round-trips wire kind `startActor`.

## Outputs

[ ] `src/Shared/History.fs`
[ ] `src/Shared/EventJson.fs`
[ ] `src/Shared/MailboxLog.fs`
[ ] `src/Shared/RunLaunch.fs`
[ ] `src/Shared/ActorLive.fs`
[ ] `src/Server/Core/MailboxHost.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Server/Core/CoreEventDispatch.fs`
[ ] `tests/Shared.Tests/EventJsonTests.fs` `--filter` ``Ev JSON round-trips each EventBody.Order case``
[ ] `tests/Shared.Tests/EventTests.fs`
[ ] `tests/Shared.Tests/RunEditCommitTests.fs`
[ ] `tests/Shared.Tests/SyncLogicTests.fs`
[ ] `tests/Shared.Tests/ActorLiveTests.fs`
[ ] `tests/Server.Tests/TestActorCommandErrorTests.fs` `--filter` ``postPlacedOrder refuses ClientRequestActor and appends nothing``
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs`
[ ] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
[ ] `tests/Server.Tests/OrderedEventStreamTests.fs`
[ ] `tests/Server.Tests/QueryActorTests.fs`
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs`
[ ] `tests/Server.Tests/ActorOrderFromStartTests.fs`
[ ] `tests/Server.Tests/ApiPostCommandTests.fs`

## Stop when

[x] Rewrite `16-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[x] Stop when the change passes 8 source files or 4 test files. [Size limit](../../../context/size-limit.md).
[ ] Stop when the edit deletes `Order.StartActor` or `Order.CancelActor`.
[ ] Stop when the edit drops the `historyBody` arm for this order.
[ ] Stop when the edit changes wire kind `startActor`.
[ ] Stop when `postPlacedOrder` posts `Order.ClientRequestActor`.
[ ] Stop when a search of `src/` and `tests/` still finds `StartActorDeprecated` or `EventBody.ActorStart`.

## Check

[ ] Run `scripts/agent-test.sh --full`.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``Ev JSON round-trips each EventBody.Order case``
  - `tests/Server.Tests`: ``postPlacedOrder refuses ClientRequestActor and appends nothing``
[ ] All executed tests pass.
[x] Rewrite `16-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
