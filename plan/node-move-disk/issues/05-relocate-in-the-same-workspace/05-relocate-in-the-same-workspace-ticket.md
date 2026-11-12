# 05 — Relocate in the same workspace
**Status**: defined
One job: The Server moves a File or a Directory that stays under the same Workspace Node with one `git mv`, so git tracks that move. Story 2 Relocate with git mv in [Node move and disk](plan/node-move-disk/spec.md) is that move, and it includes a File whose strictly enclosing artifact becomes the Workspace Node.

Type: coding
Blockers: [04 — Expand the node enclosure lock](../04-expand-the-node-enclosure-lock/04-expand-the-node-enclosure-lock-ticket.md)

## Inputs

### Working

#### Path class

[ ] `DocumentPathMove.sameWorkspaceMoves` is new in `src/Shared/DocumentPathMove.fs`.
[ ] `sameWorkspaceMoves` has type `Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list`.
[ ] The first `Graph` is the pre graph and the second `Graph` is the post graph.
[ ] `sameWorkspaceMoves` keeps a `Special File` or `Special Directory` whose `GraphQuery.enclosingWorkspace` id is `Some` on both graphs and those ids are equal.
[ ] `sameWorkspaceMoves` drops a move whose enclosing Workspace ids differ.
[ ] `sameWorkspaceMoves` drops a move when either enclosing Workspace id is `None`.
[ ] `sameWorkspaceMoves` drops a `Special Workspace` node.
[ ] `sameWorkspaceMoves` keeps the input order of the moves it keeps.
[ ] `sameWorkspaceMoves` reads no parse state.
[ ] `GraphQuery.enclosingWorkspace` in `src/Shared/GraphQuery.fs` has type `Graph -> NodeId -> NodeId option`.
[ ] `enclosingWorkspace` returns the Workspace id, including `Graph.rootId` for ROOT.

#### Workspace repository

[ ] `DocumentPersistPath.repositoryRoot` is new in `src/Server/DocumentPersistPath.fs`.
[ ] `repositoryRoot` has type `string -> Graph -> NodeId -> string option`.
[ ] For a named Workspace, `repositoryRoot` returns the same string as `DocumentPersistPath.workspaceRootFor`.
[ ] For enclosing Workspace `Graph.rootId`, `repositoryRoot` returns the data directory with trailing separators trimmed, the string `dataDirBase` already returns in that module.
[ ] When `enclosingWorkspace` is `None`, `repositoryRoot` returns `None`.
[ ] `DocumentPersistPath.gitPath` is new in `src/Server/DocumentPersistPath.fs`.
[ ] `gitPath` has type `Graph -> NodeId -> string option`.
[ ] For a File, the data-directory path is `DocumentPartition.artifactFileRelative`.
[ ] For a Directory, the data-directory path is `DocumentPartition.artifactDirectoryRelative`.
[ ] For a Workspace node, `gitPath` returns `None`.
[ ] For a named Workspace, `gitPath` is that data-directory path without the workspace directory name and the following slash.
[ ] For ROOT, `gitPath` is that data-directory path unchanged.
[ ] When the named-Workspace data-directory path lacks that prefix, `gitPath` returns `None`.

#### Same-workspace relocate

[ ] `WorkspaceGit.gitMove` is new in `src/Server/WorkspaceGit.fs`.
[ ] `gitMove` has type `string -> string -> string -> Result<unit, string>`.
[ ] The arguments are the repository root, the old git path, and the new git path.
[ ] `gitMove` makes one `GitSave.runGit` call whose arguments are `mv --`, the old git path, and the new git path.
[ ] `GitSave.runGit` in `src/Server/GitSave.fs` has type `string -> string -> Result<string, string>`.
[ ] One same-workspace move is one `gitMove` call.
[ ] `gitMove` of a Directory path moves that Directory tree.
[ ] `gitMove` takes no child list and no parse state.

#### Copy-from

[ ] `DocumentPersistChange.executePathMoves` in `src/Server/DocumentPersistChange.fs` relocates with `File.Move` and `Directory.Move`.
[ ] `DocumentPersistPath.workspaceRootFor` in `src/Server/DocumentPersistPath.fs` has type `string -> Graph -> NodeId -> string option` and returns `None` when the enclosing Workspace is ROOT.
[ ] `WorkspaceGit.ensureInit` in `src/Server/WorkspaceGit.fs` creates the Workspace repository the seam test initializes.
[ ] ``enclosingWorkspace resolves named workspace and canonical roots`` in `tests/Shared.Tests/GraphQueryTests.fs` checks `enclosingWorkspace` for a named Workspace and for ROOT.

#### Match sites

[ ] This ticket adds no union case.
[ ] This ticket changes no existing public signature.
[ ] `sameWorkspaceMoves`, `repositoryRoot`, `gitPath`, and `gitMove` have no production caller in this ticket.
[ ] The seam test is the caller: it classifies with `sameWorkspaceMoves`, resolves `repositoryRoot` and `gitPath`, and calls `gitMove` once.

#### Tests the ticket leaves as they are

[ ] ``planPathMovesBetweenGraphs includes file reparent across workspaces`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` checks a file move from `//home/readme.txt` to `//other/readme.txt`.
[ ] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` checks coalesce keeps the directory move for `docs` to `archive`.
[ ] ``persistGraphChange moves renamed file artifact before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` checks `persistGraphChange` moves the file on disk.
[ ] ``persistGraphChange moves renamed directory tree before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` checks `persistGraphChange` moves the directory tree on disk.

### Reference

[ ] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Path class**, **Same-workspace relocate**, and **Workspace repository**, §4 flow 2 **Same-workspace relocate**, §6 seam 7, and §8 question 1.
[ ] [05 — Relocate in the same workspace scope](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-build.md).
[ ] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[ ] `src/Shared/GraphQuery.fs` stays as it is.
[ ] `src/Server/DocumentPersistChange.fs` stays as it is. `executePathMoves` and `persistGraphChange` keep `File.Move` and `Directory.Move`.
[ ] `src/Server/GitSave.fs` stays as it is. `runGit` stays the git process call.
[ ] `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` stay as they are. Post change becomes the relocate caller in a later ticket.
[ ] `crossWorkspaceMoves` and `moveAcrossWorkspaces` stay absent.
[ ] `coalescePathMoves` stays as it is.
[ ] `src/Server/NodeLockPool.fs` stays as it is. This ticket takes no node lock.
[ ] `src/Shared/ChangeAmendment.fs` stays as it is.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand `src/Shared/DocumentPathMove.fs` with `sameWorkspaceMoves` as Working specifies.
3. [ ] Expand `src/Server/DocumentPersistPath.fs` with `repositoryRoot` and `gitPath` as Working specifies.
4. [ ] Expand `src/Server/WorkspaceGit.fs` with `gitMove` as Working specifies.
5. [ ] Expand `tests/Shared.Tests/DocumentPathMoveTests.fs` with the classification test in Outputs. Reuse the graph helpers already in that file.
6. [ ] Expand `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` with the three server tests in Outputs. Reuse `newTempDir`, `graphWithNestedDocs`, and `WorkspaceGit.ensureInit`.

## Outputs

[ ] `src/Shared/DocumentPathMove.fs` — `sameWorkspaceMoves`.
[ ] `src/Server/DocumentPersistPath.fs` — `repositoryRoot` and `gitPath`.
[ ] `src/Server/WorkspaceGit.fs` — `gitMove`.
[ ] Distinct source files: 3. `src/Shared/DocumentPathMove.fs`, `src/Server/DocumentPersistPath.fs`, and `src/Server/WorkspaceGit.fs`.
[ ] `tests/Shared.Tests/DocumentPathMoveTests.fs` — the classification test below.
[ ] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — the three tests below.
[ ] ``sameWorkspaceMoves keeps a same-workspace move and drops a cross-workspace move and a workspace move`` — a File rename under one named Workspace stays; the `//home/readme.txt` to `//other/readme.txt` move drops; a `DocumentPathMove` whose node is `Special Workspace` drops; a File rename under ROOT stays.
[ ] ``repository root and git path follow the workspace and ROOT`` — for a File under the named Workspace `home`, `repositoryRoot` equals `workspaceRootFor` and `gitPath` is `artifactFileRelative` without the `home/` prefix; for a File owned by `Graph.rootId`, `repositoryRoot` is the trimmed data directory and `gitPath` equals `artifactFileRelative`.
[ ] ``same-workspace relocate runs git mv in the Workspace repository`` — the File `home/docs/readme.txt` is tracked in the `home` repository, `Graph.setParseState` sets that File to `ParseState.Unparsed`, and the post graph owns that File directly under the Workspace Node. `sameWorkspaceMoves` returns that File. `repositoryRoot` is the `home` repository. The old git path is `docs/readme.txt` and the new git path is `readme.txt`. One `gitMove` leaves the file at `readme.txt` in that repository, and `GitSave.runGit` `ls-files` lists `readme.txt`.
[ ] ``gitMove moves a directory tree with one git mv`` — the tracked directory `docs` contains `readme.txt`. `gitPath` for that Directory is `docs/`. One `gitMove` from `docs/` to `archive/` leaves `archive/readme.txt` in the repository, and the test calls `gitMove` once.

## Stop when

[ ] Record the stop at `plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-build.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit changes `GraphQuery.enclosingWorkspace`, `executePathMoves`, `persistGraphChange`, `FileAgent`, `DbAgent`, `coalescePathMoves`, or `ChangeAmendment`.
[ ] Stop when the edit contradicts story 2 Relocate with git mv in the spec.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``sameWorkspaceMoves keeps a same-workspace move and drops a cross-workspace move and a workspace move``
  - `tests/Server.Tests`: ``repository root and git path follow the workspace and ROOT``
  - `tests/Server.Tests`: ``same-workspace relocate runs git mv in the Workspace repository``
  - `tests/Server.Tests`: ``gitMove moves a directory tree with one git mv``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-build.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
