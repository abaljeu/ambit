# 25 — Remove the coreMailboxStartActor door
**Status**: coded
One job: `CoreMailboxApi.coreMailboxStartActor` is gone. No test calls it, and nothing places `Order.StartActorDeprecated` through it.

[x] [16 — Delete the old Order cases](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) deletes `Order.StartActorDeprecated`.

Type:

[x] coding

Blockers:

[x] [11 — Move door and case tests](../11-move-door-and-case-tests/11-move-door-and-case-tests-ticket.md). `coreMailboxStartActor` is gone from `CoreMailboxDoorTests.fs`, `CoreMsgActorCasesTests.fs`, and `AskCancelHarness.fs`.
[x] [12 — Move pool and issue tests](../12-move-pool-and-issue-tests/12-move-pool-and-issue-tests-ticket.md). `coreMailboxStartActor` is gone from `CoreActorPoolDeliverTests.fs`, `Issue41CoreMailboxTests.fs`, and `MailboxHistoryDurabilityTests.fs`.
[x] [13 — Move persist and command tests](../13-move-persist-and-command-tests/13-move-persist-and-command-tests-ticket.md). `coreMailboxStartActor` is gone from `PersistHandlersRestoreTests.fs`.
[x] [22 — Convert CoreMsg.PostEvent](../22-convert-coremsg-postevent/22-convert-coremsg-postevent-ticket.md). `coreMailboxStartActor` is gone from ``mailbox appends StartActor and ActorStop in lifecycle order`` in `Issue41CoreMailboxTests.fs`.

## Inputs

### Working

[x] `coreMailboxStartActor` is gone from `src/Server/Core/CoreMailboxApi.fs`.
[x] A whole-word search of `src/` and `tests/` for `coreMailboxStartActor` is empty.
[x] `coreMailboxStartActorFromFunction` stays in `src/Server/Core/CoreMailboxApi.fs`.
[x] `Order.StartActor` stays.
[x] `placeOrder` in `src/Server/Core/CoreMailboxLoop.fs` replies `Error "StartActorDeprecated is retired"` for `Order.StartActorDeprecated`.
[x] `dispatchStartActor` is gone.
[x] `replyStarted` is gone.
[x] `dispatchActorStartResult` stays.
[x] `CoreMailboxLoad.dispatchStartLoadSaveCommand` calls `dispatchActorStartResult`.
[x] `finishReadyStart` stays with that call.
[x] `EventBody.ActorStart` stays in `src/Shared/History.fs`.
[x] `EventBody.ActorStart` builds `EventBody.Order` of `Order.StartActorDeprecated`.
[x] ``Peer Actor is not available through the Run actor table`` is deleted.
[x] ``unregistered Actor name fails start`` is deleted.

### Reference

[x] [Arch](../../arch.md).

## Do not load

[x] `Order.StartActorDeprecated`, its codec, and its log arm. 16 — Delete the old Order cases owns them.
[x] `EventBody.ActorStart` writers. [24 — Move the remaining ActorStart writers onto StartActor](../24-move-the-remaining-actorstart-writers/24-move-the-remaining-actorstart-writers-ticket.md) owns them.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] ``Peer Actor is not available through the Run actor table`` and ``unregistered Actor name fails start`` are deleted.
3. [x] Delete `coreMailboxStartActor`.
4. [x] Search `src/` and `tests/` for `coreMailboxStartActor` as a whole word. The search is empty.

## Outputs

[x] `src/Server/Core/CoreMailboxActors.fs`
[x] `src/Server/Core/CoreMailboxApi.fs`
[x] `src/Server/Core/CoreMailboxLoop.fs`
[x] `tests/Server.Tests/AgentAskTests.fs`
[x] `tests/Server.Tests/AskCancelHarness.fs`
[x] `tests/Server.Tests/CancelByFocusTests.fs`
[x] `tests/Server.Tests/CoreActorPoolDeliverTests.fs`
[x] `tests/Server.Tests/CoreMailboxDoorTests.fs`
[x] `tests/Server.Tests/CoreMsgActorCasesTests.fs`
[x] `tests/Server.Tests/GithubTransportActorTests.fs`
[x] `tests/Server.Tests/Issue41CoreMailboxTests.fs`
[x] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
[x] `tests/Server.Tests/PersistHandlersRestoreTests.fs`
[x] `tests/Server.Tests/TestActorCommandErrorTests.fs`

## Stop when

[x] Rewrite `25-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when a search hit remains outside Outputs.
[ ] Stop when the edit deletes `coreMailboxStartActorFromFunction` or `Order.StartActor`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `GithubTransportActorTests`
  - `tests/Server.Tests`: `TestActorCommandErrorTests`
[x] All executed tests pass.
[x] Rewrite `25-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
