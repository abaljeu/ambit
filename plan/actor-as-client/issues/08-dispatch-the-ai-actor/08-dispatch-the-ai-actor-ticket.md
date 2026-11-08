# 08 — Start the AI Actor
**Status**: cancelled

#545 was squash-merged as `c1413b80` on `cursor/remove-order-peer-and-loadsave-ff54` and then cancelled. This ticket does not move `?ai` off `ActorStart`.
One job: Run on a node that contains `?ai` moves off `CoreMsg.StartActor` onto `Order.ActorOrder`. `actorFn` returns `ActorFunction`. `FunctionRun.sessionId` is the pool-minted Actor session id.

[x] The ask-start seam is the only test start for `?ai`. Suites call that seam.
[ ] `runSchedule` fills `FunctionRun.sessionId` from the live row. `putFunctionStart` minted that id.
[ ] `postActorStart` for `?ai` admits the caller, then posts `ActorOrder`. `startActor` still starts the other command names.
[ ] Cancel uses the focus node id. That id is the actor handle and removes the live row. `startAiRun` does not cancel. `FunctionRun.sessionId` is for the Grok oneshot POST and for `deliver` and `takeInbox`.
[ ] The source files are 5. The test files are 10. The size limit is 8 source files and 4 test files. The test count is over the limit. The ask suites share one seam, and `actorFn` does not compile while those suites still register an `ActorFn`.

Type:

[ ] coding

Blockers:

[ ] [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `RouteRegistration.fs` registers `ActorName "ai"` as `RunAgentActor.actorFn`.
[ ] `RunAgentActor.actorFn` is an `ActorFn`. It reads `input.sessionId` and passes that string to `GrokBotRunner.wake` (library name) and to `requestGrokCancel`.
[ ] `FunctionRun` is `getGraph` and `secret` in `src/Shared/History.fs`.
[ ] `putFunctionStart` mints the Actor `sessionId` with `Guid.NewGuid` and stores it on the live row.
[ ] `runSchedule` for `FunctionPending` builds `FunctionRun` from `getGraph` and `secret`. It does not copy `row.sessionId`.
[ ] `deliver` and `takeInbox` key the live row by that Actor session id.
[ ] `postActorOrder` is on `MailboxHost`.
[ ] `postActorStart` for `?test` admits the caller, then posts `ActorOrder`.
[ ] `AskCancelHarness.createHost` registers `ActorName "ai"` as `RunAgentActor.actorFn`.
[ ] `startAsk`, `startLiveAsk`, and `startBrowserAsk` call `CoreMailboxApi.startActor`. `launchBrowserAsk` calls `startBrowserAsk`.
[ ] ``second launch on the same Focus is refused`` in `tests/Server.Tests/AgentAskTests.fs` calls `CoreMailboxApi.startActor` for `?ai`.

### Reference

[ ] [Arch](../../arch.md). The pool mints the Actor session id. The closure holds `GrokBotBinding`. `actorFn` is `AiKeySet -> AiRepo list -> GrokBotBinding -> NodeId -> ActorFunction`.

## Do not load

[ ] The git Load and Save names.
[ ] Do not add a `CoreMsg` case.
[ ] Do not mint the Actor session id in `RunAgentActor`.
[ ] Do not store the Actor session id in the `GrokBotBinding` closure or the curried `NodeId` closure.
[ ] Do not route cancel through `startAiRun` or through `FunctionRun.sessionId`.
[ ] Do not add a cancel API. `cancelByFocus` already takes the focus node id.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.

### 1. Ask-start seam

2. [x] Add `startAiRun` in `tests/Server.Tests/AskCancelHarness.fs`. `startAsk` and `startBrowserAsk` call `startAiRun`. `startLiveAsk` calls `startAsk`. `launchBrowserAsk` calls `startBrowserAsk`.
3. [x] The first `startAiRun` calls `CoreMailboxApi.startActor`. `createHost` still registers `ActorName "ai"` as `RunAgentActor.actorFn`.
4. [x] ``second launch on the same Focus is refused`` calls `startAiRun` for both starts.
5. [x] A suite calls `startAsk`, `startLiveAsk`, or `launchBrowserAsk`. It does not call `CoreMailboxApi.startActor` for `?ai`. A probe `ActorFn` stays on `startActor`.

### 2. Session id on FunctionRun

6. [ ] Add `sessionId` to `FunctionRun` in `src/Shared/History.fs`.
7. [ ] `runSchedule` for `FunctionPending` sets `FunctionRun.sessionId` from `row.sessionId`. `putFunctionStart` still mints that id. `deliver` and `takeInbox` keep that id.
8. [ ] The Grok oneshot POST reads `run.sessionId`. `deliver` and `takeInbox` keep that id. The library call is `GrokBotRunner.wake`.
9. [ ] Contract: Cancel stays `cancelByFocus`. The focus node id is the actor handle. That id removes the live row. `startAiRun` does not cancel. `FunctionRun.sessionId` is not the cancel handle.

### 3. ActorOrder for ?ai

10. [ ] Change: `actorFn` is `AiKeySet -> AiRepo list -> GrokBotBinding -> NodeId -> ActorFunction`. The closure holds `GrokBotBinding`.
11. [ ] Change: `postActorStart` for a `?ai` Run closes over the actor node id from that Run in the `ActorFunction`. It admits the caller, builds `ActorOrder` with that function, the FocusId, and `actorName`, and calls `postActorOrder`.
12. [ ] Contract: The `?ai` path does not post `CoreMsg.StartActor` and does not call `startActor` for `ai`. `startActor` still starts the other command names.
13. [ ] Contract: Remove the `ai` pair from `pool.register` in `RouteRegistration.fs` and from `createHost`.
14. [ ] Change: `startAiRun` posts `ActorOrder`. The actor is `RunAgentActor.actorFn` applied to the harness keys, repos, `GrokBotBinding`, and that Run's actor node id.
15. [ ] Do not write `Order.StartActor` or `Order.RunActor`. Do not look up the function from `actorName`.

## Outputs

[ ] `src/Shared/History.fs` — `FunctionRun.sessionId`
[ ] `src/Server/Core/CoreActorPool.fs` — `runSchedule` copies `row.sessionId`
[ ] `src/Server/Core/CoreMailboxActors.fs` — `?ai` posts `ActorOrder` after `admitCaller`
[ ] `src/Server/RunAgentActor.fs` — `actorFn` returns `ActorFunction`. The Grok oneshot POST reads `run.sessionId`.
[ ] `src/Server/RouteRegistration.fs` — remove `ai` from `pool.register`
[ ] `tests/Server.Tests/AskCancelHarness.fs` — `startAiRun`
[ ] `tests/Server.Tests/AgentAskTests.fs` — the second-launch fact calls `startAiRun`
[ ] `tests/Server.Tests/AgentActorStreamTests.fs` — calls the ask-start seam
[ ] `tests/Server.Tests/AgentAuthErrorTests.fs` — calls the ask-start seam
[ ] `tests/Server.Tests/AgentFailurePreserveTests.fs` — calls the ask-start seam
[ ] `tests/Server.Tests/AgentGrokBotStreamTests.fs` — calls the ask-start seam. ``inbound deliver stays live for a second chunk`` delivers with the `SessionId` from the Grok oneshot POST.
[ ] `tests/Server.Tests/AiKeysTests.fs` — calls the ask-start seam
[ ] `tests/Server.Tests/AiReposTests.fs` — calls the ask-start seam
[ ] `tests/Server.Tests/BrowserAskProofTests.fs` — calls the ask-start seam
[ ] `tests/Server.Tests/CancelByFocusTests.fs` — starts through the ask-start seam, then `cancelByFocus` with the focus node id. That cancel is not `startAiRun` and does not use `FunctionRun.sessionId`.

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/08-dispatch-the-ai-actor-stop.md`, then ask.
[ ] Stop when the edit mints the Actor session id outside the pool.
[ ] Stop when the edit stores the Actor session id in the actor closure or the curried `NodeId` closure.
[ ] Stop when the edit adds a route, a handler, `ActorDispatch.fs`, or a command-name map.
[ ] Stop when a test file outside Outputs must change.
[ ] Stop when a suite chooses `ActorFn` or `ActorFunction` for `?ai` instead of `startAiRun`.
[ ] Stop when cancel uses the Actor session id, or when `startAiRun` cancels.

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
