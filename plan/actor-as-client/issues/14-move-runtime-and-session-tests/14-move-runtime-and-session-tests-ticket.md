# 14 — Move runtime and session tests
**Status**: defined
One job: The Core boot tests and the session tests start an Actor with `startFunction` or with `postActorOrder`.

[ ] Two test files. They are the remainder after the four-file batches.

Type:

[ ] coding

Blockers:

[ ] [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `tests/Server.Tests/CoreRuntimeTests.fs` boots `CoreBoot.Actors` and `pool.register`.
[ ] `tests/Server.Tests/SessionTests.fs` boots Actors through that same boot.

### Reference

[ ] [Arch](../../arch.md).
[ ] [Size limit](../../context/size-limit.md).

## Do not load

[ ] 11 — Move door and case tests, 12 — Move pool and issue tests, and 13 — Move persist and command tests.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: A direct pool start calls `startFunction`. A booted command uses that command's `ActorFunction` and `postActorOrder`. `ActorFunction` is in `src/Shared/History.fs`.
3. [ ] Keep old pool fields until 15 — Delete the old Actor starts.

## Outputs

[ ] `tests/Server.Tests/CoreRuntimeTests.fs`
[ ] `tests/Server.Tests/SessionTests.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/14-move-runtime-and-session-tests-stop.md`, then ask.
[ ] Stop when a third test file must change. That file is a missed batch.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `CoreRuntimeTests`
  - `tests/Server.Tests`: `SessionTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/14-move-runtime-and-session-tests-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
