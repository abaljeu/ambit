# 03 — Research: directory-level persist gate for moves

**Type:** research
**Status:** defined
**Blocked by:** None

## Context

Today's gate is `WorkspaceGit.withWorkTreeGate` ([src/Server/WorkspaceGit.fs](../../../../src/Server/WorkspaceGit.fs)). It is one `SemaphoreSlim` per normalized Workspace root. The git pull (`WorkspaceGit.pullTracked`, wrapped by `commitInGate` in `GithubTransportActor`), git Save (`WorkspaceGit.saveTracked`), and the persist writes (`DocumentPersistWrite.writeArtifactText`, `writeDocument`, `writeAllDocuments`) all take it. `DocumentPersistPath.withWorkTreeGates` takes several roots in sorted order. The path-move persist in `DocumentPersistChange` gates the Workspace roots of both the pre graph and the post graph before `executePathMoves`. Shared `GitRun` holds its own separate per-folder lock around each git command.

The gate is Workspace-wide only. [core-refinement architecture](../../../core-refinement/arch.md) §6 and [Parse and persist](../../../../doc/current/parse-persist.md) mark it obsolescent. Alan, 2026-11-10: one `NodeLockPool.withLock` replaces the two grants this research records. [11 — Expand the node lock pool](../11-expand-the-node-lock-pool/11-expand-the-node-lock-pool-ticket.md) built that pool. The answers below stay the 2026-11-09 record.

2026-11-10 — Alan: [31 — Migrate onto the workspace lock](../../../core-refinement/issues/31-migrate-onto-the-workspace-lock.md) removes `withWorkTreeGate` and the `GitRun` private gate together. `NodeLockPool.withLock` is the protocol on Persist and git paths. The interview answers and the recommendation below stay the 2026-11-09 record.

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

- [ ] The key is one Workspace Node or one Directory Node.
- [ ] The lock guards a persist whose granted owner chain contains that `NodeId`.
- [ ] A persist write is [Document persist write](../../../../src/Server/DocumentPersistWrite.fs) `writeDocument` or `writeArtifactText`.
- [ ] git pull takes the aggregate on the Workspace Node.
- [ ] A Directory move takes the aggregate on the Directory Node.
- [ ] A Workspace move takes the aggregate on the Workspace Node.
- [ ] The holder is the git pull caller or the relocate caller. The hold lasts for that git operation.
- [ ] A per-member persist lock is the write of one document root. §6 **Per-member persist locks** is that lock.
- [ ] Aggregate acquire waits while a per-member persist lock still stores this `NodeId`.
- [ ] A new per-member persist lock waits while an ancestor aggregate is pending or held.
- [ ] The persist thread defers that new request.
- [ ] The deferred node stays Unpersisted.
- [ ] `Notice.PersistFailed` is question **Blocked descendant** in §4.
- [ ] **Persist thread write** still requires `PersistThread.runRequest` to skip `persistNode` when `PersistThread.unparsedNode` is true. That skip is a separate rule. **Move write gate** keeps the moved document root Parsed.

### 2. Design

#### 2.1 Key — NodeId, owner chain

- [ ] The aggregate key is the `NodeId`. The path is a property of the node, and the move changes the path.
- [ ] `GraphQuery.enclosing` walks the owner chain and includes the start node (`src/Shared/GraphQuery.fs` lines 16-34).
- [ ] Grant of a per-member persist lock stores every ancestor `NodeId` from that walk, plus the document root.
- [ ] Drain waits on stored ids. A later parent edit leaves the in-flight write in the drain set.
- [ ] `DocumentPathMove` keeps one `nodeId` with `oldPath` and `newPath` (`src/Shared/DocumentPathMove.fs` lines 3-7, `planPathMovesBetweenGraphs` lines 59-69).
- [ ] After the post graph is stored, a descendant's owner chain still contains the moved Directory Node. One id covers the source path and the destination path.
- [ ] `PersistThread.runRequest` reads `currentState` once (`src/Server/Core/PersistThread.fs` lines 30-38) and `persistNode` writes that graph. Drain covers that call when the granted lock stored the aggregate id.
- [ ] A request that starts while the aggregate is pending reads the post graph. `enclosing` still finds the moved Directory Node.
- [ ] `writeDocument` resolves the artifact path from that same node (`DocumentPersistWrite.writeDocument` lines 278-287 into `writeDocumentCore`). The writer has a node id.
- [ ] `persistGraphChangeWith` collects Workspace roots from the pre graph and the post graph (`src/Server/DocumentPersistChange.fs` lines 204-211) because today's gate is one semaphore per repository root. The Directory aggregate uses the stable node id, so those two roots are a git-process detail.
- [ ] `DocumentPathMove.coalescePathMoves` drops a descendant move whose old relative path has a Directory prefix (`isStrictPrefix`, `coalescePathMoves`, `src/Shared/DocumentPathMove.fs` lines 56-92). The move operation is already one Directory prefix. The lock follows the node, which is what that prefix names.
- [ ] `GraphQuery.ownedArtifactsInDirectory` lists owned artifact children and stops at a nested Directory or Workspace (`src/Shared/GraphQuery.fs` lines 58-95). A lock that walks members misses files inside a child Directory, and the walk is O(subtree). The aggregate takes no member list.
- [ ] `validateDestinationAvailable` requires the destination full path to be absent (`DocumentPersistChange.fs` lines 95-104). The destination tree has no second node whose persist would write that prefix.

#### 2.2 Scope — Workspace and Directory, one mechanism

- [ ] git pull and a Directory move are the same aggregate. Pull passes the Workspace Node. A Directory move passes the Directory Node. A Workspace move passes the Workspace Node.
- [ ] **Move write gate** is this hold across `gitMove` or `moveAcrossWorkspaces`.
- [ ] A sibling whose stored ancestor list omits the aggregate id takes its per-member persist lock.
- [ ] §6 item 6: a holder takes one lock, and an aggregate lock exists. The move holds the aggregate. It holds no per-member persist lock.
- [ ] Cross-workspace git runs in two repositories, one call at a time. `GitSave.runGit` is `GitRun.gitExec` (`src/Server/GitSave.fs` lines 13-14). Persist exclusion for the moved tree stays the one Directory Node.

#### 2.3 Work-tree gate today, and the aggregate that replaces it

- [ ] `WorkspaceGit.withWorkTreeGate` is one `SemaphoreSlim(1, 1)` per `normalizeWorkTreeRoot` (`src/Server/WorkspaceGit.fs` lines 19-55). `normalizeWorkTreeRoots` distincts and sorts (lines 36-41).
- [ ] `WorkspaceGit.pullTracked` takes it around `pullTrackedBranch` (lines 262-280).
- [ ] `WorkspaceGit.saveTracked` takes it around `commitTracked` (lines 313-319). `pushTrackedBranch` runs after release (lines 320-321).
- [ ] `DocumentPersistWrite.writeArtifactText` takes it (lines 55-65). `writeDocument` takes it (lines 278-287).
- [ ] `DocumentPersistPath.withWorkTreeGates` nests those semaphores in sorted order (`src/Server/DocumentPersistPath.fs` lines 51-59).
- [ ] `persistGraphChangeWith` takes that list, then `executePathMoves`, then `writeDocumentsSoft` (`DocumentPersistChange.fs` lines 212-219).
- [ ] `writeAllDocuments` takes that list, then `writeDocuments` (`DocumentPersistWrite.fs` lines 360-371).
- [ ] `writeDocumentsSoft` calls `writeDocumentCore` (lines 325-337). The batch path takes the semaphore once.
- [ ] `GithubTransportActor.commitInGate` holds the semaphore across commit and the next step (`src/Server/GithubTransportActor.fs` lines 77-80).
- [ ] Load `pullTracked` uses `commitInGate` around commit and `pullAndReadHeads` (lines 82-88). `workspaceGit.pullTracked` is `WorkspaceGit.pullTrackedBranch` (lines 399-408).
- [ ] Save `commitAndPull` uses `commitInGate` around commit and pull (lines 307-314). `pushAfterPull` runs after release (lines 316-323).
- [ ] A second `withWorkTreeGate` on the same root in the same thread stops in `SemaphoreSlim.Wait`. The actor calls `pullTrackedBranch`, which leaves the semaphore to `commitInGate`.
- [ ] `executePathMove` moves a directory with `Directory.Move` and a file with `File.Move` (`DocumentPersistChange.fs` lines 137-162). **Same-workspace relocate** `gitMove` is the planned git call. **Disk relocate** records `File.Move` and `Directory.Move` as the relocation in code now.
- [ ] Production live save calls `DocumentPersistChange.persistNode` (`FileAgent.defaultDependencies`, `src/Server/Core/FileAgent.fs` line 35; `DbAgent.createWithLiveSave`, `src/Server/Core/DbAgent.fs` line 655). `persistNode` calls `writeDocument` (`DocumentPersistChange.fs` lines 261-269).
- [ ] `DbAgent.validatePostChange` and `FileAgent.validatePostChange` call `validatePathMoves` (`DbAgent.fs` lines 296-305, `FileAgent.fs` lines 93-105). That call checks the destination. It takes no gate.
- [ ] `workspaceRootFor` returns None when `artifactDirectoryRelative` returns None (`DocumentPersistPath.fs` lines 41-49).
- [ ] `artifactDirectoryRelative` returns None for `Graph.rootId` (`src/Shared/DocumentPartition.fs` lines 210-212).
- [ ] That None branch calls `writeDocumentCore` (`DocumentPersistWrite.fs` lines 283-284).
- [ ] Parse and persist **Lock** marks `withWorkTreeGate` obsolescent and names the workspace lock plus per-member persist locks as the protocol.
- [ ] [01 — Persist/git work-tree gate](../../../core-refinement/issues/01-persist-git-work-tree-gate.md) revokes the exclusive gate as the lasting protocol. §3 step 4 **§6 locks catch-up** stands the §6 locks beside it, then removes it from persist and git paths.
- [ ] The ancestor aggregate is that §6 lock, with the Directory Node added as an aggregate root. It replaces `WorkspaceGit.withWorkTreeGate` on persist writes and on git pull.
- [ ] Expand order while both exist: mark the aggregate pending; drain per-member persist locks; take `withWorkTreeGate`; run git; release the semaphore; release the aggregate.
- [ ] A persist tries the per-member persist lock first. It takes `withWorkTreeGate` only after that grant. It releases the semaphore before it releases the per-member persist lock.
- [ ] Contract deletes `withWorkTreeGate` from those paths. `GitRun` stays.

#### 2.4 Planned workspace lock and per-member persist locks

- [ ] §6 **Workspace lock** is this aggregate on a Workspace Node. Pending, drain, pull, mark arrived files Unparsed, and release stay §6 items 7-10.
- [ ] §6 **Per-member persist locks** are the document-root locks. A pending or held ancestor aggregate blocks a new grant.
- [ ] §6 item 9: parse reads the file and takes no persist lock. The pending workspace lock drains those reads.
- [ ] A Directory aggregate is the same pending-and-drain protocol. §6 states the Workspace case because git pull is Workspace-wide.
- [ ] The design adds no third lock family beside §6 and the obsolescent work-tree gate.

#### 2.5 Persist thread

- [ ] `PersistThread.start` runs one thread (`PersistThread.fs` lines 65-69).
- [ ] `PersistRequestStack` is LIFO (`src/Server/Core/PersistRequestStack.fs` lines 11-12 and 30-35). `take` blocks until a push (lines 52-54).
- [ ] `runRequest` reads `CoreEventDispatch.persistedNode` (`PersistThread.fs` lines 30-34, `src/Server/Core/CoreEventDispatch.fs` lines 191-196). Persisted skips the write. Unpersisted posts `Notice.NodePersisting` and calls `persistNode`.
- [ ] `PersistThread.unparsedNode` is true when `parseState` is `ParseState.Unparsed` (`PersistThread.fs` lines 23-28). `runRequest` has no call to `unparsedNode`.
- [ ] **Unparsed save gate**: `DocumentPartition.shouldWriteDocumentRoot` is true when `Node.documentStateEnum` is Current (`DocumentPartition.fs` lines 16-18). `writeDocument` has no call to it. `CoreEventDispatch.pushChangeSpecials` pushes each owning special (`CoreEventDispatch.fs` lines 241-272).
- [ ] The one persist thread must stay runnable for siblings. A block inside `runRequest` stops every later request. The thread pushes a blocked descendant to a deferred list and takes the next stack request. Aggregate release pushes those deferred ids.
- [ ] Immediate re-push onto the LIFO stack retries the same descendant ahead of every sibling.
- [ ] The per-member persist lock is granted inside `writeDocument` and `writeArtifactText`, around the file bytes, and released before `Notice.NodePersisted`.
- [ ] `CoreMailboxApi.postNotice` queues the notice (`src/Server/Core/CoreMailboxApi.fs` lines 19-20) through `MailboxHost.postItem` (`src/Server/Core/MailboxHost.fs` lines 38-39).
- [ ] The aggregate wait runs on the git Load actor or on the relocate caller. **Open question 5 — Relocate caller** still names that caller. The caller is a thread that can wait for the persist thread. The persist thread is the writer, and the mailbox thread is the notice applier.

#### 2.6 Deadlock

- [ ] Directory under Workspace: pull marks the Workspace aggregate pending and drains locks that stored that id. A Directory acquire sees the pending ancestor and waits. The waiter holds no lock.
- [ ] Workspace under an in-flight Directory move: pull drains the Directory aggregate. The Directory holder takes no Workspace aggregate. §6 item 6 is the rule that blocks taking the second lock.
- [ ] Two acquires that both wait, with neither grant stored, leave the descendant absent from the drain set. The ancestor drain finishes. The descendant acquire runs after release.
- [ ] Source path and destination path are one `NodeId`. A design that instead takes two path semaphores uses `normalizeWorkTreeRoots` order: normalize, distinct, sort, then nest, as `withWorkTreeGates` does.
- [ ] `GitRun.gitExec` takes a private per-folder lock for the git process (`src/Shared/dotnet/GitRun.fs` lines 24-47). The comparer is `OrdinalIgnoreCase` (lines 24-26). `WorkspaceGit` uses `Ordinal` off Windows (lines 19-23). The aggregate key is a `NodeId`, so those two path comparers stay inside the git-process lock.
- [ ] `GitRun.gitCapture` takes that process lock on no path (`GitRun.fs` lines 50-57). `GitSave.runGit` uses `gitExec`. **Same-workspace relocate** and **Cross-workspace move** call `GitSave.runGit`.
- [ ] Order is aggregate, then `GitSave.runGit`. A thread that holds the `GitRun` lock takes no aggregate.
- [ ] Cross-repo git calls stay sequential. `withWorkTreeGates` can hold two Workspace semaphores. `GitRun` holds one work directory per `gitExec`.
- [ ] Dual-run cycle to avoid: the persist holds `withWorkTreeGate` and waits on the aggregate, while pull holds the aggregate and waits on that persist. The order in §2.3 **Expand order** breaks that cycle. Drain finishes before pull waits on the semaphore. The persist waits on the aggregate before it waits on the semaphore.

### 3. Recommendation

- [ ] Implement the §6 aggregate keyed by `NodeId`. git pull takes the Workspace Node. A Directory move takes the Directory Node. A Workspace move takes the Workspace Node.
- [ ] Grant per-member persist locks in `writeDocument` and `writeArtifactText`. Store the owner-chain ids from `GraphQuery.enclosing`.
- [ ] On the one persist thread, defer a descendant whose ancestor aggregate is pending or held. Release of the aggregate pushes the deferred ids.
- [ ] Treat the locked node's own Directory File as inside the guarded set. That file is `DirName/.amb` inside the moved Directory.
- [ ] Follow §3 step 4 **§6 locks catch-up** for `WorkspaceGit.withWorkTreeGate`: stand the aggregate beside it with the Expand order above, then remove it from persist and git-pull paths.
- [ ] Keep `GitRun.gitExec` as the git-process lock, taken after the aggregate.
- [ ] Keep the Unparsed skip in `PersistThread.runRequest` as **Persist thread write**. The move aggregate is what blocks persist for a move, because the moved document root stays Parsed.

### 4. Questions for Alan

- [x] Directory move and parse reads — §6 item 9 drains parse reads for the workspace lock. Should a Directory aggregate drain parse reads of that Directory the same way?
  - Answer (Alan 2026-11-09): yes. A Directory lock holds off parse reads under that Directory the same way the workspace lock does: it drains in-flight parse reads and holds new ones off until it releases.
- [x] Directory File of the locked node — Should persist of the moved Directory Node wait with its descendants?
  - Answer (Alan 2026-11-09): yes. The moved Directory Node's own file waits with its descendants under the Directory lock, the same way the workspace lock drains in-flight reads and writes.
- [x] File move — **Move write gate** names a Directory or a Workspace. Should a File move hold that File Node's per-member persist lock for the `git mv`?
  - Answer (Alan 2026-11-09): yes. A single File move holds that File Node's per-member persist lock for the `git mv`.
- [x] git Save commit — `commitAndPull` holds `withWorkTreeGate` across the Save commit and the pull. §6 defines the workspace lock for the pull. Should the Workspace aggregate include that Save commit?
  - Answer (Alan 2026-11-09): yes. The Workspace aggregate covers the git Save commit, and it also holds while the git Save function is doing a pull.
- [x] Blocked descendant — Is defer-and-stay-Unpersisted the result, or `Notice.PersistFailed`?
  - Answer (Alan 2026-11-09): treat it as a lock. The persist tries to acquire the lock, cannot get it, and waits until it is available. No `Notice.PersistFailed`.
  - Clarification (Alan 2026-11-09): the persist thread itself blocks and waits for the lock; it does not set the node aside on a deferred list. This rests on the lock not being held for as long as ten seconds.
- [x] Cross-workspace siblings — Does one Directory Node aggregate leave sibling persists in the source Workspace and the destination Workspace free to write during `moveAcrossWorkspaces`?
  - Answer (Alan 2026-11-09): yes. There was never a prohibition against writing siblings. Sibling files in the source and destination Workspaces may keep writing during a cross-Workspace move.
- [x] Lock key — Added by Alan 2026-11-09: the Directory lock and the Workspace lock record the node, not the path. Every node under the locked node is locked. The lock does not check whether a path matches.
