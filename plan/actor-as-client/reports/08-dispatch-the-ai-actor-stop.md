# 08 — Start the AI Actor stop

Ticket: [08 — Start the AI Actor](../issues/08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md)

## Superseded

[x] Alan, 2026-11-05. Do not lift the old stops. Rewrite the ticket.

[x] The ticket now has three slices. The ask-start seam is `startAiRun`. `FunctionRun.sessionId` comes from the live row. The `?ai` Run then posts `Order.ActorOrder`.

[x] Outputs name `src/Shared/History.fs`, `src/Server/Core/CoreActorPool.fs`, `tests/Server.Tests/AskCancelHarness.fs`, and the ask suites that call the seam.

[x] The old stop "a test file outside Outputs must change" is superseded for those named files.

[x] The old stop "do not add the Actor session id to `FunctionRun`" is superseded. Minting outside the pool stays a stop. Storing the id in the actor closure stays a stop.

[x] Alan, 2026-11-05. Cancel uses the focus node id. That id removes the live row. Cancel is not `startAiRun` and not `FunctionRun.sessionId`. The session id stays for the Grok oneshot POST, `deliver`, and `takeInbox`.

## Build

[x] The ticket rewrite names `startAiRun`, `CoreMailboxApi.coreMailboxStartActor`, and `CoreMailboxApi.coreMailboxStartActorFromFunction`.

[ ] Product code is not started.

[ ] `scripts/agent-test.sh` did not run.

[ ] `plan/actor-as-client/reports/08-dispatch-the-ai-actor-check.md` was not written.

## 2026-11-10 input mismatch

Process step 1: an input is missing or does not match the ticket. The edit did not start. Status stays `defined`.

### Working inputs that differ

[ ] Ticket: `startAsk`, `startLiveAsk`, and `startBrowserAsk` call `CoreMailboxApi.startActor`. `launchBrowserAsk` calls `startBrowserAsk`.
[ ] Code: `startAsk` and `startBrowserAsk` call `startAiRun` in `tests/Server.Tests/AskCancelHarness.fs`. `startLiveAsk` calls `startAsk`. `launchBrowserAsk` calls `startBrowserAsk`.
[ ] Code: `startAiRun` calls `CoreMailboxApi.coreMailboxStartActor`. `CoreMailboxApi` has `coreMailboxStartActor` and `coreMailboxStartActorFromFunction`. It has no `startActor`.
[ ] Ticket: ``second launch on the same Focus is refused`` in `tests/Server.Tests/AgentAskTests.fs` calls `CoreMailboxApi.startActor` for `?ai`.
[ ] Code: that fact calls `startAiRun` for both starts.

### Checked process line that names the same missing symbol

[ ] Process step 3 is marked done and says the first `startAiRun` calls `CoreMailboxApi.startActor`.
[ ] `startAiRun` calls `CoreMailboxApi.coreMailboxStartActor`.

### Working inputs that match

[ ] `RouteRegistration.CreateBoot` registers `ActorName "ai"` as `RunAgentActor.actorFn`.
[ ] `RunAgentActor.actorFn` is an `ActorFn` and reads `input.sessionId` in `src/Server/RunAgentActor.fs`.
[ ] `FunctionRun` in `src/Shared/History.fs` is `getGraph` and `secret`.
[ ] `ActorInput.sessionId` and the live-row `sessionId` are in `src/Server/Core/CoreActorPool.fs`.
[ ] `postActorOrder` is on `MailboxHost`.
[ ] `postActorStart` for `?test` admits the caller, then posts `Order.StartActor`. `queueAdmittedTest` builds that order with `TestActor.functionActorAt`.
[ ] `createHost` in `tests/Server.Tests/AskCancelHarness.fs` registers `ActorName "ai"` as `RunAgentActor.actorFn`.

### Contract line that differs

[ ] Process step 8 says `CoreMsg` in `src/Server/Core/CoreMsg.fs` is `StartPeerActor`, `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `ActorStop`, and `PostEvent`.
[ ] `CoreMsg` cases are `StartLoadSaveCommand`, `RecordSearchStart`, `RecordSearchStop`, `ActorStop`, and `PostEvent`.
[ ] Do not load says do not add a `CoreMsg` case.

### Ask

[x] The `?ai` test start is `startAiRun` in `tests/Server.Tests/AskCancelHarness.fs`. That seam calls `CoreMailboxApi.coreMailboxStartActor`. This ticket moves `?ai` off that call.
[x] Process drops `CoreMsg.StartPeerActor`. [15 — Delete the old Actor starts](../issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md) already deleted that case.
