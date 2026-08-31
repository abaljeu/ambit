# 10 — Child-list approximation polish

**Context:** Ticket 05 locks occurrence-bag Accept Both and **order invariants** (context order, intent add order, honored removes, no id cancel — [[../details/replace-amendment.md]] §4). This ticket improves **interleaving polish** when multiple valid orderings satisfy those invariants; it does not randomize and does not change which occurrences survive.

**What to build:** Clearer ordered-list interleaving while preserving occurrence-bag Accept Both and the §4 order invariants for same-parent concurrent edits. Demo: concurrent same-parent edits keep critical edges and a clearer order than the post-05 deterministic rule where several interleavings are valid.

**Blocked by:** 05 — Child-list Accept Both (same-parent merge)

**See also:** [[../details/conflict-resolution.md]], [[../details/merge-invariant.md]]

**Status:** ready-for-agent

- [ ] Same-parent concurrent merges keep occurrence-bag Accept Both semantics.
- [ ] Ordered result is observably clearer than the post-05 deterministic interleaving for representative concurrent insert/remove cases where multiple valid orderings exist.
- [ ] No new Reject path or protocol channel is introduced.
