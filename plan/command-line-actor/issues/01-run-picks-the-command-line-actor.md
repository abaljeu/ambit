# 01 — Run picks the command-line actor
**Status**: needs-info
One job: A person who runs a node whose text starts with `>` starts the command-line actor. The job comes from the Pick criteria in [spec](../spec.md).

Type: coding
Blockers: none

Needs-info: open questions 7, 8, 9, 13, 15, and 16 in the [spec](../spec.md) gate the Run choice. Each one names the rule this ticket would write.

## Inputs

### Working

[ ] `src/Client/Commands.fs`: `execRunOp`, `submitQuery`, and `execAmbleRunOp`. `submitQuery` runs before the `>` check.
[ ] `src/Shared/CommandRequest.fs`: `isCommandText`, `isQueryText`, and `tryStart`.
[ ] `src/Shared/AmbleRun.fs`: `shouldExec` and `runPlan`. The `>` test is in `shouldExec` today.

### Reference

[ ] [22 — Run sends = lines to Amble](../../online-search/issues/22-run-sends-equals-lines-to-amble.md)
[ ] [Run](../../../doc/user/reference/commands/run.md)

## Do not load

[ ] The Amble `=` and `?` paths that ticket 22 owns.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Wait for the answers to the open questions named above. Then write the edits.

## Outputs

[ ] Files and tests wait on the answers to the open questions.

## Stop when

[ ] Rewrite `01-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The edit chooses an open question.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names. The tests are named when the ticket leaves `needs-info`.
[ ] All executed tests pass.
[ ] Rewrite `01-build.md` with the present line. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
