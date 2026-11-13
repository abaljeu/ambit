STOP: the door's test callers sit outside Outputs, and the size limit of 4 tests cannot hold them.

`coreMailboxStartActor` in `src/Server/Core/CoreMailboxApi.fs` places `Order.StartActorDeprecated` through `MailboxHost.postPlacedOrder`. `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` sends that order to `dispatchStartActor` in `src/Server/Core/CoreMailboxActors.fs`. No file under `src/` calls `coreMailboxStartActor`. That loop arm is the only reach of `dispatchStartActor`.

`Order.StartActorDeprecated` stays. [16 — Delete the old Order cases](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) owns it. `EventBody.ActorStart` stays as the client start request. A posted name with no function start replies `Error "unknown actor"`.

[25 — Remove the coreMailboxStartActor door](25-remove-the-coremailboxstartactor-door-ticket.md) names two test files in Outputs and says to move those calls onto `coreMailboxStartActorFromFunction` or `postActorOrder`, then delete `coreMailboxStartActor`. The door still has the callers below. Deleting the function needs an edit in each caller file. Outputs name `CoreMailboxApi.fs`, `GithubTransportActorTests.fs`, and `TestActorCommandErrorTests.fs`. The size limit is 8 source files and 4 tests. The caller set is larger than both caps. The ticket file stays as written.

A delete is a test that only starts a fixture name through the door. The fixture names in these tests are root, probe, load, and a test start. A move keeps a behavior that `coreMailboxStartActorFromFunction` still performs. The function is `TestActor.functionActorAt`, `RunAgentActor.actorFn`, `RunAgentActor.aiActorOrder`, or `SearchActor.functionActorAt`. A hang or a probe uses an `ActorFunction` of the same shape as `RunAgentActor.actorFn`. The door stores `EventBody.ActorStart`. A function start stores `Order.StartActor`. A moved start assertion follows `Order.StartActor`.

## `tests/Server.Tests/GithubTransportActorTests.fs`

- ``Peer Actor is not available through the Run actor table`` — delete. The start is the fixture name load, and the reply is `actor 'load' not registered`.

## `tests/Server.Tests/TestActorCommandErrorTests.fs`

- ``unregistered Actor name fails start`` — delete. The start is an unregistered name, and the reply contains `not registered`.

## `tests/Server.Tests/CoreMailboxDoorTests.fs`

- ``CoreMailboxApi.startActor calls pool.startActor and returns bookkeeping result`` — delete. The start is the root fixture through `pool.startActor`.
- ``CoreMailboxApi.startActor from an inactive Actor is dropped`` — move onto `TestActor.functionActorAt`. `admitCaller` still refuses an inactive Actor.
- ``Actor start rejects a secret the pool does not admit`` — delete. The assertion is `pool.admit` after `pool.startActor`. `startFunction` has no separate admit.
- ``Actor post with a secret the pool does not hold is dropped`` — delete. The admit count is the old start. ``Actor PostChange without a live row is dropped`` in `CoreMsgActorCasesTests.fs` already posts with a secret the pool does not hold.
- ``CoreMailbox door exposes Graph lockPresent via getState`` — move onto `TestActor.functionActorAt`. `graphAtEvent` stamps `lockPresent` from the live table.
- ``CoreMailboxApi.startActor appends ActorStart to lifecycle events`` — delete. The stored body is the door's `EventBody.ActorStart`.
- ``CoreActorPool.startActor uses client graphIds to build subgraph`` — delete. `startFunction` takes no `graphIds`.
- ``CoreActorPool.startActor selects actor from command node text`` — delete. The selection is the fixture name test.
- ``CoreActorPool.startActor fails when graphIds is empty`` — delete. That check is `pool.startActor`.
- ``CoreActorPool.startActor fails when runDefinitionId not in graphIds`` — delete. That check is `pool.startActor`.
- ``mailbox records ActorStarted before actor body runs`` — delete. The door appends `EventBody.ActorStart` before `schedule`. A function start schedules, then stores `Order.StartActor`.
- ``CoreActorPool.startActor creates live row synchronously`` — move onto `TestActor.functionActorAt`. `startFunction` puts the live row before it returns.
- ``ActorStart persist Error leaves Focus out of liveActorNodeIds`` — move onto `TestActor.functionActorAt`. An append error still drops the secret.

## `tests/Server.Tests/CoreMsgActorCasesTests.fs`

- ``StartActor with live credentials calls startActor with ActorStart`` — delete. The pool stub is `startActor`.
- ``StartActor reply is startActor bookkeeping without waiting for an Actor body`` — move onto an `ActorFunction` that returns at once, in the shape of `RunAgentActor.actorFn`.
- ``GetState stamps lockPresent from the live table after startActor`` — move onto `TestActor.functionActorAt`.
- ``StartActor from an inactive Actor does not hand off`` — move onto `TestActor.functionActorAt`.
- ``StartActor with blank Authority does not hand off`` — move onto `TestActor.functionActorAt`.
- ``live Actor stop succeeds and a later stop is dropped`` — move. The start becomes an `ActorFunction` that yields its secret, in the shape of `RunAgentActor.actorFn`. The stop stays `actorStop`.

## `tests/Server.Tests/CoreActorPoolDeliverTests.fs`

- ``mint deliver enqueue drop then deliver fails`` — move. The start becomes an `ActorFunction` that stays live and records the session, in the shape of `RunAgentActor.actorFn`. `TestActor.functionActorAt` returns.
- ``empty deliver keeps session live for a later chunk`` — move, same function shape.
- ``second start on the same runDefinitionId is rejected`` — delete. The reply is `command already has a live Actor` from `pool.startActor`. ``second launch on the same Focus is refused`` in `AgentAskTests.fs` already checks function-start exclusivity.

## `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`

`runMixedHello` is the door call for the first five tests. ``File empty Graph checkpoint live append continues past Graph`` calls the door itself. Each move uses `TestActor.functionActorAt`. `mixedIds` follows `Order.StartActor` in place of `EventBody.ActorStart`.

- ``File mixed hello survives dispose create`` — move.
- ``Db mixed hello survives dispose create`` — move.
- ``Undo after restart refuses a target from before the restart`` — move.
- ``Undo after restart accepts a target after the restart`` — move.
- ``getEventId is ActorStop after in-process hello`` — move.
- ``File empty Graph checkpoint live append continues past Graph`` — move.

## `tests/Server.Tests/PersistHandlersRestoreTests.fs`

- ``ActorStart persists across File restart`` — move onto `TestActor.functionActorAt`. The restored body is `Order.StartActor`.
- ``ActorStart persist Error does not keep Ev in mailbox log`` — move onto `TestActor.functionActorAt`.

## `tests/Server.Tests/Issue41CoreMailboxTests.fs`

- ``mailbox appends ActorStart and ActorStop in lifecycle order`` — move onto `TestActor.functionActorAt`. The start body is `Order.StartActor`. The stop body stays `EventBody.ActorStop`.

## `tests/Server.Tests/AskCancelHarness.fs`

`startOnCommand` is the door call. The tests that reach it:

- ``Change before Cancel keeps the accepted children`` in `tests/Server.Tests/CancelByFocusTests.fs` — move. The probe becomes an `ActorFunction` in the shape of `RunAgentActor.actorFn`.
- ``Ask extract carries Graph.focus from launch`` in `tests/Server.Tests/AgentAskTests.fs` — move, same function shape, reading focus from `FunctionRun.getGraph`.

## Count

- Delete: 13 tests.
- Move: 22 tests.
- Files with a door call: 9. Two more files reach the harness.
- Outputs hold 2 of those test files, and both of those tests are deletes.

## Suites

`scripts/agent-test.sh --full` ran `dotnet test tests/Server.Tests`. Server passed 777, failed 1, skipped 1. The failure is ``DbAgent serves reads while sweep buffers FIFO mutations then trims`` in `tests/Server.Tests/DbAgentTests.fs`. The message is `Expected first queued mutation to complete first.` A direct rerun of that one test passed. That test is outside this ticket. The script stopped before Shared. Shared then ran with the same `dotnet test` arguments and passed 1940, failed 0, skipped 3.
