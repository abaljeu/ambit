# 26 — Failed Actor stop must still drop

**Status:** cancelled
**Blocked by:** none

## What happened

The rewound implementation did not drop a failed Actor. Its live registry and secret credential remained. It also had no durable ActorFinished failure Event.

## What I expected

Failed-stop cleanup is [[plan/core-creation/issues/18-finish-and-drop.md]], locked by [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]].

## Why cancelled

This was a patch ticket on the discarded pool mailbox. Sets 1–3 product was rewound on `dev`. Reimplement through [[plan/core-creation/issues/18-finish-and-drop.md]]. The finding lives in [[plan/core-creation/reports/actor-pool-rewind-review.md]].

## Comments

- 2026-09-11 — Reconciled with the locked redesign. This issue remains `cancelled`; [[18-finish-and-drop.md]] owns failed-stop cleanup through the one mailbox and shared drop path.

## See also

[[plan/core-creation/issues/18-finish-and-drop.md]], [[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]]

## Time

- 2026-09-11 15m — filed from Core 18 review (from chat)
- 2026-09-11 — cancelled; rewind/redo, not a wrap patch (from chat)
