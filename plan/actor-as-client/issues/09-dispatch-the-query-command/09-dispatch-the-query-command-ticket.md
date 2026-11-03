# 09 — Dispatch the query command
**Status**: defined
One job: The dispatcher maps a query Run to the query actor function, and that call is `startFunction`. Find and Move `recordSearchStart` logs `Order.RunActor` and does not write `Order.StartActor`.

[ ] `startFunction` is the pool start. This ticket does not replace it.
[ ] Find and Move stays inside this ticket. The added files are `CoreMailboxApi.fs` and `SearchActorTests.fs`. That is 4 source files and 3 test files, inside the size limit.

Type:

[ ] coding

Blockers:

[ ] [07 — Dispatch the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `startChosen` calls `SearchActor.functionStart` and then `pool.startFunction` when `SearchActor.isQueryRequest` is true.
[ ] `SearchActor.functionStart` builds a `FunctionStart` with `getGraph`, `focusId`, and `commandId`.
[x] The actor function's input is `FunctionRun` and `CoreChangesDoor`.
[o] The query start provides `ActorStart` of `zoomId`, `focusId`, `commandId`, `graphIds`, and `eventId`.
[ ] `recordSearchStart` in `src/Server/Core/CoreMailboxApi.fs` posts `CoreMsg.RecordSearchStart`. `CoreMailboxLoop` calls `CoreEventDispatch.actorStart`, which logs `Order.StartActor`.
[ ] `RouteRegistration.fs` calls `CoreMailboxApi.recordSearchStart`. `tests/Server.Tests/SearchActorTests.fs` calls it too.

### Reference

[ ] [Actor as client](../../project.md).

## Do not load

[ ] `RunAgentActor.fs`.
[ ] `CoreEventDispatch.fs`. The old arm stays until 15 — Delete the old Actor starts. This ticket stops calling it.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] The dispatcher builds the query actor function. The closure holds anything beyond `FunctionRun` and `CoreChangesDoor`, including the command node.
3. [ ] `startChosen` calls `pool.startFunction` for a query Run. The log line is `Order.RunActor`. This ticket does not write `Order.StartActor`.
4. [ ] `recordSearchStart` places `Order.RunActor` of the command name and the FocusId. It does not post `CoreMsg.RecordSearchStart`. It does not write `Order.StartActor`. Find and Move does not call `startFunction`.

## Outputs

[ ] `src/Server/ActorDispatch.fs`
[ ] `src/Server/SearchActor.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `tests/Server.Tests/QueryActorTests.fs` — the existing query start tests
[ ] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` — keep the `startFunction` tests green
[ ] `tests/Server.Tests/SearchActorTests.fs` — Find and Move logs `Order.RunActor`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/09-dispatch-the-query-command-stop.md`, then ask.
[ ] Stop when this ticket writes `Order.StartActor`.
[ ] Stop when a test file outside Outputs must change.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `QueryActorTests`
  - `tests/Server.Tests`: `CoreActorPoolFunctionStartTests`
  - `tests/Server.Tests`: `SearchActorTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/09-dispatch-the-query-command-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
