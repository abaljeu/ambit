# 113 — Actors return their ActorResult
**Status**: `coded`
One job: An Actor returns its `ActorResult`. `CoreActorPool.runReturned` binds that result. `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped`. This is a strict refactor. The observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results stay the same. The reason is the 2026-10-24 decision in [core-refinement](../project.md): an Actor ends, and the pool posts the stop.

Type: coding
Blockers: [112 — Place Cancel through PlaceOrder](112-place-cancel-through-placeorder.md) is `coded` and landed on staging in PR 522.

## Scope

Slice: expand the return of the live Actor contract to `Async<ActorResult>`. `CoreActorPool.runReturned` binds that result. `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped`.

[x] The build keeps the observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results the same.

### Signatures

[x] `ActorFunction` in `src/Shared/History.fs` is `FunctionRun -> ActorChanges -> Async<ActorResult>`. Live starts use this contract.
[x] `ActorFunction` in `src/Server/Core/CoreActorPool.fs` is `FunctionRun -> CoreChangesDoor -> Async<ActorResult>`. `CoreMailboxLoop.poolActor` adapts the shared function onto this wrapper.
[x] `ActorFn` in `src/Server/Core/CoreActorPool.fs` is `ActorInput -> CoreChangesDoor -> Async<ActorResult>`. It remains on the pool. `CoreActorPool.register`, `CoreActorPool.startActor`, `BagPending`, `CoreRuntime.Actors`, and `TestActor.actorFn` use it.
[x] `ActorOrderFromStart.FunctionActorPlacement` in `src/Server/ActorOrderFromStart.fs` is the placement record. Its `actor` field is the shared `ActorFunction`. The source name `FunctionActor` occurs only in that record.
[x] `CoreActorPool.runSchedule` in `src/Server/Core/CoreActorPool.fs` is private. `CoreActorPool.schedule` is `runSchedule`. It starts the pending body with `Async.Start` on the row cancellation token.
[x] The shared `ActorFunction`, the pool `ActorFunction`, and the leftover `ActorFn` return `Async<ActorResult>`.
[x] `CoreActorPool.runSchedule` starts `CoreActorPool.runReturned`. `runReturned` binds the `ActorResult`. `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped` unless the row token is cancelled.

### Match sites

[x] `GithubTransportActor.finishActor` in `src/Server/GithubTransportActor.fs` returns `actorOutcome`. `loadActorFunction` and `saveActorFunction` use `finishActor`.
[x] `RunAgentActor.stop` has left. `RunAgentActor.actorFn` in `src/Server/RunAgentActor.fs` returns the result.
[x] `SearchActor.queryActorBody` in `src/Server/SearchActor.fs` returns `Async<ActorResult>`.
[x] `SearchActor.functionStart`, `SearchActor.functionActorAt`, and `SearchActor.searchActorAt` return the result. `searchActorAt` returns `ActorSucceeded`.
[x] `TestActor.dispatch` and `TestActor.runBehavior` in `src/Server/TestActor.fs` return the result. `TestActor.actorFn` is `dispatch`. `TestActor.functionActor` is the pool `ActorFunction`. `TestActor.functionActorAt` is the shared `ActorFunction`. Both call `runBehavior`.
[x] `CoreMailboxLoop.poolActor` in `src/Server/Core/CoreMailboxLoop.fs` calls the shared `ActorFunction` and returns that `ActorResult`.
[x] `EventJson.stubActor` in `src/Shared/EventJson.fs` is an `ActorFunction` that returns `ActorSucceeded`.
[x] `CoreDoor.actorChanges` fills `postChanges`, `graphAtEvent`, `loadNode`, `postNotice`, `eventsSince`, and `postGraphOnly`. `ActorChanges` is those fields.

### Existing tests

[x] `TestBackend.waitCancelled` in `tests/Server.Tests/TestBackend.fs` reads `Async.CancellationToken`, waits on `Async.AwaitWaitHandle` of that token's wait handle, and returns `ActorSucceeded`.
[x] ``function start reply is bookkeeping without waiting for an Actor body`` in `tests/Server.Tests/CoreMsgActorCasesTests.fs` starts an `ActorFunction` that calls `TestBackend.waitCancelled`, then asserts the focus is in `pool.liveActorNodeIds`.
[x] ``ActorOrder places the actor on the thread pool`` in `tests/Server.Tests/CoreMsgActorCasesTests.fs` starts an `ActorFunction` that calls `TestBackend.waitCancelled`, then asserts `pool.isLive`.
[x] `AskCancelHarness.postChildThenWait` in `tests/Server.Tests/AskCancelHarness.fs` posts one child, then calls `TestBackend.waitCancelled`. The row stays live for the test.
[x] `CoreActorPoolDeliverTests.liveSessionActor` in `tests/Server.Tests/CoreActorPoolDeliverTests.fs` records the session, then calls `TestBackend.waitCancelled`.
[x] ``Actor getEventId surfaces persist error instead of Revision 0`` and ``Actor postChange on scheduled handle reaches persist`` in `tests/Server.Tests/ActorCoreChangesDoorTests.fs` pass an `ActorFunction` that records the observation and then calls `TestBackend.waitCancelled`. Those bodies do not call `actorStop`.
[x] ``coreMailboxStartActorFromFunction places StartActor from the selector`` in `tests/Server.Tests/ActorOrderFromStartTests.fs` starts an `ActorFunction` that calls `TestBackend.waitCancelled`. The assertions read the `Order.StartActor` row.
[x] ``ActorOrder round-trips on the wire with stubActor`` in `tests/Shared.Tests/EventJsonTests.fs` builds two `ActorFunction` values that return `ActorSucceeded` and does not run them.
[x] `createHost` in `tests/Server.Tests/CoreMailboxDoorTests.fs` registers an `ActorFn` named `root` that returns `ActorSucceeded`. No test starts that name.
[x] ``ActorStop reads the carrier graph once`` in `tests/Server.Tests/QueryActorTests.fs` starts `SearchActor.functionStart` through `pool.startFunction` and `pool.schedule`. It reads `EventBody.ActorStop` of `ActorQuery`.

### Copy-from

[x] `CoreActorPool.runReturned` binds the `ActorResult`. `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped` unless the row token is cancelled.

### Overlap

[x] [23 — Server search works](../../online-search/issues/23-server-search-works.md) has landed. It changed `SearchActor.reply`. This ticket leaves that function.
[x] This ticket's `SearchActor` edits are `queryActorBody`, `functionStart`, `functionActorAt`, and `searchActorAt`. `SearchActor.reply`, `SearchActor.actorStart`, and `SearchActor.oneReply` stay.
[x] [24 — Remove CoreMsg.RecordSearchStop](../../online-search/issues/24-remove-coremsg-recordsearchstop.md) is next. [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md) is `coded`. `SearchActor.searchActorAt` returns `ActorSucceeded`. The pool posts `Notice.ActorStopped`. `CoreMsg.RecordSearchStop` stays.

## Inputs

### Working

[x] The shared `ActorFunction` in `src/Shared/History.fs` is the live start contract.
[x] The pool `ActorFunction` in `src/Server/Core/CoreActorPool.fs` is the wrapper `runSchedule` calls for `FunctionPending`.
[x] `ActorFn` in `src/Server/Core/CoreActorPool.fs` is the leftover id-bag contract. `runSchedule` still starts `BagPending`.
[x] `GithubTransportActor.finishActor` returns `actorOutcome`.
[x] `RunAgentActor.stop` has left. `RunAgentActor.actorFn` returns the result.
[x] `SearchActor.queryActorBody`, `SearchActor.functionStart`, `SearchActor.functionActorAt`, and `SearchActor.searchActorAt` return the result. `searchActorAt` returns `ActorSucceeded`.
[x] `TestActor.dispatch` and `TestActor.runBehavior` return the result.
[x] `CoreDoor.actorChanges` fills `postChanges`, `graphAtEvent`, `loadNode`, `postNotice`, `eventsSince`, and `postGraphOnly`.
[x] Each of those producers returns the `ActorResult`. The pool posts `Notice.ActorStopped`.
[x] A test body that is started, and that does not assert a stop, calls `TestBackend.waitCancelled` after its observation. The row stays live. `CoreActorPool.finish` cancels that wait.
[x] ``function start reply is bookkeeping without waiting for an Actor body`` and ``ActorOrder places the actor on the thread pool`` keep their assertions. The body calls `TestBackend.waitCancelled`.
[x] `EventJson.stubActor` and the two unrun `ActorFunction` values in ``ActorOrder round-trips on the wire with stubActor`` return `ActorSucceeded`.

### Reference

[x] The 2026-10-24 Actor-end decision is in the project file. The One job line links it.
[x] [Size limit](../../context/size-limit.md). Alan, 2026-10-24, accepted this ticket over the test-file limit. The accepted count was 9 test files. The tree changes 9 test files. `TestBackend.waitCancelled` is the shared wait. The other 8 hold a body of this type.

## Do not load

[x] `CoreMsg` is `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`. [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md) is `coded`.
[x] `CoreMailboxLoop.dispatchItem`. [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md) posts the notice.
[x] `CoreMsg.RecordSearchStop`. 24 — Remove CoreMsg.RecordSearchStop owns that case.
[x] `SearchActor.reply`, `SearchActor.actorStart`, and `SearchActor.oneReply`. 23 — Server search works owns the Find reply path.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change `ActorFunction` in `src/Shared/History.fs` to return `Async<ActorResult>`.
3. [x] Change `ActorFunction` and `ActorFn` in `src/Server/Core/CoreActorPool.fs` to return `Async<ActorResult>`.
4. [x] `CoreMailboxLoop.poolActor` returns the shared actor's `ActorResult`.
5. [x] `CoreActorPool.runSchedule` starts `CoreActorPool.runReturned` on the row cancellation token. `runReturned` binds the `ActorResult`.
6. [x] `GithubTransportActor.finishActor` returns the outcome.
7. [x] `RunAgentActor.stop` has left. `RunAgentActor.actorFn` returns the result.
8. [x] `SearchActor.queryActorBody`, `functionStart`, and `functionActorAt` return the `ActorResult`. `searchActorAt` returns `ActorSucceeded`. `SearchActor.reply`, `SearchActor.actorStart`, and `SearchActor.oneReply` stay.
9. [x] `TestActor.dispatch` and `TestActor.runBehavior` return the result.
10. [x] `EventJson.stubActor` returns `ActorSucceeded`.
11. [x] Each started test body that must stay live calls `TestBackend.waitCancelled` after its observation. The assertions stay.

## Outputs

[x] Source files: `src/Shared/History.fs`, `src/Server/Core/CoreActorPool.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/GithubTransportActor.fs`, `src/Server/RunAgentActor.fs`, `src/Server/SearchActor.fs`, `src/Server/TestActor.fs`, and `src/Shared/EventJson.fs`. 8 source files.
[x] Plan file: [Actor as client architecture](../../actor-as-client/arch.md). The shared `ActorFunction`, the pool `ActorFunction`, and `ActorFn` return `Async<ActorResult>`. Pool launch records that `CoreActorPool.runReturned` binds the `ActorResult` and `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped`.
[x] Test files: `tests/Server.Tests/CoreMsgActorCasesTests.fs`, `tests/Server.Tests/AskCancelHarness.fs`, `tests/Server.Tests/CoreActorPoolDeliverTests.fs`, `tests/Server.Tests/ActorCoreChangesDoorTests.fs`, `tests/Server.Tests/ActorOrderFromStartTests.fs`, `tests/Shared.Tests/EventJsonTests.fs`, `tests/Server.Tests/CoreMailboxDoorTests.fs`, `tests/Server.Tests/SearchActorTests.fs`, and `tests/Server.Tests/TestBackend.fs`. 9 test files. Over the 4-test limit. The 2026-10-24 acceptance covers this ticket. `TestBackend.waitCancelled` is the shared wait.
[x] ``postSearch records ActorStart at the root`` in `tests/Server.Tests/SearchActorTests.fs` returns the result of `SearchActor.searchActorAt`. The assertions stay.
[x] No test assertion changes.

## Stop when

[ ] Rewrite `113-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit removes a stop call.
[ ] Stop when the edit changes `EventBody.ActorStop`, `Notice.ActorStopped`, or an `ActorResult`.
[ ] Stop when the edit changes a `SearchActor` function other than `queryActorBody`, `functionStart`, `functionActorAt`, and `searchActorAt`.
[ ] Stop when the edit needs a file outside Inputs.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``function start reply is bookkeeping without waiting for an Actor body``
  - `tests/Server.Tests`: ``ActorOrder places the actor on the thread pool``
  - `tests/Server.Tests`: ``ActorStop reads the carrier graph once``
  - `tests/Server.Tests`: ``mailbox appends StartActor and ActorStop in lifecycle order``
  - `tests/Shared.Tests`: ``ActorOrder round-trips on the wire with stubActor``
[x] All executed tests pass.
[x] Rewrite `113-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
