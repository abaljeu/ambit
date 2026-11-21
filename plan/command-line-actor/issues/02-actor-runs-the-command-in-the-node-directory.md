# 02 — The actor runs the command in the node directory
**Status**: defined
One job: `CommandActor` starts the program named by the first word after the `>`, with the rest of the line as arguments and no shell, in the focus node's enclosing directory, on the server. The job comes from the Command and Directory criteria in [spec](../spec.md).

Type: coding
Blockers: [01 — Run picks the command-line actor](01-run-picks-the-command-line-actor.md)

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
2. [ ] Add `CommandActor` in `src/Server`, started like `queryActorBody` in `src/Server/SearchActor.fs`, and install it in `BootActors`. It runs on its own thread and a second Run while it is live is refused.
3. [ ] Derive the working directory with `enclosing` in `src/Shared/GraphQuery.fs`. A File Node or Directory Node uses its directory. Any other node uses the nearest enclosing directory.
4. [ ] Start the process with `Process.Start`, the first word as file name, the rest as arguments, no shell, and the server environment.
5. [ ] Put the process behind a seam so tests use a fake runner.

## Outputs

[ ] `CommandActor` that runs the process in the directory, with no children built yet.
[ ] Tests for the Command and Directory criteria in the [spec](../spec.md), using a fake runner. The test names are chosen at build.

## Stop when

[ ] Rewrite `02-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] The edit changes a decision in the [spec](../spec.md).
[ ] The edit invents a hosting or path model.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names. The tests are named in Outputs.
[ ] All executed tests pass.
[ ] Rewrite `02-build.md` with the present line. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
