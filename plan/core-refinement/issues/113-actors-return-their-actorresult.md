# 113 — Actors return their ActorResult
**Status**: defined
One job: An Actor returns its `ActorResult` to the pool, and the pool ignores that result. The Actor still sends the stop. This is a strict refactor. The observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results stay the same. The reason is the 2026-10-24 decision in [core-refinement](../project.md): an Actor ends, and the pool posts the stop.

Type: coding
Blockers: [112 — Place Cancel through PlaceOrder](112-place-cancel-through-placeorder.md) is `coded` and landed on staging in PR 522.

## Scope

Slice: expand the return of the live Actor contract to `Async<ActorResult>`. `runSchedule` discards that result with `Async.Ignore`. Each stop call stays.

[ ] The build keeps the observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results the same.

### Signatures

[x] `ActorFunction` in `src/Shared/History.fs` is `FunctionRun -> ActorChanges -> Async<unit>`. Live starts use this contract.
[x] `ActorFunction` in `src/Server/Core/CoreActorPool.fs` is `FunctionRun -> CoreChangesDoor -> Async<unit>`. `CoreMailboxLoop.poolActor` adapts the shared function onto this wrapper.
[x] `ActorFn` in `src/Server/Core/CoreActorPool.fs` is `ActorInput -> CoreChangesDoor -> Async<unit>`. It remains on the pool. `CoreActorPool.register`, `CoreActorPool.startActor`, `BagPending`, `CoreRuntime.Actors`, and `TestActor.actorFn` use it.
[x] `ActorOrderFromStart.FunctionActorPlacement` in `src/Server/ActorOrderFromStart.fs` is the placement record. Its `actor` field is the shared `ActorFunction`. The source name `FunctionActor` occurs only in that record.
[x] `CoreActorPool.runSchedule` in `src/Server/Core/CoreActorPool.fs` is private. `CoreActorPool.schedule` is `runSchedule`. It starts the pending body with `Async.Start` on the row cancellation token.
[ ] The shared `ActorFunction`, the pool `ActorFunction`, and the leftover `ActorFn` return `Async<ActorResult>`.
[ ] `runSchedule` starts `work |> Async.Ignore`. The pool does not read the result.

### Match sites

[x] `GithubTransportActor.finishActor` in `src/Server/GithubTransportActor.fs` calls `changes.actorStop` with `actorOutcome`, then returns unit. `loadActorFunction` and `saveActorFunction` use `finishActor`.
[x] `RunAgentActor.stop` in `src/Server/RunAgentActor.fs` calls `changes.actorStop`. `RunAgentActor.actorFn` returns the shared `ActorFunction` and calls `stop`.
[x] `SearchActor.functionStart` in `src/Server/SearchActor.fs` builds a pool `ActorFunction`. That body calls `CoreDoor.actorStop`. `SearchActor.functionActorAt` and `SearchActor.searchActorAt` are shared `ActorFunction` values. `functionActorAt` passes `changes.actorStop`. `searchActorAt` calls `changes.actorStop` with `ActorSucceeded`.
[x] `TestActor.dispatch` in `src/Server/TestActor.fs` calls `CoreDoor.actorStop`. `TestActor.actorFn` is `dispatch`. `TestActor.runBehavior` calls `changes.actorStop`. `TestActor.functionActor` is the pool `ActorFunction`. `TestActor.functionActorAt` is the shared `ActorFunction`. Both call `runBehavior`.
[x] `CoreMailboxLoop.poolActor` in `src/Server/Core/CoreMailboxLoop.fs` calls the shared `ActorFunction` and returns `Async<unit>`.
[x] `EventJson.stubActor` in `src/Shared/EventJson.fs` is an `ActorFunction` that returns unit.
[x] `CoreDoor.actorStop`, `CoreDoor.actorChanges`, and `ActorChanges.actorStop` stay. This ticket still calls them.

### Existing tests

[x] ``function start reply is bookkeeping without waiting for an Actor body`` in `tests/Server.Tests/CoreMsgActorCasesTests.fs` starts an `ActorFunction` that returns unit, then asserts the focus is in `pool.liveActorNodeIds`.
[x] ``ActorOrder places the actor on the thread pool`` in `tests/Server.Tests/CoreMsgActorCasesTests.fs` starts an `ActorFunction` that returns unit, then asserts `pool.isLive`.
[x] `AskCancelHarness.postChildThenWait` in `tests/Server.Tests/AskCancelHarness.fs` posts one child, then sleeps. The row stays live for the test.
[x] `CoreActorPoolDeliverTests.liveSessionActor` in `tests/Server.Tests/CoreActorPoolDeliverTests.fs` records the session, then sleeps.
[x] ``Actor getEventId surfaces persist error instead of Revision 0`` and ``Actor postChange on scheduled handle reaches persist`` in `tests/Server.Tests/ActorCoreChangesDoorTests.fs` pass an `ActorFunction` that records the observation and then ends. Those bodies do not call `actorStop`.
[x] ``coreMailboxStartActorFromFunction places StartActor from the selector`` in `tests/Server.Tests/ActorOrderFromStartTests.fs` starts an `ActorFunction` that returns unit. The assertions read the `Order.StartActor` row.
[x] ``ActorOrder round-trips on the wire with stubActor`` in `tests/Shared.Tests/EventJsonTests.fs` builds two `ActorFunction` values and does not run them.
[x] `createHost` in `tests/Server.Tests/CoreMailboxDoorTests.fs` registers an `ActorFn` named `root` that returns unit. No test starts that name.
[x] ``ActorStop reads the carrier graph once`` in `tests/Server.Tests/QueryActorTests.fs` starts `SearchActor.functionStart` through `pool.startFunction` and `pool.schedule`. It reads `EventBody.ActorStop` of `ActorQuery`.

### Copy-from

[x] `CoreActorPool.runSchedule` already starts the body and does not read a result. `Async.Ignore` is the discard that keeps that shape when the body returns `ActorResult`.

### Overlap

[x] [23 — Server search works](../../online-search/issues/23-server-search-works.md) is building in parallel and touches `src/Server/SearchActor.fs`.
[ ] This ticket's `SearchActor` edits are the stop calls in `functionStart`, `functionActorAt`, and `searchActorAt`. `SearchActor.reply`, `SearchActor.actorStart`, and `SearchActor.oneReply` stay.
[x] [24 — Post Notice.SearchStopped](../../online-search/issues/24-post-searchstopped-notice.md) is owned by AmbotX. Its build waits until [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md) lands. This ticket does not edit that file. `SearchActor.searchActorAt` stays the Find actor. The stop call stays in this ticket's return change.

## Inputs

### Working

[x] The shared `ActorFunction` in `src/Shared/History.fs` is the live start contract.
[x] The pool `ActorFunction` in `src/Server/Core/CoreActorPool.fs` is the wrapper `runSchedule` calls for `FunctionPending`.
[x] `ActorFn` in `src/Server/Core/CoreActorPool.fs` is the leftover id-bag contract. `runSchedule` still starts `BagPending`.
[x] `GithubTransportActor.finishActor` calls `ActorChanges.actorStop`.
[x] `RunAgentActor.stop` calls `ActorChanges.actorStop`.
[x] `SearchActor.functionStart`, `SearchActor.functionActorAt`, and `SearchActor.searchActorAt` call a stop.
[x] `TestActor.dispatch` and `TestActor.runBehavior` call a stop.
[x] `CoreMailboxApi.actorStop` in `src/Server/Core/CoreMailboxApi.fs` stays. `CoreDoor.actorStop` and `CoreDoor.actorChanges` in `src/Server/Core/CoreDoor.fs` stay. `CoreMailboxActors.dispatchActorStop` in `src/Server/Core/CoreMailboxActors.fs` stays.
[ ] Each of those producers returns the same `ActorResult` it passes to the stop.
[ ] A test body that is started, and that does not assert a stop, waits on `Async.CancellationToken` after its observation. The row stays live. `runSchedule` cancels that wait when `finish` runs.
[ ] ``function start reply is bookkeeping without waiting for an Actor body`` and ``ActorOrder places the actor on the thread pool`` keep their assertions. The body waits on `Async.CancellationToken`.
[ ] `EventJson.stubActor` and the two unrun `ActorFunction` values in ``ActorOrder round-trips on the wire with stubActor`` return `ActorSucceeded`.

### Reference

[x] The 2026-10-24 Actor-end decision is in the project file. The One job line links it.
[x] [Size limit](../../context/size-limit.md). Alan, 2026-10-24, accepted this ticket over the test-file limit. The accepted count was 9 test files. The tree now has 7 test files that hold a body of this type.

## Do not load

[x] `CoreMsg.ActorStop`. 114 — Remove CoreMsg.ActorStop removes that case.
[x] `CoreMailboxLoop.dispatchItem`. [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md) posts the notice.
[x] `CoreMsg.RecordSearchStop`. 24 — Post Notice.SearchStopped owns that case.
[x] `SearchActor.reply`, `SearchActor.actorStart`, and `SearchActor.oneReply`. 23 — Server search works owns the Find reply path.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `ActorFunction` in `src/Shared/History.fs` to return `Async<ActorResult>`.
3. [ ] Change `ActorFunction` and `ActorFn` in `src/Server/Core/CoreActorPool.fs` to return `Async<ActorResult>`.
4. [ ] `CoreMailboxLoop.poolActor` returns the shared actor's `ActorResult`.
5. [ ] `runSchedule` starts `work |> Async.Ignore` on the row cancellation token.
6. [ ] `GithubTransportActor.finishActor` calls `changes.actorStop` with the outcome, then returns that outcome.
7. [ ] `RunAgentActor.stop` calls `changes.actorStop`, then `RunAgentActor.actorFn` returns that result.
8. [ ] `SearchActor.functionStart`, `SearchActor.functionActorAt`, and `SearchActor.searchActorAt` call the same stop, then return that result. The other `SearchActor` functions stay.
9. [ ] `TestActor.dispatch` and `TestActor.runBehavior` call the same stop, then return that result.
10. [ ] `EventJson.stubActor` returns `ActorSucceeded`.
11. [ ] Each started test body that must stay live waits on `Async.CancellationToken` after its observation. The assertions stay.

## Outputs

[ ] Source files: `src/Shared/History.fs`, `src/Server/Core/CoreActorPool.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/GithubTransportActor.fs`, `src/Server/RunAgentActor.fs`, `src/Server/SearchActor.fs`, `src/Server/TestActor.fs`, and `src/Shared/EventJson.fs`. 8 source files.
[ ] Test files: `tests/Server.Tests/CoreMsgActorCasesTests.fs`, `tests/Server.Tests/AskCancelHarness.fs`, `tests/Server.Tests/CoreActorPoolDeliverTests.fs`, `tests/Server.Tests/ActorCoreChangesDoorTests.fs`, `tests/Server.Tests/ActorOrderFromStartTests.fs`, `tests/Shared.Tests/EventJsonTests.fs`, and `tests/Server.Tests/CoreMailboxDoorTests.fs`. 7 test files. Over the 4-test limit. The 2026-10-24 acceptance covers this ticket.
[ ] No test assertion changes.

## Stop when

[ ] Rewrite `113-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit removes a stop call.
[ ] Stop when the edit changes `EventBody.ActorStop`, `Notice.ActorStopped`, or an `ActorResult`.
[ ] Stop when the edit changes a `SearchActor` function other than `functionStart`, `functionActorAt`, and `searchActorAt`.
[ ] Stop when the edit needs a file outside Inputs.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``function start reply is bookkeeping without waiting for an Actor body``
  - `tests/Server.Tests`: ``ActorOrder places the actor on the thread pool``
  - `tests/Server.Tests`: ``ActorStop reads the carrier graph once``
  - `tests/Server.Tests`: ``mailbox appends StartActor and ActorStop in lifecycle order``
  - `tests/Shared.Tests`: ``ActorOrder round-trips on the wire with stubActor``
[ ] All executed tests pass.
[ ] Rewrite `113-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
