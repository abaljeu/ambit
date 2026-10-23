# 33 — Contract the work-tree gate

**Status:** `defined`
**Type:** coding
**Blocked by:** 31 — Migrate onto the workspace lock
**Trackers:** architecture

## Context

31 — Migrate onto the workspace lock moves Persist, parse-thread file use, and git Load and git Save onto the workspace lock. `withWorkTreeGate` still runs for that dual-run. 01 — Persist/git work-tree gate revoked the gate as the lasting protocol. This ticket removes the gate. 32 — Workspace lock handoff is the separate landing beat. It uses the workspace lock. This ticket removes the gate once the lock protocol is the only protocol on Persist and git paths.

## What to build

`withWorkTreeGate` leaves Persist and git paths. The workspace lock and the per-member persist locks are the protocol on those paths.

### Contract

1. [ ] **Remove the gate** — `withWorkTreeGate` is removed from Persist and git paths.
2. [ ] **Lock protocol** — Persist, parse-thread file use, and git Load and git Save use the workspace lock and the per-member persist locks.

### Obligations

1. [ ] **Parse and persist claim** — The Parse and persist claim says `withWorkTreeGate` is gone from Persist and git paths.
2. [ ] **Gate test** — A test shows Persist and git Load and git Save run without `withWorkTreeGate`.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement](plan/core-refinement/project.md)
- [01 — Persist/git work-tree gate](plan/core-refinement/issues/01-persist-git-work-tree-gate.md)
- [31 — Migrate onto the workspace lock](plan/core-refinement/issues/31-migrate-onto-the-workspace-lock.md)
- [32 — Workspace lock handoff](plan/core-refinement/issues/32-workspace-lock-handoff.md)
- [Parse and persist](doc/current/parse-persist.md)
