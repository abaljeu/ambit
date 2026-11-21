# 05 — Relocate in the same workspace

Ticket: [[05-relocate-in-the-same-workspace-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

Check: pass

## 1. Standards

- **Hard** — `.agents/rules/fsharp-source.md`: 100 characters or less per line on source code. ``sameWorkspaceMoves keeps a same-workspace move and drops a cross-workspace move and a workspace move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` is the Outputs name. That name is 100 characters. The `let` line that binds it is 113 characters. The name stays, because Outputs names that filter string.

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

- `bash scripts/check-test.sh 10940` printed `DONE` and `/tmp/ambit-full-suite-20261112-231108.log`. Server.Tests passed 781, failed 0, skipped 1, total 782. Shared.Tests passed 1937, failed 0, skipped 3, total 1940.
- ``sameWorkspaceMoves keeps a same-workspace move and drops a cross-workspace move and a workspace move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` keeps a File rename and a Directory rename under the named Workspace `home`, and a File rename under `Graph.rootId`. It drops the `//home/readme.txt` to `//other/readme.txt` move, a `Special Workspace` move, and a detached File whose `GraphQuery.enclosingWorkspace` id is `None` on the pre graph and the post graph. Kept order is the input order.
- ``repository root and git path follow the workspace and ROOT`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` checks a File under `home`: `DocumentPersistPath.repositoryRoot` equals `DocumentPersistPath.workspaceRootFor`, and `DocumentPersistPath.gitPath` is `docs/readme.txt`. For a File owned by `Graph.rootId`, `repositoryRoot` is `DocumentPersistPath.dataDirBase` and `gitPath` is `root.txt`. For a Directory owned by `Graph.rootId`, `gitPath` is `notes/` and `repositoryRoot` is `dataDirBase`. `gitPath` of the Workspace node is `None`. A detached File has `enclosingWorkspace` `None`, `repositoryRoot` `None`, and `gitPath` `None`.
- ``same-workspace relocate runs git mv in the Workspace repository`` sets the File `home/docs/readme.txt` to `ParseState.Unparsed` and places it under the Workspace Node. `DocumentPathMove.sameWorkspaceMoves` returns that File. `repositoryRoot` is the `home` repository. `gitPath` is `docs/readme.txt` before the move and `readme.txt` after. One `WorkspaceGit.gitMove` leaves `readme.txt` on disk, and `GitSave.runGit` `ls-files` lists `readme.txt`.
- ``gitMove moves a directory tree with one git mv`` checks `gitPath` `docs/` for the Directory. One `WorkspaceGit.gitMove` from `docs/` to `archive/` leaves `archive/readme.txt` in the repository.

## 5. Miss

- No findings.

## 6. Found elsewhere

The following issues were found beyond the ticket scope.  The fixes are suggested.

- 1. **Relocate caller** — Accept.
  - **Problem.** [Node move and disk architecture](../../arch.md) §8 question 5 said `gitMove` and `moveAcrossWorkspaces` do not exist yet.
  - **Fix.** Question 5 now says `WorkspaceGit.gitMove` in `src/Server/WorkspaceGit.fs` is the same-Workspace move. `moveAcrossWorkspaces` stays absent.
- 2. **Research ticket** — Accept.
  - **Problem.** 03 — Research directory-level persist gate said `gitMove` is absent.
  - **Fix.** That ticket now names `WorkspaceGit.gitMove` in `src/Server/WorkspaceGit.fs`. The Move write gate checkbox stays open. `moveAcrossWorkspaces` stays absent.
- 3. **Later tickets** — Accept.
  - **Problem.** 06 — Keep a nested node whose derived path differs, 07 — Move an owned node across workspaces, and 09 — Relocate under the node lock still described `sameWorkspaceMoves`, `repositoryRoot`, `gitPath`, or `gitMove` as absent.
  - **Fix.** Those files now say the four symbols are in source. 09 — Relocate under the node lock still waits on 06 — Keep a nested node whose derived path differs, 07 — Move an owned node across workspaces, and 08 — Amend a change conflict before the graph is stored.
