# 12 — Compare locks on the live graph

Ticket: [[../issues/12-compare-locks-on-the-live-graph.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- No findings.

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/agent-test.sh --full` wrote `/tmp/ambit-full-suite-20261110-165003.log`. `scripts/check-test.sh 2858` printed `DONE` and that log path. Server.Tests passed 763, failed 0, skipped 1, total 764. Shared.Tests passed 1937, failed 0, skipped 3, total 1940.
- 2. **Absent node** — ``an absent node proceeds and the same thread is rejected`` in `tests/Server.Tests/NodeLockPoolTests.fs` runs `withLock` on an id absent from `Graph.create()`, and during that action a File `withLock` on `poolGraph` enters. During a held File, an id absent from that graph enters. The holding thread's sibling File returns `Error NodeLockRejection.HolderHoldsLock`, the sibling action stays uncalled, and `entryCount` stays 1. A second thread's `withLock` on the held File stays outside until release.
- 3. **Own graph** — ``a request uses its own graph when the holder's graph disagrees`` keeps the Directory outside until the File releases when the requester's graph places that File under the Directory and the holder's graph places the File beside the Directory. The reverse graphs let the Directory enter during the File hold. `assertEntersDuringHold` calls `waitUntilQueued 2` before it reads `pendingCount`. A request that enters and leaves before that wait is registered leaves the count at 1, so the wait does not complete. This run passed. The Outputs check is `requestEntered` completing while the File release is still open.
- 4. **Directory wait** — ``taking a directory waits while a descendant file is held and a sibling file enters`` is unchanged. The Directory stays outside while the child File is held, and the sibling File enters.
- 5. **Workspace and ancestor** — ``taking a workspace waits while a descendant file is held and a file waits while an ancestor is held`` is unchanged. The Workspace stays outside while the sibling File is held, then the child File stays outside while the Directory is held.
- 6. **Pending directory** — ``a pending directory lock runs before a new descendant file`` is unchanged. The later File stays outside until the earlier Directory request releases.
- 7. **Parse read** — ``a parse read under a locked directory waits until release`` is unchanged. The File stays outside while the Directory is held, and the Directory stays outside while the File is held.
- 8. **Thrown wait** — ``a thrown wait drops the pending entry`` is unchanged. The interrupted waiter leaves the pool, and the pending count returns to zero.

## 5. Miss

- No findings.

## 6. Found elsewhere

The following issues were found beyond the ticket scope.  The fixes are suggested.

- 1. **Ticket 11 NodeAbsent record** — Accept.
  - **Problem.** `plan/node-move-disk/issues/11-expand-the-node-lock-pool.md` line 16 and line 112, and `plan/node-move-disk/review/11-expand-the-node-lock-pool.md` line 24, still describe `NodeAbsent` as the result for an id absent from `Graph.create()`.
  - **Fix.** Leave those as the record of ticket 11. A later reader should use ticket 12 for the current rejection type.
