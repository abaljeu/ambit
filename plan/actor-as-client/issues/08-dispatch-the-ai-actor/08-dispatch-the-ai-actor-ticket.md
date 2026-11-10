# 08 — Start the AI Actor
**Status**: coded
One job: Run on a node that contains `?ai` posts `Order.StartActor` of `ActorOrder`. `?ai` moves off `startAiRun` -> `CoreMailboxApi.coreMailboxStartActor`.

[x] The ask-start seam is `startAiRun` in `tests/Server.Tests/AskCancelHarness.fs`. Suites call that seam.
[x] `postActorStart` for `?ai` admits the caller, then posts `Order.StartActor`. `CoreActorPool.startActor` still starts the other command names.
[x] Cancel uses the focus node id. That id is the actor handle and removes the live row. `startAiRun` does not cancel.
[x] `FunctionRun` in `src/Shared/History.fs` is `getGraph` and `secret`. This ticket leaves that shape.
[x] The Actor session id stays on `ActorInput` and the pool live row in `src/Server/Core/CoreActorPool.fs`.
[x] The source files are 4. The test files are 10. The size limit is 8 source files and 4 test files. The test count is over the limit. The ask suites share `startAiRun`.

Type:

[x] coding

Blockers:

[x] [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[x] `RouteRegistration.CreateBoot` in `src/Server/RouteRegistration.fs` calls `RunAgentActor.installActorBinding`. `Actors` is empty.
[x] `RunAgentActor.actorFn` in `src/Server/RunAgentActor.fs` is `AiKeySet -> AiRepo list -> GrokBotBinding -> ActorStart -> ActorFunction`. `ActorFunction` is the one in `src/Shared/History.fs`. `completeGrok` reads `ActorInput.sessionId`. That value is the live-row id from `CoreActorPool.sessionIdFor`.
[x] `FunctionRun` in `src/Shared/History.fs` is `getGraph` and `secret`.
[x] `ActorInput.sessionId` and the live-row `sessionId` are in `src/Server/Core/CoreActorPool.fs`. `putActorStart` and `putFunctionStart` mint that id.
[x] `MailboxHost.postActorOrder` in `src/Server/Core/MailboxHost.fs` places `Order.StartActor`.
[x] `postActorStart` for `?test` calls `queueTestOrder`. `queueAdmittedTest` in `src/Server/Core/CoreMailboxActors.fs` admits through `admitCaller`, builds `ActorOrder` with `TestActor.functionActorAt`, and calls `MailboxHost.postActorOrder`.
[x] `AskCancelHarness.createHost` keeps the host's keys, repos, and `GrokBotBinding` for `startAiRun`. It does not register `ActorName "ai"`.
[x] `startAsk` and `startBrowserAsk` call `startAiRun`. `startLiveAsk` calls `startAsk`. `launchBrowserAsk` calls `startBrowserAsk`. `startAiRun` calls `MailboxHost.postActorOrder`.
[x] ``second launch on the same Focus is refused`` in `tests/Server.Tests/AgentAskTests.fs` calls `startAiRun` for both starts.
[x] `CoreMailboxApi` in `src/Server/Core/CoreMailboxApi.fs` has `coreMailboxStartActor` and `coreMailboxStartActorFromFunction`.
[x] `ActorOrder` in `src/Shared/History.fs` is `actor`, `actorNodeId`, and `actorName`. `ActorFunction` there is `FunctionRun -> ActorChanges -> Async<unit>`.
[x] `CoreMailboxLoop.scheduleActorOrder` in `src/Server/Core/CoreMailboxLoop.fs` calls `startFunction` and runs `order.actor`.
[x] `CoreMailboxApi.cancelByFocus` takes the focus node id.

### Reference

[x] [Arch](../../arch.md). The closure holds `GrokBotBinding`. Curried actor is `NodeId -> ActorFunction`.
[x] [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md). The copy-from is `queueAdmittedTest`, `TestActor.functionActorAt`, `MailboxHost.postActorOrder`, and `admitCaller`.

## Do not load

[ ] The git Load and Save names.
[ ] Do not add a `CoreMsg` case. [15 — Delete the old Actor starts](../15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md) deleted `CoreMsg.StartPeerActor`.
[ ] This ticket does not add `sessionId` to `FunctionRun`.
[ ] The session id stays on `ActorInput` and the pool live row. `putFunctionStart` already mints the live-row id.
[ ] Do not add a cancel API. `cancelByFocus` already takes the focus node id. `startAiRun` does not cancel.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.

### 1. Ask-start seam

2. [x] `startAiRun` is in `tests/Server.Tests/AskCancelHarness.fs`. `startAsk` and `startBrowserAsk` call `startAiRun`. `startLiveAsk` calls `startAsk`. `launchBrowserAsk` calls `startBrowserAsk`.
3. [x] `startAiRun` posts `Order.StartActor`. `createHost` stores that host's keys, repos, and `GrokBotBinding` for the call.
4. [x] ``second launch on the same Focus is refused`` calls `startAiRun` for both starts.
5. [x] A suite calls `startAsk`, `startLiveAsk`, or `launchBrowserAsk`. It does not call `CoreMailboxApi.coreMailboxStartActor` for `?ai`. A probe `ActorFn` stays on `CoreMailboxApi.coreMailboxStartActor`.

### 2. ActorOrder for ?ai

6. [x] Change: `RunAgentActor.actorFn` is `AiKeySet -> AiRepo list -> GrokBotBinding -> ActorStart -> ActorFunction`. `ActorFunction` is the one in `src/Shared/History.fs`. The closure holds `GrokBotBinding`. The body reads `ActorStart.zoomId`, `ActorStart.focusId`, and `ActorStart.runDefinitionId`.
7. [x] Change: the wake reads the live-row `sessionId` for `FunctionRun.secret` through `CoreActorPool.sessionIdFor`. That read uses the row `putFunctionStart` already stores. The id stays off `FunctionRun` and off the `ActorFunction` closure.
8. [x] Change: `postActorStart` for a `?ai` Run follows `queueAdmittedTest`. It admits the caller, builds `ActorOrder` with `RunAgentActor.actorFn` applied to the installed keys, repos, `GrokBotBinding`, and that `ActorStart`, sets `actorNodeId` to `ActorStart.focusId` and `actorName` to `ai`, and calls `MailboxHost.postActorOrder`.
9. [x] Contract: the `?ai` path does not call `CoreMailboxApi.coreMailboxStartActor` or `CoreActorPool.startActor`. `CoreActorPool.startActor` still starts the other command names.
10. [x] Contract: `RouteRegistration.CreateBoot` calls `RunAgentActor.installActorBinding` with `AiKeys.fromConfig`, `AiRepos.fromConfig`, and `GrokBotSettings.fromConfigBinding`. Remove the `ai` pair from `pool.register` there and from `AskCancelHarness.createHost`.
11. [x] Change: `startAiRun` posts `Order.StartActor` through `MailboxHost.postActorOrder`. The actor is `RunAgentActor.actorFn` applied to that host's keys, repos, `GrokBotBinding`, and that `ActorStart`. `actorNodeId` is `ActorStart.focusId`.
12. [x] Do not write `Order.StartActorDeprecated` on this path. Do not look up the function from `actorName`. Leave `Order` as the cases in `src/Shared/History.fs`. Leave `FunctionRun` as `getGraph` and `secret`.
13. [x] ``second launch on the same Focus is refused`` still calls `startAiRun` twice. The refusal text is `admitFunctionStart` in `src/Server/Core/CoreActorPool.fs`: `actor node already has a live Actor`.

## Outputs

[x] `src/Server/Core/CoreMailboxActors.fs` — `?ai` posts `Order.StartActor` after `admitCaller`
[x] `src/Server/RunAgentActor.fs` — `actorFn` returns `ActorFunction`
[x] `src/Server/RouteRegistration.fs` — `installActorBinding`; remove `ai` from `pool.register`
[x] `src/Server/Core/CoreActorPool.fs` — `sessionIdFor` reads the live-row `sessionId`
[x] `tests/Server.Tests/AskCancelHarness.fs` — `startAiRun` posts `Order.StartActor`
[x] `tests/Server.Tests/AgentAskTests.fs` — the second-launch fact calls `startAiRun`
[x] `tests/Server.Tests/AgentActorStreamTests.fs` — calls `startAiRun` through `startAsk` or `startLiveAsk`
[x] `tests/Server.Tests/AgentAuthErrorTests.fs` — calls `startAiRun` through `startAsk`
[x] `tests/Server.Tests/AgentFailurePreserveTests.fs` — calls `startAiRun` through `startAsk`
[x] `tests/Server.Tests/AgentGrokBotStreamTests.fs` — calls `startAiRun` through `startAsk` or `startLiveAsk`
[x] `tests/Server.Tests/AiKeysTests.fs` — calls `startAiRun` through `startAsk`
[x] `tests/Server.Tests/AiReposTests.fs` — calls `startAiRun` through `startAsk`
[x] `tests/Server.Tests/BrowserAskProofTests.fs` — calls `startAiRun` through `launchBrowserAsk`
[x] `tests/Server.Tests/CancelByFocusTests.fs` — starts through `startAiRun`, then `cancelByFocus` with the focus node id. That cancel is not `startAiRun`.

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/08-dispatch-the-ai-actor-stop.md`, then ask.
[ ] Stop when the edit adds `sessionId` to `FunctionRun`.
[ ] Stop when the edit moves the session id off `ActorInput` or the pool live row.
[ ] Stop when the edit stores the session id in the `ActorFunction` closure.
[ ] Stop when the edit adds a route, a handler, `ActorDispatch.fs`, or a command-name map.
[ ] Stop when a test file outside Outputs must change.
[ ] Stop when a suite chooses `ActorFn` or `ActorFunction` for `?ai` instead of `startAiRun`.
[ ] Stop when `startAiRun` cancels.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `AgentAskTests`
  - `tests/Server.Tests`: `AgentActorStreamTests`
  - `tests/Server.Tests`: `AgentAuthErrorTests`
  - `tests/Server.Tests`: `AgentFailurePreserveTests`
  - `tests/Server.Tests`: `AgentGrokBotStreamTests`
  - `tests/Server.Tests`: `AiKeysTests`
  - `tests/Server.Tests`: `AiReposTests`
  - `tests/Server.Tests`: `BrowserAskProofTests`
  - `tests/Server.Tests`: `CancelByFocusTests`
[x] All executed tests pass.
[x] Write the verdict to `plan/actor-as-client/reports/08-dispatch-the-ai-actor-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
