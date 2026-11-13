# 16 — Rename the client start request to ClientRequestActor
**Status**: coded
One job: The client sends `EventBody.ActorStart` of `Order.ClientRequestActor`, and `postPlacedOrder` refuses that order so it never enters the mailbox log. The server converts the request into `Order.StartActor` of `ActorOrder`, and the client never builds an `ActorOrder`. Place actor order in [Arch](../../arch.md) is this order.

Type:

[x] coding

Blockers:

[ ] none

## Inputs

### Working

[x] `Order` in `src/Shared/History.fs` is `Change`, `Undo`, `Redo`, `ClientRequestActor` of `ActorStart`, `StartActor` of `ActorOrder`, `Load`, and `CancelActor`.
[x] `EventBody.ActorStart` and `(|ActorStart|_|)` in `src/Shared/History.fs` build and match `EventBody.Order` of `Order.ClientRequestActor`.
[x] `postPlacedOrder` in `src/Server/Core/MailboxHost.fs` returns `Error "ClientRequestActor is retired"` for `Order.ClientRequestActor` and posts nothing.
[x] `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` uses that same refusal for `Order.ClientRequestActor`. `replyPlaced` appends only after `MailboxLog.eventFromPlacedOrder` returns `Ok`.
[x] `encodeOrder` and `decodeOrder` in `src/Shared/EventJson.fs` use wire kind `startActor` for `Order.ClientRequestActor` and wire kind `actorOrder` for `Order.StartActor`.
[x] `historyBody` in `src/Shared/MailboxLog.fs` returns `Ok (EventBody.Order order)` for `Order.ClientRequestActor`, in the same arm group as `Order.StartActor`. `orderOfBody` reads any `EventBody.Order`.
[x] ``Ev JSON round-trips each EventBody.Order case`` in `tests/Shared.Tests/EventJsonTests.fs` builds `Order.ClientRequestActor`.
[x] ``?test Run admits the caller and queues ActorOrder`` in `tests/Server.Tests/TestActorCommandErrorTests.fs` expects a stored `Order.StartActor` and expects no stored `EventBody.ActorStart`.
[x] `withHost` and `eventPast` in `tests/Server.Tests/TestActorCommandErrorTests.fs` open a mailbox and read its log.

### Reference

[x] Place actor order in [Arch](../../arch.md).
[x] [Size limit](../../../context/size-limit.md).

## Do not load

[x] `EventBody.ActorStart` and `(|ActorStart|_|)` keep those names
[x] `queueStart`, `finishReadyStart`, `dispatchPostEvent`, `applyEvent`, `resultOf`, `completeOther`, `expandStored`, and `affirmedStart`
[x] `Order.StartActor` of `ActorOrder`
[x] `postActorStart`, `queueAdmittedTest`, `queueAdmittedAi`, and `queueAdmittedQuery`
[x] The `ActorStart` record
[x] Wire kind `startActor`
[x] A new `Order` case
[x] A new ticket

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change: rename `Order.StartActorDeprecated` to `Order.ClientRequestActor` on `Order`. `EventBody.ActorStart` and `(|ActorStart|_|)` keep their names and build and match `Order.ClientRequestActor`.
3. [x] Change: `encodeOrder` and `decodeOrder` keep wire kind `startActor` for `Order.ClientRequestActor`. Client and server share this codec, so the kind stays `startActor`.
4. [x] Change: `historyBody` keeps the `Order.ClientRequestActor` arm and still returns `Ok (EventBody.Order order)`. A stored log can hold that order, and `orderOfBody` reads it back.
5. [x] Change: `postPlacedOrder` returns `Error "ClientRequestActor is retired"` for `Order.ClientRequestActor` and does not post that order.
6. [x] Change: `placeOrder` uses that same refusal, so a `PlaceOrder` of `Order.ClientRequestActor` appends nothing.
7. [x] Add ``postPlacedOrder refuses ClientRequestActor and appends nothing`` in `tests/Server.Tests/TestActorCommandErrorTests.fs`. The test calls `MailboxHost.postPlacedOrder` with `Order.ClientRequestActor`. The reply is `Error "ClientRequestActor is retired"`. `eventPast` is the same log as before the call.
8. [x] Change: ``Ev JSON round-trips each EventBody.Order case`` builds `Order.ClientRequestActor` and still round-trips wire kind `startActor`.

## Outputs

[x] `src/Shared/History.fs`
[x] `src/Shared/EventJson.fs`
[x] `src/Shared/MailboxLog.fs`
[x] `src/Server/Core/MailboxHost.fs`
[x] `src/Server/Core/CoreMailboxLoop.fs`
[x] `tests/Shared.Tests/EventJsonTests.fs` `--filter` ``Ev JSON round-trips each EventBody.Order case``
[x] `tests/Server.Tests/TestActorCommandErrorTests.fs` `--filter` ``postPlacedOrder refuses ClientRequestActor and appends nothing``

## Stop when

[x] Rewrite `16-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the change passes 8 source files or 4 test files. [Size limit](../../../context/size-limit.md).
[ ] Stop when the edit deletes `Order.StartActor` or `Order.CancelActor`.
[ ] Stop when the edit renames `EventBody.ActorStart` or `(|ActorStart|_|)`.
[ ] Stop when the edit drops the `historyBody` arm for this order.
[ ] Stop when the edit changes wire kind `startActor`.
[ ] Stop when `postPlacedOrder` posts `Order.ClientRequestActor`.
[ ] Stop when a search of `src/` and `tests/` still finds `StartActorDeprecated`.

## Check

[x] Run `scripts/agent-test.sh --full`.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``Ev JSON round-trips each EventBody.Order case``
  - `tests/Server.Tests`: ``postPlacedOrder refuses ClientRequestActor and appends nothing``
[x] All executed tests pass.
[x] Rewrite `16-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
