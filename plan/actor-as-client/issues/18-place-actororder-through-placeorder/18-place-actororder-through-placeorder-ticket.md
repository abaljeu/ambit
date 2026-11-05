# 18 — Place ActorOrder through PlaceOrder
**Status**: defined
One job: The client and the server place `Order.ActorOrder` through `Mailbox.PlaceOrder`. The pull runs that order's `ActorFunction` on the .NET thread pool. `startFunction` mints the credential and the session id.

[ ] This ticket replaces [80 — Place Actor starts through PlaceOrder](../../../core-refinement/issues/80-place-actor-orders-through-placeorder.md).
[ ] The client's `Caller` is the `Caller` on that `Mailbox.PlaceOrder`.
[ ] A Desk Load or Save runs no actor.
[ ] A wire-read `ActorOrder` stays a log record.

Type:

[ ] coding

Blockers:

[x] [06 — Log ActorOrder](../06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md)

## Inputs

### Working

[ ] `postActorOrder` on `MailboxHost` puts one `ActorOrder` on the mailbox queue. It posts `Authority "Actor"` and `Credential ""`.
[ ] `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` replies unsupported for `Order.StartActor`, `Order.StartPeerActor`, `Order.LoadSave`, and `Order.ActorOrder`. `Order.CancelActor` is already accepted.
[ ] `startFunction` in `src/Server/Core/CoreActorPool.fs` mints the Actor credential and the Actor session id.
[ ] The Shared actor type and the pool actor type are both named `FunctionActor`.
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs`, `tests/Server.Tests/LoadSaveCommandTests.fs`, and `tests/Server.Tests/CoreMailboxDoorTests.fs`.

### Reference

[ ] [Arch](../../arch.md). Types name `ActorFunction` and Curried actor. Place actor order is the pull.

## Do not load

[ ] `ActorOrder` gains no `actorNodeId` field. Curried actor applies that id before the post.
[ ] `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave` stay unsupported. 15 — Delete the old Actor starts and 16 — Delete the old Order cases delete the old cases.
[ ] `src/Server/ActorDispatch.fs`. This ticket does not add it.
[ ] A new `CoreMsg` case.
[ ] A lookup of the actor from `actorName`.
[ ] `Authority "Actor"` and `Credential ""` on this path.
[ ] Remote starts.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Types stay on Arch. Rename the Shared actor type and the pool actor type to `ActorFunction`.
3. [ ] Change: `postActorOrder` takes the client's `Caller` and posts `Mailbox.PlaceOrder` with that `Caller`.
4. [ ] Change: `placeOrder` accepts `Order.ActorOrder`. The pull calls `startFunction` and runs `order.actor` on the .NET thread pool through Shared changes. `FunctionStart.actorNodeId` comes from the closure on that `ActorFunction`.
5. [ ] A Desk Load or Save places `Order.ActorOrder` and runs no actor.
6. [ ] Leave `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave` unsupported.
7. [ ] A wire-read order stays a log record. Leave `stubActor` unrun.

## Outputs

[ ] `src/Server/Core/MailboxHost.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreDoor.fs`
[ ] `src/Shared/History.fs`
[ ] `src/Server/Core/CoreActorPool.fs`
[ ] `src/Shared/EventJson.fs`
[ ] `src/Server/TestActor.fs`
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs` — ``ActorOrder places the actor on the thread pool``
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs` — ``Desk load-save places its order and runs no Actor``
[ ] `tests/Shared.Tests/EventJsonTests.fs` — ``ActorOrder round-trips on the wire with stubActor``

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/18-place-actororder-through-placeorder-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs and Outputs.
[ ] Stop when the pull cannot both call `startFunction` and run `order.actor`.
[ ] Stop when `ActorOrder` carries `actorNodeId`, or the pull reads that id from `actorName`.
[ ] Stop when the edit adds `ActorDispatch.fs` or a `CoreMsg` case.
[ ] Stop when the edit runs `stubActor`.
[ ] Stop when this path posts `Authority "Actor"` and `Credential ""`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``ActorOrder places the actor on the thread pool``
  - `tests/Server.Tests`: ``Desk load-save places its order and runs no Actor``
  - `tests/Shared.Tests`: ``ActorOrder round-trips on the wire with stubActor``
  - `tests/Server.Tests`: ``CoreMailboxApi.startActor appends ActorStart to lifecycle events``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/18-place-actororder-through-placeorder-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
