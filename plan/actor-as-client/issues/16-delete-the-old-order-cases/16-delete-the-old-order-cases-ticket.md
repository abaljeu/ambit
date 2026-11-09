# 16 — Delete the old Order cases
**Status**: defined
One job: `Order.StartActorDeprecated` is gone. `Order.StartActor` of `ActorOrder` stays. Nothing writes `Order.StartActorDeprecated` before this ticket.

[ ] This ticket is the Order half of 15 — Delete the old Actor starts. That ticket was over the size limit.
[ ] The source files are 4. The test files are 2. The size limit is 8 source files and 4 test files.
[ ] This ticket starts after 112 — Place Cancel through PlaceOrder has landed. The cancel case is `Order.CancelActor`. The wire kind stays `cancel`.

Type:

[ ] coding

Blockers:

[ ] [15 — Delete the old Actor starts](../15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md)
[ ] [112 — Place Cancel through PlaceOrder](../../../core-refinement/issues/112-place-cancel-through-placeorder.md)

## Inputs

### Working

[ ] `Order.StartActorDeprecated` of `ActorStart` and `Order.StartActor` of `ActorOrder` are in `src/Shared/History.fs`.
[ ] `encodeOrder` in `src/Shared/EventJson.fs` writes wire kind `startActor` for `Order.StartActorDeprecated` and wire kind `actorOrder` for `Order.StartActor`. `historyBody` in `src/Shared/MailboxLog.fs` matches both cases.
[ ] `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` runs `replyActorOrder` for `Order.StartActor` and `dispatchStartActor` for `Order.StartActorDeprecated`.
[ ] `Order.StartActor` of `ActorOrder` is the case 06 — Log ActorOrder adds.
[ ] `tests/Shared.Tests/MailboxLogTests.fs` round-trips the orders that stay.

### Reference

[ ] [Arch](../../arch.md). `Order` in `src/Shared/History.fs` is `Change`, `Undo`, `Redo`, `StartActorDeprecated`, `StartActor`, `Load`, and `CancelActor`.

## Do not load

[ ] `startFunction` and the pool.
[ ] `Order.StartActor` of `ActorOrder`. Leave it.
[ ] A reader for an old `Order.StartActorDeprecated` row.
[ ] A new `CoreMsg` case.
[ ] `MailboxHost.fs`. 112 leaves it untouched, and this ticket does not add a queue function.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Contract: Delete `Order.StartActorDeprecated` and its codec and log arms. Leave `Order.StartActor` of `ActorOrder`.
3. [ ] The cancel match uses `Order.CancelActor`.
4. [ ] Delete `PeerActorName` when no case carries it.
5. [ ] The decoder has no arm for an old `Order.StartActorDeprecated` row. Wire kind `startActor` leaves. Wire kind `actorOrder` stays.
6. [ ] Remove the `placeOrder` arm that calls `dispatchStartActor` for `Order.StartActorDeprecated`.
7. [ ] Search `src/` and `tests/` for `Order.StartActorDeprecated`. The search is empty. `Order.StartActor` remains.

## Outputs

[ ] `src/Shared/History.fs`
[ ] `src/Shared/EventJson.fs`
[ ] `src/Shared/MailboxLog.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `tests/Shared.Tests/EventJsonTests.fs`
[ ] `tests/Shared.Tests/MailboxLogTests.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/16-delete-the-old-order-cases-stop.md`, then ask.
[ ] Stop when a search hit remains outside Outputs.
[ ] Stop when the edit deletes `Order.StartActor` or `Order.CancelActor`.
[ ] Stop when the edit keeps a reader for an old `Order.StartActorDeprecated` row.
[ ] Stop when the edit adds an `Order` case.
[ ] Stop when 112 has not landed.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: `EventJsonTests`
  - `tests/Shared.Tests`: `MailboxLogTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/16-delete-the-old-order-cases-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
