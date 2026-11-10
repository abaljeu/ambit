# 11 — Expand the node lock pool scope

## Signatures

[ ] `NodeEnclosureLock.withNodeLock` in `src/Server/NodeEnclosureLock.fs` has type `NodeId -> (unit -> 'a) -> 'a`.
[ ] `NodeEnclosureLock.withPersist` in `src/Server/NodeEnclosureLock.fs` has type `Graph -> NodeId -> (NodeId list -> 'a) -> 'a`.
[ ] `NodeEnclosureLock.withParseRead` in `src/Server/NodeEnclosureLock.fs` has type `Graph -> NodeId -> (unit -> 'a) -> 'a`.
[ ] `Graph.ownerParentByChild` in `src/Shared/Model.fs` has type `Map<NodeId, NodeId>`.
[ ] `GraphQuery.enclosing` in `src/Shared/GraphQuery.fs` has type `Graph -> (Node -> bool) -> NodeId -> NodeId option`.
[ ] `NodeKind.container` in `src/Shared/Model.fs` is true for `Special Workspace` and `Special Directory`.
[ ] `NodeKind.artifact` in `src/Shared/Model.fs` is true for `Special File`, `Special Directory`, and `Special Workspace`.
[ ] `SpecialKind` in `src/Shared/Model.fs` is `Workspaces`, `Workspace`, `Directory`, or `File`.

## Match sites

[ ] `NodeEnclosureLock.ownerChain` walks `graph.ownerParentByChild` from the start id, includes the start id, and a visited set stops a cycle.
[ ] `NodeEnclosureLock.withPersist` and `NodeEnclosureLock.withParseRead` store that owner chain on a grant and wait while any stored id has phase `Pending` or `Held`.
[ ] `NodeEnclosureLock.withNodeLock` takes no `Graph`. It marks one id `Pending`, waits until no grant stores that id, marks that id `Held`, runs the action, and clears the phase in `finally`.
[ ] `withNodeLock`, `withPersist`, and `withParseRead` have no caller under `src/`.
[ ] `tests/Server.Tests/NodeEnclosureLockTests.fs` is the only caller of those three functions.
[ ] ``directory lock blocks a descendant, allows a sibling, and a workspace lock blocks its file`` calls `withNodeLock` twice and `withPersist` three times.
[ ] ``node enclosure acquire drains an in-flight persist and a parse read`` calls `withPersist` twice, `withParseRead` once, and `withNodeLock` once.
[ ] ``persist grant stores the owner chain and includes the directory node`` calls `withPersist` twice and `GraphQuery.enclosing` once.
[ ] `src/Server/Gambol.Server.fsproj` compiles `NodeEnclosureLock.fs` immediately after `WorkspaceGit.fs`.
[ ] `tests/Server.Tests/Gambol.Server.Tests.fsproj` compiles `NodeEnclosureLockTests.fs` immediately after `WorkspaceGitTests.fs`.
[ ] No source union case names a node enclosure lock.

## Existing tests

[ ] ``directory lock blocks a descendant, allows a sibling, and a workspace lock blocks its file`` in `tests/Server.Tests/NodeEnclosureLockTests.fs` checks a Directory `withNodeLock` keeps a child File `withPersist` outside and lets a sibling File `withPersist` enter, then a Workspace `withNodeLock` keeps that sibling `withPersist` outside.
[ ] ``node enclosure acquire drains an in-flight persist and a parse read`` in `tests/Server.Tests/NodeEnclosureLockTests.fs` checks a Directory `withNodeLock` stays outside until an in-flight child `withPersist` and an in-flight child `withParseRead` return, and a `withPersist` started during that wait stays outside.
[ ] ``persist grant stores the owner chain and includes the directory node`` in `tests/Server.Tests/NodeEnclosureLockTests.fs` checks `GraphQuery.enclosing` on the child File and checks that `withPersist` passes a list containing that File id and the Directory id.
[ ] `enclosureGraph` in `tests/Server.Tests/NodeEnclosureLockTests.fs` builds one Workspace Node, one Directory Node owned by that Workspace, one File Node owned by that Directory, and one sibling File Node owned by that Workspace, through `Graph.create`, `Node.Create`, `Graph.addDetachedNode`, and `Graph.replace`.

## Copy-from

[ ] `NodeEnclosureLock` in `src/Server/NodeEnclosureLock.fs` is the module this slice reworks: one `gate` object, `Monitor.Wait`, `Monitor.PulseAll`, and release in `finally`.
[ ] `NodeEnclosureLock.ownerChain` is the owner-chain walk the pool keeps private.
[ ] ``work tree gate waits then continues without overlap`` in `tests/Server.Tests/WorkspaceGitTests.fs` is the `Task.Run` and `TaskCompletionSource` wait shape the lock tests already use.

## write-ticket

[ ] `src/Server/NodeLockPool.fs` is the new pool module.
[ ] `src/Server/NodeEnclosureLock.fs` leaves the tree.
[ ] `src/Server/Gambol.Server.fsproj` names `NodeLockPool.fs` in the `NodeEnclosureLock.fs` slot.
[ ] Distinct source files: 3. `src/Server/NodeLockPool.fs`, `src/Server/NodeEnclosureLock.fs`, and `src/Server/Gambol.Server.fsproj`.
[ ] `tests/Server.Tests/NodeLockPoolTests.fs` replaces `tests/Server.Tests/NodeEnclosureLockTests.fs`.
[ ] `tests/Server.Tests/Gambol.Server.Tests.fsproj` names `NodeLockPoolTests.fs` in the `NodeEnclosureLockTests.fs` slot.
[ ] The test file is outside the source-file count.
[ ] Tests added or changed: 4. ``taking a directory waits while a descendant file is held and a sibling file enters``, ``taking a workspace waits while a descendant file is held and a file waits while an ancestor is held``, ``a second request on a held node waits and the same thread is rejected``, ``a pending directory lock runs before a new descendant file``.
