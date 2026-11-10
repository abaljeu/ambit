# 09 — Relocate under the node lock

## 1. Stop

[x] Build stops before a source edit.
[x] Staging is `85c132de`, [31 — Migrate onto the workspace lock](plan/core-refinement/issues/31-migrate-onto-the-workspace-lock.md). `origin/staging` is that commit.
[x] Process step 1 in [09 — Relocate under the node lock](../issues/09-relocate-under-the-node-enclosure-lock.md) stops when an input is missing.
[x] `relocateAcceptedMoves` calls `sameWorkspaceMoves`, `crossWorkspaceMoves`, `repositoryRoot`, `gitPath`, `gitMove`, and `moveAcrossWorkspaces`. Those names are absent from source. Adding them edits files this ticket lists under Do not load.

## 2. Missing inputs

[x] `DocumentPathMove.sameWorkspaceMoves` belongs in `src/Shared/DocumentPathMove.fs`. [05 — Relocate in the same workspace](../issues/05-relocate-in-the-same-workspace.md) is `defined`.
[x] `DocumentPersistPath.repositoryRoot` and `DocumentPersistPath.gitPath` belong in `src/Server/DocumentPersistPath.fs`. That file's public functions end at `discoverImmediateMembers`.
[x] `WorkspaceGit.gitMove` belongs in `src/Server/WorkspaceGit.fs`.
[x] `DocumentPathMove.crossWorkspaceMoves` and `WorkspaceGit.moveAcrossWorkspaces` belong with [07 — Move an owned node across workspaces](../issues/07-move-an-owned-node-across-workspaces.md). That ticket is `defined`.
[x] [06 — Keep a nested node whose derived path differs](../issues/06-keep-a-nested-node-whose-derived-path-differs.md) is `defined`. It is the blocker of [08 — Amend a change conflict before the graph is stored](../issues/08-amend-a-change-conflict-before-the-graph-is-stored.md).
[x] `DocumentPersistChange.findIncomingPathConflicts` belongs in `src/Server/DocumentPersistChange.fs` from 08 — Amend a change conflict before the graph is stored. That ticket is `defined`. The post graph for `relocateAcceptedMoves` is the graph after that rename.

## 3. Inputs that match

[x] `NodeLockPool.withLock` in `src/Server/NodeLockPool.fs` has type `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`.
[x] `DocumentPathMove.planPathMovesBetweenGraphs` and `DocumentPathMove.coalescePathMoves` are in `src/Shared/DocumentPathMove.fs`.
[x] `GraphQuery.enclosingWorkspace` is in `src/Shared/GraphQuery.fs`.
[x] `FileAgent.processPostEvents` stores `loaded.state` after `validatePostChange` returns `Ok`.
[x] `DbAgent.finishAppliedPostChange` stores through `commitPostChange` after `validatePostChange` returns `Ok`. `DbAgent.createForTestWithDependencies` takes `liveSaveDataDir`.
[x] ``FileAgent applyEvent marks a File Node at the later Edit id`` is in `tests/Server.Tests/PersistApplyTests.fs`.
[x] `DocumentPersistWrite.writeDocument` and `DocumentPersistWrite.writeArtifactText` call `NodeLockPool.withLockResult` around the write.
[x] `ParseThread.reconcileDirectory` calls `NodeLockPool.withLockResult` around `DirectoryReconcile.planDirectoryReconcile`. `parseDirectory` calls `reconcileDirectory`.
[x] `DocumentPersistWrite.planParseFile` calls `preparedParseText`. The disk-read branch of `preparedParseText` calls `withLockResult`. The `withLock` the ticket places on `planParseFile` around `preparedParseText` is still open.
[x] `executePathMoves` uses `File.Move` and `Directory.Move`. `persistGraphChangeWith` calls `executePathMoves` with no `withLock`.
[x] [04 — Expand the node enclosure lock](../issues/04-expand-the-node-enclosure-lock.md), [12 — Compare locks on the live graph](../issues/12-compare-locks-on-the-live-graph.md), and 31 — Migrate onto the workspace lock are on this commit. `WorkspaceGit.withWorkTreeGate` is absent.

## 4. Ask

[x] Land 05 — Relocate in the same workspace, 06 — Keep a nested node whose derived path differs, 07 — Move an owned node across workspaces, and 08 — Amend a change conflict before the graph is stored on staging. Then run this build again.
