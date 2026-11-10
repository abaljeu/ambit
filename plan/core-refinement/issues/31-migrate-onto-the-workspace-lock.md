# 31 — Migrate onto the workspace lock

**Status:** `defined`
**Type:** coding
**Blocked by:** 11 — Expand the node lock pool. 11 has passed review, so this ticket is unblocked. It does not wait for 11 to be `done`.
**Trackers:** architecture

## Context

30 — Expand the workspace lock is superseded by 11 — Expand the node lock pool. `NodeLockPool` replaces the lock this ticket used. Persist, parse-thread file use, and git Load and git Save follow `NodeLockPool.withLock`, including git Save. The same change removes `withWorkTreeGate` and the `GitRun` private gate. `NodeLockPool.withLock` is the only protocol on Persist and git paths. 33 — Contract the work-tree gate is superseded by this ticket. The architecture keeps Workspace lock handoff as its own beat under Parse stack Migrate. 11 — Expand the node lock pool has passed review, so that beat is unblocked. It does not wait for 11 to be `done`. This migrate is a different beat. Parse-thread file use here is the drain and the hold of parse reads while a Workspace `withLock` is pending. 32 — Workspace lock handoff owns the landing order after that drain: pull or Upload lands, arrived files are marked Unparsed, the lock releases, and Unparsed starts the parse thread. 01 — Persist/git work-tree gate names this caller shift. This ticket builds it.

## What to build

Persist, parse-thread file use, and git Load and git Save follow `NodeLockPool.withLock`, including the git Save commit. The same change removes `withWorkTreeGate` and the `GitRun` private gate (`GitRun.withWorkTreeGate` around `gitExec`). `NodeLockPool.withLock` is the only protocol on Persist and git paths.

### Migrate

1. [ ] **Persist locks** — Persist calls `NodeLockPool.withLock` on the document-root node. While a conflicting `withLock` is pending, that new request waits.
2. [ ] **Parse reads** — An in-flight parse read drains under a pending Directory or Workspace `withLock`. A new parse read is `NodeLockPool.withLock` on its node and waits until that lock releases.
3. [ ] **Git Load and git Save** — Git Load and git Save call `NodeLockPool.withLock` on the Workspace node, including the git Save commit, and wait for the drain. The landing order after that drain is 32 — Workspace lock handoff.

### Contract

1. [ ] **Remove the gates** — `withWorkTreeGate` and the `GitRun` private gate are removed from Persist and git paths. `NodeLockPool.withLock` is the only protocol on those paths.
2. [ ] **Lock protocol** — Persist, parse-thread file use, and git Load and git Save use `NodeLockPool.withLock`, including the git Save commit.

### Obligations

1. [ ] **Parse and persist claim** — The Parse and persist claim says Persist, parse-thread file use, and git Load and git Save follow `NodeLockPool.withLock`, including the git Save commit, and `withWorkTreeGate` and the `GitRun` private gate are gone from Persist and git paths.
2. [ ] **Caller test** — A test shows Persist calls `NodeLockPool.withLock` on the document-root node, a parse read waits while a Workspace `withLock` is pending, and git Load and git Save call `withLock` on the Workspace node.
3. [ ] **Gate test** — A test shows Persist and git Load and git Save run with `NodeLockPool.withLock` as the only protocol, and shows `withWorkTreeGate` and the `GitRun` private gate absent from those paths.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement](plan/core-refinement/project.md)
- [01 — Persist/git work-tree gate](plan/core-refinement/issues/01-persist-git-work-tree-gate.md)
- [11 — Expand the node lock pool](plan/node-move-disk/issues/11-expand-the-node-lock-pool.md)
- [32 — Workspace lock handoff](plan/core-refinement/issues/32-workspace-lock-handoff.md)
- [33 — Contract the work-tree gate](plan/core-refinement/issues/33-contract-the-work-tree-gate.md) — superseded by this ticket
- [Parse and persist](doc/current/parse-persist.md)

## Notes

- 2026-11-10 — Alan: `NodeLockPool` replaces the lock this ticket used. The lock is `NodeLockPool.withLock`, including git Save.
- 2026-11-10 — Alan: this ticket also removes `withWorkTreeGate` from Persist and git paths in the same change. There is no dual-run. 33 — Contract the work-tree gate is superseded by this ticket. The listed blocker is 11 — Expand the node lock pool, not 30 — Expand the workspace lock. 11 has passed review, so this ticket is unblocked. It does not wait for 11 to be `done`.
- 2026-11-10 — Alan: the `GitRun` private gate goes in this same change. `NodeLockPool.withLock` is the only protocol on Persist and git paths.
