# 09 — Relocate under the node lock
**Status**: defined
One job: The Server runs the relocation from 05 — Relocate in the same workspace, 06 — Keep a nested node whose derived path differs, 07 — Move an owned node across workspaces, and 08 — Amend a change conflict before the graph is stored under the lock 13 — Lock the persist path moves installs, and then stores the graph. Story 2 Relocate with git mv and story 10 Move into a different Workspace in [Node move and disk](plan/node-move-disk/spec.md) are those relocates.

Type: coding
Blockers: [05 — Relocate in the same workspace](../05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md), [06 — Keep a nested node whose derived path differs](../06-keep-a-nested-node-whose-derived-path-differs/06-keep-a-nested-node-whose-derived-path-differs-ticket.md), [07 — Move an owned node across workspaces](../07-move-an-owned-node-across-workspaces/07-move-an-owned-node-across-workspaces-ticket.md), [08 — Amend a change conflict before the graph is stored](../08-amend-a-change-conflict-before-the-graph-is-stored/08-amend-a-change-conflict-before-the-graph-is-stored-ticket.md), [13 — Lock the persist path moves](../13-lock-the-persist-path-moves/13-lock-the-persist-path-moves-ticket.md)

Note, 2026-11-10: 13 — Lock the persist path moves holds `NodeLockPool.withLockResult` inside `executeCoalescedMove` for each coalesced `File.Move` and `Directory.Move`. This ticket puts the 05–08 relocation under that same lock. The stop at [09 — Relocate under the node lock stop](plan/node-move-disk/issues/09-relocate-under-the-node-enclosure-lock/09-build.md) stays the record of the earlier build.

## Scope

Slice: Flow 6 **Move under the node lock** in [Node move and disk architecture](plan/node-move-disk/arch.md) section 4. Post change is the single relocate caller. It classifies moves and runs `gitMove` or `moveAcrossWorkspaces` inside one `NodeLockPool.withLock` on `move.nodeId`, then stores the graph and queues persists. A holder does not take a second lock. `NodeLockRejection` is only `HolderHoldsLock`.

### Signatures

[x] `DocumentPathMove.planPathMovesBetweenGraphs : Graph -> Graph -> DocumentPathMove list` in `src/Shared/DocumentPathMove.fs`.

[x] `DocumentPathMove.coalescePathMoves : Graph -> DocumentPathMove list -> DocumentPathMove list` in `src/Shared/DocumentPathMove.fs`.

[x] `DocumentPathMove.sameWorkspaceMoves : Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list` lands in [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md); it is absent from source until that ticket merges.

[x] `DocumentPathMove.crossWorkspaceMoves : Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list` lands in [07 — Move an owned node across workspaces](plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces/07-move-an-owned-node-across-workspaces-ticket.md); it is absent from source until that ticket merges.

[x] `GraphQuery.enclosingWorkspace : Graph -> NodeId -> NodeId option` in `src/Shared/GraphQuery.fs`.

[x] `GraphQuery.enclosing : Graph -> (Node -> bool) -> NodeId -> NodeId option` in `src/Shared/GraphQuery.fs` walks `ownerParentByChild` with a visited set. `NodeLockPool.ownerChain` is that parent walk and does not call `GraphQuery`.

[x] `DocumentPersistPath.repositoryRoot : string -> Graph -> NodeId -> string option` and `DocumentPersistPath.gitPath : Graph -> NodeId -> string option` land in [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md); they are absent from source until that ticket merges.

[ ] `WorkspaceGit.gitMove : string -> string -> string -> Result<unit, string>` lands in [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md).

[ ] `WorkspaceGit.moveAcrossWorkspaces : string -> string -> string -> string -> Result<unit, string>` lands in [07 — Move an owned node across workspaces](plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces/07-move-an-owned-node-across-workspaces-ticket.md); per that ticket, the destination work tree already holds the artifact at the destination git path before the call.

[x] `WorkspaceGit.withWorkTreeGate` is absent. `NodeLockPool.withLock` has type `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`. `withLockResult` is internal.

[x] `GitSave.runGit : string -> string -> Result<string, string>` in `src/Server/GitSave.fs` calls `GitRun.gitExec`.

[x] `NodeEnclosureLock`, `withNodeLock`, `withPersist`, and `withParseRead` are absent. [11 — Expand the node lock pool](../11-expand-the-node-lock-pool/11-expand-the-node-lock-pool-ticket.md) replaced them with `NodeLockPool.withLock`.

[x] `DocumentPersistChange.validatePathMoves : string -> Graph -> Graph -> Result<unit, string>` in `src/Server/DocumentPersistChange.fs`.

[x] `DocumentPersistChange.executePathMoves` folds `executeCoalescedMove`. That takes `withLockResult` on the post graph and `move.nodeId`, then calls `resolveMovePaths` and `executePathMove`. `executePathMove` uses `File.Move` and `Directory.Move`, and calls `validateDestinationAvailable`.

[x] `DocumentPersistChange.persistNode : string -> Graph -> NodeId -> Result<DateTime, string>` calls `DocumentPersistWrite.writeDocument`.

[x] `FileAgent.processPostEvents` is private in `src/Server/Core/FileAgent.fs`. It calls `validatePostChange`, then assigns `loaded.state.Value <- newState`.

[x] `DbAgent.finishAppliedPostChange` is private in `src/Server/Core/DbAgent.fs`. It calls `validatePostChange`, `persistLiveChange`, then `commitPostChange`, which sets `loaded.state.Value <- stateToStore`.

[x] `PersistThread.runRequest` is private in `src/Server/Core/PersistThread.fs`. It calls `deps.persistNode` when `CoreEventDispatch.persistedNode` is false. This slice does not add the Unparsed reject at the top of `runRequest`.

### Match sites

[x] `FileAgent.applyOne` in `src/Server/Core/FileAgent.fs` calls `ChangeAmendment.applyPosted` before post-change validation. [08 — Amend a change conflict before the graph is stored](plan/node-move-disk/issues/08-amend-a-change-conflict-before-the-graph-is-stored/08-amend-a-change-conflict-before-the-graph-is-stored-ticket.md) extends that path with disk-conflict rename ops before store. That extension is not in the tree.

[ ] `FileAgent.processPostEvents` in `FileAgent.fs` calls `applyBatch` then `validatePostChange loaded graphOnly preGraph newState.graph` with `preGraph = loaded.state.Value.graph`; on `Ok ()` it stores the graph. This slice inserts classify-and-relocate after validation `Ok` and before `loaded.state.Value <- newState`.

[x] `FileAgent.validatePostChange` calls `DocumentPersistChange.validatePathMoves loaded.dataDir` and `validateGraphDiskEffects` when `graphOnly` is false.

[x] `FileAgent.coreMailboxAccess` sets `applyEvent` to `processPostEvents loaded [ event ] graphOnly` and `pushPersist` to `PersistRequestStack.pushBoundNode loaded.stack`.

[x] `DbAgent.applyOneEvent` calls `ChangeAmendment.applyPosted` inside the `Ev.ops` branch; `DbAgent.processPostEvents` calls `finishAppliedPostChange` when the applied event id advances.

[x] `DbAgent.validatePostChange` calls `validatePathMoves` only when `liveSaveDataDir` is `Some dataDir`. It returns `Ok ()` for `graphOnly` or when `liveSaveDataDir` is `None`.

[ ] `DbAgent.finishAppliedPostChange` matches `FileAgent` on validation then store; this slice inserts the same relocate step after validation `Ok` and before `commitPostChange` when `graphOnly` is false and a data directory is present (`loaded.dataDir` is always set on `FileAgent`; `liveSaveDataDir` on `DbAgent`).

[ ] `DocumentPersistChange.relocateAcceptedMoves` takes `dataDir`, `preGraph`, and `postGraph`. It coalesces with `coalescePathMoves`, partitions with `sameWorkspaceMoves` and `crossWorkspaceMoves`, and for each move calls `NodeLockPool.withLockResult` on the post graph and `move.nodeId`. `gitMove` or `moveAcrossWorkspaces` runs inside that action. The caller passes no second node id. `withWorkTreeGate`, `withNodeLock`, `withPersist`, and `withParseRead` are absent and are not part of this call.

[ ] Cross-workspace byte copy: [07 — Move an owned node across workspaces](plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces/07-move-an-owned-node-across-workspaces-ticket.md) requires the destination work tree to hold the File or Directory before `moveAcrossWorkspaces`; `moveAcrossWorkspaces` returns `Error` and runs no git command when the destination path is missing. The Post change relocate caller (the new `DocumentPersistChange` helper or inline agent step), not `moveAcrossWorkspaces`, copies bytes with `executePathMoves` (or `executePathMove` via `resolveMovePaths`) on `crossWorkspaceMoves` only, then calls `moveAcrossWorkspaces`.

[ ] Same-workspace moves: one `gitMove` per classified move; `gitMove` runs `git mv` in the work tree via `GitSave.runGit` per [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md). This slice does not call `executePathMoves` for same-workspace classified moves when `gitMove` handles the tree.

[x] `DocumentPersistChange.persistGraphChangeWith` calls `executePathMoves` then `writeDocumentsSoft`. The path-move lock is inside `executeCoalescedMove`. This slice does not reroute `persistGraphChange` or `persistGraphOps` through git relocate.

[x] Lock order is one `withLockResult` on `move.nodeId`. Git runs inside that action. The action's return releases the request. A second `withLock` on that thread returns `HolderHoldsLock`. There is no work-tree gate to nest.

[x] A Directory move and a File move use that same call on `move.nodeId`. A File move does not also lock the parent Directory.

[x] `CoreEventDispatch.persistNew` calls `persist` (`context.persist.applyEvent`), then `store`, then `markAcceptedSpecials`.
[ ] Relocate runs inside `applyEvent` before the graph store step in the agent. That call is not in the tree.

[x] `CoreEventDispatch.pushChangeSpecials` calls `DocumentOpImpact.owningSpecials` then `pushPersist` after `applyEvent` returns. `CoreMailboxFilesAxis.pushParsedUnpersisted` also calls `persist.pushPersist` when parse state is Parsed and persist state is Unpersisted.

[x] Persist queue for a posted Change stays outside the agent today: this slice leaves `markAcceptedSpecials` → `pushChangeSpecials` → `pushPersist` in `CoreEventDispatch.fs` unchanged; Post change still reaches the persist stack through that path after store.

[x] `PersistThread.runRequest` is not an edit. `writeDocument` and `writeArtifactText` already call `withLockResult`. A conflicting persist waits inside that call. This ticket does not take a second lock around them.

[x] Parse reads already call the pool. `preparedParseText` calls `withLockResult` around a file-body read. `ParseThread.parseFile` calls `DocumentPersistWrite.planParseFile`. `ParseThread.reconcileDirectory` calls `withLockResult` around `DirectoryReconcile.planDirectoryReconcile`. This ticket does not add `withParseRead`.

[x] `persistNode` calls `writeDocument`. The persist lock is already `withLockResult` on the document-root node. This ticket does not wrap it again.

### Existing tests

[x] ``planPathMovesBetweenGraphs includes file reparent across workspaces`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` builds the `home` to `other` File reparent move.

[x] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` checks coalesce for a directory rename.

[x] ``persistGraphChange moves renamed file artifact before writing`` and ``persistGraphChange moves renamed directory tree before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` assert `File.Move` / `Directory.Move` through `persistGraphChange`, not Post change git relocate.

[ ] [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md) names ``same-workspace relocate runs git mv in the Workspace repository`` and ``gitMove moves a directory tree with one git mv`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs`; those tests call the relocate helpers directly with no production caller until this slice.

[ ] [07 — Move an owned node across workspaces](plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces/07-move-an-owned-node-across-workspaces-ticket.md) names ``cross-workspace move deletes from the source repository and adds to the destination repository`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` as a direct `moveAcrossWorkspaces` caller test.

[x] Those three enclosure-lock test names are absent. The current pool tests are in `tests/Server.Tests/NodeLockPoolTests.fs`, including ``taking a directory waits while a descendant file is held and a sibling file enters`` and ``an absent node proceeds and the same thread is rejected``.

[x] ``FileAgent applyEvent applies Ev Ops without leftover Change`` and related facts in `tests/Server.Tests/PersistApplyTests.fs` use `(FileAgent.persist (FileAgent.create dataDir)).handlers.applyEvent` and `handlers.graphAtEvent ()` as the public Post change seam.

[x] ``DbAgent applyEvent applies Ev Ops without leftover Change`` in `PersistApplyTests.fs` uses `DbAgent.createForTest` handlers the same way.

[ ] Arch section 6 has no seam 10 yet; seam 7 and seam 8 name git relocate tests in `DocumentPathMoveExecutionTests.fs` without FileAgent or the node lock.

### Copy-from

[x] Disk byte placement before cross-workspace git: private `executePathMove` and `executePathMoves` in `src/Server/DocumentPersistChange.fs`.

[ ] Same-workspace git: `WorkspaceGit.gitMove` from [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md).

[ ] Cross-workspace git: `WorkspaceGit.moveAcrossWorkspaces` from [07 — Move an owned node across workspaces](plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces/07-move-an-owned-node-across-workspaces-ticket.md); read behavior only from that ticket, not from source.

[ ] Repository and git-relative paths: `DocumentPersistPath.repositoryRoot` and `gitPath` from [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md).

[x] Lock to reuse: `NodeLockPool.withLockResult` inside `executeCoalescedMove` in `src/Server/DocumentPersistChange.fs`. The wait shape is ``taking a directory waits while a descendant file is held and a sibling file enters`` in `tests/Server.Tests/NodeLockPoolTests.fs`. `withNodeLock`, `withPersist`, `withParseRead`, and ``work tree gate waits then continues without overlap`` are absent.

[x] `DocumentPersistPath.withWorkTreeGates` is absent. `persistGraphChangeWith` calls `executePathMoves`, then `writeDocumentsSoft`.

[x] Post-change handler test wiring: `PersistApplyTests.fs` `applyThenAppend`, `fileHandlers`, `requireOk "applyEvent" (handlers.applyEvent event false)`, and `DocumentPathMoveExecutionTests.fs` `graphWithNestedDocs`, `newTempDir`, `WorkspaceGit.ensureInit`.

### write-ticket

[ ] Blockers: [04 — Expand the node enclosure lock](plan/node-move-disk/issues/04-expand-the-node-enclosure-lock/04-expand-the-node-enclosure-lock-ticket.md), [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace/05-relocate-in-the-same-workspace-ticket.md), [07 — Move an owned node across workspaces](plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces/07-move-an-owned-node-across-workspaces-ticket.md); [08 — Amend a change conflict before the graph is stored](plan/node-move-disk/issues/08-amend-a-change-conflict-before-the-graph-is-stored/08-amend-a-change-conflict-before-the-graph-is-stored-ticket.md) may land amended graphs before this relocate step.

[ ] Flow 6 order in [arch.md](plan/node-move-disk/arch.md): after Accept validation passes, classify moves, hold one `withLockResult` on `move.nodeId`, run same-workspace or cross-workspace git inside that action, release, store graph. A second lock is `HolderHoldsLock`. The persist queue stays `CoreEventDispatch.pushChangeSpecials`.

[ ] Proposed arch seam 10 **Post change relocate under lock** / **Move write gate**: two Server tests observe git relocate from `FileAgent` `handlers.applyEvent`, not from direct helper calls alone.

[ ] This slice does not change `PersistThread.runRequest` Unparsed reject behavior; that rule stays a separate ticket per [arch.md](plan/node-move-disk/arch.md) **Persist thread write**.

#### Source files

[ ] `src/Server/DocumentPersistChange.fs` — add `relocateAcceptedMoves`: coalesce, classify, one `withLockResult` on `move.nodeId`, `gitMove` or `moveAcrossWorkspaces` inside that action.

[ ] `src/Server/Core/FileAgent.fs` — call `relocateAcceptedMoves` in `processPostEvents` after `validatePostChange` returns `Ok` and before the graph store. Pass `loaded.dataDir`.

[ ] `src/Server/Core/DbAgent.fs` — the same call in `finishAppliedPostChange` when `liveSaveDataDir` is `Some`. Skip when `graphOnly` or there is no live data directory.

[x] `src/Server/DocumentPersistWrite.fs` and `src/Server/ParseThread.fs` already call `withLockResult`. This slice does not wrap them again.

[ ] Distinct source files: 3. `DocumentPersistChange.fs`, `FileAgent.fs`, and `DbAgent.fs`. Under the 8-file limit in [Size limit](plan/context/size-limit.md).

[x] `PersistThread.fs` stays closed. The persist wait is the existing `withLockResult` in `writeDocument`.

[ ] `src/Shared/DocumentPathMove.fs`, `src/Server/WorkspaceGit.fs`, and `src/Server/DocumentPersistPath.fs` come from 05 and 07. This slice calls `sameWorkspaceMoves`, `crossWorkspaceMoves`, `gitMove`, `moveAcrossWorkspaces`, `repositoryRoot`, and `gitPath` and does not redefine them. `NodeLockPool.fs` stays as it is.

[ ] `src/Server/Core/CoreEventDispatch.fs` stays unchanged for persist queue; `pushChangeSpecials` remains the `pushPersist` caller after `applyEvent`.

#### Tests

[ ] ``Post change same-workspace relocate runs git mv under the node lock`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` — seed the `home` repo, post a same-Workspace path change through `FileAgent` `applyEvent`, and assert `git ls-files` shows the new git path. The git call runs inside the lock from 13.

[ ] ``Post change cross-workspace relocate deletes from the source repository and adds to the destination repository`` — two Workspace repos, `applyEvent` reparents the File, and the git indexes match [07 — Move an owned node across workspaces](../07-move-an-owned-node-across-workspaces/07-move-an-owned-node-across-workspaces-ticket.md).

[ ] ``Post change directory relocate drains an in-flight persist`` — `withLock` on a File under the Directory stays inside its action while `applyEvent` of that Directory rename waits.

[ ] ``DbAgent same-workspace relocate runs git mv when a data directory is set`` — `DbAgent.createForTestWithDependencies` with `Some dataDir`.

[ ] Distinct tests added or changed: 2 required (same-workspace and cross-workspace Post change); up to 4 total per [Size limit](plan/context/size-limit.md).

[ ] Register `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` in `Gambol.Server.Tests.fsproj` only if the file is new; the module already exists.

#### Split

[ ] No split. Five source files (agents, relocate orchestration, `DocumentPersistWrite` persist lock, parse-read wire in `ParseThread.fs` or `DocumentPersistWrite.fs`) stay within the 8-file limit.

[x] Parse reads and persist writes already call `withLockResult`. A later pass does not add `withPersist`, `withParseRead`, or `withWorkTreeGate`. Job 1 is Post change classify and git under the one `withLock` on `move.nodeId`, then store.

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
[x] `CoreEventDispatch.pushChangeSpecials` in `src/Server/Core/CoreEventDispatch.fs` queues `pushPersist` after `applyEvent` returns.
[x] This ticket leaves that queue call where it is.
[ ] This ticket sets no `ParseState.Unparsed` on the moved node.

#### Lock from 13 — Lock the persist path moves

[ ] `relocateAcceptedMoves` calls `NodeLockPool.withLockResult` on the post graph and the node id 13 — Lock the persist path moves uses for that move.
[ ] `gitMove` or `moveAcrossWorkspaces` runs inside that action.
[ ] That call uses the same graph and the same node id as the `withLock` around `File.Move` and `Directory.Move`.
[x] `executeCoalescedMove` keeps `withLockResult` around `File.Move` and `Directory.Move`. `persistGraphChangeWith` calls `executePathMoves`.
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

[x] `NodeLockPool.withLockResult` inside `executeCoalescedMove` is the lock this ticket reuses. 13 — Lock the persist path moves added that call. This ticket does not edit the pool.
[ ] `gitMove`, `repositoryRoot`, and `gitPath` come from 05 — Relocate in the same workspace.
[ ] `moveAcrossWorkspaces` comes from 07 — Move an owned node across workspaces.
[ ] ``FileAgent applyEvent marks a File Node at the later Edit id`` in `tests/Server.Tests/PersistApplyTests.fs` is the `FileAgent.create` and `applyEvent` seam.

#### Match sites

[ ] This ticket adds no union case.
[ ] This ticket changes no signature of `gitMove`, `moveAcrossWorkspaces`, `sameWorkspaceMoves`, `crossWorkspaceMoves`, or `withLock`.
[ ] `FileAgent.processPostEvents` and `DbAgent.finishAppliedPostChange` are the relocate callers.

### Reference

[x] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Same-workspace relocate** and **Cross-workspace move**, and §4 flow 6 **Move under the node lock**.
[x] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[x] `src/Server/NodeLockPool.fs` stays as it is.
[x] `src/Server/WorkspaceGit.fs` and `src/Server/DocumentPersistPath.fs` stay as 05 and 07 leave them. Those helpers are still absent.
[x] `src/Shared/DocumentPathMove.fs` stays as 05, 06, and 07 leave it. `sameWorkspaceMoves` and `crossWorkspaceMoves` are still absent.
[x] `src/Server/DocumentPersistWrite.fs` stays as it is. It already calls `withLockResult`.
[x] `src/Server/ParseThread.fs` stays as it is. `reconcileDirectory` already calls `withLockResult`.
[x] `src/Server/Core/PersistThread.fs` stays as it is.
[x] `src/Server/Core/CoreEventDispatch.fs` stays as it is.
[x] `src/Shared/ChangeAmendment.fs` stays as 08 leaves it. The conflict rename is still absent.

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

[x] Record the stop at `plan/node-move-disk/issues/09-relocate-under-the-node-enclosure-lock/09-build.md`, then ask.
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
[ ] Write the verdict to `plan/node-move-disk/issues/09-relocate-under-the-node-enclosure-lock/09-build.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
