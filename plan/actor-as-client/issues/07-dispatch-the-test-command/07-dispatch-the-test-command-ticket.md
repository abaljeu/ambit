# 07 — Dispatch the test command
**Status**: defined
One job: A dispatcher outside Core maps the command name `test` to the TestActor function, and that function is `startFunction`.

[ ] `startActor` still starts the other command names. The split keeps this ticket inside the size limit. The AI Actor, the query command, and git Load and Save are the next tickets.

Type:

[ ] coding

Blockers:

[ ] [06 — Log Order.RunActor](../06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md)

## Inputs

### Working

[ ] `RouteRegistration.fs` registers `ActorName "test"` as `TestActor.actorFn` on `pool.register`, through `CoreRuntime` `Actors`.
[ ] `TestActor.actorFn` is an `ActorFn`. `TestActor.functionActor` is a `FunctionActor` used from `CoreActorPoolFunctionStartTests`.
[ ] `startChosen` in `src/Server/Core/CoreMailboxActors.fs` calls `pool.startActor` unless `SearchActor.isQueryRequest` is true.
[ ] Browser Run arrives as a posted start. `dispatchPostEvent` calls `postActorStart`, which calls `startChosen`.

### Reference

[ ] [Actor as client](../../project.md). The dispatcher is outside Core. The pool does not keep this name.
[ ] [05 — Expand-contract first aspect](../05-expand-contract-first-aspect.md). TestActor is the pilot.

## Do not load

[ ] `RunAgentActor.fs`, `GithubTransportActor.fs`, and `registerPeer`.
[ ] The AI session id. That is [08 — Dispatch the AI Actor](../08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md).

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Add `src/Server/ActorDispatch.fs`. It maps the command name `test` to a function. The module is outside `src/Server/Core`.
3. [ ] The function closes over anything it needs beyond `FunctionRun` and `CoreChangesDoor`. The call is `pool.startFunction`.
4. [ ] `startChosen` uses the dispatcher for `test`. Any other non-query name still calls `pool.startActor`.
5. [ ] The logged order for this command is `Order.RunActor` of the command name and the FocusId. Placing that order is 80 — Place Actor starts through PlaceOrder. This ticket does not write `Order.StartActor`.
6. [ ] Remove the `test` pair from the `pool.register` list in `RouteRegistration.fs`.

## Outputs

[ ] `src/Server/ActorDispatch.fs`
[ ] `src/Server/RouteRegistration.fs`
[ ] `src/Server/Core/CoreRuntime.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/TestActor.fs`
[ ] `tests/Server.Tests/TestActorHelloTests.fs` — the existing hello tests, on the dispatcher path
[ ] `tests/Server.Tests/TestActorCommandErrorTests.fs` — the existing command-error tests, on the dispatcher path

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/07-dispatch-the-test-command-stop.md`, then ask.
[ ] Stop when a test file outside Outputs must change.
[ ] Stop when the command name is not available to code outside `src/Server/Core`.
[ ] Stop when the edit contradicts the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `TestActorHelloTests`
  - `tests/Server.Tests`: `TestActorCommandErrorTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/07-dispatch-the-test-command-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
