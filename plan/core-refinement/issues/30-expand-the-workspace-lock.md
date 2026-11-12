# 30 — Expand the workspace lock

**Status:** `superseded`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

Persist and git paths still run `withWorkTreeGate`. That exclusive gate is not the lasting protocol. 01 — Persist/git work-tree gate records that revocation. This ticket stands the workspace lock. 31 — Migrate onto the workspace lock moves Persist, parse-thread file use, and git Load and git Save onto the lock and removes `withWorkTreeGate` from Persist and git paths in that same change. 32 — Workspace lock handoff is its own beat under Parse stack Migrate. 11 — Expand the node lock pool has passed review, so 32 is unblocked. It does not wait for 11 to be `done`. It is the landing order: the lock drains, pull or Upload lands, arrived files are marked Unparsed, the lock releases, and Unparsed starts the parse thread. 33 — Contract the work-tree gate is superseded by 31 — Migrate onto the workspace lock.

## What to build

`NodeLockPool.withLock` stands beside `withWorkTreeGate`. Callers stay on the gate. The gate stays.

### Expand

1. [ ] **Workspace lock** — `NodeLockPool.withLock` stands beside `withWorkTreeGate`. A request on the Workspace node announces that member files may change.
2. [ ] **Pending** — While that request is pending, a conflicting `withLock` waits.
3. [ ] **Drain** — The request waits until in-flight member file writes and parse reads have drained.
4. [ ] **Per-member persist locks** — A persist is the same `NodeLockPool.withLock` on the document-root node, beside `withWorkTreeGate`.
5. [ ] **Gate stays** — `withWorkTreeGate` stays on Persist and git paths.

### Obligations

1. [ ] **Parse and persist claim** — The Parse and persist claim says `NodeLockPool.withLock` stands beside `withWorkTreeGate`.
2. [ ] **Lock test** — A test calls `NodeLockPool.withLock` beside `withWorkTreeGate`, shows that a pending request blocks a conflicting `withLock`, and shows that the request waits for an in-flight member write and an in-flight parse read to drain.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement](plan/core-refinement/project.md)
- [01 — Persist/git work-tree gate](plan/core-refinement/issues/01-persist-git-work-tree-gate.md)
- [31 — Migrate onto the workspace lock](plan/core-refinement/issues/31-migrate-onto-the-workspace-lock.md)
- [32 — Workspace lock handoff](plan/core-refinement/issues/32-workspace-lock-handoff.md)
- [33 — Contract the work-tree gate](plan/core-refinement/issues/33-contract-the-work-tree-gate.md) — superseded by 31 — Migrate onto the workspace lock
- [Parse and persist](doc/current/parse-persist.md)

## Notes

- 2026-11-09 — An earlier mark named [04 — Expand the node enclosure lock](plan/node-move-disk/issues/04-expand-the-node-enclosure-lock/04-expand-the-node-enclosure-lock-ticket.md). The 2026-11-10 note names the superseding ticket.
- 2026-11-10 — Alan: this ticket is superseded by [11 — Expand the node lock pool](plan/node-move-disk/issues/11-expand-the-node-lock-pool/11-expand-the-node-lock-pool-ticket.md).
- 2026-11-10 — What to build is the superseded stand-beside job. 31 — Migrate onto the workspace lock is the one-step change: it moves onto `NodeLockPool.withLock` and removes `withWorkTreeGate` and the `GitRun` private gate together.
