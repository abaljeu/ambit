# 24 — Move the remaining ActorStart writers onto StartActor

Check: pass

- `tests/Server.Tests` passed 778, failed 0, skipped 1.
- `tests/Shared.Tests` passed 1936, failed 0, skipped 3.
- `scripts/agent-test.sh` ran `CoreMailboxDoorTests` and passed 39, failed 0, skipped 0.
- `RunEditCommitTests` passed 8, failed 0, skipped 0.
- `CoreMailboxDoorTests` passed 39, failed 0, skipped 0.
- `queueStart` queues the client `ActorStart`.
- `postActorStart` converts test, query, and ai into `Order.StartActor` and fills the server `ActorFunction`.
- The client does not build an `ActorOrder`.
