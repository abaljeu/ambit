# 05 — Relocate in the same workspace

## 1. Current

[x] [Parse and persist](../../../../doc/current/parse-persist.md) already states the persist path. A coalesced path move runs `File.Move` or `Directory.Move` inside `NodeLockPool.withLock`. This step left that page as written.
[x] Story 2 Relocate with git mv is `WorkspaceGit.gitMove` in `src/Server/WorkspaceGit.fs`. The seam tests call it. `DocumentPersistChange.persistGraphChange` calls `executePathMoves`.

## 2. User

[x] `doc/user` pages describe outline commands. A same-Workspace `git mv` stays off those pages.

## 3. Found elsewhere

[[../../arch.md]]

[x] §8 question 5 names `WorkspaceGit.gitMove` in `src/Server/WorkspaceGit.fs` as the same-Workspace move. `moveAcrossWorkspaces` stays absent.

[[../03-research-directory-level-persist-gate/03-research-directory-level-persist-gate-ticket.md]]

[x] The Move write gate checkbox stays open. That section names `WorkspaceGit.gitMove` in `src/Server/WorkspaceGit.fs`. `moveAcrossWorkspaces` stays absent.

[[../04-expand-the-node-enclosure-lock/04-expand-the-node-enclosure-lock-ticket.md]]

[x] `FileAgent` and `DbAgent` stay as they are. `WorkspaceGit.gitMove` is in `src/Server/WorkspaceGit.fs`. `moveAcrossWorkspaces` stays absent.

[[../06-keep-a-nested-node-whose-derived-path-differs/06-keep-a-nested-node-whose-derived-path-differs-ticket.md]]

[x] `DocumentPathMove.sameWorkspaceMoves` is in `src/Shared/DocumentPathMove.fs`. 06 — Keep a nested node whose derived path differs does not add it.

[[../07-move-an-owned-node-across-workspaces/07-move-an-owned-node-across-workspaces-ticket.md]]

[x] `DocumentPersistPath.repositoryRoot` and `DocumentPersistPath.gitPath` are in `src/Server/DocumentPersistPath.fs`. `WorkspaceGit.gitMove` is one `GitSave.runGit` call with `mv --`.

[[../09-relocate-under-the-node-enclosure-lock/09-relocate-under-the-node-enclosure-lock-ticket.md]]

[x] `sameWorkspaceMoves`, `repositoryRoot`, `gitPath`, and `gitMove` are in source.
[x] 09 — Relocate under the node lock still waits on 06 — Keep a nested node whose derived path differs, 07 — Move an owned node across workspaces, and 08 — Amend a change conflict before the graph is stored.
