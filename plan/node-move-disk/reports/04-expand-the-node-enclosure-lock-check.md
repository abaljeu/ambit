pass

## Check

[x] `scripts/agent-test.sh` with no arguments ran `tests/Server.Tests` filter `FullyQualifiedName~NodeEnclosureLockTests`. Passed 3, failed 0.
[x] `scripts/agent-test.sh --filter "FullyQualifiedName~directory lock blocks a descendant, allows a sibling, and a workspace lock blocks its file" server` passed 1.
[x] `scripts/agent-test.sh --filter "FullyQualifiedName~node enclosure acquire drains an in-flight persist and a parse read" server` passed 1.
[x] `scripts/agent-test.sh --filter "FullyQualifiedName~persist grant stores the owner chain and includes the directory node" server` passed 1.
[x] The build added no test the ticket leaves unnamed.
[x] `scripts/agent-test.sh --full` passed Server 759, skipped 1, and Shared 1937, skipped 3. Failed 0.
