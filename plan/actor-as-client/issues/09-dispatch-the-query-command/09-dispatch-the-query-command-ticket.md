# 09 — Start the query command
**Status**: coded
One job: The Server posts Find and Move and a query Run as `Order.StartActor` of `ActorOrder` after the pool admits the actor and launches it. A pool refusal posts no row. The reply is the failure. This ticket deletes `CoreMsg.RecordSearchStart`, and that delete removes `CoreEventDispatch.actorStart`.

[x] `startFunction` stays. A query Run leaves `startChosen` and posts through `postBuiltOrder`.
[x] Find and Move stays on `recordSearchStart`. That function posts `Order.StartActor`, and the Search Actor runs.
[x] This ticket adds no session id. `FunctionRun` stays `getGraph` and `secret`.
[x] The source files are 10. The test facts are 3. The size limit is 8 source files and 4 tests. The source count is over the limit. `BootActors` already carries the boot value, so the extra files are that record and `RouteRegistration`.

Type:

[x] coding

Blockers:

[x] [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[x] `postSearchRequest` in `src/Server/RouteRegistration.fs` calls `Api.postSearch`.
[x] `searchDoor` calls `CoreMailboxApi.recordSearchStart`. `recordStop` calls `CoreMailboxApi.recordSearchStop`.
[x] `Api.searchReply` in `src/Server/Api.fs` builds `SearchActor.actorStart`, calls `recordStart`, calls `SearchActor.reply`, then calls `recordStop`.
[x] `startChosen` in `src/Server/Core/CoreMailboxActors.fs` calls `context.pool.startActor` for a query start and for a named start. `startFunction` stays unused on that path.
[x] `postActorStart` sends `?test` through `queueAdmittedTest` and `?ai` through `queueAdmittedAi`. Every other name calls `postChosenStart`, which calls `startChosen`.
[x] `whenAdmitted` calls `CoreMailboxEvents.admitCaller`, then the queue function. `queueAdmittedTest` builds `ActorOrder` with `ActorOrderFromStart.orderFromStart` and `TestActor.functionActorAt`, then calls `postBuiltOrder`.
[x] `postBuiltOrder` calls `MailboxHost.postActorOrder`.
[x] `SearchActor.functionStart` in `src/Server/SearchActor.fs` builds a pool `FunctionStart`. `actorNodeId` is `request.focusId`. The body stops with `ActorQuery`. `FunctionStart` is in `src/Server/Core/CoreActorPool.fs`.
[x] `SearchActor.isQueryRequest` is true when `CommandRequest.isQueryText` is true for the run-definition node.
[x] `recordSearchStart` in `src/Server/Core/CoreMailboxApi.fs` posts `CoreMsg.RecordSearchStart`. That function is the only constructor.
[x] The Server project file lists `Core/CoreMailboxApi.fs` before `SearchActor.fs`. `RouteRegistration.fs` is listed after `SearchActor.fs`.
[x] `BootActors` in `src/Server/GrokBotSettings.fs` is `keys`, `repos`, and `grok`. `CoreBoot.bootActors` holds that record. `CoreRuntime.createPublishing` passes `Some install` to `CoreActorPool.createWithInstall`. `CoreActorPool.bootActors` stores it. `queueAdmittedAi` reads it.
[x] `RouteRegistration.CreateBoot` sets `bootActors`. `RouteRegistration.searchDoor` is the production caller of `recordSearchStart`.
[x] `tests/Server.Tests/AskCancelHarness.fs` builds the other `BootActors` literal and calls `CoreActorPool.createWithInstall`.
[x] `CoreMailboxLoop.runMsg` calls `CoreEventDispatch.actorStart` for `RecordSearchStart`. `actorStart` logs `EventBody.ActorStart`, which is `Order.StartActorDeprecated` in `src/Shared/History.fs`. That `runMsg` arm is its only caller.
[x] `CoreMailboxLoop.messageCaller`, `CoreMailboxEvents.operationContext`, and `CoreMailboxEvents.replyFailure` match `RecordSearchStart`.
[x] `CoreMailboxLoop.replyActorOrder` calls `startFunction` and `schedule` before it appends `Order.StartActor`. `admitFunctionStart` can refuse. A refusal posts no row. The reply is the failure.
[x] `tests/Server.Tests/SearchActorTests.fs` calls `recordSearchStart` in ``postSearch records ActorStart at the root``.
[x] ``equals-line ActorStart does not run the query function`` and ``query start error posts no rows, fails the reply, and keeps the edit`` in `tests/Server.Tests/QueryActorTests.fs` post `EventBody.ActorStart` through `CoreMailboxApi.postEvents`.

### Reference

[x] [Arch](../../arch.md). **Query command** names `postSearchRequest`. **Posted run** names `postActorStart`. **Post actor order** names `MailboxHost.postActorOrder`.
[x] [08 — Start the AI Actor](../08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md). The copy from 07 — Start the test command and from that ticket is `whenAdmitted`, `postBuiltOrder`, and `MailboxHost.postActorOrder`.

## Do not load

[ ] `RunAgentActor.fs`.
[ ] `SearchActor.actorStart`. That function builds an `ActorStart`. It is not `CoreEventDispatch.actorStart`.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.
[ ] A new `CoreMsg` case.
[ ] `CoreMsg.RecordSearchStop`. [111 — Place search start as an order, and post SearchStopped](../../../core-refinement/issues/111-place-search-start-as-an-order-and-post-searchstopped.md) removes that case.
[ ] A session id on `FunctionRun`, on `ActorOrder`, or in the `ActorFunction` closure.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change: `SearchActor.functionActorAt` in `src/Server/SearchActor.fs` returns the shared `ActorFunction` in `src/Shared/History.fs`. It takes `runDefinitionId` and `focusId`. The body is the query eval in `SearchActor.functionStart`: `ActorQuery` of those node ids, then `ActorChanges.actorStop`. `SearchActor.functionStart` stays for the pool tests.
3. [x] Change: `postActorStart` calls `whenAdmitted` and a queue function when `SearchActor.isQueryRequest` is true. That queue function copies `queueAdmittedTest`. It builds `ActorOrder` with `ActorOrderFromStart.orderFromStart` and `SearchActor.functionActorAt`, sets `actorName` to `query` and `actorNodeId` to `ActorStart.focusId`, and calls `postBuiltOrder`.
4. [x] Contract: `startChosen` stays `context.pool.startActor`. A query Run does not call `startChosen`. A named start that is not `?test` or `?ai` still calls `startChosen`.
5. [x] Change: `SearchActor.searchActorAt` returns the shared `ActorFunction`. The body calls `ActorChanges.actorStop` with `ActorSucceeded`. `Api.searchReply` still returns `SearchActor.reply`.
6. [x] Change: `recordSearchStart` receives the search `ActorFunction` as a value. It does not name `SearchActor`. It builds the Find and Move order with `ActorOrderFromStart.orderFromStart` and that value. `actorName` is `search`. `actorNodeId` is `ActorStart.focusId`. It calls `MailboxHost.postActorOrder`. `CoreMailboxLoop.replyActorOrder` calls `admitCaller`, then the pool. The pool admits the actor and launches it. `replyActorOrder` appends `Order.StartActor` after that affirmation. The value is `BootActors.search`. Extend `BootActors` in `src/Server/GrokBotSettings.fs` with `search`. `RouteRegistration.CreateBoot` sets `search` to `SearchActor.searchActorAt`. `CoreRuntime.createPublishing` and `CoreActorPool.createWithInstall` already copy that record onto the pool. `RouteRegistration.searchDoor` reads `persistence.Core.pool.bootActors` and passes `search` into `recordSearchStart`. This ticket extends `BootActors`. A sibling field on `CoreBoot` would copy the actor a second time. The `BootActors` literal in `tests/Server.Tests/AskCancelHarness.fs` sets `search`. `searchDoor` returns `Error "search actor is not installed"` when `bootActors` is `None`. A second query on a live actor node asks the pool first. `startFunction` calls `admitFunctionStart`. The refusal is `actor node already has a live Actor`. `replyActorOrder` posts no `Order.StartActor` and no `ActorFailed`. The reply is that failure. The edit stays.
7. [x] Contract: `Api.searchReply` no longer calls `recordStop`. The Search Actor posts that stop.
8. [x] Contract: Delete `CoreMsg.RecordSearchStart`. Delete the `runMsg` arm, the `messageCaller` arm, the `operationContext` arm, and the `replyFailure` arm. Delete `CoreEventDispatch.actorStart` with that `runMsg` arm.
9. [x] Change: ``equals-line ActorStart does not run the query function`` observes `Order.StartActor` and `ActorStop` of `ActorQuery` for an equals line. ``query start error posts no rows, fails the reply, and keeps the edit`` observes the refusal `actor node already has a live Actor` from `admitFunctionStart`, the edited equals line, a failed reply, and no new `Order.StartActor` or `ActorFailed` row. ``postSearch records ActorStart at the root`` observes `Order.StartActor` and `ActorSucceeded` at the root.
10. [x] Do not write `Order.StartActorDeprecated`. Do not add an `Order` case. Leave `Order` as the cases in `src/Shared/History.fs`.

## Outputs

[x] `src/Server/SearchActor.fs`
[x] `src/Server/Core/CoreMailboxActors.fs`
[x] `src/Server/Core/CoreMailboxApi.fs`
[x] `src/Server/Core/CoreMailboxLoop.fs`
[x] `src/Server/Api.fs`
[x] `src/Server/Core/CoreMsg.fs`
[x] `src/Server/Core/CoreMailboxEvents.fs`
[x] `src/Server/Core/CoreEventDispatch.fs` — delete `actorStart`
[x] `src/Server/GrokBotSettings.fs` — `BootActors.search`
[x] `src/Server/RouteRegistration.fs` — `CreateBoot` sets `search`; `searchDoor` passes it
[x] `tests/Server.Tests/AskCancelHarness.fs` — the `BootActors` literal sets `search`
[x] `tests/Server.Tests/QueryActorTests.fs` — ``equals-line ActorStart does not run the query function`` and ``query start error posts no rows, fails the reply, and keeps the edit``
[x] `tests/Server.Tests/SearchActorTests.fs` — ``postSearch records ActorStart at the root``
[x] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs` — `startFunction` stays green, and this file stays unchanged

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/09-dispatch-the-query-command-stop.md`, then ask.
[ ] Stop when this ticket writes `Order.StartActorDeprecated`.
[ ] Stop when Find and Move posts an `ActorOrder` that does not run the Search Actor.
[ ] Stop when a constructor of `CoreMsg.RecordSearchStart` sits outside `CoreMailboxApi.recordSearchStart`.
[ ] Stop when a test file outside Outputs must change.
[ ] Stop when the edit adds a session id.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `QueryActorTests`
  - `tests/Server.Tests`: `CoreActorPoolFunctionStartTests`
  - `tests/Server.Tests`: `SearchActorTests`
[x] All executed tests pass.
[x] Write the verdict to `plan/actor-as-client/reports/09-dispatch-the-query-command-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
