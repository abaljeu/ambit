# 26 — Forbid Unloaded destinations in the Move dialog

**Context:** MoveSelected must not change Unloaded child lists. Ticket 25 adds the Shared guard. The Move dialog must also stop the user from picking an Unloaded Node as a destination.

**What to build:** Change the Move dialog so a user cannot pick an Unloaded Node as a destination. Keep the Shared MoveSelected guard from ticket 25 as the commit-time rule.

**Blocked by:** 17 — Represent unloaded child lists end to end; 25 — Guard structural commands at unloaded boundaries.

**See also:** [[plan/selective-client-loading/spec.md]] (Move dialog Unloaded destinations); [[plan/selective-client-loading/issues/14-simplify-selective-loading.md]] (Structural commands); [[plan/selective-client-loading/issues/25-guard-structural-commands-at-unloaded-boundaries.md]] (Shared guard).

**Status:** ready-for-agent

- [ ] The Move dialog does not offer Unloaded Nodes as destinations.
- [ ] A user cannot confirm a Move whose destination has Unloaded children.
- [ ] The dialog rule does not Load content and does not change residency.
- [ ] MoveSelected still goes through the Shared pre-commit guard from ticket 25.

## Comments

- 2026-09-02: Parked from WORK.md Blocked. Blocked by already recorded.
