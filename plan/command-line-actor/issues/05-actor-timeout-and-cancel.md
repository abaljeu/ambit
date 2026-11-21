# 05 — The actor has a timeout and stops on Cancel
**Status**: needs-info
One job: A person can stop a command that runs too long or that Cancel stops. The job comes from open questions 4 and 18 in [spec](../spec.md).

Type: coding
Blockers: [02 — The actor runs the shell command in the node directory](02-actor-runs-the-shell-command-in-the-node-directory.md)

Needs-info: open questions 4, 17, and 18 in the [spec](../spec.md) hold the whole behavior of this ticket.

## Inputs

### Working

[ ] `src/Client/Commands.fs`: `execCancelOp` and `cancelFocusOp`.
[ ] `src/Server/Core/CoreMailboxActors.fs`: `runCancel`.
[ ] `src/Server/Core/CoreActorPool.fs`: the termination handle of a live Actor.

### Reference

[ ] [Actors](../../../doc/current/actors.md)
[ ] [Run](../../../doc/user/reference/commands/run.md)

## Do not load

[ ] Child building from stdout. That is ticket 03.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Wait for the answers to the open questions named above. Then write the edits.

## Outputs

[ ] Files and tests wait on the answers to the open questions.

## Stop when

[ ] Rewrite `05-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The edit chooses an open question.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names. The tests are named when the ticket leaves `needs-info`.
[ ] All executed tests pass.
[ ] Rewrite `05-build.md` with the present line. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
