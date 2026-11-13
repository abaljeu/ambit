# 24 — Remove CoreMsg.RecordSearchStop
**Status:** defined
One job: The Server records a finished search as `Notice.ActorStopped` through the actor stop, and `CoreMsg.RecordSearchStop` leaves, while the Find start still places `Order.StartActor` for the actor `search`. The reason is Alan's decision on 2026-11-13 in [core-refinement](../../core-refinement/project.md).

Type: coding
Blockers:

Prerequisite: [23 — Server search works](23-server-search-works.md).

## Inputs

### Working

[x] `CoreMsg` in `src/Server/Core/CoreMsg.fs` is `StartLoadSaveCommand`, `RecordSearchStop`, `ActorStop`, and `PostEvent`.
[x] `RecordSearchStop` is `caller: Caller * rootId: NodeId * AsyncReplyChannel<Result<unit, string>>`. The comment says the case puts ActorStop on the event source, and the id is the root.
[x] `CoreMailboxApi.recordSearchStop` in `src/Server/Core/CoreMailboxApi.fs` takes `MailboxHost`, `Caller`, and `NodeId`, and returns `Async<Result<unit, string>>`. It posts `RecordSearchStop` through `MailboxHost.postAndAsyncReply`.
[x] `CoreMailboxLoop.runMsg` in `src/Server/Core/CoreMailboxLoop.fs` handles `RecordSearchStop` by calling `CoreEventDispatch.actorStop` with that caller, that root id, and `ActorSucceeded`.
[x] `CoreMailboxLoop.messageCaller` matches `RecordSearchStop` and returns the caller.
[x] `CoreMailboxEvents.operationContext` in `src/Server/Core/CoreMailboxEvents.fs` matches `RecordSearchStop` and returns `"RecordSearchStop", ""`.
[x] `CoreMailboxEvents.replyFailure` matches `RecordSearchStop` and replies `Error` on that channel.
[x] `CoreEventDispatch.actorStop` in `src/Server/Core/CoreEventDispatch.fs` commits `EventBody.ActorStop` of the focus id and the result.
[x] `EventBody.ActorStop` in `src/Shared/History.fs` builds `EventBody.Notice` of `Notice.ActorStopped` of that focus id and that result. The active pattern reads that same notice.
[x] `RouteRegistration.searchDoor` in `src/Server/RouteRegistration.fs` sets `recordStop` to a function that calls `CoreMailboxApi.recordSearchStop` with the host, the browser caller, and the root id, then returns unit.
[x] `Api.SearchActorDoor` in `src/Server/Api.fs` has `recordStop: NodeId -> Async<unit>`. `Api.searchReply` calls `recordStart` and returns `SearchActor.reply`. `Api.postSearch` calls `searchReply`.
[x] `SearchActor.actorStart` in `src/Server/SearchActor.fs` sets `zoomId`, `focusId`, `runDefinitionId`, and `graphIds` to the graph root.
[x] `CoreMailboxApi.recordSearchStart` places `Order.StartActor` for the actor name `search`. `actorNodeIdOf` is `ActorOrderFromStart.focusIdOf`, which returns `ActorStart.focusId`.
[x] `SearchActor.searchActorAt` returns `ActorSucceeded`. It does not call a stop. `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped` of that result. `SearchActor.actorCaller` has left.
[x] `CoreMailboxLoop.poolActor` builds `ActorChanges` with `CoreDoor.actorChanges` for `CoreDoor.actorCaller` of the pool secret. `CoreDoor.actorStop` calls `CoreMailboxApi.actorStop`, which posts `CoreMsg.ActorStop`. `searchActorAt` does not call that stop.
[x] `CoreMailboxActors.dispatchActorStop` in `src/Server/Core/CoreMailboxActors.fs` accepts `Authority "Actor"`, reads `pool.getActorNodeId`, and calls `CoreEventDispatch.actorStop` with that actor node id and the result. That arm serves `CoreMsg.ActorStop`.
[x] `tests/Server.Tests/SearchActorTests.fs` test ``postSearch records ActorStart at the root`` sets `recordStop` to a closure that calls `CoreMailboxApi.recordSearchStop`. The test waits with `TestBackend.waitForNotices` for `EventBody.ActorStop` of the root with `ActorSucceeded`. That row is the pool post. `recordSearchStop` still logs its own stop.

### Reference

[x] [Search Actor](../../doc/current/search-actor.md) section **Uses**. The stop line says `SearchActor.searchActorAt` returns `ActorSucceeded`, and `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped`.
[x] Alan, 2026-11-13: remove `CoreMsg.RecordSearchStop`. A search stop is `Notice.ActorStopped` from the pool post.

## Do not load

[ ] `CoreMailboxApi.recordSearchStart`, `Order.StartActor`, and `SearchActor.actorStart`.
[ ] `CoreMsg.ActorStop`, `CoreMailboxApi.actorStop`, and `CoreMailboxActors.dispatchActorStop`. [114 — Remove CoreMsg.ActorStop](../../core-refinement/issues/114-remove-coremsg-actorstop.md) owns that case.
[ ] `Api.SearchActorDoor` and `Api.postSearch`.
[ ] `Notice` cases other than the `Notice.ActorStopped` the pool post already logs.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Contract: delete the `RecordSearchStop` case from `CoreMsg` in `src/Server/Core/CoreMsg.fs`.
3. [ ] Contract: delete the `RecordSearchStop` arm of `runMsg` and the `RecordSearchStop` arm of `messageCaller` in `src/Server/Core/CoreMailboxLoop.fs`.
4. [ ] Contract: delete the `RecordSearchStop` arm of `operationContext` and the `RecordSearchStop` arm of `replyFailure` in `src/Server/Core/CoreMailboxEvents.fs`.
5. [ ] Contract: delete `CoreMailboxApi.recordSearchStop` in `src/Server/Core/CoreMailboxApi.fs`.
6. [ ] Change: `RouteRegistration.searchDoor.recordStop` is `fun _ -> async.Return ()`.
7. [ ] Change: in ``postSearch records ActorStart at the root``, the `recordStop` closure is `fun _ -> async.Return ()`. The assertion stays on `EventBody.ActorStop` of the root with `ActorSucceeded`.

## Outputs

[ ] Source files: `src/Server/Core/CoreMsg.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/Core/CoreMailboxEvents.fs`, `src/Server/Core/CoreMailboxApi.fs`, and `src/Server/RouteRegistration.fs`.
[ ] That list is 5 source files. [Size limit](../../context/size-limit.md) names 8.
[ ] `tests/Server.Tests/SearchActorTests.fs` keeps ``postSearch records ActorStart at the root``. The `recordStop` closure returns unit. The log assertion stays on the actor stop of the root with `ActorSucceeded`, which is `Notice.ActorStopped`.

## Stop when

[ ] Rewrite `24-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit adds a `Notice` case, changes `recordSearchStart`, or changes `CoreMsg.ActorStop`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``postSearch records ActorStart at the root``
[ ] All executed tests pass.
[ ] Rewrite `24-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
