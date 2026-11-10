# 09 — Relocate under the node enclosure lock (scope)

Slice: Flow 6 **Move under the node lock** in [Node move and disk architecture](plan/node-move-disk/arch.md) section 4. Post change is the single relocate caller. It classifies moves and runs `gitMove` or `moveAcrossWorkspaces` under the node enclosure lock (or the File's per-member persist lock), then stores the graph and queues persists.

## Signatures

[ ] `DocumentPathMove.planPathMovesBetweenGraphs : Graph -> Graph -> DocumentPathMove list` in `src/Shared/DocumentPathMove.fs`.

[ ] `DocumentPathMove.coalescePathMoves : Graph -> DocumentPathMove list -> DocumentPathMove list` in `src/Shared/DocumentPathMove.fs`.

[ ] `DocumentPathMove.sameWorkspaceMoves : Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list` lands in [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md); it is absent from source until that ticket merges.

[ ] `DocumentPathMove.crossWorkspaceMoves : Graph -> Graph -> DocumentPathMove list -> DocumentPathMove list` lands in [07 — Move an owned node across workspaces](plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces.md); it is absent from source until that ticket merges.

[ ] `GraphQuery.enclosingWorkspace : Graph -> NodeId -> NodeId option` in `src/Shared/GraphQuery.fs`.

[ ] `GraphQuery.enclosing : Graph -> (Node -> bool) -> NodeId -> NodeId option` in `src/Shared/GraphQuery.fs` lines 16–34; [04 — Expand the node enclosure lock](plan/node-move-disk/issues/04-expand-the-node-enclosure-lock.md) uses this walk for the per-member persist owner chain.

[ ] `DocumentPersistPath.repositoryRoot : string -> Graph -> NodeId -> string option` and `DocumentPersistPath.gitPath : Graph -> NodeId -> string option` land in [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md); they are absent from source until that ticket merges.

[ ] `WorkspaceGit.gitMove : string -> string -> string -> Result<unit, string>` lands in [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md).

[ ] `WorkspaceGit.moveAcrossWorkspaces : string -> string -> string -> string -> Result<unit, string>` lands in [07 — Move an owned node across workspaces](plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces.md); per that ticket, the destination work tree already holds the artifact at the destination git path before the call.

[ ] `WorkspaceGit.withWorkTreeGate : string -> (unit -> Result<'value, string>) -> Result<'value, string>` in `src/Server/WorkspaceGit.fs` lines 43–55.

[ ] `GitSave.runGit : string -> string -> Result<string, string>` in `src/Server/GitSave.fs`.

[ ] `NodeEnclosureLock.withNodeLock : NodeId -> (unit -> 'a) -> 'a`, `NodeEnclosureLock.withPersist : Graph -> NodeId -> (NodeId list -> 'a) -> 'a`, and `NodeEnclosureLock.withParseRead : Graph -> NodeId -> (unit -> 'a) -> 'a` land in [04 — Expand the node enclosure lock](plan/node-move-disk/issues/04-expand-the-node-enclosure-lock.md) in new `src/Server/NodeEnclosureLock.fs`; the module is absent from source until that ticket merges.

[ ] `DocumentPersistChange.validatePathMoves : string -> Graph -> Graph -> Result<unit, string>` in `src/Server/DocumentPersistChange.fs` lines 112–128.

[ ] `DocumentPersistChange.executePathMoves : string -> Graph -> Graph -> DocumentPathMove list -> Result<unit, string>` in `src/Server/DocumentPersistChange.fs` lines 164–180; private `executePathMove` uses `File.Move` and `Directory.Move`.

[ ] `DocumentPersistChange.persistNode : string -> Graph -> NodeId -> Result<DateTime, string>` in `src/Server/DocumentPersistChange.fs` line 261 calls `DocumentPersistWrite.writeDocument`.

[ ] `FileAgent.processPostEvents : LoadedFile -> Ev list -> bool -> Result<CoreChangesAccepted, string>` is private in `src/Server/Core/FileAgent.fs` lines 107–123; it calls `validatePostChange`, then assigns `loaded.state.Value <- newState`.

[ ] `DbAgent.finishAppliedPostChange` is private in `src/Server/Core/DbAgent.fs` lines 338–359; it calls `validatePostChange`, `persistLiveChange`, then `commitPostChange` which sets `loaded.state.Value <- stateToStore`.

[ ] `PersistThread.runRequest` is private in `src/Server/Core/PersistThread.fs` lines 30–50; it calls `deps.persistNode` when `CoreEventDispatch.persistedNode` is false. This slice does not add the Unparsed reject at the top of `runRequest`.

## Match sites

[ ] `FileAgent.applyOne` in `src/Server/Core/FileAgent.fs` lines 63–69 calls `ChangeAmendment.applyPosted` before post-change validation; [08 — Amend a change conflict before the graph is stored](plan/node-move-disk/issues/08-amend-a-change-conflict-before-the-graph-is-stored.md) extends that path with disk-conflict rename ops before store.

[ ] `FileAgent.processPostEvents` in `FileAgent.fs` calls `applyBatch` then `validatePostChange loaded graphOnly preGraph newState.graph` with `preGraph = loaded.state.Value.graph`; on `Ok ()` it stores the graph. This slice inserts classify-and-relocate after validation `Ok` and before `loaded.state.Value <- newState`.

[ ] `FileAgent.validatePostChange` in `FileAgent.fs` lines 93–105 calls `DocumentPersistChange.validatePathMoves loaded.dataDir` and `validateGraphDiskEffects` when `graphOnly` is false.

[ ] `FileAgent.coreMailboxAccess` in `FileAgent.fs` line 148 sets `applyEvent = fun event graphOnly -> processPostEvents loaded [ event ] graphOnly` and line 154 sets `pushPersist = PersistRequestStack.pushBoundNode loaded.stack`.

[ ] `DbAgent.applyOneEvent` calls `ChangeAmendment.applyPosted` inside the `Ev.ops` branch; `DbAgent.processPostEvents` calls `finishAppliedPostChange` when the applied event id advances.

[ ] `DbAgent.validatePostChange` in `DbAgent.fs` lines 296–306 calls `validatePathMoves` only when `liveSaveDataDir` is `Some dataDir`; it returns `Ok ()` for `graphOnly` or when `liveSaveDataDir` is `None`.

[ ] `DbAgent.finishAppliedPostChange` matches `FileAgent` on validation then store; this slice inserts the same relocate step after validation `Ok` and before `commitPostChange` when `graphOnly` is false and a data directory is present (`loaded.dataDir` is always set on `FileAgent`; `liveSaveDataDir` on `DbAgent`).

[ ] Recommended shared orchestration: a new `DocumentPersistChange` entry (for example `relocateAcceptedPathMoves`) that takes `dataDir`, `preGraph`, `postGraph`, coalesces with `coalescePathMoves`, partitions with `sameWorkspaceMoves` and `crossWorkspaceMoves`, runs `executePathMoves` for cross-workspace disk placement, then for each move holds `NodeEnclosureLock.withNodeLock` on a `Special Directory` (or strictly enclosing `Directory` / `Workspace` node per move) or `NodeEnclosureLock.withPersist` on a `Special File`, nests `WorkspaceGit.withWorkTreeGate` when `repositoryRoot` is a named Workspace, then calls `gitMove` or `moveAcrossWorkspaces` with `repositoryRoot` and `gitPath` from the blocker tickets.

[ ] Cross-workspace byte copy: [07 — Move an owned node across workspaces](plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces.md) requires the destination work tree to hold the File or Directory before `moveAcrossWorkspaces`; `moveAcrossWorkspaces` returns `Error` and runs no git command when the destination path is missing. The Post change relocate caller (the new `DocumentPersistChange` helper or inline agent step), not `moveAcrossWorkspaces`, copies bytes with `executePathMoves` (or `executePathMove` via `resolveMovePaths`) on `crossWorkspaceMoves` only, then calls `moveAcrossWorkspaces`.

[ ] Same-workspace moves: one `gitMove` per classified move; `gitMove` runs `git mv` in the work tree via `GitSave.runGit` per [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md). This slice does not call `executePathMoves` for same-workspace classified moves when `gitMove` handles the tree.

[ ] `DocumentPersistChange.persistGraphChangeWith` still calls `executePathMoves` then `writeDocumentsSoft`; this slice does not reroute `persistGraphChange` / `persistGraphOps` production callers through git relocate.

[ ] Lock order while `WorkspaceGit.withWorkTreeGate` still exists: acquire node lock (or `withPersist` for a File move), drain, take `withWorkTreeGate` on the repository root, run git inside `GitSave.runGit` / `GitRun.gitExec`, release the gate, then release the node lock or persist grant.

[ ] A Directory move takes one `withNodeLock` on that Directory `NodeId`. A single File move wraps git in `withPersist` on that File `NodeId`, not `withNodeLock` on the parent Directory.

[ ] `CoreEventDispatch.persistNew` in `src/Server/Core/CoreEventDispatch.fs` line 325 calls `persist` (`context.persist.applyEvent`), then `store`, then `markAcceptedSpecials`; relocate runs inside `applyEvent` before the graph store step in the agent.

[ ] `CoreEventDispatch.pushChangeSpecials` lines 241–272 in `CoreEventDispatch.fs` calls `DocumentOpImpact.owningSpecials` then `List.iter context.persist.pushPersist` after `applyEvent` returns; `CoreMailboxFilesAxis.pushParsedUnpersisted` in `src/Server/Core/CoreMailboxFilesAxis.fs` line 62 also calls `persist.pushPersist` on a separate Unparsed path.

[ ] Persist queue for a posted Change stays outside the agent today: this slice leaves `markAcceptedSpecials` → `pushChangeSpecials` → `pushPersist` in `CoreEventDispatch.fs` unchanged; Post change still reaches the persist stack through that path after store.

[ ] `PersistThread.runRequest` is not a required edit when `NodeEnclosureLock.withPersist` wraps `DocumentPersistWrite.writeDocument` (and private `writeArtifactText`) inside `writeDocument`; blocked persists wait inside `withPersist` before `writeDocument` runs.

[ ] Parse-read sites for a follow-on lock wire in the same ticket if the lock must drain production parse reads (counts toward the source-file budget): `DocumentPersistWrite.planParseFile` / private `preparedParseText` → `DocumentPersistPath.readFileTextAtRelative` in `src/Server/DocumentPersistWrite.fs` lines 67–91 and 120–132; `ParseThread.parseFile` in `src/Server/ParseThread.fs` lines 78–95 calls `DocumentPersistWrite.planParseFile`; `ParseThread.parseDirectory` lines 97–115 calls `DirectoryReconcile.planDirectoryReconcile` with `DocumentPersistPath.discoverImmediateMembers`. Recommended `withParseRead` wrap: inside `planParseFile` for file-body reads, and in `ParseThread.parseDirectory` (or inside `planDirectoryReconcile` caller) for directory reconcile.

[ ] Per-member persist lock wire sites in the same ticket if the lock must block live writes during a move: `DocumentPersistWrite.writeDocument` lines 278–287 and private `writeArtifactText` lines 55–65 in `src/Server/DocumentPersistWrite.fs`; `DocumentPersistChange.persistNode` already funnels through `writeDocument`.

## Existing tests

[ ] ``planPathMovesBetweenGraphs includes file reparent across workspaces`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` builds the `home` to `other` File reparent move.

[ ] ``coalescePathMoves drops nested roots covered by directory move`` in `tests/Shared.Tests/DocumentPathMoveTests.fs` checks coalesce for a directory rename.

[ ] ``persistGraphChange moves renamed file artifact before writing`` and ``persistGraphChange moves renamed directory tree before writing`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` assert `File.Move` / `Directory.Move` through `persistGraphChange`, not Post change git relocate.

[ ] [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md) names ``same-workspace relocate runs git mv in the Workspace repository`` and ``gitMove moves a directory tree with one git mv`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs`; those tests call the relocate helpers directly with no production caller until this slice.

[ ] [07 — Move an owned node across workspaces](plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces.md) names ``cross-workspace move deletes from the source repository and adds to the destination repository`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` as a direct `moveAcrossWorkspaces` caller test.

[ ] [04 — Expand the node enclosure lock](plan/node-move-disk/issues/04-expand-the-node-enclosure-lock.md) names ``directory lock blocks a descendant, allows a sibling, and a workspace lock blocks its file``, ``node enclosure acquire drains an in-flight persist and a parse read``, and ``persist grant stores the owner chain and includes the directory node`` in `tests/Server.Tests/NodeEnclosureLockTests.fs`.

[ ] ``FileAgent applyEvent applies Ev Ops without leftover Change`` and related facts in `tests/Server.Tests/PersistApplyTests.fs` use `(FileAgent.persist (FileAgent.create dataDir)).handlers.applyEvent` and `handlers.graphAtEvent ()` as the public Post change seam.

[ ] ``DbAgent applyEvent applies Ev Ops without leftover Change`` in `PersistApplyTests.fs` uses `DbAgent.createForTest` handlers the same way.

[ ] Arch section 6 has no seam 10 yet; seam 7 and seam 8 name git relocate tests in `DocumentPathMoveExecutionTests.fs` without FileAgent or the node lock.

## Copy-from

[ ] Disk byte placement before cross-workspace git: private `executePathMove` and `executePathMoves` in `src/Server/DocumentPersistChange.fs`.

[ ] Same-workspace git: `WorkspaceGit.gitMove` from [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md).

[ ] Cross-workspace git: `WorkspaceGit.moveAcrossWorkspaces` from [07 — Move an owned node across workspaces](plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces.md); read behavior only from that ticket, not from source.

[ ] Repository and git-relative paths: `DocumentPersistPath.repositoryRoot` and `gitPath` from [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md).

[ ] Node lock acquire and persist grant: `NodeEnclosureLock.withNodeLock`, `withPersist`, and `withParseRead` from [04 — Expand the node enclosure lock](plan/node-move-disk/issues/04-expand-the-node-enclosure-lock.md); copy-from wait shape in ``work tree gate waits then continues without overlap`` in `tests/Server.Tests/WorkspaceGitTests.fs`.

[ ] Work-tree gate nesting: `DocumentPersistPath.withWorkTreeGates` pattern in `src/Server/DocumentPersistPath.fs` lines 51–59 and `persistGraphChangeWith` lines 206–212 in `DocumentPersistChange.fs`.

[ ] Post-change handler test wiring: `PersistApplyTests.fs` `applyThenAppend`, `fileHandlers`, `requireOk "applyEvent" (handlers.applyEvent event false)`, and `DocumentPathMoveExecutionTests.fs` `graphWithNestedDocs`, `newTempDir`, `WorkspaceGit.ensureInit`.

## write-ticket

[ ] Blockers: [04 — Expand the node enclosure lock](plan/node-move-disk/issues/04-expand-the-node-enclosure-lock.md), [05 — Relocate in the same workspace](plan/node-move-disk/issues/05-relocate-in-the-same-workspace.md), [07 — Move an owned node across workspaces](plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces.md); [08 — Amend a change conflict before the graph is stored](plan/node-move-disk/issues/08-amend-a-change-conflict-before-the-graph-is-stored.md) may land amended graphs before this relocate step.

[ ] Flow 6 order in [arch.md](arch.md): after Accept validation passes, classify moves, hold node lock or File persist lock, drain in-flight persists and parse reads, run same-workspace or cross-workspace git, release locks, store graph; persist queue follows through `CoreEventDispatch` as today.

[ ] Proposed arch seam 10 **Post change relocate under lock** / **Move write gate**: two Server tests observe git relocate from `FileAgent` `handlers.applyEvent`, not from direct helper calls alone.

[ ] This slice does not change `PersistThread.runRequest` Unparsed reject behavior; that rule stays a separate ticket per [arch.md](arch.md) **Persist thread write**.

### Source files

[ ] `src/Server/DocumentPersistChange.fs` — add Post-change relocate orchestration (coalesce, classify, cross-workspace `executePathMoves`, per-move lock, `withWorkTreeGate`, `gitMove`, `moveAcrossWorkspaces`).

[ ] `src/Server/Core/FileAgent.fs` — call relocate orchestration in `processPostEvents` after `validatePostChange` `Ok` and before graph store; pass `loaded.dataDir`.

[ ] `src/Server/Core/DbAgent.fs` — same call in `finishAppliedPostChange` when `liveSaveDataDir` is `Some`; skip relocate when `graphOnly` or no live data directory.

[ ] `src/Server/DocumentPersistWrite.fs` — wrap `writeDocument` and private `writeArtifactText` with `NodeEnclosureLock.withPersist` so persist threads wait during a move lock.

[ ] `src/Server/ParseThread.fs` — wrap `planParseFile` invocations and directory reconcile parse with `NodeEnclosureLock.withParseRead` (or wrap inside `planParseFile` in `DocumentPersistWrite.fs` instead of `ParseThread.fs`; pick one file for parse-read wire, not both, to limit diff size).

[ ] Distinct source file count for this slice: 5 (`DocumentPersistChange.fs`, `FileAgent.fs`, `DbAgent.fs`, `DocumentPersistWrite.fs`, and either `ParseThread.fs` or parse-read wiring only in `DocumentPersistWrite.fs`). Under the 8-file limit in [Size limit](plan/context/size-limit.md).

[ ] `src/Server/Core/PersistThread.fs` stays closed when persist wait lives in `withPersist` inside `DocumentPersistWrite.writeDocument`.

[ ] `src/Server/NodeEnclosureLock.fs`, `src/Shared/DocumentPathMove.fs`, `src/Server/WorkspaceGit.fs`, and `src/Server/DocumentPersistPath.fs` are inputs from blocker tickets; this slice calls them and does not redefine `withNodeLock`, `sameWorkspaceMoves`, `crossWorkspaceMoves`, `gitMove`, `moveAcrossWorkspaces`, `repositoryRoot`, or `gitPath`.

[ ] `src/Server/Core/CoreEventDispatch.fs` stays unchanged for persist queue; `pushChangeSpecials` remains the `pushPersist` caller after `applyEvent`.

### Tests

[ ] ``Post change same-workspace relocate runs git mv under the node enclosure lock`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` (proposed seam 10) — seed `home` repo with `WorkspaceGit.ensureInit`, `writeAllDocuments`, post a same-Workspace path change through `(FileAgent.persist (FileAgent.create dataDir)).handlers.applyEvent event false` per `PersistApplyTests.fs`, assert `git ls-files` shows the new git path and the old path is gone.

[ ] ``Post change cross-workspace relocate runs moveAcrossWorkspaces under the node enclosure lock`` in `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` (proposed seam 10) — two Workspace repos, tracked file under `home`, reparent Change to `other`, `applyEvent` on FileAgent handlers, assert destination work tree and git indexes match [07 — Move an owned node across workspaces](plan/node-move-disk/issues/07-move-an-owned-node-across-workspaces.md) expectations.

[ ] Optional third test (within the 4-test budget): ``Post change directory relocate holds withNodeLock on the Directory Node`` — observe a descendant `withPersist` waits during `applyEvent` directory rename (can share lock-test patterns from `NodeEnclosureLockTests.fs`).

[ ] Optional fourth test: ``Post change file relocate holds withPersist on the File Node for git mv`` — sibling file persist or parse may continue while the moved File grant is held.

[ ] Distinct tests added or changed: 2 required (same-workspace and cross-workspace Post change); up to 4 total per [Size limit](plan/context/size-limit.md).

[ ] Register `tests/Server.Tests/DocumentPathMoveExecutionTests.fs` in `Gambol.Server.Tests.fsproj` only if the file is new; the module already exists.

### Split

[ ] No split. Five source files (agents, relocate orchestration, `DocumentPersistWrite` persist lock, parse-read wire in `ParseThread.fs` or `DocumentPersistWrite.fs`) stay within the 8-file limit.

[ ] If a future pass adds more parse-read files (`LazyLoadReconciliationServer.fs`, `DirectoryReconcile.fs` under `src/Shared/dotnet`, or `DocumentPersistChange.readAllDocuments`), split job 2 would wire `withPersist` into `writeDocument` and `writeArtifactText` and `withParseRead` into those parse-read sites so the lock drains them; job 1 is Post change classify and git under `withNodeLock` or `withPersist` then store, including `withWorkTreeGate` around git.
