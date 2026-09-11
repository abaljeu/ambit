# 27 — Prove Core Actor lifecycle with TestActor

**Type:** task
**Status:** blocked
**Blocked by:** [[plan/core-creation/issues/17-cancel-a-job.md]]
Actual: 10m

## Context

The rebuilt Actor system needs public-boundary proof of the lifecycle locked by [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. It must not depend on Cursor, CloudAgents, or another Agent transport.

## What to prove

Test through the public Core request member types and universal `{ nodes; events; latestId }` response. Register TestActor through normal composition. Launch dispatch is Command Node text (`?test echo` for the first increment; later cases such as `?test fail`). Focus Header is not the case id. Cases should prove launch ordering, ActorStarted identity, Change ordering, Succeeded, safe Failed, Cancelled, duplicate terminal ignore, ActorFinished-before-drop, and Interrupted restart reconciliation. TestActor may produce controlled Changes and outcomes but does not assert.

- [ ] Tests launch TestActor through the normal Core Actor path.
- [ ] Tests observe Graph and registry outcomes from outside the Actor; TestActor does not assert.
- [ ] Tests prove ActorStarted is durable and the registry exists before Actor output admission.
- [ ] Tests prove Change-before-Cancel applies and Cancel-before-Change rejects.
- [ ] Tests prove exactly one ActorFinished for success, safe failure, cancellation, duplicate completion, and restart interruption.
- [ ] Tests prove synchronous registry and secret removal plus non-blocking task termination.
- [ ] Tests use no live service, key, or Agent transport.

## See also

[[plan/core-creation/issues/02-core-actor-pool.md]], [[plan/core-creation/issues/18-finish-and-drop.md]], [[plan/core-creation/reports/actor-pool-rewind-review.md]]

## Comments

- 2026-09-11 — First proof slice is [[29-prove-testactor-echo.md]] (echo only). This ticket stays the full TestActor proof catalog.
- 2026-09-11 — Launch dispatch is Command Node text (`?test echo`; later `?test fail`). Focus Header is not the case id.

## Time

- 2026-09-11 5m — stub provider-neutral TestActor proof after redesign reconciliation (from chat)
- 2026-09-11 5m — record Command-text TestActor dispatch (from chat)
