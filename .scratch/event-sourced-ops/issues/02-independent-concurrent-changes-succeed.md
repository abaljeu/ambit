# 02 — Independent concurrent Changes succeed

**Context:** The global revision gate refuses any concurrent Change that names a stale base revision, even when Ops touch unrelated Nodes or parents. That is critical-flaw behavior to beat. Sibling [[.scratch/relaxed-concurrency/]] slice 1 may deliver the gate removal; this ticket is then verify/handoff rather than a duplicate build. One global Server revision sequence is accepted.

**What to build:** Two Actors may post Changes against a stale global revision when their Ops do not collide on per-Op preconditions; both succeed. Unrelated attribute edits and structural edits under different parents no longer Reject solely for revision lag. Same-target compare-and-swap Reject may still exist until later amendment tickets.

**Blocked by:** None — can start immediately.

**See also:** [[../../relaxed-concurrency/spec.md]], [[../details/relation-to-relaxed-concurrency.md]]

**Status:** ready-for-agent

- [ ] Concurrent Changes that only lag the global revision, and whose Ops do not collide on per-Op preconditions, both apply successfully.
- [ ] Unrelated attribute edits and structural edits under different parents are not refused solely for revision lag.
- [ ] Auth and malformed requests remain Reject; this ticket does not invent field or child-list merge.
