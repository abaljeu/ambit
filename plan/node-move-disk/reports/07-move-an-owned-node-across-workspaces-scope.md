# 07 — Move an owned node across workspaces scope

## Signatures

[ ] `DocumentPathMove.crossWorkspaceMoves` is absent from `src/Shared/DocumentPathMove.fs`. [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Path class** requires type `Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list`.
[ ] `WorkspaceGit.moveAcrossWorkspaces` is absent from `src/Server/WorkspaceGit.fs`. [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Cross-workspace move** requires type `string -> string -> string -> string -> Result<unit, string>` for source repository root, source git path, destination repository root, and destination git path.
[ ] `GraphQuery.enclosingWorkspace` in `src/Shared/GraphQuery.fs` has type `Graph -> NodeId -> NodeId option` and returns the Workspace id or `Graph.rootId` for ROOT.
[ ] `DocumentPersistPath.repositoryRoot` and `DocumentPersistPath.gitPath` are specified in [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md) and are absent from `src/Server/DocumentPersistPath.fs` today. This slice reuses them and does not reimplement them.
[ ] `GitSave.runGit` in `src/Server/GitSave.fs` has type `string -> string -> Result<string, string>` and calls `GitRun.gitExec`.
[ ] `GitSave.commitAll` in `src/Server/GitSave.fs` commits with `-c user.email=gambol@save -c user.name=gambol commit -m "<message>"` after `add -A` (with nested-repo excludes).
[ ] `WorkspaceGit.commitAll` in `src/Server/WorkspaceGit.fs` forwards to `GitSave.commitAll` with the same gambol identity flags.

## Match sites

[ ] `DocumentPathMove.planPathMovesBetweenGraphs` in `src/Shared/DocumentPathMove.fs` feeds the move list. `crossWorkspaceMoves` filters that list after coalesce in the seam test caller, the same pattern as [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md) for `sameWorkspaceMoves`.
[ ] `crossWorkspaceMoves` calls `GraphQuery.enclosingWorkspace` on `move.nodeId` for the pre graph and the post graph.
[ ] `crossWorkspaceMoves` keeps a move when `Map.tryFind move.nodeId` is `Special File` or `Special Directory` on both graphs, both enclosing Workspace ids are `Some`, and those ids differ.
[ ] `crossWorkspaceMoves` drops a move when enclosing Workspace ids are equal, when either enclosing Workspace id is `None`, or when the node is `Special Workspace`.
[ ] `DocumentPartition.isDocumentRootNode` in `src/Shared/DocumentPartition.fs` is true for `Special Workspace`, `Special Directory`, and `Special File`. Classification still excludes `Special Workspace` explicitly so a Workspace Node is in neither path-class list.
[ ] `moveAcrossWorkspaces` has no production caller in this slice. The seam test is the caller: it classifies with `crossWorkspaceMoves`, resolves four paths with `repositoryRoot` and `gitPath` on pre and post graphs, places the artifact on disk under the destination work tree, then calls `moveAcrossWorkspaces` once.
[ ] `moveAcrossWorkspaces` distinguishes File from Directory with `File.Exists` and `Directory.Exists` on `Path.Combine(destinationRepositoryRoot, destinationGitPath)` because the destination work tree already holds the moved artifact.
[ ] `DocumentPersistChange.executePathMoves` in `src/Server/DocumentPersistChange.fs` stays unchanged. `persistGraphChange` still runs `File.Move` and `Directory.Move` through `executePathMoves`. This slice does not wire cross-workspace git into `persistGraphChange`.
[ ] `FileAgent.processPostEvents` and `DbAgent.finishAppliedPostChange` stay unchanged. [Node move and disk architecture](plan/node-move-disk/arch.md) §8 question 5 names Post change as the later relocate caller; this slice does not add that caller.

## Existing tests

[ ] ``planPathMovesBetweenGraphs includes file reparent across workspaces`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` builds `home` and `other` Workspace nodes, reparents a File from `//home/readme.txt` to `//other/readme.txt`, and asserts one `DocumentPathMove`.
[ ] ``enclosingWorkspace resolves named workspace and canonical roots`` in `tests/Shared.Tests/GraphQueryTests.fs` checks `GraphQuery.enclosingWorkspace` for a named Workspace, descendants, `Graph.rootId`, and `Graph.workspacesId`.
[ ] ``persistGraphChange moves renamed file artifact before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` checks `persistGraphChange` uses on-disk `File.Move` for a same-Workspace rename.
[ ] ``persistGraphChange moves renamed directory tree before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` checks directory rename relocation on disk.
[ ] ``persistGraphChange rejects destination artifact conflict`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` checks `validateDestinationAvailable` rejects an occupied destination.
[ ] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` checks coalesce keeps the directory move for `docs` to `archive`.
[ ] [Node move and disk architecture](plan/node-move-disk/arch.md) §6 seam 8 names ``cross-workspace move deletes from the source repository and adds to the destination repository`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs`. That test is absent today.

## Copy-from

[ ] [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md) specifies `sameWorkspaceMoves` as the complement classifier in `src/Shared/DocumentPathMove.fs`. `crossWorkspaceMoves` mirrors its `GraphQuery.enclosingWorkspace` checks with the keep/drop condition inverted for differing Workspace ids.
[ ] `WorkspaceGit.gitMove` in [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md) is one `GitSave.runGit` call with `mv --`, old path, and new path. `moveAcrossWorkspaces` uses the same `GitSave.runGit` entry point with `rm`, `rm -r`, and `add` instead of `mv`.
[ ] ``same-workspace relocate runs git mv in the Workspace repository`` in [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md) is the seam-test caller pattern: initialize repos with `WorkspaceGit.ensureInit`, seed git paths, classify, resolve `repositoryRoot` and `gitPath`, call `gitMove`, assert with `ls-files`.
[ ] `WorkspaceGit.ensureInit` in `src/Server/WorkspaceGit.fs` creates `workspaceRoot/.git` and is the init path `DocumentPersistWrite.ensureWorkspaceRepo` uses.
[ ] `GitSave.commitAll` in `src/Server/GitSave.fs` is the copy-from for path-scoped commit flags. Recommended shape for each repository commit in `moveAcrossWorkspaces`: stage only the moved path (`rm -- <path>`, `rm -r -- <path>`, or `add -- <path>`), then `runGit repositoryRoot "-c user.email=gambol@save -c user.name=gambol commit -m \"gambol: cross-workspace move\" -- <path>"` so unrelated dirty files stay uncommitted.
[ ] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` already imports `Gambol.Server.Tests.TestBackend` `newTempDir` and defines `graphWithNestedDocs` for disk layout under `home/docs`.
[ ] `tests/Shared.Tests/DocumentPathMoveTests.fs` `graphWithWorkspaceFile` builds the `home` plus File graph used in ``planPathMovesBetweenGraphs includes file reparent across workspaces``.

## write-ticket

### Source files

[ ] `src/Shared/DocumentPathMove.fs` — add `crossWorkspaceMoves`.
[ ] `src/Server/WorkspaceGit.fs` — add `moveAcrossWorkspaces` (source delete, destination add, commit at move time, optional recovery on failed destination add).
[ ] Distinct source files for this slice: 2 (`src/Shared/DocumentPathMove.fs`, `src/Server/WorkspaceGit.fs`). `DocumentPersistPath.repositoryRoot` and `gitPath` land in [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md); this slice depends on them and does not expand `src/Server/DocumentPersistPath.fs` unless [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md) is not merged yet, in which case stop and name that blocker.
[ ] `src/Server/GitSave.fs`, `src/Server/DocumentPersistChange.fs`, `src/Server/Core/FileAgent.fs`, `src/Server/Core/DbAgent.fs`, `src/Shared/ChangeAmendment.fs`, and `src/Server/NodeEnclosureLock.fs` stay unchanged.
[ ] `coalescePathMoves` stays unchanged. Nested derived-path coalesce is [06 — Keep a nested node whose derived path differs](plan/node-move-disk/issues/06-keep-a-nested-node-whose-derived-path-differs.md).

### Tests

[ ] `tests/Shared.Tests/DocumentPathMoveTests.fs` — add ``crossWorkspaceMoves keeps a cross-workspace move and drops a same-workspace move and a workspace move``. Reuse `graphWithWorkspaceFile` and the second-Workspace graph from ``planPathMovesBetweenGraphs includes file reparent across workspaces``. Assert the `//home/readme.txt` to `//other/readme.txt` move stays; a same-Workspace rename drops; a `Special Workspace` move drops.
[ ] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — add [Node move and disk architecture](plan/node-move-disk/arch.md) §6 seam 8 ``cross-workspace move deletes from the source repository and adds to the destination repository``. Two Workspace repos under one data directory, track `readme.txt` in `home`, reparent to `other`, `crossWorkspaceMoves` returns one move, move the file on disk to `other/readme.txt`, resolve four git paths, one `moveAcrossWorkspaces`, then `GitSave.runGit` `ls-files` shows the path absent in `home` and present in `other`.
[ ] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — add ``moveAcrossWorkspaces commits each repository at move time``. After a successful cross-workspace move, `git log -1 --oneline` in each repository shows a gambol commit, and unrelated dirty files in either work tree stay unstaged.
[ ] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — add ``moveAcrossWorkspaces restores the source repository when the destination add fails``. Force a destination `add` or `commit` failure after the source delete commit; assert recovery puts the file back in the source repository index (re-add from the last source commit or equivalent inside `moveAcrossWorkspaces`).
[ ] Distinct tests added or changed for this slice: 4 (within [Size limit](plan/context/size-limit.md)).

### Ticket 02 facts

[ ] **02 — Moving an owned File or Directory into a different Workspace** Expected still matches [Node move and disk architecture](plan/node-move-disk/arch.md): owned File or Directory under a different Workspace Node lands at the derived path; the tree matches the graph; delete from the source Workspace repository and add to the destination Workspace repository.
[ ] Ticket 02 Observed facts on Owner chain, `expandedPathForNodeId`, `planPathMovesBetweenGraphs`, ``planPathMovesBetweenGraphs includes file reparent across workspaces``, Accept validation, separate git roots (`WorkspaceGit.ensureInit`, `GitSave.isRepo`, nested-repo excludes), and `validatePathMoves` / `disk path already exists` still match the code and arch.
[ ] Ticket 02 Expected line on **ChangeAmendment** disambiguating move conflicts is replaced by [Node move and disk architecture](plan/node-move-disk/arch.md) flow 5 **Change conflict** and §8 questions 3–6: Post change runs path validation, then hands rename ops to `ChangeAmendment.applyPosted` without disk in shared code. This slice does not implement that amendment.
[ ] Ticket 02 Observed on `executePathMoves` with `File.Move` and `Directory.Move`, `persistGraphChange` calling `executePathMoves`, and **Persist thread write** leaving the old path while writing the post-graph artifact remains true for today's `persistGraphChange` path. Cross-workspace git in flow 4 supersedes that story for Workspace-boundary moves once Post change becomes the relocate caller; this slice only adds `crossWorkspaceMoves` and `moveAcrossWorkspaces` without changing `persistGraphChange`.
[ ] Ticket 02 Notes ("Delete from one. Add to one.") match [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Cross-workspace move** and open question 2 commit-at-move-time.
