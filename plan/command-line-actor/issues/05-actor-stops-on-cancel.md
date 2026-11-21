# 05 — The actor stops on Cancel
**Status**: defined
One job: Cancel on the focused node kills the process of a live `CommandActor`. The command has no timeout. The job comes from the Cancel criteria in [spec](../spec.md). The timeout this ticket once held is dropped.

Type: coding
Blockers: [02 — The actor runs the command in the node directory](02-actor-runs-the-command-in-the-node-directory.md)

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
2. [ ] Run `CommandActor` on its own thread and make it inherit the actor cancel. Cancel kills the process.
3. [ ] Add no timeout.
4. [ ] Keep the refusal of a second Run while the actor is live, as for other actors.

## Outputs

[ ] Cancel that kills the process.
[ ] Tests for the Cancel criteria and the live-actor refusal in the [spec](../spec.md). The test names are chosen at build.

## Stop when

[ ] Rewrite `05-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The edit changes a decision in the [spec](../spec.md).

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names. The tests are named in Outputs.
[ ] All executed tests pass.
[ ] Rewrite `05-build.md` with the present line. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
