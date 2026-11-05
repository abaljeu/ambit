# 08 — Start the AI Actor stop

Ticket: [08 — Start the AI Actor](../issues/08-dispatch-the-ai-actor/08-dispatch-the-ai-actor-ticket.md)

## Superseded

[x] Alan, 2026-11-05. Do not lift the old stops. Rewrite the ticket.

[x] The ticket now has three slices. The ask-start seam is `startAiRun`. `FunctionRun.sessionId` comes from the live row. The `?ai` Run then posts `Order.ActorOrder`.

[x] Outputs name `src/Shared/History.fs`, `src/Server/Core/CoreActorPool.fs`, `tests/Server.Tests/AskCancelHarness.fs`, and the ask suites that call the seam.

[x] The old stop "a test file outside Outputs must change" is superseded for those named files.

[x] The old stop "do not add the Actor session id to `FunctionRun`" is superseded. Minting outside the pool stays a stop. Storing the id in the actor closure stays a stop.

## Build

[ ] Product code is not started.

[ ] `scripts/agent-test.sh` did not run.

[ ] `plan/actor-as-client/reports/08-dispatch-the-ai-actor-check.md` was not written.
