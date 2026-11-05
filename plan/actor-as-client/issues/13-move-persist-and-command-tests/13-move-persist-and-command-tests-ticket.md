# 13 — Move persist and command tests
**Status**: defined
One job: The persist-handler tests and the command-post tests start an Actor with `startFunction` or with `postActorOrder`.

[ ] Four test files. Same split as 11 — Move door and case tests.

Type:

[ ] coding

Blockers:

[ ] [06 — Log ActorOrder](../06-add-the-function-and-focus-start/06-add-the-function-and-focus-start-ticket.md)

## Inputs

### Working

[ ] `tests/Server.Tests/PersistHandlersRestoreTests.fs` builds two `CoreActorPool` literals.
[ ] `tests/Server.Tests/ApiPostCommandTests.fs` boots Actors through `CoreRuntime`.
[ ] `tests/Server.Tests/CredentialedChangePostsTests.fs` boots Actors through `CoreRuntime`.
[ ] `tests/Server.Tests/OrderedEventStreamTests.fs` starts an Actor.

### Reference

[ ] [Arch](../../arch.md).
[ ] [Size limit](../../context/size-limit.md).

## Do not load

[ ] 11 — Move door and case tests, 12 — Move pool and issue tests, and 14 — Move runtime and session tests.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: A started Actor uses `startFunction`. A booted command uses that command's `FunctionActor` and `postActorOrder`.
3. [ ] Keep old pool fields until 15 — Delete the old Actor starts.

## Outputs

[ ] `tests/Server.Tests/PersistHandlersRestoreTests.fs`
[ ] `tests/Server.Tests/ApiPostCommandTests.fs`
[ ] `tests/Server.Tests/CredentialedChangePostsTests.fs`
[ ] `tests/Server.Tests/OrderedEventStreamTests.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/13-move-persist-and-command-tests-stop.md`, then ask.
[ ] Stop when a fifth test file must change.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `PersistHandlersRestoreTests`
  - `tests/Server.Tests`: `ApiPostCommandTests`
  - `tests/Server.Tests`: `CredentialedChangePostsTests`
  - `tests/Server.Tests`: `OrderedEventStreamTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/13-move-persist-and-command-tests-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
