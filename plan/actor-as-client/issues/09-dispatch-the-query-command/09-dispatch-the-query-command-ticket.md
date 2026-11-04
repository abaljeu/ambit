# 09 — Start the query command
**Status**: defined
One job: `postSearchRequest` builds the query `FunctionActor` and puts `Order.ActorOrder` on the mailbox queue. This ticket deletes `CoreMsg.RecordSearchStart` and `CoreEventDispatch.actorStart`.

[ ] `startFunction` stays. The query start on `startChosen` leaves.
[ ] Find and Move stays on `recordSearchStart`. That call logs `Order.ActorOrder` and does not run the actor.
[ ] The source files are 7. The test files are 3. The size limit is 8 source files and 4 test files.

Type:

[ ] coding

Blockers:

[ ] [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `postSearchRequest` is in `src/Server/RouteRegistration.fs`. It calls `Api.postSearch`.
[ ] `searchDoor` calls `CoreMailboxApi.recordSearchStart`.
[ ] `startChosen` calls `SearchActor.functionStart` and then `pool.startFunction` when `SearchActor.isQueryRequest` is true. `startChosen` is in `src/Server/Core/CoreMailboxActors.fs`.
[ ] `SearchActor.functionStart` builds a `FunctionStart` with `getGraph`, `focusId`, and `commandId`. It is in `src/Server/SearchActor.fs`.
[x] The actor function's input is `FunctionRun` and `ActorChanges`.
[ ] `recordSearchStart` in `src/Server/Core/CoreMailboxApi.fs` posts `CoreMsg.RecordSearchStart`. That function is the only constructor.
[ ] `CoreMailboxLoop.runMsg` calls `CoreEventDispatch.actorStart` for `RecordSearchStart`. `actorStart` logs `EventBody.ActorStart`, which is `Order.StartActor`. That `runMsg` arm is its only caller.
[ ] `CoreMailboxLoop.messageCaller`, `CoreMailboxEvents.operationContext`, and `CoreMailboxEvents.replyFailure` match `RecordSearchStart`.
[ ] `tests/Server.Tests/SearchActorTests.fs` calls `recordSearchStart`.

### Reference

[ ] [Arch](../../arch.md). `postSearchRequest` is the query handler.

## Do not load

[ ] `RunAgentActor.fs`.
[ ] `SearchActor.actorStart`. That function builds an `ActorStart`. It is not `CoreEventDispatch.actorStart`.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.
[ ] A new `CoreMsg` case. Remote starts stay out of scope.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: `postSearchRequest` builds `ActorOrder` from the query `FunctionActor`, `getGraph`, the FocusId, and the command name, and calls `postActorOrder`.
3. [ ] Contract: `startChosen` no longer starts a query. Other names still call `startActor`.
4. [ ] Change: `recordSearchStart` logs `Order.ActorOrder` and does not run the actor. Find and Move stays on `recordSearchStart`.
5. [ ] Contract: Delete `CoreMsg.RecordSearchStart`. Delete the `runMsg` arm, the `messageCaller` arm, the `operationContext` arm, and the `replyFailure` arm. Delete `CoreEventDispatch.actorStart` with that `runMsg` arm.
6. [ ] Do not write `Order.StartActor` or `Order.RunActor`. Do not add an `Order` case.

## Outputs

[ ] `src/Server/RouteRegistration.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Server/Core/CoreMsg.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxEvents.fs`
[ ] `src/Server/Core/CoreEventDispatch.fs` — delete `actorStart`
[ ] `tests/Server.Tests/QueryActorTests.fs` — the existing query start tests
[ ] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` — keep the `startFunction` tests green
[ ] `tests/Server.Tests/SearchActorTests.fs` — Find and Move logs `Order.ActorOrder`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/09-dispatch-the-query-command-stop.md`, then ask.
[ ] Stop when this ticket writes `Order.StartActor`.
[ ] Stop when logging `Order.ActorOrder` from `recordSearchStart` needs a door other than the log.
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
