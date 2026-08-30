# 11 — Completing-ops pattern beyond timing

**Context:** Fill-in timing (completing Ops in the same Change as the delete) is accepted. The rest of the completing-ops pattern is proposed. Timing already constrains earlier produce/consume work; this ticket locks the fuller pattern on the shared Actor path.

**What to build:** Locked fill-in pattern (not only timing): when an Actor's view is too small, the Server completes missing Ops in the same Change; Clients see those Ops on History with that Change. Distinct from amendment and from rewind/replay.

**Blocked by:** 07 — Generalized Server Actor produce path

**See also:** [[../details/completing-ops.md]], [[../details/actors-and-jobs.md]]

**Status:** ready-for-agent

- [ ] Server fill-in for a partial-view delete/promote lands in the same Change as the Actor's Ops (not a later second Change).
- [ ] Clients observe completing Ops on History together with that Change.
- [ ] Pattern is documented as distinct from amendment and from Client rewind/replay.
