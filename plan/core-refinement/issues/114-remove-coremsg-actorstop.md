# 114 — Remove CoreMsg.ActorStop
**Status**: coded
One job: `CoreMsg.ActorStop` leaves. The stop that remains is `Notice.ActorStopped`, posted by the pool. This is a strict refactor. The observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results stay the same. The reason is the 2026-10-24 decision in [core-refinement](../project.md): an Actor ends, and the pool posts the stop.

Type: coding
Blockers: [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md). [118 — Test Actors return their result](118-test-actors-return-their-result.md) is superseded by this ticket. Alan, 2026-11-13.

## Scope

Slice: contract. The sent stop leaves. `CoreDoor.actorStop`, `CoreMailboxApi.actorStop`, `ActorChanges.actorStop`, `CoreMailboxActors.dispatchActorStop`, and `CoreMsg.ActorStop` leave.

[x] The build keeps the observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results the same.

### Signatures

[x] `CoreMsg` in `src/Server/Core/CoreMsg.fs` is `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[x] `CoreMailboxApi.actorStop` has left `src/Server/Core/CoreMailboxApi.fs`.
[x] `CoreMailboxLoop.runMsg` in `src/Server/Core/CoreMailboxLoop.fs` matches `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[x] `CoreMailboxLoop.messageCaller` in `src/Server/Core/CoreMailboxLoop.fs` reads the caller from `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[x] `CoreMailboxEvents.operationContext` in `src/Server/Core/CoreMailboxEvents.fs` matches `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[x] `CoreMailboxEvents.replyFailure` in `src/Server/Core/CoreMailboxEvents.fs` replies on `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[x] `CoreDoor.actorStop` has left `src/Server/Core/CoreDoor.fs`.
[x] `CoreDoor.actorChanges` fills `postChanges`, `graphAtEvent`, `loadNode`, `postNotice`, `eventsSince`, and `postGraphOnly`.
[x] `ActorChanges` in `src/Shared/History.fs` is `postChanges`, `graphAtEvent`, `loadNode`, `postNotice`, `eventsSince`, and `postGraphOnly`.
[x] `CoreMailboxActors.dispatchActorStop` has left `src/Server/Core/CoreMailboxActors.fs`.
[x] `CoreMailboxLoop.runMsg` matches `RecordSearchStop` and calls `CoreEventDispatch.actorStop`. That arm stays.

### Match sites

[x] ``ActorStopped Notice drops live row`` and ``ActorStopped Notice is logged and drops live row`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` post `Notice.ActorStopped` with `CoreMailboxApi.postNotice`.
[x] `postActorStop` in `tests/Server.Tests/CoreMsgActorCasesTests.fs` posts `Notice.ActorStopped` with `CoreMailboxApi.postNotice`, then reads the log.
[x] ``actorStop after cancel does not emit a root Focus ActorStop`` in `tests/Server.Tests/CancelByFocusTests.fs` posts `Notice.ActorStopped` after the Cancel.
[x] No Actor body calls a stop. `CoreActorPool.runReturned` binds the `ActorResult`. `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped` unless the row token is cancelled.
[x] ``ActorStop with Test credentials is refused``, ``ActorStop with Browser credentials is refused``, and ``ActorStop with Parse credentials is refused`` have left `tests/Server.Tests/CoreMsgActorCasesTests.fs`.
[x] ``fetch completes before the workspace lock then the pull merges and marks`` in `tests/Server.Tests/GithubTransportLoadPullTests.fs` builds an `ActorChanges` record with `postChanges`, `graphAtEvent`, `loadNode`, `postNotice`, `eventsSince`, and `postGraphOnly`.
[x] ``mailbox appends StartActor and ActorStop in lifecycle order`` starts `TestActor.functionActorAt`. It does not call `CoreMailboxApi.actorStop`. `TestActor.runBehavior` returns the result. The pool posts `Notice.ActorStopped`.
[x] `CoreActorPool.postStopUnlessCancelled` posts `Notice.ActorStopped` of `row.actorNodeId` and the returned result unless the row token is cancelled. A cancelled row posts nothing.
[x] `runMixedHello` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` starts `TestActor.functionActorAt`. It does not call `CoreMailboxApi.actorStop`.

### Existing tests

[x] The three credential-refusal tests have left `tests/Server.Tests/CoreMsgActorCasesTests.fs`.
[x] ``actorStop after cancel does not emit a root Focus ActorStop`` posts `Notice.ActorStopped` after Cancel and expects one stop in the log.
[x] `postActorStop` posts `Notice.ActorStopped` with `CoreMailboxApi.postNotice`, then reads the log.
[x] ``ActorStopped Notice drops live row`` and ``ActorStopped Notice is logged and drops live row`` post `Notice.ActorStopped` with `CoreMailboxApi.postNotice`.
[x] `poolForFixedSecret` in `tests/Server.Tests/TestBackend.fs` sets `trySecretForActorNode` to a function that returns `None`. The two renamed door tests return the live secret from that function.
[x] `CoreMailboxLoop.dispatchItem` sends `Notice.ActorStopped` to `CoreMailboxLoop.logActorStopped`. A secret from `CoreActorPool.trySecretForActorNode` logs through `CoreEventDispatch.actorStop` with `CoreDoor.actorCaller`, then `CoreActorPool.finish`. No secret logs nothing and finishes nothing.

### Copy-from

[x] ``ActorStopped for an idle focus logs nothing`` in 117 — The pool posts Notice.ActorStopped when an Actor ends posts `Notice.ActorStopped` with `CoreMailboxApi.postNotice` for a focus with no run. The log gains no entry.
[x] The two door tests, `postActorStop`, and the Cancel test use that post for a focus that has a run.

### Overlap

[x] [24 — Remove CoreMsg.RecordSearchStop](../../online-search/issues/24-remove-coremsg-recordsearchstop.md) This ticket does not edit that file. Its build waits until this ticket lands.
[x] 24 and this ticket both touch `src/Server/Core/CoreMsg.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/Core/CoreMailboxEvents.fs`, and `src/Server/Core/CoreMailboxApi.fs`. This ticket removes `CoreMsg.ActorStop` only. `RecordSearchStop` stays for 24.
[x] [23 — Server search works](../../online-search/issues/23-server-search-works.md) is building in parallel on `src/Server/SearchActor.fs`.
[x] 117 — The pool posts Notice.ActorStopped when an Actor ends edited `queryActorBody`, `functionStart`, `functionActorAt`, and `searchActorAt` in `src/Server/SearchActor.fs`. `SearchActor.actorCaller` has left.
[x] This ticket leaves `src/Server/SearchActor.fs` unchanged.

## Inputs

### Working

[x] `CoreMsg` is `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[x] `runMsg`, `messageCaller`, `operationContext`, and `replyFailure` match `StartLoadSaveCommand`, `RecordSearchStop`, and `PostEvent`.
[x] `CoreDoor.actorChanges` fills `postChanges`, `graphAtEvent`, `loadNode`, `postNotice`, `eventsSince`, and `postGraphOnly`.
[x] `CoreEventDispatch.actorStop` still serves `RecordSearchStop`.
[x] `CoreMsg.ActorStop` and its four match arms have left.
[x] `CoreDoor.actorStop`, `CoreMailboxApi.actorStop`, `ActorChanges.actorStop`, and `CoreMailboxActors.dispatchActorStop` have left.
[x] ``ActorStopped Notice drops live row`` posts `Notice.ActorStopped` of the focus with `CoreMailboxApi.postNotice`. Its recording pool's `trySecretForActorNode` returns the live secret. The `finish` assertion stays.
[x] ``ActorStopped Notice is logged and drops live row`` posts the same notice, then reads one stop of that focus. Its recording pool's `trySecretForActorNode` returns the live secret. The stop-body assertion stays. The stop row's authority is `Authority "Actor"`.
[x] `postActorStop` posts `Notice.ActorStopped` with `CoreMailboxApi.postNotice`, then reads the log.
[x] ``actorStop after cancel does not emit a root Focus ActorStop`` posts `Notice.ActorStopped` after the Cancel. The log holds one stop.
[x] The three credential-refusal tests have left. A notice has no caller to refuse.
[x] The `ActorChanges` record in ``fetch completes before the workspace lock then the pull merges and marks`` is `postChanges`, `graphAtEvent`, `loadNode`, `postNotice`, `eventsSince`, and `postGraphOnly`.

### Reference

[x] The 2026-10-24 Actor-end decision is in the project file. The One job line links it.
[x] [Size limit](../../context/size-limit.md).

## Do not load

[x] `CoreMsg.RecordSearchStop`, its match arms, and `CoreEventDispatch.actorStop`. 24 owns that replacement.
[x] `src/Server/SearchActor.fs`.
[x] `CoreMailboxActors.runCancel`. A Cancel still logs `Notice.ActorStopped` of the focus and `ActorCancelled`.
[x] `EventBody.ActorStop` and `Notice.ActorStopped` in `src/Shared/History.fs`. The alias and the notice stay.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Rename ``CoreMailboxApi.actorStop with valid credential drops live row`` to ``ActorStopped Notice drops live row``. The test posts `Notice.ActorStopped` of the focus with `CoreMailboxApi.postNotice`. Its recording pool's `trySecretForActorNode` returns the live secret.
3. [x] Rename ``CoreMailboxApi.actorStop appends ActorStop and drops live row`` to ``ActorStopped Notice is logged and drops live row``. The test posts the same notice, then reads one stop of that focus. Its recording pool's `trySecretForActorNode` returns the live secret.
4. [x] `postActorStop` posts `Notice.ActorStopped` of the focus with `CoreMailboxApi.postNotice`, then reads the log.
5. [x] ``actorStop after cancel does not emit a root Focus ActorStop`` posts `Notice.ActorStopped` after the Cancel. The log holds one stop.
6. [x] Remove the three credential-refusal tests.
7. [x] Remove `actorStop` from the `ActorChanges` record in `GithubTransportLoadPullTests`.
8. [x] Remove `CoreDoor.actorStop`. `CoreDoor.actorChanges` fills the remaining fields.
9. [x] Remove `ActorChanges.actorStop` from `src/Shared/History.fs`.
10. [x] Remove `CoreMailboxApi.actorStop`.
11. [x] Remove `CoreMailboxActors.dispatchActorStop`.
12. [x] Remove `ActorStop` from `CoreMsg` and from `runMsg`, `messageCaller`, `operationContext`, and `replyFailure`. The `RecordSearchStop` arms stay.

## Outputs

[x] Source files: `src/Server/Core/CoreMsg.fs`, `src/Server/Core/CoreDoor.fs`, `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoop.fs`, `src/Server/Core/CoreMailboxEvents.fs`, `src/Server/Core/CoreMailboxActors.fs`, and `src/Shared/History.fs`. 7 source files.
[x] Test files: `tests/Server.Tests/CoreMailboxDoorTests.fs`, `tests/Server.Tests/CoreMsgActorCasesTests.fs`, `tests/Server.Tests/CancelByFocusTests.fs`, and `tests/Server.Tests/GithubTransportLoadPullTests.fs`. 4 test files. That is the size-limit count.

## Stop when

[ ] Rewrite `114-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit removes `RecordSearchStop` or `CoreEventDispatch.actorStop`.
[ ] Stop when the edit changes `EventBody.ActorStop`, `Notice.ActorStopped`, or an `ActorResult`.
[ ] Stop when the edit changes `src/Server/SearchActor.fs`.
[ ] Stop when the edit needs a file outside Inputs.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``ActorStopped Notice drops live row``
  - `tests/Server.Tests`: ``ActorStopped Notice is logged and drops live row``
  - `tests/Server.Tests`: `CoreMsgActorCasesTests`
  - `tests/Server.Tests`: ``actorStop after cancel does not emit a root Focus ActorStop``
  - `tests/Server.Tests`: ``mailbox appends StartActor and ActorStop in lifecycle order``
  - `tests/Server.Tests`: ``fetch completes before the workspace lock then the pull merges and marks``
[x] All executed tests pass.
[x] Rewrite `114-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
