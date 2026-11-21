# 06 — The actor re-runs on a node that has children
**Status**: needs-info
One job: A person who runs a `>` node again sees a defined result when the node already has children. The job comes from open question 5 in [spec](../spec.md).

Type: coding
Blockers: [03 — The actor builds children from stdout](03-actor-builds-children-from-stdout.md)

Needs-info: open question 5 in the [spec](../spec.md) holds the whole behavior of this ticket.

## Inputs

### Working

[ ] `src/Client/Commands.fs`: `execAmbleRunOp`. The Amble path deletes the focus node's children before it runs. This is a pattern, not a decision.

### Reference

[ ] [25 — The server evaluates Run](../../online-search/issues/25-server-evaluates-run.md). The Focus node's children stay as they are on that path.

## Do not load

[ ] The shell run. That is ticket 02.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Wait for the answer to the open question named above. Then write the edits.

## Outputs

[ ] Files and tests wait on the answer to the open question.

## Stop when

[ ] Rewrite `06-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The edit chooses an open question.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names. The tests are named when the ticket leaves `needs-info`.
[ ] All executed tests pass.
[ ] Rewrite `06-build.md` with the present line. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
