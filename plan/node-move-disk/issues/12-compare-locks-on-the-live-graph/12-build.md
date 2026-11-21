# 12 — Compare locks on the live graph

Ticket: [12 — Compare locks on the live graph](12-compare-locks-on-the-live-graph-ticket.md)

Check: pass

- `scripts/agent-test.sh` with no arguments ran `tests/Server.Tests` filter `FullyQualifiedName~NodeLockPoolTests`. Passed 7, failed 0.
- `scripts/agent-test.sh --filter "FullyQualifiedName~an absent node proceeds and the same thread is rejected" server` passed 1.
- `scripts/agent-test.sh --filter "FullyQualifiedName~a request uses its own graph when the holder's graph disagrees" server` passed 1.
- `scripts/agent-test.sh --full` passed Server 763, skipped 1, and Shared 1937, skipped 3. Failed 0.
