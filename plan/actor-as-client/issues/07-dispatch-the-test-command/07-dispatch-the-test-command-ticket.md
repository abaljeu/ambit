# 07 — Start the test command
**Status**: defined
One job: Run on a node that contains `?test` puts `Order.ActorOrder` on the mailbox queue. The actor function is `TestActor.functionActor`.

[ ] `execRunOp` in `src/Client/Commands.fs` is that Run command. It calls `queueRun`, which calls `RunLaunch.queueStart`.
[ ] `postActorStart` in `src/Server/Core/CoreMailboxActors.fs` starts the posted `ActorStart` through `startChosen` and `startActor`.
[ ] This ticket does not add a handler. `startActor` still starts the other command names.

Type:

[ ] coding

Blockers:

[ ] [06 — Log ActorOrder](../06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md)

## Inputs

### Working

[ ] `RouteRegistration.fs` registers `ActorName "test"` as `TestActor.actorFn` on `pool.register`, through `CoreRuntime` `Actors`.
[ ] `TestActor.actorFn` is an `ActorFn`. `TestActor.functionActor` is a `FunctionActor`.
[ ] `execRunOp` calls `CommandRequest.tryStart`, then `queueRun`.
[ ] `dispatchPostEvent` calls `postActorStart` for `EventBody.ActorStart`. `postActorStart` calls `startChosen`.
[ ] `postActorOrder : ActorOrder -> unit` is on `MailboxHost` after 06 — Log ActorOrder.

### Reference

[ ] [Arch](../../arch.md). The test command's function is `functionActor`.

## Do not load

[ ] `RunAgentActor.fs`, `GithubTransportActor.fs`, and `registerPeer`.
[ ] The AI session id. That is 08 — Start the AI Actor.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: `postActorStart` for a `?test` Run closes over the actor node id from that Run in the `FunctionActor`. It builds `ActorOrder` with that function, `getGraph`, the FocusId, and `actorName`, and calls `postActorOrder`.
3. [ ] Contract: Remove the `test` pair from the `pool.register` list in `RouteRegistration.fs`.
4. [ ] Do not add a handler, a route, or `ActorDispatch.fs`. Do not write `Order.StartActor` or `Order.RunActor`.
5. [ ] The mailbox pull runs that actor on the .NET thread pool. That pull is 80 — Place Actor starts through PlaceOrder.

## Outputs

[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/RouteRegistration.fs`
[ ] `src/Server/TestActor.fs`
[ ] `tests/Server.Tests/TestActorHelloTests.fs` — the existing hello tests, through `postActorOrder`
[ ] `tests/Server.Tests/TestActorCommandErrorTests.fs` — the existing command-error tests, through `postActorOrder`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/07-dispatch-the-test-command-stop.md`, then ask.
[ ] Stop when the edit adds a route, a handler, `ActorDispatch.fs`, or `actorForCommand`.
[ ] Stop when a test file outside Outputs must change.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `TestActorHelloTests`
  - `tests/Server.Tests`: `TestActorCommandErrorTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/07-dispatch-the-test-command-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
