pass

## Check

Ticket: [08 — Start the AI Actor](../issues/08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md)

[x] `scripts/agent-test.sh` with no arguments passed. Mapped runs: `CoreActorPoolTests` 1 passed, and `EventJsonTests|HistoryTests|MailboxLogTests` 86 passed.

[x] Ask suites that share `startAiRun` passed 76 tests: `AgentAskTests`, `AgentActorStreamTests`, `AgentAuthErrorTests`, `AgentFailurePreserveTests`, `AgentGrokBotStreamTests`, `AiKeysTests`, `AiReposTests`, `BrowserAskProofTests`, `CancelByFocusTests`, plus `CoreActorPoolFunctionStartTests`, `TestActorHelloTests`, and `TestActorCommandErrorTests`.

## Result

[x] `FunctionRun.sessionId` is the id `putFunctionStart` minted. `runSchedule` copies `row.sessionId`. The Grok oneshot POST reads `run.sessionId`.

[x] `postActorStart` for `?ai` admits the caller, then posts `ActorOrder`. `startActor` still starts the other command names.

[x] `pool.register` and `createHost` no longer register `ai`. `startAiRun` applies `RunAgentActor.actorFn` to the harness keys, repos, `GrokBotBinding`, and that Run's actor node id.

[x] Cancel stays `cancelByFocus` with the focus node id. `startAiRun` does not cancel.
