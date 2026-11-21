# 07 — Document the command-line actor
**Status**: needs-info
One job: A reader of the user and current docs learns what Run does on a `>` line. The job comes from the Existing behavior section in [spec](../spec.md).

Type: coding
Blockers: [01 — Run picks the command-line actor](01-run-picks-the-command-line-actor.md), [03 — The actor builds children from stdout](03-actor-builds-children-from-stdout.md)

Needs-info: the docs state behavior that open questions 1 to 21 in the [spec](../spec.md) still hold.

## Inputs

### Working

[ ] `doc/user/reference/commands/run.md`
[ ] `doc/user/reference/actor.md`
[ ] `doc/current/actors.md`
[ ] `doc/roadmap/amble-run.md`. Its Later slices list names shell `>` commands.

### Reference

[ ] [Maintain doc currency](../../../.agents/skills/maintain-doc-currency/SKILL.md)

## Do not load

[ ] Source and tests.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Wait for the tickets named in Blockers. Then write the edits.

## Outputs

[ ] Files wait on the answers to the open questions.

## Stop when

[ ] Rewrite `07-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The edit states behavior that an open question holds.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] All executed tests pass.
[ ] Rewrite `07-build.md` with the present line. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
