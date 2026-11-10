# 31 — Migrate onto the workspace lock

**Status:** `defined`
**Type:** coding
**Blocked by:** 30 — Expand the workspace lock
**Trackers:** architecture

## Context

30 — Expand the workspace lock stands `NodeLockPool.withLock` beside `withWorkTreeGate`. Persist, parse-thread file use, and git Load and git Save still need to follow that protocol. The architecture keeps Workspace lock handoff as its own beat under Parse stack Migrate, blocked by that Expand. This migrate is a different beat. Parse-thread file use here is the drain and the hold of parse reads while a Workspace `withLock` is pending. 32 — Workspace lock handoff owns the landing order after that drain: pull or Upload lands, arrived files are marked Unparsed, the lock releases, and Unparsed starts the parse thread. 01 — Persist/git work-tree gate names this caller shift. This ticket builds it.

## What to build

Persist, parse-thread file use, and git Load and git Save follow `NodeLockPool.withLock`. `withWorkTreeGate` still runs for the dual-run.

### Migrate

1. [ ] **Persist locks** — Persist calls `NodeLockPool.withLock` on the document-root node. While a conflicting `withLock` is pending, that new request waits.
2. [ ] **Parse reads** — An in-flight parse read drains under a pending Directory or Workspace `withLock`. A new parse read is `NodeLockPool.withLock` on its node and waits until that lock releases.
3. [ ] **Git Load and git Save** — Git Load and git Save call `NodeLockPool.withLock` on the Workspace node and wait for the drain. The landing order after that drain is 32 — Workspace lock handoff.
4. [ ] **Gate remains** — `withWorkTreeGate` remains while the dual-run is required.

### Obligations

1. [ ] **Parse and persist claim** — The Parse and persist claim says Persist, parse-thread file use, and git Load and git Save follow `NodeLockPool.withLock`, and `withWorkTreeGate` still runs.
2. [ ] **Caller test** — A test shows Persist calls `NodeLockPool.withLock` on the document-root node, a parse read waits while a Workspace `withLock` is pending, and git Load and git Save call `withLock` on the Workspace node. The test shows `withWorkTreeGate` still runs.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement](plan/core-refinement/project.md)
- [01 — Persist/git work-tree gate](plan/core-refinement/issues/01-persist-git-work-tree-gate.md)
- [30 — Expand the workspace lock](plan/core-refinement/issues/30-expand-the-workspace-lock.md)
- [32 — Workspace lock handoff](plan/core-refinement/issues/32-workspace-lock-handoff.md)
- [Parse and persist](doc/current/parse-persist.md)
