pass

# 05 — Relocate in the same workspace

Ticket: [05 — Relocate in the same workspace](05-relocate-in-the-same-workspace-ticket.md)

Check: pass

[04 — Expand the node enclosure lock](../04-expand-the-node-enclosure-lock/04-expand-the-node-enclosure-lock-ticket.md) is superseded by [11 — Expand the node lock pool](../11-expand-the-node-lock-pool/11-expand-the-node-lock-pool-ticket.md). `NodeLockPool.withLock` replaces `NodeEnclosureLock`. 11 has passed review. The blocker on 05 — Relocate in the same workspace is clear.

`DocumentPathMove.sameWorkspaceMoves` keeps a File or a Directory whose `GraphQuery.enclosingWorkspace` id is `Some` on the pre graph and the post graph and those ids are equal. It drops a `Special Workspace` node, a move whose enclosing Workspace ids differ, and a move whose enclosing Workspace id is `None`. It keeps input order. It reads no parse state.

`DocumentPersistPath.repositoryRoot` returns the same string as `DocumentPersistPath.workspaceRootFor` for a named Workspace. For enclosing Workspace `Graph.rootId` it returns `dataDirBase`. When `enclosingWorkspace` is `None` it returns `None`.

`DocumentPersistPath.gitPath` uses `DocumentPartition.artifactFileRelative` for a File and `DocumentPartition.artifactDirectoryRelative` for a Directory. For a named Workspace it drops that workspace directory name and the following slash. For ROOT it leaves the data-directory path unchanged. A Workspace node returns `None`. A named-Workspace path that lacks that prefix returns `None`.

`WorkspaceGit.gitMove` makes one `GitSave.runGit` call. The arguments are `mv --`, the old git path, and the new git path. One same-workspace move is one `gitMove` call. A Directory path moves that Directory tree. `gitMove` takes no child list and no parse state.

The seam test is the caller. It classifies with `sameWorkspaceMoves`, resolves `repositoryRoot` and `gitPath`, and calls `gitMove` once. `sameWorkspaceMoves`, `repositoryRoot`, `gitPath`, and `gitMove` have no production caller. `DocumentPersistChange`, `FileAgent`, `DbAgent`, `coalescePathMoves`, and `ChangeAmendment` stay as they are. This ticket takes no node lock. `NodeLockPool.withLock` is the only public lock protocol.

`crossWorkspaceMoves` and `moveAcrossWorkspaces` stay absent.

- `scripts/agent-test.sh` with no arguments passed Server 44 and Shared 20. Failed 0.
- `scripts/agent-test.sh --filter` for ``sameWorkspaceMoves keeps a same-workspace move and drops a cross-workspace move and a workspace move`` passed 1.
- `scripts/agent-test.sh --filter` for ``repository root and git path follow the workspace and ROOT`` passed 1.
- `scripts/agent-test.sh --filter` for ``same-workspace relocate runs git mv in the Workspace repository`` passed 1.
- `scripts/agent-test.sh --filter` for ``gitMove moves a directory tree with one git mv`` passed 1.
- `scripts/agent-test.sh --full` passed Server 781, skipped 1, and Shared 1937, skipped 3. Failed 0.

Distinct source files: 3. `DocumentPersistChange` stays as it is. The seam test calls `WorkspaceGit.gitMove`. `moveAcrossWorkspaces` stays absent. `WorkspaceGit.gitMove` is named in [Node move and disk architecture](../../arch.md) §8 question 5.
