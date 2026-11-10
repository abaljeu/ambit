# 09 — Relocate under the node lock
**Status**: defined
One job: The Server runs the relocation from 05 — Relocate in the same workspace, 06 — Keep a nested node whose derived path differs, 07 — Move an owned node across workspaces, and 08 — Amend a change conflict before the graph is stored under the lock 13 — Lock the persist path moves installs, and then stores the graph. Story 2 Relocate with git mv and story 10 Move into a different Workspace in [Node move and disk](plan/node-move-disk/spec.md) are those relocates.

Type: coding
Blockers: [05 — Relocate in the same workspace](05-relocate-in-the-same-workspace.md), [06 — Keep a nested node whose derived path differs](06-keep-a-nested-node-whose-derived-path-differs.md), [07 — Move an owned node across workspaces](07-move-an-owned-node-across-workspaces.md), [08 — Amend a change conflict before the graph is stored](08-amend-a-change-conflict-before-the-graph-is-stored.md), [13 — Lock the persist path moves](13-lock-the-persist-path-moves.md)

Note, 2026-11-10: 13 — Lock the persist path moves wraps each coalesced `File.Move` and `Directory.Move` in `persistGraphChangeWith`. This ticket puts the 05–08 relocation under that lock. The stop at [09 — Relocate under the node lock stop](plan/node-move-disk/reports/09-relocate-under-the-node-enclosure-lock-stop.md) stays the record of the earlier build. [09 — Relocate under the node lock scope](plan/node-move-disk/reports/09-relocate-under-the-node-enclosure-lock-scope.md) is the pre-trim record.

## Inputs

### Working

#### Relocate caller

[ ] `DocumentPersistChange.relocateAcceptedMoves` is new in `src/Server/DocumentPersistChange.fs`.
[ ] `relocateAcceptedMoves` has type `string -> Graph -> Graph -> Result<unit, string>`.
[ ] The arguments are the data directory, the pre graph, and the post graph.
[ ] The post graph is the graph after the path-conflict rename from 08 — Amend a change conflict before the graph is stored.
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

#### Lock from 13 — Lock the persist path moves

[ ] `relocateAcceptedMoves` calls `NodeLockPool.withLockResult` on the post graph and the node id 13 — Lock the persist path moves uses for that move.
[ ] `gitMove` or `moveAcrossWorkspaces` runs inside that action.
[ ] That call uses the same graph and the same node id as the `withLock` around `File.Move` and `Directory.Move`.
[ ] `persistGraphChangeWith` keeps the `withLock` around `File.Move` and `Directory.Move`.
[ ] This ticket adds no other node id.

#### Same-workspace move

[ ] A same-workspace move calls `WorkspaceGit.gitMove` once.
[ ] The repository root is `DocumentPersistPath.repositoryRoot` and the git paths are `DocumentPersistPath.gitPath` on the pre graph and the post graph.

#### Cross-workspace move

[ ] The caller copies the source artifact to the destination full path and leaves the source artifact in place.
[ ] A File copy uses `File.Copy` with overwrite false.
[ ] A Directory copy copies that directory tree.
[ ] The parent directory of the destination is created before the copy.
[ ] After the copy, the caller calls `WorkspaceGit.moveAcrossWorkspaces` once.
[ ] The four arguments come from `repositoryRoot` and `gitPath` on the pre graph and the post graph.

#### Copy-from

[ ] `NodeLockPool.withLockResult` around each coalesced move in `persistGraphChangeWith` is the lock this ticket reuses. 13 — Lock the persist path moves adds that call. This ticket does not edit the pool.
[ ] `gitMove`, `repositoryRoot`, and `gitPath` come from 05 — Relocate in the same workspace.
[ ] `moveAcrossWorkspaces` comes from 07 — Move an owned node across workspaces.
[ ] ``FileAgent applyEvent marks a File Node at the later Edit id`` in `tests/Server.Tests/PersistApplyTests.fs` is the `FileAgent.create` and `applyEvent` seam.

#### Match sites

[ ] This ticket adds no union case.
[ ] This ticket changes no signature of `gitMove`, `moveAcrossWorkspaces`, `sameWorkspaceMoves`, `crossWorkspaceMoves`, or `withLock`.
[ ] `FileAgent.processPostEvents` and `DbAgent.finishAppliedPostChange` are the relocate callers.

### Reference

[ ] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Same-workspace relocate** and **Cross-workspace move**, and §4 flow 6 **Move under the node lock**.
[ ] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[ ] `src/Server/NodeLockPool.fs` stays as it is.
[ ] `src/Server/WorkspaceGit.fs` and `src/Server/DocumentPersistPath.fs` stay as 05 — Relocate in the same workspace and 07 — Move an owned node across workspaces leave them.
[ ] `src/Shared/DocumentPathMove.fs` stays as 05 — Relocate in the same workspace, 06 — Keep a nested node whose derived path differs, and 07 — Move an owned node across workspaces leave it.
[ ] `src/Server/DocumentPersistWrite.fs` stays as it is.
[ ] `src/Server/ParseThread.fs` stays as it is.
[ ] `src/Server/Core/PersistThread.fs` stays as it is.
[ ] `src/Server/Core/CoreEventDispatch.fs` stays as it is.
[ ] `src/Shared/ChangeAmendment.fs` stays as 08 — Amend a change conflict before the graph is stored leaves it.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand `src/Server/DocumentPersistChange.fs` with `relocateAcceptedMoves` as Working specifies.
3. [ ] Change `src/Server/Core/FileAgent.fs` so `processPostEvents` calls `relocateAcceptedMoves` before the graph store.
4. [ ] Change `src/Server/Core/DbAgent.fs` so `finishAppliedPostChange` calls `relocateAcceptedMoves` before the graph store.
5. [ ] Expand `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` with the four tests in Outputs.

## Outputs

[ ] `src/Server/DocumentPersistChange.fs` — `relocateAcceptedMoves`.
[ ] `src/Server/Core/FileAgent.fs` — `processPostEvents` relocates, then stores.
[ ] `src/Server/Core/DbAgent.fs` — `finishAppliedPostChange` relocates, then stores.
[ ] Distinct source files: 3. `src/Server/DocumentPersistChange.fs`, `src/Server/Core/FileAgent.fs`, and `src/Server/Core/DbAgent.fs`.
[ ] `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — the four tests below.
[ ] ``Post change same-workspace relocate runs git mv under the node lock`` — `FileAgent.create` on a temp directory, a committed file in the `home` repository, and `applyEvent` of a same-Workspace rename. `GitSave.runGit` `ls-files` lists the new git path and omits the old git path. The stored graph has the new name. The git call runs inside the lock from 13 — Lock the persist path moves.
[ ] ``Post change cross-workspace relocate deletes from the source repository and adds to the destination repository`` — a committed file in `home`, `applyEvent` reparents that File under `other`. `ls-files` omits the git path in `home` and lists it in `other`. `git log -1 --pretty=%s` in each repository is `gambol: cross-workspace move`.
[ ] ``Post change directory relocate drains an in-flight persist`` — `withLock` on a File under the Directory is inside its action. `applyEvent` of that Directory rename stays outside its stored-graph update until that `withLock` returns. After release, `ls-files` lists the Directory's new git path.
[ ] ``DbAgent same-workspace relocate runs git mv when a data directory is set`` — `DbAgent.createForTestWithDependencies` with `Some dataDir` stores the renamed File and `ls-files` lists the new git path.

## Stop when

[x] Record the stop at `plan/node-move-disk/reports/09-relocate-under-the-node-enclosure-lock-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit changes `PersistThread.runRequest`, `pushChangeSpecials`, `gitMove`, `moveAcrossWorkspaces`, or `NodeLockPool`.
[ ] Stop when the edit adds a `withLock` on a node id other than the one 13 — Lock the persist path moves names.
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
