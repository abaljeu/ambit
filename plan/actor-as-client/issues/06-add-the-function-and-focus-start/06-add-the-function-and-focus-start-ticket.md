# 06 — Log Order.RunActor
**Status**: defined
One job: The log has `Order.RunActor` of the command name and the FocusId. The pool start stays `startFunction`.

[ ] No new pool start. No new `CoreMsg` case. The folder name stays `06-add-the-function-and-focus-start` so existing links keep their path.

Type:

[ ] coding

Blockers:

[ ] none

## Inputs

### Working

[x] `startFunction` in `src/Server/Core/CoreActorPool.fs` takes `FunctionStart`. `FunctionActor` is `FunctionRun -> CoreChangesDoor -> Async<unit>`.
[o] `startActor` and `startPeerActor` are the other pool starts.
[o] `CoreMsg.StartActor` and `CoreMsg.StartPeerActor` are the start path.
[ ] `Order` in `src/Shared/History.fs` has `StartActor` and `StartPeerActor`. It has no `RunActor`.
[ ] `Order` is matched in `src/Shared/EventJson.fs`, `src/Shared/MailboxLog.fs`, and `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs`.

### Reference

[ ] [Actor as client](../../project.md) decisions of 2026-11-03.
[ ] [Size limit](../../context/size-limit.md). This ticket is the Order expand only. A parallel pool start was dropped, so the earlier nine-file expand is not this ticket.

## Do not load

[ ] Do not add a `CoreMsg` case.
[ ] Do not add a pool field. Do not change `startFunction`'s arguments.
[ ] Do not move a caller. That is 07 — Dispatch the test command and the tickets after it.
[ ] Do not delete `startActor` or `startPeerActor`. That is 15 — Delete the old Actor starts.
[ ] Closed: `src/Client`. The browser `ActorStart` payload stays for this cutover. It is [o].
[ ] Per-run endpoints are later: the endpoint varies by what is being run, each with its own payload.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Add `Order.RunActor of commandName: string * focusId: NodeId` beside `Order.StartActor` and `Order.StartPeerActor`.
3. [ ] The payload is the command name and the FocusId. It does not store a function, an `ActorStart`, or a `CoreMsg`.
4. [ ] Add the `EventJson` and `MailboxLog` arms, and a `placeOrder` arm that stays unsupported. [80 — Place Actor starts through PlaceOrder](../../../core-refinement/issues/80-place-actor-orders-through-placeorder.md) places it.
5. [ ] Leave `startFunction`, `startActor`, `startPeerActor`, and `CoreMsg` as they are.

## Outputs

[ ] `src/Shared/History.fs`
[ ] `src/Shared/EventJson.fs`
[ ] `src/Shared/MailboxLog.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `tests/Shared.Tests/EventJsonTests.fs` — `Order.RunActor round-trips command name and FocusId`
[ ] `tests/Shared.Tests/MailboxLogTests.fs` — `RunActor carries command name and FocusId`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/06-add-the-function-and-focus-start-stop.md`, then ask.
[ ] Stop when `Order.RunActor` must store more than the command name and the FocusId.
[ ] Stop when the edit adds a `CoreMsg` case or a pool start.
[ ] Stop when the edit needs a file outside Inputs.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``Order.RunActor round-trips command name and FocusId``
  - `tests/Shared.Tests`: ``RunActor carries command name and FocusId``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/06-add-the-function-and-focus-start-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
