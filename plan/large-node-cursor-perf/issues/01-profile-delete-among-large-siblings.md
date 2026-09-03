# 01 — Profile and optimize delete among large siblings

**Status:** ready-for-agent
**Blocked by:** None — can start immediately.

## Context

[[plan/large-node-cursor-perf/delete-children-cost.md]] ranks delete cost among large sibling lists: mid-list `Replace` rebuilds via `fromNodes`, SiteMap rematch can be quadratic, and DOM takes the structural plan. Selection-only fast path does not cover Delete. No delete fix is implemented.

## What to build

Delete of one or a few children among a large expanded sibling list is profiled and made cheaper along the ranked hotspots, without a new bulk-delete command.

- [ ] HITL or measured profile of Delete among hundreds of siblings vs Fold of the same parent.
- [ ] At least one ranked hotspot from [[plan/large-node-cursor-perf/delete-children-cost.md]] is reduced with tests.
- [ ] Selection-only cursor path stays as it is.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md. Not a work-board-cleanup issue.

## See also

[[plan/large-node-cursor-perf/project.md]], [[plan/large-node-cursor-perf/implement-fix.md]]
