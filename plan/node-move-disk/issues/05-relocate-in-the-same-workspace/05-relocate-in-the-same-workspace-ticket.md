# 05 — Relocate in the same workspace
**Status**: coded
One job: The Server moves a File or a Directory that stays under the same Workspace Node with one `git mv`, so git tracks that move. Story 2 Relocate with git mv in [Node move and disk](plan/node-move-disk/spec.md) is that move, and it includes a File whose strictly enclosing artifact becomes the Workspace Node.

Type: coding
Blockers: [04 — Expand the node enclosure lock](../04-expand-the-node-enclosure-lock/04-expand-the-node-enclosure-lock-ticket.md)

## Scope

### Signatures

[x] `type DocumentPathMove = { nodeId: NodeId; oldPath: string; newPath: string }` in [src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs).

[x] `planPathMovesBetweenGraphs : Graph -> Graph -> DocumentPathMove list` in [src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs).

[x] `coalescePathMoves : Graph -> DocumentPathMove list -> DocumentPathMove list` in [src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs).

[x] `sameWorkspaceMoves : Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list` is not in code yet; arch requires it in Path class beside `crossWorkspaceMoves`, which this slice also does not add.

[x] `enclosingWorkspace : Graph -> NodeId -> NodeId option` in [src/Shared/GraphQuery.fs](src/Shared/GraphQuery.fs) calls `enclosing graph isEnclosingWorkspaceNode nodeId`.

[x] `isDocumentRootNode : Graph -> NodeId -> bool` in [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs) is true for `Special (Workspace | Directory | File)`.

[x] `artifactFileRelative : Graph -> NodeId -> string option` and `artifactDirectoryRelative : Graph -> NodeId -> string option` in [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs) yield data-directory-relative paths (`home/docs/readme.txt`, `home/`, `.amb` for ROOT).

[x] `workspaceRootFor : string -> Graph -> NodeId -> string option` in [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) resolves a named Workspace directory under `dataDir` and returns `None` when `artifactDirectoryRelative` is `None`, including when the enclosing Workspace is `Graph.rootId`. Callers are `GithubTransportActor`, `LoadSaveRouting`, and `Api`.

[x] `resolveUnderDataDir : string -> string -> Result<string, string>` in [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs).

[x] `gitMove : string -> string -> string -> Result<unit, string>` is not in code yet; arch puts it in Same-workspace relocate with repository root, old git path, and new git path.

[x] `GitSave.runGit : string -> string -> Result<string, string>` in [src/Server/GitSave.fs](src/Server/GitSave.fs) calls `GitRun.gitExec`.

[x] `WorkspaceGit.isRepo : string -> bool` and `WorkspaceGit.ensureInit : string -> Result<unit, string>` in [src/Server/WorkspaceGit.fs](src/Server/WorkspaceGit.fs).

[x] `executePathMoves : string -> Graph -> Graph -> DocumentPathMove list -> Result<unit, string>` in [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs) coalesces, then folds `executeCoalescedMove`. That takes `NodeLockPool.withLockResult` on the post graph and `move.nodeId`, then calls `resolveMovePaths` and `executePathMove`. `executePathMove` uses `File.Move` and `Directory.Move`.

[x] `validatePathMoves : string -> Graph -> Graph -> Result<unit, string>` in [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs).

[x] `persistGraphChange : string -> Graph -> Graph -> Result<PersistGraphOk, string>` in [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs) still calls `executePathMoves`, not `gitMove`; this slice does not change that caller.

### Match sites

[x] This slice adds no union case.

[x] This slice changes no existing public signature; it adds `sameWorkspaceMoves`, `gitMove`, and Workspace-repository path helpers.

[x] New `sameWorkspaceMoves` matches each `DocumentPathMove` node kind via `Map.tryFind` on `graph.nodes` and compares `GraphQuery.enclosingWorkspace preGraph nodeId` with `GraphQuery.enclosingWorkspace postGraph nodeId`.

[x] New `sameWorkspaceMoves` excludes a node when `node.kind = Special Workspace`.

[x] New `sameWorkspaceMoves` excludes a node when either enclosing workspace id is `None` or the ids differ.

[x] `crossWorkspaceMoves` has no implementation and no match sites in this slice.

[x] New `gitMove` calls `GitSave.runGit repositoryRoot` with a `git mv --` argument string for one move.

[x] No caller wires `sameWorkspaceMoves` or `gitMove` into `FileAgent.processPostEvents` or `DbAgent.finishAppliedPostChange` in this slice.

[x] `DocumentPersistChange.persistGraphChangeWith` calls `executePathMoves`, then `writeDocumentsSoft`. There is no `withWorkTreeGates`. The pool lock is inside `executeCoalescedMove`. This slice does not change that caller.

[x] `GraphQuery.enclosingWorkspace` callers today include [src/Shared/WorkspaceSyncScope.fs](src/Shared/WorkspaceSyncScope.fs), [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs), [src/Shared/ResidentProjection.fs](src/Shared/ResidentProjection.fs), [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) `workspaceRootFor`, [src/Server/Core/DbAgent.fs](src/Server/Core/DbAgent.fs), [src/Server/GithubTransportActor.fs](src/Server/GithubTransportActor.fs), and [src/Server/IgnoredDestination.fs](src/Server/IgnoredDestination.fs); this slice does not change `enclosingWorkspace`.

[x] `DocumentPersistPath.workspaceRootFor` callers are [src/Server/GithubTransportActor.fs](src/Server/GithubTransportActor.fs), [src/Server/LoadSaveRouting.fs](src/Server/LoadSaveRouting.fs), and [src/Server/Api.fs](src/Server/Api.fs). `persistGraphChangeWith` and `DocumentPersistWrite` do not call it. This slice adds repository-root and git-path helpers without breaking those three call sites.

### Existing tests

[x] ``planPathMovesBetweenGraphs includes file reparent across workspaces`` in [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) checks a file move from `//home/readme.txt` to `//other/readme.txt`.

[x] ``planPathMovesBetweenGraphs includes nested roots after directory rename`` in [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) checks directory and nested file path deltas on rename.

[x] ``coalescePathMoves drops nested roots covered by directory move`` in [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) checks coalesce keeps only the directory move for `docs` → `archive`.

[x] ``coalescePathMoves keeps file-owned directory separate from file move`` in [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) checks coalesce keeps both file and owned-directory moves.

[x] Arch seam 6 ``coalesce keeps a nested node whose derived path differs`` in [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) does not exist yet; out of scope for this slice.

[x] ``persistGraphChange moves renamed file artifact before writing`` in [tests/Server.Tests/DocumentPathMoveExecutionTests.fs](tests/Server.Tests/DocumentPathMoveExecutionTests.fs) uses `newTempDir`, `graphWithNestedDocs`, `writeAllDocuments`, and `Graph.setName` rename; asserts `File.Move` outcome on disk.

[x] ``persistGraphChange moves renamed directory tree before writing`` in [tests/Server.Tests/DocumentPathMoveExecutionTests.fs](tests/Server.Tests/DocumentPathMoveExecutionTests.fs) asserts directory tree `Directory.Move` under `home/docs` → `home/archive`.

[x] ``persistGraphChange rejects destination artifact conflict`` in [tests/Server.Tests/DocumentPathMoveExecutionTests.fs](tests/Server.Tests/DocumentPathMoveExecutionTests.fs) asserts `persistGraphChange` Error when destination file exists.

[x] ``validatePathMoves refuses rename of non-allowlisted SYSTEM file`` in [tests/Server.Tests/DocumentPersistChangeTests.fs](tests/Server.Tests/DocumentPersistChangeTests.fs) is the Accept validation / Path coalesce seam neighbor in arch section 6.

[x] ``enclosingWorkspace resolves named workspace and canonical roots`` in [tests/Shared.Tests/GraphQueryTests.fs](tests/Shared.Tests/GraphQueryTests.fs) checks file, directory, and named workspace resolve to the workspace id and ROOT entities resolve to `Graph.rootId`.

[x] [tests/Server.Tests/GitSaveTests.fs](tests/Server.Tests/GitSaveTests.fs) covers `GitSave.isRepo` and `GitSave.commitAll` with `GitSave.runGit` for `git init`; no `git mv` test exists.

[x] [tests/Server.Tests/WorkspaceGitTests.fs](tests/Server.Tests/WorkspaceGitTests.fs) uses `GitSave.runGit` for workspace git operations; no `git mv` test exists.

### Copy-from

[x] Disk relocate for one move: `executePathMoves` folds `executeCoalescedMove`, which calls `resolveMovePaths` and `executePathMove` in [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs). `artifactRelativeForMove` uses `DocumentPartition.artifactFileRelative` and `artifactDirectoryRelative`.

[x] Git process: `GitSave.runGit` in [src/Server/GitSave.fs](src/Server/GitSave.fs).

[x] Named Workspace directory root: `DocumentPersistPath.workspaceRootFor` in [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) for `dataDir` + `artifactDirectoryRelative`; ROOT repository root is `dataDir` per arch and needs a sibling helper because `workspaceRootFor` returns `None` for ROOT-enclosed nodes today.

[x] Workspace repo init in tests: `WorkspaceGit.ensureInit` pattern from [src/Server/WorkspaceGit.fs](src/Server/WorkspaceGit.fs) and [tests/Server.Tests/WorkspaceGitTests.fs](tests/Server.Tests/WorkspaceGitTests.fs).

[x] Git path prefix strip: private `workspaceDiskPrefix` in [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs) (`""` for ROOT, `name + "/"` for named Workspace) mirrors the arch rule that git path drops the workspace directory prefix for a named Workspace.

### write-ticket

[x] Blocker: [04 — Expand the node enclosure lock](plan/node-move-disk/issues/04-expand-the-node-enclosure-lock/04-expand-the-node-enclosure-lock-ticket.md); this slice does not add node lock wiring, `crossWorkspaceMoves`, `moveAcrossWorkspaces`, coalesce behavior change, change-conflict amendment, or Post change as relocate caller.

[x] Flow 2 in [plan/node-move-disk/arch.md](plan/node-move-disk/arch.md) section 4: Path class selects same-workspace moves; Same-workspace relocate runs one `git mv` per move; parse state does not gate the move.

#### Source files

[x] [src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs) — add `sameWorkspaceMoves` next to `coalescePathMoves`; use `GraphQuery.enclosingWorkspace` and exclude `Special Workspace` nodes.

[x] [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) — add helpers for repository root (named Workspace via existing resolution, ROOT via `dataDir`) and git-relative path from `artifactFileRelative` / `artifactDirectoryRelative` without the workspace prefix when the enclosing workspace is named.

[x] [src/Server/WorkspaceGit.fs](src/Server/WorkspaceGit.fs) or [src/Server/GitSave.fs](src/Server/GitSave.fs) — add `gitMove` calling `GitSave.runGit` with `git mv -- old new`; prefer the module that already owns workspace-scoped git commands (`WorkspaceGit`).

[ ] [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs) — add a public same-workspace relocate entry (for example `relocateSameWorkspaceMoves`) that classifies with `sameWorkspaceMoves`, maps each move to repository root and git paths, and calls `gitMove` once per move; do not replace `executePathMoves` inside `persistGraphChange` yet.

[ ] Source file count: 4; under the 8-file limit in [plan/context/size-limit.md](plan/context/size-limit.md).

[x] No new `.fs` file and no `Gambol.Server.fsproj` / `Gambol.Shared.fsproj` compile-order change if `gitMove` lives in an existing Server module.

#### Tests

[ ] [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) module `DocumentPathMoveTests` — add ``sameWorkspaceMoves keeps same-workspace file and drops cross-workspace and workspace-root moves`` with `--filter "sameWorkspaceMoves keeps same-workspace file and drops cross-workspace and workspace-root moves"`; reuse `graphWithWorkspaceFile`, `planPathMovesBetweenGraphs includes file reparent across workspaces` post graph, and a workspace-rename `planPathMoveForSetName` case excluded.

[ ] [tests/Server.Tests/DocumentPathMoveExecutionTests.fs](tests/Server.Tests/DocumentPathMoveExecutionTests.fs) module `Gambol.Server.Tests.DocumentPathMoveExecutionTests` — add arch seam 7 ``same-workspace relocate runs git mv in the Workspace repository`` with `--filter "same-workspace relocate runs git mv in the Workspace repository"`; pattern: `newTempDir`, `graphWithNestedDocs`, `writeAllDocuments`, `WorkspaceGit.ensureInit` on `dataDir/home`, file rename in same workspace, call new relocate entry, assert tracked path via `GitSave.runGit` `status --porcelain` or `ls-files` and on-disk file at new relative path.

[ ] [tests/Server.Tests/WorkspaceGitTests.fs](tests/Server.Tests/WorkspaceGitTests.fs) or [tests/Server.Tests/GitSaveTests.fs](tests/Server.Tests/GitSaveTests.fs) — add one skippable test ``gitMove renames a tracked file in the repository`` with `--filter "gitMove renames a tracked file in the repository"` if the seam test does not assert `runGit` received `git mv`; otherwise omit to stay at 4 tests.

[x] Test count: 2 required (classification + seam 7); 3 if a direct `gitMove` unit test is split out; at most 4 per size limit.

## Inputs

### Working

#### Path class

[x] `DocumentPathMove.sameWorkspaceMoves` is new in `src/Shared/DocumentPathMove.fs`.
[x] `sameWorkspaceMoves` has type `Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list`.
[x] The first `Graph` is the pre graph and the second `Graph` is the post graph.
[x] `sameWorkspaceMoves` keeps a `Special File` or `Special Directory` whose `GraphQuery.enclosingWorkspace` id is `Some` on both graphs and those ids are equal.
[x] `sameWorkspaceMoves` drops a move whose enclosing Workspace ids differ.
[x] `sameWorkspaceMoves` drops a move when either enclosing Workspace id is `None`.
[x] `sameWorkspaceMoves` drops a `Special Workspace` node.
[x] `sameWorkspaceMoves` keeps the input order of the moves it keeps.
[x] `sameWorkspaceMoves` reads no parse state.
[x] `GraphQuery.enclosingWorkspace` in `src/Shared/GraphQuery.fs` has type `Graph -> NodeId -> NodeId option`.
[x] `enclosingWorkspace` returns the Workspace id, including `Graph.rootId` for ROOT.

#### Workspace repository

[x] `DocumentPersistPath.repositoryRoot` is new in `src/Server/DocumentPersistPath.fs`.
[x] `repositoryRoot` has type `string -> Graph -> NodeId -> string option`.
[x] For a named Workspace, `repositoryRoot` returns the same string as `DocumentPersistPath.workspaceRootFor`.
[x] For enclosing Workspace `Graph.rootId`, `repositoryRoot` returns the data directory with trailing separators trimmed, the string `dataDirBase` already returns in that module.
[x] When `enclosingWorkspace` is `None`, `repositoryRoot` returns `None`.
[x] `DocumentPersistPath.gitPath` is new in `src/Server/DocumentPersistPath.fs`.
[x] `gitPath` has type `Graph -> NodeId -> string option`.
[x] For a File, the data-directory path is `DocumentPartition.artifactFileRelative`.
[x] For a Directory, the data-directory path is `DocumentPartition.artifactDirectoryRelative`.
[x] For a Workspace node, `gitPath` returns `None`.
[x] For a named Workspace, `gitPath` is that data-directory path without the workspace directory name and the following slash.
[x] For ROOT, `gitPath` is that data-directory path unchanged.
[x] When the named-Workspace data-directory path lacks that prefix, `gitPath` returns `None`.

#### Same-workspace relocate

[x] `WorkspaceGit.gitMove` is new in `src/Server/WorkspaceGit.fs`.
[x] `gitMove` has type `string -> string -> string -> Result<unit, string>`.
[x] The arguments are the repository root, the old git path, and the new git path.
[x] `gitMove` makes one `GitSave.runGit` call whose arguments are `mv --`, the old git path, and the new git path.
[x] `GitSave.runGit` in `src/Server/GitSave.fs` has type `string -> string -> Result<string, string>`.
[x] One same-workspace move is one `gitMove` call.
[x] `gitMove` of a Directory path moves that Directory tree.
[x] `gitMove` takes no child list and no parse state.

#### Copy-from

[x] `DocumentPersistChange.executePathMoves` folds `executeCoalescedMove`. `executePathMove` relocates with `File.Move` and `Directory.Move`.
[x] `DocumentPersistPath.workspaceRootFor` in `src/Server/DocumentPersistPath.fs` has type `string -> Graph -> NodeId -> string option` and returns `None` when the enclosing Workspace is ROOT.
[x] `WorkspaceGit.ensureInit` in `src/Server/WorkspaceGit.fs` creates the Workspace repository the seam test initializes.
[x] ``enclosingWorkspace resolves named workspace and canonical roots`` in `tests/Shared.Tests/GraphQueryTests.fs` checks `enclosingWorkspace` for a named Workspace and for ROOT.

#### Match sites

[x] This ticket adds no union case.
[x] This ticket changes no existing public signature.
[x] `sameWorkspaceMoves`, `repositoryRoot`, `gitPath`, and `gitMove` have no production caller in this ticket.
[x] The seam test is the caller: it classifies with `sameWorkspaceMoves`, resolves `repositoryRoot` and `gitPath`, and calls `gitMove` once.

#### Tests the ticket leaves as they are

[x] ``planPathMovesBetweenGraphs includes file reparent across workspaces`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` checks a file move from `//home/readme.txt` to `//other/readme.txt`.
[x] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` checks coalesce keeps the directory move for `docs` to `archive`.
[x] ``persistGraphChange moves renamed file artifact before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` checks `persistGraphChange` moves the file on disk.
[x] ``persistGraphChange moves renamed directory tree before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` checks `persistGraphChange` moves the directory tree on disk.

### Reference

[x] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Path class**, **Same-workspace relocate**, and **Workspace repository**, §4 flow 2 **Same-workspace relocate**, §6 seam 7, and §8 question 1.
[x] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[x] `src/Shared/GraphQuery.fs` stays as it is.
[x] `src/Server/DocumentPersistChange.fs` stays as it is. `executePathMove` keeps `File.Move` and `Directory.Move`.
[x] `src/Server/GitSave.fs` stays as it is. `runGit` stays the git process call.
[x] `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` stay as they are. Post change becomes the relocate caller in a later ticket.
[x] `crossWorkspaceMoves` and `moveAcrossWorkspaces` stay absent.
[x] `coalescePathMoves` stays as it is.
[x] `src/Server/NodeLockPool.fs` stays as it is. This ticket takes no node lock.
[x] `src/Shared/ChangeAmendment.fs` stays as it is.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Expand `src/Shared/DocumentPathMove.fs` with `sameWorkspaceMoves` as Working specifies.
3. [x] Expand `src/Server/DocumentPersistPath.fs` with `repositoryRoot` and `gitPath` as Working specifies.
4. [x] Expand `src/Server/WorkspaceGit.fs` with `gitMove` as Working specifies.
5. [x] Expand `tests/Shared.Tests/DocumentPathMoveTests.fs` with the classification test in Outputs. Reuse the graph helpers already in that file.
6. [x] Expand `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` with the three server tests in Outputs. Reuse `newTempDir`, `graphWithNestedDocs`, and `WorkspaceGit.ensureInit`.

## Outputs

[x] `src/Shared/DocumentPathMove.fs` — `sameWorkspaceMoves`.
[x] `src/Server/DocumentPersistPath.fs` — `repositoryRoot` and `gitPath`.
[x] `src/Server/WorkspaceGit.fs` — `gitMove`.
[x] Distinct source files: 3. `src/Shared/DocumentPathMove.fs`, `src/Server/DocumentPersistPath.fs`, and `src/Server/WorkspaceGit.fs`.
[x] `tests/Shared.Tests/DocumentPathMoveTests.fs` — the classification test below.
[x] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — the three tests below.
[x] ``sameWorkspaceMoves keeps a same-workspace move and drops a cross-workspace move and a workspace move`` — a File rename under one named Workspace stays; the `//home/readme.txt` to `//other/readme.txt` move drops; a `DocumentPathMove` whose node is `Special Workspace` drops; a File rename under ROOT stays.
[x] ``repository root and git path follow the workspace and ROOT`` — for a File under the named Workspace `home`, `repositoryRoot` equals `workspaceRootFor` and `gitPath` is `artifactFileRelative` without the `home/` prefix; for a File owned by `Graph.rootId`, `repositoryRoot` is the trimmed data directory and `gitPath` equals `artifactFileRelative`.
[x] ``same-workspace relocate runs git mv in the Workspace repository`` — the File `home/docs/readme.txt` is tracked in the `home` repository, `Graph.setParseState` sets that File to `ParseState.Unparsed`, and the post graph owns that File directly under the Workspace Node. `sameWorkspaceMoves` returns that File. `repositoryRoot` is the `home` repository. The old git path is `docs/readme.txt` and the new git path is `readme.txt`. One `gitMove` leaves the file at `readme.txt` in that repository, and `GitSave.runGit` `ls-files` lists `readme.txt`.
[x] ``gitMove moves a directory tree with one git mv`` — the tracked directory `docs` contains `readme.txt`. `gitPath` for that Directory is `docs/`. One `gitMove` from `docs/` to `archive/` leaves `archive/readme.txt` in the repository, and the test calls `gitMove` once.

## Stop when

[ ] Record the stop at `plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-build.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit changes `GraphQuery.enclosingWorkspace`, `executePathMoves`, `persistGraphChange`, `FileAgent`, `DbAgent`, `coalescePathMoves`, or `ChangeAmendment`.
[ ] Stop when the edit contradicts story 2 Relocate with git mv in the spec.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``sameWorkspaceMoves keeps a same-workspace move and drops a cross-workspace move and a workspace move``
  - `tests/Server.Tests`: ``repository root and git path follow the workspace and ROOT``
  - `tests/Server.Tests`: ``same-workspace relocate runs git mv in the Workspace repository``
  - `tests/Server.Tests`: ``gitMove moves a directory tree with one git mv``
[x] All executed tests pass.
[x] Write the verdict to `plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-build.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
