# 03 — The actor builds children from stdout
**Status**: defined
One job: `CommandActor` turns each output line into one plain child node of the focus node, up to 1000 lines, then adds a final child reading `output truncated`. The job comes from the Output criteria in [spec](../spec.md).

Type: coding
Blockers: [02 — The actor runs the command in the node directory](02-actor-runs-the-command-in-the-node-directory.md)

## Inputs

### Working

[ ] `src/Shared/ExprRun.fs`: `addAnswer` and `maxMaterialisedAnswers`. A Text Answer becomes an owned child. This is a pattern.
[ ] `src/Server/SearchActor.fs`: `queryActorBody`. The query actor posts no ops today.

### Reference

[ ] [Query Actor](../../../doc/current/query-actor.md)
[ ] [25 — The server evaluates Run](../../online-search/issues/25-server-evaluates-run.md)

## Do not load

[ ] The shell run. That is ticket 02.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Build one plain child per output line, with no outline parsing.
3. [ ] At 1000 lines stop adding children and add one final child reading `output truncated`. The process keeps running until it exits or is cancelled.

## Outputs

[ ] Child build in `CommandActor`.
[ ] Tests for the Output criteria on children and the cap in the [spec](../spec.md). The test names are chosen at build.

## Stop when

[ ] Rewrite `03-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The edit changes a decision in the [spec](../spec.md).

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names. The tests are named in Outputs.
[ ] All executed tests pass.
[ ] Rewrite `03-build.md` with the present line. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
