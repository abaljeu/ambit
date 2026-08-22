# 10 — Child-list approximation polish

**Context:** Ticket 05 locks occurrence-bag Accept Both; order may remain approximate. The approximation algorithm is explicitly later / not locked. This ticket improves order quality without changing Accept Both semantics or protocol.

**What to build:** Better ordered-list approximation while preserving occurrence-bag Accept Both for same-parent concurrent edits. Demo: concurrent same-parent edits keep critical edges and a clearer order.

**Blocked by:** 05 — Child-list Accept Both (same-parent merge)

**See also:** [[../details/conflict-resolution.md]], [[../details/merge-invariant.md]]

**Status:** ready-for-agent

- [ ] Same-parent concurrent merges keep occurrence-bag Accept Both semantics.
- [ ] Ordered result is observably clearer than the post-05 approximation for representative concurrent insert/remove cases.
- [ ] No new Reject path or protocol channel is introduced.
