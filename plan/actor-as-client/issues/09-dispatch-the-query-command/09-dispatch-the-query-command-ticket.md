# 09 — Dispatch the query command
**Status**: defined
One job: The dispatcher maps a query Run to the query actor function, and that call is `startFunction`. Find and Move `recordSearchStart` logs `Order.RunActor`. This ticket deletes `CoreMsg.RecordSearchStart`.

[ ] `startFunction` is the pool start. This ticket keeps it.
[ ] Alan, 2026-11-03: the search-start log is `Order.RunActor`. This ticket deletes `CoreMsg.RecordSearchStart`.
[ ] The source files are 7. The test files are 3. The size limit is 8 source files and 4 test files.

Type:

[ ] coding

Blockers:

[ ] [07 — Dispatch the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `startChosen` calls `SearchActor.functionStart` and then `pool.startFunction` when `SearchActor.isQueryRequest` is true. `startChosen` is `src/Server/Core/CoreMailboxActors.fs` lines 82-94.
[ ] `SearchActor.functionStart` builds a `FunctionStart` with `getGraph`, `focusId`, and `commandId`. It is `src/Server/SearchActor.fs` lines 71-90.
[x] The actor function's input is `FunctionRun` and `CoreChangesDoor`.
[o] The query start provides `ActorStart` of `zoomId`, `focusId`, `commandId`, `graphIds`, and `eventId`.
[ ] `recordSearchStart` in `src/Server/Core/CoreMailboxApi.fs` lines 308-314 posts `CoreMsg.RecordSearchStart`. That function is the only constructor.
[ ] `CoreMailboxLoop.runMsg` lines 25-27 call `CoreEventDispatch.actorStart`. `actorStart` logs `EventBody.ActorStart`, which is `Order.StartActor`. `actorStart` is `src/Server/Core/CoreEventDispatch.fs` lines 72-77. That `runMsg` arm is its only caller.
[ ] `CoreMailboxLoop.messageCaller` matches `RecordSearchStart` at line 73.
[ ] `CoreMailboxEvents.operationContext` matches `RecordSearchStart` at line 48.
[ ] `CoreMailboxEvents.replyFailure` matches `RecordSearchStart` at lines 66-67.
[ ] `CoreMsg.RecordSearchStart` is `src/Server/Core/CoreMsg.fs` lines 26-29. The comment on line 25 names that case.
[ ] `RouteRegistration.searchDoor` calls `recordSearchStart` at `src/Server/RouteRegistration.fs` line 229.
[ ] `tests/Server.Tests/SearchActorTests.fs` calls `recordSearchStart` at line 234.
[ ] `src/Server/SearchActor.fs` is 90 lines.
[ ] `src/Server/Core/CoreMailboxActors.fs` is 258 lines.
[ ] `src/Server/Core/CoreMailboxApi.fs` is 400 lines.
[ ] `src/Server/Core/CoreMsg.fs` is 116 lines.
[ ] `src/Server/Core/CoreMailboxLoop.fs` is 464 lines.
[ ] `src/Server/Core/CoreMailboxEvents.fs` is 138 lines.
[ ] `src/Server/ActorDispatch.fs` is absent on staging. This ticket adds it.
[ ] `tests/Server.Tests/QueryActorTests.fs` is 333 lines.
[ ] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` is 152 lines.
[ ] `tests/Server.Tests/SearchActorTests.fs` is 288 lines.
[ ] `RouteRegistration.searchDoor` and `SearchActorTests` call `recordSearchStart`. They build no `CoreMsg` case. Deleting the case here leaves those callers on the function. 15 — Delete the old Actor starts leaves this case to this ticket.

### Reference

[ ] [Actor as client](../../project.md).

## Do not load

[ ] `RunAgentActor.fs`.
[ ] `CoreEventDispatch.fs`. This ticket deletes the `RecordSearchStart` arm and leaves `actorStart`.
[ ] `RouteRegistration.fs`. The call to `recordSearchStart` stays.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] The dispatcher builds the query actor function. The closure holds anything beyond `FunctionRun` and `CoreChangesDoor`, including the command node.
3. [ ] `startChosen` calls `pool.startFunction` for a query Run. The log line is `Order.RunActor`.
4. [ ] `recordSearchStart` places `Order.RunActor` of the command name and the FocusId. Find and Move stays on `recordSearchStart`. The query Run is the path that calls `startFunction`.
5. [ ] Delete `CoreMsg.RecordSearchStart`. Delete the `runMsg` arm, the `messageCaller` arm, the `operationContext` arm, and the `replyFailure` arm.
6. [ ] The search-start log is `Order.RunActor`. The `Order` cases in `src/Shared/History.fs` stay as they are.

## Outputs

[ ] `src/Server/ActorDispatch.fs`
[ ] `src/Server/SearchActor.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Server/Core/CoreMsg.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxEvents.fs`
[ ] `tests/Server.Tests/QueryActorTests.fs` — the existing query start tests
[ ] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` — keep the `startFunction` tests green
[ ] `tests/Server.Tests/SearchActorTests.fs` — Find and Move logs `Order.RunActor`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/09-dispatch-the-query-command-stop.md`, then ask.
[ ] Stop when this ticket writes `Order.StartActor`.
[ ] Stop when this ticket adds an `Order` case.
[ ] Stop when a constructor of `CoreMsg.RecordSearchStart` sits outside `CoreMailboxApi.recordSearchStart`.
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
