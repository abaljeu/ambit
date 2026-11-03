# 80 — Place Actor starts through PlaceOrder
**Status**: defined
One job: The Server places `Order.RunActor` through `Mailbox.PlaceOrder`. The handler calls `startFunction`. `Order.StartPeerActor` is not that path.

[ ] `Order.RunActor` stores the command name and the FocusId. It does not store an actor function.
[o] `CoreMsg` is the start path for this order.
[o] This ticket writes `Order.StartActor` or `Order.LoadSave`.

Type:

[ ] coding

Blockers:

[ ] [06 — Log Order.RunActor](../../actor-as-client/issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md)
[ ] 07 — Dispatch the test command. The handler asks that dispatcher.

## Inputs

### Working

[ ] `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, and `CoreMsg.StartLoadSaveCommand` are built in `CoreMailboxApi` (`startActor`, `startPeerActor`, `startLoadSaveCommand`).
[ ] `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` replies unsupported for `Order.StartActor`, `Order.StartPeerActor`, `Order.LoadSave`, and `Order.Cancel`.
[ ] 06 — Log Order.RunActor adds `Order.RunActor` of the command name and the FocusId. It adds no `CoreMsg` case.
[ ] A Desk Load or Save runs no actor function. `startLoadSaveCommand` in `src/Server/Core/CoreActorPool.fs` returns `Ok None` for `LoadSavePath.Desk`. Git Load and Save log `Order.RunActor` in 10 — Dispatch git Load and Save.
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs`, `tests/Server.Tests/LoadSaveCommandTests.fs`, and `tests/Server.Tests/CoreMailboxDoorTests.fs` `CoreMailboxApi.startActor appends ActorStart to lifecycle events`.

### Reference

[ ] [Message shape](../cqrs-messages.md) item 4 **StartPeerActor** and decision 19 **One case each**.
[ ] Alan, 2026-10-24, on [core-refinement](../project.md): a Desk start and stop with no Actor stays on this ticket until a Desk start runs through an Actor.
[ ] Alan, 2026-11-03, on [Actor as client](../../actor-as-client/project.md): one start, and `Order.StartPeerActor` is obsolescent.

## Do not load

[ ] Do not delete `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, or `Order.StartPeerActor` here. That delete is [15 — Delete the old Actor starts](../../actor-as-client/issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md).
[ ] Do not move command names to functions here. That dispatcher is [07 — Dispatch the test command](../../actor-as-client/issues/07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md) and the tickets it blocks.
[ ] Closed: tickets 08, 81, 112, 113, 117, and 118 in this folder.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change the Actor start door so it places `Order.RunActor` of the command name and the FocusId through `Mailbox.PlaceOrder`. Add no `CoreMsg` case.
3. [ ] `placeOrder` sends `Order.RunActor` to the handler. The handler asks the dispatcher for that command name and calls `startFunction`. It uses `placed.orderId` as the submission id.
4. [ ] A Desk Load or Save logs `Order.RunActor` and runs no actor function. It does not write `Order.LoadSave`. It does not call `startPeerActor`.
5. [ ] Leave `Order.StartActor`, `Order.StartPeerActor`, and `Order.LoadSave` unsupported. This ticket does not write them.

## Outputs

[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs` — `RunActor places Order.RunActor`
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs` — `Desk load-save places its order and runs no Actor`
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` — keep `CoreMailboxApi.startActor appends ActorStart to lifecycle events`

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/80-place-actor-orders-through-placeorder-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when `Order.RunActor` must store more than the command name and the FocusId.
[ ] Stop when the edit adds a `CoreMsg` case.
[ ] Stop when the edit places `Order.StartPeerActor`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``RunActor places Order.RunActor``
  - `tests/Server.Tests`: ``Desk load-save places its order and runs no Actor``
  - `tests/Server.Tests`: ``CoreMailboxApi.startActor appends ActorStart to lifecycle events``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/80-place-actor-orders-through-placeorder-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
