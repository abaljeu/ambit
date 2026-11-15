# 24 — Remove CoreMsg.RecordSearchStop

Ticket: [24 — Remove CoreMsg.RecordSearchStop](24-remove-coremsg-recordsearchstop.md)

Check: pass

`CoreMsg` is `StartLoadSaveCommand` and `PostEvent`. `CoreMsg.RecordSearchStop` has left.

These symbols have left:

- the `RecordSearchStop` case in `src/Server/Core/CoreMsg.fs`
- the `RecordSearchStop` arms of `CoreMailboxLoop.runMsg` and `CoreMailboxLoop.messageCaller`
- the `RecordSearchStop` arms of `CoreMailboxEvents.operationContext` and `CoreMailboxEvents.replyFailure`
- `CoreMailboxApi.recordSearchStop`

`RouteRegistration.searchDoor.recordStop` returns unit. ``postSearch records ActorStart at the root`` sets `recordStop` to a closure that returns unit. The assertion stays on `EventBody.ActorStop` of the root with `ActorSucceeded`. That row is `Notice.ActorStopped` from `CoreActorPool.postStopUnlessCancelled`.

`CoreMailboxApi.recordSearchStart` still places `Order.StartActor` for the actor `search`. `Notice` cases are unchanged. `CoreMailboxLoop.logActorStopped` is unchanged.

## Tests

- `scripts/agent-test.sh --filter` for ``postSearch records ActorStart at the root`` passed 1, failed 0, skipped 0.
- `scripts/agent-test.sh` with no arguments passed 12 in `tests/Server.Tests` (`SearchActorTests`), failed 0, skipped 0.
- `scripts/agent-test.sh --full` passed Server 775, failed 0, skipped 1, total 776. The skip is ``Change.upload structure persistence preserves existing file mtime``.
- The same run passed Shared 1946, failed 0, skipped 3, total 1949. The skips are ``listForPush without git still returns walk``, ``reuses existing owned path without duplicating``, and ``planServerFilePresentOps transitions only matching absent files``.
- The completed full run set `DOTNET_ThreadPool_ForceMinWorkerThreads` and `COMPlus_ThreadPool_ForceMinWorkerThreads` to 64. An earlier full run on this 4-CPU host stalled in `ServerTestReset.resetBetweenTests` while `ParseThread` join waited and `AgentRunnerFake.waitFakeStream` held thread-pool workers.
- `src/Client/Gambol.Client.fsproj` is an executable. The solution has no Client test project. A `FullyQualifiedName~Client` filter passed 63 in `tests/Shared.Tests` and 25 in `tests/Server.Tests`, failed 0.

## Present pages

[24 — Remove CoreMsg.RecordSearchStop](24-doc.md) records these pages. Each page matches the program.

- [Online search architecture](../arch.md) names one `Notice.ActorStopped` from `CoreActorPool.postStopUnlessCancelled`.
- [Online search spec](../spec.md) section **Search Actor door** says `recordStart` places `Order.StartActor` for the actor `search`, and `recordStop` returns unit.
- [Online search](../project.md) marks this ticket `coded`.
- [Core refinement](../../core-refinement/project.md) says `CoreMsg` is `StartLoadSaveCommand` and `PostEvent`.

Open question: none.
