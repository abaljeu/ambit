# 21 — Convert CoreMsg.ActorStop
**Status**: defined
One job: An Actor stop is `Notice.ActorStopped` on `CoreQueue.Notice`, posted with `CoreMailboxApi.postNotice`. `CoreMsg.ActorStop` is gone. The live row still ends through `CoreActorPool.finish`.

[15 — Delete the old Actor starts](../15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md) leaves `ActorStop` as a live `CoreMsg` case. The stop body is already `Notice.ActorStopped` in `src/Shared/History.fs`.

[ ] `FunctionRun` in `src/Shared/History.fs` stays `getGraph` and `secret`.
[ ] `Notice.ActorStopped` stays `focusId` and `result`. This ticket adds no session id.
[ ] The source files are 11, plus `src/Server/Core/CoreEventDispatch.fs` when `RecordSearchStop` is already gone. The test files are 8. The size limit is 8 source files and 4 test files. The counts are over the limit because every current use of the case moves together.

Type:

[ ] coding

Blockers:

[ ] none

## Inputs

### Working

[ ] `CoreMsg.ActorStop` is defined in `src/Server/Core/CoreMsg.fs` as `caller`, `result`, and `AsyncReplyChannel<Result<unit, string>>`.
[ ] `CoreMailboxApi.actorStop` in `src/Server/Core/CoreMailboxApi.fs` is the only builder. It posts `ActorStop(caller, result, channel)`.
[ ] `CoreMailboxLoop.runMsg` in `src/Server/Core/CoreMailboxLoop.fs` matches `ActorStop` and calls `CoreMailboxActors.dispatchActorStop`.
[ ] `CoreMailboxLoop.messageCaller` matches `ActorStop`.
[ ] `CoreMailboxEvents.operationContext` in `src/Server/Core/CoreMailboxEvents.fs` matches `ActorStop`.
[ ] `CoreMailboxEvents.replyFailure` matches `ActorStop`.
[ ] `CoreMailboxActors.dispatchActorStop` in `src/Server/Core/CoreMailboxActors.fs` admits `Authority "Actor"`, reads the focus with `CoreActorPool.getActorNodeId`, appends the stop with `CoreEventDispatch.actorStop`, and calls `pool.finish`.
[ ] A missing focus replies `Ok ()` and appends nothing.
[ ] `CoreActorPool.trySecretForActorNode` in `src/Server/Core/CoreActorPool.fs` returns the secret for a focus.
[ ] `Notice.ActorStopped` is `focusId` and `result` in `src/Shared/History.fs`.
[ ] `EventBody.ActorStop` builds `EventBody.Notice` of `Notice.ActorStopped`.
[ ] `CoreMailboxApi.postNotice` posts `CoreQueue.Notice`.
[ ] `CoreMailboxLoop.dispatchItem` logs a `CoreQueue.Notice` with `CoreEventDispatch.logNotice`, then calls `applyNotice`.
[ ] `applyNotice` returns `Ok ()` for `Notice.ActorStopped`.
[ ] `CoreEventDispatch.logNotice` stores a blank authority. The notice has no caller and no session id.
[ ] `CoreDoor.actorStop` in `src/Server/Core/CoreDoor.fs` calls `CoreMailboxApi.actorStop`.
[ ] `CoreDoor.actorChanges` sets `ActorChanges.actorStop` to that function. The field is the only `actorStop` member, in `src/Shared/History.fs`.
[ ] `loadActorFunction` and `saveActorFunction` in `src/Server/GithubTransportActor.fs` each take `focusId`. `finishActor` calls `changes.actorStop` and has no focus argument.
[ ] `RunAgentActor.stop` calls `changes.actorStop`. The focus is `ActorStart.focusId` on `RunAgentActor.actorFn` in `src/Server/RunAgentActor.fs`.
[ ] `SearchActor.functionStart` calls `CoreDoor.actorStop`. The focus is `ActorStart.focusId`. `FunctionStart.actorNodeId` is that same id. The actor argument is a `CoreChangesDoor`, because the pool `ActorFunction` in `src/Server/Core/CoreActorPool.fs` takes that door.
[ ] `TestActor.dispatch` calls `CoreDoor.actorStop`. The focus is `ActorInput.focusId` in `src/Server/TestActor.fs`.
[ ] `TestActor.functionActorAt` passes `focusId` into `runBehavior` as the parent. `runBehavior` calls `changes.actorStop`.
[ ] `TestActor.functionActor` passes `graph.focus`, or `graph.root` when focus is missing, into `runBehavior` as the parent.
[ ] ``mailbox appends ActorStart and ActorStop in lifecycle order`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs` calls `CoreMailboxApi.actorStop`.
[ ] `runMixedHello` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` calls `CoreMailboxApi.actorStop`.
[ ] ``actorStop after cancel does not emit a root Focus ActorStop`` in `tests/Server.Tests/CancelByFocusTests.fs` calls `CoreMailboxApi.actorStop`.
[ ] ``CoreMailboxApi.actorStop with valid credential drops live row`` and ``CoreMailboxApi.actorStop appends ActorStop and drops live row`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` call `CoreMailboxApi.actorStop`.
[ ] `postActorStop` in `tests/Server.Tests/CoreMsgActorCasesTests.fs` calls `CoreMailboxApi.actorStop`.
[ ] ``ActorStart body does not run inside the mailbox turn`` in `tests/Server.Tests/OrderedEventStreamTests.fs` calls `CoreDoor.actorStop`.
[ ] ``query start error posts no rows, fails the reply, and keeps the edit`` in `tests/Server.Tests/QueryActorTests.fs` calls `CoreDoor.actorStop`. A start asks the pool first. `Order.StartActor` is appended only after the pool affirms. A refused start posts nothing and replies with the failure.
[ ] ``Ask extract carries Graph.focus from launch`` in `tests/Server.Tests/AgentAskTests.fs` calls `CoreDoor.actorStop`.
[ ] ``ActorStop with Test credentials is refused``, ``ActorStop with Browser credentials is refused``, and ``ActorStop with Parse credentials is refused`` in `tests/Server.Tests/CoreMsgActorCasesTests.fs` send `CoreMsg.ActorStop`.

### Reference

[ ] [Arch](../../arch.md) **Shared changes**. `actorChanges` builds `ActorChanges`. The code in `src/Server/Core/CoreDoor.fs` is the builder this ticket changes.

## Do not load

[ ] `CoreMsg.PostEvent`. [22 — Convert CoreMsg.PostEvent](../22-convert-coremsg-postevent/22-convert-coremsg-postevent-ticket.md) converts that case.
[ ] `CoreEventDispatch.actorStop` while `RecordSearchStop` still calls it from `CoreMailboxLoop.runMsg`.
[ ] `FunctionRun`. It stays `getGraph` and `secret`.
[ ] `ActorInput.sessionId` and the live-row `sessionId`. `putFunctionStart` already stores that id.
[ ] A new `CoreMsg` case.
[ ] A new `Notice` case. `Notice.ActorStopped` is the stop.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: `CoreMailboxLoop.dispatchItem` handles `Notice.ActorStopped` with `CoreActorPool.trySecretForActorNode`. A secret logs that notice through `CoreEventDispatch.logNotice`, then calls `pool.finish` with the result. No secret logs nothing and finishes nothing. That is the missing-focus arm of `dispatchActorStop`.
3. [ ] Other notices keep today's order. `dispatchItem` logs the notice, then calls `applyNotice`.
4. [ ] `applyNotice` stays `Ok ()` for `Notice.ActorStopped`.
5. [ ] Change: `loadActorFunction` and `saveActorFunction` pass their `focusId` into `finishActor`. `finishActor` posts `Notice.ActorStopped` of that id through `ActorChanges.postNotice`.
6. [ ] Change: `RunAgentActor.actorFn` passes `start.focusId` into `stop`. `stop` posts `Notice.ActorStopped` of that id through `ActorChanges.postNotice`.
7. [ ] Change: `SearchActor.functionStart` posts `Notice.ActorStopped` of `ActorStart.focusId` through `CoreMailboxApi.postNotice`.
8. [ ] Change: `TestActor.dispatch` posts `Notice.ActorStopped` of `ActorInput.focusId` through `CoreMailboxApi.postNotice`.
9. [ ] Change: `TestActor.runBehavior` posts `Notice.ActorStopped` of the parent id `functionActorAt` and `functionActor` already pass.
10. [ ] Change: each test that calls `CoreMailboxApi.actorStop` or `CoreDoor.actorStop` posts `Notice.ActorStopped` of that same focus through `CoreMailboxApi.postNotice`, then reads the log.
11. [ ] Contract: delete ``ActorStop with Test credentials is refused``, ``ActorStop with Browser credentials is refused``, and ``ActorStop with Parse credentials is refused``. A notice has no caller to refuse.
12. [ ] Contract: delete `ActorChanges.actorStop`, `CoreDoor.actorStop`, `CoreMailboxApi.actorStop`, and `CoreMailboxActors.dispatchActorStop`.
13. [ ] Contract: delete `CoreMsg.ActorStop` and its arms in `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`.
14. [ ] Leave `CoreEventDispatch.actorStop` when `RecordSearchStop` still calls it. Delete it when that case is already gone.
15. [ ] `FunctionRun` stays `getGraph` and `secret`. `Notice.ActorStopped` stays `focusId` and `result`.

## Outputs

[ ] `src/Shared/History.fs` — delete `ActorChanges.actorStop`
[ ] `src/Server/Core/CoreDoor.fs`
[ ] `src/Server/Core/CoreMsg.fs`
[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `src/Server/Core/CoreMailboxLoop.fs`
[ ] `src/Server/Core/CoreMailboxEvents.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/GithubTransportActor.fs`
[ ] `src/Server/RunAgentActor.fs`
[ ] `src/Server/SearchActor.fs`
[ ] `src/Server/TestActor.fs`
[ ] `src/Server/Core/CoreEventDispatch.fs` — only when `RecordSearchStop` is already gone
[ ] `tests/Server.Tests/Issue41CoreMailboxTests.fs`
[ ] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
[ ] `tests/Server.Tests/CancelByFocusTests.fs`
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs`
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs`
[ ] `tests/Server.Tests/OrderedEventStreamTests.fs`
[ ] `tests/Server.Tests/QueryActorTests.fs`
[ ] `tests/Server.Tests/AgentAskTests.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/21-convert-coremsg-actorstop-stop.md`, then ask.
[ ] Stop when the edit adds a session id to `Notice`, `FunctionRun`, or `ActorChanges`.
[ ] Stop when the edit adds a `CoreMsg` case or a `Notice` case.
[ ] Stop when a test file outside Outputs must change.
[ ] Stop when `CoreEventDispatch.actorStop` still has the `RecordSearchStop` caller and this ticket deletes it.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `Issue41CoreMailboxTests`
  - `tests/Server.Tests`: `MailboxHistoryDurabilityTests`
  - `tests/Server.Tests`: `CancelByFocusTests`
  - `tests/Server.Tests`: `CoreMailboxDoorTests`
  - `tests/Server.Tests`: `CoreMsgActorCasesTests`
  - `tests/Server.Tests`: `OrderedEventStreamTests`
  - `tests/Server.Tests`: `QueryActorTests`
  - `tests/Server.Tests`: `AgentAskTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/21-convert-coremsg-actorstop-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
