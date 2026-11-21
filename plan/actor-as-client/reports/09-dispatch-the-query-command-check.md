pass

## Check

Ticket: [09 — Start the query command](../issues/09-dispatch-the-query-command/09-dispatch-the-query-command-ticket.md)

[x] `scripts/agent-test.sh` with no arguments passed 23 tests. The filter was `GrokBotSettingsTests`, `QueryActorTests`, and `SearchActorTests`. Failed 0. Skipped 0.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~QueryActorTests|FullyQualifiedName~SearchActorTests|FullyQualifiedName~CoreActorPoolFunctionStartTests server` passed 20 tests. Failed 0. Skipped 0.

[x] `scripts/agent-test.sh --full` ran both suites. `tests/Server.Tests` passed 763, failed 0, skipped 1. `tests/Shared.Tests` passed 1937, failed 0, skipped 3.
