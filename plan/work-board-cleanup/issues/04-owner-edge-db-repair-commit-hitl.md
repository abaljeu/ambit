# 04 — Owner-edge DB repair: human closeout

**Status:** ready-for-human
**Blocked by:** None — can start immediately.

## Context

[[plan/owner-edge-db-repair/implement.md]] extended the startup sweep with ACID repair of `node_children`. The report left no commit. Later commit `a09f35a` includes [[src/Shared/ProjectionOwnershipRepair.fs]]. Remaining work is human confirmation, not more coding.

## What to build

A human confirms this Project is agent-done and closes it. No product F# in this ticket.

- [ ] Implementation on `dev` matches [[plan/owner-edge-db-repair/spec.md]] (GC unreachable; promote Ref when reachable has no owner).
- [ ] HITL on a damaged projection, or an explicit skip of HITL.
- [ ] Human merge to `ready` if this work is still only on `dev`.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md Active. Commit leftover was the original gap; a code commit landed later.

## See also

[[plan/owner-edge-db-repair/implement.md]], [[src/Server/DatabaseProjection.fs]]
