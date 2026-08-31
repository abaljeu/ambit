# 02 — Independent concurrent Changes succeed

**Context:** The global revision gate refused any concurrent Change that named a stale base revision, even when Ops touched unrelated Nodes or parents. That was critical-flaw behavior to beat. Gate removal was **delivered in this issue**; upstream evidence is [[.scratch/relaxed-concurrency/map.md]] known 3. One global Server revision sequence is accepted.

**What to build:** Two Actors may post Changes against a stale global revision when their Ops do not collide on per-Op preconditions; both succeed. Unrelated attribute edits and structural edits under different parents no longer Reject solely for revision lag. Same-target compare-and-swap Reject may still exist until later amendment tickets.

**Blocked by:** None — can start immediately.

**See also:** [[../../relaxed-concurrency/map.md]], [[../details/relation-to-relaxed-concurrency.md]]

**Status:** done

- [x] Concurrent Changes that only lag the global revision, and whose Ops do not collide on per-Op preconditions, both apply successfully.
- [x] Unrelated attribute edits and structural edits under different parents are not refused solely for revision lag.
- [x] Auth and malformed requests remain Reject; this ticket does not invent field or child-list merge.
