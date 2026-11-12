# 27 — Log StartActor on the door and named starts
**Status**: defined
One job: The Server logs an affirmed door start and an affirmed named start as `Order.StartActor` of `ActorOrder`. A refused start posts no event, and the caller receives the failure. Place actor order in [Arch](../../arch.md) posts `Order.StartActor` only after the pool affirms.

[ ] [26 — Move the id-record starts onto function starts](../26-move-the-id-record-starts-onto-function-starts/26-move-the-id-record-starts-onto-function-starts-ticket.md) depends on this ticket.
[ ] This ticket has no dependency on 26 — Move the id-record starts onto function starts.
[ ] 26 edits `finishReadyStart` after an affirmed start is logged as `Order.StartActor`.
[ ] 26 stops for the names `root`, `gate`, `probe`, `load`, and `nope`.
[ ] 26 adds no `ActorFunction` for `dispatchStartActor` or `postChosenStart`.

Type:

[ ] coding

Blockers:

[ ] None.

## Inputs

### Working

[ ] `dispatchStartActor` in `src/Server/Core/CoreMailboxActors.fs` calls `pool.startActor` with the `ActorStart`. `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` calls `dispatchStartActor` for `Order.StartActorDeprecated`.
[ ] `postChosenStart` in `src/Server/Core/CoreMailboxActors.fs` calls `startChosen`. `startChosen` calls `pool.startActor` with the `ActorStart`. The comment on `startChosen` says `startFunction` stays unused on this path.
[ ] `postActorStart` calls `postChosenStart` when the posted name is not `test`, `ai`, or a query. `dispatchPostEvent` calls `postActorStart` for a client `EventBody.ActorStart`.
[ ] `finishReadyStart` appends `EventBody.ActorStart` through `CoreEventDispatch.appendLifecycle` when `prepareActorStart` returns a secret. `dispatchStartActor` and `postChosenStart` call `finishReadyStart` on that result.
[ ] `prepareActorStart` returns `Error` when the pool refuses the start. `dispatchActorStartResult` replies with that error and posts no event.
[ ] `recordFailedStart` appends `EventBody.ActorStart` and then `EventBody.ActorStop(request.focusId, ActorFailed message)`. `postChosenStart` calls `recordFailedStart` when `prepareActorStart` returns an error.
[ ] `expandStored` in `src/Server/Core/CoreMailboxApi.fs` returns a stored `EventBody.ActorStart` and the next `EventBody.ActorStop` for the same focus. `affirmedStart` returns the stored start for that pair. `storedEvents` calls `expandStored`. `postEvent` returns `affirmedStart`.
[ ] `ActorOrder` in `src/Shared/History.fs` has `actor: ActorFunction`. `ActorFunction` is `FunctionRun -> ActorChanges -> Async<unit>`. `dispatchStartActor` and `postChosenStart` build no value of that type.
[ ] `pool.startActor` selects an `ActorFn` from the id bag. `ActorOrder.actor` is an `ActorFunction`.

### Reference

[ ] `Order.StartActor` of `ActorOrder` in `src/Shared/History.fs`.

## Do not load

[ ] The `test`, `ai`, and query arms of `postActorStart`. Those arms call `postActorOrder`.
[ ] `dispatchPostEvent`. [22 — Convert CoreMsg.PostEvent](../22-convert-coremsg-postevent/22-convert-coremsg-postevent-ticket.md) owns that function.
[ ] `CoreMailboxApi.coreMailboxStartActor`. [25 — Remove the coreMailboxStartActor door](../25-remove-the-coremailboxstartactor-door/25-remove-the-coremailboxstartactor-door-ticket.md) owns that door.
[ ] `Order.StartActorDeprecated`, its codec, and its log arm. [16 — Delete the old Order cases](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) owns them.
[ ] `queueStart` in `src/Shared/RunLaunch.fs`. [24 — Move the remaining ActorStart writers onto StartActor](../24-move-the-remaining-actorstart-writers/24-move-the-remaining-actorstart-writers-ticket.md) owns that caller.
[ ] A new `Order` case.
[ ] A session id on `FunctionRun`, `ActorChanges`, or `ActorOrder`.
[ ] An `ActorFunction` created in this ticket.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Stop and ask. `dispatchStartActor` and `postChosenStart` have no `ActorFunction`. `Order.StartActor` of `ActorOrder` needs one. This ticket adds no `ActorFunction`.
3. [ ] Stop and ask. Outputs names 7 test files. The size limit is 4 test files.
4. [ ] Change `finishReadyStart` so the affirmed append is `Order.StartActor` of `ActorOrder`. The pool affirms before that append.
5. [ ] Change `dispatchStartActor` and `postChosenStart` so both use that append.
6. [ ] Change `recordFailedStart` so a refused start posts no event. The reply to the caller is the failure.
7. [ ] Change `expandStored` so it pairs `Order.StartActor` with its stop.
8. [ ] Change `affirmedStart` so that pair returns the stored start.

## Outputs

[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` `--filter` ``CoreMailboxApi.startActor appends ActorStart to lifecycle events``
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` `--filter` ``CoreActorPool.startActor uses client graphIds to build subgraph``
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` `--filter` ``CoreActorPool.startActor selects actor from command node text``
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` `--filter` ``mailbox records ActorStarted before actor body runs``
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` `--filter` ``ActorStart persist Error leaves Focus out of liveActorNodeIds``
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` `--filter` ``Undo and Redo post as orders; ActorStart and Cancel still post as PostEvent``
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` `--filter` ``postEvent returns the stored start when that start stops at once``
[ ] `tests/Server.Tests/Issue41CoreMailboxTests.fs` `--filter` ``mailbox appends ActorStart and ActorStop in lifecycle order``
[ ] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` `--filter` ``File mixed hello survives dispose create``
[ ] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` `--filter` ``Db mixed hello survives dispose create``
[ ] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` `--filter` ``getEventId is ActorStop after in-process hello``
[ ] `tests/Server.Tests/PersistHandlersRestoreTests.fs` `--filter` ``ActorStart persists across File restart``
[ ] `tests/Server.Tests/ApiPostCommandTests.fs` `--filter` ``postCommand through CoreMailbox encodes ActorStart in latestId events``
[ ] `tests/Server.Tests/OrderedEventStreamTests.fs` `--filter` ``edit then unknown ActorStart stores a failed ActorStop``
[ ] `tests/Server.Tests/OrderedEventStreamTests.fs` `--filter` ``edit then rejected ActorStart stores a failed ActorStop``
[ ] `tests/Server.Tests/TestActorCommandErrorTests.fs` `--filter` ``production events door records a failed ActorStop for an unknown actor``

## Stop when

[ ] Rewrite `27-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when `dispatchStartActor` or `postChosenStart` has no `ActorFunction`.
[ ] Stop when this ticket adds an `ActorFunction`.
[ ] Stop when this ticket adds a session id.
[ ] Stop when the edit posts `Order.StartActor` before the pool affirms.
[ ] Stop when a refused start posts an event.
[ ] Stop when `expandStored` pairs a stored `EventBody.ActorStart` with its stop.
[ ] Stop when the change passes 2 source files or 7 test files.
[ ] Stop and ask. The size limit is 4 test files. Outputs names 7 test files.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``CoreMailboxApi.startActor appends ActorStart to lifecycle events``
  - `tests/Server.Tests`: ``CoreActorPool.startActor uses client graphIds to build subgraph``
  - `tests/Server.Tests`: ``CoreActorPool.startActor selects actor from command node text``
  - `tests/Server.Tests`: ``mailbox records ActorStarted before actor body runs``
  - `tests/Server.Tests`: ``ActorStart persist Error leaves Focus out of liveActorNodeIds``
  - `tests/Server.Tests`: ``Undo and Redo post as orders; ActorStart and Cancel still post as PostEvent``
  - `tests/Server.Tests`: ``postEvent returns the stored start when that start stops at once``
  - `tests/Server.Tests`: ``mailbox appends ActorStart and ActorStop in lifecycle order``
  - `tests/Server.Tests`: ``File mixed hello survives dispose create``
  - `tests/Server.Tests`: ``Db mixed hello survives dispose create``
  - `tests/Server.Tests`: ``getEventId is ActorStop after in-process hello``
  - `tests/Server.Tests`: ``ActorStart persists across File restart``
  - `tests/Server.Tests`: ``postCommand through CoreMailbox encodes ActorStart in latestId events``
  - `tests/Server.Tests`: ``edit then unknown ActorStart stores a failed ActorStop``
  - `tests/Server.Tests`: ``edit then rejected ActorStart stores a failed ActorStop``
  - `tests/Server.Tests`: ``production events door records a failed ActorStop for an unknown actor``
[ ] All executed tests pass.
[ ] Rewrite `27-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
