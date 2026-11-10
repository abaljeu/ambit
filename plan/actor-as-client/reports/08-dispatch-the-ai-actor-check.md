pass

## Check

Ticket: [08 — Start the AI Actor](../issues/08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md)

[x] `scripts/agent-test.sh` with no arguments passed 8 tests. The filter was `AgentAskTests`, `BrowserAskProofTests`, and `CoreActorPoolTests`.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~AgentAskTests server` passed 6 tests.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~AgentActorStreamTests server` passed 3 tests.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~AgentAuthErrorTests server` passed 2 tests.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~AgentFailurePreserveTests server` passed 1 test.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~AgentGrokBotStreamTests server` passed 8 tests.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~AiKeysTests server` passed 13 tests.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~AiReposTests server` passed 10 tests.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~BrowserAskProofTests server` passed 1 test.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~CancelByFocusTests server` passed 9 tests.

[x] The same names on `tests/Shared.Tests` matched 0 tests.

## Result

[x] A Run whose command text selects `ai` queues `Order.StartActor` from `postActorStart` through `MailboxHost.postActorOrder`.

[x] `startAiRun` posts that same order. `actorNodeId` is `ActorStart.focusId`.

[x] `RunAgentActor.actorFn` is `AiInstall -> (Credential -> string option) -> ActorStart -> ActorFunction`. `AiInstall` is `keys`, `repos`, and `grok`. `ActorFunction` is the one in `src/Shared/History.fs`.

[x] The wake reads the pool live row through `sessionIdFor`. A miss stops with `ActorFailed "not live"`. `FunctionRun` stays `getGraph` and `secret`.

[x] `scripts/agent-test.sh --full` passed Server 759, skipped 1, and Shared 1937, skipped 3. Failed 0.

[x] ``second launch on the same Focus is refused`` sees `actor node already has a live Actor` from `admitFunctionStart`.
