# 04 — Expand the node enclosure lock

Ticket: [04 — Expand the node enclosure lock](04-expand-the-node-enclosure-lock-ticket.md)

Check: pass

- `scripts/agent-test.sh` with no arguments ran `tests/Server.Tests` filter `FullyQualifiedName~NodeEnclosureLockTests`. Passed 3, failed 0.
- `scripts/agent-test.sh --filter "FullyQualifiedName~directory lock blocks a descendant, allows a sibling, and a workspace lock blocks its file" server` passed 1.
- `scripts/agent-test.sh --filter "FullyQualifiedName~node enclosure acquire drains an in-flight persist and a parse read" server` passed 1.
- `scripts/agent-test.sh --filter "FullyQualifiedName~persist grant stores the owner chain and includes the directory node" server` passed 1.
- The build added no test the ticket leaves unnamed.
- `scripts/agent-test.sh --full` passed Server 759, skipped 1, and Shared 1937, skipped 3. Failed 0.
