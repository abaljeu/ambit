# 117 — The pool posts Notice.ActorStopped when an Actor ends
**Status**: `coded`
One job: The pool posts `Notice.ActorStopped` when an Actor's work returns. The Actor stops sending the stop. This is a strict refactor. The stop row keeps `Authority "Actor"`, so the observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results stay the same. ``mailbox appends StartActor and ActorStop in lifecycle order`` stays unchanged. The reason is the 2026-10-24 decision in [core-refinement](../project.md), and Alan, 2026-11-13, on the stop-row authority.

Type: coding
Blockers: [113 — Actors return their ActorResult](113-actors-return-their-actorresult.md). [81 — Log Actor stops as Notices](81-log-actor-and-search-stops-as-notices.md) is `coded`.

## Scope

Slice: one change. `runSchedule` reads the `ActorResult` and posts `Notice.ActorStopped`. The stop calls leave the Actor bodies.

[x] The build keeps the observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results the same.

### Signatures

[x] `CoreActorPool.runSchedule` in `src/Server/Core/CoreActorPool.fs` is the schedule function. It binds the `ActorResult` and posts `Notice.ActorStopped` unless the row token is cancelled.
[x] `CoreActorPool.trySecretForActorNode` in `src/Server/Core/CoreActorPool.fs` returns the secret for an actor node id. The live row field is `actorNodeId`.
[x] `CoreActorPool.finish` in `src/Server/Core/CoreActorPool.fs` drops the row for every `ActorResult`. `runFinish` cancels the row token.
[x] `CoreMailboxApi.postNotice` in `src/Server/Core/CoreMailboxApi.fs` posts `CoreQueue.Notice` and returns before the mailbox applies the notice.
[x] `CoreMailboxLoop.runMsg` and `CoreMailboxLoop.dispatchItem` are in `src/Server/Core/CoreMailboxLoop.fs`. `CoreMailboxLoad` in `src/Server/Core/CoreMailboxLoad.fs` holds Load dispatch.
[x] `CoreMailboxLoop.dispatchItem` logs every `CoreQueue.Notice` with `CoreEventDispatch.logNotice`, then calls `CoreMailboxLoop.applyNotice`.
[x] `CoreMailboxLoop.applyNotice` returns `Ok ()` for `Notice.ActorStopped`.
[x] `CoreEventDispatch.actorStop` in `src/Server/Core/CoreEventDispatch.fs` stores the caller authority on the stop row.
[x] `CoreEventDispatch.logNotice` stores `Authority ""` on a notice row.
[x] ``mailbox appends StartActor and ActorStop in lifecycle order`` in `tests/Server.Tests/Issue41CoreMailboxTests.fs` asserts the stop row's authority is `Authority "Actor"`.

### Match sites

[x] `GithubTransportActor.finishActor` returns the outcome and does not call `changes.actorStop`.
[x] `RunAgentActor.stop` has left. `RunAgentActor.actorFn` returns the result.
[x] `SearchActor.queryActorBody` returns the `ActorQuery` and does not call a stop. `SearchActor.functionStart` and `SearchActor.functionActorAt` return that async. `SearchActor.searchActorAt` returns `ActorSucceeded` and does not call a stop.
[x] `TestActor.dispatch` and `TestActor.runBehavior` return the result and do not call a stop.
[x] `CoreMailboxActors.dispatchActorStop` checks `Authority "Actor"`, appends the stop with `CoreEventDispatch.actorStop`, and calls `pool.finish`.
[x] `CoreMailboxActors.runCancel` appends `EventBody.ActorStop` of the focus and `ActorCancelled`, then calls `pool.finish`. A focus with no live run logs nothing.
[x] `CoreMailboxLoop.runMsg` still matches `CoreMsg.ActorStop` and calls `dispatchActorStop`. Tests call `CoreMailboxApi.actorStop` until [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md).

### Existing tests

[x] ``mailbox appends StartActor and ActorStop in lifecycle order`` starts `TestActor.functionActorAt` and asserts the stop body and `Authority "Actor"`.
[x] ``actorStop after cancel does not emit a root Focus ActorStop`` in `tests/Server.Tests/CancelByFocusTests.fs` calls `CoreMailboxApi.actorStop` after a Cancel. The log holds one stop.
[x] ``ActorStop reads the carrier graph once`` starts `SearchActor.functionStart` and reads `ActorQuery`.

### Copy-from

[x] `CoreEventDispatch.actorStop` is the stop-row append that stores the caller authority. `CoreEventDispatch.logNotice` is the append for every other notice. `CoreMailboxLoop.dispatchItem` is the notice arm. `CoreActorPool.trySecretForActorNode` is the live-row lookup `dispatchActorStop` does with `getActorNodeId`. `CoreDoor.actorCaller` builds the Actor caller from a secret.

### Overlap

[x] [23 — Server search works](../../online-search/issues/23-server-search-works.md) is building in parallel and touches `src/Server/SearchActor.fs`.
[x] This ticket's `SearchActor` edits are the stop calls in `queryActorBody`, `functionStart`, `functionActorAt`, and `searchActorAt`. `SearchActor.actorCaller` leaves with those calls.
[x] [24 — Remove CoreMsg.RecordSearchStop](../../online-search/issues/24-remove-coremsg-recordsearchstop.md) Its build waits until 114 lands. This ticket does not edit that file. `searchActorAt` loses its `changes.actorStop` call here. `CoreMsg.RecordSearchStop` stays.

### Envelope

[x] Alan, 2026-11-13: the pool-posted stop row keeps `Authority "Actor"`.
[x] `CoreEventDispatch.actorStop` stores the caller authority. An Actor caller stores `Authority "Actor"`.
[x] `CoreEventDispatch.logNotice` stores `Authority ""`.
[x] ``mailbox appends StartActor and ActorStop in lifecycle order`` asserts `Authority "Actor"` and the stop body. That test stays unchanged.
[x] The `Notice.ActorStopped` arm logs through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller` of the live secret, the actor node id, and the result. The stored authority is `Authority "Actor"`. Every other notice keeps `logNotice`.

### Known limit

[x] Alan, 2026-10-24, accepted the same-focus limit for now. `dispatchItem` finds the run by actor node id. A new run can start on that id after the Actor ends and before `Notice.ActorStopped` is handled. That notice finishes the new run.

## Inputs

### Working

[x] `runSchedule` binds the `ActorResult`. A returned result posts `Notice.ActorStopped`. A cancelled row posts nothing.
[x] The Actor bodies return the result and do not call a stop. `RunAgentActor.stop` has left.
[x] `CoreMailboxLoop.dispatchItem` logs `Notice.ActorStopped` through `CoreEventDispatch.actorStop`, then calls `pool.finish`. Every other notice keeps `logNotice`, then `applyNotice`.
[x] `CoreActorPool.finish` cancels the row token through `runFinish`.
[x] `TestBackend.waitCancelled` in `tests/Server.Tests/TestBackend.fs` returns `ActorSucceeded` after that token signals. These files call it: `tests/Server.Tests/ActorCoreChangesDoorTests.fs`, `tests/Server.Tests/ActorOrderFromStartTests.fs`, `tests/Server.Tests/AskCancelHarness.fs`, `tests/Server.Tests/CoreActorPoolDeliverTests.fs`, and `tests/Server.Tests/CoreMsgActorCasesTests.fs`.
[x] `runCancel` logs `Notice.ActorStopped` of the focus and `ActorCancelled` through the `EventBody.ActorStop` shell, then calls `pool.finish`.
[x] `runSchedule` runs the work. On a returned result it posts `Notice.ActorStopped` of `row.actorNodeId` and that result through `CoreMailboxApi.postNotice` on `coreChanges.host`.
[x] Work that is cancelled posts nothing.
[x] For `Notice.ActorStopped`, `dispatchItem` calls `pool.trySecretForActorNode`. A secret logs through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller` of that secret, then calls `pool.finish` with that secret and the result. The stored authority is `Authority "Actor"`. No secret logs nothing and finishes nothing.
[x] Every other notice stays on today's log-then-apply path.
[x] The Actor bodies return the result and do not call a stop. `RunAgentActor.stop` leaves.

### Reference

[x] [Message shape](../cqrs-messages.md) decision 20 **Type cleanup**. The pool posts `Notice.ActorStopped` when an Actor ends.
[x] The 2026-10-24 known limit is in the project file. The One job line links it.

## Do not load

[x] `CoreMsg.ActorStop`, `CoreMailboxApi.actorStop`, `CoreDoor.actorStop`, `CoreDoor.actorChanges`, and `ActorChanges.actorStop`. 114 — Remove CoreMsg.ActorStop removes them after the tests leave them.
[x] `CoreMsg.RecordSearchStop` stays. 24 — Remove CoreMsg.RecordSearchStop owns that case. `CoreEventDispatch.actorStop` stays, and the pool-posted stop uses it for the log write.
[x] `SearchActor.reply`, `SearchActor.actorStart`, and `SearchActor.oneReply`.
[x] `CoreMailboxLoad`. Load dispatch stays there.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] `runSchedule` binds the `ActorResult`, then posts `Notice.ActorStopped` of `row.actorNodeId` and that result.
3. [x] A cancelled body posts nothing.
4. [x] `dispatchItem` handles `Notice.ActorStopped` with `trySecretForActorNode`. A secret logs through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller` of that secret, the actor node id, and the result, then calls `pool.finish`. The stored authority is `Authority "Actor"`. No secret logs nothing and finishes nothing.
5. [x] Other notices keep today's `logNotice` order.
6. [x] `applyNotice` stays `Ok ()` for `Notice.ActorStopped`.
7. [x] `GithubTransportActor.finishActor` returns the outcome and does not call `changes.actorStop`.
8. [x] `RunAgentActor.actorFn` returns the result. `RunAgentActor.stop` leaves.
9. [x] `SearchActor.queryActorBody` returns the result and does not call a stop. `SearchActor.functionStart` and `SearchActor.functionActorAt` return that async. `SearchActor.searchActorAt` returns `ActorSucceeded` and does not call a stop.
10. [x] `TestActor.dispatch` and `TestActor.runBehavior` return the result and do not call a stop.
11. [x] Add the two door tests in Tests. ``mailbox appends StartActor and ActorStop in lifecycle order`` stays unchanged.

## Outputs

[x] Source files: `src/Server/Core/CoreActorPool.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/GithubTransportActor.fs`, `src/Server/RunAgentActor.fs`, `src/Server/SearchActor.fs`, and `src/Server/TestActor.fs`. 6 source files.
[x] Test files: `tests/Server.Tests/CoreMailboxDoorTests.fs` and `tests/Server.Tests/CoreMsgActorCasesTests.fs`. 2 test files. `tests/Server.Tests/Issue41CoreMailboxTests.fs` stays unchanged. ``function start yields its secret and a later stop is dropped`` waits with `TestBackend.waitCancelled` so the explicit stop still drops the live row.
[x] The new tests are ``an Actor that returns logs Notice.ActorStopped`` and ``ActorStopped for an idle focus logs nothing``.

## Stop when

[ ] Rewrite `117-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the stop row's authority is other than `Authority "Actor"`.
[ ] Stop when the edit changes ``mailbox appends StartActor and ActorStop in lifecycle order``.
[ ] Stop when the edit changes the stop body or the `ActorResult`.
[ ] Stop when the edit removes `CoreMsg.ActorStop` or `CoreMailboxApi.actorStop`.
[ ] Stop when the edit changes a `SearchActor` function other than `queryActorBody`, `functionStart`, `functionActorAt`, and `searchActorAt`.
[ ] Stop when the edit needs a file outside Inputs.

## Tests

[x] Add ``an Actor that returns logs Notice.ActorStopped`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`. A `root` `ActorFunction` returns `ActorSucceeded` and does not call a stop. The log holds the start, then `Notice.ActorStopped` of the focus and `ActorSucceeded`. That id is absent from `pool.liveActorNodeIds`.
[x] Add ``ActorStopped for an idle focus logs nothing``. The test posts `Notice.ActorStopped` with `CoreMailboxApi.postNotice` for a focus with no run. The log gains no entry.
[x] ``actorStop after cancel does not emit a root Focus ActorStop`` keeps one stop in the log.
[x] ``mailbox appends StartActor and ActorStop in lifecycle order`` stays unchanged, including `Authority "Actor"` and the stop body.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``an Actor that returns logs Notice.ActorStopped``
  - `tests/Server.Tests`: ``ActorStopped for an idle focus logs nothing``
  - `tests/Server.Tests`: ``actorStop after cancel does not emit a root Focus ActorStop``
  - `tests/Server.Tests`: ``mailbox appends StartActor and ActorStop in lifecycle order``
  - `tests/Server.Tests`: ``ActorStop reads the carrier graph once``
[x] All executed tests pass.
[x] Rewrite `117-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
