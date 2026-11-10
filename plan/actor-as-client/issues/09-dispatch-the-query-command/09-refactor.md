# 09 — Start the query command

Ticket: [[09-dispatch-the-query-command-ticket.md]]

Arch: [[plan/actor-as-client/arch.md]]

## Stage

Stage is refactor in [09 — Start the query command](CONTEXT.md).

## 1. Query body

1. **queryActorBody.** `queryActorBody` in `src/Server/SearchActor.fs` builds `ActorQuery` from `queryIds`, then calls the supplied stop.
2. **functionStart.** `functionStart` stops through `CoreDoor.actorStop` on the pool door.
3. **functionActorAt.** `functionActorAt` stops through `ActorChanges.actorStop`.

## 2. Focus order

1. **focusIdOf.** `focusIdOf` in `src/Server/ActorOrderFromStart.fs` returns `ActorStart.focusId`.
2. **queueAdmittedFocus.** `queueAdmittedTest` and `queueAdmittedQuery` in `src/Server/Core/CoreMailboxActors.fs` call `queueAdmittedFocus`. The actor node id is `focusIdOf`.
3. **admittedQueue.** `admittedQueue` chooses `test`, `ai`, or a query Run. `postActorStart` calls `whenAdmitted` for that queue.
4. **recordSearchStart.** `recordSearchStart` in `src/Server/Core/CoreMailboxApi.fs` places the search order through `coreMailboxStartActorFromFunction`. The actor name is `search`. The actor node id is `focusIdOf`.
5. **aiActorOrder.** `aiActorOrder` in `src/Server/RunAgentActor.fs` uses `focusIdOf`.

## 3. Start fact

1. **hasStartActor.** `hasStartActor` in `tests/Server.Tests/TestBackend.fs` matches `Order.StartActor` on actor name and actor node id.
2. **Equals line.** ``equals-line ActorStart does not run the query function`` in `tests/Server.Tests/QueryActorTests.fs` calls `hasStartActor` for `query`.
3. **Live actor.** ``query start error stores ActorStart and ActorFailed and keeps the edit`` calls `hasStartActor` for `query`.
4. **Find and Move.** ``postSearch records ActorStart at the root`` in `tests/Server.Tests/SearchActorTests.fs` calls `hasStartActor` for `search`.
5. **Selector fact.** ``orderFromStart sets actorNodeId from the selector and the actor name`` in `tests/Server.Tests/ActorOrderFromStartTests.fs` passes `focusIdOf`.

## 4. Suite

1. **Full suite.** `scripts/agent-test.sh --full` passed. Server: 763 passed, 1 skipped, 0 failed. Shared: 1937 passed, 3 skipped, 0 failed.

## 5. Scan

1. **Standards scan.** `python3 scripts/standards-scan.py --diff origin/staging` exits 0. The printout is the measure list.
2. **focusIdOf.** 3 lines (17–19).
3. **newPool.** 38 lines (379–416) in `src/Server/Core/CoreActorPool.fs`.
4. **createWithInstall.** 2 lines (419–420).
5. **queueAdmittedFocus.** 16 lines (219–234).
6. **queueAdmittedTest.** 3 lines (236–238).
7. **queueAdmittedQuery.** 3 lines (240–242).
8. **admittedQueue.** 16 lines (291–306).
9. **queryActorBody.** 11 lines (70–80).
10. **functionActorAt.** 10 lines (100–109).
11. **searchActorAt.** 6 lines (111–116).

## 6. Still open

1. **Transport start.** `start` in `src/Server/GithubTransportActor.fs` builds `FunctionActorPlacement` with a focus-id lambda and maps the placed `EventId` to unit.
2. **Equals-line name.** The review judgement Mysterious Name stays on ``equals-line ActorStart does not run the query function``. The failure text is `ActorQuery missing`.
3. **Refusal name.** The review judgement Mysterious Name stays on ``query start error stores ActorStart and ActorFailed and keeps the edit``. The failure text is `expected the live-actor refusal`.
