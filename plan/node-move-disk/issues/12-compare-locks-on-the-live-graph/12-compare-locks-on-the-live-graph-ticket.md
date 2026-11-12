# 12 — Compare locks on the live graph
**Status**: coded
One job: The Server compares a new lock on the live graph passed with that request, an absent node conflicts with nothing, and a holder that already holds a pool lock gets `Error HolderHoldsLock` immediately. Alan, 2026-11-10. The rule is [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Node lock**. Story 5 Directory Node move in [Node move and disk](plan/node-move-disk/spec.md) waits on that comparison.

Type: coding
Blockers: [11 — Expand the node lock pool](../11-expand-the-node-lock-pool/11-expand-the-node-lock-pool-ticket.md)

## Scope

### Signatures

[ ] `NodeLockPool.withLock` in `src/Server/NodeLockPool.fs` has type `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`.
[ ] `NodeLockRejection` in `src/Server/NodeLockPool.fs` has `[<RequireQualifiedAccess>]` and the cases `HolderHoldsLock` and `NodeAbsent`.
[ ] `NodeLockPool.Entry` in `src/Server/NodeLockPool.fs` stores `seq`, `phase`, `graph`, `nodeId`, and `threadId`.
[ ] `Graph.ownerParentByChild` in `src/Shared/Model.fs` has type `Map<NodeId, NodeId>`.
[ ] `Graph.nodes` in `src/Shared/Model.fs` has type `Map<NodeId, Node>`.
[ ] `NodeKind.container` in `src/Shared/Model.fs` is true for `Special Workspace` and `Special Directory`.
[ ] `Environment.CurrentManagedThreadId` is the thread id `NodeLockPool.enqueue` stores.

### Match sites

[ ] `NodeLockPool.withLock` returns `Error NodeLockRejection.NodeAbsent` when `graph.nodes` has no entry for the requested id, and the action stays uncalled.
[ ] `NodeLockPool.enqueue` returns `Error NodeLockRejection.HolderHoldsLock` when any entry has the same `threadId`, before a second entry is queued.
[ ] `NodeLockPool.conflicts` reads `request.graph` and `taken.graph`.
[ ] `NodeLockPool.ownerChain` walks `graph.ownerParentByChild`, includes the start id, stops when `graph.nodes` lacks the current id, and a visited set stops a cycle.
[ ] `NodeLockPool.withLock` is the only public function on `NodeLockPool`. `entryCount`, `pendingCount`, and `waitUntilQueued` are `internal`.
[ ] `src/` has no caller of `NodeLockPool.withLock`. The callers are in `tests/Server.Tests/NodeLockPoolTests.fs`.

### Existing tests

[ ] ``a request waits when the graphs disagree on a held file`` in `tests/Server.Tests/NodeLockPoolTests.fs` calls `assertRequestWaits` twice, so either graph's owner chain makes the request stay outside until release.
[ ] ``a second request on a held node waits and the same thread is rejected`` in `tests/Server.Tests/NodeLockPoolTests.fs` expects `Error NodeLockRejection.HolderHoldsLock` for the nested call and `Error NodeLockRejection.NodeAbsent` for an id absent from `Graph.create()`.
[ ] ``taking a directory waits while a descendant file is held and a sibling file enters`` keeps the Directory outside while the child File is held and lets the sibling File enter.
[ ] ``taking a workspace waits while a descendant file is held and a file waits while an ancestor is held`` keeps the Workspace outside while a descendant File is held, then keeps the File outside while the Directory is held.
[ ] ``a pending directory lock runs before a new descendant file`` keeps the later File outside until the earlier Directory request releases.
[ ] ``a parse read under a locked directory waits until release`` keeps the File outside while the Directory is held, and keeps the Directory outside while the File is held.
[ ] ``a thrown wait drops the pending entry`` interrupts the waiting thread and expects that pending row to leave the pool.

### Copy-from

[ ] `NodeLockPool.enqueue` in `src/Server/NodeLockPool.fs` is the `Monitor.Wait`, `Monitor.PulseAll`, and `try`/`with` drop this change keeps.
[ ] `NodeLockPool.ownerChain` in `src/Server/NodeLockPool.fs` is the owner-chain walk this change keeps on the passed graph.
[ ] `disagreeGraphs` and `assertRequestWaits` in `tests/Server.Tests/NodeLockPoolTests.fs` are the two-graph fixture and the outside-until-release seam.

### implement

[ ] The pool stores `graph` on each entry. The ticket stores `seq`, `phase`, `nodeId`, and `threadId` only.
[ ] `NodeLockPool.conflicts` treats a chain on either graph as a conflict. The ticket reads only the graph passed with the new request.
[ ] `NodeLockPool.withLock` returns `Error NodeLockRejection.NodeAbsent` for a missing `graph.nodes` entry. The ticket runs the action and queues no entry.
[ ] `NodeLockRejection.NodeAbsent` exists. The ticket deletes that case and keeps `HolderHoldsLock`.
[ ] `NodeLockPool.ownerChain` stops when `graph.nodes` lacks the current id. The ticket walks `graph.ownerParentByChild` only.
[ ] ``an absent node proceeds and the same thread is rejected`` observes `withLock` at the public seam: an id absent from `Graph.create()` returns `Ok`, a File `withLock` enters during that action, an absent id enters during a held File, the holder thread's sibling `withLock` returns `Error NodeLockRejection.HolderHoldsLock` with the sibling action uncalled and `entryCount` still 1, and a second thread stays outside `waitUntilQueued` until release.
[ ] ``a request uses its own graph when the holder's graph disagrees`` observes `withLock` at the public seam: `fileBesideDir` held on the File and `fileUnderDir` requested on the Directory stays outside until release, and `fileUnderDir` held on the File lets `fileBesideDir` on the Directory enter while that File is held.

## Inputs

### Working

#### One graph

[x] There is one graph. The pool stores no graph.
[x] A request is that live graph and one `NodeId`. Conflict checks read the graph passed with the new request.
[x] An absent node conflicts with nothing. A request whose id is absent from that graph proceeds. That request queues no entry, so it blocks nothing.
[x] `withLock` blocks while a conflict is taken. Otherwise it proceeds.
[x] `NodeLockRejection` keeps `HolderHoldsLock`. `NodeAbsent` is deleted.
[x] `withLock` has type `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`.
[x] A holder that already holds a pool lock and requests another gets `Error HolderHoldsLock` immediately instead of waiting. The first entry stays. The new action stays uncalled.
[x] The same thread is `Environment.CurrentManagedThreadId`.
[x] That reject is how the pool enforces that nobody takes two locks. It prevents a holder waiting on itself.

#### Conflict check

[x] An entry stores `seq`, `phase`, `nodeId`, and `threadId`. The entry stores no `graph`.
[x] `threadId` is `Environment.CurrentManagedThreadId`.
[x] Taken means `Pending` or `Held`.
[x] The same id conflicts for every kind, when the requested id is present in the passed graph.
[x] The owner chain is a walk of `graph.ownerParentByChild` on the passed graph. The walk includes the start id. A visited set stops a cycle.
[x] A taken id is an ancestor when that walk from the requested id reaches the taken id, and the two ids differ.
[x] A taken id is a descendant when that walk from the taken id reaches the requested id, and the two ids differ.
[x] A Workspace or Directory request conflicts while a descendant id is taken.
[x] A Directory or File request conflicts while an ancestor id is taken.
[x] The request waits while any `Held` entry conflicts, and while any `Pending` entry with a smaller seq conflicts.
[x] The pool marks the entry `Held` and runs the action. The action's return releases the entry.

#### Current module this change reworks

[x] `NodeLockPool.withLock` in `src/Server/NodeLockPool.fs` returns `Result<'a, NodeLockRejection>`.
[x] `NodeLockRejection` has `HolderHoldsLock` and `NodeAbsent`. This ticket deletes `NodeAbsent` and keeps `HolderHoldsLock`.
[x] An entry stores `graph` and `threadId`. This ticket drops `graph` and keeps `threadId`.
[x] A missing `graph.nodes` entry returns `Error NodeLockRejection.NodeAbsent`.
[x] The same `threadId` already `Pending` or `Held` returns `Error NodeLockRejection.HolderHoldsLock`. This ticket keeps that reject.
[x] `conflicts` walks the request graph and the taken entry's graph.

#### Tests this change reworks

[x] ``a second request on a held node waits and the same thread is rejected`` in `tests/Server.Tests/NodeLockPoolTests.fs` expects `Error NodeLockRejection.HolderHoldsLock` and `Error NodeLockRejection.NodeAbsent`.
[x] ``a request waits when the graphs disagree on a held file`` in `tests/Server.Tests/NodeLockPoolTests.fs` waits when either graph shows the conflict.

### Reference

[x] Alan, 2026-11-10: there is one graph, and the pool stores no graph. Locks are compared on the live graph passed in to the request. An absent node is unlikely, and it conflicts with nothing. A holder that already holds a pool lock and requests another gets `Error HolderHoldsLock` immediately instead of waiting. The same thread is `Environment.CurrentManagedThreadId`. That reject is how the pool enforces that nobody takes two locks. It prevents a holder waiting on itself.
[x] Node move and disk architecture §3 **Node lock**. core-refinement architecture §6 **Workspace lock**.
[x] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[x] `src/Server/Core/PersistThread.fs` stays as it is.
[x] `src/Server/DocumentPersistWrite.fs` stays as it is.
[x] `src/Server/ParseThread.fs` stays as it is.
[x] `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` stay as they are.
[x] `src/Server/WorkspaceGit.fs` stays as it is.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change `src/Server/NodeLockPool.fs` so an entry stores no graph, conflict checks read the graph passed with the new request, an absent node conflicts with nothing, and `NodeAbsent` is deleted. `HolderHoldsLock` stays. `threadId` stays `Environment.CurrentManagedThreadId`.
3. [x] Keep `withLock` at `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`. `Ok` is the action value. `Error HolderHoldsLock` returns before a wait. A conflict blocks. No conflict proceeds.
4. [x] Change `tests/Server.Tests/NodeLockPoolTests.fs` so the two tests in Outputs replace the two tests in Working.

## Outputs

[x] `src/Server/NodeLockPool.fs` — `withLock` compares locks on the passed graph.
[x] Distinct source files: 1. `src/Server/NodeLockPool.fs`.
[x] `tests/Server.Tests/NodeLockPoolTests.fs` — the two tests below. The other tests in that file keep their names and their waits.
[x] ``an absent node proceeds and the same thread is rejected`` — `withLock` on an id absent from `Graph.create()` runs its action. During that action, `withLock` on a File in `poolGraph` enters. `withLock` on a held File is inside its action. `withLock` on an id absent from that graph enters during the hold. The holding thread's `withLock` on the sibling File returns `Error NodeLockRejection.HolderHoldsLock` immediately. The first entry stays. The sibling action stays uncalled. A second thread's `withLock` on the held File stays outside until release.
[x] ``a request uses its own graph when the holder's graph disagrees`` — the holder's graph places the held File beside the Directory, and the requester's graph places that File under the Directory. `withLock` on the Directory stays outside until the File releases. The reverse graphs: the holder's graph places the File under the Directory, and the requester's graph does not. `withLock` on the Directory enters during the File hold.

## Stop when

[ ] Record the stop at `plan/node-move-disk/issues/12-compare-locks-on-the-live-graph/12-build.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit wires `PersistThread`, `DocumentPersistWrite`, `ParseThread`, `FileAgent`, `DbAgent`, or `WorkspaceGit`.
[ ] Stop when the edit adds a public function other than `withLock`.
[ ] Stop when the edit deletes `HolderHoldsLock`.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``an absent node proceeds and the same thread is rejected``
  - `tests/Server.Tests`: ``a request uses its own graph when the holder's graph disagrees``
[x] All executed tests pass.
[x] Write the verdict to `plan/node-move-disk/issues/12-compare-locks-on-the-live-graph/12-build.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
