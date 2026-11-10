# 07 — Move an owned node across workspaces
**Status**: defined
One job: The Server deletes an owned File or Directory from the source Workspace repository and adds it to the destination Workspace repository, and it commits each repository when that move runs. Story 10 Move into a different Workspace in [Node move and disk](plan/node-move-disk/spec.md) is that move.

Type: coding
Blockers: [05 — Relocate in the same workspace](05-relocate-in-the-same-workspace.md), [06 — Keep a nested node whose derived path differs](06-keep-a-nested-node-whose-derived-path-differs.md)

## Inputs

### Working

#### Path class

[ ] `DocumentPathMove.crossWorkspaceMoves` is new in `src/Shared/DocumentPathMove.fs`.
[ ] `crossWorkspaceMoves` has type `Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list`.
[ ] The first `Graph` is the pre graph and the second `Graph` is the post graph.
[ ] `crossWorkspaceMoves` keeps a `Special File` or `Special Directory` whose `GraphQuery.enclosingWorkspace` id is `Some` on both graphs and those ids differ.
[ ] `crossWorkspaceMoves` drops a move whose enclosing Workspace ids are equal.
[ ] `crossWorkspaceMoves` drops a move when either enclosing Workspace id is `None`.
[ ] `crossWorkspaceMoves` drops a `Special Workspace` node.
[ ] `crossWorkspaceMoves` keeps the input order of the moves it keeps.
[ ] `crossWorkspaceMoves` reads no parse state.
[ ] `GraphQuery.enclosingWorkspace` in `src/Shared/GraphQuery.fs` has type `Graph -> NodeId -> NodeId option`.

#### Cross-workspace move

[ ] `WorkspaceGit.moveAcrossWorkspaces` is new in `src/Server/WorkspaceGit.fs`.
[ ] `moveAcrossWorkspaces` has type `string -> string -> string -> string -> Result<unit, string>`.
[ ] The arguments are the source repository root, the source git path, the destination repository root, and the destination git path.
[ ] The destination work tree already holds the File or Directory at the destination git path before `moveAcrossWorkspaces` runs.
[ ] When that destination path is missing, `moveAcrossWorkspaces` returns Error and runs no git command.
[ ] A File destination uses `git rm --` in the source and `git add --` in the destination.
[ ] A Directory destination uses `git rm -r --` in the source and `git add --` in the destination.
[ ] `File.Exists` is the File check and `Directory.Exists` is the Directory check, on the destination path.
[ ] The source delete is committed, then the destination add is committed.
[ ] Each commit is one `GitSave.runGit` call: `-c user.email=gambol@save -c user.name=gambol commit -m "gambol: cross-workspace move" --` and that repository's git path.
[ ] `GitSave.commitAll` stays the `add -A` commit. This ticket uses the path-scoped commit above so other dirty files stay uncommitted.
[ ] One cross-workspace move is one source delete and one destination add.
[ ] `GitSave.runGit` in `src/Server/GitSave.fs` has type `string -> string -> Result<string, string>`.
[ ] The repository root is `DocumentPersistPath.repositoryRoot` and the git path is `DocumentPersistPath.gitPath`, both from the blocker ticket 05.
[ ] When the destination add or the destination commit returns Error, `moveAcrossWorkspaces` runs `checkout HEAD~1 --` on the source git path and commits that restore with the same commit form.
[ ] When that restore commit returns Ok, `moveAcrossWorkspaces` returns the destination Error.
[ ] When that restore commit returns Error, `moveAcrossWorkspaces` returns the restore Error.

#### Copy-from

[ ] `sameWorkspaceMoves` from the blocker ticket 05 is the complement classifier.
[ ] `WorkspaceGit.gitMove` from the blocker ticket 05 is one `GitSave.runGit` call.
[ ] `GitSave.commitAll` in `src/Server/GitSave.fs` is the gambol identity `-c user.email=gambol@save -c user.name=gambol`.
[ ] ``planPathMovesBetweenGraphs includes file reparent across workspaces`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` builds the `//home/readme.txt` to `//other/readme.txt` move.

#### Ticket 02 text that still matches

[ ] A person moves an owned File Node or an owned Directory Node under a different Workspace Node.
[ ] The file or directory on disk is at the derived path under that Workspace Node.
[ ] Each owned File Node and Directory Node under a moved Directory Node is at its derived path.
[ ] This holds when the moved node is a direct child or deeper on the Owner chain, and when the File Node is parsed or unparsed.
[ ] The file or directory is deleted from the source Workspace repository and added to the destination Workspace repository.

#### Match sites

[ ] This ticket adds no union case.
[ ] This ticket changes no existing public signature.
[ ] `moveAcrossWorkspaces` and `crossWorkspaceMoves` have no production caller in this ticket.
[ ] The seam test is the caller.

#### Tests the ticket leaves as they are

[ ] ``persistGraphChange moves renamed file artifact before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` checks `File.Move` for a same-Workspace rename.
[ ] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` expects the nested File move to be absent when the Directory rename produces that File path.

### Reference

[ ] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Path class**, **Cross-workspace move**, and **Workspace repository**, §4 flow 4 **Cross-workspace move**, §6 seam 8, and §8 question 2.
[ ] [02 — Moving an owned File or Directory into a different Workspace](https://github.com/abaljeu/ambit/blob/f561ed4b993e2637dcbcbf11dbf2056a9e87116a/plan/node-move-disk/issues/02-move-owned-file-or-directory-into-different-workspace.md) on `cursor/move-owned-node-across-workspace-3738`. The conflict sentence in that ticket is flow 5 **Change conflict** in the architecture.
[ ] [07 — Move an owned node across workspaces scope](plan/node-move-disk/reports/07-move-an-owned-node-across-workspaces-scope.md).
[ ] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[ ] `src/Server/DocumentPersistPath.fs` stays as the blocker ticket 05 leaves it.
[ ] `src/Server/GitSave.fs` stays as it is.
[ ] `src/Shared/DocumentPathMove.fs` `coalescePathMoves` stays as the blocker ticket 06 leaves it.
[ ] `src/Server/DocumentPersistChange.fs` stays as it is. `executePathMoves` keeps `File.Move` and `Directory.Move`.
[ ] `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` stay as they are.
[ ] `src/Shared/ChangeAmendment.fs` stays as it is.
[ ] `src/Server/NodeLockPool.fs` stays as it is.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand `src/Shared/DocumentPathMove.fs` with `crossWorkspaceMoves` as Working specifies.
3. [ ] Expand `src/Server/WorkspaceGit.fs` with `moveAcrossWorkspaces` as Working specifies.
4. [ ] Expand `tests/Shared.Tests/DocumentPathMoveTests.fs` with the classification test in Outputs.
5. [ ] Expand `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` with the three server tests in Outputs. Reuse `newTempDir` and `WorkspaceGit.ensureInit`.

## Outputs

[ ] `src/Shared/DocumentPathMove.fs` — `crossWorkspaceMoves`.
[ ] `src/Server/WorkspaceGit.fs` — `moveAcrossWorkspaces`.
[ ] Distinct source files: 2. `src/Shared/DocumentPathMove.fs` and `src/Server/WorkspaceGit.fs`.
[ ] `tests/Shared.Tests/DocumentPathMoveTests.fs` — the classification test below.
[ ] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — the three tests below.
[ ] ``crossWorkspaceMoves keeps a cross-workspace move and drops a same-workspace move and a workspace move`` — the `//home/readme.txt` to `//other/readme.txt` move stays; a same-Workspace File rename drops; a `Special Workspace` move drops. A Directory rename inside `home` plus a nested File reparent to `other` keeps that File in `coalescePathMoves` and in `crossWorkspaceMoves`, and the Directory move stays out of `crossWorkspaceMoves`.
[ ] ``cross-workspace move deletes from the source repository and adds to the destination repository`` — Directory `docs` with `readme.txt` is tracked in the `home` repository. The post graph owns that Directory under `other`. The destination work tree holds `docs/readme.txt` before the call. `crossWorkspaceMoves` returns that Directory. One `moveAcrossWorkspaces` uses `git rm -r`. `GitSave.runGit` `ls-files` omits `docs/readme.txt` in `home` and lists it in `other`.
[ ] ``moveAcrossWorkspaces commits each repository at move time`` — after a File move, `git log -1 --pretty=%s` in each repository is `gambol: cross-workspace move`. A dirty file that is outside the moved path stays unstaged in `git status --porcelain`.
[ ] ``moveAcrossWorkspaces restores the source repository when the destination commit fails`` — the source delete commits, then the destination commit fails because `.git/hooks/pre-commit` exits 1. `ls-files` in the source lists the moved path again, and `moveAcrossWorkspaces` returns Error.

## Stop when

[ ] Record the stop at `plan/node-move-disk/reports/07-move-an-owned-node-across-workspaces-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit changes `coalescePathMoves`, `repositoryRoot`, `gitPath`, `gitMove`, `executePathMoves`, `FileAgent`, `DbAgent`, or `ChangeAmendment`.
[ ] Stop when the edit contradicts story 10 Move into a different Workspace in the spec.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``crossWorkspaceMoves keeps a cross-workspace move and drops a same-workspace move and a workspace move``
  - `tests/Server.Tests`: ``cross-workspace move deletes from the source repository and adds to the destination repository``
  - `tests/Server.Tests`: ``moveAcrossWorkspaces commits each repository at move time``
  - `tests/Server.Tests`: ``moveAcrossWorkspaces restores the source repository when the destination commit fails``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/node-move-disk/reports/07-move-an-owned-node-across-workspaces-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
