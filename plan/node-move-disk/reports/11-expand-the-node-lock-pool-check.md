pass

## Check

[x] `scripts/agent-test.sh` with no arguments ran `tests/Server.Tests` filter `FullyQualifiedName~NodeLockPoolTests`. Passed 4, failed 0.
[x] `scripts/agent-test.sh --filter "FullyQualifiedName~taking a directory waits while a descendant file is held and a sibling file enters" server` passed 1.
[x] `scripts/agent-test.sh --filter "FullyQualifiedName~taking a workspace waits while a descendant file is held and a file waits while an ancestor is held" server` passed 1.
[x] `scripts/agent-test.sh --filter "FullyQualifiedName~a second request on a held node waits and the same thread is rejected" server` passed 1.
[x] `scripts/agent-test.sh --filter "FullyQualifiedName~a pending directory lock runs before a new descendant file" server` passed 1.
[x] The build added no test the ticket leaves unnamed.
[x] `scripts/agent-test.sh --full` passed Server 760, skipped 1, and Shared 1937, skipped 3. Failed 0.
