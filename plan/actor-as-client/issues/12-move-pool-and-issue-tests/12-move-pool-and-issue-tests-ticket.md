# 12 — Move pool and issue tests
**Status**: defined
One job: The pool deliver tests and the issue mailbox tests start an Actor with `startFunction` or with `postActorOrder`.

[ ] Four test files. Same split as 11 — Move door and case tests.

Type:

[ ] coding

Blockers:

[ ] [06 — Log ActorOrder](../06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md)

## Inputs

### Working

[ ] `tests/Server.Tests/CoreActorPoolDeliverTests.fs` calls `pool.startActor`.
[ ] `tests/Server.Tests/Issue41CoreMailboxTests.fs` builds a `CoreActorPool` literal and starts an Actor.
[ ] `tests/Server.Tests/Issue42PersistHandlersTests.fs` builds two `CoreActorPool` literals.
[ ] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` builds a `CoreActorPool` literal.

### Reference

[ ] [Arch](../../arch.md).
[ ] [Size limit](../../context/size-limit.md).

## Do not load

[ ] 11 — Move door and case tests, 13 — Move persist and command tests, and 14 — Move runtime and session tests.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: A started Actor uses `startFunction`, or `postActorOrder` of an `ActorOrder` whose `actor` is that command's `FunctionActor`.
3. [ ] Keep each literal's other fields until 15 — Delete the old Actor starts.

## Outputs

[ ] `tests/Server.Tests/CoreActorPoolDeliverTests.fs`
[ ] `tests/Server.Tests/Issue41CoreMailboxTests.fs`
[ ] `tests/Server.Tests/Issue42PersistHandlersTests.fs`
[ ] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/12-move-pool-and-issue-tests-stop.md`, then ask.
[ ] Stop when a fifth test file must change.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `CoreActorPoolDeliverTests`
  - `tests/Server.Tests`: `Issue41CoreMailboxTests`
  - `tests/Server.Tests`: `Issue42PersistHandlersTests`
  - `tests/Server.Tests`: `MailboxHistoryDurabilityTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/12-move-pool-and-issue-tests-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
