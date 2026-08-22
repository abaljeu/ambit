# 05 — Child-list Accept Both (same-parent merge)

**Context:** Same-parent Replace span mismatch still Rejects after field amendment. The Actor contract is full-list `Replace(parentId, oldList, newList)` with three-way resolve and occurrence-bag Accept Both against the common prior ([[../details/replace-amendment.md]]). Order polish is issue 10.

**What to build:** Full-list Replace shape; three-way resolve (`current`, `intent`, `context` → `target`); same-parent concurrent inserts/removes merge without Reject. Deterministic acceptBoth with order invariants ([[../details/replace-amendment.md]] §4). Server amends newest Replace to `Replace(parentId, current, target)`; Client already consumes via ticket 04. Critical child edges are not discarded.

**Blocked by:** 03 — Server amends recoverable field collisions (text, name, classes), 04 — Client consumes merge success without reload

**See also:** [[../details/replace-amendment.md]], [[../details/conflict-resolution.md]], [[../details/merge-invariant.md]]

**Status:** ready-for-agent

- [ ] Same-parent concurrent inserts/removes succeed without Reject.
- [ ] Occurrence-bag Accept Both preserves critical add/remove slots and §4 order invariants against the common prior.
- [ ] Amended child-list success is consumed through the existing rewind/replay path without reload.
