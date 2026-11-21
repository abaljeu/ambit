# 04 — Expand the node enclosure lock
**Status**: coded
One job: The Server holds a node enclosure lock on one Workspace Node or one Directory Node, and a persist or a parse read under that node waits until the lock releases. Story 5 Directory Node move in [Node move and disk](plan/node-move-disk/spec.md) needs that wait so the Directory ends at its derived path.

Type: coding
Blockers: none

The current tree has no `NodeEnclosureLock`. [11 — Expand the node lock pool](../11-expand-the-node-lock-pool/11-expand-the-node-lock-pool-ticket.md) replaced it with `NodeLockPool.withLock`. Before this ticket, `NodeEnclosureLock` was absent and this ticket added `withNodeLock`, `withPersist`, and `withParseRead`. Those three names are absent again.

## Scope

### Signatures

[x] `GraphQuery.enclosing` in `src/Shared/GraphQuery.fs` has type `Graph -> (Node -> bool) -> NodeId -> NodeId option`.
[x] `WorkspaceGit.withWorkTreeGate` is absent from `src/Server/WorkspaceGit.fs`. Before this ticket it was `string -> (unit -> Result<'value, string>) -> Result<'value, string>`. This ticket left that function in place. [31 — Migrate onto the workspace lock](plan/core-refinement/issues/31-migrate-onto-the-workspace-lock.md) removed it.
[x] `GitRun.gitExec` in `src/Shared/dotnet/GitRun.fs` has type `string -> string -> Result<string, string>`.
[x] `GitRun.withWorkTreeGate` is absent. Before this ticket it was private, and `gitExec` called it. `gitExec` now calls `ProcessExec.runCapture`.
[x] `DocumentPersistWrite.writeDocument` in `src/Server/DocumentPersistWrite.fs` has type `string -> Graph -> NodeId -> Result<DocumentWriteOk, string>`.
[x] `DocumentPersistWrite.writeArtifactText` in `src/Server/DocumentPersistWrite.fs` is private and has type `string -> Graph -> NodeId -> string -> Result<unit, string>`.
[x] `NodeEnclosureLock` is absent from `src/Server`. Before this ticket it was absent. This ticket added it. 11 removed it.

### Match sites

[x] `GraphQuery.enclosing` reads `graph.nodes`, then steps with `graph.ownerParentByChild`, and a visited set stops a cycle. The walk includes the start node.
[x] `WorkspaceGit.withWorkTreeGate` is absent. Before this ticket it took one `SemaphoreSlim` per normalized workspace root.
[x] `GitRun.gitExec` does not call `withWorkTreeGate`.
[x] `DocumentPersistWrite.writeDocument` calls `NodeLockPool.withLockResult` around `writeDocumentCore`. Before this ticket it called `WorkspaceGit.withWorkTreeGate` when `workspaceRootFor` returned a root. This ticket did not change that call.
[x] `DocumentPersistWrite.writeArtifactText` calls `NodeLockPool.withLockResult`. Before this ticket it called that same work-tree gate.
[x] `src/Server/Gambol.Server.fsproj` compiles `WorkspaceGit.fs`, then `NodeLockPool.fs`. It has no `NodeEnclosureLock.fs` include. Before this ticket the slot after `WorkspaceGit.fs` had no enclosure-lock file. This ticket placed `NodeEnclosureLock.fs` there. 11 placed `NodeLockPool.fs` there.
[x] No source union case names a node enclosure lock.

### Existing tests

[x] ``work tree gate waits then continues without overlap`` is absent from `tests/Server.Tests/WorkspaceGitTests.fs`. Before this ticket it checked two `WorkspaceGit.withWorkTreeGate` calls on one root.
[x] ``work tree root aliases acquire one gate without deadlock`` is absent. Before this ticket it checked `DocumentPersistPath.withWorkTreeGates`. That function is absent.
[x] ``Persist write waits on the Workspace work tree gate`` is absent.
[x] ``pullTracked waits for the work tree gate then continues`` is absent.
[x] ``saveTracked waits for the work tree gate then continues`` is absent.
[x] ``Edit reply returns while the persist thread still holds the write`` in `tests/Server.Tests/FileAgentFailureTests.fs` holds a fake `persistNode` on `TaskCompletionSource`. It does not call `WorkspaceGit.withWorkTreeGate`.
[x] ``second git waits instead of failing on index.lock`` is absent from `tests/Shared.Tests/GitRunTests.fs`. `GitRun.gitExec` takes no per-folder gate.
[x] ``enclosing finds first owner-chain match including start`` in `tests/Shared.Tests/GraphQueryTests.fs` checks `GraphQuery.enclosing` for a Directory predicate on a Directory Node, a File Node under it, and a Workspace Node.
[x] `graphWithWorkspace` in `tests/Server.Tests/WorkspaceGitTests.fs` builds a Workspace Node with `Graph.create`, `Node.Create`, `Graph.addDetachedNode`, and `Graph.replace`.

### Copy-from

[x] Before this ticket, `WorkspaceGit.withWorkTreeGate` was the gate this lock stood beside, and this ticket left that function in place. The function is absent. `NodeLockPool.withLock` is the node lock.
[x] ``work tree gate waits then continues without overlap`` is absent. The current wait tests are in `tests/Server.Tests/NodeLockPoolTests.fs`.

### implement

[x] Before this ticket, `NodeEnclosureLock.withNodeLock`, `withPersist`, and `withParseRead` were absent. This ticket added those three functions. They are absent. `NodeLockPool.withLock` is the public function. `withLockResult` and `withLockAsync` are internal.
[x] Before this ticket, `src/Server/Gambol.Server.fsproj` had no `NodeEnclosureLock.fs` include. This ticket placed that include immediately after `WorkspaceGit.fs`. The current include in that slot is `NodeLockPool.fs`.
[x] Before this ticket, `tests/Server.Tests/NodeEnclosureLockTests.fs` was absent. This ticket added it immediately after `WorkspaceGitTests.fs`. The current file in that slot is `NodeLockPoolTests.fs`.
[x] Before this ticket, the three enclosure-lock tests were absent. This ticket added them. Those names are absent. `NodeLockPoolTests` covers descendant wait, ancestor wait, and `HolderHoldsLock`.

## Inputs

### Working

This ticket added `NodeEnclosureLock`. The current tree has `NodeLockPool` instead, so the names below are absent.

#### Node enclosure lock

[ ] New module `NodeEnclosureLock` in `src/Server/NodeEnclosureLock.fs`, namespace `Gambol.Server`.
[ ] `NodeEnclosureLock.withNodeLock` has type `NodeId -> (unit -> 'a) -> 'a`.
[ ] `withNodeLock` marks that `NodeId` pending, waits until every in-flight persist and every in-flight parse read that stored that `NodeId` has finished, runs the action, and then releases that `NodeId`.
[ ] `NodeEnclosureLock.withPersist` has type `Graph -> NodeId -> (NodeId list -> 'a) -> 'a`.
[ ] `withPersist` stores the document-root `NodeId` and each owner ancestor `NodeId`, waits while any stored `NodeId` has a pending or held node enclosure lock, runs the action with that stored list, and then drops the in-flight persist.
[ ] `NodeEnclosureLock.withParseRead` has type `Graph -> NodeId -> (unit -> 'a) -> 'a`.
[ ] `withParseRead` stores the same owner chain, waits while any stored `NodeId` has a pending or held node enclosure lock, runs the action, and then drops the in-flight parse read.
[ ] In-flight means the action has started and has not returned. A call that is still waiting on a node enclosure lock is not in-flight.
[ ] The lock table is process-wide and the key is `NodeId`.
[ ] Each function releases in `finally`, including when the action raises.
[x] This ticket's lock order stopped at the enclosure lock and left `withWorkTreeGate` and `GitRun.gitExec` in their modules. `withWorkTreeGate` is absent. `gitExec` remains in `GitRun`.
[ ] The module returns the action's value. A waiting call stays a wait.

#### Owner chain

[x] `GraphQuery.enclosing` in `src/Shared/GraphQuery.fs` has type `Graph -> (Node -> bool) -> NodeId -> NodeId option`.
[x] `enclosing` returns the first owner-chain node that matches the predicate, and the walk includes the start node.
[x] This ticket stored the start node, then each parent from `graph.ownerParentByChild`, and a visited set stopped a cycle. The current pool keeps that walk private in `ownerChain` and does not store the list on the entry.
[x] This ticket's list included the document root. The current entry stores no owner-chain list. The same node id still conflicts, and that id is the Directory node's own file (`DirName/.amb`).
[x] This ticket's grant stored that walk. The current pool does not.
[x] This ticket let a sibling whose stored list omitted the locked `NodeId` take `withPersist`. The current pool lets a non-conflicting `withLock` run. `withPersist` is absent.
[x] This ticket's functions took `Graph` and `NodeId` and no path string. `NodeLockPool.withLock` takes `Graph`, `NodeId`, and the action.

#### Copy-from

[x] `WorkspaceGit.withWorkTreeGate` is absent. Before this ticket it was one `SemaphoreSlim` per workspace root, and this ticket stood the enclosure lock beside it.
[x] `GitRun.workTreeGate` and `GitRun.withWorkTreeGate` are absent. `GitRun.gitExec` has type `string -> string -> Result<string, string>` and calls `ProcessExec.runCapture`.
[x] `DocumentPersistWrite.writeDocument` calls `NodeLockPool.withLockResult`. Before this ticket it took `WorkspaceGit.withWorkTreeGate` when `workspaceRootFor` returned a root.
[x] `DocumentPersistWrite.writeArtifactText` calls `NodeLockPool.withLockResult`.
[x] ``work tree gate waits then continues without overlap`` is absent. `NodeLockPoolTests` uses `Task.Run` and `TaskCompletionSource` for the wait.

#### Match sites

[x] This ticket adds no union case.
[x] This ticket changes no existing public signature.
[x] No separate workspace-lock type and no per-member persist-lock type exist. `NodeLockPool.withLock` is the one request.

#### Tests the ticket leaves as they are

[x] The five work-tree-gate test names are absent from `tests/Server.Tests/WorkspaceGitTests.fs`.
[x] ``Edit reply returns while the persist thread still holds the write`` holds a fake `persistNode`. It does not call `withWorkTreeGate`.
[x] ``second git waits instead of failing on index.lock`` is absent.
[x] ``enclosing finds first owner-chain match including start`` in `tests/Shared.Tests/GraphQueryTests.fs` checks `GraphQuery.enclosing` for a Directory predicate on a Directory Node, a File Node under it, and a Workspace Node.

### Reference

[x] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Move write gate** (Node enclosure lock, What the lock holds off, Blocked persist, Work-tree gate and git process lock) and §4 flow 6 **Move under the node lock**.
[x] [core-refinement architecture](plan/core-refinement/arch.md) §6 **Workspace lock**. The node enclosure lock is that aggregate lock, covering a Workspace Node and a Directory Node.
[x] [03 — Research: directory-level persist gate for moves](../03-research-directory-level-persist-gate/03-research-directory-level-persist-gate-ticket.md), including Alan's lock-key answer on 2026-11-09.
[x] [30 — Expand the workspace lock](plan/core-refinement/issues/30-expand-the-workspace-lock.md). This ticket is the expand that covers that Workspace lock.
[x] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[x] This ticket left `WorkspaceGit.fs`, `DocumentPersistPath.fs`, and `GithubTransportActor.fs` on `withWorkTreeGate`. That function is absent. `GithubTransportActor` now calls `NodeLockPool.withLockResult` and `withLockAsync`.
[x] This ticket left `DocumentPersistWrite.fs` unchanged. `writeDocument` and `writeArtifactText` now call `withLockResult`.
[x] `GitRun` has no private gate. `gitExec` calls `ProcessExec.runCapture`.
[x] `PersistThread` does not call the pool. The persist wait is inside `writeDocument`'s `withLockResult`. `withPersist` is absent.
[x] `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` stay as they are. The relocate caller is [09 — Relocate under the node lock](../09-relocate-under-the-node-enclosure-lock/09-relocate-under-the-node-enclosure-lock-ticket.md). `WorkspaceGit.gitMove` is in `src/Server/WorkspaceGit.fs`. `moveAcrossWorkspaces` stays absent.
[x] Git pull and git Save hold `NodeLockPool` on the Workspace node. `WorkspaceGit.gitMove` runs one `git mv`. This ticket left pull and Save off `NodeEnclosureLock`.
[x] Removing `WorkspaceGit.withWorkTreeGate` is [31 — Migrate onto the workspace lock](plan/core-refinement/issues/31-migrate-onto-the-workspace-lock.md). [33 — Contract the work-tree gate](plan/core-refinement/issues/33-contract-the-work-tree-gate.md) is superseded by that ticket.
[x] `src/Shared/GraphQuery.fs` stays as it is.
[x] `src/Shared/History.fs` stays as it is. `Notice.PersistFailed` stays off this module.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] This ticket expanded `src/Server/NodeEnclosureLock.fs` with `withNodeLock`, `withPersist`, and `withParseRead`. That file is absent. `src/Server/NodeLockPool.fs` has `withLock`.
3. [x] `src/Server/Gambol.Server.fsproj` compiles `NodeLockPool.fs` immediately after `WorkspaceGit.fs`. This ticket had placed `NodeEnclosureLock.fs` in that slot.
4. [ ] This ticket added `tests/Server.Tests/NodeEnclosureLockTests.fs`. That file is absent. `NodeLockPoolTests.fs` is the current module.
5. [x] `tests/Server.Tests/Gambol.Server.Tests.fsproj` compiles `NodeLockPoolTests.fs` immediately after `WorkspaceGitTests.fs`.

## Outputs

[ ] This ticket's output `src/Server/NodeEnclosureLock.fs` is absent. `src/Server/NodeLockPool.fs` has `withLock`.
[x] `src/Server/Gambol.Server.fsproj` compiles `NodeLockPool.fs`. It has no `NodeEnclosureLock.fs` include.
[ ] This ticket's two source files were `NodeEnclosureLock.fs` and `Gambol.Server.fsproj`. `NodeEnclosureLock.fs` is absent.
[ ] This ticket's `tests/Server.Tests/NodeEnclosureLockTests.fs` is absent. `NodeLockPoolTests.fs` is the test file.
[x] `tests/Server.Tests/Gambol.Server.Tests.fsproj` compiles `NodeLockPoolTests.fs`.
[x] The three enclosure-lock test names are absent. This ticket's ``directory lock blocks a descendant, allows a sibling, and a workspace lock blocks its file`` observed `withNodeLock` and `withPersist`. ``node enclosure acquire drains an in-flight persist and a parse read`` observed `withPersist`, `withParseRead`, and `withNodeLock`. ``persist grant stores the owner chain and includes the directory node`` observed `withPersist` passing the File id and the Directory id. `NodeLockPoolTests` is the current suite.

## Stop when

[ ] Record the stop at `plan/node-move-disk/issues/04-expand-the-node-enclosure-lock/04-build.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit changes `WorkspaceGit.withWorkTreeGate`, `GitRun`, `DocumentPersistWrite`, `GraphQuery.enclosing`, `PersistThread`, `FileAgent`, or `DbAgent`.
[ ] Stop when the edit contradicts story 5 Directory Node move in the spec.

## Check

[x] [04-build.md](04-build.md) records the run that passed the three enclosure-lock tests. Those three test names are absent from the current tree.
[ ] The three named enclosure-lock tests are not in `tests/Server.Tests`.
[x] Write the verdict to `plan/node-move-disk/issues/04-expand-the-node-enclosure-lock/04-build.md`. The check line is `pass`.
[x] A reader who reads only the One job line can say what the ticket builds.
