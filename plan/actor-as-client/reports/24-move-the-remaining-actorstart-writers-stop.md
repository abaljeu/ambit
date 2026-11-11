# 24 — Move the remaining ActorStart writers onto StartActor

Ticket: [24 — Move the remaining ActorStart writers onto StartActor](../issues/24-move-the-remaining-actorstart-writers/24-move-the-remaining-actorstart-writers-ticket.md)

## Stop

[ ] `queueStart` stays on `EventBody.ActorStart`.
[ ] `EventBody.ActorStart` still has readers outside this ticket's outputs and outside the files [22 — Convert CoreMsg.PostEvent](../issues/22-convert-coremsg-postevent/22-convert-coremsg-postevent-ticket.md) and [25 — Remove the coreMailboxStartActor door](../issues/25-remove-the-coremailboxstartactor-door/25-remove-the-coremailboxstartactor-door-ticket.md) own.
[ ] A green suite for the refusal change needs six more test files. The size limit is 4 test files. This change edits `tests/Server.Tests/CoreMailboxDoorTests.fs` only.

## queueStart

[ ] `queueStart` in `src/Shared/RunLaunch.fs` runs on the client through `queueRun` in `src/Client/Commands.fs`.
[ ] `queueRun` holds an `ActorStart` from `CommandRequest`.
[ ] `Order.StartActor` is an `ActorOrder`.
[ ] `ActorOrder.actor` is an `ActorFunction` (`FunctionRun -> ActorChanges -> Async<unit>` in `src/Shared/History.fs`).
[ ] The functions that run test, query, and ai are `TestActor.functionActor`, `SearchActor.functionActorAt`, and `RunAgentActor.aiActorOrder` in `src/Server`.
[ ] `src/Shared` and `src/Client` have no `ActorFunction` for a Run.
[ ] A function built in the client is dropped on the wire. `EventJson.decodeActorOrder` fills `actor` with `EventJson.stubActor`. `scheduleActorOrder` would run that stub.

### Options

[ ] Leave `queueStart` on `EventBody.ActorStart`. 22 — Convert CoreMsg.PostEvent owns the client post of that body.
[ ] Put the `ActorStart` id bag on `ActorOrder` and let the server fill `actor`. That changes `ActorOrder`. This ticket forbids a new `Order` case, and the stop rule forbids a new type.
[ ] Move `queueStart` onto `Order.StartActor` after the client holds a real `ActorFunction`.

Which option should `queueStart` take?

## Search hits that remain

### Source

[ ] `src/Shared/ActorLive.fs` matches `EventBody.ActorStart` in `applyEvent` and `resultOf`.
[ ] `src/Server/Core/CoreEventDispatch.fs` matches `EventBody.ActorStart` in `completeOther`.
[ ] `src/Shared/RunLaunch.fs` builds `EventBody.ActorStart` in `queueStart`. This file is an output. The `queueStart` stop leaves that call.
[ ] `src/Server/Core/CoreMailboxActors.fs` matches `EventBody.ActorStart` in `dispatchPostEvent`. 22 — Convert CoreMsg.PostEvent owns that arm.
[ ] `EventBody.ActorStart` and `(|ActorStart|_|)` stay in `src/Shared/History.fs` because those callers remain.

### Tests outside the owned files

[ ] `tests/Shared.Tests/SyncLogicTests.fs`
[ ] `tests/Shared.Tests/EventJsonTests.fs`
[ ] `tests/Shared.Tests/EventTests.fs`
[ ] `tests/Shared.Tests/ActorLiveTests.fs`
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs`
[ ] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
[ ] `tests/Server.Tests/OrderedEventStreamTests.fs`
[ ] `tests/Server.Tests/PersistHandlersRestoreTests.fs`
[ ] `tests/Server.Tests/QueryActorTests.fs`
[ ] `tests/Server.Tests/ActorOrderFromStartTests.fs`
[ ] `tests/Server.Tests/ApiPostCommandTests.fs`

## Logged order

[ ] `finishReadyStart` posts `Order.StartActor` after `pool.startActor` returns a secret.
[ ] That pool path stores a bag `ActorFn`. `ActorOrder.actor` is an `ActorFunction`.
[ ] The logged `ActorOrder.actor` is `EventJson.stubActor`, the same stand-in the `actorOrder` codec stores.
[ ] The pool has already admitted and scheduled the bag actor before that append.
[ ] `recordFailedStart` replies the error and appends nothing.

## Suite files that still expect the old log

[ ] `tests/Server.Tests/Issue41CoreMailboxTests.fs`
[ ] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
[ ] `tests/Server.Tests/PersistHandlersRestoreTests.fs`
[ ] `tests/Server.Tests/ApiPostCommandTests.fs`
[ ] `tests/Server.Tests/OrderedEventStreamTests.fs`
[ ] `tests/Server.Tests/TestActorCommandErrorTests.fs`
