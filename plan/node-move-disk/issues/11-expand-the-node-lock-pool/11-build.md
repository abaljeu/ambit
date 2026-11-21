# 11 — Expand the node lock pool

Ticket: [11 — Expand the node lock pool](11-expand-the-node-lock-pool-ticket.md)

Check: pass

- `scripts/agent-test.sh` with no arguments ran `tests/Server.Tests` filter `FullyQualifiedName~NodeLockPoolTests`. Passed 7, failed 0.
- `scripts/agent-test.sh --filter "FullyQualifiedName~taking a directory waits while a descendant file is held and a sibling file enters" server` passed 1.
- `scripts/agent-test.sh --filter "FullyQualifiedName~taking a workspace waits while a descendant file is held and a file waits while an ancestor is held" server` passed 1.
- `scripts/agent-test.sh --filter "FullyQualifiedName~a second request on a held node waits and the same thread is rejected" server` passed 1. Ticket 12 replaced that name with ``an absent node proceeds and the same thread is rejected``. An absent id now runs the action. `NodeAbsent` is gone.
- `scripts/agent-test.sh --filter "FullyQualifiedName~a pending directory lock runs before a new descendant file" server` passed 1.
- `scripts/agent-test.sh --filter "FullyQualifiedName~a parse read under a locked directory waits until release" server` passed 1.
- `scripts/agent-test.sh --filter "FullyQualifiedName~a request waits when the graphs disagree on a held file" server` passed 1. Ticket 12 replaced that name with ``a request uses its own graph when the holder's graph disagrees``. Conflict checks read only the graph passed with the request.
- `scripts/agent-test.sh --filter "FullyQualifiedName~a thrown wait drops the pending entry" server` passed 1.
- `scripts/agent-test.sh --full` passed Server 763, skipped 1, and Shared 1937, skipped 3. Failed 0.
