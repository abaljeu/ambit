# 25 — Remove the coreMailboxStartActor door
**Status**: defined
One job: `CoreMailboxApi.coreMailboxStartActor` is gone. No test calls it, and nothing places `Order.StartActorDeprecated` through it.

[ ] [16 — Delete the old Order cases](../16-delete-the-old-order-cases/16-delete-the-old-order-cases-ticket.md) depends on this ticket. 16 deletes `Order.StartActorDeprecated` once this door no longer places it.

Type:

[ ] coding

Blockers:

[ ] [11 — Move door and case tests](../11-move-door-and-case-tests/11-move-door-and-case-tests-ticket.md) moves `CoreMailboxDoorTests.fs`, `CoreMsgActorCasesTests.fs`, and `AskCancelHarness.fs`.
[ ] [12 — Move pool and issue tests](../12-move-pool-and-issue-tests/12-move-pool-and-issue-tests-ticket.md) moves `CoreActorPoolDeliverTests.fs`, `Issue41CoreMailboxTests.fs`, and `MailboxHistoryDurabilityTests.fs`.
[ ] [13 — Move persist and command tests](../13-move-persist-and-command-tests/13-move-persist-and-command-tests-ticket.md) moves `PersistHandlersRestoreTests.fs`.
[ ] [22 — Convert CoreMsg.PostEvent](../22-convert-coremsg-postevent/22-convert-coremsg-postevent-ticket.md) keeps `coreMailboxStartActor` in ``mailbox appends ActorStart and ActorStop in lifecycle order`` in `Issue41CoreMailboxTests.fs`. That use must be gone first.

## Inputs

### Working

[ ] `coreMailboxStartActor` in `src/Server/Core/CoreMailboxApi.fs` places `Order.StartActorDeprecated request` through `MailboxHost.postPlacedOrder`.
[ ] `coreMailboxStartActorFromFunction` in the same file places `Order.StartActor` from a supplied function.
[ ] `tests/Server.Tests/GithubTransportActorTests.fs` calls `coreMailboxStartActor`. No other ticket moves that call.
[ ] `tests/Server.Tests/TestActorCommandErrorTests.fs` calls `coreMailboxStartActor`. No other ticket moves that call.

### Reference

[ ] [Arch](../../arch.md).

## Do not load

[ ] The test files that 11, 12, and 13 move.
[ ] `Order.StartActorDeprecated`, its codec, and its log arm. 16 owns them.
[ ] `EventBody.ActorStart` writers. 24 — Move the remaining ActorStart writers onto StartActor owns them.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Move the calls in `GithubTransportActorTests.fs` and `TestActorCommandErrorTests.fs` onto `coreMailboxStartActorFromFunction` or `postActorOrder`.
3. [x] Delete `coreMailboxStartActor`.
4. [x] Search `src/` and `tests/` for `coreMailboxStartActor` as a whole word. The search is empty.

## Outputs

[ ] `src/Server/Core/CoreMailboxApi.fs`
[ ] `tests/Server.Tests/GithubTransportActorTests.fs`
[ ] `tests/Server.Tests/TestActorCommandErrorTests.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/25-remove-the-coremailboxstartactor-door-stop.md`, then ask.
[ ] Stop when a search hit remains outside Outputs.
[ ] Stop when the edit deletes `coreMailboxStartActorFromFunction` or `Order.StartActor`.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `GithubTransportActorTests`
  - `tests/Server.Tests`: `TestActorCommandErrorTests`
[x] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/25-remove-the-coremailboxstartactor-door-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
