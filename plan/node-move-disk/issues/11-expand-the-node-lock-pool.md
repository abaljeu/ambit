# 11 — Expand the node lock pool
**Status**: coded
One job: The Server takes one node lock from one pool, and a conflicting request waits until the holder releases. Story 5 Directory Node move in [Node move and disk](plan/node-move-disk/spec.md) needs that wait so a write under the moved Directory waits until the move finishes.

Type: coding
Blockers: [04 — Expand the node enclosure lock](04-expand-the-node-enclosure-lock.md)

## Inputs

### Working

#### Node lock pool

[x] `NodeLockPool` in `src/Server/NodeLockPool.fs` replaces `NodeEnclosureLock` in `src/Server/NodeEnclosureLock.fs`. Namespace `Gambol.Server`. The module has `[<RequireQualifiedAccess>]`.
[x] `NodeLockPool.withLock` has type `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`.
[x] `NodeLockRejection` has `[<RequireQualifiedAccess>]` and two cases: `HolderHoldsLock` and `NodeAbsent`.
[x] A request is the `Graph` and one `NodeId`. The action is `unit -> 'a`. The caller passes no owner chain.
[x] The pool is process-wide. One gate object guards the table. The action runs outside the gate. Table edits run inside the gate.
[x] The gate uses `Monitor.Wait` and `Monitor.PulseAll` the way `NodeEnclosureLock` does. `PulseAll` runs when an entry is removed. A woken request checks the wait again inside the gate.
[x] The pool removes the entry in `finally` when the action returns and when the action raises. The pool lets that exception propagate. `Error` before the entry is queued leaves the table as it was.
[x] A throw after the entry is queued, and before that seq is returned to the caller, drops that seq and pulses. The action stays uncalled.
[x] `waitUntilQueued`, `entryCount`, and `pendingCount` are internal. A test waits until the table holds that many entries, then checks the request stayed outside.
[x] `Ok` is the action's value. `HolderHoldsLock` and `NodeAbsent` leave the action uncalled.
[x] Persist, a move or a rename, and git pull plus marking nodes Unparsed are the holders of `withLock`. Each holder runs its algorithm and then releases.
[x] Alan, 2026-11-10: keep the existing rule. A Directory or Workspace lock holds off a parse read under it. An in-flight parse read drains. A new parse read waits until release.
[x] A parse read is a pool lock request on its node. The one conflict rule covers it: same node, taken ancestor, taken descendant.
[x] That request calls `NodeLockPool.withLock`. `withLock` stays the pool's public function.

#### One conflict rule

[x] The pool stores each request as `Pending` or `Held`, with that request's `Graph`, `NodeId`, `Environment.CurrentManagedThreadId`, and an increasing seq.
[x] The pool reads the request's `graph.nodes` for the kind. `Special Workspace` and `Special Directory` are containers (`NodeKind.container`). `Special File` is a File. `Special Workspaces` and `Normal` use the same-id rule only.
[x] A missing `graph.nodes` entry for the requested id returns `Error NodeLockRejection.NodeAbsent` before a wait.
[x] The same thread id already `Pending` or `Held` returns `Error NodeLockRejection.HolderHoldsLock` before a second entry is queued. The first entry stays.
[x] The owner chain is a walk of `graph.ownerParentByChild` on the request's `Graph`. The walk includes the start id. A visited set stops a cycle. That is the parent step `GraphQuery.enclosing` uses. The pool keeps the walk private and calls no `GraphQuery` function.
[x] A taken id is an ancestor of the requested id when a walk on the request's graph or the taken entry's graph, from the requested id, reaches the taken id, and the two ids differ.
[x] A taken id is a descendant of the requested id when a walk on the request's graph or the taken entry's graph, from the taken id, reaches the requested id, and the two ids differ.
[x] Taken means `Pending` or `Held`.
[x] The same id conflicts for every kind.
[x] A Workspace or Directory request conflicts while a descendant id is taken.
[x] A Directory or File request conflicts while an ancestor id is taken.
[x] The same id, a container's taken descendant, and a Directory or File's taken ancestor are the whole conflict rule.
[x] A request on a Directory node id waits while that same id is taken. That id is the Directory node's own file (`DirName/.amb`).
[x] The request waits while any `Held` entry conflicts, and while any `Pending` entry with a smaller seq conflicts. A `Pending` entry blocks a conflicting request only when its seq is smaller. A request that does not conflict becomes `Held` while an unrelated request stays `Pending`.
[x] After the wait, the pool marks that entry `Held` and runs the action.

#### Current module this change reworks

[x] `NodeEnclosureLock.withNodeLock` has type `NodeId -> (unit -> 'a) -> 'a`.
[x] `NodeEnclosureLock.withPersist` has type `Graph -> NodeId -> (NodeId list -> 'a) -> 'a`.
[x] `NodeEnclosureLock.withParseRead` has type `Graph -> NodeId -> (unit -> 'a) -> 'a`.
[x] `withPersist` and `withParseRead` store the owner chain and wait on a separate grant list. `withNodeLock` marks one id pending, drains grants that stored that id, then holds that id.
[x] Those three functions leave with `src/Server/NodeEnclosureLock.fs`.

#### Ripple

[x] `withNodeLock`, `withPersist`, and `withParseRead` have no caller under `src/`.
[x] ``directory lock blocks a descendant, allows a sibling, and a workspace lock blocks its file`` in `tests/Server.Tests/NodeEnclosureLockTests.fs` calls `withNodeLock` twice and `withPersist` three times.
[x] ``node enclosure acquire drains an in-flight persist and a parse read`` in `tests/Server.Tests/NodeEnclosureLockTests.fs` calls `withPersist` twice, `withParseRead` once, and `withNodeLock` once.
[x] ``persist grant stores the owner chain and includes the directory node`` in `tests/Server.Tests/NodeEnclosureLockTests.fs` calls `withPersist` twice.
[x] This ticket adds no union case.

#### Copy-from

[x] `NodeEnclosureLock.ownerChain` and the `gate` object in `src/Server/NodeEnclosureLock.fs` are the walk and the wait this pool keeps.
[x] `enclosureGraph` in `tests/Server.Tests/NodeEnclosureLockTests.fs` is the graph builder. The new builder adds one more File Node owned by the Directory Node.

#### Match sites

[x] `src/Server/Gambol.Server.fsproj` compiles `NodeEnclosureLock.fs` immediately after `WorkspaceGit.fs`.
[x] `tests/Server.Tests/Gambol.Server.Tests.fsproj` compiles `NodeEnclosureLockTests.fs` immediately after `WorkspaceGitTests.fs`.

### Reference

[x] Alan, 2026-11-10: one pool, one conflict rule, a request is one node id, and the pool derives the owner chain.
[x] Alan, 2026-11-10: a parse read keeps the existing Directory and Workspace hold-off, as one `withLock` request under that conflict rule.
[x] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Move write gate**. Pending then drain stays. The two-kind grant model is the text this pool replaces.
[x] [core-refinement architecture](plan/core-refinement/arch.md) §6 **Workspace lock** and **Per-member persist locks**. Those two kinds are one request in this pool.
[x] [11 — Expand the node lock pool scope](plan/node-move-disk/reports/11-expand-the-node-lock-pool-scope.md).
[x] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests. The parse-read amendment adds a fifth test on Alan's 2026-11-10 instruction.

## Do not load

[x] `src/Server/Core/PersistThread.fs` stays as it is.
[x] `src/Server/DocumentPersistWrite.fs` stays as it is.
[x] `src/Server/ParseThread.fs` stays as it is.
[x] `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` stay as they are.
[x] `src/Server/WorkspaceGit.fs`, `src/Server/DocumentPersistPath.fs`, and `src/Shared/dotnet/GitRun.fs` stay as they are.
[x] `src/Shared/GraphQuery.fs` stays as it is.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Expand `src/Server/NodeLockPool.fs` with `withLock` as Working specifies.
3. [x] Contract `src/Server/NodeEnclosureLock.fs`.
4. [x] Change `src/Server/Gambol.Server.fsproj` so `<Compile Include="NodeLockPool.fs" />` stands where `NodeEnclosureLock.fs` stands, immediately after `WorkspaceGit.fs`.
5. [x] Expand `tests/Server.Tests/NodeLockPoolTests.fs` as module `Gambol.Server.Tests.NodeLockPoolTests` with the four tests in Outputs. The graph has one Workspace Node, one Directory Node owned by that Workspace, two File Nodes owned by that Directory, and one sibling File Node owned by that Workspace. Build it with `Graph.create`, `Node.Create`, `Graph.addDetachedNode`, and `Graph.replace`.
6. [x] Contract `tests/Server.Tests/NodeEnclosureLockTests.fs`.
7. [x] Change `tests/Server.Tests/Gambol.Server.Tests.fsproj` so `<Compile Include="NodeLockPoolTests.fs" />` stands where `NodeEnclosureLockTests.fs` stands, immediately after `WorkspaceGitTests.fs`.
8. [x] Close the parse-read question in Working, and add the parse-read test in `tests/Server.Tests/NodeLockPoolTests.fs`.

## Outputs

[x] `src/Server/NodeLockPool.fs` — `withLock`.
[x] `src/Server/NodeEnclosureLock.fs` — removed.
[x] `src/Server/Gambol.Server.fsproj` — compile include for `NodeLockPool.fs`.
[x] Distinct source files: 3. `src/Server/NodeLockPool.fs`, `src/Server/NodeEnclosureLock.fs`, and `src/Server/Gambol.Server.fsproj`.
[x] `tests/Server.Tests/NodeLockPoolTests.fs` — the tests below. The test file is a test output, and it is outside the source-file count.
[x] `tests/Server.Tests/Gambol.Server.Tests.fsproj` — compile include for `NodeLockPoolTests.fs`.
[x] ``taking a directory waits while a descendant file is held and a sibling file enters`` — `withLock` on a File under the Directory is inside its action. `withLock` on that Directory stays outside. `withLock` on the sibling File, owned by the Workspace, enters and returns during the File hold. After the File releases, the Directory enters.
[x] ``taking a workspace waits while a descendant file is held and a file waits while an ancestor is held`` — `withLock` on the sibling File keeps `withLock` on the Workspace outside until release. `withLock` on the Directory keeps `withLock` on the child File outside until release.
[x] ``a second request on a held node waits and the same thread is rejected`` — a second thread's `withLock` on the held File stays outside until release. The holding thread's nested `withLock` returns `Error NodeLockRejection.HolderHoldsLock` and the first entry stays. `withLock` on an id absent from `Graph.create()` returns `Error NodeLockRejection.NodeAbsent`. The holding action then raises, and the waiting thread enters after release.
[x] ``a pending directory lock runs before a new descendant file`` — `withLock` on a File under the Directory is inside its action. `withLock` on that Directory stays outside. `withLock` on the other File under that Directory stays outside, and `withLock` on the sibling File owned by the Workspace enters during that wait. After the held File releases, the Directory enters and the other File stays outside. After the Directory releases, the other File enters.
[x] ``a parse read under a locked directory waits until release`` — `withLock` on the Directory is inside its action. A parse read is `withLock` on the File under that Directory and stays outside. `withLock` on the sibling File owned by the Workspace enters during that wait. After the Directory releases, the parse read enters. Then that parse read is inside its action. `withLock` on the Directory stays outside. The sibling File enters during that wait. After the parse read releases, the Directory enters.
[x] ``a request waits when the graphs disagree on a held file`` — the requester's graph places a held File under the requested Directory and the holder's graph does not, and the request stays pending. The reverse, a held Directory whose graph places the File under it while the requester's graph does not, also stays pending. Each request enters after release.
[x] ``a thrown wait drops the pending entry`` — a wait that throws drops that pending row. The table then holds only the holder. After the holder releases, the table is empty.

## Stop when

[ ] Record the stop at `plan/node-move-disk/reports/11-expand-the-node-lock-pool-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit wires `PersistThread`, `DocumentPersistWrite`, `ParseThread`, `FileAgent`, `DbAgent`, `WorkspaceGit`, or `GitRun`.
[ ] Stop when the edit adds a public function other than `withLock`.
[ ] Stop when the edit contradicts story 5 Directory Node move in the spec.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``taking a directory waits while a descendant file is held and a sibling file enters``
  - `tests/Server.Tests`: ``taking a workspace waits while a descendant file is held and a file waits while an ancestor is held``
  - `tests/Server.Tests`: ``a second request on a held node waits and the same thread is rejected``
  - `tests/Server.Tests`: ``a pending directory lock runs before a new descendant file``
  - `tests/Server.Tests`: ``a parse read under a locked directory waits until release``
[x] All executed tests pass.
[x] Write the verdict to `plan/node-move-disk/reports/11-expand-the-node-lock-pool-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
