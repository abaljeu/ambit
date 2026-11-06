# 18 — Place ActorOrder through PlaceOrder
**Status**: coded
One job: The client and the server place `Order.ActorOrder` through `Mailbox.PlaceOrder`. The pull runs that order's `ActorFunction` on the .NET thread pool. `startFunction` mints the credential and the session id.

[x] This ticket replaces [80 — Place Actor starts through PlaceOrder](../../../core-refinement/issues/80-place-actor-orders-through-placeorder.md).
[x] The client's `Caller` is the `Caller` on that `Mailbox.PlaceOrder`.
[x] A Desk Load or Save runs no actor.
[x] A wire-read `ActorOrder` stays a log record.

Type:

[x] coding

Blockers:

[x] [06 — Log ActorOrder](../06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md)

## Inputs

### Working

[x] `postActorOrder` on `MailboxHost` puts one `ActorOrder` on the mailbox queue. It posts `Authority "Actor"` and `Credential ""`.
[x] `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` replies unsupported for `Order.StartActor` and `Order.ActorOrder`. `Order.CancelActor` is already accepted.
[x] `startFunction` in `src/Server/Core/CoreActorPool.fs` mints the Actor credential and the Actor session id.
[x] The Shared actor type and the pool actor type are both named `FunctionActor`.
[x] `tests/Server.Tests/CoreMsgActorCasesTests.fs`, `tests/Server.Tests/LoadSaveCommandTests.fs`, and `tests/Server.Tests/CoreMailboxDoorTests.fs`.

### Reference

[x] [Arch](../../arch.md). Types name `ActorFunction` and Curried actor. Place actor order is the pull.

## Do not load

[x] `ActorOrder` gains no `actorNodeId` field. Curried actor applies that id before the post.
[x] `Order.StartActor` stays unsupported. 15 — Delete the old Actor starts and 16 — Delete the old Order cases delete the old case.
[x] `src/Server/ActorDispatch.fs`. This ticket does not add it.
[x] A new `CoreMsg` case.
[x] A lookup of the actor from `actorName`.
[x] `Authority "Actor"` and `Credential ""` on this path.
[x] Remote starts.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Types stay on Arch. Rename the Shared actor type and the pool actor type to `ActorFunction`.
3. [x] Change: `postActorOrder` takes the client's `Caller` and posts `Mailbox.PlaceOrder` with that `Caller`.
4. [x] Change: `placeOrder` accepts `Order.ActorOrder`. The pull calls `startFunction` and runs `order.actor` on the .NET thread pool through Shared changes. The mailbox supplies `getGraph`. `ActorOrder` carries `focusId` and `ActorFunction`.
5. [x] A Desk Load or Save places `Order.ActorOrder` and runs no actor.
6. [x] Leave `Order.StartActor` unsupported.
7. [x] A wire-read order stays a log record. Leave `stubActor` unrun.

## Outputs

[x] `src/Server/Core/MailboxHost.fs`
[x] `src/Server/Core/CoreMailboxLoop.fs`
[x] `src/Server/Core/CoreDoor.fs`
[x] `src/Shared/History.fs`
[x] `src/Server/Core/CoreActorPool.fs`
[x] `src/Shared/EventJson.fs`
[x] `src/Server/TestActor.fs`
[x] `tests/Server.Tests/CoreMsgActorCasesTests.fs` — ``ActorOrder places the actor on the thread pool``
[x] `tests/Server.Tests/LoadSaveCommandTests.fs` — ``Desk load-save places its order and runs no Actor``
[x] `tests/Shared.Tests/EventJsonTests.fs` — ``ActorOrder round-trips on the wire with stubActor``

## Stop when

[x] Record the stop at `plan/actor-as-client/reports/18-place-actororder-through-placeorder-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs and Outputs.
[ ] Stop when the pull cannot both call `startFunction` and run `order.actor`.
[ ] Stop when `ActorOrder` carries `actorNodeId`, or the pull reads that id from `actorName`.
[ ] Stop when the edit adds `ActorDispatch.fs` or a `CoreMsg` case.
[ ] Stop when the edit runs `stubActor`.
[ ] Stop when this path posts `Authority "Actor"` and `Credential ""`.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``ActorOrder places the actor on the thread pool``
  - `tests/Server.Tests`: ``Desk load-save places its order and runs no Actor``
  - `tests/Shared.Tests`: ``ActorOrder round-trips on the wire with stubActor``
  - `tests/Server.Tests`: ``CoreMailboxApi.startActor appends ActorStart to lifecycle events``
[x] All executed tests pass.
[x] Write the verdict to `plan/actor-as-client/reports/18-place-actororder-through-placeorder-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
