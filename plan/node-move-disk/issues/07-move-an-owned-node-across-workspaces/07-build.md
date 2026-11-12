# 07 — Move an owned node across workspaces

Ticket: [07 — Move an owned node across workspaces](07-move-an-owned-node-across-workspaces-ticket.md)

Build has not run. Status is `defined`.

The scope is a write-ticket record. The planned edit is:

- `src/Shared/DocumentPathMove.fs` — `crossWorkspaceMoves`.
- `src/Server/WorkspaceGit.fs` — `moveAcrossWorkspaces`: source delete, destination add, commit at move time, and recovery when the destination add fails.
- `DocumentPersistPath.repositoryRoot` and `gitPath` stay on 05 — Relocate in the same workspace.
- `coalescePathMoves` stays. Nested derived-path coalesce is 06 — Keep a nested node whose derived path differs.
- `tests/Shared.Tests/DocumentPathMoveTests.fs` — ``crossWorkspaceMoves keeps a cross-workspace move and drops a same-workspace move and a workspace move``.
- `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — ``cross-workspace move deletes from the source repository and adds to the destination repository``, ``moveAcrossWorkspaces commits each repository at move time``, and ``moveAcrossWorkspaces restores the source repository when the destination add fails``.
