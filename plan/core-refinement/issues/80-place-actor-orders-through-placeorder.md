# 80 — Place Actor starts through PlaceOrder
**Status**: cancelled
One job: The Server places `Order.ActorOrder` through `Mailbox.PlaceOrder`. The mailbox pull runs that order's actor on the .NET thread pool.

Replaced by [18 — Place ActorOrder through PlaceOrder](../../actor-as-client/issues/18-place-actororder-through-placeorder/18-place-actororder-through-placeorder-ticket.md).

[ ] The actor function is already on the order. This ticket does not look up a command name.
[o] `CoreMsg` is the start path for this order.
[o] This ticket writes `Order.StartActor`.
[ ] A Desk Load or Save runs no Actor.

Type:

[ ] coding

Blockers:

[ ] [06 — Log ActorOrder](../../actor-as-client/issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md)

## Inputs

### Working

[ ] `postActorOrder` on `MailboxHost` puts one `ActorOrder` on the mailbox queue. 06 — Log ActorOrder adds it.
[ ] `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` replies unsupported for `Order.StartActor` and `Order.CancelActor`. On this branch the cancel case is still `Order.Cancel` until 112 lands.
[ ] `startFunction` in `src/Server/Core/CoreActorPool.fs` mints the Actor credential and the Actor session id.
[ ] A Desk Load or Save runs no actor function. `startLoadSaveCommand` returns `Ok None` for `LoadSavePath.Desk`.
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs`, `tests/Server.Tests/LoadSaveCommandTests.fs`, and `tests/Server.Tests/CoreMailboxDoorTests.fs`.

### Reference

[ ] [Arch](../../actor-as-client/arch.md). A wire-read `ActorOrder` is a log record. Nothing re-runs `stubActor`.
[ ] [Message shape](../cqrs-messages.md) decision 19 **One case each**.

## Do not load

[ ] Do not delete `CoreMsg.StartActor` or `CoreMsg.StartPeerActor` here. That delete is 15 — Delete the old Actor starts and 16 — Delete the old Order cases.
[ ] Do not add `ActorDispatch.fs`, `actorForCommand`, or `Order.RunActor`.
[ ] Do not add a `CoreMsg` case.
[ ] The cancel arm. 112 — Place Cancel through PlaceOrder renames it to `Order.CancelActor`.
[ ] Remote starts.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: `placeOrder` accepts `Order.ActorOrder`. The pull runs that order's actor on the .NET thread pool. The pool mints the session id.
3. [ ] A Desk Load or Save logs `Order.ActorOrder` and runs no actor. It does not call `startPeerActor`.
4. [ ] Leave `Order.StartActor` unsupported. This ticket does not write it.
5. [ ] A wire-read order stays a log record. Do not call `stubActor`.

## Outputs

[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs` — `ActorOrder places the actor on the thread pool`
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs` — `Desk load-save places its order and runs no Actor`
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` — keep `CoreMailboxApi.startActor appends ActorStart to lifecycle events`

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/80-place-actor-orders-through-placeorder-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the pull cannot both call `startFunction` and run `order.actor`. `startFunction`'s actor node id comes from the Run path closure.
[ ] Stop when the edit adds a `CoreMsg` case or looks up the function from `actorName`.
[ ] Stop when the edit runs `stubActor`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``ActorOrder places the actor on the thread pool``
  - `tests/Server.Tests`: ``Desk load-save places its order and runs no Actor``
  - `tests/Server.Tests`: ``CoreMailboxApi.startActor appends ActorStart to lifecycle events``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/80-place-actor-orders-through-placeorder-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
