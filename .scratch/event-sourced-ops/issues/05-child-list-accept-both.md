# 05 — Child-list Accept Both (same-parent merge)

**Context:** Same-parent Replace span mismatch still Rejects after field amendment. The accepted rule is occurrence-bag Accept Both against the common prior; order may be approximate. Approximation polish is a later ticket.

**What to build:** Same-parent concurrent inserts/removes merge without Reject. Occurrence-bag Accept Both against the common prior; order may be approximate. Server amends the newest Replace; Client already consumes via ticket 04. Critical child edges are not discarded.

**Blocked by:** 03 — Server amends recoverable field collisions (text, name, classes), 04 — Client consumes merge success without reload

**See also:** [[../details/conflict-resolution.md]], [[../details/merge-invariant.md]]

**Status:** ready-for-agent

- [ ] Same-parent concurrent inserts/removes succeed without Reject.
- [ ] Occurrence-bag Accept Both preserves critical add/remove slots against the common prior (order may be approximate).
- [ ] Amended child-list success is consumed through the existing rewind/replay path without reload.
