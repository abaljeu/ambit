pass

`scripts/agent-test.sh` with no arguments passed.

- Shared.Tests `EventJsonTests`, `HistoryTests`, and `MailboxLogTests`: passed 84, failed 0, skipped 0

`scripts/agent-test.sh --filter` for each test the ticket names passed.

- Shared.Tests ``An old actorStart or cancel row is skipped on read``: passed 1
- Shared.Tests ``Ev JSON round-trips ActorStart and ActorStop``: passed 1
- Shared.Tests ``cancel request JSON round-trips Focus NodeId and EventId``: passed 1
- Shared.Tests ``ActorStart adds and ActorStop removes a live Focus``: passed 2 (the filter also matches ``applyServerTail ActorStart adds and ActorStop removes a live Focus``)
- Shared.Tests ``applyServerTail ActorStart adds and ActorStop removes a live Focus``: passed 1
- Server.Tests ``CoreMailboxApi.startActor appends ActorStart to lifecycle events``: passed 1
- Server.Tests `CancelByFocusTests`: passed 6

`scripts/agent-test.sh --full` passed.

- Server.Tests: passed 733, failed 0, skipped 1
- Shared.Tests: passed 1926, failed 0, skipped 3
