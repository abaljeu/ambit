# 06 — The actor re-runs on a node that has children
**Status**: defined
One job: A re-run of a `>` node replaces the node's existing children with the new output. The job comes from the Output criteria in [spec](../spec.md).

Type: coding
Blockers: [03 — The actor builds children from stdout](03-actor-builds-children-from-stdout.md)

## Inputs

### Working

[ ] `src/Client/Commands.fs`: `execAmbleRunOp`. The Amble path deletes the focus node's children before it runs. This is a pattern.

### Reference

[ ] [25 — The server evaluates Run](../../online-search/issues/25-server-evaluates-run.md). The Focus node's children stay as they are on that path.

## Do not load

[ ] The shell run. That is ticket 02.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Before the first new output line, remove the start node's existing children, then add the new output as in [03 — The actor builds children from stdout](03-actor-builds-children-from-stdout.md).

## Outputs

[ ] Re-run replacement in `CommandActor`.
[ ] A test for the re-run criterion in the [spec](../spec.md). The test name is chosen at build.

## Stop when

[ ] Rewrite `06-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The edit changes a decision in the [spec](../spec.md).

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names. The tests are named in Outputs.
[ ] All executed tests pass.
[ ] Rewrite `06-build.md` with the present line. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
