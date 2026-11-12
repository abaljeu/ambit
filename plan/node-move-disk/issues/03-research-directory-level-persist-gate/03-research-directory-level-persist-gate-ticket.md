# 03 — Research: directory-level persist gate for moves

**Type:** research
**Status:** defined
**Blocked by:** None

## Context

On 2026-11-09 the gate was `WorkspaceGit.withWorkTreeGate` ([src/Server/WorkspaceGit.fs](../../../../src/Server/WorkspaceGit.fs)), one `SemaphoreSlim` per normalized Workspace root. Git pull, git Save, and the persist writes took it. `DocumentPersistPath.withWorkTreeGates` nested several roots. Shared `GitRun` held a separate per-folder lock around each git command.

Those names are gone. `NodeLockPool.withLock` ([src/Server/NodeLockPool.fs](../../../../src/Server/NodeLockPool.fs)) is the node lock. `withLockResult` and `withLockAsync` are internal. `NodeLockRejection` has only `HolderHoldsLock`, so a holder does not take a second lock. Pool entries store no graph. `conflicts` reads the graph passed with the request. An absent node runs the action and queues no entry. `ownerChain` walks `ownerParentByChild` with a visited set.

`writeDocument` and `writeArtifactText` call `withLockResult`. A file-body parse read calls `withLockResult` from `preparedParseText`. A directory reconcile calls `withLockResult` from `ParseThread.reconcileDirectory`. `executePathMoves` folds `executeCoalescedMove`, which takes `withLockResult` on the post graph and `move.nodeId`, then calls `resolveMovePaths` and `executePathMove`. `File.Move`, `Directory.Move`, and `validateDestinationAvailable` are in `executePathMove`. `GithubTransportActor` holds `withLockResult` for a Workspace commit and `withLockAsync` for the pull merge and Unparsed mark. Fetch holds no pool lock. `workspaceRootFor` callers are `GithubTransportActor`, `LoadSaveRouting`, and `Api`.

The 2026-11-09 gate was Workspace-wide only. [core-refinement architecture](../../../core-refinement/arch.md) §6 and [Parse and persist](../../../../doc/current/parse-persist.md) marked that gate obsolescent. Alan, 2026-11-10: one `NodeLockPool.withLock` replaces the two grants this research records. [11 — Expand the node lock pool](../11-expand-the-node-lock-pool/11-expand-the-node-lock-pool-ticket.md) built that pool. [31 — Migrate onto the workspace lock](../../../core-refinement/issues/31-migrate-onto-the-workspace-lock.md) removed `withWorkTreeGate` and the `GitRun` private gate. The interview answers and the recommendation below stay the 2026-11-09 record.

[Node move and disk architecture](../../arch.md) **Move write gate** needs a lock below Workspace scope: while a git move of a Directory runs, no write is allowed under that Directory until the move completes.

## Goal

Come up with a solution that supports both of these:

- A git pull locks a Workspace.
- A Directory move locks a Directory.

In both cases, the lock prevents any of its descendants from being persisted.

## Research

1. Key — How is a gate keyed by Directory path prefix, or by walking the Directory's members? What does each need when a path is itself moving?
2. Planned locks — How does this lock relate to the planned workspace lock and per-member persist locks in core-refinement §6?
3. `withWorkTreeGate` — Should it be replaced or coexist with the new lock?
4. Deadlock — What are the deadlock risks with nested gates, such as a Directory gate inside a Workspace gate, a move that holds both a source and a destination, and the separate `GitRun` lock?

## Outputs

A written answer to each research question, with a recommended solution. No code changes.

## Notes

- 2026-11-09 — Alan asked for this research ticket, on the same branch after the arch. Do not implement anything.

## Findings

Research date 2026-11-09. This section is the research answer. No source change.

### 1. Definition — ancestor aggregate lock

The directory-level persist gate is the core-refinement architecture §6 aggregate lock, keyed by one `NodeId`.

- [x] A persist write is [Document persist write](../../../../src/Server/DocumentPersistWrite.fs) `writeDocument` or `writeArtifactText`. Each calls `NodeLockPool.withLockResult` on that node.
- [x] Git pull holds `NodeLockPool.withLockAsync` on the Workspace Node in `GithubTransportActor.lockMergeAndMark`.
- [x] A Directory path move holds `withLockResult` on that Directory node inside `executeCoalescedMove`. A File path move holds the File node. A Workspace path move holds the Workspace node.
- [x] Holders are the git pull caller, the path-move caller, and the persist caller. Each runs its action and then releases.
- [x] A second lock on the same thread returns `NodeLockRejection.HolderHoldsLock`. The pool stores no owner-chain list on the entry.
- [x] A container request waits while a descendant id is `Held`, or `Pending` with a smaller seq. A Directory or File request waits the same way on an ancestor id.
- [x] The persist thread does not keep a deferred list. `withLockResult` waits, and the node stays Unpersisted until the write finishes. No `Notice.PersistFailed` is posted for that wait.
- [ ] **Persist thread write** still requires `PersistThread.runRequest` to skip `persistNode` when `PersistThread.unparsedNode` is true. That skip is a separate rule. `runRequest` does not call `unparsedNode`. **Move write gate** keeps the moved document root Parsed.

### 2. Design

#### 2.1 Key — NodeId, owner chain

- [x] The lock key is the `NodeId`. The path is a property of the node, and the move changes the path. The caller passes no owner chain.
- [x] `GraphQuery.enclosing` in `src/Shared/GraphQuery.fs` walks `ownerParentByChild`, includes the start node, and a visited set stops a cycle. The pool's private `ownerChain` is that parent walk. It does not call `GraphQuery`.
- [ ] The 2026-11-09 grant stored every ancestor `NodeId` from `enclosing`, plus the document root. The current entry stores `seq`, `phase`, `nodeId`, and `threadId` only.
- [x] `DocumentPathMove` keeps one `nodeId` with `oldPath` and `newPath`. `planPathMovesBetweenGraphs` records a move when both graphs have a path and the paths differ.
- [ ] After the post graph is stored, a descendant's owner chain still contains the moved Directory Node. One id covers the source path and the destination path.
- [x] `PersistThread.runRequest` reads `currentState` once and `persistNode` writes that graph. The pool does not store the aggregate id on the grant. The write waits inside `withLockResult`.
- [x] `writeDocument` calls `withLockResult`, then `writeDocumentCore` resolves the artifact path from that node.
- [x] `persistGraphChangeWith` does not collect Workspace roots. It calls `executePathMoves`, then `writeDocumentsSoft`. `executePathMoves` coalesces, then each move takes `withLockResult` on the post graph.
- [x] `DocumentPathMove.coalescePathMoves` drops a descendant move whose old relative path has a kept Directory prefix (`isStrictPrefix`). The new path is not an input to that drop. The lock follows `move.nodeId`.
- [x] `GraphQuery.ownedArtifactsInDirectory` lists owned artifact children and does not descend into a nested artifact. The pool takes no member list. `conflicts` walks `ownerParentByChild` on the passed graph.
- [x] `validateDestinationAvailable` returns `disk path already exists` when the destination full path exists and differs from the source. `executePathMove` calls it before `File.Move` or `Directory.Move`.

#### 2.2 Scope — Workspace and Directory, one mechanism

- [x] Git pull and a path move use the same `NodeLockPool.withLock`. Pull passes the Workspace Node. A path move passes `move.nodeId`.
- [ ] **Move write gate** is this hold across `gitMove` or `moveAcrossWorkspaces`. Those functions are absent.
- [x] A sibling whose owner chain on the passed graph does not reach the locked node does not conflict, so its `withLock` runs during the hold.
- [x] A holder takes one lock. `HolderHoldsLock` rejects a second request from that thread. There is no separate per-member grant list.
- [x] `GitSave.runGit` calls `GitRun.gitExec`. `gitExec` calls `ProcessExec.runCapture` and takes no per-folder lock. Cross-workspace git is still one planned delete and one planned add. Persist exclusion for a moved Directory is `withLockResult` on that Directory node.

#### 2.3 Work-tree gate, and the lock that replaced it

On 2026-11-09 `WorkspaceGit.withWorkTreeGate` was one `SemaphoreSlim` per workspace root, and `DocumentPersistPath.withWorkTreeGates` nested those roots. That is not the current tree.

- [x] `WorkspaceGit.withWorkTreeGate`, `DocumentPersistPath.withWorkTreeGates`, and private `GitRun.withWorkTreeGate` are absent.
- [x] `WorkspaceGit.pullTracked` and `WorkspaceGit.saveTracked` take no pool lock. `GithubTransportActor` holds the Workspace lock around commit (`withLockResult`) and around the pull merge and Unparsed mark (`withLockAsync`). Fetch runs with no pool lock.
- [x] `writeArtifactText` and `writeDocument` call `withLockResult`. `writeAllDocuments` filters with `shouldWriteDocumentRoot`, then `writeDocuments` calls `writeDocument`. There is no shared semaphore around the batch.
- [x] `writeDocumentsSoft` calls `writeDocument`, which calls `withLockResult` and then `writeDocumentCore`.
- [x] `executePathMove` moves a directory with `Directory.Move` and a file with `File.Move`. `executeCoalescedMove` takes `withLockResult` before that call. **Same-workspace relocate** `gitMove` is still absent.
- [x] `FileAgent.defaultDependencies` and `DbAgent.createWithLiveSave` set `persistNode` to `DocumentPersistChange.persistNode`. `persistNode` calls `writeDocument`.
- [x] `DbAgent.validatePostChange` and `FileAgent.validatePostChange` call `validatePathMoves`. That call checks the destination. It takes no pool lock.
- [x] `workspaceRootFor` returns None when `artifactDirectoryRelative` returns None, including when the enclosing Workspace is `Graph.rootId`. Callers are `GithubTransportActor`, `LoadSaveRouting`, and `Api`.
- [x] `writeDocument` always enters `withLockResult` before `writeDocumentCore`. There is no branch that skips the lock when `workspaceRootFor` is None.
- [x] [31 — Migrate onto the workspace lock](../../../core-refinement/issues/31-migrate-onto-the-workspace-lock.md) removed `withWorkTreeGate`. `NodeLockPool.withLock` is the protocol on Persist and git paths.

#### 2.4 Planned workspace lock and per-member persist locks

- [x] One `NodeLockPool.withLock` is the Workspace lock and the document-root lock. A Workspace pull holds that request, marks arrived nodes Unparsed, and releases.
- [x] A Directory or File request waits while an ancestor id is taken. A Workspace or Directory request waits while a descendant id is taken.
- [x] A parse read takes `withLockResult` on its node. It does not skip the pool.
- [x] A Directory move uses that same request. The work-tree gate is gone, so there is no third lock family beside the pool.

#### 2.5 Persist thread

- [x] `PersistThread.start` runs one background thread.
- [x] `PersistRequestStack` is LIFO (`ConcurrentStack`). `take` blocks until a push.
- [x] `runRequest` reads `CoreEventDispatch.persistedNode`. Persisted skips the write. Unpersisted posts `Notice.NodePersisting` and calls `persistNode`.
- [x] `PersistThread.unparsedNode` is true when `parseState` is `ParseState.Unparsed`. `runRequest` has no call to `unparsedNode`.
- [x] `DocumentPartition.shouldWriteDocumentRoot` is true when `Node.documentStateEnum` is Current and the name is not a directory-file name. `writeDocument` has no call to it. `writeAllDocuments` filters with it. `CoreEventDispatch.pushChangeSpecials` calls `DocumentOpImpact.owningSpecials` and then `pushPersist`.
- [x] There is no deferred persist list. A blocked `withLockResult` waits inside `runRequest`, so later stack requests wait behind it.
- [x] `writeDocument` and `writeArtifactText` call `withLockResult` around the file bytes. `runRequest` posts `Notice.NodePersisted` after `persistNode` returns.
- [x] `CoreMailboxApi.postNotice` queues the notice through `MailboxHost.postItem`.
- [ ] The relocate caller is still open question 5. Git pull waits on the Load actor. The persist thread is the writer.

#### 2.6 Deadlock

- [x] A Directory request waits while an ancestor Workspace id is taken. The waiter holds no pool entry until its own turn.
- [x] A holder does not take a second lock. `HolderHoldsLock` returns before a second entry is queued.
- [x] Source path and destination path are one `NodeId` on `DocumentPathMove`. `withWorkTreeGates` is absent, so there is no nested workspace-semaphore order.
- [x] `GitRun.gitExec` and `GitRun.gitCapture` take no per-folder lock. `GitSave.runGit` calls `gitExec`. **Same-workspace relocate** and **Cross-workspace move** are still planned to call `GitSave.runGit`.
- [x] The 2026-11-09 cycle was persist holding `withWorkTreeGate` while pull held the aggregate. Both gates are gone. A second lock on the holder thread is `HolderHoldsLock` instead of a wait.

### 3. Recommendation

This is the 2026-11-09 recommendation. The current tree uses one `NodeLockPool.withLock` and `HolderHoldsLock`. It does not store an owner chain from `GraphQuery.enclosing`, and it does not keep a `GitRun` process lock.

- [x] Git pull takes `withLockAsync` on the Workspace Node. A path move takes `withLockResult` on `move.nodeId`.
- [ ] The 2026-11-09 text stored owner-chain ids from `GraphQuery.enclosing` on the grant. The current entry does not.
- [ ] The 2026-11-09 text deferred a blocked descendant on the persist thread. The current thread waits inside `withLockResult`.
- [x] The same node id conflicts, so the Directory node's own file (`DirName/.amb`) waits with that Directory id.
- [x] `withWorkTreeGate` is absent from persist and git paths.
- [ ] `GitRun.gitExec` is not a lock.
- [ ] `PersistThread.runRequest` still has no Unparsed skip.

### 4. Questions for Alan

- [x] Directory move and parse reads — §6 item 9 drains parse reads for the workspace lock. Should a Directory aggregate drain parse reads of that Directory the same way?
  - Answer (Alan 2026-11-09): yes. A Directory lock holds off parse reads under that Directory the same way the workspace lock does: it drains in-flight parse reads and holds new ones off until it releases.
- [x] Directory File of the locked node — Should persist of the moved Directory Node wait with its descendants?
  - Answer (Alan 2026-11-09): yes. The moved Directory Node's own file waits with its descendants under the Directory lock, the same way the workspace lock drains in-flight reads and writes.
- [x] File move — **Move write gate** names a Directory or a Workspace. Should a File move hold that File Node's per-member persist lock for the `git mv`?
  - Answer (Alan 2026-11-09): yes. A single File move holds that File Node's per-member persist lock for the `git mv`.
- [x] git Save commit — On 2026-11-09, `commitAndPull` held `withWorkTreeGate` across the Save commit and the pull. The current Save commit holds `NodeLockPool.withLockResult` on the Workspace node, and the pull merge holds `withLockAsync`. Should that Workspace hold include the Save commit?
  - Answer (Alan 2026-11-09): yes. The Workspace aggregate covers the git Save commit, and it also holds while the git Save function is doing a pull.
- [x] Blocked descendant — Is defer-and-stay-Unpersisted the result, or `Notice.PersistFailed`?
  - Answer (Alan 2026-11-09): treat it as a lock. The persist tries to acquire the lock, cannot get it, and waits until it is available. No `Notice.PersistFailed`.
  - Clarification (Alan 2026-11-09): the persist thread itself blocks and waits for the lock; it does not set the node aside on a deferred list. This rests on the lock not being held for as long as ten seconds.
- [x] Cross-workspace siblings — Does one Directory Node aggregate leave sibling persists in the source Workspace and the destination Workspace free to write during `moveAcrossWorkspaces`?
  - Answer (Alan 2026-11-09): yes. There was never a prohibition against writing siblings. Sibling files in the source and destination Workspaces may keep writing during a cross-Workspace move.
- [x] Lock key — Added by Alan 2026-11-09: the Directory lock and the Workspace lock record the node, not the path. Every node under the locked node is locked. The lock does not check whether a path matches.
