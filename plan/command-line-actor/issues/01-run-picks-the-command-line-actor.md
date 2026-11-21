# 01 — Run picks the command-line actor
**Status**: defined
One job: Run on a node whose first character is `>` posts an order to `CommandActor`, and a bare `>` does nothing. The job comes from the Pick criteria in [spec](../spec.md).

Type: coding
Blockers: none

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
2. [ ] In `src/Shared/CommandRequest.fs`, add the first-character `>` test with no trimming. A bare `>` has no program.
3. [ ] In `src/Client/Commands.fs`, make `execRunOp` test `>` before `submitQuery`, so `>` wins over `=`. Post the Command order for the start node. Post nothing for a bare `>`.
4. [ ] Remove the client Amble `>` path (`execAmbleRunOp` and `legacyRun` for `>` lines) and the trimmed `>` test in `src/Shared/AmbleRun.fs`.
5. [ ] Apply the same test to a `>` line that the owner scan reaches, with no special case for File, Directory, or Workspace nodes.

## Outputs

[ ] The Run choice and the Command order, with no process run.
[ ] Tests in `tests/Shared.Tests` for the acceptance criteria under Pick in the [spec](../spec.md). The test names are chosen at build.

## Stop when

[ ] Rewrite `01-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The edit changes a decision in the [spec](../spec.md).

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names. The tests are named in Outputs.
[ ] All executed tests pass.
[ ] Rewrite `01-build.md` with the present line. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
