# 07 — Move an owned node across workspaces
**Status**: defined
One job: The Server deletes an owned File or Directory from the source Workspace repository and adds it to the destination Workspace repository, and it commits each repository when that move runs. Story 10 Move into a different Workspace in [Node move and disk](plan/node-move-disk/spec.md) is that move.

Type: coding
Blockers: [05 — Relocate in the same workspace](../05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md), [06 — Keep a nested node whose derived path differs](../06-keep-a-nested-node-whose-derived-path-differs/06-keep-a-nested-node-whose-derived-path-differs-ticket.md)

## Scope

### Signatures

[x] `DocumentPathMove.crossWorkspaceMoves` is absent from `src/Shared/DocumentPathMove.fs`. [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Path class** requires type `Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list`.
[x] `WorkspaceGit.moveAcrossWorkspaces` is absent from `src/Server/WorkspaceGit.fs`. [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Cross-workspace move** requires type `string -> string -> string -> string -> Result<unit, string>` for source repository root, source git path, destination repository root, and destination git path.
[x] `GraphQuery.enclosingWorkspace` in `src/Shared/GraphQuery.fs` has type `Graph -> NodeId -> NodeId option` and returns the Workspace id or `Graph.rootId` for ROOT.
[x] `DocumentPersistPath.repositoryRoot` and `DocumentPersistPath.gitPath` are in `src/Server/DocumentPersistPath.fs`. [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md) added them. This slice reuses them and does not reimplement them.
[x] `GitSave.runGit` in `src/Server/GitSave.fs` has type `string -> string -> Result<string, string>` and calls `GitRun.gitExec`.
[x] `GitSave.commitAll` in `src/Server/GitSave.fs` commits with `-c user.email=gambol@save -c user.name=gambol commit -m "<message>"` after `add -A` (with nested-repo excludes).
[x] `WorkspaceGit.commitAll` in `src/Server/WorkspaceGit.fs` forwards to `GitSave.commitAll` with the same gambol identity flags.

### Match sites

[ ] `DocumentPathMove.planPathMovesBetweenGraphs` in `src/Shared/DocumentPathMove.fs` feeds the move list. `crossWorkspaceMoves` filters that list after coalesce in the seam test caller, the same pattern as [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md) for `sameWorkspaceMoves`.
[ ] `crossWorkspaceMoves` calls `GraphQuery.enclosingWorkspace` on `move.nodeId` for the pre graph and the post graph.
[ ] `crossWorkspaceMoves` keeps a move when `Map.tryFind move.nodeId` is `Special File` or `Special Directory` on both graphs, both enclosing Workspace ids are `Some`, and those ids differ.
[ ] `crossWorkspaceMoves` drops a move when enclosing Workspace ids are equal, when either enclosing Workspace id is `None`, or when the node is `Special Workspace`.
[x] `DocumentPartition.isDocumentRootNode` in `src/Shared/DocumentPartition.fs` is true for `Special Workspace`, `Special Directory`, and `Special File`.
[ ] `crossWorkspaceMoves` excludes `Special Workspace` so a Workspace Node is in neither path-class list.
[ ] `moveAcrossWorkspaces` has no production caller in this slice. The seam test is the caller: it classifies with `crossWorkspaceMoves`, resolves four paths with `repositoryRoot` and `gitPath` on pre and post graphs, places the artifact on disk under the destination work tree, then calls `moveAcrossWorkspaces` once.
[ ] `moveAcrossWorkspaces` distinguishes File from Directory with `File.Exists` and `Directory.Exists` on `Path.Combine(destinationRepositoryRoot, destinationGitPath)` because the destination work tree already holds the moved artifact.
[x] `DocumentPersistChange.executePathMoves` stays unchanged by this slice. It folds `executeCoalescedMove`, which takes `NodeLockPool.withLockResult` and then `executePathMove`. `persistGraphChange` still relocates with `File.Move` and `Directory.Move`. This slice does not wire cross-workspace git into `persistGraphChange`.
[x] `FileAgent.processPostEvents` and `DbAgent.finishAppliedPostChange` stay unchanged. [Node move and disk architecture](plan/node-move-disk/arch.md) §8 question 5 names Post change as the later relocate caller; this slice does not add that caller.

### Existing tests

[x] ``planPathMovesBetweenGraphs includes file reparent across workspaces`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` builds `home` and `other` Workspace nodes, reparents a File from `//home/readme.txt` to `//other/readme.txt`, and asserts one `DocumentPathMove`.
[x] ``enclosingWorkspace resolves named workspace and canonical roots`` in `tests/Shared.Tests/GraphQueryTests.fs` checks `GraphQuery.enclosingWorkspace` for a named Workspace, descendants, `Graph.rootId`, and `Graph.workspacesId`.
[x] ``persistGraphChange moves renamed file artifact before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` checks `persistGraphChange` uses on-disk `File.Move` for a same-Workspace rename.
[x] ``persistGraphChange moves renamed directory tree before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` checks directory rename relocation on disk.
[x] ``persistGraphChange rejects destination artifact conflict`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` checks `validateDestinationAvailable` rejects an occupied destination.
[x] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` checks coalesce keeps the directory move for `docs` to `archive`.
[x] [Node move and disk architecture](plan/node-move-disk/arch.md) §6 seam 8 names ``cross-workspace move deletes from the source repository and adds to the destination repository`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs`. That test is absent today.

### Copy-from

[ ] [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md) specifies `sameWorkspaceMoves` as the complement classifier in `src/Shared/DocumentPathMove.fs`. `crossWorkspaceMoves` mirrors its `GraphQuery.enclosingWorkspace` checks with the keep/drop condition inverted for differing Workspace ids.
[ ] `WorkspaceGit.gitMove` in [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md) is one `GitSave.runGit` call with `mv --`, old path, and new path. `moveAcrossWorkspaces` uses the same `GitSave.runGit` entry point with `rm`, `rm -r`, and `add` instead of `mv`.
[ ] ``same-workspace relocate runs git mv in the Workspace repository`` in [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md) is the seam-test caller pattern: initialize repos with `WorkspaceGit.ensureInit`, seed git paths, classify, resolve `repositoryRoot` and `gitPath`, call `gitMove`, assert with `ls-files`.
[x] `WorkspaceGit.ensureInit` in `src/Server/WorkspaceGit.fs` creates `workspaceRoot/.git` and is the init path `DocumentPersistWrite.ensureWorkspaceRepo` uses.
[x] `GitSave.commitAll` in `src/Server/GitSave.fs` is the copy-from for path-scoped commit flags. Recommended shape for each repository commit in `moveAcrossWorkspaces`: stage only the moved path (`rm -- <path>`, `rm -r -- <path>`, or `add -- <path>`), then `runGit repositoryRoot "-c user.email=gambol@save -c user.name=gambol commit -m \"gambol: cross-workspace move\" -- <path>"` so unrelated dirty files stay uncommitted.
[x] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` already imports `Gambol.Server.Tests.TestBackend` `newTempDir` and defines `graphWithNestedDocs` for disk layout under `home/docs`.
[x] `tests/Shared.Tests/DocumentPathMoveTests.fs` `graphWithWorkspaceFile` builds the `home` plus File graph used in ``planPathMovesBetweenGraphs includes file reparent across workspaces``.

### write-ticket

#### Source files

[ ] `src/Shared/DocumentPathMove.fs` — add `crossWorkspaceMoves`.
[ ] `src/Server/WorkspaceGit.fs` — add `moveAcrossWorkspaces` (source delete, destination add, commit at move time, optional recovery on failed destination add).
[ ] Distinct source files for this slice: 2 (`src/Shared/DocumentPathMove.fs`, `src/Server/WorkspaceGit.fs`). `DocumentPersistPath.repositoryRoot` and `gitPath` land in [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md); this slice depends on them and does not expand `src/Server/DocumentPersistPath.fs` unless [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md) is not merged yet, in which case stop and name that blocker.
[x] `src/Server/GitSave.fs`, `src/Server/DocumentPersistChange.fs`, `src/Server/Core/FileAgent.fs`, `src/Server/Core/DbAgent.fs`, `src/Shared/ChangeAmendment.fs`, and `src/Server/NodeLockPool.fs` stay unchanged by this slice. `NodeEnclosureLock.fs` is absent.
[ ] `coalescePathMoves` stays unchanged. Nested derived-path coalesce is [06 — Keep a nested node whose derived path differs](plan/node-move-disk/issues/06-keep-a-nested-node-whose-derived-path-differs/06-keep-a-nested-node-whose-derived-path-differs-ticket.md).

#### Tests

[ ] `tests/Shared.Tests/DocumentPathMoveTests.fs` — add ``crossWorkspaceMoves keeps a cross-workspace move and drops a same-workspace move and a workspace move``. Reuse `graphWithWorkspaceFile` and the second-Workspace graph from ``planPathMovesBetweenGraphs includes file reparent across workspaces``. Assert the `//home/readme.txt` to `//other/readme.txt` move stays; a same-Workspace rename drops; a `Special Workspace` move drops.
[ ] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — add [Node move and disk architecture](plan/node-move-disk/arch.md) §6 seam 8 ``cross-workspace move deletes from the source repository and adds to the destination repository``. Two Workspace repos under one data directory, track `readme.txt` in `home`, reparent to `other`, `crossWorkspaceMoves` returns one move, move the file on disk to `other/readme.txt`, resolve four git paths, one `moveAcrossWorkspaces`, then `GitSave.runGit` `ls-files` shows the path absent in `home` and present in `other`.
[ ] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — add ``moveAcrossWorkspaces commits each repository at move time``. After a successful cross-workspace move, `git log -1 --oneline` in each repository shows a gambol commit, and unrelated dirty files in either work tree stay unstaged.
[ ] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — add ``moveAcrossWorkspaces restores the source repository when the destination add fails``. Force a destination `add` or `commit` failure after the source delete commit; assert recovery puts the file back in the source repository index (re-add from the last source commit or equivalent inside `moveAcrossWorkspaces`).
[ ] Distinct tests added or changed for this slice: 4 (within [Size limit](plan/context/size-limit.md)).

#### Ticket 02 facts

[ ] **02 — Moving an owned File or Directory into a different Workspace** Expected still matches [Node move and disk architecture](plan/node-move-disk/arch.md): owned File or Directory under a different Workspace Node lands at the derived path; the tree matches the graph; delete from the source Workspace repository and add to the destination Workspace repository.
[ ] Ticket 02 Observed facts on Owner chain, `expandedPathForNodeId`, `planPathMovesBetweenGraphs`, ``planPathMovesBetweenGraphs includes file reparent across workspaces``, Accept validation, separate git roots (`WorkspaceGit.ensureInit`, `GitSave.isRepo`, nested-repo excludes), and `validatePathMoves` / `disk path already exists` still match the code and arch.
[ ] Ticket 02 Expected line on **ChangeAmendment** disambiguating move conflicts is replaced by [Node move and disk architecture](plan/node-move-disk/arch.md) flow 5 **Change conflict** and §8 questions 3–6: Post change runs path validation, then hands rename ops to `ChangeAmendment.applyPosted` without disk in shared code. This slice does not implement that amendment.
[ ] Ticket 02 Observed on `executePathMoves` with `File.Move` and `Directory.Move`, `persistGraphChange` calling `executePathMoves`, and **Persist thread write** leaving the old path while writing the post-graph artifact remains true for today's `persistGraphChange` path. Cross-workspace git in flow 4 supersedes that story for Workspace-boundary moves once Post change becomes the relocate caller; this slice only adds `crossWorkspaceMoves` and `moveAcrossWorkspaces` without changing `persistGraphChange`.
[ ] Ticket 02 Notes ("Delete from one. Add to one.") match [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Cross-workspace move** and open question 2 commit-at-move-time.

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

[x] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Path class**, **Cross-workspace move**, and **Workspace repository**, §4 flow 4 **Cross-workspace move**, §6 seam 8, and §8 question 2.
[ ] [02 — Moving an owned File or Directory into a different Workspace](https://github.com/abaljeu/ambit/blob/f561ed4b993e2637dcbcbf11dbf2056a9e87116a/plan/node-move-disk/issues/02-move-owned-file-or-directory-into-different-workspace.md) on `cursor/move-owned-node-across-workspace-3738`. The conflict sentence in that ticket is flow 5 **Change conflict** in the architecture.
[x] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

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

[ ] Record the stop at `plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces/07-build.md`, then ask.
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
[ ] Write the verdict to `plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces/07-build.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
