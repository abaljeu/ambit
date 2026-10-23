# 30 — Expand the workspace lock

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

Persist and git paths still run `withWorkTreeGate`. That exclusive gate is not the lasting protocol. 01 — Persist/git work-tree gate records that revocation. This ticket stands the workspace lock. The architecture keeps three later beats. 31 — Migrate onto the workspace lock moves Persist, parse-thread file use, and git Load and git Save onto the lock while the gate still runs. 32 — Workspace lock handoff is its own beat under Parse stack Migrate. It is blocked by this Expand. It is the landing order: the lock drains, pull or Upload lands, arrived files are marked Unparsed, the lock releases, and Unparsed starts the parse thread. 33 — Contract the work-tree gate removes `withWorkTreeGate` after the migrate.

## What to build

The workspace lock and the per-member persist locks stand beside `withWorkTreeGate`. Callers stay on the gate. The gate stays.

### Expand

1. [ ] **Workspace lock** — The workspace lock stands beside `withWorkTreeGate`. It is an aggregate lock. It announces that member files may change.
2. [ ] **Pending** — While the workspace lock is pending, new per-member persist locks for those member files cannot be taken.
3. [ ] **Drain** — The workspace lock waits until in-flight member file writes and parse reads have drained.
4. [ ] **Per-member persist locks** — Per-member persist locks stand beside `withWorkTreeGate`.
5. [ ] **Gate stays** — `withWorkTreeGate` stays on Persist and git paths.

### Obligations

1. [ ] **Parse and persist claim** — The Parse and persist claim says the workspace lock and the per-member persist locks stand beside `withWorkTreeGate`.
2. [ ] **Lock test** — A test takes the workspace lock beside `withWorkTreeGate`, shows that a pending lock blocks a new per-member persist lock, and shows that the lock waits for an in-flight member write and an in-flight parse read to drain.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement](plan/core-refinement/project.md)
- [01 — Persist/git work-tree gate](plan/core-refinement/issues/01-persist-git-work-tree-gate.md)
- [31 — Migrate onto the workspace lock](plan/core-refinement/issues/31-migrate-onto-the-workspace-lock.md)
- [32 — Workspace lock handoff](plan/core-refinement/issues/32-workspace-lock-handoff.md)
- [33 — Contract the work-tree gate](plan/core-refinement/issues/33-contract-the-work-tree-gate.md)
- [Parse and persist](doc/current/parse-persist.md)
