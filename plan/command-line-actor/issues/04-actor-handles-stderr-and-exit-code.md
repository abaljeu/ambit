# 04 — The actor handles stderr and the exit code
**Status**: defined
One job: `CommandActor` captures stderr with stdout, interleaved in arrival order, and a non-zero exit code sets the cssclass `error` on the `>` node. A successful run clears the `error` class. The job comes from the Output criteria in [spec](../spec.md).

Type: coding
Blockers: [02 — The actor runs the command in the node directory](02-actor-runs-the-command-in-the-node-directory.md)

## Inputs

### Working

[ ] [ ] `src/Shared/AmbleRun.fs`: `redletterClasses` and its `Op.SetClasses`. This is the pattern for setting a cssclass.

### Reference

[ ] [Run](../../../doc/user/reference/commands/run.md)

## Do not load

[ ] Child building from stdout. That is ticket 03.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Read stdout and stderr as one stream of output lines in arrival order, so the children ticket builds children from both.
3. [ ] On a non-zero exit, set the cssclass `error` on the start node and add no child. On a zero exit, clear the class.

## Outputs

[ ] Interleaved capture and the exit mark in `CommandActor`.
[ ] Tests for the stderr and exit code criteria in the [spec](../spec.md). The test names are chosen at build.

## Stop when

[ ] Rewrite `04-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The edit changes a decision in the [spec](../spec.md).

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names. The tests are named in Outputs.
[ ] All executed tests pass.
[ ] Rewrite `04-build.md` with the present line. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
