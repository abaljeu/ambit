# 65 — Place Undo and Redo as orders
**Status**: `defined`
One job: The Server places a posted Undo or Redo as `Order.Undo` or `Order.Redo` of a target and an Op list through `placeOrder`, and `CoreEventDispatch.completeAction` handles those orders itself: it refuses a target from before the restart and fills an empty Op list from the target's inverse ops. The reason is decision 2 **Undo and Redo** in [Message shape](plan/core-refinement/cqrs-messages.md): the logged Undo or Redo holds the target and the filled Op list.

Type: coding
Blockers: none ([108 — Log Change as EventBody.Order](108-log-change-as-eventbody-order.md) is on staging)

## Inputs

### Working

#### Order and EventBody

[ ] ``Order`` in `src/Shared/History.fs` has `Undo of target: EventId` and `Redo of target: EventId`. They hold no Op list.
[ ] ``EventBody`` in `src/Shared/History.fs` has `Undo of target: EventId * ops: Op list` and `Redo of target: EventId * ops: Op list`.
[ ] ``Ev.ops``, ``Ev.target`` and ``Ev.withOps`` in `src/Shared/History.fs` read `EventBody.Undo` and `EventBody.Redo`. For `EventBody.Order`, they read only `Order.Change`.
[ ] ``encodeOrder`` and ``decodeOrderUndo`` / ``decodeOrderRedo`` in `src/Shared/EventJson.fs` write and read `kind` `undo` and `redo` with `target` only.
[ ] ``orderOfBody`` in `src/Shared/MailboxLog.fs` maps `EventBody.Undo(target, _)` to `Order.Undo target`, dropping the ops. `Redo` is the same.
[ ] ``historyBody`` in `src/Shared/MailboxLog.fs` maps `Order.Undo target` to `EventBody.Undo(target, [])`. `Redo` is the same.

#### Posting

[ ] ``changeMessage`` in `src/Server/Core/CoreMailboxApi.fs` places only a zero-id Change as `Mailbox.PlaceOrder`. An Undo or Redo posts `CoreMsg.PostEvent`.
[ ] ``postEvent`` in `src/Server/Core/CoreMailboxApi.fs` posts `CoreMsg.PostEvent` for any Ev. Only tests call it.
[ ] ``dispatchPostEvent`` in `src/Server/Core/CoreMailboxActors.fs` sends an Undo or Redo to `CoreEventDispatch.postEvent` with `graphOnly` `false`. Read only.
[ ] ``placeOrder`` in `src/Server/Core/CoreMailboxLoop.fs` replies `Error "unsupported"` for `Order.Undo` and `Order.Redo`.
[ ] ``replyEdit`` in `src/Server/Core/CoreMailboxLoop.fs` builds the Ev through `MailboxLog.toEv`, calls `CoreEventDispatch.postEvent`, and replies `Ok` of the stored id.

#### completeAction

[ ] ``completeAction`` in `src/Server/Core/CoreEventDispatch.fs` refuses `EventBody.Undo` or `EventBody.Redo` whose target is from before the restart, through `targetBeforeRestart`.
[ ] ``completeAction`` fills an empty Op list of `EventBody.Undo` or `EventBody.Redo` from `Ev.inverseOps` of the target.
[ ] ``completeAction`` passes `EventBody.Order (Order.Change _)` through unchanged.
[ ] ``completeAction`` refuses every other `EventBody.Order` with `Error "Order Events are mailbox-generated"`.
[ ] Alan, 2026-10-31: the `Order.Change` pass-through and the blanket `EventBody.Order` refusal are temporary. `Order` takes over each case as the old `EventBody` cases leave. This ticket begins that with `Order.Undo` and `Order.Redo`.

#### Match sites

[ ] `Order.Undo` and `Order.Redo` appear in `History.fs`, `EventJson.fs`, `MailboxLog.fs` and `CoreMailboxLoop.fs`.
[ ] Tests build `Order.Undo` and `Order.Redo` in `tests/Shared.Tests/EventJsonTests.fs` and `tests/Shared.Tests/MailboxLogTests.fs`.
[ ] `EventBody.Undo` and `EventBody.Redo` appear in 6 source files and 10 test files.

#### Tests

[ ] ``Ev JSON round-trips each EventBody.Order case`` in `tests/Shared.Tests/EventJsonTests.fs` builds `Order.Undo` and `Order.Redo` of a target only.
[ ] ``Undo and Redo keep the target and drop the ops`` in `tests/Shared.Tests/MailboxLogTests.fs`.
[ ] ``Undo, Redo, ActorStart, and Cancel still post as PostEvent`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` posts an Undo and a Redo through `postEvents`.
[ ] ``postEvents places Order.Change with origin Edit`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` reads the placed order of a stored Ev through `MailboxLog.ofEv`.
[ ] ``name-only Undo and Redo store completed Events with submission ids`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs` posts through `postEvent`.
[ ] ``Undo after restart refuses a target from before the restart`` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` posts through `postEvent` and needs the test database.

### Reference

[ ] [Message shape](plan/core-refinement/cqrs-messages.md) §2 **Order**, §3 decision 2 **Undo and Redo**, and decision 1 **Logged as placed**.

## Do not load

[ ] Removing the `EventBody.Undo` and `EventBody.Redo` cases and their JSON kinds is [109 — Log Undo, Redo, and Load as EventBody.Order](109-log-undo-redo-and-load-as-eventbody-order.md).
[ ] Load, ActorStart, Cancel and their `completeAction` arms stay.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] `Order.Undo` and `Order.Redo` carry the target `EventId` and the Op list.
3. [ ] A posted Undo or Redo reaches `placeOrder` as `Order.Undo` or `Order.Redo`, and `placeOrder` no longer replies unsupported for them.
4. [ ] `completeAction` handles `Order.Undo` and `Order.Redo`: it refuses a target from before the restart, and fills an empty Op list from the target's inverse ops.
5. [ ] The stored Undo or Redo holds the target and the filled Op list.
6. [ ] Add a test that a placed empty-op Undo of a Change stores `Order.Undo` of that target and its inverse ops, and the graph returns to its state before the Change.
7. [ ] Add a test that a placed empty-op Redo of that Undo stores `Order.Redo` of the Undo and its ops, and the graph returns to its state after the Change.
8. [ ] Add a test that a placed Undo of a target from before the restart is refused.

## Outputs

[ ] The source files the builder changes, up to 8
[ ] The three tests from steps 6, 7 and 8, in `tests/Server.Tests`
[ ] Any test from Inputs whose Undo or Redo shape changes

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/65-place-undo-and-redo-as-orders-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts [Message shape](plan/core-refinement/cqrs-messages.md).
[ ] Stop when the edit needs more than 8 source files or 4 test files.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: the three new test names written in the test files
  - `tests/Server.Tests`: ``postEvents places Order.Change with origin Edit``
  - `tests/Server.Tests`: ``name-only Undo and Redo store completed Events with submission ids``
  - `tests/Server.Tests`: ``Undo after restart refuses a target from before the restart``
  - `tests/Shared.Tests`: ``Ev JSON round-trips each EventBody.Order case``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/65-place-undo-and-redo-as-orders-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
