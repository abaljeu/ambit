# 07 — Start the test command
**Status**: coded
One job: Run on a node that contains `?test` puts `Order.StartActor` of `ActorOrder` on the mailbox queue. The actor function is `TestActor.functionActorAt`.

[x] `postActorStart` in `src/Server/Core/CoreMailboxActors.fs` calls `queueTestOrder`. `queueAdmittedTest` builds the order with `TestActor.functionActorAt` in `src/Server/TestActor.fs` and calls `MailboxHost.postActorOrder`.
[x] `postActorOrder` in `src/Server/Core/MailboxHost.fs` places `Order.StartActor`.
[x] `RouteRegistration.CreateBoot` in `src/Server/RouteRegistration.fs` registers `ActorName "ai"` only.

[ ] `execRunOp` in `src/Client/Commands.fs` is that Run command. It calls `queueRun`, which calls `RunLaunch.queueStart`.
[x] `postActorStart` in `src/Server/Core/CoreMailboxActors.fs` starts `?test` through `queueTestOrder`. Other names go through `startChosen` and `startActor`.
[x] This ticket does not add a handler. `startActor` still starts the other command names.

Type:

[x] coding

Blockers:

[x] [06 — Log ActorOrder](../06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md)

## Inputs

### Working

[x] `RouteRegistration.CreateBoot` registers `ActorName "ai"` on `CoreRuntime` `Actors`. The `test` pair is absent.
[ ] `TestActor.actorFn` is an `ActorFn`. `TestActor.functionActor` and `TestActor.functionActorAt` are `ActorFunction` values in `src/Server/TestActor.fs`. The pool `ActorFunction` is in `src/Server/Core/CoreActorPool.fs`. `functionActorAt` returns the shared `ActorFunction` in `src/Shared/History.fs`.
[ ] `execRunOp` calls `CommandRequest.tryStart`, then `queueRun`.
[x] `dispatchPostEvent` calls `postActorStart` for `EventBody.ActorStart`. `EventBody.ActorStart` is `Order.StartActorDeprecated` in `src/Shared/History.fs`.
[x] `postActorOrder` in `src/Server/Core/MailboxHost.fs` takes `MailboxHost`, `Caller`, and `ActorOrder`.

### Reference

[x] [Arch](../../arch.md). The test command's function is `functionActorAt`.

## Do not load

[ ] `RunAgentActor.fs`, `GithubTransportActor.fs`, and `registerPeer`.
[ ] The AI session id. That is 08 — Start the AI Actor.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change: `postActorStart` for a `?test` Run closes over the run definition id and the focus id in `TestActor.functionActorAt`. It builds `ActorOrder` with that function, `actorNodeId`, and `actorName`, and calls `postActorOrder`. `actorNodeId` is `ActorStart.focusId`.
3. [x] Contract: The `test` pair is absent from `RouteRegistration.CreateBoot`. The list is `ActorName "ai"`.
4. [x] Do not add a handler, a route, or `ActorDispatch.fs`. Do not write `Order.StartActorDeprecated`. Leave `Order` as the cases in `src/Shared/History.fs`.
5. [ ] The mailbox pull runs that actor on the .NET thread pool. That pull is [18 — Place ActorOrder through PlaceOrder](../18-place-actororder-through-placeorder/18-place-actororder-through-placeorder-ticket.md).

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
