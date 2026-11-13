# 114 — Remove CoreMsg.ActorStop
**Status**: defined
One job: `CoreMsg.ActorStop` leaves. The stop that remains is `Notice.ActorStopped`, posted by the pool. This is a strict refactor. The observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results stay the same. The reason is the 2026-10-24 decision in [core-refinement](../project.md): an Actor ends, and the pool posts the stop.

Type: coding
Blockers: [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md). [118 — Test Actors return their result](118-test-actors-return-their-result.md) is superseded by this ticket. Alan, 2026-11-13.

## Scope

Slice: contract. The sent stop leaves. `CoreDoor.actorStop`, `CoreMailboxApi.actorStop`, `ActorChanges.actorStop`, `CoreMailboxActors.dispatchActorStop`, and `CoreMsg.ActorStop` leave.

[ ] The build keeps the observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results the same.

### Signatures

[x] `CoreMsg.ActorStop` in `src/Server/Core/CoreMsg.fs` is `caller`, `result`, and `AsyncReplyChannel<Result<unit, string>>`.
[x] `CoreMailboxApi.actorStop` in `src/Server/Core/CoreMailboxApi.fs` is the only builder. It posts `ActorStop(caller, result, channel)`.
[x] `CoreMailboxLoop.runMsg` in `src/Server/Core/CoreMailboxLoop.fs` matches `ActorStop` and calls `CoreMailboxActors.dispatchActorStop`. `CoreMailboxLoop.messageCaller` matches `ActorStop`.
[x] `CoreMailboxEvents.operationContext` and `CoreMailboxEvents.replyFailure` in `src/Server/Core/CoreMailboxEvents.fs` match `ActorStop`.
[x] `CoreDoor.actorStop` in `src/Server/Core/CoreDoor.fs` calls `CoreMailboxApi.actorStop`. `CoreDoor.actorChanges` sets `ActorChanges.actorStop` in `src/Shared/History.fs`.
[x] `CoreMailboxActors.dispatchActorStop` in `src/Server/Core/CoreMailboxActors.fs` admits `Authority "Actor"`, reads the focus with `CoreActorPool.getActorNodeId`, appends the stop with `CoreEventDispatch.actorStop`, and calls `pool.finish`.
[x] `CoreMailboxLoop.runMsg` matches `RecordSearchStop` and calls `CoreEventDispatch.actorStop`. That arm stays.

### Match sites

[x] The `CoreMailboxApi.actorStop` callers outside an Actor body are the two recording-pool tests in `tests/Server.Tests/CoreMailboxDoorTests.fs`, `postActorStop` in `tests/Server.Tests/CoreMsgActorCasesTests.fs`, and ``actorStop after cancel does not emit a root Focus ActorStop`` in `tests/Server.Tests/CancelByFocusTests.fs`.
[ ] After 117 — The pool posts Notice.ActorStopped when an Actor ends, no Actor body calls a stop.
[x] ``ActorStop with Test credentials is refused``, ``ActorStop with Browser credentials is refused``, and ``ActorStop with Parse credentials is refused`` in `tests/Server.Tests/CoreMsgActorCasesTests.fs` send `CoreMsg.ActorStop`.
[x] ``fetch completes before the workspace lock then the pull merges and marks`` in `tests/Server.Tests/GithubTransportLoadPullTests.fs` builds an `ActorChanges` record with an `actorStop` field.
[x] ``mailbox appends StartActor and ActorStop in lifecycle order`` starts `TestActor.functionActorAt`. It does not call `CoreMailboxApi.actorStop`. `TestActor.runBehavior` calls `ActorChanges.actorStop`.
[ ] After 117 — The pool posts Notice.ActorStopped when an Actor ends, the pool posts that stop.
[x] `runMixedHello` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` starts `TestActor.functionActorAt`. It does not call `CoreMailboxApi.actorStop`.

### Existing tests

[x] The three credential tests expect `Error CoreAuth.refuse` and no `finish` call.
[x] ``actorStop after cancel does not emit a root Focus ActorStop`` posts a stop after Cancel and expects one stop in the log.
[x] `postActorStop` is the helper those case tests call.
[x] ``CoreMailboxApi.actorStop with valid credential drops live row`` and ``CoreMailboxApi.actorStop appends ActorStop and drops live row`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` call `CoreMailboxApi.actorStop`.
[x] `poolForFixedSecret` in `tests/Server.Tests/TestBackend.fs` sets `trySecretForActorNode` to a function that returns `None`. Those two recording pools leave that function as `None`.
[ ] After 117 — The pool posts Notice.ActorStopped when an Actor ends, `CoreMailboxLoop.dispatchItem` finishes a run for `Notice.ActorStopped` when `CoreActorPool.trySecretForActorNode` returns a secret.

### Copy-from

[ ] ``ActorStopped for an idle focus logs nothing`` in 117 — The pool posts Notice.ActorStopped when an Actor ends posts `Notice.ActorStopped` with `CoreMailboxApi.postNotice`. The two door tests, `postActorStop`, and the Cancel test use that post for a focus that has a run.

### Overlap

[x] [24 — Remove CoreMsg.RecordSearchStop](../../online-search/issues/24-remove-coremsg-recordsearchstop.md) This ticket does not edit that file. Its build waits until this ticket lands.
[x] 24 and this ticket both touch `src/Server/Core/CoreMsg.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/Core/CoreMailboxEvents.fs`, and `src/Server/Core/CoreMailboxApi.fs`. This ticket removes `CoreMsg.ActorStop` only. `RecordSearchStop` stays for 24.
[x] [23 — Server search works](../../online-search/issues/23-server-search-works.md) is building in parallel on `src/Server/SearchActor.fs`.
[ ] This ticket leaves `src/Server/SearchActor.fs` unchanged. 117 — The pool posts Notice.ActorStopped when an Actor ends edits `queryActorBody`, `functionStart`, `functionActorAt`, and `searchActorAt`.

## Inputs

### Working

[x] `CoreMsg` still has `ActorStop` and `RecordSearchStop`.
[x] `runMsg`, `messageCaller`, `operationContext`, and `replyFailure` each match `ActorStop`.
[x] `ActorChanges.actorStop` is the field `CoreDoor.actorChanges` fills.
[x] `CoreEventDispatch.actorStop` still serves `RecordSearchStop`.
[ ] `CoreMsg.ActorStop` and its four match arms leave.
[ ] `CoreDoor.actorStop`, `CoreMailboxApi.actorStop`, `ActorChanges.actorStop`, and `CoreMailboxActors.dispatchActorStop` leave.
[ ] ``CoreMailboxApi.actorStop with valid credential drops live row`` is renamed ``ActorStopped Notice drops live row``. It posts `Notice.ActorStopped` of the focus with `CoreMailboxApi.postNotice`. Its recording pool's `trySecretForActorNode` returns the live secret. The `finish` assertion stays.
[ ] ``CoreMailboxApi.actorStop appends ActorStop and drops live row`` is renamed ``ActorStopped Notice is logged and drops live row``. It posts the same notice, then reads one stop of that focus. Its recording pool's `trySecretForActorNode` returns the live secret. The stop-body assertion stays.
[ ] `postActorStop` posts `Notice.ActorStopped` with `CoreMailboxApi.postNotice`, then reads the log.
[ ] ``actorStop after cancel does not emit a root Focus ActorStop`` posts `Notice.ActorStopped` after the Cancel. The log holds one stop.
[ ] The three credential-refusal tests leave. A notice has no caller to refuse.
[ ] The `ActorChanges` record in ``fetch completes before the workspace lock then the pull merges and marks`` drops the `actorStop` field.

### Reference

[x] The 2026-10-24 Actor-end decision is in the project file. The One job line links it.
[x] [Size limit](../../context/size-limit.md).

## Do not load

[x] `CoreMsg.RecordSearchStop`, its match arms, and `CoreEventDispatch.actorStop`. 24 owns that replacement.
[x] `src/Server/SearchActor.fs`.
[x] `CoreMailboxActors.runCancel`. A Cancel still logs `Notice.ActorStopped` of the focus and `ActorCancelled`.
[x] `EventBody.ActorStop` and `Notice.ActorStopped` in `src/Shared/History.fs`. The alias and the notice stay.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Rename ``CoreMailboxApi.actorStop with valid credential drops live row`` to ``ActorStopped Notice drops live row``. The test posts `Notice.ActorStopped` of the focus with `CoreMailboxApi.postNotice`. Its recording pool's `trySecretForActorNode` returns the live secret.
3. [ ] Rename ``CoreMailboxApi.actorStop appends ActorStop and drops live row`` to ``ActorStopped Notice is logged and drops live row``. The test posts the same notice, then reads one stop of that focus. Its recording pool's `trySecretForActorNode` returns the live secret.
4. [ ] `postActorStop` posts `Notice.ActorStopped` of the focus with `CoreMailboxApi.postNotice`, then reads the log.
5. [ ] ``actorStop after cancel does not emit a root Focus ActorStop`` posts `Notice.ActorStopped` after the Cancel. The log holds one stop.
6. [ ] Remove the three credential-refusal tests.
7. [ ] Remove `actorStop` from the `ActorChanges` record in `GithubTransportLoadPullTests`.
8. [ ] Remove `CoreDoor.actorStop`. `CoreDoor.actorChanges` no longer sets `actorStop`.
9. [ ] Remove `ActorChanges.actorStop` from `src/Shared/History.fs`.
10. [ ] Remove `CoreMailboxApi.actorStop`.
11. [ ] Remove `CoreMailboxActors.dispatchActorStop`.
12. [ ] Remove `ActorStop` from `CoreMsg` and from `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`. The `RecordSearchStop` arms stay.

## Outputs

[ ] Source files: `src/Server/Core/CoreMsg.fs`, `src/Server/Core/CoreDoor.fs`, `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/Core/CoreMailboxEvents.fs`, `src/Server/Core/CoreMailboxActors.fs`, and `src/Shared/History.fs`. 7 source files.
[ ] Test files: `tests/Server.Tests/CoreMailboxDoorTests.fs`, `tests/Server.Tests/CoreMsgActorCasesTests.fs`, `tests/Server.Tests/CancelByFocusTests.fs`, and `tests/Server.Tests/GithubTransportLoadPullTests.fs`. 4 test files. That is the size-limit count.

## Stop when

[ ] Rewrite `114-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit removes `RecordSearchStop` or `CoreEventDispatch.actorStop`.
[ ] Stop when the edit changes `EventBody.ActorStop`, `Notice.ActorStopped`, or an `ActorResult`.
[ ] Stop when the edit changes `src/Server/SearchActor.fs`.
[ ] Stop when the edit needs a file outside Inputs.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``ActorStopped Notice drops live row``
  - `tests/Server.Tests`: ``ActorStopped Notice is logged and drops live row``
  - `tests/Server.Tests`: `CoreMsgActorCasesTests`
  - `tests/Server.Tests`: ``actorStop after cancel does not emit a root Focus ActorStop``
  - `tests/Server.Tests`: ``mailbox appends StartActor and ActorStop in lifecycle order``
  - `tests/Server.Tests`: ``fetch completes before the workspace lock then the pull merges and marks``
[ ] All executed tests pass.
[ ] Rewrite `114-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
