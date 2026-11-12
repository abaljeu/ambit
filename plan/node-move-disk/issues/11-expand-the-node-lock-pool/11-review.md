# 11 — Expand the node lock pool

Ticket: [[11-expand-the-node-lock-pool-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

Check: pass

## 1. Standards

- No findings.

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/agent-test.sh --full` wrote `/tmp/ambit-full-suite-20261110-150030.log`. `scripts/check-test.sh 3137` printed `DONE` and that log path. Server.Tests passed 763, failed 0, skipped 1, total 764. Shared.Tests passed 1937, failed 0, skipped 3, total 1940.
- 2. **Directory wait** — ``taking a directory waits while a descendant file is held and a sibling file enters`` in `tests/Server.Tests/NodeLockPoolTests.fs` holds the child File, keeps the Directory outside, lets the sibling File enter, then lets the Directory enter after release.
- 3. **Workspace and ancestor** — ``taking a workspace waits while a descendant file is held and a file waits while an ancestor is held`` keeps the Workspace outside while the sibling File is held, then keeps the child File outside while the Directory is held.
- 4. **Same node and rejection** — ``a second request on a held node waits and the same thread is rejected`` keeps a second thread outside until release, returns `Error NodeLockRejection.HolderHoldsLock` for the holding thread, returns `Error NodeLockRejection.NodeAbsent` for an id absent from `Graph.create()`, and lets the waiting thread enter after the holder raises.
- 5. **Pending directory** — ``a pending directory lock runs before a new descendant file`` waits on `NodeLockPool.waitUntilQueued`. The Directory stays outside while the first File is held. The other File under that Directory stays outside. The sibling File enters. After the held File releases, the Directory enters and the other File stays outside. After the Directory releases, the other File enters.
- 6. **Parse read** — ``a parse read under a locked directory waits until release`` keeps `withLock` on the child File outside while the Directory is held, lets the sibling File enter, then after release holds the File and keeps the Directory outside until that File releases.
- 7. **Disagreeing graphs** — ``a request waits when the graphs disagree on a held file`` keeps a Directory request pending when the requester's graph places the held File under that Directory and the holder's graph does not, and keeps a File request pending when the holder's graph places that File under the held Directory and the requester's graph does not. Each request enters after release.
- 8. **Thrown wait** — ``a thrown wait drops the pending entry`` interrupts the waiting thread after `waitUntilQueued 2`. The table then holds one entry and zero pending entries. After the holder releases, the table is empty.

## 5. Miss

- No findings.

## 6. Found elsewhere

The following issues were found beyond the ticket scope.  The fixes are suggested.

- 1. **Move write gate** — Accept.
  - **Problem.** Node move and disk architecture §3 **Move write gate** still describes two grants: a node enclosure lock and a per-member persist lock. §4 flow 6 **Move under the node lock** still says the relocate caller holds the moved File Node's per-member persist lock.
  - **Fix.** Rewrite **Move write gate** so one `withLock` request is the node lock. Pending-then-drain stays. Retarget the flow 6 checkbox at that same request.
- 2. **Core locking model** — Accept.
  - **Problem.** core-refinement architecture §6 **Workspace lock** and **Per-member persist locks** still names two kinds.
  - **Fix.** Point §6 at `NodeLockPool.withLock` when that architecture page is next edited.
- 3. **Later tickets** — Accept. 06 — Keep a nested node whose derived path differs stays as written.
  - **Problem.** These tickets still name the enclosure lock or two lock kinds.
    - 05 — Relocate in the same workspace names `NodeEnclosureLock`.
    - 07 — Move an owned node across workspaces names `NodeEnclosureLock`.
    - 08 — Amend a change conflict before the graph is stored names `NodeEnclosureLock`.
    - 09 — Relocate under the node enclosure lock calls `withNodeLock`, `withPersist`, and `withParseRead`.
    - 10 — Reject an unparsed persist request names `NodeEnclosureLock`.
    - 31 — Migrate onto the workspace lock names per-member persist locks and a separate parse-read drain.
    - 32 — Workspace lock handoff names git pull plus marking Unparsed under the workspace lock.
    - 33 — Contract the work-tree gate is superseded by 31 — Migrate onto the workspace lock. The gate removal is that migrate.
  - **Fix.** Retarget each listed ticket at `withLock` before it is built.
