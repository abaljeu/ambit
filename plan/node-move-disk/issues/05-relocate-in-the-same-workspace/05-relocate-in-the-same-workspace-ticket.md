# 05 — Relocate in the same workspace
**Status**: defined
One job: The Server moves a File or a Directory that stays under the same Workspace Node with one `git mv`, so git tracks that move. Story 2 Relocate with git mv in [Node move and disk](plan/node-move-disk/spec.md) is that move, and it includes a File whose strictly enclosing artifact becomes the Workspace Node.

Type: coding
Blockers: [04 — Expand the node enclosure lock](../04-expand-the-node-enclosure-lock/04-expand-the-node-enclosure-lock-ticket.md)

## Scope

### Signatures

[x] `type DocumentPathMove = { nodeId: NodeId; oldPath: string; newPath: string }` in [src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs).

[x] `planPathMovesBetweenGraphs : Graph -> Graph -> DocumentPathMove list` in [src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs).

[x] `coalescePathMoves : Graph -> DocumentPathMove list -> DocumentPathMove list` in [src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs).

[x] `sameWorkspaceMoves : Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list` is not in code yet; arch requires it in Path class beside `crossWorkspaceMoves`, which this slice also does not add.

[x] `enclosingWorkspace : Graph -> NodeId -> NodeId option` in [src/Shared/GraphQuery.fs](src/Shared/GraphQuery.fs) lines 366–367; walk uses `enclosing graph isEnclosingWorkspaceNode nodeId`.

[x] `isDocumentRootNode : Graph -> NodeId -> bool` in [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs) is true for `Special (Workspace | Directory | File)`.

[x] `artifactFileRelative : Graph -> NodeId -> string option` and `artifactDirectoryRelative : Graph -> NodeId -> string option` in [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs) yield data-directory-relative paths (`home/docs/readme.txt`, `home/`, `.amb` for ROOT).

[x] `workspaceRootFor : string -> Graph -> NodeId -> string option` in [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) lines 41–49 resolves a named Workspace directory under `dataDir` and returns `None` when `artifactDirectoryRelative` is `None` (including ROOT as enclosing workspace).

[x] `resolveUnderDataDir : string -> string -> Result<string, string>` in [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs).

[x] `gitMove : string -> string -> string -> Result<unit, string>` is not in code yet; arch puts it in Same-workspace relocate with repository root, old git path, and new git path.

[x] `GitSave.runGit : string -> string -> Result<string, string>` in [src/Server/GitSave.fs](src/Server/GitSave.fs) line 13 calls `GitRun.gitExec`.

[x] `WorkspaceGit.isRepo : string -> bool` and `WorkspaceGit.ensureInit : string -> Result<unit, string>` in [src/Server/WorkspaceGit.fs](src/Server/WorkspaceGit.fs).

[ ] `executePathMoves : string -> Graph -> Graph -> DocumentPathMove list -> Result<unit, string>` in [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs) lines 164–180 uses `File.Move` and `Directory.Move` via private `executePathMove`.

[x] `validatePathMoves : string -> Graph -> Graph -> Result<unit, string>` in [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs) lines 112–128.

[x] `persistGraphChange : string -> Graph -> Graph -> Result<PersistGraphOk, string>` in [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs) still calls `executePathMoves`, not `gitMove`; this slice does not change that caller.

### Match sites

[x] This slice adds no union case.

[ ] This slice changes no existing public signature; it adds `sameWorkspaceMoves`, `gitMove`, and Workspace-repository path helpers.

[ ] New `sameWorkspaceMoves` matches each `DocumentPathMove` node kind via `Map.tryFind` on `graph.nodes` and compares `GraphQuery.enclosingWorkspace preGraph nodeId` with `GraphQuery.enclosingWorkspace postGraph nodeId`.

[ ] New `sameWorkspaceMoves` excludes a node when `node.kind = Special Workspace`.

[ ] New `sameWorkspaceMoves` excludes a node when either enclosing workspace id is `None` or the ids differ.

[ ] `crossWorkspaceMoves` has no implementation and no match sites in this slice.

[ ] New `gitMove` calls `GitSave.runGit repositoryRoot` with a `git mv --` argument string for one move.

[x] No caller wires `sameWorkspaceMoves` or `gitMove` into `FileAgent.processPostEvents` or `DbAgent.finishAppliedPostChange` in this slice.

[ ] `DocumentPersistChange.persistGraphChangeWith` line 213 still calls only `executePathMoves` inside `DocumentPersistPath.withWorkTreeGates`.

[x] `GraphQuery.enclosingWorkspace` callers today include [src/Shared/WorkspaceSyncScope.fs](src/Shared/WorkspaceSyncScope.fs), [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs), [src/Shared/ResidentProjection.fs](src/Shared/ResidentProjection.fs), [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) `workspaceRootFor`, [src/Server/Core/DbAgent.fs](src/Server/Core/DbAgent.fs), [src/Server/GithubTransportActor.fs](src/Server/GithubTransportActor.fs), and [src/Server/IgnoredDestination.fs](src/Server/IgnoredDestination.fs); this slice does not change `enclosingWorkspace`.

[ ] `DocumentPersistPath.workspaceRootFor` callers include [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs) `persistGraphChangeWith`, [src/Server/DocumentPersistWrite.fs](src/Server/DocumentPersistWrite.fs), [src/Server/LoadSaveRouting.fs](src/Server/LoadSaveRouting.fs), and [src/Server/Api.fs](src/Server/Api.fs); this slice should add repository-root and git-path helpers without breaking those call sites.

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

[x] Disk relocate for one move: private `executePathMove` and `executePathMoves` in [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs); resolves full paths via private `resolveMovePaths` and `artifactRelativeForMove` using `DocumentPartition.artifactFileRelative` / `artifactDirectoryRelative`.

[x] Git process: `GitSave.runGit` in [src/Server/GitSave.fs](src/Server/GitSave.fs).

[x] Named Workspace directory root: `DocumentPersistPath.workspaceRootFor` in [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) for `dataDir` + `artifactDirectoryRelative`; ROOT repository root is `dataDir` per arch and needs a sibling helper because `workspaceRootFor` returns `None` for ROOT-enclosed nodes today.

[x] Workspace repo init in tests: `WorkspaceGit.ensureInit` pattern from [src/Server/WorkspaceGit.fs](src/Server/WorkspaceGit.fs) and [tests/Server.Tests/WorkspaceGitTests.fs](tests/Server.Tests/WorkspaceGitTests.fs).

[x] Git path prefix strip: private `workspaceDiskPrefix` in [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs) (`""` for ROOT, `name + "/"` for named Workspace) mirrors the arch rule that git path drops the workspace directory prefix for a named Workspace.

### write-ticket

[ ] Blocker: [04 — Expand the node enclosure lock](plan/node-move-disk/issues/04-expand-the-node-enclosure-lock/04-expand-the-node-enclosure-lock-ticket.md); this slice does not add node lock wiring, `crossWorkspaceMoves`, `moveAcrossWorkspaces`, coalesce behavior change, change-conflict amendment, or Post change as relocate caller.

[ ] Flow 2 in [plan/node-move-disk/arch.md](plan/node-move-disk/arch.md) section 4: Path class selects same-workspace moves; Same-workspace relocate runs one `git mv` per move; parse state does not gate the move.

#### Source files

[ ] [src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs) — add `sameWorkspaceMoves` next to `coalescePathMoves`; use `GraphQuery.enclosingWorkspace` and exclude `Special Workspace` nodes.

[ ] [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) — add helpers for repository root (named Workspace via existing resolution, ROOT via `dataDir`) and git-relative path from `artifactFileRelative` / `artifactDirectoryRelative` without the workspace prefix when the enclosing workspace is named.

[ ] [src/Server/WorkspaceGit.fs](src/Server/WorkspaceGit.fs) or [src/Server/GitSave.fs](src/Server/GitSave.fs) — add `gitMove` calling `GitSave.runGit` with `git mv -- old new`; prefer the module that already owns workspace-scoped git commands (`WorkspaceGit`).

[ ] [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs) — add a public same-workspace relocate entry (for example `relocateSameWorkspaceMoves`) that classifies with `sameWorkspaceMoves`, maps each move to repository root and git paths, and calls `gitMove` once per move; do not replace `executePathMoves` inside `persistGraphChange` yet.

[ ] Source file count: 4; under the 8-file limit in [plan/context/size-limit.md](plan/context/size-limit.md).

[ ] No new `.fs` file and no `Gambol.Server.fsproj` / `Gambol.Shared.fsproj` compile-order change if `gitMove` lives in an existing Server module.

#### Tests

[ ] [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) module `DocumentPathMoveTests` — add ``sameWorkspaceMoves keeps same-workspace file and drops cross-workspace and workspace-root moves`` with `--filter "sameWorkspaceMoves keeps same-workspace file and drops cross-workspace and workspace-root moves"`; reuse `graphWithWorkspaceFile`, `planPathMovesBetweenGraphs includes file reparent across workspaces` post graph, and a workspace-rename `planPathMoveForSetName` case excluded.

[ ] [tests/Server.Tests/DocumentPathMoveExecutionTests.fs](tests/Server.Tests/DocumentPathMoveExecutionTests.fs) module `Gambol.Server.Tests.DocumentPathMoveExecutionTests` — add arch seam 7 ``same-workspace relocate runs git mv in the Workspace repository`` with `--filter "same-workspace relocate runs git mv in the Workspace repository"`; pattern: `newTempDir`, `graphWithNestedDocs`, `writeAllDocuments`, `WorkspaceGit.ensureInit` on `dataDir/home`, file rename in same workspace, call new relocate entry, assert tracked path via `GitSave.runGit` `status --porcelain` or `ls-files` and on-disk file at new relative path.

[ ] [tests/Server.Tests/WorkspaceGitTests.fs](tests/Server.Tests/WorkspaceGitTests.fs) or [tests/Server.Tests/GitSaveTests.fs](tests/Server.Tests/GitSaveTests.fs) — add one skippable test ``gitMove renames a tracked file in the repository`` with `--filter "gitMove renames a tracked file in the repository"` if the seam test does not assert `runGit` received `git mv`; otherwise omit to stay at 4 tests.

[ ] Test count: 2 required (classification + seam 7); 3 if a direct `gitMove` unit test is split out; at most 4 per size limit.

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
