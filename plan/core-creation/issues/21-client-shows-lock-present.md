# 21 — Client shows lock-present

**Status:** ready-for-agent
**Blocked by:** [[plan/core-creation/issues/02-core-actor-pool.md]], [[plan/core-creation/issues/20-client-presents-credential.md]]

## Context

A person may need to see that an Actor is live for a Focus. The Graph lock-present field and span display are superseded by the durable lifecycle Event model in [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. This issue name is historical.

## What to build

If this UI remains required, the Browser projects live Actor state from ActorStarted and ActorFinished received through normal Core responses and Poll. The UI does not add a Graph field or a second History application.

- [ ] Browser lifecycle projection identifies a live Actor by Focus after ActorStarted and clears it after ActorFinished.
- [ ] No Graph lock-present field, span lock, or separate History/audit UI is added.

## See also

[[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]], [[plan/core-creation/issues/12-define-actor-pool-shutdown-behavior.md]]
