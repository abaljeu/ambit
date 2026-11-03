# 80 — Place Actor starts through PlaceOrder
**Status**: defined
One job: The Server places the function-and-focus start through `Mailbox.PlaceOrder`. `Order.StartPeerActor` is not that path.

[ ] The accepted start is logged as today, as an `EventBody.Order`, and the reply stays `Result<unit, string>` at `CoreMailboxApi`.

Type:

[ ] coding

Blockers:

[ ] [06 — Add the function-and-focus start](../../actor-as-client/issues/06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md)

## Inputs

### Working

[ ] `CoreMsg.StartActor`, `CoreMsg.StartPeerActor`, and `CoreMsg.StartLoadSaveCommand` are built in `CoreMailboxApi` (`startActor`, `startPeerActor`, `startLoadSaveCommand`).
[ ] `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` replies unsupported for `Order.StartActor`, `Order.StartPeerActor`, `Order.LoadSave`, and `Order.Cancel`.
[ ] 06 — Add the function-and-focus start adds the function-and-focus message and its Order beside those cases.
[ ] A Desk Load or Save runs no Actor. `startLoadSaveCommand` in `src/Server/Core/CoreActorPool.fs` returns `Ok None` for `LoadSavePath.Desk`.
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
2. [ ] Change `CoreMailboxApi` so the function-and-focus start places the new Order through `Mailbox.PlaceOrder`.
3. [ ] `placeOrder` sends that Order to the handler the new message uses. The handler uses `placed.orderId` as the submission id.
4. [ ] A Desk Load or Save that runs no Actor still logs its start and then `Notice.ActorStopped` of its focus and `ActorSucceeded`. It does not call `startPeerActor`.
5. [ ] Leave `Order.StartPeerActor` unsupported. Do not add a handler that keeps it.

## Outputs

[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs` — `function start places its Order`
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs` — `Desk load-save places its order and runs no Actor`
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` — keep `CoreMailboxApi.startActor appends ActorStart to lifecycle events`

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/80-place-actor-orders-through-placeorder-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the new Order must store the function. A function is not a log value. Ask.
[ ] Stop when the edit places `Order.StartPeerActor`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``function start places its Order``
  - `tests/Server.Tests`: ``Desk load-save places its order and runs no Actor``
  - `tests/Server.Tests`: ``CoreMailboxApi.startActor appends ActorStart to lifecycle events``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/80-place-actor-orders-through-placeorder-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
