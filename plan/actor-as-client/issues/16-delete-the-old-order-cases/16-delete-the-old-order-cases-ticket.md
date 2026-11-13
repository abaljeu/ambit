# 16 — Delete the old Order cases
**Status**: defined
One job: `Order.StartActorDeprecated` is gone. `Order.StartActor` of `ActorOrder` stays, and `EventBody.ActorStart` stays as the client request the server converts into `Order.StartActor`. Place actor order in [Arch](../../arch.md) is this order.

[ ] `EventBody.ActorStart` still builds `Order.StartActorDeprecated`. Process step 2 is the present stop.

Type:

[ ] coding

Blockers:

[ ] none

## Inputs

### Working

[ ] `Order` in `src/Shared/History.fs` is `Change`, `Undo`, `Redo`, `StartActorDeprecated` of `ActorStart`, `StartActor` of `ActorOrder`, `Load`, and `CancelActor`.
[ ] `EventBody.ActorStart` and `(|ActorStart|_|)` in `src/Shared/History.fs` build and match `EventBody.Order` of `Order.StartActorDeprecated`.
[ ] `queueStart` in `src/Shared/RunLaunch.fs` queues `posted "Exec" (EventBody.ActorStart request)`.
[ ] `finishReadyStart` in `src/Server/Core/CoreMailboxActors.fs` appends `EventBody.ActorStart`. `dispatchStartLoadSaveCommand` in `src/Server/Core/CoreMailboxLoad.fs` calls `dispatchActorStartResult`, and that call reaches `finishReadyStart`.
[ ] `dispatchPostEvent` in `src/Server/Core/CoreMailboxActors.fs` matches `EventBody.ActorStart` and calls `postActorStart`. `postActorStart` places `Order.StartActor` for `test`, for `ai`, and for a query. Every other name replies `Error "unknown actor"`.
[ ] `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` runs `replyActorOrder` for `Order.StartActor`. The `Order.StartActorDeprecated` arm replies `Error "StartActorDeprecated is retired"`.
[ ] `encodeOrder` and `decodeOrder` in `src/Shared/EventJson.fs` use wire kind `startActor` for `Order.StartActorDeprecated` and wire kind `actorOrder` for `Order.StartActor`.
[ ] `historyBody` in `src/Shared/MailboxLog.fs` matches `Order.StartActorDeprecated`.
[ ] ``Ev JSON round-trips each EventBody.Order case`` in `tests/Shared.Tests/EventJsonTests.fs` builds `Order.StartActorDeprecated`.
[ ] ``?test Run admits the caller and queues ActorOrder`` in `tests/Server.Tests/TestActorCommandErrorTests.fs` expects a stored `Order.StartActor` and expects no stored `EventBody.ActorStart`.
[ ] `PeerActorName` is in `src/Shared/History.fs`. No `Order` case carries it. `CoreMsg.StartLoadSaveCommand`, `CoreActorPool`, and desk Load and Save carry it.

### Reference

[ ] Place actor order in Arch.

## Do not load

[ ] `EventBody.ActorStart` and `(|ActorStart|_|)`
[ ] `queueStart` and `finishReadyStart`
[ ] `dispatchPostEvent` and `postActorStart`
[ ] `Order.StartActor` of `ActorOrder`
[ ] `Order.CancelActor`
[ ] `PeerActorName`
[ ] `startFunction` and `pool.startActor`
[ ] A reader that accepts an old wire kind `startActor`
[ ] A new `Order` case
[ ] A new `CoreMsg` case

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] STOP: `EventBody.ActorStart` builds `Order.StartActorDeprecated`. `queueStart` queues that request. `finishReadyStart` stores that request for desk Load and Save. Those uses remain. A change that keeps `EventBody.ActorStart` and `(|ActorStart|_|)` and stops them building this order comes first. The later steps are the retirement, and they stay at 4 source files and 1 test file.
3. [ ] Contract: Delete `Order.StartActorDeprecated`. Leave `Order.StartActor` of `ActorOrder` and `Order.CancelActor`.
4. [ ] Contract: Delete the `placeOrder` arm that replies `Error "StartActorDeprecated is retired"`.
5. [ ] Contract: Delete the `startActor` arms in `encodeOrder` and `decodeOrder`. `decodeOrder` keeps `actorOrder`.
6. [ ] Contract: Delete the `Order.StartActorDeprecated` arm in `historyBody`.
7. [ ] Contract: Remove `Order.StartActorDeprecated` from ``Ev JSON round-trips each EventBody.Order case``.
8. [ ] Search `src/` and `tests/` for `Order.StartActorDeprecated`. The search is empty. `Order.StartActor` and `EventBody.ActorStart` remain.

## Outputs

[ ] `src/Shared/History.fs`
[ ] `src/Shared/EventJson.fs`
[ ] `src/Shared/MailboxLog.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `tests/Shared.Tests/EventJsonTests.fs` `--filter` ``Ev JSON round-trips each EventBody.Order case``

## Stop when

[ ] Rewrite `16-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when `EventBody.ActorStart` still builds `Order.StartActorDeprecated`.
[ ] Stop when the edit deletes `EventBody.ActorStart` or `(|ActorStart|_|)`
[ ] Stop when the edit deletes `Order.StartActor` or `Order.CancelActor`.
[ ] Stop when the edit deletes `PeerActorName`.
[ ] Stop when the edit keeps a `startActor` reader.
[ ] Stop when a search hit for `Order.StartActorDeprecated` remains outside Outputs.
[ ] Stop when the change passes 8 source files or 4 test files. [Size limit](../../../context/size-limit.md).

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``Ev JSON round-trips each EventBody.Order case``
[ ] All executed tests pass.
[ ] Rewrite `16-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
