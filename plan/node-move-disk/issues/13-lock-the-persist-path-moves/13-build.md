# 13 — Lock the persist path moves

Ticket: [13 — Lock the persist path moves](13-lock-the-persist-path-moves-ticket.md)

Check: pass

- `scripts/agent-test.sh` with no arguments ran `tests/Server.Tests` filter `FullyQualifiedName~DocumentPathMoveExecutionTests`. Passed 7, failed 0.
- `scripts/agent-test.sh --filter "FullyQualifiedName~persist path move waits while the moved file is held" server` passed 1.
- `scripts/agent-test.sh --filter "FullyQualifiedName~persist path move waits while a file under the moved directory is held" server` passed 1.
- `scripts/agent-test.sh --filter "FullyQualifiedName~a path move on a thread that already holds a lock leaves the artifact" server` passed 1.
- `scripts/agent-test.sh --filter "FullyQualifiedName~a sibling file lock lets a file path move finish" server` passed 1.
- `scripts/agent-test.sh --full` passed Server 766, skipped 1, and Shared 1937, skipped 3. Failed 0.
