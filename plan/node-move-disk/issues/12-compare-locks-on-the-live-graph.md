# 12 — Compare locks on the live graph
**Status**: defined
One job: The Server compares a new lock on the live graph passed with that request, an absent node conflicts with nothing, and a holder that already holds a pool lock gets `Error HolderHoldsLock` immediately. Alan, 2026-11-10. The rule is [Node move and disk architecture](plan/node-move-disk/arch.md) §3 **Node lock**. Story 5 Directory Node move in [Node move and disk](plan/node-move-disk/spec.md) waits on that comparison.

Type: coding
Blockers: [11 — Expand the node lock pool](11-expand-the-node-lock-pool.md)

## Inputs

### Working

#### One graph

[ ] There is one graph. The pool stores no graph.
[ ] A request is that live graph and one `NodeId`. Conflict checks read the graph passed with the new request.
[ ] An absent node conflicts with nothing. A request whose id is absent from that graph proceeds. That request queues no entry, so it blocks nothing.
[ ] `withLock` blocks while a conflict is taken. Otherwise it proceeds.
[ ] `NodeLockRejection` keeps `HolderHoldsLock`. `NodeAbsent` is deleted.
[ ] `withLock` has type `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`.
[ ] A holder that already holds a pool lock and requests another gets `Error HolderHoldsLock` immediately instead of waiting. The first entry stays. The new action stays uncalled.
[ ] The same thread is `Environment.CurrentManagedThreadId`.
[ ] That reject is how the pool enforces that nobody takes two locks. It prevents a holder waiting on itself.

#### Conflict check

[ ] An entry stores `seq`, `phase`, `nodeId`, and `threadId`. The entry stores no `graph`.
[ ] `threadId` is `Environment.CurrentManagedThreadId`.
[ ] Taken means `Pending` or `Held`.
[ ] The same id conflicts for every kind, when the requested id is present in the passed graph.
[ ] The owner chain is a walk of `graph.ownerParentByChild` on the passed graph. The walk includes the start id. A visited set stops a cycle.
[ ] A taken id is an ancestor when that walk from the requested id reaches the taken id, and the two ids differ.
[ ] A taken id is a descendant when that walk from the taken id reaches the requested id, and the two ids differ.
[ ] A Workspace or Directory request conflicts while a descendant id is taken.
[ ] A Directory or File request conflicts while an ancestor id is taken.
[ ] The request waits while any `Held` entry conflicts, and while any `Pending` entry with a smaller seq conflicts.
[ ] The pool marks the entry `Held` and runs the action. The action's return releases the entry.

#### Current module this change reworks

[ ] `NodeLockPool.withLock` in `src/Server/NodeLockPool.fs` returns `Result<'a, NodeLockRejection>`.
[ ] `NodeLockRejection` has `HolderHoldsLock` and `NodeAbsent`. This ticket deletes `NodeAbsent` and keeps `HolderHoldsLock`.
[ ] An entry stores `graph` and `threadId`. This ticket drops `graph` and keeps `threadId`.
[ ] A missing `graph.nodes` entry returns `Error NodeLockRejection.NodeAbsent`.
[ ] The same `threadId` already `Pending` or `Held` returns `Error NodeLockRejection.HolderHoldsLock`. This ticket keeps that reject.
[ ] `conflicts` walks the request graph and the taken entry's graph.

#### Tests this change reworks

[ ] ``a second request on a held node waits and the same thread is rejected`` in `tests/Server.Tests/NodeLockPoolTests.fs` expects `Error NodeLockRejection.HolderHoldsLock` and `Error NodeLockRejection.NodeAbsent`.
[ ] ``a request waits when the graphs disagree on a held file`` in `tests/Server.Tests/NodeLockPoolTests.fs` waits when either graph shows the conflict.

### Reference

[ ] Alan, 2026-11-10: there is one graph, and the pool stores no graph. Locks are compared on the live graph passed in to the request. An absent node is unlikely, and it conflicts with nothing. A holder that already holds a pool lock and requests another gets `Error HolderHoldsLock` immediately instead of waiting. The same thread is `Environment.CurrentManagedThreadId`. That reject is how the pool enforces that nobody takes two locks. It prevents a holder waiting on itself.
[ ] Node move and disk architecture §3 **Node lock**. core-refinement architecture §6 **Workspace lock**.
[ ] [Size limit](plan/context/size-limit.md): 8 source files, 4 tests.

## Do not load

[ ] `src/Server/Core/PersistThread.fs` stays as it is.
[ ] `src/Server/DocumentPersistWrite.fs` stays as it is.
[ ] `src/Server/ParseThread.fs` stays as it is.
[ ] `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` stay as they are.
[ ] `src/Server/WorkspaceGit.fs` stays as it is.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `src/Server/NodeLockPool.fs` so an entry stores no graph, conflict checks read the graph passed with the new request, an absent node conflicts with nothing, and `NodeAbsent` is deleted. `HolderHoldsLock` stays. `threadId` stays `Environment.CurrentManagedThreadId`.
3. [ ] Keep `withLock` at `Graph -> NodeId -> (unit -> 'a) -> Result<'a, NodeLockRejection>`. `Ok` is the action value. `Error HolderHoldsLock` returns before a wait. A conflict blocks. No conflict proceeds.
4. [ ] Change `tests/Server.Tests/NodeLockPoolTests.fs` so the two tests in Outputs replace the two tests in Working.

## Outputs

[ ] `src/Server/NodeLockPool.fs` — `withLock` compares locks on the passed graph.
[ ] Distinct source files: 1. `src/Server/NodeLockPool.fs`.
[ ] `tests/Server.Tests/NodeLockPoolTests.fs` — the two tests below. The other tests in that file keep their names and their waits.
[ ] ``an absent node proceeds and the same thread is rejected`` — `withLock` on an id absent from `Graph.create()` runs its action. During that action, `withLock` on a File in `poolGraph` enters. `withLock` on a held File is inside its action. `withLock` on an id absent from that graph enters during the hold. The holding thread's `withLock` on the sibling File returns `Error NodeLockRejection.HolderHoldsLock` immediately. The first entry stays. The sibling action stays uncalled. A second thread's `withLock` on the held File stays outside until release.
[ ] ``a request uses its own graph when the holder's graph disagrees`` — the holder's graph places the held File beside the Directory, and the requester's graph places that File under the Directory. `withLock` on the Directory stays outside until the File releases. The reverse graphs: the holder's graph places the File under the Directory, and the requester's graph does not. `withLock` on the Directory enters during the File hold.

## Stop when

[ ] Record the stop at `plan/node-move-disk/reports/12-compare-locks-on-the-live-graph-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit wires `PersistThread`, `DocumentPersistWrite`, `ParseThread`, `FileAgent`, `DbAgent`, or `WorkspaceGit`.
[ ] Stop when the edit adds a public function other than `withLock`.
[ ] Stop when the edit deletes `HolderHoldsLock`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``an absent node proceeds and the same thread is rejected``
  - `tests/Server.Tests`: ``a request uses its own graph when the holder's graph disagrees``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/node-move-disk/reports/12-compare-locks-on-the-live-graph-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
