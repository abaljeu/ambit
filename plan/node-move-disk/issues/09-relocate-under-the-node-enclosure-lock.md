# 09 — Relocate under the node lock
**Status**: defined
One job: The Server relocates each owned File or Directory whose derived path changed, under one `NodeLockPool.withLock` on the moved node, and then stores the graph. Story 2 Relocate with git mv and story 10 Move into a different Workspace in [Node move and disk](plan/node-move-disk/spec.md) are those relocates.

Type: coding
Blockers: [04 — Expand the node enclosure lock](04-expand-the-node-enclosure-lock.md), [05 — Relocate in the same workspace](05-relocate-in-the-same-workspace.md), [07 — Move an owned node across workspaces](07-move-an-owned-node-across-workspaces.md), [08 — Amend a change conflict before the graph is stored](08-amend-a-change-conflict-before-the-graph-is-stored.md)

## Inputs

### Working

#### Relocate caller

[ ] `DocumentPersistChange.relocateAcceptedMoves` is new in `src/Server/DocumentPersistChange.fs`.
[ ] `relocateAcceptedMoves` has type `string -> Graph -> Graph -> Result<unit, string>`.
[ ] The arguments are the data directory, the pre graph, and the post graph.
[ ] The post graph is the graph after the path-conflict rename from the blocker ticket 08.
[ ] `relocateAcceptedMoves` plans with `planPathMovesBetweenGraphs` and `coalescePathMoves`.
[ ] `sameWorkspaceMoves` selects the moves whose Workspace id stays the same.
[ ] `crossWorkspaceMoves` selects the moves whose Workspace id changes.
[ ] A coalesced move that is in neither list returns Error `path move has no workspace class`.
[ ] The moves run one at a time, in the coalesced list order.
[ ] On Error, `relocateAcceptedMoves` returns that Error and runs no later move.
[ ] `FileAgent.processPostEvents` calls `relocateAcceptedMoves` with `loaded.dataDir` after the conflict amend and before `loaded.state` is stored.
[ ] `DbAgent.finishAppliedPostChange` calls `relocateAcceptedMoves` when `liveSaveDataDir` is `Some`, after the conflict amend and before `loaded.state` is stored.
[ ] A graph-only change skips `relocateAcceptedMoves`.
[ ] `DbAgent` with `liveSaveDataDir` `None` skips `relocateAcceptedMoves`.
[ ] On relocate Error, the agent returns that Error and leaves `loaded.state` as it was.
[ ] `CoreEventDispatch.pushChangeSpecials` in `src/Server/Core/CoreEventDispatch.fs` queues `pushPersist` after `applyEvent` returns.
[ ] This ticket leaves that queue call where it is.
[ ] This ticket sets no `ParseState.Unparsed` on the moved node.

#### Same-workspace move

[ ] A same-workspace move calls `WorkspaceGit.gitMove` once.
[ ] The repository root is `DocumentPersistPath.repositoryRoot` and the git paths are `DocumentPersistPath.gitPath` on the pre graph and the post graph.
[ ] A `Special Directory` move calls `NodeLockPool.withLock` on that Directory node id around the git call.
[ ] A `Special File` move calls `NodeLockPool.withLock` on that File node id around the git call.
[ ] Inside that hold, the git call runs under `WorkspaceGit.withWorkTreeGate` on the repository root.
[ ] The gate is released before the node hold is released.

#### Cross-workspace move

[ ] The caller copies the source artifact to the destination full path and leaves the source artifact in place.
[ ] A File copy uses `File.Copy` with overwrite false.
[ ] A Directory copy copies that directory tree.
[ ] The parent directory of the destination is created before the copy.
[ ] After the copy, the caller calls `WorkspaceGit.moveAcrossWorkspaces` once.
[ ] The four arguments come from `repositoryRoot` and `gitPath` on the pre graph and the post graph.
[ ] A `Special Directory` move calls `withLock` on that Directory node id around the copy and the git call.
[ ] A `Special File` move calls `withLock` on that File node id around the copy and the git call.
[ ] Inside that hold, both repository roots are taken with `DocumentPersistPath.withWorkTreeGates`.

#### Persist and parse waits

[ ] `DocumentPersistWrite.writeDocument` calls `NodeLockPool.withLock` on the document-root node around the write, and the existing `WorkspaceGit.withWorkTreeGate` stays inside that call.
[ ] `DocumentPersistWrite.writeArtifactText` calls `withLock` on that node around the write, and its existing gate stays inside that call.
[ ] `DocumentPersistWrite.planParseFile` calls `NodeLockPool.withLock` on that node around `preparedParseText`.
[ ] `ParseThread.parseDirectory` calls `withLock` on that Directory node around `DirectoryReconcile.planDirectoryReconcile`.
[ ] `PersistThread.runRequest` stays as it is. The persist wait is the `withLock` inside `writeDocument`.

#### Copy-from

[ ] `NodeLockPool.withLock` in `src/Server/NodeLockPool.fs` has type `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`. The caller passes one `Graph` and the node id.
[ ] `gitMove`, `repositoryRoot`, and `gitPath` come from the blocker ticket 05.
[ ] `moveAcrossWorkspaces` comes from the blocker ticket 07.
[ ] ``FileAgent applyEvent marks a File Node at the later Edit id`` in `tests/Server.Tests/PersistApplyTests.fs` is the `FileAgent.create` and `applyEvent` seam.

#### Match sites

[ ] This ticket adds no union case.
[ ] This ticket changes no signature of `gitMove`, `moveAcrossWorkspaces`, `sameWorkspaceMoves`, `crossWorkspaceMoves`, or `withLock`.
[ ] `persistGraphChange` and `executePathMoves` stay on `File.Move` and `Directory.Move`.
[ ] `FileAgent.processPostEvents` and `DbAgent.finishAppliedPostChange` are the relocate callers.

### Reference

[ ] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Move write gate**, **Same-workspace relocate**, and **Cross-workspace move**, §4 flow 6 **Move under the node lock**, and §8 questions 5 and 7.
[ ] [09 — Relocate under the node lock scope](plan/node-move-disk/reports/09-relocate-under-the-node-enclosure-lock-scope.md).
[ ] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[ ] `src/Server/NodeLockPool.fs` stays as it is.
[ ] `src/Server/WorkspaceGit.fs` and `src/Server/DocumentPersistPath.fs` stay as the blocker tickets 05 and 07 leave them.
[ ] `src/Shared/DocumentPathMove.fs` stays as the blocker tickets 05, 06, and 07 leave it.
[ ] `src/Server/Core/PersistThread.fs` stays as it is.
[ ] `src/Server/Core/CoreEventDispatch.fs` stays as it is.
[ ] `src/Shared/ChangeAmendment.fs` stays as the blocker ticket 08 leaves it.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand `src/Server/DocumentPersistChange.fs` with `relocateAcceptedMoves` as Working specifies.
3. [ ] Change `src/Server/Core/FileAgent.fs` so `processPostEvents` calls `relocateAcceptedMoves` before the graph store.
4. [ ] Change `src/Server/Core/DbAgent.fs` so `finishAppliedPostChange` calls `relocateAcceptedMoves` before the graph store.
5. [ ] Change `src/Server/DocumentPersistWrite.fs` so `writeDocument`, `writeArtifactText`, and `planParseFile` call `withLock` as Working specifies.
6. [ ] Change `src/Server/ParseThread.fs` so `parseDirectory` calls `withLock` as Working specifies.
7. [ ] Expand `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` with the four tests in Outputs.

## Outputs

[ ] `src/Server/DocumentPersistChange.fs` — `relocateAcceptedMoves`.
[ ] `src/Server/Core/FileAgent.fs` — `processPostEvents` relocates, then stores.
[ ] `src/Server/Core/DbAgent.fs` — `finishAppliedPostChange` relocates, then stores.
[ ] `src/Server/DocumentPersistWrite.fs` — `withLock` around the writes and around `planParseFile`.
[ ] `src/Server/ParseThread.fs` — `withLock` around the directory reconcile.
[ ] Distinct source files: 5. `src/Server/DocumentPersistChange.fs`, `src/Server/Core/FileAgent.fs`, `src/Server/Core/DbAgent.fs`, `src/Server/DocumentPersistWrite.fs`, and `src/Server/ParseThread.fs`.
[ ] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — the four tests below.
[ ] ``Post change same-workspace relocate runs git mv under the node lock`` — `FileAgent.create` on a temp directory, a committed file in the `home` repository, and `applyEvent` of a same-Workspace rename. `GitSave.runGit` `ls-files` lists the new git path and omits the old git path. The stored graph has the new name.
[ ] ``Post change cross-workspace relocate deletes from the source repository and adds to the destination repository`` — a committed file in `home`, `applyEvent` reparents that File under `other`. `ls-files` omits the git path in `home` and lists it in `other`. `git log -1 --pretty=%s` in each repository is `gambol: cross-workspace move`.
[ ] ``Post change directory relocate drains an in-flight persist`` — `withLock` on a File under the Directory is inside its action. `applyEvent` of that Directory rename stays outside its stored-graph update until that `withLock` returns. After release, `ls-files` lists the Directory's new git path.
[ ] ``DbAgent same-workspace relocate runs git mv when a data directory is set`` — `DbAgent.createForTestWithDependencies` with `Some dataDir` stores the renamed File and `ls-files` lists the new git path.

## Stop when

[ ] Record the stop at `plan/node-move-disk/reports/09-relocate-under-the-node-enclosure-lock-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit changes `PersistThread.runRequest`, `pushChangeSpecials`, `gitMove`, `moveAcrossWorkspaces`, or `NodeLockPool`.
[ ] Stop when the edit contradicts story 2 Relocate with git mv or story 10 Move into a different Workspace in the spec.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``Post change same-workspace relocate runs git mv under the node lock``
  - `tests/Server.Tests`: ``Post change cross-workspace relocate deletes from the source repository and adds to the destination repository``
  - `tests/Server.Tests`: ``Post change directory relocate drains an in-flight persist``
  - `tests/Server.Tests`: ``DbAgent same-workspace relocate runs git mv when a data directory is set``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/node-move-disk/reports/09-relocate-under-the-node-enclosure-lock-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
