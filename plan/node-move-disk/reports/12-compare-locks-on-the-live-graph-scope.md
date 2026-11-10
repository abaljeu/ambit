# 12 — Compare locks on the live graph scope

## Signatures

[ ] `NodeLockPool.withLock` in `src/Server/NodeLockPool.fs` has type `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`.
[ ] `NodeLockRejection` in `src/Server/NodeLockPool.fs` has `[<RequireQualifiedAccess>]` and the cases `HolderHoldsLock` and `NodeAbsent`.
[ ] `NodeLockPool.Entry` in `src/Server/NodeLockPool.fs` stores `seq`, `phase`, `graph`, `nodeId`, and `threadId`.
[ ] `Graph.ownerParentByChild` in `src/Shared/Model.fs` has type `Map<NodeId, NodeId>`.
[ ] `Graph.nodes` in `src/Shared/Model.fs` has type `Map<NodeId, Node>`.
[ ] `NodeKind.container` in `src/Shared/Model.fs` is true for `Special Workspace` and `Special Directory`.
[ ] `Environment.CurrentManagedThreadId` is the thread id `NodeLockPool.enqueue` stores.

## Match sites

[ ] `NodeLockPool.withLock` returns `Error NodeLockRejection.NodeAbsent` when `graph.nodes` has no entry for the requested id, and the action stays uncalled.
[ ] `NodeLockPool.enqueue` returns `Error NodeLockRejection.HolderHoldsLock` when any entry has the same `threadId`, before a second entry is queued.
[ ] `NodeLockPool.conflicts` reads `request.graph` and `taken.graph`.
[ ] `NodeLockPool.ownerChain` walks `graph.ownerParentByChild`, includes the start id, stops when `graph.nodes` lacks the current id, and a visited set stops a cycle.
[ ] `NodeLockPool.withLock` is the only public function on `NodeLockPool`. `entryCount`, `pendingCount`, and `waitUntilQueued` are `internal`.
[ ] `src/` has no caller of `NodeLockPool.withLock`. The callers are in `tests/Server.Tests/NodeLockPoolTests.fs`.

## Existing tests

[ ] ``a request waits when the graphs disagree on a held file`` in `tests/Server.Tests/NodeLockPoolTests.fs` calls `assertRequestWaits` twice, so either graph's owner chain makes the request stay outside until release.
[ ] ``a second request on a held node waits and the same thread is rejected`` in `tests/Server.Tests/NodeLockPoolTests.fs` expects `Error NodeLockRejection.HolderHoldsLock` for the nested call and `Error NodeLockRejection.NodeAbsent` for an id absent from `Graph.create()`.
[ ] ``taking a directory waits while a descendant file is held and a sibling file enters`` keeps the Directory outside while the child File is held and lets the sibling File enter.
[ ] ``taking a workspace waits while a descendant file is held and a file waits while an ancestor is held`` keeps the Workspace outside while a descendant File is held, then keeps the File outside while the Directory is held.
[ ] ``a pending directory lock runs before a new descendant file`` keeps the later File outside until the earlier Directory request releases.
[ ] ``a parse read under a locked directory waits until release`` keeps the File outside while the Directory is held, and keeps the Directory outside while the File is held.
[ ] ``a thrown wait drops the pending entry`` interrupts the waiting thread and expects that pending row to leave the pool.

## Copy-from

[ ] `NodeLockPool.enqueue` in `src/Server/NodeLockPool.fs` is the `Monitor.Wait`, `Monitor.PulseAll`, and `try`/`with` drop this change keeps.
[ ] `NodeLockPool.ownerChain` in `src/Server/NodeLockPool.fs` is the owner-chain walk this change keeps on the passed graph.
[ ] `disagreeGraphs` and `assertRequestWaits` in `tests/Server.Tests/NodeLockPoolTests.fs` are the two-graph fixture and the outside-until-release seam.

## implement

[ ] The pool stores `graph` on each entry. The ticket stores `seq`, `phase`, `nodeId`, and `threadId` only.
[ ] `NodeLockPool.conflicts` treats a chain on either graph as a conflict. The ticket reads only the graph passed with the new request.
[ ] `NodeLockPool.withLock` returns `Error NodeLockRejection.NodeAbsent` for a missing `graph.nodes` entry. The ticket runs the action and queues no entry.
[ ] `NodeLockRejection.NodeAbsent` exists. The ticket deletes that case and keeps `HolderHoldsLock`.
[ ] `NodeLockPool.ownerChain` stops when `graph.nodes` lacks the current id. The ticket walks `graph.ownerParentByChild` only.
[ ] ``an absent node proceeds and the same thread is rejected`` observes `withLock` at the public seam: an id absent from `Graph.create()` returns `Ok`, a File `withLock` enters during that action, an absent id enters during a held File, the holder thread's sibling `withLock` returns `Error NodeLockRejection.HolderHoldsLock` with the sibling action uncalled and `entryCount` still 1, and a second thread stays outside `waitUntilQueued` until release.
[ ] ``a request uses its own graph when the holder's graph disagrees`` observes `withLock` at the public seam: `fileBesideDir` held on the File and `fileUnderDir` requested on the Directory stays outside until release, and `fileUnderDir` held on the File lets `fileBesideDir` on the Directory enter while that File is held.
