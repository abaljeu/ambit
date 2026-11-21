# 11 — Expand the node lock pool
**Status**: coded
One job: The Server takes one node lock from one pool, and a conflicting request waits until the holder releases. Story 5 Directory Node move in [Node move and disk](plan/node-move-disk/spec.md) needs that wait so a write under the moved Directory waits until the move finishes.

Type: coding
Blockers: [04 — Expand the node enclosure lock](../04-expand-the-node-enclosure-lock/04-expand-the-node-enclosure-lock-ticket.md)

## Scope

### Signatures

[x] `NodeEnclosureLock` is absent. Before this ticket, `withNodeLock` was `NodeId -> (unit -> 'a) -> 'a`, `withPersist` was `Graph -> NodeId -> (NodeId list -> 'a) -> 'a`, and `withParseRead` was `Graph -> NodeId -> (unit -> 'a) -> 'a`. This ticket removed those three functions.
[x] `Graph.ownerParentByChild` in `src/Shared/Model.fs` has type `Map<NodeId, NodeId>`.
[x] `GraphQuery.enclosing` in `src/Shared/GraphQuery.fs` has type `Graph -> (Node -> bool) -> NodeId -> NodeId option`.
[x] `NodeKind.container` in `src/Shared/Model.fs` is true for `Special Workspace` and `Special Directory`.
[x] `NodeKind.artifact` in `src/Shared/Model.fs` is true for `Special File`, `Special Directory`, and `Special Workspace`.
[x] `SpecialKind` in `src/Shared/Model.fs` is `Workspaces`, `Workspace`, `Directory`, or `File`.

### Match sites

[x] Before this ticket, `NodeEnclosureLock.ownerChain` walked `ownerParentByChild` with a visited set. The current private walk is `NodeLockPool.ownerChain`.
[x] Before this ticket, `withPersist` and `withParseRead` stored that chain on a grant. The current entry stores no owner chain and no graph.
[x] Before this ticket, `withNodeLock` took no `Graph`. `NodeLockPool.withLock` takes the live graph and one `NodeId`.
[x] `withNodeLock`, `withPersist`, and `withParseRead` are absent. Callers under `src/` use `NodeLockPool.withLock`, `withLockResult`, or `withLockAsync`.
[x] `tests/Server.Tests/NodeEnclosureLockTests.fs` is absent. `NodeLockPoolTests.fs` is the pool test file.
[x] The three enclosure-lock test names are absent.
[x] `src/Server/Gambol.Server.fsproj` compiles `NodeLockPool.fs` immediately after `WorkspaceGit.fs`.
[x] `tests/Server.Tests/Gambol.Server.Tests.fsproj` compiles `NodeLockPoolTests.fs` immediately after `WorkspaceGitTests.fs`.
[x] No source union case names a node enclosure lock.

### Existing tests

[x] The three enclosure-lock tests and `enclosureGraph` are absent. Before this ticket they lived in `NodeEnclosureLockTests.fs`. `NodeLockPoolTests` keeps the descendant-wait, ancestor-wait, and `HolderHoldsLock` behavior on `withLock`.

### Copy-from

[x] Before this ticket, `NodeEnclosureLock` was the module this slice reworked: one `gate` object, `Monitor.Wait`, `Monitor.PulseAll`, and release in `finally`. That module is absent. `NodeLockPool` keeps that wait.
[x] `NodeLockPool.ownerChain` is the private owner-chain walk.
[x] ``work tree gate waits then continues without overlap`` is absent. `NodeLockPoolTests` uses `Task.Run` and `TaskCompletionSource`.

### write-ticket

[x] `src/Server/NodeLockPool.fs` is the new pool module.
[x] `src/Server/NodeEnclosureLock.fs` leaves the tree.
[x] `src/Server/Gambol.Server.fsproj` names `NodeLockPool.fs` in the `NodeEnclosureLock.fs` slot.
[x] This ticket's source set was `src/Server/NodeLockPool.fs`, the removed `src/Server/NodeEnclosureLock.fs`, and `src/Server/Gambol.Server.fsproj`. `NodeEnclosureLock.fs` is absent.
[x] `tests/Server.Tests/NodeLockPoolTests.fs` replaces `tests/Server.Tests/NodeEnclosureLockTests.fs`.
[x] `tests/Server.Tests/Gambol.Server.Tests.fsproj` names `NodeLockPoolTests.fs` in the `NodeEnclosureLockTests.fs` slot.
[x] `tests/Server.Tests/NodeLockPoolTests.fs` is outside the source-file count.
[x] This ticket named four tests, then added the parse-read test. 12 replaced ``a second request on a held node waits and the same thread is rejected`` and ``a request waits when the graphs disagree on a held file``. The current file has seven tests, including ``an absent node proceeds and the same thread is rejected`` and ``a request uses its own graph when the holder's graph disagrees``.

## Inputs

### Working

#### Node lock pool

[x] `NodeLockPool` in `src/Server/NodeLockPool.fs` replaces `NodeEnclosureLock` in `src/Server/NodeEnclosureLock.fs`. Namespace `Gambol.Server`. The module has `[<RequireQualifiedAccess>]`.
[x] `NodeLockPool.withLock` has type `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`.
[x] `NodeLockRejection` has `[<RequireQualifiedAccess>]` and only `HolderHoldsLock`. This ticket also added `NodeAbsent`. 12 deleted that case. An absent node runs the action and queues no entry.
[x] A request is the `Graph` and one `NodeId`. The action is `unit -> 'a`. The caller passes no owner chain.
[x] The pool is process-wide. One gate object guards the table. The action runs outside the gate. Table edits run inside the gate.
[x] The pool uses `Monitor.Wait` and `Monitor.PulseAll`. `PulseAll` runs when an entry is removed. A woken request checks the wait again inside the gate. `NodeEnclosureLock` is absent.
[x] The pool removes the entry in `finally` when the action returns and when the action raises. The pool lets that exception propagate. `Error` before the entry is queued leaves the table as it was.
[x] A throw after the entry is queued, and before that seq is returned to the caller, drops that seq and pulses. The action stays uncalled.
[x] `waitUntilQueued`, `entryCount`, and `pendingCount` are internal. A test waits until the table holds that many entries, then checks the request stayed outside.
[x] `Ok` is the action's value. `HolderHoldsLock` leaves the action uncalled. An absent node runs the action. `NodeAbsent` is absent.
[x] Persist, a move or a rename, and git pull plus marking nodes Unparsed are the holders of `withLock`. Each holder runs its algorithm and then releases.
[x] Alan, 2026-11-10: keep the existing rule. A Directory or Workspace lock holds off a parse read under it. An in-flight parse read drains. A new parse read waits until release.
[x] A parse read is a pool lock request on its node. The one conflict rule covers it: same node, taken ancestor, taken descendant.
[x] That request calls `NodeLockPool.withLock`. `withLock` stays the pool's public function.

#### One conflict rule

[x] An entry stores `seq`, `phase`, `nodeId`, and `threadId`. It stores no graph. This ticket stored the request `Graph` on the entry. 12 dropped that field. `threadId` is `Environment.CurrentManagedThreadId`.
[x] The pool reads the request's `graph.nodes` for the kind. `Special Workspace` and `Special Directory` are containers (`NodeKind.container`). `Special File` is a File. `Special Workspaces` and `Normal` use the same-id rule only.
[x] A missing `graph.nodes` entry runs the action and queues no entry. This ticket returned `Error NodeLockRejection.NodeAbsent` and left the action uncalled. 12 deleted that case.
[x] The same thread id already `Pending` or `Held` returns `Error NodeLockRejection.HolderHoldsLock` before a second entry is queued. The first entry stays.
[x] The owner chain is a walk of `graph.ownerParentByChild` on the request's `Graph`. The walk includes the start id. A visited set stops a cycle. That is the parent step `GraphQuery.enclosing` uses. The pool keeps the walk private and calls no `GraphQuery` function.
[x] `conflicts` reads the graph passed with the request. A taken id is an ancestor when `ownerChain` from the requested id reaches the taken id, and the two ids differ. This ticket also walked the taken entry's graph. 12 kept only the passed graph.
[x] A taken id is a descendant when `ownerChain` from the taken id reaches the requested id, and the two ids differ. That walk uses the same passed graph.
[x] Taken means `Pending` or `Held`.
[x] The same id conflicts for every kind.
[x] A Workspace or Directory request conflicts while a descendant id is taken.
[x] A Directory or File request conflicts while an ancestor id is taken.
[x] The same id, a container's taken descendant, and a Directory or File's taken ancestor are the whole conflict rule.
[x] A request on a Directory node id waits while that same id is taken. That id is the Directory node's own file (`DirName/.amb`).
[x] The request waits while any `Held` entry conflicts, and while any `Pending` entry with a smaller seq conflicts. A `Pending` entry blocks a conflicting request only when its seq is smaller. A request that does not conflict becomes `Held` while an unrelated request stays `Pending`.
[x] After the wait, the pool marks that entry `Held` and runs the action.

#### Current module this change reworks

[x] Before this ticket, `withNodeLock`, `withPersist`, and `withParseRead` had those types, and the persist and parse grants stored the owner chain. Those functions left with `src/Server/NodeEnclosureLock.fs`.

#### Ripple

[x] Before this ticket, those three functions had no caller under `src/`, and the three enclosure-lock tests were their callers. Those tests are absent.
[x] This ticket adds no union case.

#### Copy-from

[x] Before this ticket, `NodeEnclosureLock.ownerChain` and its `gate` object were the walk and the wait. `NodeLockPool` keeps both. `enclosureGraph` is absent.

#### Match sites

[x] Before this ticket, the fsproj files compiled `NodeEnclosureLock.fs` and `NodeEnclosureLockTests.fs` after the WorkspaceGit files. They now compile `NodeLockPool.fs` and `NodeLockPoolTests.fs` in those slots.

### Reference

[x] Alan, 2026-11-10: one pool, one conflict rule, a request is one node id, and the pool derives the owner chain.
[x] Alan, 2026-11-10: a parse read keeps the existing Directory and Workspace hold-off, as one `withLock` request under that conflict rule.
[x] [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Move write gate**. Pending then drain stays. The two-kind grant model is the text this pool replaces.
[x] [core-refinement architecture](plan/core-refinement/arch.md) §6 **Workspace lock** and **Per-member persist locks**. Those two kinds are one request in this pool.
[x] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests. The parse-read amendment adds a fifth test on Alan's 2026-11-10 instruction.

## Do not load

[x] `src/Server/Core/PersistThread.fs` stays as it is.
[x] This ticket left `DocumentPersistWrite.fs` and `ParseThread.fs` unchanged. Both now call `NodeLockPool.withLockResult`.
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
[x] This ticket's source set was `src/Server/NodeLockPool.fs`, the removed `src/Server/NodeEnclosureLock.fs`, and `src/Server/Gambol.Server.fsproj`. `NodeEnclosureLock.fs` is absent.
[x] `tests/Server.Tests/NodeLockPoolTests.fs` — the tests below. The test file is a test output, and it is outside the source-file count.
[x] `tests/Server.Tests/Gambol.Server.Tests.fsproj` — compile include for `NodeLockPoolTests.fs`.
[x] ``taking a directory waits while a descendant file is held and a sibling file enters`` — `withLock` on a File under the Directory is inside its action. `withLock` on that Directory stays outside. `withLock` on the sibling File, owned by the Workspace, enters and returns during the File hold. After the File releases, the Directory enters.
[x] ``taking a workspace waits while a descendant file is held and a file waits while an ancestor is held`` — `withLock` on the sibling File keeps `withLock` on the Workspace outside until release. `withLock` on the Directory keeps `withLock` on the child File outside until release.
[x] Before 12, ``a second request on a held node waits and the same thread is rejected`` expected `Error NodeLockRejection.NodeAbsent` for an id absent from `Graph.create()`. That test name is absent. ``an absent node proceeds and the same thread is rejected`` runs the action for an absent id and returns `Error NodeLockRejection.HolderHoldsLock` for the holding thread's second call.
[x] ``a pending directory lock runs before a new descendant file`` — `withLock` on a File under the Directory is inside its action. `withLock` on that Directory stays outside. `withLock` on the other File under that Directory stays outside, and `withLock` on the sibling File owned by the Workspace enters during that wait. After the held File releases, the Directory enters and the other File stays outside. After the Directory releases, the other File enters.
[x] ``a parse read under a locked directory waits until release`` — `withLock` on the Directory is inside its action. A parse read is `withLock` on the File under that Directory and stays outside. `withLock` on the sibling File owned by the Workspace enters during that wait. After the Directory releases, the parse read enters. Then that parse read is inside its action. `withLock` on the Directory stays outside. The sibling File enters during that wait. After the parse read releases, the Directory enters.
[x] Before 12, ``a request waits when the graphs disagree on a held file`` waited when either graph showed the conflict. That test name is absent. ``a request uses its own graph when the holder's graph disagrees`` waits only when the requester's graph shows the conflict.
[x] ``a thrown wait drops the pending entry`` — a wait that throws drops that pending row. The table then holds only the holder. After the holder releases, the table is empty.

## Stop when

[ ] Record the stop at `plan/node-move-disk/issues/11-expand-the-node-lock-pool/11-build.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit wires `PersistThread`, `DocumentPersistWrite`, `ParseThread`, `FileAgent`, `DbAgent`, `WorkspaceGit`, or `GitRun`.
[ ] Stop when the edit adds a public function other than `withLock`.
[ ] Stop when the edit contradicts story 5 Directory Node move in the spec.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``taking a directory waits while a descendant file is held and a sibling file enters``
  - `tests/Server.Tests`: ``taking a workspace waits while a descendant file is held and a file waits while an ancestor is held``
  - `tests/Server.Tests`: ``an absent node proceeds and the same thread is rejected`` replaced ``a second request on a held node waits and the same thread is rejected``. The old name is absent. The build file records the old filter.
  - `tests/Server.Tests`: ``a pending directory lock runs before a new descendant file``
  - `tests/Server.Tests`: ``a parse read under a locked directory waits until release``
[x] All executed tests pass.
[x] Write the verdict to `plan/node-move-disk/issues/11-expand-the-node-lock-pool/11-build.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
