# 04 — Expand the node enclosure lock
**Status**: coded
One job: The Server holds a node enclosure lock on one Workspace Node or one Directory Node, and a persist or a parse read under that node waits until the lock releases. Story 5 Directory Node move in [Node move and disk](plan/node-move-disk/spec.md) needs that wait so the Directory ends at its derived path.

Type: coding
Blockers: none

## Scope

### Signatures

[x] `GraphQuery.enclosing` in `src/Shared/GraphQuery.fs` has type `Graph -> (Node -> bool) -> NodeId -> NodeId option`.
[ ] `WorkspaceGit.withWorkTreeGate` in `src/Server/WorkspaceGit.fs` has type `string -> (unit -> Result<'value, string>) -> Result<'value, string>`.
[x] `GitRun.gitExec` in `src/Shared/dotnet/GitRun.fs` has type `string -> string -> Result<string, string>`.
[ ] `GitRun.withWorkTreeGate` in `src/Shared/dotnet/GitRun.fs` is private.
[x] `DocumentPersistWrite.writeDocument` in `src/Server/DocumentPersistWrite.fs` has type `string -> Graph -> NodeId -> Result<DocumentWriteOk, string>`.
[x] `DocumentPersistWrite.writeArtifactText` in `src/Server/DocumentPersistWrite.fs` is private and has type `string -> Graph -> NodeId -> string -> Result<unit, string>`.
[x] `NodeEnclosureLock` is absent from `src/Server`.

### Match sites

[x] `GraphQuery.enclosing` reads `graph.nodes`, then steps with `graph.ownerParentByChild`, and a visited set stops a cycle. The walk includes the start node.
[ ] `WorkspaceGit.withWorkTreeGate` takes one `SemaphoreSlim` from `workTreeGates` per normalized workspace root, calls `Wait`, runs the action, and calls `Release` in `finally`.
[ ] `GitRun.gitExec` calls private `GitRun.withWorkTreeGate`.
[ ] `DocumentPersistWrite.writeDocument` calls `WorkspaceGit.withWorkTreeGate` when `DocumentPersistPath.workspaceRootFor` returns a root.
[ ] `DocumentPersistWrite.writeArtifactText` calls that same gate when `workspaceRootFor` returns a root.
[x] `src/Server/Gambol.Server.fsproj` compiles `WorkspaceGit.fs` and has no `NodeEnclosureLock.fs` include.
[x] No source union case names a node enclosure lock.

### Existing tests

[ ] ``work tree gate waits then continues without overlap`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks two `WorkspaceGit.withWorkTreeGate` calls on one root run one at a time. It uses `Task.Run` and `TaskCompletionSource`.
[ ] ``work tree root aliases acquire one gate without deadlock`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks `DocumentPersistPath.withWorkTreeGates` on aliased roots takes one gate.
[ ] ``Persist write waits on the Workspace work tree gate`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks `DocumentPersistWrite.writeDocument` waits while `WorkspaceGit.withWorkTreeGate` is held.
[ ] ``pullTracked waits for the work tree gate then continues`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks `WorkspaceGit.pullTracked` waits on `WorkspaceGit.withWorkTreeGate`.
[ ] ``saveTracked waits for the work tree gate then continues`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks `WorkspaceGit.saveTracked` waits on `WorkspaceGit.withWorkTreeGate`.
[ ] ``Edit reply returns while the persist thread still holds the write`` in `tests/Server.Tests/FileAgentFailureTests.fs` holds `WorkspaceGit.withWorkTreeGate` inside a fake `persistNode`.
[ ] ``second git waits instead of failing on index.lock`` in `tests/Shared.Tests/GitRunTests.fs` checks two `GitRun.gitExec` calls on one folder run one at a time.
[x] ``enclosing finds first owner-chain match including start`` in `tests/Shared.Tests/GraphQueryTests.fs` checks `GraphQuery.enclosing` for a Directory predicate on a Directory Node, a File Node under it, and a Workspace Node.
[x] `graphWithWorkspace` in `tests/Server.Tests/WorkspaceGitTests.fs` builds a Workspace Node with `Graph.create`, `Node.Create`, `Graph.addDetachedNode`, and `Graph.replace`.

### Copy-from

[ ] `WorkspaceGit.withWorkTreeGate` is the gate the node enclosure lock stands beside. This ticket leaves that function in place.
[ ] ``work tree gate waits then continues without overlap`` is the wait shape for the new tests.

### implement

[ ] `NodeEnclosureLock.withNodeLock`, `withPersist`, and `withParseRead` are absent. [04 — Expand the node enclosure lock](plan/node-move-disk/issues/04-expand-the-node-enclosure-lock/04-expand-the-node-enclosure-lock-ticket.md) adds those three functions.
[ ] `src/Server/Gambol.Server.fsproj` has no compile include for `NodeEnclosureLock.fs`. The ticket places that include immediately after `WorkspaceGit.fs`.
[ ] `tests/Server.Tests/NodeEnclosureLockTests.fs` is absent. The ticket adds that file and a compile include immediately after `WorkspaceGitTests.fs`.
[ ] ``directory lock blocks a descendant, allows a sibling, and a workspace lock blocks its file`` observes `withNodeLock` and `withPersist`: the child File action stays outside while the Directory lock runs, the sibling File action enters, and after release the Workspace lock keeps the sibling File action outside.
[ ] ``node enclosure acquire drains an in-flight persist and a parse read`` observes `withPersist`, `withParseRead`, and `withNodeLock`: the Directory lock action stays outside until the in-flight child persist and the in-flight child parse read return, and a persist started during that drain stays outside its action.
[ ] ``persist grant stores the owner chain and includes the directory node`` observes `GraphQuery.enclosing` on the child File Node and the `NodeId list` passed by `withPersist` for that File Node and for the Directory Node.

## Inputs

### Working

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
[ ] Lock order for this ticket stops at the node enclosure lock. `WorkspaceGit.withWorkTreeGate` and `GitRun.gitExec` stay in their own modules.
[ ] The module returns the action's value. A waiting call stays a wait.

#### Owner chain

[x] `GraphQuery.enclosing` in `src/Shared/GraphQuery.fs` has type `Graph -> (Node -> bool) -> NodeId -> NodeId option`.
[x] `enclosing` returns the first owner-chain node that matches the predicate, and the walk includes the start node.
[ ] The stored list is the start node, then each parent from `graph.ownerParentByChild`, and a visited set stops a cycle. That is the same parent step `enclosing` uses.
[ ] The list includes the document root. A Directory Node id in that list is the Directory Node's own file (`DirName/.amb`).
[ ] The grant stores that walk. `GraphQuery.enclosing` stays the predicate lookup. It returns one id, so the stored list is the walk.
[ ] A sibling whose stored list omits the locked `NodeId` takes `withPersist` during the lock.
[ ] The functions take `Graph` and `NodeId`. They take no path string.

#### Copy-from

[ ] `WorkspaceGit.withWorkTreeGate` in `src/Server/WorkspaceGit.fs` has type `string -> (unit -> Result<'value, string>) -> Result<'value, string>`. It is one `SemaphoreSlim` per workspace root. The node enclosure lock stands beside it.
[ ] `GitRun.workTreeGate` and `GitRun.withWorkTreeGate` in `src/Shared/dotnet/GitRun.fs` are private. `GitRun.gitExec` has type `string -> string -> Result<string, string>` and takes that private gate. It stays the git process lock.
[ ] `DocumentPersistWrite.writeDocument` in `src/Server/DocumentPersistWrite.fs` has type `string -> Graph -> NodeId -> Result<DocumentWriteOk, string>` and takes `WorkspaceGit.withWorkTreeGate` when `workspaceRootFor` returns a root.
[ ] `DocumentPersistWrite.writeArtifactText` in `src/Server/DocumentPersistWrite.fs` is private, has type `string -> Graph -> NodeId -> string -> Result<unit, string>`, and takes the same gate.
[ ] ``work tree gate waits then continues without overlap`` in `tests/Server.Tests/WorkspaceGitTests.fs` is the wait shape for the new tests. It uses `Task.Run` and shows a second caller stays out until the holder releases.

#### Match sites

[x] This ticket adds no union case.
[x] This ticket changes no existing public signature.
[x] No workspace-lock type and no per-member persist-lock type exist in source today.

#### Tests the ticket leaves as they are

[ ] ``work tree gate waits then continues without overlap`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks two `WorkspaceGit.withWorkTreeGate` calls on one root run one at a time.
[ ] ``work tree root aliases acquire one gate without deadlock`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks `DocumentPersistPath.withWorkTreeGates` on aliased roots takes one gate.
[ ] ``Persist write waits on the Workspace work tree gate`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks `DocumentPersistWrite.writeDocument` waits while `WorkspaceGit.withWorkTreeGate` is held.
[ ] ``pullTracked waits for the work tree gate then continues`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks `WorkspaceGit.pullTracked` waits on `WorkspaceGit.withWorkTreeGate`.
[ ] ``saveTracked waits for the work tree gate then continues`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks `WorkspaceGit.saveTracked` waits on `WorkspaceGit.withWorkTreeGate`.
[ ] ``Edit reply returns while the persist thread still holds the write`` in `tests/Server.Tests/FileAgentFailureTests.fs` holds `WorkspaceGit.withWorkTreeGate` inside a fake `persistNode`.
[ ] ``second git waits instead of failing on index.lock`` in `tests/Shared.Tests/GitRunTests.fs` checks two `GitRun.gitExec` calls on one folder run one at a time.
[x] ``enclosing finds first owner-chain match including start`` in `tests/Shared.Tests/GraphQueryTests.fs` checks `GraphQuery.enclosing` for a Directory predicate on a Directory Node, a File Node under it, and a Workspace Node.

### Reference

[x] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Move write gate** (Node enclosure lock, What the lock holds off, Blocked persist, Work-tree gate and git process lock) and §4 flow 6 **Move under the node lock**.
[x] [core-refinement architecture](plan/core-refinement/arch.md) §6 **Workspace lock**. The node enclosure lock is that aggregate lock, covering a Workspace Node and a Directory Node.
[x] [03 — Research: directory-level persist gate for moves](../03-research-directory-level-persist-gate/03-research-directory-level-persist-gate-ticket.md), including Alan's lock-key answer on 2026-11-09.
[x] [30 — Expand the workspace lock](plan/core-refinement/issues/30-expand-the-workspace-lock.md). This ticket is the expand that covers that Workspace lock.
[x] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[ ] `src/Server/WorkspaceGit.fs`, `src/Server/DocumentPersistPath.fs`, and `src/Server/GithubTransportActor.fs` stay on `WorkspaceGit.withWorkTreeGate`.
[ ] `src/Server/DocumentPersistWrite.fs` stays as it is. `writeDocument` and `writeArtifactText` gain the per-member persist lock in a later migrate ticket.
[ ] `src/Shared/dotnet/GitRun.fs` stays the git process lock, taken after the node enclosure lock in a later migrate ticket.
[ ] `src/Server/Core/PersistThread.fs` stays as it is. The persist thread's wait is the later caller of `withPersist`.
[x] `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` stay as they are. Flow 6's relocate caller, `gitMove`, and `moveAcrossWorkspaces` are a later ticket.
[ ] git pull, git Save, and a single File move's `git mv` stay off this lock until a later migrate ticket.
[x] Removing `WorkspaceGit.withWorkTreeGate` is [31 — Migrate onto the workspace lock](plan/core-refinement/issues/31-migrate-onto-the-workspace-lock.md). [33 — Contract the work-tree gate](plan/core-refinement/issues/33-contract-the-work-tree-gate.md) is superseded by that ticket.
[x] `src/Shared/GraphQuery.fs` stays as it is.
[x] `src/Shared/History.fs` stays as it is. `Notice.PersistFailed` stays off this module.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand `src/Server/NodeEnclosureLock.fs` with `withNodeLock`, `withPersist`, and `withParseRead` as Working specifies.
3. [ ] Expand `src/Server/Gambol.Server.fsproj` with `<Compile Include="NodeEnclosureLock.fs" />` immediately after `WorkspaceGit.fs`.
4. [ ] Expand `tests/Server.Tests/NodeEnclosureLockTests.fs` as module `Gambol.Server.Tests.NodeEnclosureLockTests` with the three tests in Outputs. The graph has one Workspace Node, one Directory Node owned by that Workspace, one File Node owned by that Directory, and one sibling File Node owned by that Workspace. Build it with `Graph.create`, `Node.Create`, `Graph.addDetachedNode`, and `Graph.replace`, the same attachment steps as `graphWithWorkspace` in `tests/Server.Tests/WorkspaceGitTests.fs`.
5. [ ] Expand `tests/Server.Tests/Gambol.Server.Tests.fsproj` with `<Compile Include="NodeEnclosureLockTests.fs" />` immediately after `WorkspaceGitTests.fs`.

## Outputs

[ ] `src/Server/NodeEnclosureLock.fs` — `withNodeLock`, `withPersist`, and `withParseRead`.
[ ] `src/Server/Gambol.Server.fsproj` — compile include for `NodeEnclosureLock.fs`.
[ ] Distinct source files: 2. `src/Server/NodeEnclosureLock.fs` and `src/Server/Gambol.Server.fsproj`.
[ ] `tests/Server.Tests/NodeEnclosureLockTests.fs` — the three tests below. The test file is a test output, and it is outside the source-file count.
[ ] `tests/Server.Tests/Gambol.Server.Tests.fsproj` — compile include for `NodeEnclosureLockTests.fs`.
[ ] ``directory lock blocks a descendant, allows a sibling, and a workspace lock blocks its file`` — `withNodeLock` on the Directory Node: `withPersist` on the child File Node stays outside the action, and `withPersist` on the sibling File Node enters. After release, `withNodeLock` on the Workspace Node keeps `withPersist` on the sibling File Node outside the action until release.
[ ] ``node enclosure acquire drains an in-flight persist and a parse read`` — an in-flight `withPersist` and an in-flight `withParseRead` on the child File Node both store the Directory Node id. `withNodeLock` on that Directory Node stays outside its action until both return. A `withPersist` started while that acquire is still draining also stays outside its action.
[ ] ``persist grant stores the owner chain and includes the directory node`` — `GraphQuery.enclosing` on the child File Node with a Directory predicate returns the Directory Node id. `withPersist` on that File Node passes a list that contains the File Node id and the Directory Node id. `withPersist` on the Directory Node id passes a list that contains the Directory Node id.

## Stop when

[ ] Record the stop at `plan/node-move-disk/issues/04-expand-the-node-enclosure-lock/04-build.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit changes `WorkspaceGit.withWorkTreeGate`, `GitRun`, `DocumentPersistWrite`, `GraphQuery.enclosing`, `PersistThread`, `FileAgent`, or `DbAgent`.
[ ] Stop when the edit contradicts story 5 Directory Node move in the spec.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``directory lock blocks a descendant, allows a sibling, and a workspace lock blocks its file``
  - `tests/Server.Tests`: ``node enclosure acquire drains an in-flight persist and a parse read``
  - `tests/Server.Tests`: ``persist grant stores the owner chain and includes the directory node``
[x] All executed tests pass.
[x] Write the verdict to `plan/node-move-disk/issues/04-expand-the-node-enclosure-lock/04-build.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
