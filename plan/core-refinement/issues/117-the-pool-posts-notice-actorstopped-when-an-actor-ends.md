# 117 — The pool posts Notice.ActorStopped when an Actor ends
**Status**: defined
One job: The pool posts `Notice.ActorStopped` when an Actor's work returns. The Actor stops sending the stop. This is a strict refactor. The stop row keeps `Authority "Actor"`, so the observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results stay the same. ``mailbox appends StartActor and ActorStop in lifecycle order`` stays unchanged. The reason is the 2026-10-24 decision in [core-refinement](../project.md), and Alan, 2026-11-13, on the stop-row authority.

Type: coding
Blockers: [113 — Actors return their ActorResult](113-actors-return-their-actorresult.md). [81 — Log Actor stops as Notices](81-log-actor-and-search-stops-as-notices.md) is `coded`.

## Scope

Slice: one change. `runSchedule` reads the `ActorResult` and posts `Notice.ActorStopped`. The stop calls leave the Actor bodies.

[ ] The build keeps the observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results the same.

### Signatures

[x] `CoreActorPool.runSchedule` in `src/Server/Core/CoreActorPool.fs` is the schedule function. After 113 it starts `work |> Async.Ignore`.
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

[x] `GithubTransportActor.finishActor` calls `changes.actorStop`.
[x] `RunAgentActor.stop` calls `changes.actorStop`. `RunAgentActor.actorFn` calls `stop`.
[x] `SearchActor.functionStart` calls `CoreDoor.actorStop`. `SearchActor.functionActorAt` passes `changes.actorStop`. `SearchActor.searchActorAt` calls `changes.actorStop` with `ActorSucceeded`.
[x] `TestActor.dispatch` calls `CoreDoor.actorStop`. `TestActor.runBehavior` calls `changes.actorStop`.
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
[ ] This ticket's `SearchActor` edits are the stop calls in `functionStart`, `functionActorAt`, and `searchActorAt`.
[x] [24 — Post Notice.SearchStopped](../../online-search/issues/24-post-searchstopped-notice.md) is owned by AmbotX. Its build waits until 114 lands. This ticket does not edit that file. `searchActorAt` loses its `changes.actorStop` call here. `CoreMsg.RecordSearchStop` stays.

### Envelope

[x] Alan, 2026-11-13: the pool-posted stop row keeps `Authority "Actor"`.
[x] `CoreEventDispatch.actorStop` stores the caller authority. An Actor caller stores `Authority "Actor"`.
[x] `CoreEventDispatch.logNotice` stores `Authority ""`.
[x] ``mailbox appends StartActor and ActorStop in lifecycle order`` asserts `Authority "Actor"` and the stop body. That test stays unchanged.
[ ] The `Notice.ActorStopped` arm logs through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller` of the live secret, the actor node id, and the result. The stored authority is `Authority "Actor"`. Every other notice keeps `logNotice`.

### Known limit

[x] Alan, 2026-10-24, accepted the same-focus limit for now. `dispatchItem` finds the run by actor node id. A new run can start on that id after the Actor ends and before `Notice.ActorStopped` is handled. That notice finishes the new run.

## Inputs

### Working

[x] After 113, `runSchedule` discards the `ActorResult`.
[x] The four Actor modules still call a stop. The producers are `finishActor`, `RunAgentActor.stop`, `SearchActor.functionStart`, `SearchActor.functionActorAt`, `SearchActor.searchActorAt`, `TestActor.dispatch`, and `TestActor.runBehavior`.
[x] `CoreMailboxLoop.dispatchItem` logs every notice, then calls `applyNotice`.
[x] `pool.finish` cancels the row token. Work started with that token does not reach a later line.
[x] `runCancel` logs `Notice.ActorStopped` of the focus and `ActorCancelled` through the `EventBody.ActorStop` shell, then calls `pool.finish`.
[ ] `runSchedule` runs the work. On a returned result it posts `Notice.ActorStopped` of `row.actorNodeId` and that result through `CoreMailboxApi.postNotice` on `coreChanges.host`.
[ ] Work that is cancelled posts nothing.
[ ] For `Notice.ActorStopped`, `dispatchItem` calls `pool.trySecretForActorNode`. A secret logs through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller` of that secret, then calls `pool.finish` with that secret and the result. The stored authority is `Authority "Actor"`. No secret logs nothing and finishes nothing.
[ ] Every other notice stays on today's log-then-apply path.
[ ] The Actor bodies return the result and do not call a stop. `RunAgentActor.stop` leaves.

### Reference

[x] [Message shape](../cqrs-messages.md) decision 20 **Type cleanup**. The pool posts `Notice.ActorStopped` when an Actor ends.
[x] The 2026-10-24 known limit is in the project file. The One job line links it.

## Do not load

[x] `CoreMsg.ActorStop`, `CoreMailboxApi.actorStop`, `CoreDoor.actorStop`, `CoreDoor.actorChanges`, and `ActorChanges.actorStop`. 114 — Remove CoreMsg.ActorStop removes them after the tests leave them.
[x] `CoreMsg.RecordSearchStop` stays. 24 — Post Notice.SearchStopped owns that case. `CoreEventDispatch.actorStop` stays, and the pool-posted stop uses it for the log write.
[x] `SearchActor.reply`, `SearchActor.actorStart`, and `SearchActor.oneReply`.
[x] `CoreMailboxLoad`. Load dispatch stays there.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] `runSchedule` binds the `ActorResult`, then posts `Notice.ActorStopped` of `row.actorNodeId` and that result.
3. [ ] A cancelled body posts nothing.
4. [ ] `dispatchItem` handles `Notice.ActorStopped` with `trySecretForActorNode`. A secret logs through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller` of that secret, the actor node id, and the result, then calls `pool.finish`. The stored authority is `Authority "Actor"`. No secret logs nothing and finishes nothing.
5. [ ] Other notices keep today's `logNotice` order.
6. [ ] `applyNotice` stays `Ok ()` for `Notice.ActorStopped`.
7. [ ] `GithubTransportActor.finishActor` returns the outcome and does not call `changes.actorStop`.
8. [ ] `RunAgentActor.actorFn` returns the result. `RunAgentActor.stop` leaves.
9. [ ] `SearchActor.functionStart`, `SearchActor.functionActorAt`, and `SearchActor.searchActorAt` return the result and do not call a stop.
10. [ ] `TestActor.dispatch` and `TestActor.runBehavior` return the result and do not call a stop.
11. [ ] Add the two door tests in Tests. ``mailbox appends StartActor and ActorStop in lifecycle order`` stays unchanged.

## Outputs

[ ] Source files: `src/Server/Core/CoreActorPool.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/GithubTransportActor.fs`, `src/Server/RunAgentActor.fs`, `src/Server/SearchActor.fs`, and `src/Server/TestActor.fs`. 6 source files.
[ ] Test file: `tests/Server.Tests/CoreMailboxDoorTests.fs`. 1 test file. `tests/Server.Tests/Issue41CoreMailboxTests.fs` stays unchanged.
[ ] The new tests are ``an Actor that returns logs Notice.ActorStopped`` and ``ActorStopped for an idle focus logs nothing``.

## Stop when

[ ] Rewrite `117-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the stop row's authority is other than `Authority "Actor"`.
[ ] Stop when the edit changes ``mailbox appends StartActor and ActorStop in lifecycle order``.
[ ] Stop when the edit changes the stop body or the `ActorResult`.
[ ] Stop when the edit removes `CoreMsg.ActorStop` or `CoreMailboxApi.actorStop`.
[ ] Stop when the edit changes a `SearchActor` function other than `functionStart`, `functionActorAt`, and `searchActorAt`.
[ ] Stop when the edit needs a file outside Inputs.

## Tests

[ ] Add ``an Actor that returns logs Notice.ActorStopped`` in `tests/Server.Tests/CoreMailboxDoorTests.fs`. A `root` `ActorFunction` returns `ActorSucceeded` and does not call a stop. The log holds the start, then `Notice.ActorStopped` of the focus and `ActorSucceeded`. That id is absent from `pool.liveActorNodeIds`.
[ ] Add ``ActorStopped for an idle focus logs nothing``. The test posts `Notice.ActorStopped` with `CoreMailboxApi.postNotice` for a focus with no run. The log gains no entry.
[ ] ``actorStop after cancel does not emit a root Focus ActorStop`` keeps one stop in the log.
[ ] ``mailbox appends StartActor and ActorStop in lifecycle order`` stays unchanged, including `Authority "Actor"` and the stop body.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``an Actor that returns logs Notice.ActorStopped``
  - `tests/Server.Tests`: ``ActorStopped for an idle focus logs nothing``
  - `tests/Server.Tests`: ``actorStop after cancel does not emit a root Focus ActorStop``
  - `tests/Server.Tests`: ``mailbox appends StartActor and ActorStop in lifecycle order``
  - `tests/Server.Tests`: ``ActorStop reads the carrier graph once``
[ ] All executed tests pass.
[ ] Rewrite `117-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
