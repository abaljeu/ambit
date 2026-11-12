# 05 — Relocate in the same workspace

Ticket: [05 — Relocate in the same workspace](05-relocate-in-the-same-workspace-ticket.md)

Build has not run. Status is `defined`.

The scope is a write-ticket record. The planned edit is:

- `src/Shared/DocumentPathMove.fs` — `sameWorkspaceMoves` beside `coalescePathMoves`, using `GraphQuery.enclosingWorkspace`, excluding a `Special Workspace` node.
- `src/Server/DocumentPersistPath.fs` — repository root and the git-relative path.
- `src/Server/WorkspaceGit.fs` — `gitMove` through `GitSave.runGit` with `git mv`.
- `src/Server/DocumentPersistChange.fs` — a same-workspace relocate entry that calls `gitMove` once per move. `executePathMoves` inside `persistGraphChange` stays.
- `tests/Shared.Tests/DocumentPathMoveTests.fs` — ``sameWorkspaceMoves keeps same-workspace file and drops cross-workspace and workspace-root moves``.
- `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — ``same-workspace relocate runs git mv in the Workspace repository``.
