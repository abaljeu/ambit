# 08 — Start the AI Actor stop

Ticket: [08 — Start the AI Actor](../issues/08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md)

## Stop

[ ] The build stops before any source edit and before any commit of source.

[ ] Stop when a test file outside Outputs must change.

[ ] Stop when the edit mints the Actor session id outside the pool, or stores it in the closure.

[ ] Continue when a human names the extra files, or names how wake and cancel read the id `putFunctionStart` already stored.

## Why the test file must change

[ ] Process step 2 sets `actorFn` to `AiKeySet -> AiRepo list -> GrokBotBinding -> NodeId -> ActorFunction`.

[ ] `pool.register` takes `ActorFn`. `ActorFn` is `ActorInput -> CoreChangesDoor -> Async<unit>`.

[ ] `tests/Server.Tests/AskCancelHarness.fs` registers `ActorName "ai"` with `RunAgentActor.actorFn`. That call is the only test caller.

[ ] That file is outside Outputs. A compiling tree after the signature change edits it.

[ ] `startAsk` and `startLiveAsk` in that same file call `CoreMailboxApi.startActor`. These tests call them:

- `tests/Server.Tests/AgentActorStreamTests.fs`
- `tests/Server.Tests/AgentAuthErrorTests.fs`
- `tests/Server.Tests/AgentFailurePreserveTests.fs`
- `tests/Server.Tests/AgentGrokBotStreamTests.fs`
- `tests/Server.Tests/AiKeysTests.fs`
- `tests/Server.Tests/AiReposTests.fs`
- `tests/Server.Tests/BrowserAskProofTests.fs`
- `tests/Server.Tests/CancelByFocusTests.fs`

[ ] Outputs name `tests/Server.Tests/AgentAskTests.fs` only.

## Why the session id has no legal path

[ ] `putFunctionStart` mints `sessionId` with `Guid.NewGuid` and stores it on the live row. `deliver` and `takeInbox` key that row by that id.

[ ] `FunctionRun` holds `getGraph` and `secret`. It has no `sessionId`.

[ ] `completeGrok` reads `input.sessionId` and passes that string to the Grok wake and to `requestGrokCancel`.

[ ] The order actor receives `FunctionRun` and `ActorChanges`. The minted id is not an argument.

[ ] A closure value would store the id. That is a Stop when on this ticket.

[ ] A new id in `RunAgentActor` would mint outside the pool. That is a Stop when on this ticket.

[ ] A read of the live row is `src/Server/Core/CoreActorPool.fs`. That file is outside Outputs.

[ ] ``inbound deliver stays live for a second chunk`` in `tests/Server.Tests/AgentGrokBotStreamTests.fs` delivers to `pool.deliver` with the wake `SessionId`. That id must be the pool id.

## Inputs that match

[ ] `RouteRegistration.fs` registers `ActorName "ai"` as `RunAgentActor.actorFn`.

[ ] `RunAgentActor.actorFn` is an `ActorFn`. It reads `input.sessionId`.

[ ] `putFunctionStart` mints the Actor session id.

[ ] `postActorOrder` is on `MailboxHost`.

[ ] `postActorStart` for `?test` admits the caller, then posts `ActorOrder`. `startActor` still starts the other names.

## Check

[ ] Check did not run. `scripts/agent-test.sh` did not run.

[ ] `plan/actor-as-client/reports/08-dispatch-the-ai-actor-check.md` was not written.
