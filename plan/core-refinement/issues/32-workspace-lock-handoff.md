# 32 — Workspace lock handoff

**Status:** `coded`
**Type:** coding
**Blocked by:** 11 — Expand the node lock pool. 11 has passed review, so this ticket is unblocked. It does not wait for 11 to be `done`.
**Trackers:** architecture

## Context

The architecture keeps Workspace lock handoff as its own beat under Parse stack Migrate. 30 — Expand the workspace lock is superseded by 11 — Expand the node lock pool. The lock in this handoff is `NodeLockPool.withLock`. 31 — Migrate onto the workspace lock stays a separate beat. That migrate moves Persist, parse-thread file use, and git Load and git Save onto `withLock` and removes `withWorkTreeGate` and the `GitRun` private gate in that same change. `NodeLockPool.withLock` is the only protocol on Persist and git paths. This ticket is the landing order. 02 — Git Load: Unparsed then Parse stack names that order. This ticket builds it. 07 — Git changed list sets Unparsed is superseded by 12 — Git pull marks each received file Unparsed and 13 — Git pull marks containing Directories Unparsed. Those tickets own the pull mark. This ticket runs that mark inside the pull.

## What to build

Fetch runs to completion first. Fetch holds no `withLock`. `NodeLockPool.withLock` on the Workspace node then drains. The pull merges the fetched commits and marks the arrived nodes Unparsed. The lock releases. Unparsed starts the parse thread.

### Workspace lock handoff

1. [x] **Fetch** — Fetch runs to completion. Fetch holds no `NodeLockPool.withLock`.
2. [x] **Drain** — `withLock` on the Workspace node drains in-flight member file use.
3. [x] **Pull** — The pull merges the fetched commits inside that `withLock`.
4. [x] **Mark Unparsed** — Arrived nodes are marked Unparsed inside that same `withLock`. The git pull mark supplies that Unparsed. The rules stay on 12 — Git pull marks each received file Unparsed and 13 — Git pull marks containing Directories Unparsed.
5. [x] **Release** — That `withLock` releases after that mark.
6. [x] **Parse thread** — Unparsed starts the parse thread. Core pushes the work onto the Parse stack.
7. [x] **Upload** — Upload uses that same `withLock` for the land, the Unparsed mark, and the release. Upload has no fetch.

### Obligations

1. [x] **Parse and persist claim** — The Parse and persist claim says fetch holds no `withLock`, the pull holds the Workspace `withLock` for the merge and the Unparsed mark, and Unparsed starts the parse thread after that release.
2. [x] **Landing test** — A test runs the order for pull: fetch completes before `withLock`, then drain, merge, mark Unparsed, and release run inside `withLock`, and the parse thread starts. A test runs Upload as land, mark Unparsed, and release inside `withLock`.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement](plan/core-refinement/project.md)
- [02 — Git Load: Unparsed then Parse stack](plan/core-refinement/issues/02-git-load-unparsed-then-parse-stack.md)
- [07 — Git changed list sets Unparsed](plan/core-refinement/issues/07-git-changed-list-unparsed.md) — superseded by 12 — Git pull marks each received file Unparsed and 13 — Git pull marks containing Directories Unparsed
- [12 — Git pull marks each received file Unparsed](plan/parse-thread/issues/12-git-pull-marks-each-received-file-unparsed.md)
- [13 — Git pull marks containing Directories Unparsed](plan/parse-thread/issues/13-git-pull-marks-containing-directories-unparsed.md)
- [11 — Expand the node lock pool](plan/node-move-disk/issues/11-expand-the-node-lock-pool/11-expand-the-node-lock-pool-ticket.md)
- [31 — Migrate onto the workspace lock](plan/core-refinement/issues/31-migrate-onto-the-workspace-lock.md)
- [Parse and persist](doc/current/parse-persist.md)

## Notes

- 2026-11-10 — Alan: fetch runs to completion first, unlocked. The pull holds the Workspace `withLock`. The pull is the merge of the fetched commits plus marking the arrived nodes Unparsed.
- 2026-11-10 — Alan: the listed blocker is 11 — Expand the node lock pool, not 30 — Expand the workspace lock. 11 has passed review, so this ticket is unblocked. It does not wait for 11 to be `done`. 31 — Migrate onto the workspace lock removes `withWorkTreeGate` from Persist and git paths in the same change as the move onto `withLock`.
- 2026-11-10 — Alan: that removal includes the `GitRun` private gate. `NodeLockPool.withLock` is the only protocol on Persist and git paths.
- 2026-11-10 — Build names `UploadHandoff` and `WorkspaceWebDav.WorkspaceFileLand`. `UploadHandoff` is the graph read, the Unparsed notice, and the parse load. `WorkspaceFileLand` is the label, the relative path, and the byte write.
