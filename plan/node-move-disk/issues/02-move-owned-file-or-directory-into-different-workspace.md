# 02 — Moving an owned File or Directory into a different Workspace

**Type:** coding
**Status:** defined
**Blocked by:** None

## Context

A person moves an owned File Node or an owned Directory Node so the Workspace Node on its Owner chain changes. The graph outline is to match the file tree. Files are shared by git push and pull.

## Observed

The derived path follows the Owner chain.

- `GraphBuild.addOwnerEdges` records `ownerParentByChild` for an `Ownership.Owner` edge.
- `GraphBuild.appendChildren` records `ownerParentByChild` for an `Ownership.Owner` edge.
- `Node.owner` comes from `ownerParentByChild`.
- `NodeDesktopPath.expandedPathForNodeId` walks `node.owner`. A Normal Node on that chain passes the walk to its owner.
- `DocumentPathMove.planPathMovesBetweenGraphs` records a move when the expanded path changes.
- The test `planPathMovesBetweenGraphs includes file reparent across workspaces` records `//home/readme.txt` to `//other/readme.txt`.
- `DocumentPartition.artifactFileRelative` and `DocumentPartition.artifactDirectoryRelative` build the data-directory relative path.
- The private `workspaceDiskPrefix` in `DocumentPartition` puts the Workspace name first.
- `DocumentPersistPath.resolveUnderDataDir` resolves that relative path under the one data directory.

Accept validation runs before the new graph is stored.

- `FileAgent.validatePostChange` calls `DocumentPersistChange.validatePathMoves` for a persisted change.
- A graph-only change returns Ok from `FileAgent.validatePostChange`.
- A graph-only change returns Ok from `DbAgent.validatePostChange`.
- `DbAgent.validatePostChange` calls `DocumentPersistChange.validatePathMoves` when `liveSaveDataDir` is set.
- The private `validateDestinationAvailable` returns `disk path already exists` when the destination full path exists and the full paths differ.
- `FileAgent.processPostEvents` returns that error and keeps the stored graph.
- `DbAgent.finishAppliedPostChange` returns that error and keeps the stored graph.
- `ChangeAmendment.applyPosted` runs in `FileAgent.applyOne` and `DbAgent.applyOneEvent` before that validation.
- `ChangeAmendment` amends a compare-and-swap mismatch for old text.
- `ChangeAmendment` amends a compare-and-swap mismatch for old name.
- `ChangeAmendment` amends a compare-and-swap mismatch for old classes.
- `ChangeAmendment` amends a compare-and-swap mismatch for old span.

The live persist writes the post-graph artifact and leaves the old file at the old path.

- `CoreEventDispatch.pushChangeSpecials` pushes `DocumentOpImpact.owningSpecials`.
- `PersistThread.runRequest` calls `DocumentPersistChange.persistNode`.
- `persistNode` calls `DocumentPersistWrite.writeDocument`.
- `writeDocument` writes the artifact at the post-graph path.
- `DocumentPersistChange.executePathMoves` relocates with `File.Move` and `Directory.Move` when the source path exists.
- `persistGraphChange` and `persistGraphOps` call `executePathMoves`.
- Tests call `persistGraphChange` and `persistGraphOps`.
- `FileAgent` and `DbAgent` call `persistNode`.

A named Workspace directory and the data directory are separate git roots when each has a `.git` directory.

- `WorkspaceGit.ensureInit` creates `workspaceRoot/.git`.
- `DocumentPersistWrite.ensureWorkspaceRepo` calls `ensureInit` when a Workspace node is written and `WorkspaceGit.isRepo` is false for that directory.
- `WorkspaceGit.isRepo` checks `workspaceRoot/.git`.
- `GitSave.isRepo` checks `dataDir/.git`.
- The private `addAllArguments` in `GitSave` passes each name from the private `nestedRepoNames` as a path exclusion on `git add`.
- `DailyGitSave.discoverRepoRoots` commits the data-directory repository and each child Workspace repository on their own.

## Expected

A person moves an owned File Node or an owned Directory Node under a different Workspace Node. The file or directory on disk is at the derived path under that Workspace Node. Each owned File Node and Directory Node under a moved Directory Node is at its derived path. This holds when the moved node is a direct child or deeper on the Owner chain, and when the File Node is parsed or unparsed. The graph outline matches that file tree. When that move detects a conflict, `ChangeAmendment` disambiguates before the change lands. The rule is Decision in [Node move and disk](plan/node-move-disk/project.md). The file or directory is deleted from the source Workspace repository and added to the destination Workspace repository.

## Notes

- 2026-11-09 — Alan: moving a file or directory to a different workspace is to be supported. Only owned nodes persist. The user story for this move is added in spec.md by [Node move: write the spec](https://github.com/abaljeu/ambit/pull/560).
- 2026-11-09 — Alan: "The obvious effect. Delete from one. Add to one."
