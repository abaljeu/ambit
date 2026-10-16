# 31 — Migrate onto the workspace lock

**Status:** `defined`
**Type:** coding
**Blocked by:** 30 — Expand the workspace lock
**Trackers:** architecture

## Context

30 — Expand the workspace lock stands the workspace lock and the per-member persist locks beside `withWorkTreeGate`. Persist, parse-thread file use, and git Load and git Save still need to follow that protocol. The architecture keeps Workspace lock handoff as its own beat under Parse stack Migrate, blocked by that Expand. This migrate is a different beat. Parse-thread file use here is the drain and the hold of parse reads while the workspace lock is pending. 32 — Workspace lock handoff owns the landing order after that drain: pull or Upload lands, arrived files are marked Unparsed, the lock releases, and Unparsed starts the parse thread. 01 — Persist/git work-tree gate names this caller shift. This ticket builds it.

## What to build

Persist, parse-thread file use, and git Load and git Save follow the workspace lock. `withWorkTreeGate` still runs for the dual-run.

### Migrate

1. [ ] **Persist locks** — Persist takes a per-member persist lock. While the workspace lock is pending, a new per-member persist lock cannot be taken.
2. [ ] **Parse reads** — An in-flight parse read drains under the pending workspace lock. A new parse read waits until that lock releases. Parse does not take a persist lock.
3. [ ] **Git Load and git Save** — Git Load and git Save take the workspace lock and wait for the drain. The landing order after that drain is 32 — Workspace lock handoff.
4. [ ] **Gate remains** — `withWorkTreeGate` remains while the dual-run is required.

### Obligations

1. [ ] **Parse and persist claim** — The Parse and persist claim says Persist, parse-thread file use, and git Load and git Save follow the workspace lock, and `withWorkTreeGate` still runs.
2. [ ] **Caller test** — A test shows Persist takes a per-member persist lock, a parse read waits while the workspace lock is pending, and git Load and git Save take the workspace lock. The test shows `withWorkTreeGate` still runs.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement](plan/core-refinement/project.md)
- [01 — Persist/git work-tree gate](plan/core-refinement/issues/01-persist-git-work-tree-gate.md)
- [30 — Expand the workspace lock](plan/core-refinement/issues/30-expand-the-workspace-lock.md)
- [32 — Workspace lock handoff](plan/core-refinement/issues/32-workspace-lock-handoff.md)
- [Parse and persist](doc/current/parse-persist.md)
