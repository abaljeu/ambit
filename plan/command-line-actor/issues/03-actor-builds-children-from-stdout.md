# 03 — The actor builds children from stdout
**Status**: needs-info
One job: The command-line actor captures stdout and builds the focus node's child nodes from it. The job comes from the Output criteria in [spec](../spec.md).

Type: coding
Blockers: [02 — The actor runs the shell command in the node directory](02-actor-runs-the-shell-command-in-the-node-directory.md)

Needs-info: open questions 1 and 19 in the [spec](../spec.md) gate the child shape and the size.

## Inputs

### Working

[ ] `src/Shared/ExprRun.fs`: `addAnswer` and `maxMaterialisedAnswers`. A Text Answer becomes an owned child. This is a pattern, not a decision.
[ ] `src/Server/SearchActor.fs`: `queryActorBody`. The query actor posts no ops today.

### Reference

[ ] [Query Actor](../../../doc/current/query-actor.md)
[ ] [25 — The server evaluates Run](../../online-search/issues/25-server-evaluates-run.md)

## Do not load

[ ] The shell run. That is ticket 02.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Wait for the answers to the open questions named above. Then write the edits.

## Outputs

[ ] Files and tests wait on the answers to the open questions.

## Stop when

[ ] Rewrite `03-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The edit chooses an open question.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names. The tests are named when the ticket leaves `needs-info`.
[ ] All executed tests pass.
[ ] Rewrite `03-build.md` with the present line. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
