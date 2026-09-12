# 27 — Prove Core Actor lifecycle with TestActor

**Type:** task
**Status:** blocked
**Blocked by:** [[plan/core-creation/issues/17-cancel-a-job.md]]
Actual: 15m

## Context

The rebuilt Actor system needs public-boundary proof of the lifecycle locked by [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. It must not depend on Cursor, CloudAgents, or another Agent transport.

## What to prove

Prove the rebuilt lifecycle through the public Core path with TestActor and no Agent transport. Dispatch is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. First increment is [[29-prove-testactor-hello.md]]. Later cases stay on this catalog.

Tests observe Graph and registry outcomes from outside; TestActor does not assert.

- [ ] Tests launch TestActor through the normal Core Actor path.
- [ ] Tests observe Graph and registry outcomes from outside the Actor; TestActor does not assert.
- [ ] Catalog covers launch ordering, Change/Cancel order, success, safe Failed, Cancelled, duplicate terminal, drop, and Interrupted restart.
- [ ] Tests use no live service, key, or Agent transport.

## See also

[[plan/core-creation/issues/02-core-actor-pool.md]], [[plan/core-creation/issues/18-finish-and-drop.md]], [[plan/core-creation/reports/actor-pool-rewind-review.md]]

## Comments

- 2026-09-11 — First proof slice is [[29-prove-testactor-hello.md]]. This ticket stays the full TestActor proof catalog.
- 2026-09-11 — Dispatch is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]].

## Time

- 2026-09-11 5m — stub provider-neutral TestActor proof after redesign reconciliation (from chat)
- 2026-09-11 5m — record Command-text TestActor dispatch (from chat)
- 2026-09-11 5m — drop restated hello and finish contracts; keep unique catalog (from chat)
