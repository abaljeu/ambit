# 26 — Retire the posted named start
**Status**: coded
One job: The server retires the posted named start. A posted start whose name has no function start receives `Error "unknown actor"`, and `postChosenStart` and `startChosen` are gone.

[x] The source files are 1. The test files are 5. [Size limit](../../../context/size-limit.md) is 8 source files and 4 test files. This ticket's cap is 1 source file and 5 test files. The test count is over the shared limit because each posted start sits in its own file.
[x] [27 — Reject unknown Run commands before the queue](../27-reject-unknown-run-commands-before-the-queue/27-reject-unknown-run-commands-before-the-queue-ticket.md) has landed. A production Run reaches `admittedQueue` for `test`, `ai`, and a query.
[x] [25 — Remove the coreMailboxStartActor door](../25-remove-the-coremailboxstartactor-door/25-remove-the-coremailboxstartactor-door-ticket.md) keeps `dispatchStartActor` and `coreMailboxStartActor`. [16 — Delete the old Order cases](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) keeps `Order.StartActorDeprecated`. `EventBody.ActorStart` stays the client start request.

Type:

[x] coding

Blockers:

[x] none

## Inputs

### Working

[x] `postActorStart` in `src/Server/Core/CoreMailboxActors.fs` calls `postChosenStart` when `admittedQueue` returns `None`. `admittedQueue` returns a queue for the name `test`, the name `ai`, and a query (`SearchActor.isQueryRequest`). Every other posted name takes the `None` arm.
[x] `postChosenStart` calls `startChosen`. `startChosen` calls `pool.startActor`. On `Error`, `postChosenStart` calls `recordFailedStart`. `failedMessage` maps `StartError.UnknownActor` to `"unknown actor"`. `recordFailedStart` stores `EventBody.ActorStart` and `EventBody.ActorStop` with `ActorFailed`, then replies the stored start.
[x] `queueAdmittedAi` replies `reply.Reply(Error "ai actor is not installed")`. `dispatchPostEvent` replies `reply.Reply(Error "ActorStop is not a client event type")`. The refusal in this ticket uses that `reply.Reply(Error ...)` form. The words are `unknown actor`, the same words `CommandRequest.tryStart` returns for a `?` name other than `test` and `ai`.
[x] `failedMessage` and `recordFailedStart` have no caller besides `postChosenStart`. `TreatWarningsAsErrors` is true.
[x] `dispatchStartActor` calls `pool.startActor` through `dispatchActorStartResult`. `finishReadyStart` appends `EventBody.ActorStart` for that door. `placeOrder` calls `dispatchStartActor` for `Order.StartActorDeprecated`.
[x] ``ActorStart body does not run inside the mailbox turn`` in `tests/Server.Tests/OrderedEventStreamTests.fs` posts `?gate` through `CoreMailboxApi.postEvents`. The host registers `ActorName "gate"`. The name is `gate`, so the post reaches `postChosenStart`. The test holds that fixture body.
[x] ``edit then unknown ActorStart stores a failed ActorStop`` in `tests/Server.Tests/OrderedEventStreamTests.fs` posts an edit to `?nope` and an `EventBody.ActorStart` through `/ambit/events`. The name is `nope`, so the post reaches `postChosenStart`. The test expects HTTP 200, a stored `EventBody.ActorStart`, and `ActorFailed "unknown actor"`.
[x] ``edit then rejected ActorStart stores a failed ActorStop`` in `tests/Server.Tests/OrderedEventStreamTests.fs` posts an edit to `row b` and an `EventBody.ActorStart` with empty `graphIds` through `/ambit/events`. `row b` is not `test`, `ai`, or a query, so the post reaches `postChosenStart`. The stored `ActorFailed` text comes from `startChosen` (`graphIds required`).
[x] ``production events door records a failed ActorStop for an unknown actor`` in `tests/Server.Tests/TestActorCommandErrorTests.fs` posts `?unknown` and `?nope` through `/ambit/changes`. Each name reaches `postChosenStart`. The test expects HTTP 200 and `ActorFailed "unknown actor"`.
[x] ``Undo and Redo post as orders; ActorStart and Cancel still post as PostEvent`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` posts `sampleRequest` through `CoreMailboxApi.postEvents`. `runDefinitionId` is `Graph.rootId` and the node text is `ROOT`, so the post reaches `postChosenStart`. The host registers `ActorName "root"`. The same test posts Undo, Redo, and Cancel.
[x] ``postEvent returns the stored start when that start stops at once`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` posts that same root `sampleRequest` through `CoreMailboxApi.postEvent`. The first post reaches `postChosenStart`. The second post of the same submission hits `storedBySubmission` in `postActorStart` and returns the stored event.
[x] ``query start error posts no rows, fails the reply, and keeps the edit`` in `tests/Server.Tests/QueryActorTests.fs` posts an edit to `?gate` and an `EventBody.ActorStart` through `CoreMailboxApi.postEvents`. The name is `gate`, so the post reaches `postChosenStart`. The host registers `ActorName "gate"` and holds the body. A later query post expects `actor node already has a live Actor`.
[x] ``postCommand through CoreMailbox encodes ActorStart in latestId events`` and ``postCommand startActor can produce hello child`` in `tests/Server.Tests/ApiPostCommandTests.fs` post `EventBody.ActorStart` for a node whose text is `hello` and whose class is `actor-test`. `actorNameFromText` returns `None` for `hello`, and the line is not a query, so each post reaches `postChosenStart`. The pool then reads the `actor-test` class.
[x] ``edit then ActorStart classifies the line after the edit`` and ``events door applies an edit then ActorStart as one list`` post `?test`. ``equals-line ActorStart does not run the query function`` posts a query line. Those three stay on `admittedQueue`. ``unregistered Actor name fails start`` calls `coreMailboxStartActor`.

### Reference

[x] `CommandRequest.tryStart` in `src/Shared/CommandRequest.fs`. `TestActor.functionActorAt` in `src/Server/TestActor.fs`. `RunAgentActor.actorFn` and `RunAgentActor.aiActorOrder` in `src/Server/RunAgentActor.fs`. `SearchActor.functionActorAt` in `src/Server/SearchActor.fs`.

## Do not load

[x] `dispatchStartActor`, `finishReadyStart`, `dispatchActorStartResult`, and `CoreMailboxApi.coreMailboxStartActor`. 25 — Remove the coreMailboxStartActor door owns that door.
[x] `Order.StartActorDeprecated`. 16 — Delete the old Order cases owns that case.
[x] `EventBody.ActorStart` as the client start request.
[x] The `admittedQueue` arms for `test`, `ai`, and a query.
[x] A test whose start reaches the server through `coreMailboxStartActor`.
[x] ``edit then ActorStart classifies the line after the edit``, ``events door applies an edit then ActorStart as one list``, ``equals-line ActorStart does not run the query function``, and ``unregistered Actor name fails start``.
[x] ``production boot Run of ?test hello posts Owned hello child``. That test already posts `?test hello` through `postActorStart`.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Delete `postChosenStart` and `startChosen` in `src/Server/Core/CoreMailboxActors.fs`. Delete `failedMessage` and `recordFailedStart` in that file. They have no remaining caller.
3. [x] Change the `None` arm of `postActorStart`. Reply `reply.Reply(Error "unknown actor")`. Leave the `admittedQueue` arms for `test`, `ai`, and a query.
4. [x] Delete ``ActorStart body does not run inside the mailbox turn``. It only holds the `gate` fixture.
5. [x] Move ``edit then unknown ActorStart stores a failed ActorStop`` onto the new refusal. Rename it ``edit then unknown ActorStart replies unknown actor``. The response is HTTP 400 and the error is `unknown actor`. The log has no `EventBody.ActorStart` and no `EventBody.ActorStop` for that line.
6. [x] Move ``edit then rejected ActorStart stores a failed ActorStop`` onto the new refusal. Rename it ``edit then rejected ActorStart replies unknown actor``. The line `row b` has no function start, so the reply is the same HTTP 400 `unknown actor`. The `graphIds required` text leaves with `startChosen`.
7. [x] Move ``production events door records a failed ActorStop for an unknown actor`` onto the new refusal. Rename it ``production events door replies unknown actor``. Keep the `?unknown` and `?nope` rows. Each response is HTTP 400 and the error is `unknown actor`. The log has no `EventBody.ActorStart` and no `EventBody.ActorStop` for that line.
8. [x] Delete the posted root start from ``Undo and Redo post as orders; ActorStart and Cancel still post as PostEvent``, and delete the assertion that the stored body is `EventBody.ActorStart`. Rename the test ``Undo and Redo post as orders; Cancel posts as PostEvent``. The Undo, Redo, and Cancel posts stay.
9. [x] Move ``postEvent returns the stored start when that start stops at once`` onto `?test hello` through `TestActor.functionActorAt`. The first post stores `Order.StartActor` with actor name `test`. The second post of the same submission returns that stored event. Those two assertions are the behavior that stays.
10. [x] Move ``query start error posts no rows, fails the reply, and keeps the edit`` onto the new refusal. Rename it ``posted gate start replies unknown actor``. The posted `?gate` start replies `Error "unknown actor"`. The log has no `EventBody.ActorStart` and no `EventBody.ActorStop`. The later query post leaves with the `gate` fixture. `TestActor.functionActorAt`, `RunAgentActor.actorFn`, and `SearchActor.functionActorAt` each stop when the body finishes, so none of them holds the node for that later query.
11. [x] Delete ``postCommand through CoreMailbox encodes ActorStart in latestId events`` and ``postCommand startActor can produce hello child``. They only exercise the `actor-test` class suffix. ``production boot Run of ?test hello posts Owned hello child`` already covers the hello child on the function start.
12. [x] Search `src/` and `tests/` for `postChosenStart` and for `startChosen`. Each search is empty.

## Outputs

[x] `src/Server/Core/CoreMailboxActors.fs`
[x] `tests/Server.Tests/OrderedEventStreamTests.fs`
[x] ``ActorStart body does not run inside the mailbox turn`` — delete
[x] ``edit then unknown ActorStart stores a failed ActorStop`` — move to ``edit then unknown ActorStart replies unknown actor``
[x] ``edit then rejected ActorStart stores a failed ActorStop`` — move to ``edit then rejected ActorStart replies unknown actor``
[x] `tests/Server.Tests/TestActorCommandErrorTests.fs`
[x] ``production events door records a failed ActorStop for an unknown actor`` — move to ``production events door replies unknown actor``
[x] `tests/Server.Tests/CoreMailboxDoorTests.fs`
[x] ``Undo and Redo post as orders; ActorStart and Cancel still post as PostEvent`` — delete the posted root start, and rename the test ``Undo and Redo post as orders; Cancel posts as PostEvent``
[x] ``postEvent returns the stored start when that start stops at once`` — move onto `?test hello` and `TestActor.functionActorAt`
[x] `tests/Server.Tests/QueryActorTests.fs`
[x] ``query start error posts no rows, fails the reply, and keeps the edit`` — move to ``posted gate start replies unknown actor``
[x] `tests/Server.Tests/ApiPostCommandTests.fs`
[x] ``postCommand through CoreMailbox encodes ActorStart in latestId events`` — delete
[x] ``postCommand startActor can produce hello child`` — delete

## Stop when

[x] Rewrite `26-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit needs a file outside Outputs.
[ ] Stop when the edit deletes `dispatchStartActor`, `coreMailboxStartActor`, `Order.StartActorDeprecated`, or `EventBody.ActorStart`.
[ ] Stop when the edit changes a test that reaches the server through `coreMailboxStartActor`.
[ ] Stop when the edit goes over 1 source file or 5 test files.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``edit then unknown ActorStart replies unknown actor``
  - `tests/Server.Tests`: ``edit then rejected ActorStart replies unknown actor``
  - `tests/Server.Tests`: ``production events door replies unknown actor``
  - `tests/Server.Tests`: ``Undo and Redo post as orders; Cancel posts as PostEvent``
  - `tests/Server.Tests`: ``postEvent returns the stored start when that start stops at once``
  - `tests/Server.Tests`: ``posted gate start replies unknown actor``
  - `tests/Server.Tests`: `ApiPostCommandTests`
[x] All executed tests pass.
[x] Rewrite `26-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
