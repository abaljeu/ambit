# 07 — Document the command-line actor
**Status**: defined
One job: A reader of the user and current docs learns what Run does on a `>` line, as the decisions in [spec](../spec.md) state.

Type: coding
Blockers:

- [01 — Run picks the command-line actor](01-run-picks-the-command-line-actor.md)
- [03 — The actor builds children from stdout](03-actor-builds-children-from-stdout.md)
- [04 — The actor handles stderr and the exit code](04-actor-handles-stderr-and-exit-code.md)
- [05 — The actor stops on Cancel](05-actor-stops-on-cancel.md)
- [06 — The actor re-runs on a node that has children](06-actor-re-run-with-existing-children.md)

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
2. [ ] After the Blockers pass review, rewrite the `>` entry in `doc/user/reference/commands/run.md` to the decisions in the spec, and remove the Amble `>` text.
3. [ ] Add `CommandActor` to `doc/current/actors.md` and `doc/user/reference/actor.md`.
4. [ ] Move shell `>` out of the Later slices in `doc/roadmap/amble-run.md`, and note that the AMBLE language will later encompass exec.

## Outputs

[ ] The four docs agree with the shipped behavior.

## Stop when

[ ] Rewrite `07-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The edit changes a decision in the [spec](../spec.md).

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] All executed tests pass.
[ ] Rewrite `07-build.md` with the present line. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
