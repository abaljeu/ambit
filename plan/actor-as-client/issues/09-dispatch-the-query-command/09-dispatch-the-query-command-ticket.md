# 09 — Dispatch the query command
**Status**: defined
One job: The dispatcher maps a query Run to the query function, and that call is `startFunction`.

[ ] `startFunction` is the pool start. This ticket does not replace it.

Type:

[ ] coding

Blockers:

[ ] [07 — Dispatch the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `startChosen` calls `SearchActor.functionStart` and then `pool.startFunction` when `SearchActor.isQueryRequest` is true.
[ ] `SearchActor.functionStart` builds a `FunctionStart` with `getGraph`, `focusId`, and `commandId`.
[x] The function input is `FunctionRun` and `CoreChangesDoor`.
[o] The query start provides `ActorStart` of `zoomId`, `focusId`, `commandId`, `graphIds`, and `eventId`.

### Reference

[ ] [Actor as client](../../project.md).

## Do not load

[ ] Find and Move bookkeeping: `RecordSearchStart` and `recordSearchStart`. Alan named browser Run and git Load and Save. He did not name this bookkeeping start.
[ ] `RunAgentActor.fs`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] The dispatcher builds the query function. The closure holds anything beyond `FunctionRun` and `CoreChangesDoor`, including the command node.
3. [ ] `startChosen` calls `pool.startFunction` for a query Run.
4. [ ] Leave `startFunction` as the pool start. The logged order is `Order.RunActor` of the command name and the FocusId.

## Outputs

[ ] `src/Server/ActorDispatch.fs`
[ ] `src/Server/SearchActor.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `tests/Server.Tests/QueryActorTests.fs` — the existing query start tests
[ ] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` — keep the `startFunction` tests green

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/09-dispatch-the-query-command-stop.md`, then ask.
[ ] Stop when Find and Move `recordSearchStart` must move in the same edit.
[ ] Stop when a test file outside Outputs must change.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `QueryActorTests`
  - `tests/Server.Tests`: `CoreActorPoolFunctionStartTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/09-dispatch-the-query-command-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
