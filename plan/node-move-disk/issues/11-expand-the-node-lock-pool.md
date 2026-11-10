# 11 — Expand the node lock pool
**Status**: defined
One job: The Server takes one node lock from one pool, and a conflicting request waits until the holder releases. Story 5 Directory Node move in [Node move and disk](plan/node-move-disk/spec.md) needs that wait so a write under the moved Directory waits until the move finishes.

Type: coding
Blockers: [04 — Expand the node enclosure lock](04-expand-the-node-enclosure-lock.md)

## Inputs

### Working

#### Node lock pool

[ ] `NodeLockPool` in `src/Server/NodeLockPool.fs` replaces `NodeEnclosureLock` in `src/Server/NodeEnclosureLock.fs`. Namespace `Gambol.Server`. The module has `[<RequireQualifiedAccess>]`.
[ ] `NodeLockPool.withLock` has type `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`.
[ ] `NodeLockRejection` has `[<RequireQualifiedAccess>]` and two cases: `HolderHoldsLock` and `NodeAbsent`.
[ ] A request is the `Graph` and one `NodeId`. The action is `unit -> 'a`. The caller passes no owner chain.
[ ] The pool is process-wide. One gate object guards the table. The action runs outside the gate. Table edits run inside the gate.
[ ] The gate uses `Monitor.Wait` and `Monitor.PulseAll` the way `NodeEnclosureLock` does. `PulseAll` runs when an entry is removed. A woken request checks the wait again inside the gate.
[ ] The pool removes the entry in `finally` when the action returns and when the action raises. The pool lets that exception propagate. `Error` before the entry is queued leaves the table as it was.
[ ] `Ok` is the action's value. `HolderHoldsLock` and `NodeAbsent` leave the action uncalled.
[ ] Persist, a move or a rename, and git pull plus marking nodes Unparsed are the holders of `withLock`. Each holder runs its algorithm and then releases.
[ ] Whether a parse read takes `withLock` is open. The pool's public function is `withLock`.

#### One conflict rule

[ ] The pool stores each request as `Pending` or `Held`, with that request's `Graph`, `NodeId`, `Environment.CurrentManagedThreadId`, and an increasing seq.
[ ] The pool reads the request's `graph.nodes` for the kind. `Special Workspace` and `Special Directory` are containers (`NodeKind.container`). `Special File` is a File. `Special Workspaces` and `Normal` use the same-id rule only.
[ ] A missing `graph.nodes` entry for the requested id returns `Error NodeLockRejection.NodeAbsent` before a wait.
[ ] The same thread id already `Pending` or `Held` returns `Error NodeLockRejection.HolderHoldsLock` before a second entry is queued. The first entry stays.
[ ] The owner chain is a walk of `graph.ownerParentByChild` on the request's `Graph`. The walk includes the start id. A visited set stops a cycle. That is the parent step `GraphQuery.enclosing` uses. The pool keeps the walk private and calls no `GraphQuery` function.
[ ] A taken id is an ancestor of the requested id when the walk from the requested id reaches the taken id and the two ids differ.
[ ] A taken id is a descendant of the requested id when the walk from the taken id reaches the requested id and the two ids differ. The descendant check walks taken ids.
[ ] Taken means `Pending` or `Held`.
[ ] The same id conflicts for every kind.
[ ] A Workspace or Directory request conflicts while a descendant id is taken.
[ ] A Directory or File request conflicts while an ancestor id is taken.
[ ] The same id, a container's taken descendant, and a Directory or File's taken ancestor are the whole conflict rule.
[ ] A request on a Directory node id waits while that same id is taken. That id is the Directory node's own file (`DirName/.amb`).
[ ] The request waits while any `Held` entry conflicts, and while any `Pending` entry with a smaller seq conflicts. A `Pending` entry blocks a conflicting request only when its seq is smaller. A request that does not conflict becomes `Held` while an unrelated request stays `Pending`.
[ ] After the wait, the pool marks that entry `Held` and runs the action.

#### Current module this change reworks

[ ] `NodeEnclosureLock.withNodeLock` has type `NodeId -> (unit -> 'a) -> 'a`.
[ ] `NodeEnclosureLock.withPersist` has type `Graph -> NodeId -> (NodeId list -> 'a) -> 'a`.
[ ] `NodeEnclosureLock.withParseRead` has type `Graph -> NodeId -> (unit -> 'a) -> 'a`.
[ ] `withPersist` and `withParseRead` store the owner chain and wait on a separate grant list. `withNodeLock` marks one id pending, drains grants that stored that id, then holds that id.
[ ] Those three functions leave with `src/Server/NodeEnclosureLock.fs`.

#### Ripple

[ ] `withNodeLock`, `withPersist`, and `withParseRead` have no caller under `src/`.
[ ] ``directory lock blocks a descendant, allows a sibling, and a workspace lock blocks its file`` in `tests/Server.Tests/NodeEnclosureLockTests.fs` calls `withNodeLock` twice and `withPersist` three times.
[ ] ``node enclosure acquire drains an in-flight persist and a parse read`` in `tests/Server.Tests/NodeEnclosureLockTests.fs` calls `withPersist` twice, `withParseRead` once, and `withNodeLock` once.
[ ] ``persist grant stores the owner chain and includes the directory node`` in `tests/Server.Tests/NodeEnclosureLockTests.fs` calls `withPersist` twice.
[ ] This ticket adds no union case.

#### Copy-from

[ ] `NodeEnclosureLock.ownerChain` and the `gate` object in `src/Server/NodeEnclosureLock.fs` are the walk and the wait this pool keeps.
[ ] `enclosureGraph` in `tests/Server.Tests/NodeEnclosureLockTests.fs` is the graph builder. The new builder adds one more File Node owned by the Directory Node.

#### Match sites

[ ] `src/Server/Gambol.Server.fsproj` compiles `NodeEnclosureLock.fs` immediately after `WorkspaceGit.fs`.
[ ] `tests/Server.Tests/Gambol.Server.Tests.fsproj` compiles `NodeEnclosureLockTests.fs` immediately after `WorkspaceGitTests.fs`.

### Reference

[ ] Alan, 2026-11-10: one pool, one conflict rule, a request is one node id, and the pool derives the owner chain.
[ ] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Move write gate**. Pending then drain stays. The two-kind grant model is the text this pool replaces.
[ ] [core-refinement architecture](plan/core-refinement/arch.md) §6 **Workspace lock** and **Per-member persist locks**. Those two kinds are one request in this pool.
[ ] [11 — Expand the node lock pool scope](plan/node-move-disk/reports/11-expand-the-node-lock-pool-scope.md).
[ ] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[ ] `src/Server/Core/PersistThread.fs` stays as it is.
[ ] `src/Server/DocumentPersistWrite.fs` stays as it is.
[ ] `src/Server/ParseThread.fs` stays as it is.
[ ] `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` stay as they are.
[ ] `src/Server/WorkspaceGit.fs`, `src/Server/DocumentPersistPath.fs`, and `src/Shared/dotnet/GitRun.fs` stay as they are.
[ ] `src/Shared/GraphQuery.fs` stays as it is.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand `src/Server/NodeLockPool.fs` with `withLock` as Working specifies.
3. [ ] Contract `src/Server/NodeEnclosureLock.fs`.
4. [ ] Change `src/Server/Gambol.Server.fsproj` so `<Compile Include="NodeLockPool.fs" />` stands where `NodeEnclosureLock.fs` stands, immediately after `WorkspaceGit.fs`.
5. [ ] Expand `tests/Server.Tests/NodeLockPoolTests.fs` as module `Gambol.Server.Tests.NodeLockPoolTests` with the four tests in Outputs. The graph has one Workspace Node, one Directory Node owned by that Workspace, two File Nodes owned by that Directory, and one sibling File Node owned by that Workspace. Build it with `Graph.create`, `Node.Create`, `Graph.addDetachedNode`, and `Graph.replace`.
6. [ ] Contract `tests/Server.Tests/NodeEnclosureLockTests.fs`.
7. [ ] Change `tests/Server.Tests/Gambol.Server.Tests.fsproj` so `<Compile Include="NodeLockPoolTests.fs" />` stands where `NodeEnclosureLockTests.fs` stands, immediately after `WorkspaceGitTests.fs`.

## Outputs

[ ] `src/Server/NodeLockPool.fs` — `withLock`.
[ ] `src/Server/NodeEnclosureLock.fs` — removed.
[ ] `src/Server/Gambol.Server.fsproj` — compile include for `NodeLockPool.fs`.
[ ] Distinct source files: 3. `src/Server/NodeLockPool.fs`, `src/Server/NodeEnclosureLock.fs`, and `src/Server/Gambol.Server.fsproj`.
[ ] `tests/Server.Tests/NodeLockPoolTests.fs` — the four tests below. The test file is a test output, and it is outside the source-file count.
[ ] `tests/Server.Tests/Gambol.Server.Tests.fsproj` — compile include for `NodeLockPoolTests.fs`.
[ ] ``taking a directory waits while a descendant file is held and a sibling file enters`` — `withLock` on a File under the Directory is inside its action. `withLock` on that Directory stays outside. `withLock` on the sibling File, owned by the Workspace, enters and returns during the File hold. After the File releases, the Directory enters.
[ ] ``taking a workspace waits while a descendant file is held and a file waits while an ancestor is held`` — `withLock` on the sibling File keeps `withLock` on the Workspace outside until release. `withLock` on the Directory keeps `withLock` on the child File outside until release.
[ ] ``a second request on a held node waits and the same thread is rejected`` — a second thread's `withLock` on the held File stays outside until release. The holding thread's nested `withLock` returns `Error NodeLockRejection.HolderHoldsLock` and the first entry stays. `withLock` on an id absent from `Graph.create()` returns `Error NodeLockRejection.NodeAbsent`. The holding action then raises, and the waiting thread enters after release.
[ ] ``a pending directory lock runs before a new descendant file`` — `withLock` on a File under the Directory is inside its action. `withLock` on that Directory stays outside. `withLock` on the other File under that Directory stays outside, and `withLock` on the sibling File owned by the Workspace enters during that wait. After the held File releases, the Directory enters and the other File stays outside. After the Directory releases, the other File enters.

## Stop when

[ ] Record the stop at `plan/node-move-disk/reports/11-expand-the-node-lock-pool-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit wires `PersistThread`, `DocumentPersistWrite`, `ParseThread`, `FileAgent`, `DbAgent`, `WorkspaceGit`, or `GitRun`.
[ ] Stop when the edit adds a public function other than `withLock`.
[ ] Stop when the edit contradicts story 5 Directory Node move in the spec.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``taking a directory waits while a descendant file is held and a sibling file enters``
  - `tests/Server.Tests`: ``taking a workspace waits while a descendant file is held and a file waits while an ancestor is held``
  - `tests/Server.Tests`: ``a second request on a held node waits and the same thread is rejected``
  - `tests/Server.Tests`: ``a pending directory lock runs before a new descendant file``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/node-move-disk/reports/11-expand-the-node-lock-pool-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
