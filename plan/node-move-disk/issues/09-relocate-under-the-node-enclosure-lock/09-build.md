# 09 — Relocate under the node lock

Ticket: [09 — Relocate under the node lock](09-relocate-under-the-node-enclosure-lock-ticket.md)

STOP: Prerequisite inputs from 05 — Relocate in the same workspace, 06 — Keep a nested node whose derived path differs, 07 — Move an owned node across workspaces, and 08 — Amend a change conflict before the graph is stored are absent from source. `relocateAcceptedMoves` calls `sameWorkspaceMoves`, `crossWorkspaceMoves`, `repositoryRoot`, `gitPath`, `gitMove`, and `moveAcrossWorkspaces`. Those names are absent. Adding them edits files this ticket lists under Do not load.

## Missing

- `DocumentPathMove.sameWorkspaceMoves` in `src/Shared/DocumentPathMove.fs`. 05 — Relocate in the same workspace is `defined`.
- `DocumentPersistPath.repositoryRoot` and `DocumentPersistPath.gitPath` in `src/Server/DocumentPersistPath.fs`. Public functions end at `discoverImmediateMembers`.
- `WorkspaceGit.gitMove` in `src/Server/WorkspaceGit.fs`.
- `DocumentPathMove.crossWorkspaceMoves` and `WorkspaceGit.moveAcrossWorkspaces`. 07 — Move an owned node across workspaces is `defined`.
- 06 — Keep a nested node whose derived path differs is `defined`. It blocks 08 — Amend a change conflict before the graph is stored.
- `DocumentPersistChange.findIncomingPathConflicts` in `src/Server/DocumentPersistChange.fs`. 08 is `defined`. The post graph for `relocateAcceptedMoves` is the graph after that rename.

## In source

- `NodeLockPool.withLock` in `src/Server/NodeLockPool.fs` is `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`.
- `DocumentPathMove.planPathMovesBetweenGraphs` and `DocumentPathMove.coalescePathMoves` are in `src/Shared/DocumentPathMove.fs`.
- `GraphQuery.enclosingWorkspace` is in `src/Shared/GraphQuery.fs`.
- `FileAgent.processPostEvents` stores `loaded.state` after `validatePostChange` returns `Ok`.
- `DbAgent.finishAppliedPostChange` stores through `commitPostChange` after `validatePostChange` returns `Ok`.
- `DocumentPersistWrite.writeDocument` and `DocumentPersistWrite.writeArtifactText` call `NodeLockPool.withLockResult` around the write.
- `ParseThread.reconcileDirectory` calls `NodeLockPool.withLockResult` around `DirectoryReconcile.planDirectoryReconcile`.
- `executePathMoves` uses `File.Move` and `Directory.Move`. 13 — Lock the persist path moves wraps those moves in `persistGraphChangeWith`.
