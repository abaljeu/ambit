# 04 — Expand the node enclosure lock scope

## Signatures

[ ] `GraphQuery.enclosing` in `src/Shared/GraphQuery.fs` has type `Graph -> (Node -> bool) -> NodeId -> NodeId option`.
[ ] `WorkspaceGit.withWorkTreeGate` in `src/Server/WorkspaceGit.fs` has type `string -> (unit -> Result<'value, string>) -> Result<'value, string>`.
[ ] `GitRun.gitExec` in `src/Shared/dotnet/GitRun.fs` has type `string -> string -> Result<string, string>`.
[ ] `GitRun.withWorkTreeGate` in `src/Shared/dotnet/GitRun.fs` is private.
[ ] `DocumentPersistWrite.writeDocument` in `src/Server/DocumentPersistWrite.fs` has type `string -> Graph -> NodeId -> Result<DocumentWriteOk, string>`.
[ ] `DocumentPersistWrite.writeArtifactText` in `src/Server/DocumentPersistWrite.fs` is private and has type `string -> Graph -> NodeId -> string -> Result<unit, string>`.
[ ] `NodeEnclosureLock` is absent from `src/Server`.

## Match sites

[ ] `GraphQuery.enclosing` reads `graph.nodes`, then steps with `graph.ownerParentByChild`, and a visited set stops a cycle. The walk includes the start node.
[ ] `WorkspaceGit.withWorkTreeGate` takes one `SemaphoreSlim` from `workTreeGates` per normalized workspace root, calls `Wait`, runs the action, and calls `Release` in `finally`.
[ ] `GitRun.gitExec` calls private `GitRun.withWorkTreeGate`.
[ ] `DocumentPersistWrite.writeDocument` calls `WorkspaceGit.withWorkTreeGate` when `DocumentPersistPath.workspaceRootFor` returns a root.
[ ] `DocumentPersistWrite.writeArtifactText` calls that same gate when `workspaceRootFor` returns a root.
[ ] `src/Server/Gambol.Server.fsproj` compiles `WorkspaceGit.fs` and has no `NodeEnclosureLock.fs` include.
[ ] No source union case names a node enclosure lock.

## Existing tests

[ ] ``work tree gate waits then continues without overlap`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks two `WorkspaceGit.withWorkTreeGate` calls on one root run one at a time. It uses `Task.Run` and `TaskCompletionSource`.
[ ] ``work tree root aliases acquire one gate without deadlock`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks `DocumentPersistPath.withWorkTreeGates` on aliased roots takes one gate.
[ ] ``Persist write waits on the Workspace work tree gate`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks `DocumentPersistWrite.writeDocument` waits while `WorkspaceGit.withWorkTreeGate` is held.
[ ] ``pullTracked waits for the work tree gate then continues`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks `WorkspaceGit.pullTracked` waits on `WorkspaceGit.withWorkTreeGate`.
[ ] ``saveTracked waits for the work tree gate then continues`` in `tests/Server.Tests/WorkspaceGitTests.fs` checks `WorkspaceGit.saveTracked` waits on `WorkspaceGit.withWorkTreeGate`.
[ ] ``Edit reply returns while the persist thread still holds the write`` in `tests/Server.Tests/FileAgentFailureTests.fs` holds `WorkspaceGit.withWorkTreeGate` inside a fake `persistNode`.
[ ] ``second git waits instead of failing on index.lock`` in `tests/Shared.Tests/GitRunTests.fs` checks two `GitRun.gitExec` calls on one folder run one at a time.
[ ] ``enclosing finds first owner-chain match including start`` in `tests/Shared.Tests/GraphQueryTests.fs` checks `GraphQuery.enclosing` for a Directory predicate on a Directory Node, a File Node under it, and a Workspace Node.
[ ] `graphWithWorkspace` in `tests/Server.Tests/WorkspaceGitTests.fs` builds a Workspace Node with `Graph.create`, `Node.Create`, `Graph.addDetachedNode`, and `Graph.replace`.

## Copy-from

[ ] `WorkspaceGit.withWorkTreeGate` is the gate the node enclosure lock stands beside. This ticket leaves that function in place.
[ ] ``work tree gate waits then continues without overlap`` is the wait shape for the new tests.

## implement

[ ] `NodeEnclosureLock.withNodeLock`, `withPersist`, and `withParseRead` are absent. [04 — Expand the node enclosure lock](../issues/04-expand-the-node-enclosure-lock.md) adds those three functions.
[ ] `src/Server/Gambol.Server.fsproj` has no compile include for `NodeEnclosureLock.fs`. The ticket places that include immediately after `WorkspaceGit.fs`.
[ ] `tests/Server.Tests/NodeEnclosureLockTests.fs` is absent. The ticket adds that file and a compile include immediately after `WorkspaceGitTests.fs`.
[ ] ``directory lock blocks a descendant, allows a sibling, and a workspace lock blocks its file`` observes `withNodeLock` and `withPersist`: the child File action stays outside while the Directory lock runs, the sibling File action enters, and after release the Workspace lock keeps the sibling File action outside.
[ ] ``node enclosure acquire drains an in-flight persist and a parse read`` observes `withPersist`, `withParseRead`, and `withNodeLock`: the Directory lock action stays outside until the in-flight child persist and the in-flight child parse read return, and a persist started during that drain stays outside its action.
[ ] ``persist grant stores the owner chain and includes the directory node`` observes `GraphQuery.enclosing` on the child File Node and the `NodeId list` passed by `withPersist` for that File Node and for the Directory Node.
