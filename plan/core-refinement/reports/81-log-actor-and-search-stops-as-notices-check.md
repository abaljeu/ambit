round

## Counts

- Server.Tests: 733 passed, 0 failed, 1 skipped
- Shared.Tests: 1931 passed, 0 failed, 3 skipped

## Commands

- `scripts/agent-test.sh --filter "FullyQualifiedName~ActorStop failed without message decodes as empty" shared` round 1
- `scripts/agent-test.sh --filter "FullyQualifiedName~Notice JSON round trip" shared` round 1
- `scripts/agent-test.sh --filter "FullyQualifiedName~An old actorStop row is skipped on read" shared` round 1
- `scripts/agent-test.sh --filter "FullyQualifiedName~a stop-only poll removes the live focus and starts no catch-up" shared` round 1
- `scripts/agent-test.sh --filter "FullyQualifiedName~Ev JSON round-trips ActorStart and ActorStop" shared` round 1
- `scripts/agent-test.sh --filter "FullyQualifiedName~ActorStop removes a live Focus" shared` round 2
- `scripts/agent-test.sh --filter "FullyQualifiedName~postSearch records ActorStart at the root" server` round 1
- `scripts/agent-test.sh --full` failed Server.Tests (729 passed, 4 failed, 1 skipped). Shared.Tests did not run. `changesOnly` drops every Notice, so the stop left that list and `MailboxHistoryDurabilityTests.mixedIds` failed four facts with `expected three EventLog events`.
- `scripts/agent-test.sh --filter "FullyQualifiedName~MailboxHistoryDurabilityTests" server` round 12 after `mixedIds` kept ActorStop, Change, and ActorStart from the log
- `scripts/agent-test.sh --full` round Server.Tests (733 passed, 0 failed, 1 skipped) and Shared.Tests (1931 passed, 0 failed, 3 skipped)
