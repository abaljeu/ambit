fail: the full Server suite has 11 failures in six test files that still expect `EventBody.ActorStart` or a stored `ActorFailed` on a refused start. The size limit is 4 test files, and this change edits `tests/Server.Tests/CoreMailboxDoorTests.fs` only.

## Check

Ticket: [24 — Move the remaining ActorStart writers onto StartActor](../issues/24-move-the-remaining-actorstart-writers/24-move-the-remaining-actorstart-writers-ticket.md)

[x] `scripts/agent-test.sh` with no arguments passed 39 tests. The filter was `CoreMailboxDoorTests`. Failed 0. Skipped 0.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~RunEditCommitTests shared` passed 8 tests. Failed 0. Skipped 0.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~CoreMailboxDoorTests server` passed 39 tests. Failed 0. Skipped 0.

[ ] `AMBIT_FULL_SUITE=1` on `tests/Server.Tests` passed 767, failed 11, skipped 1. The command also passed `-p:WarningsNotAsErrors=FS3511`.

[x] `AMBIT_FULL_SUITE=1` on `tests/Shared.Tests` passed 1936, failed 0, skipped 3.

## Server failures

[ ] `PersistHandlersRestoreTests.ActorStart persists across File restart`
[ ] `MailboxHistoryDurabilityTests.File mixed hello survives dispose create`
[ ] `MailboxHistoryDurabilityTests.Db mixed hello survives dispose create`
[ ] `MailboxHistoryDurabilityTests.getEventId is ActorStop after in-process hello`
[ ] `MailboxHistoryDurabilityTests.Undo after restart refuses a target from before the restart`
[ ] `Issue41CoreMailboxTests.mailbox appends ActorStart and ActorStop in lifecycle order`
[ ] `OrderedEventStreamTests.edit then unknown ActorStart stores a failed ActorStop`
[ ] `OrderedEventStreamTests.edit then rejected ActorStart stores a failed ActorStop`
[ ] `ApiPostCommandTests.postCommand through CoreMailbox encodes ActorStart in latestId events`
[ ] `TestActorCommandErrorTests.production events door records a failed ActorStop for an unknown actor` for `?unknown` and for `?nope`
