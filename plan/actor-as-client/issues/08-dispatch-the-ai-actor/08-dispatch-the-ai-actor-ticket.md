# 08 — Start the AI Actor
**Status**: defined
One job: Run on a node that contains `?ai` posts `Order.StartActor` of `ActorOrder`. `?ai` moves off `startActor`.

[x] The ask-start seam is the only test start for `?ai`. Suites call that seam.
[ ] `postActorStart` for `?ai` admits the caller, then posts `Order.StartActor`. `startActor` still starts the other command names.
[ ] Cancel uses the focus node id. That id is the actor handle and removes the live row. `startAiRun` does not cancel.
[ ] `FunctionRun` in `src/Shared/History.fs` is `getGraph` and `secret`. This ticket leaves that shape.
[ ] The Actor session id stays on `ActorInput` and the pool live row in `src/Server/Core/CoreActorPool.fs`.
[ ] The source files are 3. The test files are 10. The size limit is 8 source files and 4 test files. The test count is over the limit. The ask suites share one seam, and `actorFn` does not compile while those suites still register an `ActorFn`.

Type:

[ ] coding

Blockers:

[ ] [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `RouteRegistration.fs` registers `ActorName "ai"` as `RunAgentActor.actorFn`.
[ ] `RunAgentActor.actorFn` is an `ActorFn`. It reads `input.sessionId` in `src/Server/RunAgentActor.fs`.
[ ] `FunctionRun` is `getGraph` and `secret` in `src/Shared/History.fs`.
[ ] `ActorInput.sessionId` and the live-row `sessionId` are in `src/Server/Core/CoreActorPool.fs`.
[ ] `postActorOrder` is on `MailboxHost`.
[ ] `postActorStart` for `?test` admits the caller, then posts `Order.StartActor`. `queueAdmittedTest` in `src/Server/Core/CoreMailboxActors.fs` builds that order with `TestActor.functionActorAt`.
[ ] `AskCancelHarness.createHost` registers `ActorName "ai"` as `RunAgentActor.actorFn`.
[ ] `startAsk`, `startLiveAsk`, and `startBrowserAsk` call `CoreMailboxApi.startActor`. `launchBrowserAsk` calls `startBrowserAsk`.
[ ] ``second launch on the same Focus is refused`` in `tests/Server.Tests/AgentAskTests.fs` calls `CoreMailboxApi.startActor` for `?ai`.

### Reference

[ ] [Arch](../../arch.md). The closure holds `GrokBotBinding`. `actorFn` is `AiKeySet -> AiRepo list -> GrokBotBinding -> NodeId -> ActorFunction`.

## Do not load

[ ] The git Load and Save names.
[ ] Do not add a `CoreMsg` case.
[ ] This ticket does not add `sessionId` to `FunctionRun`.
[ ] The session id stays on `ActorInput` and the pool live row.
[ ] Do not add a cancel API. `cancelByFocus` already takes the focus node id. `startAiRun` does not cancel.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.

### 1. Ask-start seam

2. [x] Add `startAiRun` in `tests/Server.Tests/AskCancelHarness.fs`. `startAsk` and `startBrowserAsk` call `startAiRun`. `startLiveAsk` calls `startAsk`. `launchBrowserAsk` calls `startBrowserAsk`.
3. [x] The first `startAiRun` calls `CoreMailboxApi.startActor`. `createHost` still registers `ActorName "ai"` as `RunAgentActor.actorFn`.
4. [x] ``second launch on the same Focus is refused`` calls `startAiRun` for both starts.
5. [x] A suite calls `startAsk`, `startLiveAsk`, or `launchBrowserAsk`. It does not call `CoreMailboxApi.startActor` for `?ai`. A probe `ActorFn` stays on `startActor`.

### 2. ActorOrder for ?ai

6. [ ] Change: `actorFn` is `AiKeySet -> AiRepo list -> GrokBotBinding -> NodeId -> ActorFunction`. The closure holds `GrokBotBinding`.
7. [ ] Change: `postActorStart` for a `?ai` Run closes over the actor node id from that Run in the `ActorFunction`. It admits the caller, builds `ActorOrder` with that function, `actorNodeId`, and `actorName`, and calls `postActorOrder`. `ActorOrder` in `src/Shared/History.fs` is `actor`, `actorNodeId`, and `actorName`.
8. [ ] Contract: The `?ai` path does not call `startActor` for `ai`. `startActor` still starts the other command names. `CoreMsg` in `src/Server/Core/CoreMsg.fs` is `StartPeerActor`, `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `ActorStop`, and `PostEvent`.
9. [ ] Contract: Remove the `ai` pair from `pool.register` in `RouteRegistration.fs` and from `createHost`.
10. [ ] Change: `startAiRun` posts `Order.StartActor`. The actor is `RunAgentActor.actorFn` applied to the harness keys, repos, `GrokBotBinding`, and that Run's actor node id.
11. [ ] Do not write `Order.StartActorDeprecated`. Do not look up the function from `actorName`. Leave `Order` as the cases in `src/Shared/History.fs`. Leave `FunctionRun` as `getGraph` and `secret`.

## Outputs

[ ] `src/Server/Core/CoreMailboxActors.fs` — `?ai` posts `Order.StartActor` after `admitCaller`
[ ] `src/Server/RunAgentActor.fs` — `actorFn` returns `ActorFunction`
[ ] `src/Server/RouteRegistration.fs` — remove `ai` from `pool.register`
[ ] `tests/Server.Tests/AskCancelHarness.fs` — `startAiRun`
[ ] `tests/Server.Tests/AgentAskTests.fs` — the second-launch fact calls `startAiRun`
[ ] `tests/Server.Tests/AgentActorStreamTests.fs` — calls the ask-start seam
[ ] `tests/Server.Tests/AgentAuthErrorTests.fs` — calls the ask-start seam
[ ] `tests/Server.Tests/AgentFailurePreserveTests.fs` — calls the ask-start seam
[ ] `tests/Server.Tests/AgentGrokBotStreamTests.fs` — calls the ask-start seam
[ ] `tests/Server.Tests/AiKeysTests.fs` — calls the ask-start seam
[ ] `tests/Server.Tests/AiReposTests.fs` — calls the ask-start seam
[ ] `tests/Server.Tests/BrowserAskProofTests.fs` — calls the ask-start seam
[ ] `tests/Server.Tests/CancelByFocusTests.fs` — starts through the ask-start seam, then `cancelByFocus` with the focus node id. That cancel is not `startAiRun`.

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/08-dispatch-the-ai-actor-stop.md`, then ask.
[ ] Stop when the edit adds `sessionId` to `FunctionRun`.
[ ] Stop when the edit moves the session id off `ActorInput` or the pool live row.
[ ] Stop when the edit adds a route, a handler, `ActorDispatch.fs`, or a command-name map.
[ ] Stop when a test file outside Outputs must change.
[ ] Stop when a suite chooses `ActorFn` or `ActorFunction` for `?ai` instead of `startAiRun`.
[ ] Stop when `startAiRun` cancels.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `AgentAskTests`
  - `tests/Server.Tests`: `AgentActorStreamTests`
  - `tests/Server.Tests`: `AgentAuthErrorTests`
  - `tests/Server.Tests`: `AgentFailurePreserveTests`
  - `tests/Server.Tests`: `AgentGrokBotStreamTests`
  - `tests/Server.Tests`: `AiKeysTests`
  - `tests/Server.Tests`: `AiReposTests`
  - `tests/Server.Tests`: `BrowserAskProofTests`
  - `tests/Server.Tests`: `CancelByFocusTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/08-dispatch-the-ai-actor-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
