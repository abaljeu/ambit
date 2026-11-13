# 21 — Convert CoreMsg.ActorStop
**Status**: superseded
One job: An Actor stop is `Notice.ActorStopped` on `CoreQueue.Notice`, posted with `CoreMailboxApi.postNotice`. `CoreMsg.ActorStop` is gone. The live row still ends through `CoreActorPool.finish`.

Alan, 2026-10-24 and 2026-11-13: this ticket is superseded by the chain [113 — Actors return their ActorResult](../../../core-refinement/issues/113-actors-return-their-actorresult.md), then [117 — The pool posts Notice.ActorStopped when an Actor ends](../../../core-refinement/issues/117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md), then [114 — Remove CoreMsg.ActorStop](../../../core-refinement/issues/114-remove-coremsg-actorstop.md). [118 — Test Actors return their result](../../../core-refinement/issues/118-test-actors-return-their-result.md) is superseded by 114. Alan, 2026-11-13. Each live ticket is a strict refactor. The observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results stay the same. The pool-posted stop row keeps `Authority "Actor"`. The steps below stay as the record of this ticket. The chain is the work.

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

[x] `CoreMsg` in `src/Server/Core/CoreMsg.fs` is `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[x] `CoreMailboxLoop.runMsg` in `src/Server/Core/CoreMailboxLoop.fs` matches those three. The `RecordSearchStop` arm calls `CoreEventDispatch.actorStop`.
[x] `CoreMailboxLoop.messageCaller` reads the caller from `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[x] `CoreMailboxEvents.operationContext` in `src/Server/Core/CoreMailboxEvents.fs` matches those three.
[x] `CoreMailboxEvents.replyFailure` replies on those three.
[x] `CoreMailboxLoop.logActorStopped` reads `CoreActorPool.trySecretForActorNode`. A secret logs through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller`, then `CoreActorPool.finish`. The stored authority is `Authority "Actor"`. No secret logs nothing and finishes nothing.
[x] `CoreActorPool.trySecretForActorNode` in `src/Server/Core/CoreActorPool.fs` returns the secret for an actor node id.
[x] `Notice.ActorStopped` is `focusId` and `result` in `src/Shared/History.fs`.
[x] `EventBody.ActorStop` builds `EventBody.Notice` of `Notice.ActorStopped`.
[x] `CoreMailboxApi.postNotice` posts `CoreQueue.Notice`.
[x] `CoreActorPool.runReturned` binds the `ActorResult`. `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped` unless the row token is cancelled. A cancelled row posts nothing.
[x] `CoreMailboxLoop.dispatchItem` sends `Notice.ActorStopped` to `CoreMailboxLoop.logActorStopped`. Every other `CoreQueue.Notice` goes to `CoreMailboxLoop.logPostedNotice`, which calls `CoreEventDispatch.logNotice`, then `CoreMailboxLoop.applyNotice`.
[x] `CoreMailboxLoop.applyNotice` returns `Ok ()` for `Notice.ActorStopped`.
[x] `CoreEventDispatch.logNotice` stores `Authority ""`.
[x] `CoreDoor.actorChanges` in `src/Server/Core/CoreDoor.fs` fills `postChanges`, `graphAtEvent`, `loadNode`, `postNotice`, `eventsSince`, and `postGraphOnly`. `ActorChanges` in `src/Shared/History.fs` is those fields.
[x] `loadActorFunction` and `saveActorFunction` in `src/Server/GithubTransportActor.fs` each take `focusId`. `GithubTransportActor.finishActor` returns the outcome. It does not call `changes.actorStop`.
[x] `RunAgentActor.stop` has left. `RunAgentActor.actorFn` in `src/Server/RunAgentActor.fs` returns the result.
[x] `SearchActor.functionStart` returns `FunctionStart`. Its actor calls `SearchActor.queryActorBody` and returns that `ActorResult`. `actorNodeId` is `ActorStart.focusId`. The actor argument is a `CoreChangesDoor`.
[x] `SearchActor.functionActorAt` and `SearchActor.searchActorAt` return the result. They do not call a stop. `SearchActor.actorCaller` has left.
[x] `TestActor.dispatch` and `TestActor.runBehavior` in `src/Server/TestActor.fs` return the result. They do not call a stop.
[x] `TestActor.functionActorAt` passes `focusId` into `runBehavior` as the parent.
[x] `TestActor.functionActor` passes `graph.focus`, or `graph.root` when focus is missing, into `runBehavior` as the parent.
[x] ``mailbox appends StartActor and ActorStop in lifecycle order`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs` starts `TestActor.functionActorAt`. It does not call `CoreMailboxApi.actorStop`. The pool posts the stop. The stored authority is `Authority "Actor"`.
[x] `runMixedHello` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` starts `TestActor.functionActorAt`. It does not call `CoreMailboxApi.actorStop`.
[x] ``actorStop after cancel does not emit a root Focus ActorStop`` in `tests/Server.Tests/CancelByFocusTests.fs` posts `Notice.ActorStopped` with `CoreMailboxApi.postNotice`.
[x] ``ActorStopped Notice drops live row`` and ``ActorStopped Notice is logged and drops live row`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` post `Notice.ActorStopped` with `CoreMailboxApi.postNotice`.
[x] `postActorStop` in `tests/Server.Tests/CoreMsgActorCasesTests.fs` posts `Notice.ActorStopped` with `CoreMailboxApi.postNotice`.
[x] ``ActorStart body does not run inside the mailbox turn`` has left `tests/Server.Tests/OrderedEventStreamTests.fs`. That file does not call `CoreDoor.actorStop`.
[x] ``query start error posts no rows, fails the reply, and keeps the edit`` has left `tests/Server.Tests/QueryActorTests.fs`. That file does not call `CoreDoor.actorStop`. ``posted gate start replies unknown actor`` replies `unknown actor`.
[x] ``Ask extract carries Graph.focus from launch`` has left `tests/Server.Tests/AgentAskTests.fs`. That file does not call `CoreDoor.actorStop`.
[x] ``ActorStop with Test credentials is refused``, ``ActorStop with Browser credentials is refused``, and ``ActorStop with Parse credentials is refused`` have left `tests/Server.Tests/CoreMsgActorCasesTests.fs`.

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
