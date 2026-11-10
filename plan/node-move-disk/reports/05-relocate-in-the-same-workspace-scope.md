## Signatures

[ ] `type DocumentPathMove = { nodeId: NodeId; oldPath: string; newPath: string }` in [src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs).

[ ] `planPathMovesBetweenGraphs : Graph -> Graph -> DocumentPathMove list` in [src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs).

[ ] `coalescePathMoves : Graph -> DocumentPathMove list -> DocumentPathMove list` in [src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs).

[ ] `sameWorkspaceMoves : Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list` is not in code yet; arch requires it in Path class beside `crossWorkspaceMoves`, which this slice also does not add.

[ ] `enclosingWorkspace : Graph -> NodeId -> NodeId option` in [src/Shared/GraphQuery.fs](src/Shared/GraphQuery.fs) lines 366–367; walk uses `enclosing graph isEnclosingWorkspaceNode nodeId`.

[ ] `isDocumentRootNode : Graph -> NodeId -> bool` in [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs) is true for `Special (Workspace | Directory | File)`.

[ ] `artifactFileRelative : Graph -> NodeId -> string option` and `artifactDirectoryRelative : Graph -> NodeId -> string option` in [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs) yield data-directory-relative paths (`home/docs/readme.txt`, `home/`, `.amb` for ROOT).

[ ] `workspaceRootFor : string -> Graph -> NodeId -> string option` in [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) lines 41–49 resolves a named Workspace directory under `dataDir` and returns `None` when `artifactDirectoryRelative` is `None` (including ROOT as enclosing workspace).

[ ] `resolveUnderDataDir : string -> string -> Result<string, string>` in [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs).

[ ] `gitMove : string -> string -> string -> Result<unit, string>` is not in code yet; arch puts it in Same-workspace relocate with repository root, old git path, and new git path.

[ ] `GitSave.runGit : string -> string -> Result<string, string>` in [src/Server/GitSave.fs](src/Server/GitSave.fs) line 13 calls `GitRun.gitExec`.

[ ] `WorkspaceGit.isRepo : string -> bool` and `WorkspaceGit.ensureInit : string -> Result<unit, string>` in [src/Server/WorkspaceGit.fs](src/Server/WorkspaceGit.fs).

[ ] `executePathMoves : string -> Graph -> Graph -> DocumentPathMove list -> Result<unit, string>` in [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs) lines 164–180 uses `File.Move` and `Directory.Move` via private `executePathMove`.

[ ] `validatePathMoves : string -> Graph -> Graph -> Result<unit, string>` in [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs) lines 112–128.

[ ] `persistGraphChange : string -> Graph -> Graph -> Result<PersistGraphOk, string>` in [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs) still calls `executePathMoves`, not `gitMove`; this slice does not change that caller.

## Match sites

[ ] This slice adds no union case.

[ ] This slice changes no existing public signature; it adds `sameWorkspaceMoves`, `gitMove`, and Workspace-repository path helpers.

[ ] New `sameWorkspaceMoves` matches each `DocumentPathMove` node kind via `Map.tryFind` on `graph.nodes` and compares `GraphQuery.enclosingWorkspace preGraph nodeId` with `GraphQuery.enclosingWorkspace postGraph nodeId`.

[ ] New `sameWorkspaceMoves` excludes a node when `node.kind = Special Workspace`.

[ ] New `sameWorkspaceMoves` excludes a node when either enclosing workspace id is `None` or the ids differ.

[ ] `crossWorkspaceMoves` has no implementation and no match sites in this slice.

[ ] New `gitMove` calls `GitSave.runGit repositoryRoot` with a `git mv --` argument string for one move.

[ ] No caller wires `sameWorkspaceMoves` or `gitMove` into `FileAgent.processPostEvents` or `DbAgent.finishAppliedPostChange` in this slice.

[ ] `DocumentPersistChange.persistGraphChangeWith` line 213 still calls only `executePathMoves` inside `DocumentPersistPath.withWorkTreeGates`.

[ ] `GraphQuery.enclosingWorkspace` callers today include [src/Shared/WorkspaceSyncScope.fs](src/Shared/WorkspaceSyncScope.fs), [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs), [src/Shared/ResidentProjection.fs](src/Shared/ResidentProjection.fs), [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) `workspaceRootFor`, [src/Server/Core/DbAgent.fs](src/Server/Core/DbAgent.fs), [src/Server/GithubTransportActor.fs](src/Server/GithubTransportActor.fs), and [src/Server/IgnoredDestination.fs](src/Server/IgnoredDestination.fs); this slice does not change `enclosingWorkspace`.

[ ] `DocumentPersistPath.workspaceRootFor` callers include [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs) `persistGraphChangeWith`, [src/Server/DocumentPersistWrite.fs](src/Server/DocumentPersistWrite.fs), [src/Server/LoadSaveRouting.fs](src/Server/LoadSaveRouting.fs), and [src/Server/Api.fs](src/Server/Api.fs); this slice should add repository-root and git-path helpers without breaking those call sites.

## Existing tests

[ ] ``planPathMovesBetweenGraphs includes file reparent across workspaces`` in [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) checks a file move from `//home/readme.txt` to `//other/readme.txt`.

[ ] ``planPathMovesBetweenGraphs includes nested roots after directory rename`` in [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) checks directory and nested file path deltas on rename.

[ ] ``coalescePathMoves drops nested roots covered by directory move`` in [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) checks coalesce keeps only the directory move for `docs` → `archive`.

[ ] ``coalescePathMoves keeps file-owned directory separate from file move`` in [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) checks coalesce keeps both file and owned-directory moves.

[ ] Arch seam 6 ``coalesce keeps a nested node whose derived path differs`` in [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) does not exist yet; out of scope for this slice.

[ ] ``persistGraphChange moves renamed file artifact before writing`` in [tests/Server.Tests/DocumentPathMoveExecutionTests.fs](tests/Server.Tests/DocumentPathMoveExecutionTests.fs) uses `newTempDir`, `graphWithNestedDocs`, `writeAllDocuments`, and `Graph.setName` rename; asserts `File.Move` outcome on disk.

[ ] ``persistGraphChange moves renamed directory tree before writing`` in [tests/Server.Tests/DocumentPathMoveExecutionTests.fs](tests/Server.Tests/DocumentPathMoveExecutionTests.fs) asserts directory tree `Directory.Move` under `home/docs` → `home/archive`.

[ ] ``persistGraphChange rejects destination artifact conflict`` in [tests/Server.Tests/DocumentPathMoveExecutionTests.fs](tests/Server.Tests/DocumentPathMoveExecutionTests.fs) asserts `persistGraphChange` Error when destination file exists.

[ ] ``validatePathMoves refuses rename of non-allowlisted SYSTEM file`` in [tests/Server.Tests/DocumentPersistChangeTests.fs](tests/Server.Tests/DocumentPersistChangeTests.fs) is the Accept validation / Path coalesce seam neighbor in arch section 6.

[ ] ``enclosingWorkspace resolves named workspace and canonical roots`` in [tests/Shared.Tests/GraphQueryTests.fs](tests/Shared.Tests/GraphQueryTests.fs) checks file, directory, and named workspace resolve to the workspace id and ROOT entities resolve to `Graph.rootId`.

[ ] [tests/Server.Tests/GitSaveTests.fs](tests/Server.Tests/GitSaveTests.fs) covers `GitSave.isRepo` and `GitSave.commitAll` with `GitSave.runGit` for `git init`; no `git mv` test exists.

[ ] [tests/Server.Tests/WorkspaceGitTests.fs](tests/Server.Tests/WorkspaceGitTests.fs) uses `GitSave.runGit` for workspace git operations; no `git mv` test exists.

## Copy-from

[ ] Disk relocate for one move: private `executePathMove` and `executePathMoves` in [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs); resolves full paths via private `resolveMovePaths` and `artifactRelativeForMove` using `DocumentPartition.artifactFileRelative` / `artifactDirectoryRelative`.

[ ] Git process: `GitSave.runGit` in [src/Server/GitSave.fs](src/Server/GitSave.fs).

[ ] Named Workspace directory root: `DocumentPersistPath.workspaceRootFor` in [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) for `dataDir` + `artifactDirectoryRelative`; ROOT repository root is `dataDir` per arch and needs a sibling helper because `workspaceRootFor` returns `None` for ROOT-enclosed nodes today.

[ ] Workspace repo init in tests: `WorkspaceGit.ensureInit` pattern from [src/Server/WorkspaceGit.fs](src/Server/WorkspaceGit.fs) and [tests/Server.Tests/WorkspaceGitTests.fs](tests/Server.Tests/WorkspaceGitTests.fs).

[ ] Git path prefix strip: private `workspaceDiskPrefix` in [src/Shared/DocumentPartition.fs](src/Shared/DocumentPartition.fs) (`""` for ROOT, `name + "/"` for named Workspace) mirrors the arch rule that git path drops the workspace directory prefix for a named Workspace.

## write-ticket

[ ] Blocker: [04 — Expand the node enclosure lock](issues/04-expand-the-node-enclosure-lock.md); this slice does not add node lock wiring, `crossWorkspaceMoves`, `moveAcrossWorkspaces`, coalesce behavior change, change-conflict amendment, or Post change as relocate caller.

[ ] Flow 2 in [plan/node-move-disk/arch.md](arch.md) section 4: Path class selects same-workspace moves; Same-workspace relocate runs one `git mv` per move; parse state does not gate the move.

### Source files

[ ] [src/Shared/DocumentPathMove.fs](src/Shared/DocumentPathMove.fs) — add `sameWorkspaceMoves` next to `coalescePathMoves`; use `GraphQuery.enclosingWorkspace` and exclude `Special Workspace` nodes.

[ ] [src/Server/DocumentPersistPath.fs](src/Server/DocumentPersistPath.fs) — add helpers for repository root (named Workspace via existing resolution, ROOT via `dataDir`) and git-relative path from `artifactFileRelative` / `artifactDirectoryRelative` without the workspace prefix when the enclosing workspace is named.

[ ] [src/Server/WorkspaceGit.fs](src/Server/WorkspaceGit.fs) or [src/Server/GitSave.fs](src/Server/GitSave.fs) — add `gitMove` calling `GitSave.runGit` with `git mv -- old new`; prefer the module that already owns workspace-scoped git commands (`WorkspaceGit`).

[ ] [src/Server/DocumentPersistChange.fs](src/Server/DocumentPersistChange.fs) — add a public same-workspace relocate entry (for example `relocateSameWorkspaceMoves`) that classifies with `sameWorkspaceMoves`, maps each move to repository root and git paths, and calls `gitMove` once per move; do not replace `executePathMoves` inside `persistGraphChange` yet.

[ ] Source file count: 4; under the 8-file limit in [plan/context/size-limit.md](plan/context/size-limit.md).

[ ] No new `.fs` file and no `Gambol.Server.fsproj` / `Gambol.Shared.fsproj` compile-order change if `gitMove` lives in an existing Server module.

### Tests

[ ] [tests/Shared.Tests/DocumentPathMoveTests.fs](tests/Shared.Tests/DocumentPathMoveTests.fs) module `DocumentPathMoveTests` — add ``sameWorkspaceMoves keeps same-workspace file and drops cross-workspace and workspace-root moves`` with `--filter "sameWorkspaceMoves keeps same-workspace file and drops cross-workspace and workspace-root moves"`; reuse `graphWithWorkspaceFile`, `planPathMovesBetweenGraphs includes file reparent across workspaces` post graph, and a workspace-rename `planPathMoveForSetName` case excluded.

[ ] [tests/Server.Tests/DocumentPathMoveExecutionTests.fs](tests/Server.Tests/DocumentPathMoveExecutionTests.fs) module `Gambol.Server.Tests.DocumentPathMoveExecutionTests` — add arch seam 7 ``same-workspace relocate runs git mv in the Workspace repository`` with `--filter "same-workspace relocate runs git mv in the Workspace repository"`; pattern: `newTempDir`, `graphWithNestedDocs`, `writeAllDocuments`, `WorkspaceGit.ensureInit` on `dataDir/home`, file rename in same workspace, call new relocate entry, assert tracked path via `GitSave.runGit` `status --porcelain` or `ls-files` and on-disk file at new relative path.

[ ] [tests/Server.Tests/WorkspaceGitTests.fs](tests/Server.Tests/WorkspaceGitTests.fs) or [tests/Server.Tests/GitSaveTests.fs](tests/Server.Tests/GitSaveTests.fs) — add one skippable test ``gitMove renames a tracked file in the repository`` with `--filter "gitMove renames a tracked file in the repository"` if the seam test does not assert `runGit` received `git mv`; otherwise omit to stay at 4 tests.

[ ] Test count: 2 required (classification + seam 7); 3 if a direct `gitMove` unit test is split out; at most 4 per size limit.
