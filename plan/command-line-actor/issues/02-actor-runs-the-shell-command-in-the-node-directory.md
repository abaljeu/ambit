# 02 — The actor runs the shell command in the node directory
**Status**: needs-info
One job: The command-line actor runs the text after the `>` as a shell command in the Azure environment, with the focus node's directory as the working directory. The job comes from the Command and the Environment and directory criteria in [spec](../spec.md).

Type: coding
Blockers: [01 — Run picks the command-line actor](01-run-picks-the-command-line-actor.md)

Needs-info: open questions 6, 10, 11, 12, 13, 14, and 20 in the [spec](../spec.md) gate the shell run and the directory.

## Inputs

### Working

[ ] `src/Server/Core/CoreActorPool.fs`: `startFunction`. The pool starts an actor function.
[ ] `src/Server/RouteRegistration.fs`: `BootActors`. The record installs the named actors.
[ ] `src/Server/SearchActor.fs`: `queryActorBody`. This is the nearest actor pattern.
[ ] `src/Shared/GraphQuery.fs`: `enclosing` and `enclosingContainer`. The enclosing artifact is where a node is persisted.

### Reference

[ ] [Actors](../../../doc/current/actors.md)
[ ] [Deploying to Azure](../../../doc/reference/deploy-azure.md)
[ ] [Persistence model](../../../doc/current/persistence-model.md)
[ ] [Workspace local mapping](../../../doc/current/workspace-local-mapping.md)
[ ] [GLOSSARY.md](../../../GLOSSARY.md)

## Do not load

[ ] The Amble `=` and `?` paths.
[ ] Child building. That is [03 — The actor builds children from stdout](03-actor-builds-children-from-stdout.md).

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Wait for the answers to the open questions named above. Then write the edits.

## Outputs

[ ] Files and tests wait on the answers to the open questions.

## Stop when

[ ] Rewrite `02-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The edit chooses an open question.
[ ] The edit invents a hosting or path model.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names. The tests are named when the ticket leaves `needs-info`.
[ ] All executed tests pass.
[ ] Rewrite `02-build.md` with the present line. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
