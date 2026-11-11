# 24 — Move the remaining ActorStart writers onto StartActor
**Status**: coded
One job: `queueStart` queues the client `ActorStart`. `postActorStart` converts a test, query, or ai request into `Order.StartActor` of `ActorOrder` with that server `ActorFunction`, and `replyActorOrder` posts the order only after the pool affirms. A refused start posts nothing.

[ ] [16 — Delete the old Order cases](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) depends on this ticket. 16 deletes the case once nothing here writes or reads it.
[ ] [22 — Convert CoreMsg.PostEvent](../22-convert-coremsg-postevent/22-convert-coremsg-postevent-ticket.md) covers the `dispatchPostEvent` arm. This ticket leaves that arm to 22.

Type:

[x] coding

Blockers:

[ ] None.

## Inputs

### Working

[ ] `EventBody.ActorStart` and `(|ActorStart|_|)` in `src/Shared/History.fs` build and match `EventBody.Order(Order.StartActorDeprecated request)`.
[ ] `dispatchStartActor` in `src/Server/Core/CoreMailboxActors.fs` appends `EventBody.ActorStart request` through `CoreEventDispatch.appendLifecycle`. `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` calls it for `Order.StartActorDeprecated`.
[ ] `recordFailedStart` in `src/Server/Core/CoreMailboxActors.fs` appends `EventBody.ActorStart request` and then `EventBody.ActorStop(request.focusId, ActorFailed message)`. `postChosenStart` calls it.
[ ] `expandStored` in `src/Server/Core/CoreMailboxApi.fs` reads the old case and never writes it. When a stored `EventBody.ActorStart` is followed by the `EventBody.ActorStop` for the same focus, it returns both events. `storedEvents` is its only caller.
[ ] `postEvent` in `src/Server/Core/CoreMailboxApi.fs` matches `Ok [ stored ]` on `storedEvents log [ eventId ]`. When `expandStored` returns a start and its stop, `postEvent` returns `Error "stored Event not found"`.
[ ] `queueStart` in `src/Shared/RunLaunch.fs` queues `posted "Exec" (EventBody.ActorStart request)`. `queueRun` in `src/Client/Commands.fs` calls it, and `tests/Shared.Tests/RunEditCommitTests.fs` calls it.

### Reference

[ ] [Arch](../../arch.md).
[ ] `Order.StartActor` of `ActorOrder` in `src/Shared/History.fs`. `ActorOrder` carries `actor: ActorFunction`, `actorNodeId`, and `actorName`.

## Do not load

[ ] `dispatchPostEvent`. 22 — Convert CoreMsg.PostEvent owns it.
[ ] `CoreMailboxApi.coreMailboxStartActor`. 25 — Remove the coreMailboxStartActor door owns it.
[ ] `Order.StartActorDeprecated` itself, its codec, and its log arm. 16 — Delete the old Order cases owns them.
[ ] A new `Order` case or a new `CoreMsg` case.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] `dispatchStartActor` and `recordFailedStart` post `Order.StartActor` of `ActorOrder`, after the pool affirms the start. A refused start posts no `Order.StartActor`.
3. [x] `expandStored` pairs `Order.StartActor` with its stop. It does not match `EventBody.ActorStart`.
4. [x] `postEvent` returns the stored start when `storedEvents` returns that start with its stop. It does not return `Error "stored Event not found"` for that pair.
5. [x] `queueStart` queues the client `ActorStart`. `postActorStart` converts test, query, and ai into `Order.StartActor` of `ActorOrder` and fills `actor` from `TestActor.functionActorAt`, `SearchActor.functionActorAt`, or `RunAgentActor.aiActorOrder`. `MailboxHost.postActorOrder` places that order. `replyActorOrder` posts it only after the pool affirms.
6. [x] `EventBody.ActorStart` and `(|ActorStart|_|)` stay in `src/Shared/History.fs`. `queueStart` builds the client request. `dispatchPostEvent` matches it. `Order.StartActorDeprecated` still uses the case.
7. [x] Search `src/` and `tests/` for `EventBody.ActorStart`. Hits stay for the client request, for `Order.StartActorDeprecated`, and in the files 22 or 25 owns.

## Outputs

[ ] `src/Shared/History.fs`
[ ] `src/Shared/RunLaunch.fs`
[ ] `src/Client/Commands.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `tests/Shared.Tests/RunEditCommitTests.fs`
[x] `tests/Server.Tests/CoreMailboxDoorTests.fs`: a test where `postEvent` posts a start that stops at once and returns the stored start.
[x] `tests/Server.Tests/Issue41CoreMailboxTests.fs`
[x] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
[x] `tests/Server.Tests/PersistHandlersRestoreTests.fs`
[x] `tests/Server.Tests/ApiPostCommandTests.fs`
[x] `tests/Server.Tests/OrderedEventStreamTests.fs`
[x] `tests/Server.Tests/TestActorCommandErrorTests.fs`

## Stop when

[x] Record a resolved stop at `plan/actor-as-client/reports/24-move-the-remaining-actorstart-writers-stop.md` when the client request stays `ActorStart` and the server fills the `ActorFunction`.
[ ] Stop when a search hit for `EventBody.ActorStart` is a new writer outside `queueStart`, `dispatchPostEvent`, and `Order.StartActorDeprecated`.
[ ] Stop when the edit deletes `Order.StartActorDeprecated`.
[ ] Stop when the edit posts `Order.StartActor` before the pool affirms.
[ ] Stop when the change passes 8 source files. Alan lifted the 4 test-file cap for the six Server files named in Outputs.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: `RunEditCommitTests`
  - `tests/Server.Tests`: `CoreMailboxDoorTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/24-move-the-remaining-actorstart-writers-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
