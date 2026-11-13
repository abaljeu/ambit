# 24 — Post Notice.SearchStopped
**Status:** defined
One job: The Server posts `Notice.SearchStopped` where it now enqueues `CoreMsg.RecordSearchStop`. The Find start already places `Order.StartActor` for the actor `search`, and this ticket leaves that start in place. The reason is the 2026-10-24 search-stop decision in [core-refinement](../../core-refinement/project.md).

Type: coding
Blockers:

Prerequisite: [23 — Server search works](23-server-search-works.md).

## Inputs

### Working

[ ] `CoreMailboxApi.recordSearchStart` places `Order.StartActor` through `coreMailboxStartActorFromFunction`. The placement actor name is `search`. The actor node id is `ActorOrderFromStart.focusIdOf`.
[ ] `CoreMailboxLoop.placeOrder` handles `Order.StartActor` in `replyActorOrder`. That arm calls `pool.startFunction`, then appends `EventBody.Order` of that `Order.StartActor`.
[ ] `CoreMsg` is `StartLoadSaveCommand`, `RecordSearchStop`, `ActorStop`, and `PostEvent`.
[ ] `CoreMailboxApi.recordSearchStop` enqueues `RecordSearchStop` of the caller, the root id, and the reply channel.
[ ] `CoreMailboxLoop.runMsg` handles `RecordSearchStop` by calling `CoreEventDispatch.actorStop` with that root id and `ActorSucceeded`.
[ ] `EventBody.ActorStop` is the shell over `Notice.ActorStopped`.
[ ] `runMsg`, `messageCaller`, `CoreMailboxEvents.operationContext`, and `CoreMailboxEvents.replyFailure` each match `RecordSearchStop`.
[ ] `RouteRegistration.searchDoor.recordStop` calls `recordSearchStop` with the browser caller and the root id.
[ ] `Api.searchReply` calls `recordStart` and returns `SearchActor.reply`.
[ ] `SearchActor.searchActorAt` calls `changes.actorStop` with `ActorSucceeded`. `CreateBoot` installs that function as `BootActors.search`.
[ ] `CoreMailboxApi.postNotice` posts `CoreQueue.Notice`. `dispatchItem` logs that notice, then calls `applyNotice`.
[ ] These matches cover every `Notice` case: `encodeNotice` and the notice decoder in `src/Shared/EventJson.fs`, `writeNoticeRow` in `src/Server/Database.fs`, `applyNotice` in `src/Server/Core/CoreMailboxLoop.fs`, `Ev.documentStateFromNotice` in `src/Shared/History.fs`, `writeGraph` in `src/Shared/NoticeGraph.fs`, `replaySkipsNotice` in `src/Shared/EventLog.fs`, and `noticeNodeId` in `src/Shared/SyncLogic.fs`.
[ ] `tests/Server.Tests/SearchActorTests.fs` test `postSearch records ActorStart at the root` asserts `Order.StartActor` for `search` and `EventBody.ActorStop` of the root with `ActorSucceeded`. Its `recordStop` closure calls `recordSearchStop`. `Api.postSearch` leaves that closure uncalled.
[ ] `tests/Shared.Tests/EventJsonTests.fs` test `Notice JSON round trip` lists the current `Notice` cases.

### Reference

[ ] [Search Actor](../../doc/current/search-actor.md) section **Uses**. The open door line says `recordStop` posts `Notice.SearchStopped` of the root id through the notice function.

## Do not load

[ ] `Order.StartSearch` and `Order.CancelSearch`.
[ ] `recordSearchStart`, `coreMailboxStartActorFromFunction`, and `Order.StartActor`.
[ ] `CoreMsg.ActorStop` and `SearchActor.searchActorAt`. [114 — Remove CoreMsg.ActorStop](../../core-refinement/issues/114-remove-coremsg-actorstop.md) owns the actor stop case.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand: add `Notice.SearchStopped of rootId: NodeId` in `src/Shared/History.fs`.
3. [ ] Expand: each exhaustive `Notice` match in Inputs handles `Notice.SearchStopped`. `applyNotice` returns `Ok ()`. `documentStateFromNotice` returns `None`. `writeGraph` returns the same graph. `noticeNodeId` returns `rootId`. `replaySkipsNotice` returns true. `writeNoticeRow` returns a completed task.
4. [ ] Expand: `EventJson` writes and reads the notice kind `searchStopped` with `rootId`.
5. [ ] Change: `CoreMailboxApi.recordSearchStop` becomes `searchStopped`. It takes the host and the root id, posts `Notice.SearchStopped rootId` through `postNotice`, and returns `unit`.
6. [ ] Change: `searchDoor.recordStop` calls `searchStopped`.
7. [ ] Contract: remove `RecordSearchStop` from `CoreMsg` and from `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`.

## Outputs

[ ] Source files: `src/Shared/History.fs`, `src/Shared/EventJson.fs`, `src/Shared/EventLog.fs`, `src/Shared/NoticeGraph.fs`, `src/Shared/SyncLogic.fs`, `src/Server/Database.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/Core/CoreMailboxEvents.fs`, `src/Server/Core/CoreMsg.fs`, `src/Server/Core/CoreMailboxApi.fs`, and `src/Server/RouteRegistration.fs`.
[ ] That list is 11 source files. [Size limit](../../context/size-limit.md) names 8. The extra files are the exhaustive `Notice` matches one new case requires.
[ ] `tests/Server.Tests/SearchActorTests.fs` gains ``searchStopped posts Notice.SearchStopped``. The log holds `Notice.SearchStopped` of the root id.
[ ] `tests/Shared.Tests/EventJsonTests.fs` adds `Notice.SearchStopped` to ``Notice JSON round trip``.
[ ] ``postSearch records ActorStart at the root`` stays. Its `recordStop` closure compiles against `searchStopped`.

## Stop when

[ ] Rewrite `24-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit adds `Order.StartSearch` or changes `recordSearchStart`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``searchStopped posts Notice.SearchStopped``
  - `tests/Shared.Tests`: ``Notice JSON round trip``
  - `tests/Server.Tests`: ``postSearch records ActorStart at the root``
[ ] All executed tests pass.
[ ] Rewrite `24-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
