pass

## Check

Ticket: [08 — Start the AI Actor](../issues/08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md)

[x] `scripts/agent-test.sh` with no arguments passed. Mapped runs: `BrowserAskProofTests|CoreActorPoolTests` 4 passed, and `EventJsonTests|HistoryTests|MailboxLogTests|RunEditCommitTests` 95 passed.

[x] Ask, cancel, function-start, and test-command filters passed 78 tests: `AgentAskTests`, `AgentActorStreamTests`, `AgentAuthErrorTests`, `AgentFailurePreserveTests`, `AgentGrokBotStreamTests`, `AiKeysTests`, `AiReposTests`, `BrowserAskProofTests`, `CancelByFocusTests`, plus `CoreActorPoolFunctionStartTests`, `TestActorHelloTests`, and `TestActorCommandErrorTests`.

## Result

[x] `FunctionRun.sessionId` is the id `putFunctionStart` minted. `runSchedule` copies `row.sessionId`. The Grok oneshot POST reads `run.sessionId`.

[x] A `?ai` Run is an `ActorOrder`. The server admits the caller, then posts that order. An `ActorStart` event for `?ai` is refused and does not store `ActorStart`. `startActor` still starts the other command names.

[x] `pool.register` and `createHost` no longer register `ai`. `startAiRun` applies `RunAgentActor.actorFn` to the harness keys, repos, `GrokBotBinding`, and that Run's actor node id, then posts `ActorOrder`.

[x] Cancel stays `cancelByFocus` with the focus node id. `startAiRun` does not cancel.
