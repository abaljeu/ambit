# 11 — Move door and case tests
**Status**: defined
One job: The mailbox door tests and the actor-case tests start an Actor with `startFunction`.

[ ] This batch is four test files. The caller move is larger than the size limit, so the Server test files are split across 11, 12, 13, and 14.

Type:

[ ] coding

Blockers:

[ ] [07 — Dispatch the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs` calls `CoreMailboxApi.startActor` and builds `CoreActorPool` literals.
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs` does the same.
[ ] `tests/Server.Tests/ActorCoreChangesDoorTests.fs` registers an Actor and starts it.
[ ] `tests/Server.Tests/AskCancelHarness.fs` starts an Actor through the mailbox API.

### Reference

[ ] [Actor as client](../../project.md).
[ ] [Size limit](../../context/size-limit.md).

## Do not load

[ ] Production `startActor` callers. Tickets 07 through 10 own those.
[ ] The other batches are 12 — Move pool and issue tests, 13 — Move persist and command tests, and 14 — Move runtime and session tests.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change each named file so a started Actor uses `startFunction`. The function input is `FunctionRun` and `CoreChangesDoor`.
3. [ ] A test that only needs the name `test` goes through the dispatcher.
4. [ ] Leave `startPeerActor` and `registerPeer` to the batch that owns that file.

## Outputs

[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs`
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs`
[ ] `tests/Server.Tests/ActorCoreChangesDoorTests.fs`
[ ] `tests/Server.Tests/AskCancelHarness.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/11-move-door-and-case-tests-stop.md`, then ask.
[ ] Stop when a fifth test file must change.
[ ] Stop when a production file outside the dispatcher must change.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `CoreMailboxDoorTests`
  - `tests/Server.Tests`: `CoreMsgActorCasesTests`
  - `tests/Server.Tests`: `ActorCoreChangesDoorTests`
  - `tests/Server.Tests`: `AskCancelHarness`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/11-move-door-and-case-tests-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
