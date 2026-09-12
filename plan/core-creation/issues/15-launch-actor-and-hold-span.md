# 15 — Launch an Actor and hold the span

**Status:** blocked
**Blocked by:** [[plan/core-creation/issues/14-server-tracks-credentials.md]]
**Actual:** 1h40m

## Context

This issue records a historical span-based launch delivery. The span, Graph lock-present, Browser-selected ActorName, and transient-only public number are superseded by [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Its replacement contract directs the launch part of the rebuild; the historical delivery remains in Comments and Time.

## What to build

Implement launch and Focus registration from [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Dispatch is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. Pool shape is [[02-core-actor-pool.md]].

- [ ] Register public identity, secret, termination handle, and Focus NodeId in mailbox state.
- [ ] Append ActorStarted, then schedule so output cannot be admitted first.
- [ ] Refuse only a second live Actor for the same Focus.

## See also

[[plan/core-creation/issues/09-define-core-command-launch-contract.md]], [[plan/core-creation/reports/to-build-09-12-actor-pool.md]], [[plan/core-creation/reports/commit-14-implement-15.md]]

## Comments

- 2026-09-06 — Implementation started on `dev`.
- 2026-09-06 — Delivered Core Actor pool launch, span extract, never-reused public number, send credential in the Actor and the Core set, overlap refuse, and lock-present overlay on live Nodes. Node JSON, History Changes, and projection SQL rows omit lock. HTTP Command launch, query, cancel, and Browser lock UI stay 16–22. See [[plan/core-creation/reports/commit-14-implement-15.md]].
- 2026-09-11 — Reconciled with [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. Credential and registry responsibility remain relevant. Focus NodeId replaces span membership: refuse only a second live Actor for the same Focus and allow every other extract overlap.
- 2026-09-11 — [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] superseded the remaining span and non-event assumptions. ActorStarted now communicates durable public Actor identity.
- 2026-09-11 — Reopened as `blocked` by [[plan/core-creation/issues/14-server-tracks-credentials.md]] because the replacement launch contract is not delivered.
- 2026-09-11 — Dispatch is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]].

## Time

- 2026-09-06 1h30m — launch pool, span extract, and lock-present overlay (from chat)
- 2026-09-11 5m — record Command-text Actor dispatch (from chat)
- 2026-09-11 5m — drop restated launch membership; keep unique Focus registration (from chat)
